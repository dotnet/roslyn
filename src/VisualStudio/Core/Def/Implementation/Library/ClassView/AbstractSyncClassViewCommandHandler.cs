// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using Roslyn.Utilities;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ClassView
{
    internal abstract class AbstractSyncClassViewCommandHandler : ForegroundThreadAffinitizedObject,
        VSC.ICommandHandler<SyncClassViewCommandArgs>
    {
        private const string ClassView = "Class View";

        private readonly IServiceProvider _serviceProvider;
        private readonly IWaitIndicator _waitIndicator;

        public bool InterestedInReadOnlyBuffer => false;

        protected AbstractSyncClassViewCommandHandler(
            SVsServiceProvider serviceProvider,
            IWaitIndicator waitIndicator)
        {
            Contract.ThrowIfNull(serviceProvider);
            Contract.ThrowIfNull(waitIndicator);

            _serviceProvider = serviceProvider;
            _waitIndicator = waitIndicator;
        }

        public bool ExecuteCommand(SyncClassViewCommandArgs args)
        {
            this.AssertIsForeground();

            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;
            if (caretPosition < 0)
            {
                return false;
            }

            var snapshot = args.SubjectBuffer.CurrentSnapshot;

            _waitIndicator.Wait(
                title: string.Format(ServicesVSResources.Synchronize_0, ClassView),
                message: string.Format(ServicesVSResources.Synchronizing_with_0, ClassView),
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

            return true;
        }

        private static bool IsValidSymbolToSynchronize(ISymbol symbol) =>
            symbol.Kind == SymbolKind.Event ||
            symbol.Kind == SymbolKind.Field ||
            symbol.Kind == SymbolKind.Method ||
            symbol.Kind == SymbolKind.NamedType ||
            symbol.Kind == SymbolKind.Property;

        public VSC.CommandState GetCommandState(SyncClassViewCommandArgs args)
        {
            return VSC.CommandState.CommandIsUnavailable;
        }
    }
}
