// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnusedVariable
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedVariable), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal partial class CSharpRemoveUnusedVariableCodeFixProvider : AbstractRemoveUnusedVariableCodeFixProvider<LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, VariableDeclarationSyntax>
    {
        private const string CS0168 = nameof(CS0168);
        private const string CS0219 = nameof(CS0219);

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0168, CS0219);
    }
}
