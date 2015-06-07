using System.Collections.Generic;

namespace Bender.Internal.IO
{
    public sealed class ObservableStreamEvent
    {
        public StreamOperation Operation { get; private set; }
        public IEnumerable<byte> Data { get; private set; }

        public ObservableStreamEvent(StreamOperation operation, IEnumerable<byte> data)
        {
            Operation = operation;
            Data = data;
        }
    }
}
