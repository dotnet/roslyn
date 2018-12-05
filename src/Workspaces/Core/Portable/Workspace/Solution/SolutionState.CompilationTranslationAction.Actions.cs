// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        private abstract partial class CompilationTranslationAction
        {
            internal sealed class TouchDocumentAction : CompilationTranslationAction
            {
                private readonly DocumentState _oldState;
                private readonly DocumentState _newState;

                public TouchDocumentAction(DocumentState oldState, DocumentState newState)
                {
                    _oldState = oldState;
                    _newState = newState;
                }

                public override Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return UpdateDocumentInCompilationAsync(oldCompilation, _oldState, _newState, cancellationToken);
                }

                public DocumentId DocumentId => _newState.Attributes.Id;
            }

            internal sealed class RemoveDocumentAction : SimpleCompilationTranslationAction<DocumentState>
            {
                private static readonly Func<Compilation, DocumentState, CancellationToken, Task<Compilation>> s_action =
                    async (o, d, c) => o.RemoveSyntaxTrees(await d.GetSyntaxTreeAsync(c).ConfigureAwait(false));

                public RemoveDocumentAction(DocumentState document)
                    : base(document, s_action)
                {
                }
            }

            internal sealed class AddDocumentsAction : CompilationTranslationAction
            {
                private readonly ImmutableArray<DocumentState> _documents;

                public AddDocumentsAction(ImmutableArray<DocumentState> documents)
                {
                    _documents = documents;
                }

                public override async Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    var syntaxTrees = new List<SyntaxTree>();
                    foreach (var document in _documents)
                    {
                        syntaxTrees.Add(await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                    }

                    return oldCompilation.AddSyntaxTrees(syntaxTrees);
                }
            }

            internal sealed class ReplaceAllSyntaxTreesAction : CompilationTranslationAction
            {
                private readonly ProjectState _state;

                public ReplaceAllSyntaxTreesAction(ProjectState state)
                {
                    _state = state;
                }

                public override async Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    var syntaxTrees = new List<SyntaxTree>(capacity: _state.DocumentIds.Count);

                    foreach (var documentState in _state.OrderedDocumentStates)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        syntaxTrees.Add(await documentState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                    }

                    return oldCompilation.RemoveAllSyntaxTrees().AddSyntaxTrees(syntaxTrees);
                }
            }

            internal sealed class ProjectCompilationOptionsAction : SimpleCompilationTranslationAction<CompilationOptions>
            {
                private static readonly Func<Compilation, CompilationOptions, CancellationToken, Task<Compilation>> s_action =
                    (o, d, c) => Task.FromResult(o.WithOptions(d));

                public ProjectCompilationOptionsAction(CompilationOptions option)
                    : base(option, s_action)
                {
                }
            }

            internal sealed class ProjectAssemblyNameAction : SimpleCompilationTranslationAction<string>
            {
                private static readonly Func<Compilation, string, CancellationToken, Task<Compilation>> s_action =
                    (o, d, c) => Task.FromResult(o.WithAssemblyName(d));

                public ProjectAssemblyNameAction(string assemblyName)
                    : base(assemblyName, s_action)
                {
                }
            }

            internal class SimpleCompilationTranslationAction<T> : CompilationTranslationAction
            {
                private readonly T _data;
                private readonly Func<Compilation, T, CancellationToken, Task<Compilation>> _action;

                public SimpleCompilationTranslationAction(T data, Func<Compilation, T, CancellationToken, Task<Compilation>> action)
                {
                    _data = data;
                    _action = action;
                }

                public override Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return _action(oldCompilation, _data, cancellationToken);
                }
            }
        }
    }
}
