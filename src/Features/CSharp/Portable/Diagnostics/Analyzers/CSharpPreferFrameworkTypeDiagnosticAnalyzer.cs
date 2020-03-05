﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PreferFrameworkType;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpPreferFrameworkTypeDiagnosticAnalyzer :
        PreferFrameworkTypeDiagnosticAnalyzerBase<SyntaxKind, ExpressionSyntax, PredefinedTypeSyntax>
    {
        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get; } =
            ImmutableArray.Create(SyntaxKind.PredefinedType);

        ///<remarks>
        /// every predefined type keyword except <c>void</c> can be replaced by its framework type in code.
        ///</remarks>
        protected override bool IsPredefinedTypeReplaceableWithFrameworkType(PredefinedTypeSyntax node)
            => node.Keyword.Kind() != SyntaxKind.VoidKeyword;

        protected override bool IsInMemberAccessOrCrefReferenceContext(ExpressionSyntax node)
            => node.IsDirectChildOfMemberAccessExpression() || node.InsideCrefReference();

        protected override string GetLanguageName()
            => LanguageNames.CSharp;
    }
}
