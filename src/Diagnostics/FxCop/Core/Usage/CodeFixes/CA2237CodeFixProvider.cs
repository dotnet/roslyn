// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "CA2237 CodeFix provider"), Shared]
    public sealed class CA2237CodeFixProvider : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SerializationRulesDiagnosticAnalyzer.RuleCA2237Id); }
        }

        protected sealed override string GetCodeFixDescription(Diagnostic diagnostic)
        {
            return FxCopFixersResources.AddSerializableAttribute;
        }

        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var attr = generator.Attribute(generator.TypeExpression(WellKnownTypes.SerializableAttribute(model.Compilation)));
            var newNode = generator.AddAttributes(nodeToFix, attr);
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(nodeToFix, newNode)));
        }
    }
}
