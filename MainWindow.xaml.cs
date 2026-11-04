using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PrototypeRadioReceiverSystem.Controllers;
using PrototypeRadioReceiverSystem.Models;

namespace PrototypeRadioReceiverSystem
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// Представление (View) в архитектуре MVC: только отображает состояние
    /// и передаёт нажатия кнопок в MainController.
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isDarkTheme = false;
        private bool _isConnected = false;

        // Контроллер MVC: модель и журнал создаются внутри него
        private readonly MainController _controller = new MainController();

        public MainWindow()
        {
            InitializeComponent();
            _controller.UiNeedsUpdate += RefreshFromModel;
        }


        #region Тема

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme(_isDarkTheme);
            ThemeButton.Content = _isDarkTheme ? "☀" : "🌙";
        }

        private void ApplyTheme(bool dark)
        {
            var res = Application.Current.Resources;

            if (dark)
            {
                res["WindowBgBrush"] = MakeBrush(0x7F, 0x4E, 0xA8);
                res["PanelBrush"] = MakeBrush(0xBA, 0x8C, 0xBE);
                res["PrimaryButtonBrush"] = MakeBrush(0xC7, 0xA3, 0xD2);
                res["PrimaryButtonFgBrush"] = Brushes.White;
                res["AccentBrush"] = MakeBrush(0x9C, 0x93, 0xE5);
                res["AccentButtonFgBrush"] = Brushes.White;
                res["TextBrush"] = Brushes.White;
                res["TabActiveBrush"] = Brushes.White;
                // ComboBox: чёрный фон, белый текст, обводка #7F4EA8
                res["ComboBoxBgBrush"] = MakeBrush(0x00, 0x00, 0x00);
                res["ComboBoxBorderBrush"] = MakeBrush(0x7F, 0x4E, 0xA8);
                res["ComboBoxFgBrush"] = Brushes.White;
                // Обводка кнопок
                res["ButtonBorderBrush"] = MakeBrush(0x9C, 0x93, 0xE5);
                // Карточки
                res["CardBgBrush"] = MakeBrush(0xC7, 0xA3, 0xD2);
                res["CardBorderBrush"] = MakeBrush(0x9C, 0x93, 0xE5);
                res["CardHeaderBrush"] = MakeBrush(0x9C, 0x93, 0xE5);
            }
            else
            {
                res["WindowBgBrush"] = MakeBrush(0xF5, 0xED, 0xF0);
                res["PanelBrush"] = MakeBrush(0xDC, 0xBD, 0xDF);
                res["PrimaryButtonBrush"] = MakeBrush(0x9A, 0x99, 0xE1);
                res["PrimaryButtonFgBrush"] = Brushes.White;
                res["AccentBrush"] = MakeBrush(0x00, 0x8B, 0xD0);
                res["AccentButtonFgBrush"] = Brushes.White;
                res["TextBrush"] = MakeBrush(0x3A, 0x2B, 0x45);
                res["TabActiveBrush"] = MakeBrush(0x00, 0x8B, 0xD0);
                // ComboBox: белый фон, текст #333333, обводка #F5EDF0
                res["ComboBoxBgBrush"] = Brushes.White;
                res["ComboBoxBorderBrush"] = MakeBrush(0xF5, 0xED, 0xF0);
                res["ComboBoxFgBrush"] = MakeBrush(0x33, 0x33, 0x33);
                // Обводка кнопок
                res["ButtonBorderBrush"] = Brushes.Black;
                // Карточки
                res["CardBgBrush"] = Brushes.White;
                res["CardBorderBrush"] = MakeBrush(0x99, 0x99, 0x99);
                res["CardHeaderBrush"] = MakeBrush(0x9A, 0x99, 0xE1);
            }
        }

        private static SolidColorBrush MakeBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        #endregion

        #region Подключение (заглушка)

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectToPort();
        }

        private void ConnectToPort()
        {
            // TODO: заменить переключатель вида на реальный вызов
            // _controller.connectPort(PortComboBox.Text) / _controller.disconnectPort()
            _isConnected = !_isConnected;


            if (_isConnected)
            {
                ConnectButton.Content = "Отключиться";
                StatusIndicator.Fill = MakeBrush(0x00, 0xC8, 0x53);
            }
            else
            {
                ConnectButton.Content = "Подключиться";
                StatusIndicator.Fill = MakeBrush(0x9E, 0x9E, 0x9E);
            }
        }

        #endregion

        #region Кнопки управления

        // Обработчики из XAML передают работу методам с именами из документации (btn*_Click),
        // а те - контроллеру. Так View остаётся тонким слоем.
        private void FreqUpButton_Click(object sender, RoutedEventArgs e) { btnUp_Click(sender, e); }
        private void FreqDownButton_Click(object sender, RoutedEventArgs e) { btnDown_Click(sender, e); }
        private void ScanButton_Click(object sender, RoutedEventArgs e) { btnScan_Click(sender, e); }
        private void SaveButton_Click(object sender, RoutedEventArgs e) { btnSave_Click(sender, e); }

        /// <summary>Кнопка «Частота +»: шаг 0.1 МГц вверх.</summary>
        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            _controller.onButtonClick(MainController.CommandUp);
        }

        /// <summary>Кнопка «Частота -»: шаг 0.1 МГц вниз.</summary>
        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            _controller.onButtonClick(MainController.CommandDown);
        }

        /// <summary>Кнопка «Поиск»: запуск автопоиска станции.</summary>
        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            _controller.onButtonClick(MainController.CommandScan);
            // TODO: дождаться строки состояния от платы и показать найденную станцию
        }

        /// <summary>Кнопка «Сохранить»: текущая частота в избранное и в EEPROM платы.</summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            _controller.onButtonClick(MainController.CommandSave);
            // TODO: обновить список в окне «Избранное»
        }

        /// <summary>
        /// Обновление таблицы журнала в главном окне.
        /// Реального журнала в главном окне пока нет - метод-заглушка для соответствия документации.
        /// </summary>
        private void updateLogTable()
        {
            // TODO: вывести _controller.Log.getRecords() в таблицу главного окна
        }

        /// <summary>Обновление элементов окна новым состоянием модели (вызывается контроллером).</summary>
        private void RefreshFromModel(RadioData data)
        {
            if (data == null) return;

            // Частота приводится к invariant-формату, чтобы делитель остался точкой: "101.5 МГц"
            FrequencyTextBlock.Text = data.getFrequency().ToString("0.0", CultureInfo.InvariantCulture) + " МГц";

            // Уровень сигнала отображается из строки состояния платы (см. SecondController.receiveData)
        }

        #endregion

        #region Навигация

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            var favoritesWindow = new FavoritesWindow { Owner = this };
            favoritesWindow.ShowDialog();
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            var logWindow = new LogWindow { Owner = this };
            logWindow.ShowDialog();
        }

        #endregion

        #region Управление окном

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
