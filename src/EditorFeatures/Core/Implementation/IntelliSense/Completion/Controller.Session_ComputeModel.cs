// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session
        {
            public void ComputeModel(
                CompletionService completionService,
                CompletionTrigger trigger,
                ImmutableHashSet<string> roles,
                OptionSet options)
            {
                AssertIsForeground();

                // If we've already computed a model then we can just ignore this request and not
                // generate any tasks.
                if (this.Computation.InitialUnfilteredModel != null)
                {
                    return;
                }

                new ModelComputer(this, completionService, trigger, roles, options).Do();
            }

            private class ModelComputer : ForegroundThreadAffinitizedObject
            {
                private static readonly Func<string, List<CompletionItem>> s_createList = _ => new List<CompletionItem>();

                private readonly Session _session;
                private readonly CompletionService _completionService;
                private readonly OptionSet _options;
                private readonly CompletionTrigger _trigger;
                private readonly SnapshotPoint _subjectBufferCaretPosition;
                private readonly SourceText _text;
                private readonly ImmutableHashSet<string> _roles;

                private Document _documentOpt;
                private readonly bool _useSuggestionMode;
                private readonly DisconnectedBufferGraph _disconnectedBufferGraph;

                public ModelComputer(
                    Session session,
                    CompletionService completionService,
                    CompletionTrigger trigger,
                    ImmutableHashSet<string> roles,
                    OptionSet options)
                    : base(session.ThreadingContext)
                {
                    _session = session;
                    _completionService = completionService;
                    _options = options;
                    _trigger = trigger;
                    _subjectBufferCaretPosition = session.Controller.TextView.GetCaretPoint(session.Controller.SubjectBuffer).Value;
                    _roles = roles;

                    _text = _subjectBufferCaretPosition.Snapshot.AsText();

                    _useSuggestionMode = options.GetOption(Options.EditorCompletionOptions.UseSuggestionMode);

                    _disconnectedBufferGraph = new DisconnectedBufferGraph(session.Controller.SubjectBuffer, session.Controller.TextView.TextBuffer);
                }

                public void Do()
                {
                    AssertIsForeground();
                    _session.Computation.ChainTaskAndNotifyControllerWhenFinished(
                        (model, cancellationToken) => model != null ? Task.FromResult(model) : DoInBackgroundAsync(cancellationToken));
                }

                private async Task<Model> DoInBackgroundAsync(CancellationToken cancellationToken)
                {
                    using (Logger.LogBlock(FunctionId.Completion_ModelComputer_DoInBackground, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (_completionService == null || _options == null)
                        {
                            // both completionService and options can be null if given buffer is not registered to workspace yet.
                            // could happen in razor more frequently
                            Logger.Log(FunctionId.Completion_ModelComputer_DoInBackground,
                                (c, o) => $"service: {c != null}, options: {o != null}", _completionService, _options);

                            return null;
                        }

                        // get partial solution from background thread.
                        _documentOpt = _text.GetDocumentWithFrozenPartialSemantics(cancellationToken);

                        // TODO(cyrusn): We're calling into extensions, we need to make ourselves resilient
                        // to the extension crashing.
                        var completionList = _documentOpt == null
                            ? null
                            : await _completionService.GetCompletionsAsync(
                                _documentOpt, _subjectBufferCaretPosition, _trigger, _roles, _options, cancellationToken).ConfigureAwait(false);
                        if (completionList == null)
                        {
                            Logger.Log(FunctionId.Completion_ModelComputer_DoInBackground,
                                d => $"No completionList, document: {d != null}, document open: {d?.IsOpen()}", _documentOpt);

                            return null;
                        }

                        var suggestionMode = _useSuggestionMode || completionList.SuggestionModeItem != null;
                        return Model.CreateModel(
                            _documentOpt,
                            _disconnectedBufferGraph,
                            completionList,
                            useSuggestionMode: suggestionMode,
                            trigger: _trigger);
                    }
                }
            }
        }
    }
}
