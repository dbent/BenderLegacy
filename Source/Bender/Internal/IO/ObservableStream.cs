using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bender.Internal.IO
{
    public sealed class ObservableStream : WrapperStreamBase, IObservable<ObservableStreamEvent>
    {
        private readonly List<IObserver<ObservableStreamEvent>> _observers = new List<IObserver<ObservableStreamEvent>>();

        public ObservableStream(Stream stream)
            : base(stream) { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var ret =  base.Read(buffer, offset, count);

            NotifySubscribers(new ObservableStreamEvent(StreamOperation.Read, buffer.Skip(offset).Take(ret).ToList()));

            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);

            NotifySubscribers(new ObservableStreamEvent(StreamOperation.Write, buffer.Skip(offset).Take(count).ToList()));
        }

        public IDisposable Subscribe(IObserver<ObservableStreamEvent> observer)
        {
            _observers.Add(observer);

            return new Unsubscriber(observer, _observers);
        }

        private void NotifySubscribers(ObservableStreamEvent streamEvent)
        {            
            foreach (var observer in _observers)
            {
                observer.OnNext(streamEvent);
            }
        }

        private class Unsubscriber : IDisposable
        {
            private readonly IObserver<ObservableStreamEvent> _observer;
            private readonly List<IObserver<ObservableStreamEvent>> _observers;

            public Unsubscriber(IObserver<ObservableStreamEvent> observer, List<IObserver<ObservableStreamEvent>> observers)
            {
                _observer = observer;
                _observers = observers;
            }

            public void Dispose()
            {
                lock (_observers)
                {
                    _observers.Remove(_observer);
                }
            }
        }
    }
}
