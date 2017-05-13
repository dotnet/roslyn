// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.GenerateMember;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructor
{
    internal static class GenerateConstructorDiagnosticIds
    {
        public const string CS0122 = nameof(CS0122); // CS0122: 'C' is inaccessible due to its protection level
        public const string CS1729 = nameof(CS1729); // CS1729: 'C' does not contain a constructor that takes n arguments
        public const string CS1739 = nameof(CS1739); // CS1739: The best overload for 'Program' does not have a parameter named 'v'
        public const string CS1503 = nameof(CS1503); // CS1503: Argument 1: cannot convert from 'T1' to 'T2'
        public const string CS7036 = nameof(CS7036); // CS7036: There is no argument given that corresponds to the required formal parameter 'v' of 'C.C(int)'

        public static readonly ImmutableArray<string> AllDiagnosticIds = 
            ImmutableArray.Create(CS0122, CS1729, CS1739, CS1503, CS7036);

        public static readonly ImmutableArray<string> TooManyArgumentsDiagnosticIds =
            ImmutableArray.Create(CS1729);
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateConstructor), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.FullyQualify)]
    internal class GenerateConstructorCodeFixProvider : AbstractGenerateMemberCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => GenerateConstructorDiagnosticIds.AllDiagnosticIds;

        protected override Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
            Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IGenerateConstructorService>();
            return service.GenerateConstructorAsync(document, node, cancellationToken);
        }

        protected override bool IsCandidate(SyntaxNode node, SyntaxToken token, Diagnostic diagnostic)
        {
            return node is ObjectCreationExpressionSyntax ||
                   node is ConstructorInitializerSyntax ||
                   node is AttributeSyntax;
        }

        protected override SyntaxNode GetTargetNode(SyntaxNode node)
        {
            switch (node)
            {
                case ObjectCreationExpressionSyntax objectCreationNode:
                    return objectCreationNode.Type.GetRightmostName();
                case AttributeSyntax attributeNode:
                    return attributeNode.Name;
            }

            return node;
        }
    }
}