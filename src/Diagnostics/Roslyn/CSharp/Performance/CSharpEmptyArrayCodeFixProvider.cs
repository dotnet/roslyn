// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Diagnostics.Analyzers;

namespace Microsoft.CodeAnalysis.Performance
{
    /// <summary>Provides a code fix for the EmptyArrayDiagnosticAnalyzer.</summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "CSharpEmptyArrayCodeFixProvider"), Shared]
    public sealed class CSharpEmptyArrayCodeFixProvider : CodeFixProviderBase
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get { return ImmutableArray.Create(RoslynDiagnosticIds.UseArrayEmptyRuleId); } }

        public override FixAllProvider GetFixAllProvider() { return WellKnownFixAllProviders.BatchFixer; }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            Debug.Assert(ruleId == EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor.Id);
            return EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor.Description.ToString(CultureInfo.CurrentUICulture);
        }

        internal override Task<Document> GetUpdatedDocumentAsync(
            Document document, SemanticModel model, SyntaxNode root,
            SyntaxNode nodeToFix, string diagnosticId, CancellationToken cancellationToken)
        {
            ArrayTypeSyntax arrayType = GetArrayType(nodeToFix);
            if (arrayType != null)
            {
                TypeSyntax elementType = arrayType.ElementType.WithoutLeadingTrivia().WithoutTrailingTrivia();
                if (arrayType.RankSpecifiers.Count > 1)
                {
                    elementType = SyntaxFactory.ArrayType(elementType, SyntaxFactory.List(arrayType.RankSpecifiers.Skip(1)));
                }

                InvocationExpressionSyntax syntax = InvokeStaticGenericParameterlessMethod(
                    model.Compilation.GetTypeByMetadataName("System.Array"),
                    EmptyArrayDiagnosticAnalyzer.ArrayEmptyMethodName,
                    elementType.WithoutLeadingTrivia().WithoutTrailingTrivia());

                if (nodeToFix.HasLeadingTrivia)
                {
                    syntax = syntax.WithLeadingTrivia(nodeToFix.GetLeadingTrivia());
                }

                if (nodeToFix.HasTrailingTrivia)
                {
                    syntax = syntax.WithTrailingTrivia(nodeToFix.GetTrailingTrivia());
                }

                if (syntax != null)
                {
                    root = root.ReplaceNode(nodeToFix, syntax);
                    document = document.WithSyntaxRoot(root);
                }
            }

            return Task.FromResult(document);
        }

        /// <summary>Gets the ArrayTypeSyntax from a syntax node representing an empty array construction.</summary>
        /// <param name="nodeToFix">The syntax node.</param>
        /// <returns>The ArrayTypeSyntax if it could be extracted; otherwise, null.</returns>
        private static ArrayTypeSyntax GetArrayType(SyntaxNode nodeToFix)
        {
            // ArrayCreationExpressionSyntax aces = nodeToFix as ArrayCreationExpressionSyntax;
            // return aces != null ? aces.Type :
            //     ((nodeToFix as InitializerExpressionSyntax)?.Parent?.Parent?.Parent as VariableDeclarationSyntax)?.Type as ArrayTypeSyntax;

            ArrayCreationExpressionSyntax aces = nodeToFix as ArrayCreationExpressionSyntax;
            if (aces != null)
            {
                return aces.Type;
            }

            SyntaxNode sn = nodeToFix as InitializerExpressionSyntax;
            for (int i = 0; i < 3 && sn != null; sn = sn.Parent, i++)
            {
            }

            VariableDeclarationSyntax vds = sn as VariableDeclarationSyntax;
            return vds != null ? vds.Type as ArrayTypeSyntax : null;
        }

        /// <summary>Create an invocation expression for typeSymbol.methodName&lt;genericParameter&gt;()".</summary>
        /// <param name="typeSymbol">The type on which to invoke the static method.</param>
        /// <param name="methodName">The name of the static, parameterless, generic method.</param>
        /// <param name="genericParameter">the type to use for the method's generic parameter.</param>
        /// <returns>The resulting invocation expression.</returns>
        private static InvocationExpressionSyntax InvokeStaticGenericParameterlessMethod(
            INamedTypeSymbol typeSymbol, string methodName, TypeSyntax genericParameter)
        {
            return (typeSymbol != null && methodName != null && genericParameter != null) ?
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.ParseName(typeSymbol.ContainingNamespace.ToDisplayString()),
                            SyntaxFactory.IdentifierName(typeSymbol.Name))
                                .WithAdditionalAnnotations(Simplification.Simplifier.Annotation),
                        SyntaxFactory.GenericName(methodName).WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(genericParameter))))) :
                null;
        }
    }
}
