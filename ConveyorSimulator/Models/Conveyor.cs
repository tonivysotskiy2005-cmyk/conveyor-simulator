using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ConveyorSimulator.Models
{
    public sealed class Conveyor : IDisposable
    {
        private const int MaterialsPerPart = 3;
        private const int PartSpacing = 20;
        private const int WorkIntervalMilliseconds = 500;
        private static int _nextConveyorId;

        private readonly object _stateLock = new object();
        private readonly List<Part> _parts = new List<Part>();
        private readonly Random _random;
        private readonly double _breakdownProbability;
        private readonly ManualResetEventSlim _stopSignal = new ManualResetEventSlim(false);
        private Thread _workerThread;
        private bool _isRunning;
        private bool _isBroken;
        private bool _disposed;
        private int _materialsCount;
        private int _partsLimit;
        private int _breakdownCount;
        private int _localPartCounter;

        public delegate void PartCreatedHandler(Part part);
        public event PartCreatedHandler OnPartCreated;
        public delegate void MaterialsChangedHandler(int count);
        public event MaterialsChangedHandler OnMaterialsChanged;
        public delegate void ConveyorStatusChangedHandler(bool isRunning, bool isBroken);
        public event ConveyorStatusChangedHandler OnStatusChanged;
        public event EventHandler MaterialsDepleted;
        public event EventHandler<string> OnConveyorEvent;

        public int ConveyorId { get; }
        public int ConveyorSpeed { get; }
        public int Width { get; }
        public bool IsRunning { get { lock (_stateLock) { return _isRunning; } } }
        public bool IsBroken { get { lock (_stateLock) { return _isBroken; } } }
        public int MaterialsCount { get { lock (_stateLock) { return _materialsCount; } } }

        public Conveyor(int width, int initialMaterials, int conveyorSpeed, int partsLimit,
            double breakdownProbability)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (initialMaterials < 0) throw new ArgumentOutOfRangeException(nameof(initialMaterials));
            if (conveyorSpeed <= 0) throw new ArgumentOutOfRangeException(nameof(conveyorSpeed));
            if (partsLimit <= 0 || partsLimit > 30) throw new ArgumentOutOfRangeException(nameof(partsLimit));
            if (breakdownProbability < 0 || breakdownProbability > 1)
                throw new ArgumentOutOfRangeException(nameof(breakdownProbability));

            ConveyorId = Interlocked.Increment(ref _nextConveyorId);
            Width = width;
            ConveyorSpeed = conveyorSpeed;
            _materialsCount = initialMaterials;
            _partsLimit = partsLimit;
            _breakdownProbability = breakdownProbability;
            _random = new Random(unchecked(Environment.TickCount * 31 + ConveyorId));
        }

        public int GetConveyorId() => ConveyorId;

        public void Start()
        {
            lock (_stateLock)
            {
                if (_disposed) return;
                if (_isRunning || _isBroken) return;
                _isRunning = true;
                _stopSignal.Reset();
                _workerThread = new Thread(WorkLoop)
                {
                    IsBackground = true,
                    Name = $"Conveyor-{ConveyorId}-Worker"
                };
                _workerThread.Start();
            }

            RaiseStatusChanged();
            RaiseConveyorEvent($"Конвейер #{ConveyorId} запущен в потоке {_workerThread.ManagedThreadId}");
        }

        public void Stop()
        {
            Thread threadToJoin;
            bool statusChanged;
            lock (_stateLock)
            {
                statusChanged = _isRunning;
                _isRunning = false;
                _stopSignal.Set();
                threadToJoin = _workerThread;
            }

            if (threadToJoin != null && threadToJoin != Thread.CurrentThread && threadToJoin.IsAlive)
                threadToJoin.Join(1000);

            if (statusChanged)
            {
                RaiseStatusChanged();
                RaiseConveyorEvent($"Конвейер #{ConveyorId} остановлен");
            }
        }

        private void WorkLoop()
        {
            while (!_stopSignal.Wait(WorkIntervalMilliseconds))
            {
                if (!ProcessTick()) return;
            }
        }

        private bool ProcessTick()
        {
            Part createdPart = null;
            int? materialCount = null;
            string eventMessage = null;
            bool broke = false;
            bool depleted = false;

            lock (_stateLock)
            {
                if (!_isRunning || _isBroken) return false;
                if (_random.NextDouble() < _breakdownProbability)
                {
                    _isBroken = true;
                    _isRunning = false;
                    _breakdownCount++;
                    _stopSignal.Set();
                    broke = true;
                    eventMessage = $"Авария! Конвейер #{ConveyorId} сломался! (Всего поломок: {_breakdownCount})";
                }
                else if (_materialsCount < MaterialsPerPart)
                {
                    _isRunning = false;
                    _stopSignal.Set();
                    depleted = true;
                    eventMessage = $"Конвейер #{ConveyorId}: материалы закончились. Вызван погрузчик.";
                }
                else
                {
                    MovePartsLocked();
                    if (CanCreatePartLocked())
                    {
                        createdPart = CreatePartLocked();
                        _materialsCount -= MaterialsPerPart;
                        materialCount = _materialsCount;
                        eventMessage = $"Создана {createdPart}";
                    }
                }
            }

            if (broke || depleted) RaiseStatusChanged();
            if (createdPart != null) OnPartCreated?.Invoke(createdPart);
            if (materialCount.HasValue) OnMaterialsChanged?.Invoke(materialCount.Value);
            if (eventMessage != null) RaiseConveyorEvent(eventMessage);
            if (depleted) MaterialsDepleted?.Invoke(this, EventArgs.Empty);
            return !broke && !depleted;
        }

        private void MovePartsLocked()
        {
            for (int i = _parts.Count - 1; i >= 0; i--)
            {
                _parts[i].PositionX += ConveyorSpeed;
                if (_parts[i].PositionX >= Width) _parts.RemoveAt(i);
            }
        }

        private bool CanCreatePartLocked()
        {
            return _parts.Count < _partsLimit && _materialsCount >= MaterialsPerPart &&
                   _parts.All(part => part.PositionX >= PartSpacing);
        }

        private Part CreatePartLocked()
        {
            _localPartCounter++;
            var part = new Part(_localPartCounter, ConveyorId);
            _parts.Add(part);
            return part;
        }

        public void Repair()
        {
            lock (_stateLock)
            {
                if (_disposed) return;
                if (!_isBroken) return;
                _isBroken = false;
            }
            RaiseConveyorEvent($"Механик починил конвейер #{ConveyorId}");
            RaiseStatusChanged();
            Start();
        }

        public void LoadMaterials(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int total;
            bool shouldStart;
            lock (_stateLock)
            {
                if (_disposed) return;
                _materialsCount += amount;
                total = _materialsCount;
                shouldStart = !_isRunning && !_isBroken;
            }
            OnMaterialsChanged?.Invoke(total);
            RaiseConveyorEvent($"Конвейер #{ConveyorId}: загружено {amount} материалов. Всего: {total}");
            if (shouldStart) Start();
        }

        public string GetStatus()
        {
            lock (_stateLock)
            {
                if (_isBroken) return "СЛОМАН";
                return _isRunning ? "РАБОТАЕТ" : "ОСТАНОВЛЕН";
            }
        }

        public void SetPartsLimit(int newLimit)
        {
            if (newLimit <= 0 || newLimit > 30) throw new ArgumentOutOfRangeException(nameof(newLimit));
            lock (_stateLock) { _partsLimit = newLimit; }
            RaiseConveyorEvent($"Конвейер #{ConveyorId}: лимит деталей изменён на {newLimit}");
        }

        public int GetPartsLimit() { lock (_stateLock) { return _partsLimit; } }
        public int GetBreakdownCount() { lock (_stateLock) { return _breakdownCount; } }
        public int GetLocalPartCounter() { lock (_stateLock) { return _localPartCounter; } }

        public IReadOnlyList<Part> GetPartsSnapshot()
        {
            lock (_stateLock)
            {
                return _parts.Select(part => part.CreateSnapshot()).ToList();
            }
        }

        private void RaiseStatusChanged() => OnStatusChanged?.Invoke(IsRunning, IsBroken);
        private void RaiseConveyorEvent(string message) => OnConveyorEvent?.Invoke(this, message);

        public void Dispose()
        {
            lock (_stateLock)
            {
                if (_disposed) return;
                _disposed = true;
            }
            Stop();
            _stopSignal.Dispose();
        }
    }
}
