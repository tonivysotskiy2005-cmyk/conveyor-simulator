using System;
using System.Threading;
using System.Threading.Tasks;
using ConveyorSimulator.Interfaces;

namespace ConveyorSimulator.Models
{
    public sealed class Mechanic : IMechanicService
    {
        private readonly Conveyor _conveyor;
        private readonly int _minimumRepairTime;
        private readonly int _maximumRepairTime;
        private readonly Random _random = new Random();
        private int _isRepairing;

        public event EventHandler<string> RepairCompleted;
        public event EventHandler<string> OnMechanicEvent;

        public Mechanic(Conveyor conveyor, int minimumRepairTime, int maximumRepairTime)
        {
            _conveyor = conveyor ?? throw new ArgumentNullException(nameof(conveyor));
            if (minimumRepairTime < 0 || maximumRepairTime <= minimumRepairTime)
                throw new ArgumentOutOfRangeException(nameof(minimumRepairTime));
            _minimumRepairTime = minimumRepairTime;
            _maximumRepairTime = maximumRepairTime;
        }

        public void RepairConveyor(object conveyor)
        {
            var targetConveyor = conveyor as Conveyor;
            if (targetConveyor == null || Interlocked.Exchange(ref _isRepairing, 1) == 1) return;
            try
            {
                OnMechanicEvent?.Invoke(this,
                    $"Механик начал ремонт в потоке {Thread.CurrentThread.ManagedThreadId}...");
                Thread.Sleep(_random.Next(_minimumRepairTime, _maximumRepairTime));
                targetConveyor.Repair();
                OnMechanicEvent?.Invoke(this, "Механик завершил ремонт!");
                RepairCompleted?.Invoke(this, "Ремонт успешно завершен");
            }
            finally
            {
                Interlocked.Exchange(ref _isRepairing, 0);
            }
        }

        public Task RepairConveyorAsync()
        {
            return Task.Run(() => RepairConveyor(_conveyor));
        }
    }
}
