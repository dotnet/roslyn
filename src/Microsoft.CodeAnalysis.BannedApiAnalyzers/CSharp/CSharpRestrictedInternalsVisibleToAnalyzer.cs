// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.BannedApiAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpRestrictedInternalsVisibleToAnalyzer : RestrictedInternalsVisibleToAnalyzer<NameSyntax, SyntaxKind>
    {
        protected override ImmutableArray<SyntaxKind> NameSyntaxKinds =>
            ImmutableArray.Create(
                SyntaxKind.IdentifierName,
                SyntaxKind.GenericName,
                SyntaxKind.QualifiedName,
                SyntaxKind.AliasQualifiedName);

        protected override bool IsInTypeOnlyContext(NameSyntax node)
            => SyntaxFacts.IsInTypeOnlyContext(node);
    }
}