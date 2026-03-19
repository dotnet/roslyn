// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace;

[Export(typeof(IMoveToNamespaceOptionsService)), Shared]
internal sealed class VisualStudioMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
{
    private const int HistorySize = 3;

    public readonly LinkedList<string> History = new();
    private readonly Func<MoveToNamespaceDialogViewModel, bool?> _showDialog;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioMoveToNamespaceOptionsService()
    {
        _showDialog = viewModel => new MoveToNamespaceDialog(viewModel).ShowModal();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should be marked with 'ImportingConstructorAttribute'", Justification = "Test constructor")]
    internal VisualStudioMoveToNamespaceOptionsService(Func<MoveToNamespaceDialogViewModel, bool?> showDialog)
    {
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
            [.. History.WhereNotNull()]);

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
        History.Remove(namespaceName);
        History.AddFirst(namespaceName);

        if (History.Count > HistorySize)
        {
            History.RemoveLast();
        }
    }
}
