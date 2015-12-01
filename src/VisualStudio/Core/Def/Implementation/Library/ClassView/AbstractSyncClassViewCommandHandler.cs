// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ClassView
{
    internal abstract class AbstractSyncClassViewCommandHandler : ForegroundThreadAffinitizedObject,
        ICommandHandler<SyncClassViewCommandArgs>
    {
        private const string ClassView = "Class View";

        private readonly IServiceProvider _serviceProvider;
        private readonly IWaitIndicator _waitIndicator;

        protected AbstractSyncClassViewCommandHandler(
            SVsServiceProvider serviceProvider,
            IWaitIndicator waitIndicator)
        {
            Contract.ThrowIfNull(serviceProvider);
            Contract.ThrowIfNull(waitIndicator);

            _serviceProvider = serviceProvider;
            _waitIndicator = waitIndicator;
        }

        public void ExecuteCommand(SyncClassViewCommandArgs args, Action nextHandler)
        {
            this.AssertIsForeground();

            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;
            if (caretPosition < 0)
            {
                nextHandler();
                return;
            }

            var snapshot = args.SubjectBuffer.CurrentSnapshot;

            _waitIndicator.Wait(
                title: string.Format(ServicesVSResources.SynchronizeClassView, ClassView),
                message: string.Format(ServicesVSResources.SynchronizingWithClassView, ClassView),
                allowCancel: true,
                action: context =>
                {
                    var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document == null)
                    {
                        return;
                    }

                    var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                    if (syntaxFactsService == null)
                    {
                        return;
                    }

                    var libraryService = document.Project.LanguageServices.GetService<ILibraryService>();
                    if (libraryService == null)
                    {
                        return;
                    }

                    var semanticModel = document
                        .GetSemanticModelAsync(context.CancellationToken)
                        .WaitAndGetResult(context.CancellationToken);

                    var root = semanticModel.SyntaxTree
                        .GetRootAsync(context.CancellationToken)
                        .WaitAndGetResult(context.CancellationToken);

                    var memberDeclaration = syntaxFactsService.GetContainingMemberDeclaration(root, caretPosition);

                    var symbol = memberDeclaration != null
                        ? semanticModel.GetDeclaredSymbol(memberDeclaration, context.CancellationToken)
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
                        return;
                    }

                    var navigationTool = _serviceProvider.GetService<SVsClassView, IVsNavigationTool>();
                    navigationTool.NavigateToNavInfo(navInfo);
                });
        }

        private static bool IsValidSymbolToSynchronize(ISymbol symbol) =>
            symbol.Kind == SymbolKind.Event ||
            symbol.Kind == SymbolKind.Field ||
            symbol.Kind == SymbolKind.Method ||
            symbol.Kind == SymbolKind.NamedType ||
            symbol.Kind == SymbolKind.Property;

        public CommandState GetCommandState(SyncClassViewCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }
    }
}
