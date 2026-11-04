using System;
using System.Collections.Generic;

namespace PrototypeRadioReceiverSystem.Models
{
    /// <summary>
    /// Модель данных FM-приёмника (слой Model в архитектуре MVC).
    /// Хранит текущую частоту, уровень сигнала и список избранных станций.
    /// </summary>
    public class RadioData
    {
        // Границы и шаг перестройки FM-диапазона (совпадают со скетчем Arduino)
        public const float DefaultFrequency = 101.5f;
        public const float MinFrequency = 87.5f;
        public const float MaxFrequency = 108.0f;
        public const float FrequencyStep = 0.1f;

        private float _frequency = DefaultFrequency;
        private int _signalLevel = 0;
        private bool _isStereo = false;
        private readonly List<float> _favorites = new List<float>();

        /// <summary>Возникает при изменении частоты или уровня сигнала.</summary>
        public event EventHandler Changed;

        /// <summary>Текущая частота, МГц.</summary>
        public float getFrequency()
        {
            return _frequency;
        }

        /// <summary>Текущий уровень сигнала (0..10 в отображении приложения).</summary>
        public int getSignalLevel()
        {
            return _signalLevel;
        }

        /// <summary>Принимается ли стерео-сигнал.</summary>
        public bool getIsStereo()
        {
            return _isStereo;
        }

        /// <summary>Список избранных частот.</summary>
        public List<float> getFavorites()
        {
            return _favorites;
        }

        /// <summary>Устанавливает частоту, удерживая её в границах диапазона.</summary>
        public void setFrequency(float value)
        {
            float clamped = value;
            if (clamped < MinFrequency) clamped = MinFrequency;
            if (clamped > MaxFrequency) clamped = MaxFrequency;

            // Округляем до десятых, чтобы не накапливать ошибку float
            clamped = (float)Math.Round(clamped, 1, MidpointRounding.AwayFromZero);

            if (Math.Abs(clamped - _frequency) < 0.001f) return;

            _frequency = clamped;
            raiseChanged();
        }

        /// <summary>Устанавливает уровень сигнала (0..10).</summary>
        public void setSignalLevel(int value)
        {
            int level = value;
            if (level < 0) level = 0;
            if (level > 10) level = 10;
            if (level == _signalLevel) return;

            _signalLevel = level;
            raiseChanged();
        }

        /// <summary>Устанавливает признак стерео.</summary>
        public void setIsStereo(bool value)
        {
            if (value == _isStereo) return;

            _isStereo = value;
            raiseChanged();
        }

        /// <summary>Шаг вверх на 0.1 МГц.</summary>
        public void increaseFrequency()
        {
            setFrequency(_frequency + FrequencyStep);
        }

        /// <summary>Шаг вниз на 0.1 МГц.</summary>
        public void decreaseFrequency()
        {
            setFrequency(_frequency - FrequencyStep);
        }

        /// <summary>
        /// Добавляет текущую частоту в избранное (команда SAVE).
        /// Возвращает false, если такая станция уже есть в списке.
        /// </summary>
        public bool addToFavorites()
        {
            return addToFavorites(_frequency);
        }

        /// <summary>Добавляет в избранное указанную частоту.</summary>
        public bool addToFavorites(float frequency)
        {
            float rounded = (float)Math.Round(frequency, 1, MidpointRounding.AwayFromZero);
            if (_favorites.Contains(rounded)) return false;

            _favorites.Add(rounded);
            _favorites.Sort();
            return true;
        }

        /// <summary>Удаляет частоту из избранного (окно «Избранное»).</summary>
        public bool removeFromFavorites(float frequency)
        {
            return _favorites.Remove((float)Math.Round(frequency, 1, MidpointRounding.AwayFromZero));
        }

        private void raiseChanged()
        {
            EventHandler handler = Changed;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
