// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

using static SyntaxFactory;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseCollectionInitializerDiagnosticAnalyzer :
    AbstractUseCollectionInitializerDiagnosticAnalyzer<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        InvocationExpressionSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        CSharpUseCollectionInitializerAnalyzer>
{
    protected override ISyntaxFacts SyntaxFacts
        => CSharpSyntaxFacts.Instance;

    protected override CSharpUseCollectionInitializerAnalyzer GetAnalyzer()
        => CSharpUseCollectionInitializerAnalyzer.Allocate();

    protected override bool AreCollectionInitializersSupported(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp3;

    protected override bool AreCollectionExpressionsSupported(Compilation compilation)
        => compilation.LanguageVersion().SupportsCollectionExpressions();

    protected override bool CanUseCollectionExpression(
        SemanticModel semanticModel,
        BaseObjectCreationExpressionSyntax objectCreationExpression,
        INamedTypeSymbol? expressionType,
        ImmutableArray<CollectionMatch<SyntaxNode>> preMatches,
        bool allowSemanticsChange,
        CancellationToken cancellationToken,
        out bool changesSemantics)
    {
        // Synthesize the final collection expression we would replace this object-creation with.  That will allow us to
        // determine if we end up calling the right overload in cases of overloaded methods.
        var replacement = CollectionExpression(SeparatedList(
            GetMatchElements(preMatches).Concat(GetInitializerElements(objectCreationExpression.Initializer))));

        return UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
            semanticModel, objectCreationExpression, replacement, expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out changesSemantics);

        static IEnumerable<CollectionElementSyntax> GetMatchElements(ImmutableArray<CollectionMatch<SyntaxNode>> preMatches)
        {
            foreach (var match in preMatches)
            {
                if (match.Node is ExpressionSyntax expression)
                    yield return match.UseSpread ? SpreadElement(expression) : ExpressionElement(expression);
            }
        }

        static IEnumerable<CollectionElementSyntax> GetInitializerElements(InitializerExpressionSyntax? initializer)
        {
            if (initializer != null)
            {
                foreach (var expression in initializer.Expressions)
                    yield return ExpressionElement(expression);
            }
        }
    }

    protected override bool IsValidContainingStatement(StatementSyntax node)
    {
        // We don't want to offer this for using declarations because the way they are lifted means all
        // initialization is done before entering try block. For example
        // 
        // using var c = new List<int>() { 1 };
        //
        // is lowered to:
        //
        // var __c = new List<int>();
        // __c.Add(1);
        // var c = __c;
        // try
        // {
        // }
        // finally
        // {
        //     if (c != null)
        //     {
        //         ((IDisposable)c).Dispose();
        //     }
        // }
        //
        // As can be seen, if initializing throws any kind of exception, the newly created instance will not
        // be disposed properly.
        return node is not LocalDeclarationStatementSyntax localDecl ||
            localDecl.UsingKeyword == default;
    }

    protected override bool ShouldSuppressDiagnostic(
        SemanticModel semanticModel,
        BaseObjectCreationExpressionSyntax objectCreationExpression,
        ITypeSymbol objectType,
        CancellationToken cancellationToken)
    {
        // Check if the type being created has a CollectionBuilder attribute that points to the method we're currently in.
        // If so, suppress the diagnostic to avoid suggesting a change that would cause infinite recursion.
        // For example, if we're inside the Create method of a CollectionBuilder, and we have:
        //   MyCustomCollection<T> collection = new();
        //   foreach (T item in items) { collection.Add(item); }
        // We should NOT suggest changing it to:
        //   MyCustomCollection<T> collection = [.. items];
        // Because that would recursively call the same Create method.

        if (objectType is not INamedTypeSymbol namedType)
            return false;

        // For generic types, get the type definition to check for the attribute
        var typeToCheck = namedType.OriginalDefinition;

        // Look for CollectionBuilder attribute on the type
        var collectionBuilderAttribute = typeToCheck.GetAttributes().FirstOrDefault(attr =>
            attr.AttributeClass?.IsCollectionBuilderAttribute() == true);

        if (collectionBuilderAttribute == null)
            return false;

        // Get the builder type and method name from the attribute.
        // CollectionBuilderAttribute has exactly 2 constructor parameters: builderType and methodName
        if (collectionBuilderAttribute.ConstructorArguments.Length < 2)
            return false;

        var builderTypeArg = collectionBuilderAttribute.ConstructorArguments[0];
        var methodNameArg = collectionBuilderAttribute.ConstructorArguments[1];

        if (builderTypeArg.Kind != TypedConstantKind.Type ||
            builderTypeArg.Value is not INamedTypeSymbol builderType ||
            methodNameArg.Kind != TypedConstantKind.Primitive ||
            methodNameArg.Value is not string methodName)
        {
            return false;
        }

        // Get the containing method we're currently analyzing using the NewKeyword position for more precision
        var position = objectCreationExpression switch
        {
            ObjectCreationExpressionSyntax objCreation => objCreation.NewKeyword.SpanStart,
            ImplicitObjectCreationExpressionSyntax implicitObjCreation => implicitObjCreation.NewKeyword.SpanStart,
            _ => objectCreationExpression.SpanStart
        };

        var containingMethod = semanticModel.GetEnclosingSymbol<IMethodSymbol>(position, cancellationToken);
        if (containingMethod == null)
            return false;

        // Check if the containing method matches the CollectionBuilder method
        // We need to compare the original definitions in case the method is generic
        if (containingMethod.Name == methodName &&
            SymbolEqualityComparer.Default.Equals(containingMethod.ContainingType.OriginalDefinition, builderType.OriginalDefinition))
        {
            return true;
        }

        return false;
    }
}
