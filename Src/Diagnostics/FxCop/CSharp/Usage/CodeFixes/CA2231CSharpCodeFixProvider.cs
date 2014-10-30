// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Usage;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2231: Overload operator equals on overriding ValueType.Equals
    /// </summary>
    [ExportCodeFixProvider(CA2231DiagnosticAnalyzer.RuleId, LanguageNames.CSharp), Shared]
    public class CA2231CSharpCodeFixProvider : CA2231CodeFixProviderBase
    {
        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            //// We are going to add two operators:
            ////
            ////        public static bool operator ==(A left, A right)
            ////        {
            ////            throw new NotImplementedException();
            ////        }
            ////
            ////        public static bool operator !=(A left, A right)
            ////        {
            ////            throw new NotImplementedException();
            ////        }

            var syntaxNode = nodeToFix as StructDeclarationSyntax;
            if (syntaxNode == null)
            {
                return Task.FromResult(document);
            }

            var statement = CreateThrowNotImplementedStatement(model);
            if (statement == null)
            {
                return Task.FromResult(document);
            }

            var parameters = new[] { CreateParameter(syntaxNode.Identifier.ValueText, LeftName), CreateParameter(syntaxNode.Identifier.ValueText, RightName) };

            var op_equality = CreateOperatorDeclaration(SyntaxKind.EqualsEqualsToken, parameters, statement);
            var op_inequality = CreateOperatorDeclaration(SyntaxKind.ExclamationEqualsToken, parameters, statement);
            var newNode = syntaxNode.AddMembers(new[] { op_equality, op_inequality }).WithAdditionalAnnotations(Formatter.Annotation);

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

        protected ParameterSyntax CreateParameter(string type, string name)
        {
            return SyntaxFactory.Parameter(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(),
                SyntaxFactory.ParseTypeName(type),
                SyntaxFactory.IdentifierName(name).Identifier,
                null);
        }

        protected OperatorDeclarationSyntax CreateOperatorDeclaration(SyntaxKind kind, ParameterSyntax[] parameters, StatementSyntax statement)
        {
            return SyntaxFactory.OperatorDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(new SyntaxToken[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword) }),
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                SyntaxFactory.Token(kind),
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)),
                SyntaxFactory.Block(statement),
                new SyntaxToken());
        }
    }
}