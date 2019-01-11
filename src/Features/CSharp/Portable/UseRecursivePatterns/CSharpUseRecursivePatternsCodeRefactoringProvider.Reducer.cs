// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private sealed class Reducer : Visitor<AnalyzedNode>
        {
            private static readonly Reducer s_instance = new Reducer();

            private Reducer() { }

            public static AnalyzedNode Reduce(AnalyzedNode analyzedNode) => s_instance.Visit(analyzedNode);

            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(node.Expand());

            private AnalyzedNode Visit(PatternMatch node) => new PatternMatch(node.Expression, Visit(node.Pattern));

            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node.Left).Visit(Visit(node.Right));

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => node;

            public override AnalyzedNode VisitTypePattern(TypePattern node) => node;

            public override AnalyzedNode VisitSourcePattern(SourcePattern node) => node.Expand();

            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => node;

            public override AnalyzedNode VisitVarPattern(VarPattern node) => node;
        }
    }
}
