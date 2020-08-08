// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ClassView
{
    internal abstract class AbstractSyncClassViewCommandHandler : ForegroundThreadAffinitizedObject,
        ICommandHandler<SyncClassViewCommandArgs>
    {
        private const string ClassView = "Class View";

        private readonly IServiceProvider _serviceProvider;

        public string DisplayName => ServicesVSResources.Sync_Class_View;

        protected AbstractSyncClassViewCommandHandler(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            Contract.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public bool ExecuteCommand(SyncClassViewCommandArgs args, CommandExecutionContext context)
        {
            this.AssertIsForeground();

            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;
            if (caretPosition < 0)
            {
                return false;
            }

            var snapshot = args.SubjectBuffer.CurrentSnapshot;

            using var waitScope = context.OperationContext.AddScope(allowCancellation: true, string.Format(ServicesVSResources.Synchronizing_with_0, ClassView));
            var document = snapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(
                context.OperationContext).WaitAndGetResult(context.OperationContext.UserCancellationToken);
            if (document == null)
            {
                return true;
            }

            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                return true;
            }

            var libraryService = document.GetLanguageService<ILibraryService>();
            if (libraryService == null)
            {
                return true;
            }

            var userCancellationToken = context.OperationContext.UserCancellationToken;
            var semanticModel = document
                .GetSemanticModelAsync(userCancellationToken)
                .WaitAndGetResult(userCancellationToken);

            var root = semanticModel.SyntaxTree
                .GetRootAsync(userCancellationToken)
                .WaitAndGetResult(userCancellationToken);

            var memberDeclaration = syntaxFactsService.GetContainingMemberDeclaration(root, caretPosition);

            var symbol = memberDeclaration != null
                ? semanticModel.GetDeclaredSymbol(memberDeclaration, userCancellationToken)
                : null;

            while (symbol != null && !IsValidSymbolToSynchronize(symbol))
            {
                symbol = symbol.ContainingSymbol;
            }

            IVsNavInfo navInfo = null;
            if (symbol != null)
            {
                navInfo = libraryService.NavInfoFactory.CreateForSymbol(symbol, document.Project, semanticModel.Compilation, useExpandedHierarchy: true);
            }

            if (navInfo == null)
            {
                navInfo = libraryService.NavInfoFactory.CreateForProject(document.Project);
            }

            if (navInfo == null)
            {
                return true;
            }

            var navigationTool = _serviceProvider.GetService<SVsClassView, IVsNavigationTool>();
            navigationTool.NavigateToNavInfo(navInfo);
            return true;
        }

        private static bool IsValidSymbolToSynchronize(ISymbol symbol) =>
            symbol.Kind == SymbolKind.Event ||
            symbol.Kind == SymbolKind.Field ||
            symbol.Kind == SymbolKind.Method ||
            symbol.Kind == SymbolKind.NamedType ||
            symbol.Kind == SymbolKind.Property;

        public CommandState GetCommandState(SyncClassViewCommandArgs args)
            => Commanding.CommandState.Available;
    }
}
