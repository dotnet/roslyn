// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RegularExpressions;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class RegexPatternTokenClassifier : AbstractSyntaxClassifier
    {
        private static readonly ConditionalWeakTable<SemanticModel, RegexPatternDetector> _modelToDetector =
            new ConditionalWeakTable<SemanticModel, RegexPatternDetector>();

        public override ImmutableArray<int> SyntaxTokenKinds { get; } = ImmutableArray.Create<int>((int)SyntaxKind.StringLiteralToken);

        public override void AddClassifications(SyntaxToken token, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            Debug.Assert(token.Kind() == SyntaxKind.StringLiteralToken);

            // Do some quick syntactic checks before doing any complex work.
            if (RegexPatternDetector.IsDefinitelyNotPattern(token, CSharpSyntaxFactsService.Instance))
            {
                return;
            }

            // Looks like it could be a regex pattern.  Do more complex check.
            // Cache the detector we create, so we don't have to continually do
            // the same semantic work for every string literal token we visit.
            var detector = _modelToDetector.GetValue(
                semanticModel, m => RegexPatternDetector.TryCreate(
                    m, CSharpSyntaxFactsService.Instance, CSharpSemanticFactsService.Instance));

            if (!detector.IsRegexPattern(token, cancellationToken, out var options))
            {
                return;
            }

            var virtualCharService = CSharpVirtualCharService.Instance;
            var chars = virtualCharService.TryConvertToVirtualChars(token);
            if (chars.IsDefaultOrEmpty)
            {
                return;
            }

            var tree = RegexParser.Parse(chars, options);
            AddClassifications(tree, result);
        }

        private void AddClassifications(RegexTree tree, ArrayBuilder<ClassifiedSpan> result)
        {
            throw new NotImplementedException();
        }
    }
}
