// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExplicitArrayInExpressionTree;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExplicitArrayInExpressionTree), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpUseExplicitArrayInExpressionTreeCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    private const string CS9226 = nameof(CS9226); // An expression tree may not contain an expanded form of non-array params collection parameter.

    public override ImmutableArray<string> FixableDiagnosticIds => [CS9226];

    private static (int index, ITypeSymbol arrayElementType)? DetermineFirstArgumentIndexToWrap(
        SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
    {
        // Only offer if we actually know which method this was calling.
        if (semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol is not IMethodSymbol originalMethod ||
            originalMethod.Parameters is not [.., { IsParams: true } originalParamsParameter])
        {
            return null;
        }

        // Now we have to find a sibling method that exists that is the same, except that it takes a params array instead.
        var memberGroup = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken);

        foreach (var symbol in memberGroup)
        {
            if (symbol is IMethodSymbol currentMethod &&
                currentMethod.Parameters.Length == originalMethod.Parameters.Length &&
                currentMethod.Parameters is [.., { IsParams: true, Type: IArrayTypeSymbol arrayType }])
            {
                if (currentMethod.ReturnsVoid != originalMethod.ReturnsVoid)
                    continue;

                // Different return type, can't switch to this method.
                if (!currentMethod.ReturnsVoid && !Equals(currentMethod.ReturnType, originalMethod.ReturnType))
                    continue;

                if (!ParameterTypesMatch(originalMethod, currentMethod))
                    continue;

                // Sibling looks good. Determine the index of the first argument that needs to be wrapped with an array
                // creation expression.
                var arguments = invocationExpression.ArgumentList.Arguments;
                for (int i = 0, n = arguments.Count; i < n; i++)
                {
                    var parameter = arguments[i].DetermineParameter(semanticModel, allowUncertainCandidates: false, allowParams: true, cancellationToken);

                    if (originalParamsParameter.Equals(parameter))
                        return (i, arrayType.ElementType);
                }

                return (arguments.Count, arrayType.ElementType);
            }
        }

        return null;

        static bool ParameterTypesMatch(IMethodSymbol originalMethod, IMethodSymbol currentMethod)
        {
            for (int i = 0, n = currentMethod.Parameters.Length - 1; i < n; i++)
            {
                var originalParameter = originalMethod.Parameters[i];
                var currentParameter = currentMethod.Parameters[i];

                // Different parameter types, look for another sibling that matches
                if (!originalParameter.Type.Equals(currentParameter.Type))
                    return false;
            }

            return true;
        }
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var diagnostic = context.Diagnostics.First();
        var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);

        if (node is InvocationExpressionSyntax invocationExpression)
        {
            var semanticModel = await context.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel.Compilation.ArrayType() != null)
            {
                var index = DetermineFirstArgumentIndexToWrap(semanticModel, invocationExpression, cancellationToken);
                if (index != null)
                {
                    RegisterCodeFix(
                        context, CSharpCodeFixesResources.Use_explicit_array, nameof(CSharpCodeFixesResources.Use_explicit_array));
                }
            }
        }
    }

    protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var arrayType = semanticModel.Compilation.ArrayType();
        Contract.ThrowIfNull(arrayType); // Validated in RegisterCodeFixesAsync

        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
        {
            if (diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken) is not InvocationExpressionSyntax invocation)
                continue;

            if (DetermineFirstArgumentIndexToWrap(semanticModel, invocation, cancellationToken) is not (int indexToWrap, ITypeSymbol arrayElementType))
                continue;

            // We can use an implicit array if at least one of the arguments we're wrapping has the same type as the
            // params array type we're creating.  Note: skip `default` literals as the compiler reports them as having
            // the expected type, instead of showing that as the converted type of the literal.
            var canUseImplicitArray = invocation.ArgumentList.Arguments.Skip(indexToWrap).Any(
                a => a.Expression.Kind() != SyntaxKind.DefaultLiteralExpression && Equals(arrayElementType, semanticModel.GetTypeInfo(a.Expression).Type));

            editor.ReplaceNode(
                invocation.ArgumentList,
                (current, _) =>
                {
                    var argumentList = (ArgumentListSyntax)current;

                    // Cases to handle:
                    //
                    // 1. The index is at the end of the argument list.  In which case no actual explicit arguments
                    //    were passed to the params parameter.  In that case, we want to explicitly pass
                    //    Array.Empty<T>() instead.
                    var arrayElementTypeNode = arrayElementType.GenerateTypeSyntax(allowVar: false);
                    if (indexToWrap == argumentList.Arguments.Count)
                    {
                        return argumentList.AddArguments(Argument(InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                arrayType.GenerateTypeSyntax(),
                                GenericName(
                                    Identifier(nameof(Array.Empty)),
                                    TypeArgumentList([arrayElementTypeNode]))))));
                    }
                    else if (indexToWrap < argumentList.Arguments.Count)
                    {
                        // Take the index * 2 so we skip past the prior arguments and their comma separators.
                        var argumentsWithSeparators = argumentList.Arguments.GetWithSeparators();

                        var expressionsAndCommasToWrap = argumentsWithSeparators.Skip(indexToWrap * 2)
                            .Select(a => a.IsNode ? ((ArgumentSyntax)a.AsNode()!).Expression : a);
                        var initializer = InitializerExpression(
                            SyntaxKind.ArrayInitializerExpression,
                            SeparatedList<ExpressionSyntax>(expressionsAndCommasToWrap));

                        var wrappedArgument = Argument(canUseImplicitArray
                            ? ImplicitArrayCreationExpression(initializer)
                            : ArrayCreationExpression(ArrayType(
                                arrayElementTypeNode,
                                [ArrayRankSpecifier([OmittedArraySizeExpression()])]), initializer));

                        var finalArgumentsWithSeparators = argumentsWithSeparators.Take(indexToWrap * 2).Concat(wrappedArgument);
                        return argumentList.WithArguments(SeparatedList<ArgumentSyntax>(finalArgumentsWithSeparators));
                    }

                    return current;
                });
        }
    }
}
