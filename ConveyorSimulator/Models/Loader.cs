using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConveyorSimulator.Models
{
    public sealed class Loader
    {
        private readonly Conveyor _conveyor;
        private readonly int _loadAmount;
        private readonly int _minimumDelay;
        private readonly int _maximumDelay;
        private readonly Random _random = new Random();
        private int _isLoading;

        public event EventHandler<string> OnLoaderEvent;

        public Loader(Conveyor conveyor, int loadAmount, int minimumDelay, int maximumDelay)
        {
            _conveyor = conveyor ?? throw new ArgumentNullException(nameof(conveyor));
            if (loadAmount <= 0) throw new ArgumentOutOfRangeException(nameof(loadAmount));
            if (minimumDelay < 0 || maximumDelay <= minimumDelay)
                throw new ArgumentOutOfRangeException(nameof(minimumDelay));
            _loadAmount = loadAmount;
            _minimumDelay = minimumDelay;
            _maximumDelay = maximumDelay;
        }

        public async Task LoadMaterialsAsync()
        {
            if (Interlocked.Exchange(ref _isLoading, 1) == 1) return;
            try
            {
                OnLoaderEvent?.Invoke(this,
                    $"Погрузчик начал загрузку в потоке {Thread.CurrentThread.ManagedThreadId}...");
                await Task.Delay(_random.Next(_minimumDelay, _maximumDelay)).ConfigureAwait(false);
                _conveyor.LoadMaterials(_loadAmount);
                OnLoaderEvent?.Invoke(this, $"Погрузчик загрузил {_loadAmount} материалов");
            }
            finally
            {
                Interlocked.Exchange(ref _isLoading, 0);
            }
        }

        public void LoadMaterials()
        {
            _conveyor.LoadMaterials(_loadAmount);
            OnLoaderEvent?.Invoke(this, $"Погрузчик загрузил {_loadAmount} материалов");
        }
    }
}
