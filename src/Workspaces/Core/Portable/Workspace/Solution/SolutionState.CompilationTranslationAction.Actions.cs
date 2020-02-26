// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            internal sealed class RemoveDocumentsAction : CompilationTranslationAction
            {
                private readonly ImmutableArray<DocumentState> _documents;

                public RemoveDocumentsAction(ImmutableArray<DocumentState> documents)
                {
                    _documents = documents;
                }

                public override async Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    var syntaxTrees = new List<SyntaxTree>(_documents.Length);
                    foreach (var document in _documents)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        syntaxTrees.Add(await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                    }

                    return oldCompilation.RemoveSyntaxTrees(syntaxTrees);
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
                    var syntaxTrees = new List<SyntaxTree>(capacity: _documents.Length);
                    foreach (var document in _documents)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
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

            internal sealed class ProjectCompilationOptionsAction : CompilationTranslationAction
            {
                private readonly CompilationOptions _options;

                public ProjectCompilationOptionsAction(CompilationOptions options)
                {
                    _options = options;
                }

                public override Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return Task.FromResult(oldCompilation.WithOptions(_options));
                }
            }

            internal sealed class ProjectAssemblyNameAction : CompilationTranslationAction
            {
                private readonly string _assemblyName;

                public ProjectAssemblyNameAction(string assemblyName)
                {
                    _assemblyName = assemblyName;
                }

                public override Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return Task.FromResult(oldCompilation.WithAssemblyName(_assemblyName));
                }
            }
        }
    }
}
