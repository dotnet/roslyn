// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

#if !CODE_STYLE
using Humanizer;
#endif

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class SemanticModelExtensions
{
    private const string DefaultBuiltInParameterName = "v";

    /// <summary>
    /// Gets semantic information, such as type, symbols, and diagnostics, about the parent of a token.
    /// </summary>
    /// <param name="semanticModel">The SemanticModel object to get semantic information
    /// from.</param>
    /// <param name="token">The token to get semantic information from. This must be part of the
    /// syntax tree associated with the binding.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        => semanticModel.GetSymbolInfo(token.Parent!, cancellationToken);

    public static DataFlowAnalysis AnalyzeRequiredDataFlow(this SemanticModel semanticModel, SyntaxNode statementOrExpression)
        => semanticModel.AnalyzeDataFlow(statementOrExpression) ?? throw new InvalidOperationException();

    public static DataFlowAnalysis AnalyzeRequiredDataFlow(this SemanticModel semanticModel, SyntaxNode firstStatement, SyntaxNode lastStatement)
        => semanticModel.AnalyzeDataFlow(firstStatement, lastStatement) ?? throw new InvalidOperationException();

    public static ControlFlowAnalysis AnalyzeRequiredControlFlow(this SemanticModel semanticModel, SyntaxNode statement)
        => semanticModel.AnalyzeControlFlow(statement) ?? throw new InvalidOperationException();

    public static ControlFlowAnalysis AnalyzeRequiredControlFlow(this SemanticModel semanticModel, SyntaxNode firstStatement, SyntaxNode lastStatement)
        => semanticModel.AnalyzeControlFlow(firstStatement, lastStatement) ?? throw new InvalidOperationException();

    public static ISymbol GetRequiredDeclaredSymbol(this SemanticModel semanticModel, SyntaxNode declaration, CancellationToken cancellationToken)
    {
        return semanticModel.GetDeclaredSymbol(declaration, cancellationToken)
            ?? throw new InvalidOperationException();
    }

    public static IOperation GetRequiredOperation(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        return semanticModel.GetOperation(node, cancellationToken)
            ?? throw new InvalidOperationException();
    }

    public static ISymbol GetRequiredEnclosingSymbol(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
    {
        return semanticModel.GetEnclosingSymbol(position, cancellationToken)
            ?? throw new InvalidOperationException();
    }

    public static TSymbol? GetEnclosingSymbol<TSymbol>(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        where TSymbol : class, ISymbol
    {
        for (var symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
             symbol != null;
             symbol = symbol.ContainingSymbol)
        {
            if (symbol is TSymbol tSymbol)
            {
                return tSymbol;
            }
        }

        return null;
    }

    public static ISymbol GetEnclosingNamedTypeOrAssembly(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
    {
        return semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken) ??
            (ISymbol)semanticModel.Compilation.Assembly;
    }

    public static INamedTypeSymbol? GetEnclosingNamedType(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        => semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken);

    public static INamespaceSymbol? GetEnclosingNamespace(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        => semanticModel.GetEnclosingSymbol<INamespaceSymbol>(position, cancellationToken);

    public static IEnumerable<ISymbol> GetExistingSymbols(
                this SemanticModel semanticModel, SyntaxNode? container, CancellationToken cancellationToken, Func<SyntaxNode, bool>? descendInto = null)
    {
        // Ignore an anonymous type property or tuple field.  It's ok if they have a name that
        // matches the name of the local we're introducing.
        return semanticModel.GetAllDeclaredSymbols(container, cancellationToken, descendInto)
            .Where(s => !s.IsAnonymousTypeProperty() && !s.IsTupleField());
    }

    public static SemanticModel GetOriginalSemanticModel(this SemanticModel semanticModel)
    {
        if (!semanticModel.IsSpeculativeSemanticModel)
        {
            return semanticModel;
        }

        Contract.ThrowIfNull(semanticModel.ParentModel);
        Contract.ThrowIfTrue(semanticModel.ParentModel.IsSpeculativeSemanticModel);
        Contract.ThrowIfTrue(semanticModel.ParentModel.ParentModel != null);
        return semanticModel.ParentModel;
    }

    public static HashSet<ISymbol> GetAllDeclaredSymbols(
       this SemanticModel semanticModel, SyntaxNode? container, CancellationToken cancellationToken, Func<SyntaxNode, bool>? filter = null)
    {
        var symbols = new HashSet<ISymbol>();
        if (container != null)
        {
            GetAllDeclaredSymbols(semanticModel, container, symbols, cancellationToken, filter);
        }

        return symbols;
    }

    private static void GetAllDeclaredSymbols(
        SemanticModel semanticModel, SyntaxNode node,
        HashSet<ISymbol> symbols, CancellationToken cancellationToken, Func<SyntaxNode, bool>? descendInto = null)
    {
        var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

        if (symbol != null)
        {
            symbols.Add(symbol);
        }

        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.IsNode)
            {
                var childNode = child.AsNode()!;
                if (ShouldDescendInto(childNode, descendInto))
                {
                    GetAllDeclaredSymbols(semanticModel, childNode, symbols, cancellationToken, descendInto);
                }
            }
        }

        static bool ShouldDescendInto(SyntaxNode node, Func<SyntaxNode, bool>? filter)
            => filter != null ? filter(node) : true;
    }
    public static string GenerateNameFromType(this SemanticModel semanticModel, ITypeSymbol type, ISyntaxFacts syntaxFacts, bool capitalize)
    {
        var pluralize = semanticModel.ShouldPluralize(type);
        var typeArguments = type.GetAllTypeArguments();

        // We may be able to use the type's arguments to generate a name if we're working with an enumerable type.
        if (pluralize && TryGeneratePluralizedNameFromTypeArgument(syntaxFacts, typeArguments, capitalize, out var typeArgumentParameterName))
        {
            return typeArgumentParameterName;
        }

        // If there's no type argument and we have an array type, we should pluralize, e.g. using 'frogs' for 'new Frog[]' instead of 'frog'
        if (type.TypeKind == TypeKind.Array && typeArguments.IsEmpty)
        {
            return Pluralize(type.CreateParameterName(capitalize));
        }

        // Otherwise assume no pluralization, e.g. using 'immutableArray', 'list', etc. instead of their
        // plural forms
        if (type.IsSpecialType() ||
            type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
            type.TypeKind == TypeKind.Pointer)
        {
            return capitalize ? DefaultBuiltInParameterName.ToUpper() : DefaultBuiltInParameterName;
        }
        else
        {
            return type.CreateParameterName(capitalize);
        }
    }

    private static bool ShouldPluralize(this SemanticModel semanticModel, ITypeSymbol type)
    {
        if (type == null)
            return false;

        // string implements IEnumerable<char>, so we need to specifically exclude it.
        if (type.SpecialType == SpecialType.System_String)
            return false;

        var enumerableType = semanticModel.Compilation.IEnumerableOfTType();
        return type.AllInterfaces.Any(static (i, enumerableType) => i.OriginalDefinition.Equals(enumerableType), enumerableType);
    }

    private static bool TryGeneratePluralizedNameFromTypeArgument(
        ISyntaxFacts syntaxFacts,
        ImmutableArray<ITypeSymbol> typeArguments,
        bool capitalize,
        [NotNullWhen(true)] out string? parameterName)
    {
        // We only consider generating a name if there's one type argument.
        // This logic can potentially be expanded upon in the future.
        if (typeArguments.Length == 1)
        {
            // We only want the last part of the type, i.e. we don't want namespaces.
            var typeArgument = typeArguments.Single().ToDisplayParts().Last().ToString();
            if (syntaxFacts.IsValidIdentifier(typeArgument))
            {
                typeArgument = Pluralize(typeArgument);
                parameterName = capitalize ? typeArgument.ToPascalCase() : typeArgument.ToCamelCase();
                return true;
            }
        }

        parameterName = null;
        return false;
    }

    public static string Pluralize(string word)
    {
#if CODE_STYLE
        return word;
#else
        return word.Pluralize();
#endif
    }

    /// <summary>
    /// Fetches the ITypeSymbol that should be used if we were generating a parameter or local that would accept <paramref name="expression"/>. If
    /// expression is a type, that's returned; otherwise this will see if it's something like a method group and then choose an appropriate delegate.
    /// </summary>
    public static ITypeSymbol GetType(
        this SemanticModel semanticModel,
        SyntaxNode expression,
        CancellationToken cancellationToken)
    {
        var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

        if (typeInfo.Type != null)
        {
            return typeInfo.Type;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
        return symbolInfo.GetAnySymbol().ConvertToType(semanticModel.Compilation);
    }
}
