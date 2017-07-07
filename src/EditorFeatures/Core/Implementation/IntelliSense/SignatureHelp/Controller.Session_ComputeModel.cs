﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        internal partial class Session
        {
            public void ComputeModel(
                ImmutableArray<ISignatureHelpProvider> providers,
                SignatureHelpTriggerInfo triggerInfo)
            {
                AssertIsForeground();

                var caretPosition = Controller.TextView.GetCaretPoint(Controller.SubjectBuffer).Value;
                var disconnectedBufferGraph = new DisconnectedBufferGraph(Controller.SubjectBuffer, Controller.TextView.TextBuffer);

                // If this is a retrigger command then update the retrigger-id.  This way
                // any in-flight retrigger-updates will immediately bail out.
                if (IsNonTypeCharRetrigger(triggerInfo))
                {
                    Interlocked.Increment(ref _retriggerId);
                }

                var localId = _retriggerId;

                // If we've already computed a model, then just use that.  Otherwise, actually
                // compute a new model and send that along.
                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    (model, cancellationToken) => ComputeModelInBackgroundAsync(
                        localId, model, providers, caretPosition,
                        disconnectedBufferGraph, triggerInfo, cancellationToken));
            }

            private static bool IsNonTypeCharRetrigger(SignatureHelpTriggerInfo triggerInfo)
                => triggerInfo.TriggerReason == SignatureHelpTriggerReason.RetriggerCommand &&
                   triggerInfo.TriggerCharacter == null;

            private async Task<Model> ComputeModelInBackgroundAsync(
                int localRetriggerId,
                Model currentModel,
                ImmutableArray<ISignatureHelpProvider> providers,
                SnapshotPoint caretPosition,
                DisconnectedBufferGraph disconnectedBufferGraph,
                SignatureHelpTriggerInfo triggerInfo,
                CancellationToken cancellationToken)
            {
                try
                {
                    using (Logger.LogBlock(FunctionId.SignatureHelp_ModelComputation_ComputeModelInBackground, cancellationToken))
                    {
                        AssertIsBackground();
                        cancellationToken.ThrowIfCancellationRequested();

                        var document = await Controller.DocumentProvider.GetDocumentAsync(caretPosition.Snapshot, cancellationToken).ConfigureAwait(false);
                        if (document == null)
                        {
                            return currentModel;
                        }

                        if (triggerInfo.TriggerReason == SignatureHelpTriggerReason.RetriggerCommand)
                        {
                            if (currentModel == null)
                            {
                                return null;
                            }

                            if (triggerInfo.TriggerCharacter.HasValue &&
                                !currentModel.Provider.IsRetriggerCharacter(triggerInfo.TriggerCharacter.Value))
                            {
                                return currentModel;
                            }
                        }

                        // first try to query the providers that can trigger on the specified character
                        var providerAndItemsOpt = await ComputeItemsAsync(
                            localRetriggerId, providers, caretPosition,
                            triggerInfo, document, cancellationToken).ConfigureAwait(false);

                        if (providerAndItemsOpt == null)
                        {
                            // Another retrigger was enqueued while we were inflight.  Just 
                            // stop all work and return the last computed model.  We'll compute
                            // the correct model when we process the other retrigger task.
                            return currentModel;
                        }

                        var (provider, items) = providerAndItemsOpt.Value;
                        if (provider == null)
                        {
                            // No provider produced items. So we can't produce a model
                            return null;
                        }

                        if (currentModel != null &&
                            currentModel.Provider == provider &&
                            currentModel.GetCurrentSpanInSubjectBuffer(disconnectedBufferGraph.SubjectBufferSnapshot).Span.Start == items.ApplicableSpan.Start &&
                            currentModel.ArgumentIndex == items.ArgumentIndex &&
                            currentModel.ArgumentCount == items.ArgumentCount &&
                            currentModel.ArgumentName == items.ArgumentName)
                        {
                            // The new model is the same as the current model.  Return the currentModel
                            // so we keep the active selection.
                            return currentModel;
                        }

                        var selectedItem = GetSelectedItem(currentModel, items, provider);
                        var model = new Model(disconnectedBufferGraph, items.ApplicableSpan, provider,
                            items.Items, selectedItem, items.ArgumentIndex, items.ArgumentCount, items.ArgumentName,
                            selectedParameter: 0);

                        var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
                        var isCaseSensitive = syntaxFactsService == null || syntaxFactsService.IsCaseSensitive;
                        var selection = DefaultSignatureHelpSelector.GetSelection(model.Items,
                            model.SelectedItem, model.ArgumentIndex, model.ArgumentCount, model.ArgumentName, isCaseSensitive);

                        return model.WithSelectedItem(selection.SelectedItem)
                                    .WithSelectedParameter(selection.SelectedParameter);
                    }
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private static bool SequenceEquals(IEnumerable<string> s1, IEnumerable<string> s2)
            {
                if (s1 == s2)
                {
                    return true;
                }

                return s1 != null && s2 != null && s1.SequenceEqual(s2);
            }

            private static SignatureHelpItem GetSelectedItem(Model currentModel, SignatureHelpItems items, ISignatureHelpProvider provider)
            {
                // Try to find the most appropriate item in the list to select by default.

                // If the provider specified one a selected item, then always stick with that one. 
                if (items.SelectedItemIndex.HasValue)
                {
                    return items.Items[items.SelectedItemIndex.Value];
                }

                // If the provider did not pick a default, and it's the same provider as the previous
                // model we have, then try to return the same item that we had before. 
                if (currentModel != null && currentModel.Provider == provider)
                {
                    return items.Items.FirstOrDefault(i => DisplayPartsMatch(i, currentModel.SelectedItem)) ?? items.Items.First();
                }

                // Otherwise, just pick the first item we have.
                return items.Items.First();
            }

            private static bool DisplayPartsMatch(SignatureHelpItem i1, SignatureHelpItem i2)
                => i1.GetAllParts().SequenceEqual(i2.GetAllParts(), CompareParts);

            private static bool CompareParts(TaggedText p1, TaggedText p2)
                => p1.ToString() == p2.ToString();

            /// <summary>
            /// Returns <code>null</code> if our work was preempted and we want to return the 
            /// previous model we've computed.
            /// </summary>
            private async Task<(ISignatureHelpProvider provider, SignatureHelpItems items)?> ComputeItemsAsync(
                int localRetriggerId,
                ImmutableArray<ISignatureHelpProvider> providers,
                SnapshotPoint caretPosition,
                SignatureHelpTriggerInfo triggerInfo,
                Document document,
                CancellationToken cancellationToken)
            {
                try
                {
                    ISignatureHelpProvider bestProvider = null;
                    SignatureHelpItems bestItems = null;

                    // TODO(cyrusn): We're calling into extensions, we need to make ourselves resilient
                    // to the extension crashing.
                    foreach (var provider in providers)
                    {
                        // If this is a retrigger command, and another retrigger command has already
                        // been issued then we can bail out immediately.
                        if (IsNonTypeCharRetrigger(triggerInfo) &&
                            localRetriggerId != _retriggerId)
                        {
                            return null;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        var currentItems = await provider.GetItemsAsync(document, caretPosition, triggerInfo, cancellationToken).ConfigureAwait(false);
                        if (currentItems != null && currentItems.ApplicableSpan.IntersectsWith(caretPosition.Position))
                        {
                            // If another provider provides sig help items, then only take them if they
                            // start after the last batch of items.  i.e. we want the set of items that
                            // conceptually are closer to where the caret position is.  This way if you have:
                            //
                            //  Foo(new Bar($$
                            //
                            // Then invoking sig help will only show the items for "new Bar(" and not also
                            // the items for "Foo(..."
                            if (IsBetter(bestItems, currentItems.ApplicableSpan))
                            {
                                bestItems = currentItems;
                                bestProvider = provider;
                            }
                        }
                    }

                    return (bestProvider, bestItems);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private bool IsBetter(SignatureHelpItems bestItems, TextSpan? currentTextSpan)
            {
                // If we have no best text span, then this span is definitely better.
                if (bestItems == null)
                {
                    return true;
                }

                // Otherwise we want the one that is conceptually the innermost signature.  So it's
                // only better if the distance from it to the caret position is less than the best
                // one so far.
                return currentTextSpan.Value.Start > bestItems.ApplicableSpan.Start;
            }
        }
    }
}
