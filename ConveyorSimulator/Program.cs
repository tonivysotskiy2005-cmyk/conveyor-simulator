using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ConveyorSimulator.ViewModels;
using ConveyorSimulator.Views;
using ConveyorSimulator.Models;

namespace ConveyorSimulator
{
    internal static class Program
    {
        private static List<MainViewModel> _viewModels = new List<MainViewModel>();
        private static List<ConveyorControl> _conveyorControls = new List<ConveyorControl>();
        private static List<Timer> _timers = new List<Timer>();
        private static Form _form;
        private static int _conveyorCounter = 0;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _form = new Form();
            _form.Text = "Симулятор конвейера (несколько моделей)";
            _form.Size = new Size(950, 600);
            _form.StartPosition = FormStartPosition.CenterScreen;
            _form.AutoScroll = true; // Включаем прокрутку если много конвейеров
            _form.FormClosed += (sender, args) =>
            {
                foreach (var timer in _timers) timer.Dispose();
                foreach (var viewModel in _viewModels) viewModel.Dispose();
            };

            // Кнопка добавления нового конвейера
            var btnAddConveyor = new Button()
            {
                Text = "➕ Добавить конвейер",
                Location = new Point(20, 10),
                Size = new Size(150, 35),
                BackColor = Color.LightGreen
            };
            btnAddConveyor.Click += (s, e) => AddNewConveyor();

            // Кнопка для демонстрации рефлексии
            var btnReflection = new Button()
            {
                Text = "🔍 Показать информацию (Reflection)",
                Location = new Point(180, 10),
                Size = new Size(200, 35),
                BackColor = Color.LightYellow
            };
            btnReflection.Click += (s, e) => ShowReflectionInfo();

            _form.Controls.Add(btnAddConveyor);
            _form.Controls.Add(btnReflection);

            // Добавляем первый конвейер
            AddNewConveyor();

            Application.Run(_form);
        }

        static void AddNewConveyor()
        {
            _conveyorCounter++;
            int conveyorNumber = _conveyorCounter;
            int currentY = 60 + (_conveyorCounter - 1) * 230;

            var settings = new SimulationSettings(
                conveyorWidth: 800,
                initialMaterials: 60,
                conveyorSpeed: 20,
                partsLimit: 8,
                breakdownProbability: 0.05,
                loadAmount: 20,
                minimumLoadDelay: 1000,
                maximumLoadDelay: 3000,
                minimumRepairTime: 1500,
                maximumRepairTime: 3500);
            var viewModel = new MainViewModel(settings);
            var conveyorControl = new ConveyorControl();
            conveyorControl.SetViewModel(viewModel);
            conveyorControl.Location = new Point(20, currentY);
            conveyorControl.Size = new Size(850, 160);

            // Панель для группировки элементов конвейера
            int btnY = currentY + 170;

            var btnStart = new Button()
            {
                Text = $"▶ Запустить {conveyorNumber}",
                Location = new Point(20, btnY),
                Size = new Size(100, 30),
                BackColor = Color.LightGreen
            };

            var btnStop = new Button()
            {
                Text = $"⏹ Остановить {conveyorNumber}",
                Location = new Point(130, btnY),
                Size = new Size(100, 30),
                BackColor = Color.LightCoral
            };

            var btnLoad = new Button()
            {
                Text = $"📦 Загрузить {conveyorNumber}",
                Location = new Point(240, btnY),
                Size = new Size(100, 30),
                BackColor = Color.LightBlue
            };

            // Поле для изменения лимита для каждого конвейера
            var lblLimit = new Label()
            {
                Text = "Лимит:",
                Location = new Point(360, btnY + 5),
                Size = new Size(40, 20),
                Font = new Font("Arial", 8)
            };

            var txtLimit = new TextBox()
            {
                Text = viewModel.GetPartsLimit().ToString(),
                Location = new Point(400, btnY + 3),
                Size = new Size(40, 20),
                TextAlign = HorizontalAlignment.Center
            };

            var btnSetLimit = new Button()
            {
                Text = "Уст.",
                Location = new Point(445, btnY),
                Size = new Size(40, 25),
                BackColor = Color.LightYellow
            };

            // Метки информации
            var lblStatus = new Label()
            {
                Location = new Point(500, btnY + 3),
                Size = new Size(120, 25),
                Font = new Font("Arial", 9),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblMaterials = new Label()
            {
                Location = new Point(625, btnY + 3),
                Size = new Size(100, 25),
                Font = new Font("Arial", 9),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblParts = new Label()
            {
                Location = new Point(730, btnY + 3),
                Size = new Size(100, 25),
                Font = new Font("Arial", 9),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblBreakdown = new Label()
            {
                Location = new Point(20, btnY + 35),
                Size = new Size(800, 20),
                Font = new Font("Arial", 8),
                ForeColor = Color.DarkRed
            };

            // Обработчики кнопок с замыканием на viewModel
            btnStart.Click += (s, e) => viewModel.StartConveyor();
            btnStop.Click += (s, e) => viewModel.StopConveyor();
            btnLoad.Click += (s, e) => viewModel.ForceLoadMaterials();

            btnSetLimit.Click += (s, e) =>
            {
                if (int.TryParse(txtLimit.Text, out int newLimit) && newLimit > 0 && newLimit <= 30)
                {
                    viewModel.SetPartsLimit(newLimit);
                    txtLimit.Text = viewModel.GetPartsLimit().ToString();
                }
                else
                {
                    MessageBox.Show($"Введите число от 1 до 30 для конвейера {conveyorNumber}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtLimit.Text = viewModel.GetPartsLimit().ToString();
                }
            };

            // Добавляем все элементы на форму
            _form.Controls.Add(conveyorControl);
            _form.Controls.Add(btnStart);
            _form.Controls.Add(btnStop);
            _form.Controls.Add(btnLoad);
            _form.Controls.Add(lblLimit);
            _form.Controls.Add(txtLimit);
            _form.Controls.Add(btnSetLimit);
            _form.Controls.Add(lblStatus);
            _form.Controls.Add(lblMaterials);
            _form.Controls.Add(lblParts);
            _form.Controls.Add(lblBreakdown);

            _viewModels.Add(viewModel);
            _conveyorControls.Add(conveyorControl);

            // Таймер обновления для этого конвейера
            Timer timer = new Timer();
            timer.Interval = 100;
            timer.Tick += (s, e) =>
            {
                // Проверяем, что форма ещё существует
                if (_form.IsDisposed) return;

                lblStatus.Text = $"Статус: {viewModel.StatusText}";
                lblMaterials.Text = $"Материалы: {viewModel.MaterialsCount}";
                lblParts.Text = $"Детали: {viewModel.PartsCount}";
                lblBreakdown.Text = $"🔧 Поломок: {viewModel.BreakdownCount} | Последнее: {Truncate(viewModel.LastEvent, 50)}";

                // Меняем цвет статуса
                if (viewModel.StatusText == "РАБОТАЕТ")
                    lblStatus.BackColor = Color.LightGreen;
                else if (viewModel.StatusText == "СЛОМАН")
                    lblStatus.BackColor = Color.LightCoral;
                else
                    lblStatus.BackColor = Color.LightYellow;
            };
            timer.Start();
            _timers.Add(timer);

            // Обновляем положение формы
            if (currentY + 250 > _form.Height)
            {
                _form.Height = currentY + 300;
            }
        }

        static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        static void ShowReflectionInfo()
        {
            string info = "=== ИНФОРМАЦИЯ О КЛАССЕ Conveyor (Reflection) ===\n\n";

            Type type = typeof(Models.Conveyor);

            info += $"Имя класса: {type.Name}\n";
            info += $"Пространство имён: {type.Namespace}\n";
            info += $"Базовый класс: {type.BaseType?.Name ?? "нет"}\n\n";

            info += "Свойства:\n";
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                info += $"  - {prop.Name} : {prop.PropertyType.Name}\n";
            }

            info += "\nПоля:\n";
            var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic |
                                        System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                info += $"  - {field.Name} : {field.FieldType.Name}\n";
            }

            info += "\nМетоды:\n";
            var methods = type.GetMethods();
            foreach (var method in methods)
            {
                if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                    info += $"  - {method.Name}\n";
            }

            MessageBox.Show(info, "Рефлексия - информация о классе",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
