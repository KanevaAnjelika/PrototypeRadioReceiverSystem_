using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PrototypeRadioReceiverSystem.Models
{
    /// <summary>
    /// Модель журнала операций (слой Model в архитектуре MVC).
    /// Хранит список записей и умеет сохранять их в файл.
    /// </summary>
    public class StorageOperationsLog
    {
        private readonly List<string> _records = new List<string>();

        /// <summary>Файл журнала по умолчанию (рядом с исполняемым файлом).</summary>
        public static string DefaultFilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "operations_log.txt"); }
        }

        /// <summary>Все записи журнала в порядке добавления.</summary>
        public List<string> getRecords()
        {
            return _records;
        }

        /// <summary>Количество записей.</summary>
        public int getCount()
        {
            return _records.Count;
        }

        /// <summary>Добавляет запись с текущей датой и временем.</summary>
        public void addRecord(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            string stamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            _records.Add(stamp + " - " + message);
        }

        /// <summary>Добавляет запись с параметрами (аналог format).</summary>
        public void addRecord(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format)) return;

            addRecord(string.Format(format, args));
        }

        /// <summary>Весь журнал одной строкой (для вывода в таблицу или файл).</summary>
        public string getLog()
        {
            return string.Join(Environment.NewLine, _records.ToArray());
        }

        /// <summary>Сохраняет журнал в файл по умолчанию.</summary>
        public bool saveToFile()
        {
            return saveToFile(DefaultFilePath);
        }

        /// <summary>
        /// Сохраняет журнал в указанный файл.
        /// Возвращает false, если запись в файл не удалась.
        /// </summary>
        public bool saveToFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                File.WriteAllText(path, getLog(), Encoding.UTF8);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>Очищает журнал (кнопка «Очистить»).</summary>
        public void clearLog()
        {
            _records.Clear();
        }
    }
}
