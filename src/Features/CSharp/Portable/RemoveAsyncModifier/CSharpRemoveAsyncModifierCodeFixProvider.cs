// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.InitializeParameter;
using Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RemoveAsyncModifier;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveAsyncModifier), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.MakeMethodSynchronous)]
    internal partial class CSharpRemoveAsyncModifierCodeFixProvider : AbstractRemoveAsyncModifierCodeFixProvider<ReturnStatementSyntax>
    {
        private const string CS1998 = nameof(CS1998); // This async method lacks 'await' operators and will run synchronously.

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpRemoveAsyncModifierCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1998);

        protected override bool IsAsyncSupportingFunctionSyntax(SyntaxNode node)
            => node.IsAsyncSupportingFunctionSyntax();

        protected override SyntaxNode? ConvertToBlockBody(SyntaxNode node, SyntaxNode expressionBody)
        {
            if (InitializeParameterHelpers.TryConvertExpressionBodyToStatement(expressionBody, SyntaxFactory.Token(SyntaxKind.SemicolonToken), false, out var statement))
            {
                var block = SyntaxFactory.Block(statement);
                return node switch
                {
                    MethodDeclarationSyntax method => method.WithBody(block).WithExpressionBody(null).WithSemicolonToken(default),
                    LocalFunctionStatementSyntax localFunction => localFunction.WithBody(block).WithExpressionBody(null).WithSemicolonToken(default),
                    AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.WithBody(block).WithExpressionBody(null),
                    _ => throw ExceptionUtilities.Unreachable
                };
            }

            return null;
        }

        protected override SyntaxNode RemoveAsyncModifier(SyntaxNode node)
            => node switch
            {
                MethodDeclarationSyntax method => RemoveAsyncModifierHelpers.WithoutAsyncModifier(method, method.ReturnType),
                LocalFunctionStatementSyntax localFunction => RemoveAsyncModifierHelpers.WithoutAsyncModifier(localFunction, localFunction.ReturnType),
                AnonymousMethodExpressionSyntax method => RemoveAsyncModifierHelpers.WithoutAsyncModifier(method),
                ParenthesizedLambdaExpressionSyntax lambda => RemoveAsyncModifierHelpers.WithoutAsyncModifier(lambda),
                SimpleLambdaExpressionSyntax lambda => RemoveAsyncModifierHelpers.WithoutAsyncModifier(lambda),
                _ => node,
            };
    }
}
