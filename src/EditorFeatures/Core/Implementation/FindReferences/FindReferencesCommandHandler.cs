// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.FindReferences,
       ContentTypeNames.RoslynContentType)]
    internal class FindReferencesCommandHandler :
        ICommandHandler<FindReferencesCommandArgs>
    {
        private readonly IEnumerable<INavigableItemsPresenter> _presenters;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        internal FindReferencesCommandHandler(
            IWaitIndicator waitIndicator,
            [ImportMany] IEnumerable<INavigableItemsPresenter> presenters)
        {
            Contract.ThrowIfNull(waitIndicator);
            Contract.ThrowIfNull(presenters);

            _waitIndicator = waitIndicator;
            _presenters = presenters;
        }

        internal void FindReferences(ITextSnapshot snapshot, int caretPosition)
        {
            _waitIndicator.Wait(
                title: EditorFeaturesResources.Find_References,
                message: EditorFeaturesResources.Finding_references,
                action: context =>
            {
                Document document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    var service = document.Project.LanguageServices.GetService<IFindReferencesService>();
                    if (service != null)
                    {
                        var cancellationToken = context.CancellationToken;
                        using (Logger.LogBlock(FunctionId.CommandHandler_FindAllReference, cancellationToken))
                        {
                            var items = service.FindReferencesAsync(document, caretPosition, context).WaitAndGetResult(cancellationToken);

                            foreach (var presenter in _presenters)
                            {
                                presenter.DisplayResult(items);
                                return;
                            }
                        }
                    }
                }
            }, allowCancel: true);
        }

        public CommandState GetCommandState(FindReferencesCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(FindReferencesCommandArgs args, Action nextHandler)
        {
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;

            if (caretPosition < 0)
            {
                nextHandler();
                return;
            }

            var snapshot = args.SubjectBuffer.CurrentSnapshot;

            FindReferences(snapshot, caretPosition);
        }
    }
}
