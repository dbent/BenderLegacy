using System;
using System.Linq;
using System.Text;
using Bender.Internal.Exceptions;

namespace Bender.Internal.IO
{
    public sealed class ConsoleStreamObserver : IObserver<ObservableStreamEvent>
    {
        private readonly Encoding _encoding;
        private readonly ConsoleColor _readColor;
        private readonly ConsoleColor _writeColor;

        public ConsoleStreamObserver(Encoding encoding, ConsoleColor readColor, ConsoleColor writeColor)
        {
            _encoding = encoding;
            _readColor = readColor;
            _writeColor = writeColor;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(ObservableStreamEvent value)
        {
            ColorConsole.Temp(GetConsoleColor(value.Operation),
                () => Console.Write(_encoding.GetString(value.Data.ToArray())));
        }

        private ConsoleColor GetConsoleColor(StreamOperation operation)
        {
            switch (operation)
            {
                case StreamOperation.Read:
                    return _readColor;
                case StreamOperation.Write:
                    return _writeColor;
                default:
                    throw new UnhandledEnumException(operation);
            }
        }
    }
}
