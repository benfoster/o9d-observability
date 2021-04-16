using System;
using System.Collections.Generic;

namespace O9d.Observability.Tests.Diagnostics
{
    internal class DelegateObserver : IObserver<KeyValuePair<string, object?>>
    {
        private readonly Action<KeyValuePair<string, object?>> _onNextHandler;

        public DelegateObserver(Action<KeyValuePair<string, object?>> onNextHandler)
        {
            _onNextHandler = onNextHandler;
        }
        
        public void OnCompleted()
        {
            
        }

        public void OnError(Exception error)
        {
            
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            _onNextHandler.Invoke(value);
        }
    }
}