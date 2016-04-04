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
    internal static class GenerateMethodDiagnosticIds
    {
        private const string CS0103 = nameof(CS0103); // error CS0103: Error The name 'Foo' does not exist in the current context
        private const string CS1061 = nameof(CS1061); // error CS1061: Error 'Class' does not contain a definition for 'Foo' and no extension method 'Foo' 
        private const string CS0117 = nameof(CS0117); // error CS0117: 'Class' does not contain a definition for 'Foo'
        private const string CS0122 = nameof(CS0122); // error CS0122: 'Class' is inaccessible due to its protection level.
        private const string CS0539 = nameof(CS0539); // error CS0539: 'A.Foo<T>()' in explicit interface declaration is not a member of interface
        private const string CS1501 = nameof(CS1501); // error CS1501: No overload for method 'M' takes 1 arguments
        private const string CS1503 = nameof(CS1503); // error CS1503: Argument 1: cannot convert from 'double' to 'int'
        private const string CS0305 = nameof(CS0305); // error CS0305: Using the generic method 'CA.M<V>()' requires 1 type arguments
        private const string CS0308 = nameof(CS0308); // error CS0308: The non-generic method 'Program.Foo()' cannot be used with type arguments
        private const string CS1660 = nameof(CS1660); // error CS1660: Cannot convert lambda expression to type 'string[]' because it is not a delegate type
        private const string CS1739 = nameof(CS1739); // error CS1739: The best overload for 'M' does not have a parameter named 'x'
        private const string CS7036 = nameof(CS7036); // error CS7036: There is no argument given that corresponds to the required formal parameter 'x' of 'C.M(int)'

        public static readonly ImmutableArray<string> FixableDiagnosticIds =
            ImmutableArray.Create(CS0103, CS1061, CS0117, CS0122, CS0539, CS1501, CS1503, CS0305, CS0308, CS1660, CS1739, CS7036);
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateMethod), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateEnumMember)]
    internal class GenerateMethodCodeFixProvider : AbstractGenerateMemberCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = 
            GenerateMethodDiagnosticIds.FixableDiagnosticIds;

        protected override bool IsCandidate(SyntaxNode node, Diagnostic diagnostic)
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
            var service = document.GetLanguageService<IGenerateParameterizedMemberService>();
            return service.GenerateMethodAsync(document, node, cancellationToken);
        }
    }
}
