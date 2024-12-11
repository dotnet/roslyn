// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;

internal static class GenerateConstructorHelpers
{
    public static bool CanDelegateTo<TExpressionSyntax>(
        SemanticDocument document,
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<TExpressionSyntax?> expressions,
        IMethodSymbol constructor)
        where TExpressionSyntax : SyntaxNode
    {
        // Look for constructors in this specified type that are:
        // 1. Accessible.  We obviously need our constructor to be able to call that other constructor.
        // 2. Won't cause a cycle.  i.e. if we're generating a new constructor from an existing constructor,
        //    then we don't want it calling back into us.
        // 3. Are compatible with the parameters we're generating for this constructor.  Compatible means there
        //    exists an implicit conversion from the new constructor's parameter types to the existing
        //    constructor's parameter types.
        var semanticFacts = document.Document.GetRequiredLanguageService<ISemanticFactsService>();
        var semanticModel = document.SemanticModel;
        var compilation = semanticModel.Compilation;

        return constructor.Parameters.Length == parameters.Length &&
               constructor.Parameters.SequenceEqual(parameters, (p1, p2) => p1.RefKind == p2.RefKind) &&
               IsSymbolAccessible(compilation, constructor) &&
               IsCompatible(semanticFacts, semanticModel, constructor, expressions);
    }

    private static bool IsSymbolAccessible(Compilation compilation, ISymbol symbol)
    {
        if (symbol == null)
            return false;

        if (symbol is IPropertySymbol { SetMethod: { } setMethod } &&
            !IsSymbolAccessible(compilation, setMethod))
        {
            return false;
        }

        // Public and protected constructors are accessible.  Internal constructors are
        // accessible if we have friend access.  We can't call the normal accessibility
        // checkers since they will think that a protected constructor isn't accessible
        // (since we don't have the destination type that would have access to them yet).
        switch (symbol.DeclaredAccessibility)
        {
            case Accessibility.ProtectedOrInternal:
            case Accessibility.Protected:
            case Accessibility.Public:
                return true;
            case Accessibility.ProtectedAndInternal:
            case Accessibility.Internal:
                return compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(symbol.ContainingAssembly);

            default:
                return false;
        }
    }

    private static bool IsCompatible<TExpressionSyntax>(
        ISemanticFactsService semanticFacts,
        SemanticModel semanticModel,
        IMethodSymbol constructor,
        ImmutableArray<TExpressionSyntax?> expressions)
        where TExpressionSyntax : SyntaxNode
    {
        Debug.Assert(constructor.Parameters.Length == expressions.Length);

        // Resolve the constructor into our semantic model's compilation; if the constructor we're looking at is from
        // another project with a different language.
        var constructorInCompilation = (IMethodSymbol?)SymbolKey.Create(constructor).Resolve(semanticModel.Compilation).Symbol;

        if (constructorInCompilation == null)
        {
            // If the constructor can't be mapped into our invocation project, we'll just bail.
            // Note the logic in this method doesn't handle a complicated case where:
            //
            // 1. Project A has some public type.
            // 2. Project B references A, and has one constructor that uses the public type from A.
            // 3. Project C, which references B but not A, has an invocation of B's constructor passing some
            //    parameters.
            //
            // The algorithm of this class tries to map the constructor in B (that we might delegate to) into
            // C, but that constructor might not be mappable if the public type from A is not available.
            // However, theoretically the public type from A could have a user-defined conversion.
            // The alternative approach might be to map the type of the parameters back into B, and then
            // classify the conversions in Project B, but that'll run into other issues if the experssions
            // don't have a natural type (like default). We choose to ignore all complicated cases here.
            return false;
        }

        for (var i = 0; i < constructorInCompilation.Parameters.Length; i++)
        {
            var constructorParameter = constructorInCompilation.Parameters[i];
            if (constructorParameter == null)
                return false;

            var expression = expressions[i];
            if (expression is null)
                continue;

            var conversion = semanticFacts.ClassifyConversion(semanticModel, expression, constructorParameter.Type);
            if (!conversion.IsIdentity && !conversion.IsImplicit)
                return false;
        }

        return true;
    }

    public static async Task<
            (ImmutableArray<IParameterSymbol> parameters,
             ImmutableDictionary<string, ISymbol> parameterToExistingMemberMap,
             ImmutableDictionary<string, string> parameterToNewFieldMap,
             ImmutableDictionary<string, string> parameterToNewPropertyMap)> GetParametersAsync<TExpressionSyntax>(
        SemanticDocument document,
        INamedTypeSymbol typeToGenerateIn,
        ImmutableArray<Argument<TExpressionSyntax>> arguments,
        ImmutableArray<ITypeSymbol> parameterTypes,
        ImmutableArray<ParameterName> parameterNames,
        CancellationToken cancellationToken)
        where TExpressionSyntax : SyntaxNode
    {
        var fieldNamingRule = await document.Document.GetApplicableNamingRuleAsync(SymbolKind.Field, Accessibility.Private, cancellationToken).ConfigureAwait(false);
        var propertyNamingRule = await document.Document.GetApplicableNamingRuleAsync(SymbolKind.Property, Accessibility.Public, cancellationToken).ConfigureAwait(false);
        var parameterNamingRule = await document.Document.GetApplicableNamingRuleAsync(SymbolKind.Parameter, Accessibility.NotApplicable, cancellationToken).ConfigureAwait(false);

        var rules = await document.Document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);

        var parameterToExistingMemberMap = ImmutableDictionary.CreateBuilder<string, ISymbol>();
        var parameterToNewFieldMap = ImmutableDictionary.CreateBuilder<string, string>();
        var parameterToNewPropertyMap = ImmutableDictionary.CreateBuilder<string, string>();

        var unavailableMemberNames = GetUnavailableMemberNames(typeToGenerateIn).ToImmutableArray();

        using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var parameters);

        for (var i = 0; i < parameterNames.Length; i++)
        {
            var parameterName = parameterNames[i];
            var parameterType = parameterTypes[i];
            var argument = arguments[i];

            // See if there's a matching field or property we can use, or create a new member otherwise.
            parameterName = FindExistingOrCreateNewMember(parameterName, parameterType, argument);

            parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default,
                refKind: argument.RefKind,
                isParams: false,
                type: parameterType,
                name: parameterName.BestNameForParameter));
        }

        return (parameters.ToImmutable(),
            parameterToExistingMemberMap.ToImmutable(),
            parameterToNewFieldMap.ToImmutable(),
            parameterToNewPropertyMap.ToImmutable());

        ParameterName FindExistingOrCreateNewMember(
            ParameterName parameterName,
            ITypeSymbol parameterType,
            Argument<TExpressionSyntax> argument)
        {
            var isFixed = argument.IsNamed;

            var symbol = TryFindMatchingMember(parameterName);
            if (symbol != null)
            {
                if (IsViableFieldOrProperty(document, parameterType, symbol))
                {
                    // Ok!  We can just the existing field.  
                    parameterToExistingMemberMap[parameterName.BestNameForParameter] = symbol;
                }
                else
                {
                    // Uh-oh.  Now we have a problem.  We can't assign this parameter to
                    // this field.  So we need to create a new field.  Find a name not in
                    // use so we can assign to that.  
                    var baseName = GenerateNameForArgument(document, argument, cancellationToken);

                    var baseFieldWithNamingStyle = fieldNamingRule.NamingStyle.MakeCompliant(baseName).First();
                    var basePropertyWithNamingStyle = propertyNamingRule.NamingStyle.MakeCompliant(baseName).First();

                    var newFieldName = NameGenerator.EnsureUniqueness(baseFieldWithNamingStyle, unavailableMemberNames.Concat(parameterToNewFieldMap.Values));
                    var newPropertyName = NameGenerator.EnsureUniqueness(basePropertyWithNamingStyle, unavailableMemberNames.Concat(parameterToNewPropertyMap.Values));

                    if (isFixed)
                    {
                        // Can't change the parameter name, so map the existing parameter
                        // name to the new field name.
                        parameterToNewFieldMap[parameterName.NameBasedOnArgument] = newFieldName;
                        parameterToNewPropertyMap[parameterName.NameBasedOnArgument] = newPropertyName;
                    }
                    else
                    {
                        // Can change the parameter name, so do so.  
                        // But first remove any prefix added due to field naming styles
                        var fieldNameMinusPrefix = newFieldName[fieldNamingRule.NamingStyle.Prefix.Length..];
                        var newParameterName = new ParameterName(fieldNameMinusPrefix, isFixed: false, parameterNamingRule);
                        parameterName = newParameterName;

                        parameterToNewFieldMap[newParameterName.BestNameForParameter] = newFieldName;
                        parameterToNewPropertyMap[newParameterName.BestNameForParameter] = newPropertyName;
                    }
                }
            }
            else
            {
                // If no matching field was found, use the fieldNamingRule to create suitable name
                var bestNameForParameter = parameterName.BestNameForParameter;
                var nameBasedOnArgument = parameterName.NameBasedOnArgument;
                parameterToNewFieldMap[bestNameForParameter] = fieldNamingRule.NamingStyle.MakeCompliant(nameBasedOnArgument).First();
                parameterToNewPropertyMap[bestNameForParameter] = propertyNamingRule.NamingStyle.MakeCompliant(nameBasedOnArgument).First();
            }

            return parameterName;
        }

        ISymbol? TryFindMatchingMember(ParameterName parameterName)
        {
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameterName.NameBasedOnArgument);

            // Try with the explicit field naming rule first, so that we def match with a field that matches the user's
            // chosen naming settings.
            var symbol = TryFindMemberWithRule(fieldNamingRule, parameterNameParts);
            if (symbol != null)
                return symbol;

            // But if that fails, fall back to other common ways of naming fields (for example, with or without an
            // underscore prefix), to see if we can find a match that way.
            foreach (var rule in rules)
            {
                symbol = TryFindMemberWithRule(rule, parameterNameParts);
                if (symbol != null)
                    return symbol;
            }

            return null;
        }

        ISymbol? TryFindMemberWithRule(NamingRule rule, IdentifierNameParts parameterNameParts)
        {
            var memberName = rule.NamingStyle.CreateName(parameterNameParts.BaseNameParts);

            // For non-out parameters, see if there's already a field there with the same name.
            // If so, and it has a compatible type, then we can just assign to that field.
            // Otherwise, we'll need to choose a different name for this member so that it
            // doesn't conflict with something already in the type. First check the current type
            // for a matching field.  If so, defer to it.

            var members = from t in typeToGenerateIn.GetBaseTypesAndThis()
                          let ignoreAccessibility = t.Equals(typeToGenerateIn)
                          from m in t.GetMembers()
                          where m.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase)
                          where ignoreAccessibility || IsSymbolAccessible(m, document)
                          select m;

            var membersArray = members.ToImmutableArray();

            return membersArray.FirstOrDefault(m => m.Name.Equals(memberName, StringComparison.Ordinal)) ?? membersArray.FirstOrDefault();
        }
    }

    private static string GenerateNameForArgument<TExpressionSyntax>(
        SemanticDocument document, Argument<TExpressionSyntax> argument, CancellationToken cancellationToken)
        where TExpressionSyntax : SyntaxNode
    {
        // If it named argument then we use the name provided.
        if (argument.IsNamed)
            return argument.Name;

        if (argument.Expression is null)
            return ITypeSymbolExtensions.DefaultParameterName;

        var semanticFacts = document.Document.GetRequiredLanguageService<ISemanticFactsService>();
        var name = semanticFacts.GenerateNameForExpression(
            document.SemanticModel, argument.Expression, capitalize: false, cancellationToken);
        return string.IsNullOrEmpty(name) ? ITypeSymbolExtensions.DefaultParameterName : name;
    }

    private static IEnumerable<string> GetUnavailableMemberNames(INamedTypeSymbol typeToGenerateIn)
    {
        Contract.ThrowIfNull(typeToGenerateIn);

        return typeToGenerateIn.MemberNames.Concat(
            from type in typeToGenerateIn.GetBaseTypes()
            from member in type.GetMembers()
            select member.Name);
    }

    private static bool IsSymbolAccessible(ISymbol? symbol, SemanticDocument document)
    {
        if (symbol == null)
        {
            return false;
        }

        if (symbol.Kind == SymbolKind.Property)
        {
            if (!IsSymbolAccessible(((IPropertySymbol)symbol).SetMethod, document))
            {
                return false;
            }
        }

        // Public and protected constructors are accessible.  Internal constructors are
        // accessible if we have friend access.  We can't call the normal accessibility
        // checkers since they will think that a protected constructor isn't accessible
        // (since we don't have the destination type that would have access to them yet).
        switch (symbol.DeclaredAccessibility)
        {
            case Accessibility.ProtectedOrInternal:
            case Accessibility.Protected:
            case Accessibility.Public:
                return true;
            case Accessibility.ProtectedAndInternal:
            case Accessibility.Internal:
                return document.SemanticModel.Compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(
                    symbol.ContainingAssembly);

            default:
                return false;
        }
    }

    private static bool IsViableFieldOrProperty(
        SemanticDocument document,
        ITypeSymbol parameterType,
        ISymbol symbol)
    {
        if (parameterType.Language != symbol.Language)
            return false;

        if (symbol != null && !symbol.IsStatic)
        {
            if (symbol is IFieldSymbol field)
            {
                return
                    !field.IsConst &&
                    IsConversionImplicit(document.SemanticModel.Compilation, parameterType, field.Type);
            }
            else if (symbol is IPropertySymbol property)
            {
                return
                    property.Parameters.Length == 0 &&
                    property.IsWritableInConstructor() &&
                    IsConversionImplicit(document.SemanticModel.Compilation, parameterType, property.Type);
            }
        }

        return false;
    }

    private static bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
        => compilation.ClassifyCommonConversion(sourceType, targetType).IsImplicit;
}
