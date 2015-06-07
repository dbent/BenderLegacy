using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bender.Bend.Utility
{
    public sealed class MultiObserver<T> : IObserver<T>
    {
        private readonly ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();
        private readonly HashSet<IObserver<T>> _observers = new HashSet<IObserver<T>>();

        public IDisposable Add(IObserver<T> observer)
        {
            _rw.EnterWriteLock();
            try
            {
                _observers.Add(observer);
            }
            finally
            {
                _rw.ExitWriteLock();
            }

            return new Unsubscriber(() =>
                {
                    _rw.EnterWriteLock();
                    try
                    {
                        _observers.Remove(observer);
                    }
                    finally
                    {
                        _rw.EnterWriteLock();
                    }
                });
        }

        public void OnCompleted()
        {
            ForEachObserver(i => i.OnCompleted());
        }

        public void OnError(Exception error)
        {
            ForEachObserver(i => i.OnError(error));
        }

        public void OnNext(T value)
        {
            ForEachObserver(i => i.OnNext(value));
        }

        private void ForEachObserver(Action<IObserver<T>> action)
        {
            var exceptions = new List<Exception>();
            
            _rw.EnterReadLock();
            try
            {
                foreach (var o in _observers)
                {
                    try
                    {
                        action(o);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }
            }
            finally
            {
                _rw.ExitReadLock();
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        private class Unsubscriber : IDisposable
        {
            private readonly Action _unsunscribe;

            public Unsubscriber(Action unsubcribe)
            {
                _unsunscribe = unsubcribe;
            }

            public void Dispose()
            {
                _unsunscribe();
            }
        }        
    }
}
