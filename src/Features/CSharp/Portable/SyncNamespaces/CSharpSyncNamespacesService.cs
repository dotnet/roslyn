// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Analyzers.MatchFolderAndNamespace;
using Microsoft.CodeAnalysis.CodeFixes.MatchFolderAndNamespace;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MatchFolderAndNamespace;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.MatchFolderAndNamespace;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SyncNamespaces;

namespace Microsoft.CodeAnalysis.CSharp.SyncNamespaces;

[ExportLanguageService(typeof(ISyncNamespacesService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSyncNamespacesService(
    CSharpMatchFolderAndNamespaceDiagnosticAnalyzer diagnosticAnalyzer,
    CSharpChangeNamespaceToMatchFolderCodeFixProvider codeFixProvider) : AbstractSyncNamespacesService<SyntaxKind, BaseNamespaceDeclarationSyntax>
{
    public override AbstractMatchFolderAndNamespaceDiagnosticAnalyzer<SyntaxKind, BaseNamespaceDeclarationSyntax> DiagnosticAnalyzer { get; } = diagnosticAnalyzer;

    public override AbstractChangeNamespaceToMatchFolderCodeFixProvider CodeFixProvider { get; } = codeFixProvider;
}
