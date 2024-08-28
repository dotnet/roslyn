// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

[Export(typeof(IUIContextActivationService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioUIContextActivationService() : IUIContextActivationService
{
    public IDisposable ExecuteWhenActivated(Guid uiContext, Action action)
    {
        var context = UIContext.FromUIContextGuid(uiContext);
        if (context.IsActive)
        {
            action();
            return EmptyDisposable.Instance;
        }
        else
        {
            return new WhenActivatedHandler(context, action);
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed class WhenActivatedHandler : IDisposable
    {
        private readonly Action _action;
        private UIContext? _context;

        public WhenActivatedHandler(UIContext context, Action action)
        {
            _context = context;
            _action = action;
            _context.UIContextChanged += OnContextChanged;
        }

        public void Dispose()
        {
            if (_context is not null)
            {
                _context.UIContextChanged -= OnContextChanged;
            }

            _context = null;
        }

        private void OnContextChanged(object sender, UIContextChangedEventArgs e)
        {
            Contract.ThrowIfNull(_context);

            if (e.Activated)
            {
                _action();
                Dispose();
            }
        }
    }
}
