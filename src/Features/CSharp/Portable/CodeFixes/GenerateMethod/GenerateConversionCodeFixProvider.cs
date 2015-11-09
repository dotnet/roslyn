// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        private const string CS0029 = "CS0029"; // error CS0029: Cannot implicitly convert type 'type' to 'type'
        private const string CS0030 = "CS0030"; // error CS0030: Cannot convert type 'type' to 'type'

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0029, CS0030); }
        }

        protected override bool IsCandidate(SyntaxNode node)
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
            var invocation = node as InvocationExpressionSyntax;
            if (invocation != null)
            {
                return invocation.Expression.GetRightmostName();
            }

            var memberBindingExpression = node as MemberBindingExpressionSyntax;
            if (memberBindingExpression != null)
            {
                return memberBindingExpression.Name;
            }

            return node;
        }

        protected override Task<IEnumerable<CodeAction>> GetCodeActionsAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IGenerateConversionService>();
            return service.GenerateConversionAsync(document, node, cancellationToken);
        }
    }
}
