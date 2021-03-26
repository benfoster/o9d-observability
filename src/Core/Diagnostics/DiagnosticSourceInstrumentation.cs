using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace O9d.Observability.Diagnostics
{
    public class DiagnosticSourceInstrumentation : IInstrumentation, IObserver<DiagnosticListener>, IDisposable
    {
        private readonly Func<string, IObserver<KeyValuePair<string, object?>>> _handlerFactory;
        private readonly Func<DiagnosticListener, bool> _diagnosticSourceFilter;
        private readonly Func<string, object?, object?, bool>? _isEnabledFilter;
        private long _disposed;
        private IDisposable? _allSourcesSubscription;
        private readonly List<IDisposable>_listenerSubscriptions = new();

        public DiagnosticSourceInstrumentation(
            Func<string, IObserver<KeyValuePair<string, object?>>> handlerFactory,
            Func<DiagnosticListener, bool> diagnosticSourceFilter,
            Func<string, object?, object?, bool>? isEnabledFilter)
        {
            _handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
            _diagnosticSourceFilter = diagnosticSourceFilter ?? throw new ArgumentNullException(nameof(diagnosticSourceFilter));
            _isEnabledFilter = isEnabledFilter;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _allSourcesSubscription ??= DiagnosticListener.AllListeners.Subscribe(this);
            return Task.CompletedTask;
        }

        public void OnNext(DiagnosticListener value)
        {
            if ((Interlocked.Read(ref _disposed) == 0) && _diagnosticSourceFilter(value))
            {
                var handler = _handlerFactory(value.Name);
                var subscription = _isEnabledFilter is null ?
                    value.Subscribe(handler) :
                    value.Subscribe(handler, _isEnabledFilter);

                lock (_listenerSubscriptions)
                {
                    _listenerSubscriptions.Add(subscription);
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            lock (_listenerSubscriptions)
            {
                foreach (IDisposable listenerSubscription in _listenerSubscriptions)
                {
                    listenerSubscription?.Dispose();
                }

                _listenerSubscriptions.Clear();
            }

            _allSourcesSubscription?.Dispose();
            _allSourcesSubscription = null;
        }
    }
}
