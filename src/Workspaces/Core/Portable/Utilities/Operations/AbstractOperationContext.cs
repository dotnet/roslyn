// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Utilities
{
    /// <summary>
    /// Abstract base implementation of the <see cref="IOperationContext"/> interface.
    /// </summary>
    internal abstract class AbstractOperationContext : IOperationContext
    {
        private ImmutableList<OperationScope> _scopes;
        private readonly string _defaultDescription;
        private int _completedItems;
        private int _totalItems;

        protected AbstractOperationContext(string defaultDescription)
        {
            _defaultDescription = defaultDescription ?? throw new ArgumentNullException(nameof(defaultDescription));
            _scopes = ImmutableList<OperationScope>.Empty;
        }

        /// <summary>
        /// Gets user readable operation description, composed of initial context description or
        /// descriptions of all currently added scopes.
        /// </summary>
        public string Description
        {
            get
            {
                if (_scopes == null || _scopes.Count == 0)
                    return _defaultDescription;

                var scopes = _scopes;

                // Most common case
                if (scopes.Count == 1)
                    return scopes[0].Description;

                // Combine descriptions of all current scopes
                return string.Join(Environment.NewLine, scopes.Select(s => s.Description));
            }
        }

        protected int CompletedItems => _completedItems;
        protected int TotalItems => _totalItems;

        public IEnumerable<IOperationScope> Scopes => _scopes;

        /// <summary>
        /// Adds an UI thread operation scope with its own cancellability, description and progress tracker.
        /// The scope is removed from the context on dispose.
        /// </summary>
        public IOperationScope AddScope(string description)
        {
            var scope = new OperationScope(description, this);

            while (true)
            {
                var oldScopes = _scopes;
                var newScopes = oldScopes == null ? ImmutableList.Create<OperationScope>(scope) : oldScopes.Add(scope);

                var currentScopes = Interlocked.CompareExchange(ref _scopes, newScopes, oldScopes);
                if (currentScopes == oldScopes)
                {
                    // No other thread preempted us, new scopes set successfully
                    break;
                }
            }

            this.OnScopesChanged();
            return scope;
        }

        protected void OnScopeProgressChanged(IOperationScope _)
        {
            var completed = 0;
            var total = 0;

            var scopes = _scopes;
            if (scopes == null)
                return;

            foreach (var scope in scopes)
            {
                completed += scope.CompletedItems;
                total += scope.TotalItems;
            }

            Interlocked.Exchange(ref _completedItems, completed);
            Interlocked.Exchange(ref _totalItems, total);
        }

        /// <summary>
        /// Invoked when new <see cref="IOperationScope"/>s are added or disposed.
        /// </summary>
        protected virtual void OnScopesChanged() { }

        protected virtual void OnScopeChanged(IOperationScope uiThreadOperationScope)
        {
        }

        void IDisposable.Dispose()
        {
        }

        protected void OnScopeDisposed(OperationScope scope)
        {
            if (scope == null)
                return;

            if (_scopes == null)
                return;

            while (true)
            {
                var oldScopes = _scopes;
                var newScopes = oldScopes.Remove(scope);

                var currentScopes = Interlocked.CompareExchange(ref _scopes, newScopes, oldScopes);
                if (currentScopes == oldScopes)
                {
                    // No other thread preempted us, new scopes set successfully
                    break;
                }
            }

            OnScopesChanged();
        }

        protected class OperationScope : IOperationScope, IProgress<ProgressInfo>
        {
            private readonly AbstractOperationContext _context;

            private string _description = "";
            private int _completedItems;
            private int _totalItems;

            public OperationScope(string description, AbstractOperationContext context)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                this.Description = description ?? "";
            }

            public string Description
            {
                get { return _description; }
                set
                {
                    if (!string.Equals(_description, value, StringComparison.Ordinal))
                    {
                        _description = value;
                        _context.OnScopeChanged(this);
                    }
                }
            }

            public IProgress<ProgressInfo> Progress => this;

            public int CompletedItems => _completedItems;

            public int TotalItems => _totalItems;

            public void Dispose()
                => _context.OnScopeDisposed(this);

            void IProgress<ProgressInfo>.Report(ProgressInfo progressInfo)
            {
                Interlocked.Exchange(ref _completedItems, progressInfo.CompletedItems);
                Interlocked.Exchange(ref _totalItems, progressInfo.TotalItems);
                _context.OnScopeProgressChanged(this);
            }
        }
    }
}
