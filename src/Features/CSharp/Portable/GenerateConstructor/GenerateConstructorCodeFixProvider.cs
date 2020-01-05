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
using Microsoft.CodeAnalysis.Diagnostics;
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
        public const string CS1660 = nameof(CS1660); // CS1660: Cannot convert lambda expression to type 'string[]' because it is not a delegate type
        public const string CS7036 = nameof(CS7036); // CS7036: There is no argument given that corresponds to the required formal parameter 'v' of 'C.C(int)'

        public static readonly ImmutableArray<string> AllDiagnosticIds =
            ImmutableArray.Create(CS0122, CS1729, CS1739, CS1503, CS1660, CS7036, IDEDiagnosticIds.UnboundConstructorId);

        public static readonly ImmutableArray<string> TooManyArgumentsDiagnosticIds =
            ImmutableArray.Create(CS1729);

        public static readonly ImmutableArray<string> CannotConvertDiagnosticIds =
            ImmutableArray.Create(CS1503, CS1660);
    }

    /// <summary>
    /// This <see cref="CodeFixProvider"/> gives users a way to generate constructors for an existing
    /// type when a user tries to 'new' up an instance of that type with a set of parameter that does
    /// not match any existing constructor.  i.e. it is the equivalent of 'Generate-Method' but for
    /// constructors.  Parameters for the constructor will be picked in a manner similar to Generate-
    /// Method.  However, this type will also attempt to hook up those parameters to existing fields
    /// and properties, or pass them to a this/base constructor if available.
    /// 
    /// Importantly, this type is not responsible for generating constructors for a type based on 
    /// the user selecting some fields/properties of that type.  Nor is it responsible for generating
    /// derived class constructors for all unmatched base class constructors in a type hierarchy.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateConstructor), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.FullyQualify)]
    internal class GenerateConstructorCodeFixProvider : AbstractGenerateMemberCodeFixProvider
    {
        [ImportingConstructor]
        public GenerateConstructorCodeFixProvider()
        {
        }

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
