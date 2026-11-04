using System;
using System.Globalization;
using PrototypeRadioReceiverSystem.Models;

namespace PrototypeRadioReceiverSystem.Controllers
{
    /// <summary>
    /// Главный контроллер (слой Controller в архитектуре MVC).
    /// Принимает нажатия кнопок из окон (View), изменяет модель (RadioData),
    /// отправляет команды на плату через SecondController и просит представление обновиться.
    /// </summary>
    public class MainController
    {
        // Команды, понятные и приложению, и прошивке
        public const string CommandUp = "UP";
        public const string CommandDown = "DOWN";
        public const string CommandScan = "SCAN";
        public const string CommandSave = "SAVE";

        private readonly RadioData _radioData;
        private readonly StorageOperationsLog _log;
        private readonly SecondController _serial;

        /// <summary>Окно подписывается на это событие и обновляет свои элементы.</summary>
        public event Action<RadioData> UiNeedsUpdate;

        public MainController()
            : this(new RadioData(), new StorageOperationsLog(), new SecondController())
        {
        }

        public MainController(RadioData radioData, StorageOperationsLog log, SecondController serial)
        {
            _radioData = radioData ?? new RadioData();
            _log = log ?? new StorageOperationsLog();
            _serial = serial ?? new SecondController();
        }

        /// <summary>Модель данных приёмника.</summary>
        public RadioData Model
        {
            get { return _radioData; }
        }

        /// <summary>Журнал операций.</summary>
        public StorageOperationsLog Log
        {
            get { return _log; }
        }

        /// <summary>Контроллер COM-порта.</summary>
        public SecondController Serial
        {
            get { return _serial; }
        }

        /// <summary>
        /// Обработка нажатия кнопки: command = "UP", "DOWN", "SCAN", "SAVE".
        /// Регистр не важен, неизвестные команды просто пишутся в журнал.
        /// </summary>
        public void onButtonClick(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            string normalized = command.Trim().ToUpperInvariant();

            switch (normalized)
            {
                case CommandUp:
                    _radioData.increaseFrequency();
                    _log.addRecord("Частота +0.1 МГц -> {0} МГц", formatFrequency(_radioData.getFrequency()));
                    break;

                case CommandDown:
                    _radioData.decreaseFrequency();
                    _log.addRecord("Частота -0.1 МГц -> {0} МГц", formatFrequency(_radioData.getFrequency()));
                    break;

                case CommandScan:
                    _log.addRecord("Запущен автоматический поиск станции");
                    break;

                case CommandSave:
                    bool added = _radioData.addToFavorites();
                    _log.addRecord(added
                        ? "Станция {0} МГц добавлена в избранное"
                        : "Станция {0} МГц уже есть в избранном",
                        formatFrequency(_radioData.getFrequency()));
                    break;

                default:
                    _log.addRecord("Неизвестная команда: {0}", command);
                    return;
            }

            // Отправка команды на плату: без подключённого порта просто вернёт false
            _serial.sendCommand(normalized);

            updateUI();
        }

        /// <summary>
        /// Обновление представления: сообщает окну, что модель изменилась.
        /// </summary>
        public void updateUI()
        {
            Action<RadioData> handler = UiNeedsUpdate;
            if (handler != null) handler(_radioData);
        }

        /// <summary>Частота для журнала и интерфейса: вид с точкой, "101.5".</summary>
        private static string formatFrequency(float frequency)
        {
            return frequency.ToString("0.0", CultureInfo.InvariantCulture);
        }

        /// <summary>Подключение к выбранному COM-порту (используется кнопкой «Подключиться»).</summary>
        public bool connectPort(string portName)
        {
            bool ok = _serial.connect(portName);
            _log.addRecord(ok
                ? "Подключение к {0} выполнено"
                : "Не удалось подключиться к {0}", portName);
            updateUI();
            return ok;
        }

        /// <summary>Отключение от COM-порта.</summary>
        public bool disconnectPort()
        {
            bool wasConnected = _serial.disconnect();
            _log.addRecord("Порт отключён");
            updateUI();
            return wasConnected;
        }

        /// <summary>
        /// Разбор строки состояния от платы: "частота,уровень,стерео" (например, "101.5,8,1").
        /// </summary>
        public void applyStatus(string statusLine)
        {
            if (string.IsNullOrEmpty(statusLine)) return;

            string[] parts = statusLine.Trim().Split(',');
            if (parts.Length < 2) return;

            float frequency;
            if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out frequency))
            {
                return;
            }

            int level;
            if (!int.TryParse(parts[1], out level)) level = 0;

            bool stereo = parts.Length > 2 && parts[2].Trim() == "1";

            _radioData.setFrequency(frequency);
            _radioData.setSignalLevel(level);
            _radioData.setIsStereo(stereo);
            updateUI();
        }
    }
}
