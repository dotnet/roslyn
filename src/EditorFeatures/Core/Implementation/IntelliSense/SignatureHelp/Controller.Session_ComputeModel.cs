// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
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
            public void ComputeModel(SignatureHelpTrigger trigger)
            {
                AssertIsForeground();

                var caretPosition = Controller.TextView.GetCaretPoint(Controller.SubjectBuffer).Value;
                var disconnectedBufferGraph = new DisconnectedBufferGraph(Controller.SubjectBuffer, Controller.TextView.TextBuffer);

                // If we've already computed a model, then just use that.  Otherwise, actually
                // compute a new model and send that along.
                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    (model, cancellationToken) => ComputeModelInBackgroundAsync(model, caretPosition, disconnectedBufferGraph, trigger, cancellationToken));
            }

            private async Task<Model> ComputeModelInBackgroundAsync(
                Model currentModel,
                SnapshotPoint caretPosition,
                DisconnectedBufferGraph disconnectedBufferGraph,
                SignatureHelpTrigger trigger,
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

                        if (_service == null)
                        {
                            _service = document.GetLanguageService<SignatureHelpService>();
                        }

                        var signatureList = await _service.GetSignaturesAsync(document, caretPosition, trigger, cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (signatureList.Items.Length == 0)
                        {
                            return null;
                        }

                        if (currentModel != null &&
                            currentModel.OriginalList == signatureList &&
                            currentModel.GetCurrentSpanInSubjectBuffer(disconnectedBufferGraph.SubjectBufferSnapshot).Span.Start == signatureList.ApplicableSpan.Start &&
                            currentModel.ArgumentIndex == signatureList.ArgumentIndex &&
                            currentModel.ArgumentCount == signatureList.ArgumentCount &&
                            currentModel.ArgumentName == signatureList.ArgumentName)
                        {
                            // The new model is the same as the current model.  Return the currentModel
                            // so we keep the active selection.
                            return currentModel;
                        }

                        var selectedItem = GetSelectedItem(currentModel, signatureList);
                        var model = new Model(disconnectedBufferGraph, signatureList.ApplicableSpan, signatureList,
                            signatureList.Items, selectedItem, signatureList.ArgumentIndex, signatureList.ArgumentCount, signatureList.ArgumentName,
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

            private static SignatureHelpItem GetSelectedItem(Model currentModel, SignatureList signatureList)
            {
                // Try to find the most appropriate item in the list to select by default.

                // If the provider specified one a selected item, then always stick with that one. 
                if (signatureList.SelectedItemIndex.HasValue)
                {
                    return signatureList.Items[signatureList.SelectedItemIndex.Value];
                }

                // If the provider did not pick a default, and it's the same provider as the previous
                // model we have, then try to return the same item that we had before. 
                if (currentModel != null)
                {
                    return signatureList.Items.FirstOrDefault(i => DisplayPartsMatch(i, currentModel.SelectedItem)) ?? signatureList.Items.First();
                }

                // Otherwise, just pick the first item we have.
                return signatureList.Items.First();
            }

            private static bool DisplayPartsMatch(SignatureHelpItem i1, SignatureHelpItem i2)
            {
                return i1.GetAllParts().SequenceEqual(i2.GetAllParts(), CompareParts);
            }

            private static bool CompareParts(TaggedText p1, TaggedText p2)
            {
                return p1.ToString() == p2.ToString();
            }
        }
    }
}
