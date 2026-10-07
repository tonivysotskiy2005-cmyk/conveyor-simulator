using System;

namespace ConveyorSimulator.Models
{
    public sealed class SimulationSettings
    {
        public int ConveyorWidth { get; }
        public int InitialMaterials { get; }
        public int ConveyorSpeed { get; }
        public int PartsLimit { get; }
        public double BreakdownProbability { get; }
        public int LoadAmount { get; }
        public int MinimumLoadDelay { get; }
        public int MaximumLoadDelay { get; }
        public int MinimumRepairTime { get; }
        public int MaximumRepairTime { get; }

        public SimulationSettings(
            int conveyorWidth,
            int initialMaterials,
            int conveyorSpeed,
            int partsLimit,
            double breakdownProbability,
            int loadAmount,
            int minimumLoadDelay,
            int maximumLoadDelay,
            int minimumRepairTime,
            int maximumRepairTime)
        {
            ConveyorWidth = conveyorWidth;
            InitialMaterials = initialMaterials;
            ConveyorSpeed = conveyorSpeed;
            PartsLimit = partsLimit;
            BreakdownProbability = breakdownProbability;
            LoadAmount = loadAmount;
            MinimumLoadDelay = minimumLoadDelay;
            MaximumLoadDelay = maximumLoadDelay;
            MinimumRepairTime = minimumRepairTime;
            MaximumRepairTime = maximumRepairTime;
        }
    }
}
