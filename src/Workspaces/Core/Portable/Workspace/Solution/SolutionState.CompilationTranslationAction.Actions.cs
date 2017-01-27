// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        private abstract partial class CompilationTranslationAction
        {
            internal class TouchDocumentAction : CompilationTranslationAction
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

                public DocumentId DocumentId { get { return _newState.Info.Id; } }
            }

            private class RemoveAllDocumentsAction : CompilationTranslationAction
            {
                public override Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return Task.FromResult(oldCompilation.RemoveAllReferences());
                }
            }

            private class RemoveDocumentAction : SimpleCompilationTranslationAction<DocumentState>
            {
                private static readonly Func<Compilation, DocumentState, CancellationToken, Task<Compilation>> s_action =
                    async (o, d, c) => o.RemoveSyntaxTrees(await d.GetSyntaxTreeAsync(c).ConfigureAwait(false));

                public RemoveDocumentAction(DocumentState document)
                    : base(document, s_action)
                {
                }
            }

            private class AddDocumentAction : SimpleCompilationTranslationAction<DocumentState>
            {
                private static readonly Func<Compilation, DocumentState, CancellationToken, Task<Compilation>> s_action =
                    async (o, d, c) => o.AddSyntaxTrees(await d.GetSyntaxTreeAsync(c).ConfigureAwait(false));

                public AddDocumentAction(DocumentState document)
                    : base(document, s_action)
                {
                }
            }

            private class ProjectParseOptionsAction : SimpleCompilationTranslationAction<ProjectState>
            {
                private static readonly Func<Compilation, ProjectState, CancellationToken, Task<Compilation>> s_action =
                    (o, d, c) => Task.Run(() => ReplaceSyntaxTreesWithTreesFromNewProjectStateAsync(o, d, c), c);

                public ProjectParseOptionsAction(ProjectState state)
                    : base(state, s_action)
                {
                }
            }

            private class ProjectCompilationOptionsAction : SimpleCompilationTranslationAction<CompilationOptions>
            {
                private static readonly Func<Compilation, CompilationOptions, CancellationToken, Task<Compilation>> s_action =
                    (o, d, c) => Task.FromResult(o.WithOptions(d));

                public ProjectCompilationOptionsAction(CompilationOptions option)
                    : base(option, s_action)
                {
                }
            }

            private class ProjectAssemblyNameAction : SimpleCompilationTranslationAction<string>
            {
                private static readonly Func<Compilation, string, CancellationToken, Task<Compilation>> s_action =
                    (o, d, c) => Task.FromResult(o.WithAssemblyName(d));

                public ProjectAssemblyNameAction(string assemblyName)
                    : base(assemblyName, s_action)
                {
                }
            }

            private class SimpleCompilationTranslationAction<T> : CompilationTranslationAction
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
