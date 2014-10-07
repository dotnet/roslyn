// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    public partial class Solution
    {
        private abstract partial class CompilationTranslationAction
        {
            internal class TouchDocumentAction : CompilationTranslationAction
            {
                private readonly DocumentState oldState;
                private readonly DocumentState newState;

                public TouchDocumentAction(DocumentState oldState, DocumentState newState)
                {
                    this.oldState = oldState;
                    this.newState = newState;
                }

                public override Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return UpdateDocumentInCompilationAsync(oldCompilation, this.oldState, this.newState, cancellationToken);
                }

                public DocumentId DocumentId { get { return this.newState.Info.Id; } }
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
                [SuppressMessage("Microsoft.StyleCop.CSharp.SpacingRules", "SA1008:OpeningParenthesisMustBeSpacedCorrectly", Justification = "Working around StyleCop bug 7080")]
                private static readonly Func<Compilation, DocumentState, CancellationToken, Task<Compilation>> action =
                    async (o, d, c) => o.RemoveSyntaxTrees(await d.GetSyntaxTreeAsync(c).ConfigureAwait(false));

                public RemoveDocumentAction(DocumentState document)
                    : base(document, action)
                {
                }
            }

            private class AddDocumentAction : SimpleCompilationTranslationAction<DocumentState>
            {
                [SuppressMessage("Microsoft.StyleCop.CSharp.SpacingRules", "SA1008:OpeningParenthesisMustBeSpacedCorrectly", Justification = "Working around StyleCop bug 7080")]
                private static readonly Func<Compilation, DocumentState, CancellationToken, Task<Compilation>> action =
                    async (o, d, c) => o.AddSyntaxTrees(await d.GetSyntaxTreeAsync(c).ConfigureAwait(false));

                public AddDocumentAction(DocumentState document)
                    : base(document, action)
                {
                }
            }

            private class ProjectParseOptionsAction : SimpleCompilationTranslationAction<ProjectState>
            {
                private static readonly Func<Compilation, ProjectState, CancellationToken, Task<Compilation>> action =
                    (o, d, c) => Task.Run(() => ReplaceSyntaxTreesWithTreesFromNewProjectStateAsync(o, d, c), c);

                public ProjectParseOptionsAction(ProjectState state)
                    : base(state, action)
                {
                }
            }

            private class ProjectCompilationOptionsAction : SimpleCompilationTranslationAction<CompilationOptions>
            {
                private static readonly Func<Compilation, CompilationOptions, CancellationToken, Task<Compilation>> action =
                    (o, d, c) => Task.FromResult(o.WithOptions(d));

                public ProjectCompilationOptionsAction(CompilationOptions option)
                    : base(option, action)
                {
                }
            }

            private class ProjectAssemblyNameAction : SimpleCompilationTranslationAction<string>
            {
                private static readonly Func<Compilation, string, CancellationToken, Task<Compilation>> action =
                    (o, d, c) => Task.FromResult(o.WithAssemblyName(d));

                public ProjectAssemblyNameAction(string assemblyName)
                    : base(assemblyName, action)
                {
                }
            }

            private class SimpleCompilationTranslationAction<T> : CompilationTranslationAction
            {
                private readonly T data;
                private readonly Func<Compilation, T, CancellationToken, Task<Compilation>> action;

                public SimpleCompilationTranslationAction(T data, Func<Compilation, T, CancellationToken, Task<Compilation>> action)
                {
                    this.data = data;
                    this.action = action;
                }

                public override Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return action(oldCompilation, this.data, cancellationToken);
                }
            }
        }
    }
}