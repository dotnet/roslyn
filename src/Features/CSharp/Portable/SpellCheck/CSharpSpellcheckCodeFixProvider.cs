// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SpellCheck;

namespace Microsoft.CodeAnalysis.CSharp.SpellCheck
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SpellCheck), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    internal partial class CSharpSpellCheckCodeFixProvider : AbstractSpellCheckCodeFixProvider<SimpleNameSyntax>
    {
        private const string CS0426 = nameof(CS0426); // The type name '0' does not exist in the type '1'
        private const string CS1520 = nameof(CS1520); // Method must have a return type

        [ImportingConstructor]
        public CSharpSpellCheckCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            AddImportDiagnosticIds.FixableDiagnosticIds.Concat(
            GenerateMethodDiagnosticIds.FixableDiagnosticIds).Concat(
                ImmutableArray.Create(CS0426, CS1520));

        protected override bool ShouldSpellCheck(SimpleNameSyntax name)
            => !name.IsVar;

        protected override bool DescendIntoChildren(SyntaxNode arg)
        {
            // Don't dive into type argument lists.  We don't want to report spell checking
            // fixes for type args when we're called on an outer generic type.
            return !(arg is TypeArgumentListSyntax);
        }

        protected override bool IsGeneric(SyntaxToken token)
            => token.GetNextToken().Kind() == SyntaxKind.LessThanToken;

        protected override bool IsGeneric(SimpleNameSyntax nameNode)
            => nameNode is GenericNameSyntax;

        protected override bool IsGeneric(CompletionItem completionItem)
            => completionItem.DisplayTextSuffix == "<>";

        protected override SyntaxToken CreateIdentifier(SyntaxToken nameToken, string newName)
            => SyntaxFactory.Identifier(newName).WithTriviaFrom(nameToken);
    }
}
