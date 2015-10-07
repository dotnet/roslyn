// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using System;
using System.Linq;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session : Session<Controller, Model, ICompletionPresenterSession>
        {
            #region Fields that can be accessed from either thread

            private readonly CompletionRules _completionRules;

            // When we issue filter tasks, provide them with a (monotonically increasing) id.  That
            // way, when they run we can bail on computation if they've been superceded by another
            // filter task.  
            private int _filterId;

            #endregion

            public Session(Controller controller, ModelComputation<Model> computation, CompletionRules completionRules, ICompletionPresenterSession presenterSession)
                : base(controller, computation, presenterSession)
            {
                _completionRules = completionRules;

                this.PresenterSession.ItemCommitted += OnPresenterSessionItemCommitted;
                this.PresenterSession.ItemSelected += OnPresenterSessionItemSelected;

                // We need to track which (if any) of our completion sets is selected
                this.PresenterSession.CompletionListSelected += OnPresenterCompletionListSelected;
            }

            private void OnPresenterCompletionListSelected(object sender, CompletionListSelectedEventArgs e)
            {
                AssertIsForeground();
                Computation.ChainTaskAndNotifyControllerWhenFinished(models => UpdateModelSelectionStatus(models, e.newValue));
            }

            private ImmutableArray<Model> UpdateModelSelectionStatus(ImmutableArray<Model> models, int? selectedModel)
            {
                var updatedModels = ImmutableArray.CreateBuilder<Model>(models.Length);
                for (int i = 0; i < models.Length; i++)
                {
                    updatedModels.Add(models[i].WithIsSelected(i == selectedModel));
                }

                return updatedModels.ToImmutable();
            }

            private ITextBuffer SubjectBuffer
            {
                get
                {
                    AssertIsForeground();
                    return this.Controller.SubjectBuffer;
                }
            }

            internal void UpdateModelTrackingSpan(SnapshotPoint initialPosition)
            {
                AssertIsForeground();
                var currentPosition = Controller.GetCaretPointInViewBuffer();

                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    models =>
                {
                    if (models == null)
                    {
                        return models;
                    }

                    // If our tracking point maps to the caret before the edit, it should also map to the 
                    // caret after the edit.  This is for the automatic brace completion scenario where
                    // we don't want the completion commit span to include the auto-inserted ')'
                    return models.Select(m => UpdateTrackingSpanEndIfApplicable(m, initialPosition, currentPosition)).ToImmutableArray();
                });
            }

            private Model UpdateTrackingSpanEndIfApplicable(Model model, SnapshotPoint initialPosition, SnapshotPoint currentPosition)
            {
                if (model.CommitTrackingSpanEndPoint.GetPosition(initialPosition.Snapshot) == initialPosition.Position)
                {
                    return model.WithTrackingSpanEnd(currentPosition.Snapshot.Version.CreateTrackingPoint(currentPosition.Position, PointTrackingMode.Positive));
                }

                return model;
            }

            public override void Stop()
            {
                AssertIsForeground();
                this.PresenterSession.ItemSelected -= OnPresenterSessionItemSelected;
                this.PresenterSession.ItemCommitted -= OnPresenterSessionItemCommitted;
                base.Stop();
            }

            private SnapshotPoint GetCaretPointInViewBuffer()
            {
                AssertIsForeground();
                return Controller.GetCaretPointInViewBuffer();
            }

            private void OnPresenterSessionItemCommitted(object sender, CompletionItemEventArgs e)
            {
                AssertIsForeground();
                Contract.ThrowIfFalse(ReferenceEquals(this.PresenterSession, sender));

                this.Controller.CommitItem(e.CompletionItem);
            }

            private void OnPresenterSessionItemSelected(object sender, CompletionItemEventArgs e)
            {
                AssertIsForeground();
                Contract.ThrowIfFalse(ReferenceEquals(this.PresenterSession, sender));

                SetModelSelectedItem(m => e.CompletionItem.IsBuilder ? m.DefaultBuilder : e.CompletionItem);
            }
        }
    }
}
