using System;

namespace Bender
{
    public struct Automatic<T>
    {
        private readonly T _value;

        public bool HasValue { get; }

        public T Value
        {
            get
            {
                if (!HasValue)
                    throw new Exception("Value must be determined automatically by caller.");

                return _value;
            }
        }

        public Automatic(T value)
        {
            _value = value;
            HasValue = true;
        }

        public T ValueOr(T automaticValue)
        {
            return HasValue ? _value : automaticValue;
        }

        public static implicit operator Automatic<T>(T value)
        {
            return new Automatic<T>(value);
        }
    }
}
