using System;

namespace ConveyorSimulator.Models
{
    public class Part
    {
        public int Id { get; private set; }        // Номер детали на этом конвейере
        public int ConveyorId { get; private set; } // ID конвейера, который создал деталь
        public int PositionX { get; set; }
        public DateTime CreatedTime { get; private set; }

        public Part(int id, int conveyorId)
        {
            Id = id;
            ConveyorId = conveyorId;
            PositionX = 0;
            CreatedTime = DateTime.Now;
        }

        internal Part CreateSnapshot()
        {
            return new Part(Id, ConveyorId) { PositionX = PositionX };
        }

        public override string ToString()
        {
            return $"Деталь #{Id} (Конвейер {ConveyorId})";
        }
    }
}
