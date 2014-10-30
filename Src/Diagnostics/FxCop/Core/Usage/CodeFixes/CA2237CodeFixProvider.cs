// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    [ExportCodeFixProvider("CA2237 CodeFix provider", LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public sealed class CA2237CodeFixProvider : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(SerializationRulesDiagnosticAnalyzer.RuleCA2237Id);
        }

        protected sealed override string GetCodeFixDescription(Diagnostic diagnostic)
        {
            return FxCopFixersResources.AddSerializableAttribute;
        }

        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var attr = CodeGenerationSymbolFactory.CreateAttributeData(WellKnownTypes.SerializableAttribute(model.Compilation));
            var newNode = CodeGenerator.AddAttributes(nodeToFix, document.Project.Solution.Workspace, SpecializedCollections.SingletonEnumerable(attr)).WithAdditionalAnnotations(Formatting.Formatter.Annotation);
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(nodeToFix, newNode)));
        }
    }
}
