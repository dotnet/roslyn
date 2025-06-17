// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Analyzers.MatchFolderAndNamespace;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using System;
using System.Collections.Immutable;
#if WORKSPACE
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MatchFolderAndNamespace;

#if WORKSPACE
[Export(typeof(CSharpMatchFolderAndNamespaceDiagnosticAnalyzer)), Shared]
#endif
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpMatchFolderAndNamespaceDiagnosticAnalyzer
    : AbstractMatchFolderAndNamespaceDiagnosticAnalyzer<SyntaxKind, BaseNamespaceDeclarationSyntax>
{
#if WORKSPACE
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpMatchFolderAndNamespaceDiagnosticAnalyzer()
    {
    }
#endif

    protected override ISyntaxFacts GetSyntaxFacts() => CSharpSyntaxFacts.Instance;

    protected override ImmutableArray<SyntaxKind> GetSyntaxKindsToAnalyze()
        => [SyntaxKind.NamespaceDeclaration, SyntaxKind.FileScopedNamespaceDeclaration];
}
