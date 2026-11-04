using System;
using System.IO.Ports;
using System.Text;

namespace PrototypeRadioReceiverSystem.Controllers
{
    /// <summary>
    /// Второй контроллер (слой Controller в архитектуре MVC):
    /// отвечает за обмен данными с платой Arduino по COM-порту.
    /// Методы безопасны: при отсутствии порта они просто возвращают false/null,
    /// поэтому приложение работает и без подключённого устройства.
    /// </summary>
    public class SecondController
    {
        private const int DefaultBaudRate = 9600;

        private SerialPort _port;
        private readonly StringBuilder _incoming = new StringBuilder();
        private readonly object _sync = new object();

        /// <summary>Признак открытого порта.</summary>
        public bool IsConnected
        {
            get { return _port != null && _port.IsOpen; }
        }

        /// <summary>Имя подключённого порта (например, "COM1") или пустая строка.</summary>
        public string PortName
        {
            get { return IsConnected ? _port.PortName : string.Empty; }
        }

        /// <summary>Приходит целая строка состояния от платы, например "101.5,8,1".</summary>
        public event Action<string> StatusReceived;

        /// <summary>Список COM-портов для заполнения ComboBox в окне.</summary>
        public string[] getAvailablePorts()
        {
            string[] names = SerialPort.GetPortNames();
            Array.Sort(names);
            return names;
        }

        /// <summary>
        /// Открывает порт. Возвращает true, если подключение удалось.
        /// </summary>
        public bool connect(string portName)
        {
            return connect(portName, DefaultBaudRate);
        }

        public bool connect(string portName, int baudRate)
        {
            if (string.IsNullOrEmpty(portName)) return false;

            disconnect();

            try
            {
                SerialPort port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                port.NewLine = "\n";
                port.ReadTimeout = 500;
                port.WriteTimeout = 500;
                port.DataReceived += onDataReceived;
                port.Open();

                _port = port;
                return true;
            }
            catch (Exception)
            {
                // Порт недоступен или занят - остаёмся в режиме «не подключено»
                _port = null;
                return false;
            }
        }

        /// <summary>
        /// Закрывает порт. Возвращает true, если соединение было открыто.
        /// </summary>
        public bool disconnect()
        {
            SerialPort port = _port;
            _port = null;

            if (port == null) return false;

            try
            {
                port.DataReceived -= onDataReceived;
                if (port.IsOpen) port.Close();
                port.Dispose();
            }
            catch (Exception)
            {
                // Порт мог быть снят с шины во время работы - игнорируем
            }

            lock (_sync)
            {
                _incoming.Clear();
            }

            return true;
        }

        /// <summary>
        /// Отправляет команду плате (UP / DOWN / SCAN / SAVE).
        /// Возвращает true, если команда ушла в порт.
        /// </summary>
        public bool sendCommand(string command)
        {
            if (!IsConnected || string.IsNullOrEmpty(command)) return false;

            try
            {
                _port.WriteLine(command);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Читает одну готовую строку из буфера приёма (без блокировки).
        /// Возвращает null, если новых данных нет.
        /// </summary>
        public string receiveData()
        {
            lock (_sync)
            {
                int cut = indexOfNewLine(_incoming);

                // Принимаем и перевод строки, и данные вида "101.5,8,1" без \n
                if (cut < 0)
                {
                    if (_incoming.Length == 0) return null;
                    string rest = _incoming.ToString().Trim();
                    _incoming.Clear();
                    return rest;
                }

                string line = _incoming.ToString(0, cut).Trim();
                _incoming.Remove(0, cut + 1);
                return line;
            }
        }

        /// <summary>Позиция перевода строки в буфере или -1 (в .NET Framework 4.8 у StringBuilder нет IndexOf).</summary>
        private static int indexOfNewLine(StringBuilder buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == '\n') return i;
            }
            return -1;
        }

        private void onDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = sender as SerialPort;
            if (port == null) return;

            try
            {
                string chunk = port.ReadExisting();
                if (string.IsNullOrEmpty(chunk)) return;

                string readyLine = null;
                lock (_sync)
                {
                    _incoming.Append(chunk);

                    int cut = indexOfNewLine(_incoming);
                    if (cut < 0 && _incoming.Length < 64) return;

                    if (cut >= 0)
                    {
                        readyLine = _incoming.ToString(0, cut).Trim();
                        _incoming.Remove(0, cut + 1);
                    }
                    else
                    {
                        // Строка без переводa строки накопилась - забираем целиком
                        readyLine = _incoming.ToString().Trim();
                        _incoming.Clear();
                    }
                }

                if (readyLine != null)
                {
                    Action<string> handler = StatusReceived;
                    if (handler != null) handler(readyLine);
                }
            }
            catch (Exception)
            {
                // Устройство отключили во время чтения
            }
        }
    }
}
