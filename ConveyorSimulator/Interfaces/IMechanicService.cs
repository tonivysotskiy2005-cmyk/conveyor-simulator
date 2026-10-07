using System;

namespace ConveyorSimulator.Interfaces
{
    public interface IMechanicService
    {
        void RepairConveyor(object conveyor);
        event EventHandler<string> RepairCompleted;
    }
}