// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace
{
    [Export(typeof(IMoveToNamespaceOptionsService)), Shared]
    internal class VisualStudioMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
    {
        private readonly string?[] _history;
        private readonly Func<MoveToNamespaceDialogViewModel, bool?> _showDialog;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioMoveToNamespaceOptionsService()
            : this(new string[3], (viewModel) => new MoveToNamespaceDialog(viewModel).ShowModal())
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should be marked with 'ImportingConstructorAttribute'", Justification = "Test constructor")]
        internal VisualStudioMoveToNamespaceOptionsService(string?[] history, Func<MoveToNamespaceDialogViewModel, bool?> showDialog)
        {
            _history = history;
            _showDialog = showDialog;
        }

        public MoveToNamespaceOptionsResult GetChangeNamespaceOptions(
            string defaultNamespace,
            ImmutableArray<string> availableNamespaces,
            ISyntaxFacts syntaxFactsService)
        {
            var viewModel = new MoveToNamespaceDialogViewModel(
                defaultNamespace,
                availableNamespaces,
                syntaxFactsService,
                _history.WhereNotNull().ToImmutableArray());

            var result = _showDialog(viewModel);

            if (result == true)
            {
                OnSelected(viewModel.NamespaceName);
                return new MoveToNamespaceOptionsResult(viewModel.NamespaceName);
            }
            else
            {
                return MoveToNamespaceOptionsResult.Cancelled;
            }
        }

        private void OnSelected(string namespaceName)
        {
            var currentIndex = _history.IndexOf((n) => n == namespaceName);
            if (currentIndex >= 0)
            {
                _history[currentIndex] = null;
            }

            for (var i = _history.Length - 1; i > 0; i--)
            {
                if (_history[i-1] == null)
                {
                    continue;
                }

                _history[i] = _history[i - 1];
            }

            _history[0] = namespaceName;
        }
    }
}
