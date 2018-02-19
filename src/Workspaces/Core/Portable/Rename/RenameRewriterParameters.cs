// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    internal class RenameRewriterParameters
    {
        internal readonly CancellationToken CancellationToken;
        internal readonly ISet<TextSpan> ConflictLocationSpans;
        internal readonly OptionSet OptionSet;
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
        internal readonly ISet<TextSpan> StringAndCommentTextSpans;
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
            ISet<TextSpan> stringAndCommentTextSpans,
            ISet<TextSpan> conflictLocationSpans,
            Solution originalSolution,
            ISymbol renameSymbol,
            bool replacementTextValid,
            RenamedSpansTracker renameSpansTracker,
            OptionSet optionSet,
            AnnotationTable<RenameAnnotation> renameAnnotations,
            CancellationToken cancellationToken)
        {
            this.RenamedSymbolDeclarationAnnotation = renamedSymbolDeclarationAnnotation;
            this.Document = document;
            this.SemanticModel = semanticModel;
            this.SyntaxRoot = syntaxRoot;
            this.OriginalSyntaxTree = semanticModel.SyntaxTree;
            this.ReplacementText = replacementText;
            this.OriginalText = originalText;
            this.PossibleNameConflicts = possibleNameConflicts;
            this.RenameLocations = renameLocations;
            this.StringAndCommentTextSpans = stringAndCommentTextSpans;
            this.ConflictLocationSpans = conflictLocationSpans;
            this.OriginalSolution = originalSolution;
            this.RenameSymbol = renameSymbol;
            this.ReplacementTextValid = replacementTextValid;
            this.CancellationToken = cancellationToken;
            this.RenameSpansTracker = renameSpansTracker;
            this.OptionSet = optionSet;
            this.RenameAnnotations = renameAnnotations;
        }
    }
}
