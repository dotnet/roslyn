// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateFromMembers;

internal abstract partial class AbstractGenerateFromMembersCodeRefactoringProvider : CodeRefactoringProvider
{
    protected AbstractGenerateFromMembersCodeRefactoringProvider()
    {
    }

    protected static async Task<SelectedMemberInfo?> GetSelectedMemberInfoAsync(
        Document document, TextSpan textSpan, bool allowPartialSelection, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var selectedDeclarations = await syntaxFacts.GetSelectedFieldsAndPropertiesAsync(
            tree, textSpan, allowPartialSelection, cancellationToken).ConfigureAwait(false);

        if (selectedDeclarations.Length > 0)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var selectedMembers = selectedDeclarations.Select(
                d => semanticModel.GetDeclaredSymbol(d, cancellationToken)).WhereNotNull().ToImmutableArray();
            if (selectedMembers.Length > 0)
            {
                var containingType = selectedMembers.First().ContainingType;
                if (containingType != null)
                {
                    return new SelectedMemberInfo(containingType, selectedDeclarations, selectedMembers);
                }
            }
        }

        return null;
    }

    protected static bool IsReadableInstanceFieldOrProperty(ISymbol symbol)
        => !symbol.IsStatic && IsReadableFieldOrProperty(symbol);

    protected static bool IsWritableInstanceFieldOrProperty(ISymbol symbol)
        => !symbol.IsStatic && IsWritableFieldOrProperty(symbol);

    private static bool IsReadableFieldOrProperty(ISymbol symbol)
        => symbol switch
        {
            IFieldSymbol field => IsViableField(field),
            IPropertySymbol property => IsViableProperty(property) && !property.IsWriteOnly,
            _ => false,
        };

    private static bool IsWritableFieldOrProperty(ISymbol symbol)
        => symbol switch
        {
            // Can use non const fields and properties with setters in them.
            IFieldSymbol field => IsViableField(field) && !field.IsConst,
            IPropertySymbol property => IsViableProperty(property) && property.IsWritableInConstructor(),
            _ => false,
        };

    private static bool IsViableField(IFieldSymbol field)
        => field.AssociatedSymbol == null;

    private static bool IsViableProperty(IPropertySymbol property)
        => property.Parameters.IsEmpty;

    /// <summary>
    /// Returns an array of parameter symbols that correspond to selected member symbols.
    /// If a selected member symbol has an empty base identifier name, the parameter symbol will not be added.
    /// </summary>
    /// <param name="selectedMembers"></param>
    /// <param name="rules"></param>
    /// <returns></returns>
    protected static ImmutableArray<IParameterSymbol> DetermineParameters(
        ImmutableArray<ISymbol> selectedMembers, ImmutableArray<NamingRule> rules)
    {
        using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var parameters);

        foreach (var symbol in selectedMembers)
        {
            var type = symbol.GetMemberType();
            if (type == null)
                continue;

            var identifierNameParts = IdentifierNameParts.CreateIdentifierNameParts(symbol, rules);
            if (identifierNameParts.BaseName == "")
            {
                continue;
            }

            var parameterNamingRule = rules.Where(rule => rule.SymbolSpecification.AppliesTo(SymbolKind.Parameter, Accessibility.NotApplicable)).First();
            var parameterName = parameterNamingRule.NamingStyle.MakeCompliant(identifierNameParts.BaseName).First();

            parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default,
                refKind: RefKind.None,
                isParams: false,
                type: type,
                name: parameterName));
        }

        return parameters.ToImmutable();
    }

    protected static readonly SymbolDisplayFormat SimpleFormat =
        new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
}
