// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

internal partial class WpfBackgroundWorkIndicatorFactory
{
    private sealed class BackgroundWorkIndicatorContext : IBackgroundWorkIndicatorContext
    {
        private readonly IBackgroundWorkIndicator _indicator;

        public CancellationToken UserCancellationToken => _indicator.CancellationToken;

        public bool AllowCancellation => true;

        public string Description { get; }

        private readonly List<IUIThreadOperationScope> _scopes = new();
        public IEnumerable<IUIThreadOperationScope> Scopes => _scopes;

        public PropertyCollection Properties { get; } = new PropertyCollection();

        public event EventHandler? OnDisposed;

        public BackgroundWorkIndicatorContext(IBackgroundWorkIndicator indicator, string description)
        {
            _indicator = indicator;
            Description = description;
        }

        public IDisposable AddScope(string description)
            => _indicator.AddScope(description);

        public void Dispose()
        {
            _indicator.Dispose();
            OnDisposed?.Invoke(this, null);
        }

        public IUIThreadOperationScope AddScope(bool allowCancellation, string description)
        {
            var backgroundScope = _indicator.AddScope(description);
            var scope = new BackgroundWorkIndicatorScope(backgroundScope, description, this);
            _scopes.Add(scope);
            return scope;
        }

        public void TakeOwnership()
        {
        }

        public IDisposable SuppressAutoCancel()
            => _indicator.SuppressAutoCancel();

        internal void RemoveScope(BackgroundWorkIndicatorScope backgroundWorkIndicatorScope)
            => _scopes.Remove(backgroundWorkIndicatorScope);
    }
}
