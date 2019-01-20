// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private abstract class Visitor<T>
        {
            public T Visit(AnalyzedNode node) => node != null ? node.Accept(this) : default;

            public abstract T VisitPatternMatch(PatternMatch node);
            public abstract T VisitConjuction(Conjuction node);
            public abstract T VisitConstantPattern(ConstantPattern node);
            public abstract T VisitTypePattern(TypePattern node);
            public abstract T VisitNotNullPattern(NotNullPattern node);
            public abstract T VisitVarPattern(VarPattern node);
        }
    }
}
