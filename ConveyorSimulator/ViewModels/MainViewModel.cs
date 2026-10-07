using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ConveyorSimulator.Models;

namespace ConveyorSimulator.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private Conveyor _conveyor;
        private Loader _loader;
        private Mechanic _mechanic;

        private string _statusText;
        private int _materialsCount;
        private int _partsCount;
        private int _breakdownCount;
        private bool _isConveyorRunning;
        private bool _isConveyorBroken;
        private string _lastEvent;
        private int _conveyorId;
        private readonly SynchronizationContext _uiContext;

        public event EventHandler<Part> PartAdded;
        public event EventHandler UpdateUI;

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public int MaterialsCount
        {
            get => _materialsCount;
            set { _materialsCount = value; OnPropertyChanged(); }
        }

        public int PartsCount
        {
            get => _partsCount;
            set { _partsCount = value; OnPropertyChanged(); }
        }

        public int BreakdownCount
        {
            get => _breakdownCount;
            set { _breakdownCount = value; OnPropertyChanged(); }
        }

        public bool IsConveyorRunning
        {
            get => _isConveyorRunning;
            set { _isConveyorRunning = value; OnPropertyChanged(); }
        }

        public bool IsConveyorBroken
        {
            get => _isConveyorBroken;
            set { _isConveyorBroken = value; OnPropertyChanged(); }
        }

        public string LastEvent
        {
            get => _lastEvent;
            set { _lastEvent = value; OnPropertyChanged(); }
        }

        public int ConveyorId
        {
            get => _conveyorId;
            set { _conveyorId = value; OnPropertyChanged(); }
        }

        public MainViewModel(SimulationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _uiContext = SynchronizationContext.Current;
            _conveyor = new Conveyor(
                settings.ConveyorWidth,
                settings.InitialMaterials,
                settings.ConveyorSpeed,
                settings.PartsLimit,
                settings.BreakdownProbability);
            _loader = new Loader(_conveyor, settings.LoadAmount,
                settings.MinimumLoadDelay, settings.MaximumLoadDelay);
            _mechanic = new Mechanic(_conveyor,
                settings.MinimumRepairTime, settings.MaximumRepairTime);

            _conveyor.OnPartCreated += OnPartCreated;
            _conveyor.OnMaterialsChanged += OnMaterialsChanged;
            _conveyor.OnStatusChanged += OnStatusChanged;
            _conveyor.OnConveyorEvent += OnConveyorEvent;
            _conveyor.MaterialsDepleted += OnMaterialsDepleted;
            _loader.OnLoaderEvent += OnLoaderEvent;
            _mechanic.OnMechanicEvent += OnMechanicEvent;

            ConveyorId = _conveyor.GetConveyorId();
            UpdateProperties();
        }

        private void OnPartCreated(Part part)
        {
            RunOnUiThread(() => PartAdded?.Invoke(this, part));
            UpdateProperties();
            RaiseUpdateUi();
        }

        private void OnMaterialsChanged(int count)
        {
            MaterialsCount = count;
            RaiseUpdateUi();
        }

        private void OnStatusChanged(bool isRunning, bool isBroken)
        {
            IsConveyorRunning = isRunning;
            IsConveyorBroken = isBroken;
            UpdateProperties();

            if (isBroken)
            {
                _mechanic.RepairConveyorAsync();
            }

            RaiseUpdateUi();
        }

        private async void OnMaterialsDepleted(object sender, EventArgs e)
        {
            await _loader.LoadMaterialsAsync();
        }

        private void OnConveyorEvent(object sender, string message)
        {
            LastEvent = message;
            RaiseUpdateUi();
        }

        private void OnLoaderEvent(object sender, string message)
        {
            LastEvent = message;
            RaiseUpdateUi();
        }

        private void OnMechanicEvent(object sender, string message)
        {
            LastEvent = message;
            RaiseUpdateUi();
        }

        private void UpdateProperties()
        {
            StatusText = _conveyor.GetStatus();
            PartsCount = _conveyor.GetPartsSnapshot().Count;
            MaterialsCount = _conveyor.MaterialsCount;
            BreakdownCount = _conveyor.GetBreakdownCount();
        }

        public void StartConveyor()
        {
            _conveyor.Start();
            UpdateProperties();
        }

        public void StopConveyor()
        {
            _conveyor.Stop();
            UpdateProperties();
        }

        public void ForceLoadMaterials()
        {
            _loader.LoadMaterials();
        }

        public void SetPartsLimit(int newLimit)
        {
            _conveyor.SetPartsLimit(newLimit);
            UpdateProperties();
        }

        public int GetPartsLimit()
        {
            return _conveyor.GetPartsLimit();
        }

        public IReadOnlyList<Part> GetParts()
        {
            return _conveyor.GetPartsSnapshot();
        }

        public int GetConveyorWidth()
        {
            return _conveyor.Width;
        }

        public int GetConveyorId()
        {
            return _conveyor.GetConveyorId();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RunOnUiThread(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        private void RaiseUpdateUi()
        {
            RunOnUiThread(() => UpdateUI?.Invoke(this, EventArgs.Empty));
        }

        private void RunOnUiThread(Action action)
        {
            if (_uiContext == null || SynchronizationContext.Current == _uiContext)
                action();
            else
                _uiContext.Post(state => ((Action)state)(), action);
        }

        public void Dispose()
        {
            _conveyor.Dispose();
        }
    }
}
