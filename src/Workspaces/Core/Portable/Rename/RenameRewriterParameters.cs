// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename;

internal class RenameRewriterParameters(
    RenameAnnotation renamedSymbolDeclarationAnnotation,
    Document document,
    SemanticModel semanticModel,
    SyntaxNode syntaxRoot,
    string replacementText,
    string originalText,
    ImmutableArray<string> possibleNameConflicts,
    ImmutableDictionary<TextSpan, RenameLocation> renameLocations,
    ImmutableDictionary<TextSpan, ImmutableSortedSet<TextSpan>?> stringAndCommentTextSpans,
    ImmutableHashSet<TextSpan> conflictLocationSpans,
    Solution originalSolution,
    ISymbol renameSymbol,
    bool replacementTextValid,
    RenamedSpansTracker renameSpansTracker,
    bool isRenamingInStrings,
    bool isRenamingInComments,
    AnnotationTable<RenameAnnotation> renameAnnotations,
    CancellationToken cancellationToken)
{
    internal readonly CancellationToken CancellationToken = cancellationToken;
    internal readonly ImmutableHashSet<TextSpan> ConflictLocationSpans = conflictLocationSpans;
    internal readonly bool IsRenamingInStrings = isRenamingInStrings;
    internal readonly bool IsRenamingInComments = isRenamingInComments;
    internal readonly Solution OriginalSolution = originalSolution;
    internal readonly SyntaxTree OriginalSyntaxTree = semanticModel.SyntaxTree;
    internal readonly string OriginalText = originalText;
    internal readonly ImmutableArray<string> PossibleNameConflicts = possibleNameConflicts;
    internal readonly RenameAnnotation RenamedSymbolDeclarationAnnotation = renamedSymbolDeclarationAnnotation;
    internal readonly ImmutableDictionary<TextSpan, RenameLocation> RenameLocations = renameLocations;
    internal readonly RenamedSpansTracker RenameSpansTracker = renameSpansTracker;
    internal readonly ISymbol RenameSymbol = renameSymbol;
    internal readonly string ReplacementText = replacementText;
    internal readonly bool ReplacementTextValid = replacementTextValid;
    internal readonly ImmutableDictionary<TextSpan, ImmutableSortedSet<TextSpan>?> StringAndCommentTextSpans = stringAndCommentTextSpans;
    internal readonly SyntaxNode SyntaxRoot = syntaxRoot;
    internal readonly Document Document = document;
    internal readonly SemanticModel SemanticModel = semanticModel;
    internal readonly AnnotationTable<RenameAnnotation> RenameAnnotations = renameAnnotations;
}
