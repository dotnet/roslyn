// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionInitializer), Shared]
    internal partial class CSharpUseCollectionInitializerCodeFixProvider :
        AbstractUseCollectionInitializerCodeFixProvider<
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            BaseObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            ForEachStatementSyntax,
            IfStatementSyntax,
            VariableDeclaratorSyntax,
            CSharpUseCollectionInitializerAnalyzer>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseCollectionInitializerCodeFixProvider()
        {
        }

        protected override CSharpUseCollectionInitializerAnalyzer GetAnalyzer()
            => CSharpUseCollectionInitializerAnalyzer.Allocate();

        protected override async Task<StatementSyntax> GetNewStatementAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            StatementSyntax statement,
            BaseObjectCreationExpressionSyntax objectCreation,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
            var newObjectCreation = await GetNewObjectCreationAsync(
                document, fallbackOptions, objectCreation, useCollectionExpression, matches, cancellationToken).ConfigureAwait(false);
            return statement.ReplaceNode(objectCreation, newObjectCreation);
        }

        private static async Task<ExpressionSyntax> GetNewObjectCreationAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            BaseObjectCreationExpressionSyntax objectCreation,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
            return useCollectionExpression
                ? await CreateCollectionExpressionAsync(document, fallbackOptions, objectCreation, matches, cancellationToken).ConfigureAwait(false)
                : CreateObjectInitializerExpression(objectCreation, matches);
        }

        // Helpers used both by CollectionInitializers and CollectionExpressions.

        private static ExpressionSyntax ConvertExpression(
            ExpressionSyntax expression,
            Func<ExpressionSyntax, ExpressionSyntax>? indent)
        {
            // This must be called from an expression from the original tree.  Not something we're already transforming.
            // Otherwise, we'll have no idea how to apply the preferredIndentation if present.
            Contract.ThrowIfNull(expression.Parent);
            return expression switch
            {
                InvocationExpressionSyntax invocation => ConvertInvocation(invocation, indent),
                AssignmentExpressionSyntax assignment => ConvertAssignment(assignment, indent),
                _ => throw new InvalidOperationException(),
            };
        }

        private static AssignmentExpressionSyntax ConvertAssignment(
            AssignmentExpressionSyntax assignment,
            Func<ExpressionSyntax, ExpressionSyntax>? indent)
        {
            // Assignment is only used for collection-initializers, which *currently* do not do any special
            // indentation handling on elements.
            Contract.ThrowIfTrue(indent != null);

            var elementAccess = (ElementAccessExpressionSyntax)assignment.Left;
            return assignment.WithLeft(
                ImplicitElementAccess(elementAccess.ArgumentList));
        }

        private static ExpressionSyntax ConvertInvocation(
            InvocationExpressionSyntax invocation,
            Func<ExpressionSyntax, ExpressionSyntax>? indent)
        {
            indent ??= static expr => expr;
            var arguments = invocation.ArgumentList.Arguments;

            if (arguments.Count == 1)
            {
                // Assignment expressions in a collection initializer will cause the compiler to 
                // report an error.  This is because { a = b } is the form for an object initializer,
                // and the two forms are not allowed to mix/match.  Parenthesize the assignment to
                // avoid the ambiguity.
                var expression = indent(arguments[0].Expression);
                return SyntaxFacts.IsAssignmentExpression(expression.Kind())
                    ? ParenthesizedExpression(expression)
                    : expression;
            }

            return InitializerExpression(
                SyntaxKind.ComplexElementInitializerExpression,
                Token(SyntaxKind.OpenBraceToken).WithoutTrivia(),
                SeparatedList(
                    arguments.Select(a => a.Expression),
                    arguments.GetSeparators()),
                Token(SyntaxKind.CloseBraceToken).WithoutTrivia());
        }
    }
}
