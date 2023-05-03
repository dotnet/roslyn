// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal interface INavigableItem
    {
        Glyph Glyph { get; }

        /// <summary>
        /// The tagged parts to display for this item. If default, the line of text from <see
        /// cref="Document"/> is used.
        /// </summary>
        ImmutableArray<TaggedText> DisplayTaggedParts { get; }

        /// <summary>
        /// Return true to display the file path of <see cref="Document"/> and the span of <see
        /// cref="SourceSpan"/> when displaying this item.
        /// </summary>
        bool DisplayFileLocation { get; }

        /// <summary>
        /// his is intended for symbols that are ordinary symbols in the language sense, and may be
        /// used by code, but that are simply declared implicitly rather than with explicit language
        /// syntax.  For example, a default synthesized constructor in C# when the class contains no
        /// explicit constructors.
        /// </summary>
        bool IsImplicitlyDeclared { get; }

        NavigableDocument Document { get; }
        TextSpan SourceSpan { get; }

        /// <summary>
        /// True if this search result represents an item that existed in the past, but which may
        /// not exist currently, or which may have moved to a different location.  Consumers should
        /// be resilient to that being the case and not being able to necessarily navigate to the
        /// <see cref="SourceSpan"/> provided.
        /// </summary>
        bool IsStale { get; }

        ImmutableArray<INavigableItem> ChildItems { get; }

        public record NavigableDocument(NavigableProject Project, string Name, string? FilePath, IReadOnlyList<string> Folders, DocumentId Id)
        {
            public required Workspace Workspace { get; init; }

            public static NavigableDocument FromDocument(Document document)
                => new(NavigableProject.FromProject(document.Project), document.Name, document.FilePath, document.Folders, document.Id) { Workspace = document.Project.Solution.Workspace };

            internal async ValueTask<Document> GetDocumentAsync(Solution solution, CancellationToken cancellationToken)
            {
                if (solution.GetDocument(Id) is { } document)
                    return document;

                return await solution.GetRequiredDocumentAsync(Id, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            }

            internal async Task<SourceText> GetTextAsync(Solution solution, CancellationToken cancellationToken)
            {
                var document = await GetDocumentAsync(solution, cancellationToken).ConfigureAwait(false);
                return await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            }

            internal SourceText GetTextSynchronously(Solution solution, CancellationToken cancellationToken)
            {
                var document = solution.GetRequiredDocument(Id);
                return document.GetTextSynchronously(cancellationToken);
            }
        }

        public record struct NavigableProject(string Name, ProjectId Id)
        {
            public static NavigableProject FromProject(Project project)
                => new(project.Name, project.Id);
        }
    }
}
