// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1012: Abstract classes should not have public constructors
    /// </summary>
    [ExportCodeFixProvider(CA1012DiagnosticAnalyzer.RuleId, LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA1012CodeFixProvider : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CA1012DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.AbstractTypesShouldNotHavePublicConstructorsCodeFix;
        }

        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, string diagnosticId, CancellationToken cancellationToken)
        {
            var classSymbol = (INamedTypeSymbol)model.GetDeclaredSymbol(nodeToFix);
            var instanceConstructors = classSymbol.InstanceConstructors.Where(t => t.DeclaredAccessibility == Accessibility.Public).Select(t => t.DeclaringSyntaxReferences[0].GetSyntax());
            var workspace = document.Project.Solution.Workspace;
            Func<SyntaxNode, SyntaxNode, SyntaxNode> replacementForNodes = (constructorNode, dummy) =>
                {
                    var newSyntax = CodeGenerator.UpdateDeclarationAccessibility(constructorNode, workspace, Accessibility.Protected, cancellationToken: cancellationToken).WithAdditionalAnnotations(Formatter.Annotation);
                    return newSyntax;
                };

            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNodes(instanceConstructors, replacementForNodes)));
        }
    }
}