﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.GenerateMember;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateConversion), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateEnumMember)]
    internal class GenerateConversionCodeFixProvider : AbstractGenerateMemberCodeFixProvider
    {
        private const string CS0029 = nameof(CS0029); // error CS0029: Cannot implicitly convert type 'type' to 'type'
        private const string CS0030 = nameof(CS0030); // error CS0030: Cannot convert type 'type' to 'type'

        [ImportingConstructor]
        public GenerateConversionCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0029, CS0030); }
        }

        protected override bool IsCandidate(SyntaxNode node, SyntaxToken token, Diagnostic diagnostic)
        {
            return node.IsKind(SyntaxKind.IdentifierName) ||
                   node.IsKind(SyntaxKind.MethodDeclaration) ||
                   node.IsKind(SyntaxKind.InvocationExpression) ||
                   node.IsKind(SyntaxKind.CastExpression) ||
                   node is LiteralExpressionSyntax ||
                   node is SimpleNameSyntax ||
                   node is ExpressionSyntax;
        }

        protected override SyntaxNode GetTargetNode(SyntaxNode node)
        {
            if (node is InvocationExpressionSyntax invocation)
            {
                return invocation.Expression.GetRightmostName();
            }

            if (node is MemberBindingExpressionSyntax memberBindingExpression)
            {
                return memberBindingExpression.Name;
            }

            return node;
        }

        protected override Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
            Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IGenerateConversionService>();
            return service.GenerateConversionAsync(document, node, cancellationToken);
        }
    }
}
