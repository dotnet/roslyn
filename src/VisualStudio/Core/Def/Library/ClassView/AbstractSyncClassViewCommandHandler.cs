// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ClassView;

internal abstract class AbstractSyncClassViewCommandHandler(
    IThreadingContext threadingContext,
    SVsServiceProvider serviceProvider) : ICommandHandler<SyncClassViewCommandArgs>
{
    private const string ClassView = "Class View";
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public string DisplayName => ServicesVSResources.Sync_Class_View;

    public bool ExecuteCommand(SyncClassViewCommandArgs args, CommandExecutionContext context)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;
        if (caretPosition < 0)
            return false;

        using var waitScope = context.OperationContext.AddScope(allowCancellation: true, string.Format(ServicesVSResources.Synchronizing_with_0, ClassView));
        return _threadingContext.JoinableTaskFactory.Run(() => ExecuteCommandAsync(args, context, caretPosition));
    }

    private async Task<bool> ExecuteCommandAsync(
        SyncClassViewCommandArgs args, CommandExecutionContext context, int caretPosition)
    {
        var snapshot = args.SubjectBuffer.CurrentSnapshot;

        var document = await snapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(
            context.OperationContext).ConfigureAwait(true);
        if (document == null)
            return true;

        var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
        if (syntaxFactsService == null)
            return true;

        var libraryService = document.GetLanguageService<ILibraryService>();
        if (libraryService == null)
            return true;

        var userCancellationToken = context.OperationContext.UserCancellationToken;
        var semanticModel = await document.GetSemanticModelAsync(userCancellationToken).ConfigureAwait(true);

        var root = await semanticModel.SyntaxTree.GetRootAsync(userCancellationToken).ConfigureAwait(true);

        var memberDeclaration = syntaxFactsService.GetContainingMemberDeclaration(root, caretPosition);

        var symbol = memberDeclaration != null
            ? semanticModel.GetDeclaredSymbol(memberDeclaration, userCancellationToken)
            : null;

        while (symbol != null && !IsValidSymbolToSynchronize(symbol))
            symbol = symbol.ContainingSymbol;

        IVsNavInfo navInfo = null;
        if (symbol != null)
            navInfo = libraryService.NavInfoFactory.CreateForSymbol(symbol, document.Project, semanticModel.Compilation, useExpandedHierarchy: true);

        navInfo ??= libraryService.NavInfoFactory.CreateForProject(document.Project);
        if (navInfo == null)
            return true;

        var navigationTool = _serviceProvider.GetServiceOnMainThread<SVsClassView, IVsNavigationTool>();
        navigationTool.NavigateToNavInfo(navInfo);
        return true;
    }

    private static bool IsValidSymbolToSynchronize(ISymbol symbol)
        => symbol.Kind is SymbolKind.Event or
        SymbolKind.Field or
        SymbolKind.Method or
        SymbolKind.NamedType or
        SymbolKind.Property;

    public CommandState GetCommandState(SyncClassViewCommandArgs args)
        => Commanding.CommandState.Available;
}
