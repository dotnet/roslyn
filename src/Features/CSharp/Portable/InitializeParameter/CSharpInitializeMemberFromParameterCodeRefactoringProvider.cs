// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InitializeMemberFromParameter), Shared]
    [ExtensionOrder(Before = nameof(CSharpAddParameterCheckCodeRefactoringProvider))]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.Wrapping)]
    internal class CSharpInitializeMemberFromParameterCodeRefactoringProvider :
        AbstractInitializeMemberFromParameterCodeRefactoringProvider<
            BaseTypeDeclarationSyntax,
            ParameterSyntax,
            StatementSyntax,
            ExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpInitializeMemberFromParameterCodeRefactoringProvider()
        {
        }

        protected override ISyntaxFacts SyntaxFacts
            => CSharpSyntaxFacts.Instance;

        protected override bool SupportsRecords(ParseOptions options)
            => false;

        protected override bool IsFunctionDeclaration(SyntaxNode node)
            => InitializeParameterHelpers.IsFunctionDeclaration(node);

        protected override SyntaxNode? TryGetLastStatement(IBlockOperation? blockStatement)
            => InitializeParameterHelpers.TryGetLastStatement(blockStatement);

        protected override void InsertStatement(SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode? statementToAddAfter, StatementSyntax statement)
            => InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, returnsVoid, statementToAddAfter, statement);

        protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);

        // Fields are always private by default in C#.
        protected override Accessibility DetermineDefaultFieldAccessibility(INamedTypeSymbol containingType)
            => Accessibility.Private;

        // Properties are always private by default in C#.
        protected override Accessibility DetermineDefaultPropertyAccessibility()
            => Accessibility.Private;

        protected override SyntaxNode GetBody(SyntaxNode functionDeclaration)
            => InitializeParameterHelpers.GetBody(functionDeclaration);

        protected override SyntaxNode? GetAccessorBody(IMethodSymbol accessor, CancellationToken cancellationToken)
        {
            var node = accessor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            if (node is AccessorDeclarationSyntax accessorDeclaration)
                return accessorDeclaration.ExpressionBody ?? (SyntaxNode?)accessorDeclaration.Body;

            // `int Age => ...;`
            if (node is ArrowExpressionClauseSyntax arrowExpression)
                return arrowExpression;

            return null;
        }

        protected override SyntaxNode RemoveThrowNotImplemented(SyntaxNode node)
        {
            if (node is PropertyDeclarationSyntax propertyDeclaration)
            {
                if (propertyDeclaration.ExpressionBody != null)
                {
                    var result = propertyDeclaration
                        .WithExpressionBody(null)
                        .WithSemicolonToken(default)
                        .AddAccessorListAccessors(SyntaxFactory
                            .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                        .WithTrailingTrivia(propertyDeclaration.SemicolonToken.TrailingTrivia)
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    return result;
                }

                if (propertyDeclaration.AccessorList != null)
                {
                    var accessors = propertyDeclaration.AccessorList.Accessors.Select(RemoveThrowNotImplemented);
                    return propertyDeclaration.WithAccessorList(
                        propertyDeclaration.AccessorList.WithAccessors(SyntaxFactory.List(accessors)));
                }
            }

            return node;
        }

        private static AccessorDeclarationSyntax RemoveThrowNotImplemented(AccessorDeclarationSyntax accessorDeclaration)
        {
            var result = accessorDeclaration
                .WithExpressionBody(null)
                .WithBody(null)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            return result.WithTrailingTrivia(accessorDeclaration.Body?.GetTrailingTrivia() ?? accessorDeclaration.SemicolonToken.TrailingTrivia);
        }
    }
}
