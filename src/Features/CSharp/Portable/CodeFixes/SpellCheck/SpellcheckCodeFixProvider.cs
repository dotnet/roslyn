// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.SpellCheck;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddImport;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Spellcheck
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SpellCheck), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    internal partial class SpellCheckCodeFixProvider : AbstractSpellCheckCodeFixProvider<SimpleNameSyntax>
    {
        private const string CS0426 = nameof(CS0426); // The type name '0' does not exist in the type '1'

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = 
            AddImportDiagnosticIds.FixableDiagnosticIds.Concat(
            GenerateMethodDiagnosticIds.FixableDiagnosticIds).Concat(
                ImmutableArray.Create(CS0426));

        protected override bool IsGeneric(SimpleNameSyntax nameNode)
        {
            return nameNode is GenericNameSyntax;
        }

        protected override bool IsGeneric(CompletionItem completionItem)
        {
            return completionItem.DisplayText.Contains("<>");
        }

        protected override SyntaxToken CreateIdentifier(SimpleNameSyntax nameNode, string newName)
        {
            return SyntaxFactory.Identifier(newName);
        }
    }
}