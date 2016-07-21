// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.FindReferences, ContentTypeNames.RoslynContentType)]
    internal class FindReferencesCommandHandler : ICommandHandler<FindReferencesCommandArgs>
    {
        private readonly IEnumerable<IReferencedSymbolsPresenter> _synchronousPresenters;
        private readonly IEnumerable<IStreamingFindReferencesPresenter> _streamingPresenters;

        private readonly IWaitIndicator _waitIndicator;
        private readonly IAsynchronousOperationListener _asyncListener;

        [ImportingConstructor]
        internal FindReferencesCommandHandler(
            IWaitIndicator waitIndicator,
            [ImportMany] IEnumerable<IReferencedSymbolsPresenter> synchronousPresenters,
            [ImportMany] IEnumerable<IStreamingFindReferencesPresenter> streamingPresenters,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            Contract.ThrowIfNull(waitIndicator);
            Contract.ThrowIfNull(synchronousPresenters);
            Contract.ThrowIfNull(streamingPresenters);

            _waitIndicator = waitIndicator;
            _synchronousPresenters = synchronousPresenters;
            _streamingPresenters = streamingPresenters;
            _asyncListener = new AggregateAsynchronousOperationListener(
                asyncListeners, FeatureAttribute.FindReferences);
        }

        public CommandState GetCommandState(FindReferencesCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(FindReferencesCommandArgs args, Action nextHandler)
        {
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;

            if (caretPosition >= 0)
            {
                var snapshot = args.SubjectBuffer.CurrentSnapshot;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

                var streamingService = document?.Project.LanguageServices.GetService<IStreamingFindReferencesService>();
                var synchronousService = document?.Project.LanguageServices.GetService<IFindReferencesService>();

                var asyncPresenter = _streamingPresenters.FirstOrDefault();

                if (streamingService != null && asyncPresenter != null)
                {
                    StreamingFindReferences(document, streamingService, asyncPresenter, caretPosition);
                    return;
                }
                else if (synchronousService != null)
                {
                    FindReferences(document, synchronousService, caretPosition);
                    return;
                }
            }

            nextHandler();
        }

        private async void StreamingFindReferences(
            Document document, IStreamingFindReferencesService service,
            IStreamingFindReferencesPresenter presenter, int caretPosition)
        {
            using (var token = _asyncListener.BeginAsyncOperation(nameof(StreamingFindReferences)))
            {
                var context = presenter.StartSearch();
                await service.FindReferencesAsync(document, caretPosition, presenter.StartSearch()).ConfigureAwait(false);
            }
        }

        internal void FindReferences(
            Document document, IFindReferencesService service, int caretPosition)
        {
            _waitIndicator.Wait(
                title: EditorFeaturesResources.Find_References,
                message: EditorFeaturesResources.Finding_references,
                action: context =>
                {
                    using (Logger.LogBlock(FunctionId.CommandHandler_FindAllReference, context.CancellationToken))
                    {
                        if (!service.TryFindReferences(document, caretPosition, context))
                        {
                            // The service failed, so just present an empty list of references
                            foreach (var presenter in _synchronousPresenters)
                            {
                                presenter.DisplayResult(
                                    document.Project.Solution,
                                    SpecializedCollections.EmptyEnumerable<ReferencedSymbol>());
                                return;
                            }
                        }
                    }
                }, allowCancel: true);
        }
    }
}