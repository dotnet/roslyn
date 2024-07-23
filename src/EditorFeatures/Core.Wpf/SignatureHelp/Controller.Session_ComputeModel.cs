// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
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
                this.Computation.ThreadingContext.ThrowIfNotOnUIThread();

                var caretPosition = Controller.TextView.GetCaretPoint(Controller.SubjectBuffer).Value;
                var disconnectedBufferGraph = new DisconnectedBufferGraph(Controller.SubjectBuffer, Controller.TextView.TextBuffer);

                // If we've already computed a model, then just use that.  Otherwise, actually
                // compute a new model and send that along.
                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    (model, cancellationToken) => ComputeModelInBackgroundAsync(
                        model, providers, caretPosition, disconnectedBufferGraph,
                        triggerInfo, cancellationToken));
            }

            private async Task<Model> ComputeModelInBackgroundAsync(
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
                        this.Computation.ThreadingContext.ThrowIfNotOnBackgroundThread();
                        cancellationToken.ThrowIfCancellationRequested();

                        var document = Controller.DocumentProvider.GetDocument(caretPosition.Snapshot, cancellationToken);
                        if (document == null)
                        {
                            return currentModel;
                        }

                        // Let LSP handle signature help in the cloud scenario
                        if (Controller.SubjectBuffer.IsInLspEditorContext())
                        {
                            return null;
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
                        var (provider, items) = await SignatureHelpService.GetSignatureHelpAsync(
                            providers,
                            document,
                            caretPosition,
                            triggerInfo,
                            cancellationToken).ConfigureAwait(false);

                        if (provider == null)
                        {
                            // No provider produced items. So we can't produce a model
                            return null;
                        }

                        if (currentModel != null &&
                            currentModel.Provider == provider &&
                            currentModel.GetCurrentSpanInSubjectBuffer(disconnectedBufferGraph.SubjectBufferSnapshot).Span.Start == items.ApplicableSpan.Start &&
                            currentModel.Items.IndexOf(currentModel.SelectedItem) == items.SelectedItemIndex &&
                            currentModel.SemanticParameterIndex == items.SemanticParameterIndex &&
                            currentModel.SyntacticArgumentCount == items.SyntacticArgumentCount &&
                            currentModel.ArgumentName == items.ArgumentName)
                        {
                            // The new model is the same as the current model.  Return the currentModel
                            // so we keep the active selection.
                            return currentModel;
                        }

                        var selectedItem = GetSelectedItem(currentModel, items, provider, out var userSelected);

                        var model = new Model(disconnectedBufferGraph, items.ApplicableSpan, provider,
                            items.Items, selectedItem, items.SemanticParameterIndex, items.SyntacticArgumentCount, items.ArgumentName,
                            selectedParameter: 0, userSelected);

                        var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
                        var isCaseSensitive = syntaxFactsService == null || syntaxFactsService.IsCaseSensitive;
                        var selection = DefaultSignatureHelpSelector.GetSelection(model.Items,
                            model.SelectedItem, model.UserSelected, model.SemanticParameterIndex, model.SyntacticArgumentCount, model.ArgumentName, isCaseSensitive);

                        return model.WithSelectedItem(selection.SelectedItem, selection.UserSelected)
                                    .WithSelectedParameter(selection.SelectedParameter);
                    }
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            private static SignatureHelpItem GetSelectedItem(Model currentModel, SignatureHelpItems items, ISignatureHelpProvider provider, out bool userSelected)
            {
                // Try to find the most appropriate item in the list to select by default.

                // If it's the same provider as the previous model we have, and we had a user-selection,
                // then try to return the user-selection.
                if (currentModel != null && currentModel.Provider == provider && currentModel.UserSelected)
                {
                    var userSelectedItem = items.Items.FirstOrDefault(i => DisplayPartsMatch(i, currentModel.SelectedItem));
                    if (userSelectedItem != null)
                    {
                        userSelected = true;
                        return userSelectedItem;
                    }
                }

                userSelected = false;

                // If the provider specified a selected item, then pick that one.
                if (items.SelectedItemIndex.HasValue)
                {
                    return items.Items[items.SelectedItemIndex.Value];
                }

                SignatureHelpItem lastSelectionOrDefault = null;
                if (currentModel != null && currentModel.Provider == provider)
                {
                    // If the provider did not pick a default, and it's the same provider as the previous
                    // model we have, then try to return the same item that we had before.
                    lastSelectionOrDefault = items.Items.FirstOrDefault(i => DisplayPartsMatch(i, currentModel.SelectedItem));
                }

                // Otherwise, just pick the first item we have.
                lastSelectionOrDefault ??= items.Items.First();

                return lastSelectionOrDefault;
            }

            private static bool DisplayPartsMatch(SignatureHelpItem i1, SignatureHelpItem i2)
                => i1.GetAllParts().SequenceEqual(i2.GetAllParts(), CompareParts);

            private static bool CompareParts(TaggedText p1, TaggedText p2)
                => p1.ToString() == p2.ToString();
        }
    }
}
