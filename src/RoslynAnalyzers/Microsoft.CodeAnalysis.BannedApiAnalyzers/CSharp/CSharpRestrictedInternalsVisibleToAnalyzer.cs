// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
