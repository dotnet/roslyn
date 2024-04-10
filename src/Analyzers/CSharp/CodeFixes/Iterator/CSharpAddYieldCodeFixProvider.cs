// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Iterator;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ChangeToYield), Shared]
internal class CSharpAddYieldCodeFixProvider : AbstractIteratorCodeFixProvider
{
    /// <summary>
    /// CS0029: Cannot implicitly convert from type 'x' to 'y'
    /// </summary>
    private const string CS0029 = nameof(CS0029);

    /// <summary>
    /// CS0266: Cannot implicitly convert from type 'x' to 'y'. An explicit conversion exists (are you missing a cast?)
    /// </summary>
    private const string CS0266 = nameof(CS0266);

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpAddYieldCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
    {
        get { return [CS0029, CS0266]; }
    }

    protected override async Task<CodeAction?> GetCodeFixAsync(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostics, CancellationToken cancellationToken)
    {
        // Check if node is return statement
        if (node is not ReturnStatementSyntax returnStatement)
        {
            return null;
        }

        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (!TryGetMethodReturnType(node, model, cancellationToken, out var methodReturnType))
        {
            return null;
        }

        if (!TryGetExpressionType(model, returnStatement.Expression, out var returnExpressionType))
        {
            return null;
        }

        var typeArguments = methodReturnType.GetAllTypeArguments();

        var shouldOfferYieldReturn = typeArguments.Length != 1
            ? IsCorrectTypeForYieldReturn(methodReturnType, model)
            : IsCorrectTypeForYieldReturn(typeArguments.Single(), returnExpressionType, methodReturnType, model);

        if (!shouldOfferYieldReturn)
        {
            return null;
        }

        var yieldStatement = SyntaxFactory.YieldStatement(
                SyntaxKind.YieldReturnStatement,
                returnStatement.Expression)
            .WithAdditionalAnnotations(Formatter.Annotation);

        root = root.ReplaceNode(returnStatement, yieldStatement);

        return CodeAction.Create(
            CSharpCodeFixesResources.Replace_return_with_yield_return,
            _ => Task.FromResult(document.WithSyntaxRoot(root)),
            nameof(CSharpCodeFixesResources.Replace_return_with_yield_return));
    }

    private static bool TryGetExpressionType(
        SemanticModel model, ExpressionSyntax? expression, [NotNullWhen(true)] out ITypeSymbol? returnExpressionType)
    {
        if (expression == null)
        {
            returnExpressionType = null;
            return false;
        }

        var info = model.GetTypeInfo(expression);
        returnExpressionType = info.Type;
        return returnExpressionType != null;
    }

    private static bool TryGetMethodReturnType(
        SyntaxNode node, SemanticModel model, CancellationToken cancellationToken,
        [NotNullWhen(true)] out ITypeSymbol? methodReturnType)
    {
        methodReturnType = null;
        var symbol = model.GetEnclosingSymbol(node.Span.Start, cancellationToken);
        if (symbol is not IMethodSymbol method || method.ReturnsVoid)
        {
            return false;
        }

        methodReturnType = method.ReturnType;
        return methodReturnType != null;
    }

    private static bool IsCorrectTypeForYieldReturn(ITypeSymbol typeArgument, ITypeSymbol returnExpressionType, ITypeSymbol methodReturnType, SemanticModel model)
    {
        var ienumerableSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName!);
        var ienumeratorSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerator).FullName!);
        var ienumerableGenericSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName!);
        var ienumeratorGenericSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerator<>).FullName!);

        if (ienumerableGenericSymbol == null ||
            ienumerableSymbol == null ||
            ienumeratorGenericSymbol == null ||
            ienumeratorSymbol == null)
        {
            return false;
        }

        ienumerableGenericSymbol = ienumerableGenericSymbol.Construct(typeArgument);
        ienumeratorGenericSymbol = ienumeratorGenericSymbol.Construct(typeArgument);

        if (!CanConvertTypes(typeArgument, returnExpressionType, model))
        {
            return false;
        }

        if (!(methodReturnType.Equals(ienumerableGenericSymbol) ||
              methodReturnType.Equals(ienumerableSymbol) ||
              methodReturnType.Equals(ienumeratorGenericSymbol) ||
              methodReturnType.Equals(ienumeratorSymbol)))
        {
            return false;
        }

        return true;
    }

    private static bool CanConvertTypes(ITypeSymbol typeArgument, ITypeSymbol returnExpressionType, SemanticModel model)
    {
        // return false if there is no conversion for the top level type
        if (!model.Compilation.ClassifyConversion(typeArgument, returnExpressionType).Exists)
        {
            return false;
        }

        // Classify conversion does not consider type parameters on its own so we will have to recurse through them
        var leftArguments = typeArgument.GetTypeArguments();
        var rightArguments = returnExpressionType.GetTypeArguments();

        // If we have a mismatch in the number of type arguments we can immediately return as there is no way the types are convertible
        if (leftArguments != null &&
            rightArguments != null &&
            leftArguments.Length != rightArguments.Length)
        {
            return false;
        }

        // If there are no more type arguments we assume they are convertible since the outer generic types are convertible
        if (leftArguments == null || !leftArguments.Any())
        {
            return true;
        }

        // Check if all the type arguments are convertible
        for (var i = 0; i < leftArguments.Length; i++)
        {
            if (!CanConvertTypes(leftArguments[i], rightArguments[i], model))
            {
                return false;
            }
        }

        // Type argument comparisons have all succeeded, return true
        return true;
    }

    private static bool IsCorrectTypeForYieldReturn(ITypeSymbol methodReturnType, SemanticModel model)
    {
        var ienumerableSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName!);
        var ienumeratorSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerator).FullName!);

        if (ienumerableSymbol == null ||
                ienumeratorSymbol == null)
        {
            return false;
        }

        if (!(methodReturnType.Equals(ienumerableSymbol) ||
              methodReturnType.Equals(ienumeratorSymbol)))
        {
            return false;
        }

        return true;
    }

    protected override bool TryGetNode(
        SyntaxNode root, TextSpan span, [NotNullWhen(true)] out SyntaxNode? node)
    {
        node = null;
        var ancestors = root.FindToken(span.Start).GetAncestors<SyntaxNode>();
        if (!ancestors.Any())
        {
            return false;
        }

        node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root && n.IsKind(SyntaxKind.ReturnStatement));
        return node != null;
    }
}
