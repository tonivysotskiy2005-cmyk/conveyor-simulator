using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;
using ConveyorSimulator.ViewModels;

namespace ConveyorSimulator.Views
{
    public class ConveyorControl : UserControl
    {
        private MainViewModel _viewModel;
        private Timer _animationTimer;

        public ConveyorControl()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.LightGray;

            _animationTimer = new Timer();
            _animationTimer.Interval = 50;
            _animationTimer.Tick += (s, e) => Invalidate();
            _animationTimer.Start();
        }

        public void SetViewModel(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            if (_viewModel != null)
            {
                _viewModel.UpdateUI += (s, e) => Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_viewModel == null) return;

            Graphics g = e.Graphics;
            int conveyorY = this.Height / 2 - 40;

            // Заголовок с ID конвейера
            using (Font titleFont = new Font("Arial", 10, FontStyle.Bold))
            {
                g.DrawString($"Конвейер #{_viewModel.GetConveyorId()}", titleFont, Brushes.Black, 10, 5);
            }

            // Конвейер
            g.FillRectangle(Brushes.Gray, 10, conveyorY, this.Width - 20, 80);
            g.DrawRectangle(Pens.Black, 10, conveyorY, this.Width - 20, 80);

            // Детали (размер 35x35)
            int partSize = 35;

            var sortedParts = _viewModel.GetParts().OrderBy(p => p.PositionX).ToList();

            foreach (var part in sortedParts)
            {
                int partX = 20 + part.PositionX;
                int partY = conveyorY + 10;

                if (partX < this.Width - partSize && partX > -partSize)
                {
                    using (LinearGradientBrush gradient = new LinearGradientBrush(
                           new Rectangle(partX, partY, partSize, partSize),
                           Color.Gold, Color.DarkOrange, 45))
                    {
                        g.FillRectangle(gradient, partX, partY, partSize, partSize);
                    }
                    g.DrawRectangle(Pens.Black, partX, partY, partSize, partSize);

                    string partNumber = part.Id.ToString();
                    using (Font font = new Font("Arial", 10, FontStyle.Bold))
                    {
                        SizeF textSize = g.MeasureString(partNumber, font);
                        float textX = partX + (partSize - textSize.Width) / 2;
                        float textY = partY + (partSize - textSize.Height) / 2;
                        g.DrawString(partNumber, font, Brushes.Black, textX, textY);
                    }
                }
            }

            // Статус
            string status = _viewModel.StatusText;
            Color statusColor = status == "РАБОТАЕТ" ? Color.Green :
                                (status == "СЛОМАН" ? Color.Red : Color.Orange);

            using (Font font = new Font("Arial", 9, FontStyle.Bold))
            {
                using (var statusBrush = new SolidBrush(statusColor))
                    g.DrawString($"Статус: {status}", font, statusBrush, 10, conveyorY + 85);
                g.DrawString($"Деталей: {_viewModel.PartsCount}", font, Brushes.Black, 150, conveyorY + 85);
                g.DrawString($"Материалы: {_viewModel.MaterialsCount}", font, Brushes.Black, 300, conveyorY + 85);
            }
        }
    }
}
