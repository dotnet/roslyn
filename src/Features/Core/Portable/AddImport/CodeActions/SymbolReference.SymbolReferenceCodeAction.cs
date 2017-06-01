// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
            /// <summary>
            /// Code action we use when just adding a using, possibly with a project or
            /// metadata reference.  We don't use the standard code action types because
            /// we want to do things like show a glyph if this will do more than just add
            /// an import.
            /// </summary>
            private abstract class SymbolReferenceCodeAction : CodeAction
            {
                public override string Title { get; }
                public override ImmutableArray<string> Tags { get; }
                internal override CodeActionPriority Priority { get; }

                public override string EquivalenceKey => this.Title;

                /// <summary>
                /// The <see cref="Document"/> we started the add-import analysis in.
                /// </summary>
                protected readonly Document ContextDocument;

                /// <summary>
                /// The changes to make to <see cref="ContextDocument"/> to add the import.
                /// </summary>
                private readonly ImmutableArray<TextChange> _textChanges;

                protected SymbolReferenceCodeAction(
                    Document contextDocument,
                    ImmutableArray<TextChange> textChanges,
                    string title, ImmutableArray<string> tags,
                    CodeActionPriority priority)
                {
                    ContextDocument = contextDocument;
                    _textChanges = textChanges;
                    Title = title;
                    Tags = tags;
                    Priority = priority;
                }

                protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
                {
                    var oldText = await ContextDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var newText = oldText.WithChanges(_textChanges);

                    var updatedDocument = ContextDocument.WithText(newText);
                    var updatedProject = UpdateProject(updatedDocument.Project);
                    
                    var updatedSolution = updatedProject.Solution;
                    return updatedSolution;
                }

                protected abstract Project UpdateProject(Project project);
        }
    }
}