// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Utilities
{
    /// <summary>
    /// Abstract base implementation of the <see cref="ILongRunningOperationProgress"/> interface.
    /// </summary>
    internal abstract class AbstractLongRunningOperationProgress : ILongRunningOperationProgress
    {
        private ImmutableArray<ILongRunningOperationScope> _scopes;
        private readonly string _defaultDescription;
        private int _completedItems;
        private int _totalItems;

        protected AbstractLongRunningOperationProgress(string defaultDescription)
        {
            _defaultDescription = defaultDescription ?? throw new ArgumentNullException(nameof(defaultDescription));
            _scopes = ImmutableArray<ILongRunningOperationScope>.Empty;
        }

        public abstract CancellationToken CancellationToken { get; }

        /// <summary>
        /// Invoked when new <see cref="ILongRunningOperationScope"/>s are added, disposed or changed.
        /// </summary>
        protected abstract void OnScopeInformationChanged();

        public string Description
        {
            get
            {
                var scopes = _scopes;

                if (scopes.Length == 0)
                    return _defaultDescription;

                // Most common case
                if (scopes.Length == 1)
                    return scopes[0].Description;

                // Combine descriptions of all current scopes
                return string.Join(Environment.NewLine, scopes.Select(s => s.Description).Where(d => !string.IsNullOrWhiteSpace(d)));
            }
        }

        protected int CompletedItems => _completedItems;
        protected int TotalItems => _totalItems;

        public ImmutableArray<ILongRunningOperationScope> Scopes => _scopes;

        /// <summary>
        /// Adds an UI thread operation scope with its own cancellability, description and progress tracker.
        /// The scope is removed from the context on dispose.
        /// </summary>
        public ILongRunningOperationScope AddScope(string description)
        {
            var scope = new LongRunningOperationScope(this, description);

            while (true)
            {
                var oldScopes = _scopes;
                var newScopes = oldScopes.Add(scope);

                var priorValue = ImmutableInterlocked.InterlockedCompareExchange(ref _scopes, newScopes, oldScopes);
                if (priorValue == oldScopes)
                    break;
            }

            this.OnScopeInformationChanged();
            return scope;
        }

        private void OnScopeProgressChanged()
        {
            var completed = 0;
            var total = 0;

            var scopes = _scopes;
            foreach (var scope in scopes)
            {
                var scopeImpl = (LongRunningOperationScope)scope;
                completed += scopeImpl.CompletedItems;
                total += scopeImpl.TotalItems;
            }

            Interlocked.Exchange(ref _completedItems, completed);
            Interlocked.Exchange(ref _totalItems, total);

            this.OnScopeInformationChanged();
        }

        void IDisposable.Dispose()
        {
        }

        private void OnScopeDisposed(LongRunningOperationScope scope)
        {
            while (true)
            {
                var oldScopes = _scopes;
                var newScopes = oldScopes.Remove(scope);

                var priorValue = ImmutableInterlocked.InterlockedCompareExchange(ref _scopes, newScopes, oldScopes);
                if (priorValue == oldScopes)
                    break;
            }

            OnScopeInformationChanged();
        }

        private class LongRunningOperationScope : ILongRunningOperationScope, IProgress<ProgressInfo>
        {
            private readonly AbstractLongRunningOperationProgress _owner;

            private string _description = "";
            private int _completedItems;
            private int _totalItems;

            public LongRunningOperationScope(AbstractLongRunningOperationProgress owner, string description)
            {
                _owner = owner;
                _description = description ?? "";
            }

            public string Description
            {
                get => _description;
                set
                {
                    if (!string.Equals(_description, value, StringComparison.Ordinal))
                    {
                        _description = value;
                        _owner.OnScopeInformationChanged();
                    }
                }
            }

            public IProgress<ProgressInfo> Progress => this;

            public int CompletedItems => _completedItems;

            public int TotalItems => _totalItems;

            public void Dispose()
                => _owner.OnScopeDisposed(this);

            void IProgress<ProgressInfo>.Report(ProgressInfo progressInfo)
            {
                Interlocked.Exchange(ref _completedItems, progressInfo.CompletedItems);
                Interlocked.Exchange(ref _totalItems, progressInfo.TotalItems);
                _owner.OnScopeProgressChanged();
            }
        }
    }
}
