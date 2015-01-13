// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1001: Types that own disposable fields should be disposable
    /// </summary>
    [ExportCodeFixProvider(CA1001DiagnosticAnalyzer.RuleId, LanguageNames.CSharp), Shared]
    public class CA1001CSharpCodeFixProvider : CA1001CodeFixProviderBase
    {
        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            //// We are going to implement IDisposable interface:
            ////
            ////        public void Dispose()
            ////        {
            ////            throw new NotImplementedException();
            ////        }

            var syntaxNode = nodeToFix as ClassDeclarationSyntax;
            if (syntaxNode == null)
            {
                return Task.FromResult(document);
            }

            var statement = CreateThrowNotImplementedStatement(model);
            if (statement == null)
            {
                return Task.FromResult(document);
            }

            var member = CreateSimpleMethodDeclaration(CA1001DiagnosticAnalyzer.Dispose, statement);
            var newNode =
                syntaxNode.BaseList != null ?
                    syntaxNode.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(CA1001DiagnosticAnalyzer.IDisposable))).AddMembers(new[] { member }) :
                    syntaxNode.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(CA1001DiagnosticAnalyzer.IDisposable))).AddMembers(new[] { member }).WithIdentifier(syntaxNode.Identifier.WithTrailingTrivia(SyntaxFactory.Space));
            newNode = newNode.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(nodeToFix, newNode)));
        }

        protected StatementSyntax CreateThrowNotImplementedStatement(SemanticModel model)
        {
            var exceptionType = model.Compilation.GetTypeByMetadataName(NotImplementedExceptionName);
            if (exceptionType == null)
            {
                // If we can't find the exception, we can't generate anything.
                return null;
            }

            return SyntaxFactory.ThrowStatement(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.Token(SyntaxKind.NewKeyword),
                        SyntaxFactory.IdentifierName(exceptionType.Name),
                        SyntaxFactory.ArgumentList(),
                        null));
        }

        protected MethodDeclarationSyntax CreateSimpleMethodDeclaration(string name, StatementSyntax statement)
        {
            return SyntaxFactory.MethodDeclaration(
                    new SyntaxList<AttributeListSyntax>(),
                    SyntaxFactory.TokenList(new SyntaxToken[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }),
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    null,
                    SyntaxFactory.Identifier(name),
                    null,
                    SyntaxFactory.ParameterList(),
                    new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                    SyntaxFactory.Block(statement),
                    new SyntaxToken());
        }
    }
}