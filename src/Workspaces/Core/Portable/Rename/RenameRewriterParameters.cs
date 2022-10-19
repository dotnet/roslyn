// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    internal class RenameRewriterParameters
    {
        internal readonly CancellationToken CancellationToken;
        internal readonly ISet<TextSpan> ConflictLocationSpans;
        internal readonly bool IsRenamingInStrings;
        internal readonly bool IsRenamingInComments;
        internal readonly Solution OriginalSolution;
        internal readonly SyntaxTree OriginalSyntaxTree;
        internal readonly string OriginalText;
        internal readonly ICollection<string> PossibleNameConflicts;
        internal readonly RenameAnnotation RenamedSymbolDeclarationAnnotation;
        internal readonly Dictionary<TextSpan, RenameLocation> RenameLocations;
        internal readonly RenamedSpansTracker RenameSpansTracker;
        internal readonly ISymbol RenameSymbol;
        internal readonly string ReplacementText;
        internal readonly bool ReplacementTextValid;
        internal readonly ImmutableDictionary<TextSpan, ImmutableSortedSet<TextSpan>?> StringAndCommentTextSpans;
        internal readonly SyntaxNode SyntaxRoot;
        internal readonly Document Document;
        internal readonly SemanticModel SemanticModel;
        internal readonly AnnotationTable<RenameAnnotation> RenameAnnotations;

        public RenameRewriterParameters(
            RenameAnnotation renamedSymbolDeclarationAnnotation,
            Document document,
            SemanticModel semanticModel,
            SyntaxNode syntaxRoot,
            string replacementText,
            string originalText,
            ICollection<string> possibleNameConflicts,
            Dictionary<TextSpan, RenameLocation> renameLocations,
            ImmutableDictionary<TextSpan, ImmutableSortedSet<TextSpan>?> stringAndCommentTextSpans,
            ISet<TextSpan> conflictLocationSpans,
            Solution originalSolution,
            ISymbol renameSymbol,
            bool replacementTextValid,
            RenamedSpansTracker renameSpansTracker,
            bool isRenamingInStrings,
            bool isRenamingInComments,
            AnnotationTable<RenameAnnotation> renameAnnotations,
            CancellationToken cancellationToken)
        {
            RenamedSymbolDeclarationAnnotation = renamedSymbolDeclarationAnnotation;
            Document = document;
            SemanticModel = semanticModel;
            SyntaxRoot = syntaxRoot;
            OriginalSyntaxTree = semanticModel.SyntaxTree;
            ReplacementText = replacementText;
            OriginalText = originalText;
            PossibleNameConflicts = possibleNameConflicts;
            RenameLocations = renameLocations;
            StringAndCommentTextSpans = stringAndCommentTextSpans;
            ConflictLocationSpans = conflictLocationSpans;
            OriginalSolution = originalSolution;
            RenameSymbol = renameSymbol;
            ReplacementTextValid = replacementTextValid;
            CancellationToken = cancellationToken;
            RenameSpansTracker = renameSpansTracker;
            IsRenamingInStrings = isRenamingInStrings;
            IsRenamingInComments = isRenamingInComments;
            RenameAnnotations = renameAnnotations;
        }
    }
}
