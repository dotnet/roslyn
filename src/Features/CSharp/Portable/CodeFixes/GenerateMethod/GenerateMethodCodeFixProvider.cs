// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.GenerateMember;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod;

internal static class GenerateMethodDiagnosticIds
{
    private const string CS0103 = nameof(CS0103); // error CS0103: Error The name 'Goo' does not exist in the current context
    private const string CS0117 = nameof(CS0117); // error CS0117: 'Class' does not contain a definition for 'Goo'
    private const string CS0118 = nameof(CS0118); // error CS0118: 'X' is a namespace but is used like a variable
    private const string CS0122 = nameof(CS0122); // error CS0122: 'Class' is inaccessible due to its protection level.
    private const string CS0305 = nameof(CS0305); // error CS0305: Using the generic method 'CA.M<V>()' requires 1 type arguments
    private const string CS0308 = nameof(CS0308); // error CS0308: The non-generic method 'Program.Goo()' cannot be used with type arguments
    private const string CS0539 = nameof(CS0539); // error CS0539: 'A.Goo<T>()' in explicit interface declaration is not a member of interface
    private const string CS1061 = nameof(CS1061); // error CS1061: Error 'Class' does not contain a definition for 'Goo' and no extension method 'Goo' 
    private const string CS1501 = nameof(CS1501); // error CS1501: No overload for method 'M' takes 1 arguments
    private const string CS1503 = nameof(CS1503); // error CS1503: Argument 1: cannot convert from 'double' to 'int'
    private const string CS1660 = nameof(CS1660); // error CS1660: Cannot convert lambda expression to type 'string[]' because it is not a delegate type
    private const string CS1739 = nameof(CS1739); // error CS1739: The best overload for 'M' does not have a parameter named 'x'
    private const string CS7036 = nameof(CS7036); // error CS7036: There is no argument given that corresponds to the required parameter 'x' of 'C.M(int)'
    private const string CS1955 = nameof(CS1955); // error CS1955: Non-invocable member 'Goo' cannot be used like a method.
    private const string CS0123 = nameof(CS0123); // error CS0123: No overload for 'OnChanged' matches delegate 'NotifyCollectionChangedEventHandler'

    public static readonly ImmutableArray<string> FixableDiagnosticIds =
        [CS0103, CS0117, CS0118, CS0122, CS0305, CS0308, CS0539, CS1061, CS1501, CS1503, CS1660, CS1739, CS7036, CS1955, CS0123];
}

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateMethod), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateEnumMember)]
[ExtensionOrder(Before = PredefinedCodeFixProviderNames.PopulateSwitch)]
[ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateVariable)]
internal sealed class GenerateMethodCodeFixProvider : AbstractGenerateMemberCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public GenerateMethodCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        GenerateMethodDiagnosticIds.FixableDiagnosticIds;

    protected override bool IsCandidate(SyntaxNode node, SyntaxToken token, Diagnostic diagnostic)
    {
        return node.Kind()
                is SyntaxKind.IdentifierName
                or SyntaxKind.MethodDeclaration
                or SyntaxKind.InvocationExpression
                or SyntaxKind.CastExpression ||
               node is LiteralExpressionSyntax ||
               node is SimpleNameSyntax ||
               node is ExpressionSyntax;
    }

    protected override SyntaxNode? GetTargetNode(SyntaxNode node)
    {
        switch (node)
        {
            case InvocationExpressionSyntax invocation:
                return invocation.Expression.GetRightmostName();
            case MemberBindingExpressionSyntax memberBindingExpression:
                return memberBindingExpression.Name;
            case AssignmentExpressionSyntax assignment:
                return assignment.Right;
        }

        return node;
    }

    protected override Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var service = document.GetRequiredLanguageService<IGenerateParameterizedMemberService>();
        return service.GenerateMethodAsync(document, node, cancellationToken);
    }
}
