// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter;

using static InitializeParameterHelpers;
using static InitializeParameterHelpersCore;
using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InitializeMemberFromPrimaryConstructorParameter), Shared]
internal sealed partial class CSharpInitializeMemberFromPrimaryConstructorParameterCodeRefactoringProvider
    : CodeRefactoringProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpInitializeMemberFromPrimaryConstructorParameterCodeRefactoringProvider()
    {
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;

        var selectedParameter = await context.TryGetRelevantNodeAsync<ParameterSyntax>().ConfigureAwait(false);
        if (selectedParameter == null)
            return;

        if (selectedParameter.Parent is not ParameterListSyntax { Parent: TypeDeclarationSyntax(kind: SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration) typeDeclaration })
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var parameter = semanticModel.GetRequiredDeclaredSymbol(selectedParameter, cancellationToken);
        if (parameter?.Name is null or "")
            return;

        if (parameter.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.Constructor } constructor)
            return;

        // See if we're already assigning this parameter to a field/property in this type. If so, there's nothing
        // more for us to do.
        var compilation = semanticModel.Compilation;
        var (initializerValue, _) = TryFindFieldOrPropertyInitializerValue(compilation, parameter, cancellationToken);
        if (initializerValue != null)
            return;

        // Haven't initialized any fields/properties with this parameter.  Offer to assign to an existing matching
        // field/prop if we can find one, or add a new field/prop if we can't.
        var fallbackOptions = context.Options;
        var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
        var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
        if (parameterNameParts.BaseName == "")
            return;

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        var fieldOrProperty = TryFindMatchingUninitializedFieldOrPropertySymbol();
        var refactorings = fieldOrProperty == null
            ? HandleNoExistingFieldOrProperty()
            : HandleExistingFieldOrProperty();

        context.RegisterRefactorings(refactorings.ToImmutableArray(), context.Span);
        return;

        ISymbol? TryFindMatchingUninitializedFieldOrPropertySymbol()
        {
            // Look for a field/property that really looks like it corresponds to this parameter. Use a variety of
            // heuristics around the name/type to see if this is a match.

            var parameterWords = parameterNameParts.BaseNameParts;
            var containingType = parameter.ContainingType;

            // Walk through the naming rules against this parameter's name to see what name the user would like for
            // it as a member in this type.  Note that we have some fallback rules that use the standard conventions
            // around properties /fields so that can still find things even if the user has no naming preferences
            // set.
            foreach (var rule in rules)
            {
                var memberName = rule.NamingStyle.CreateName(parameterWords);
                foreach (var memberWithName in containingType.GetMembers(memberName))
                {
                    // We found members in our type with that name.  If it's a writable field that we could assign
                    // this parameter to, and it's not already been assigned to, then this field is a good candidate
                    // for us to hook up to.
                    if (memberWithName is IFieldSymbol { IsConst: false, DeclaringSyntaxReferences: [var syntaxRef1, ..] } field &&
                        IsImplicitConversion(compilation, source: parameter.Type, destination: field.Type) &&
                        syntaxRef1.GetSyntax(cancellationToken) is VariableDeclaratorSyntax { Initializer: null })
                    {
                        return field;
                    }

                    // If it's a writable property that we could assign this parameter to, and it's not already been
                    // assigned to, then this property is a good candidate for us to hook up to.
                    if (memberWithName is IPropertySymbol { DeclaringSyntaxReferences: [var syntaxRef2, ..] } property &&
                        IsImplicitConversion(compilation, source: parameter.Type, destination: property.Type) &&
                        syntaxRef2.GetSyntax(cancellationToken) is PropertyDeclarationSyntax { Initializer: null })
                    {
                        // We also allow assigning into a property of the form `=> throw new
                        // NotImplementedException()`. That way users can easily spit out those methods, but then
                        // convert them to be normal properties with ease.
                        if (IsThrowNotImplementedProperty(compilation, property, cancellationToken))
                            return property;

                        if (property.IsWritableInConstructor())
                            return property;
                    }
                }
            }

            // Couldn't find any existing member.  Just return nothing so we can offer to create a member for them.
            return null;
        }

        static CodeAction CreateCodeAction(string title, Func<CancellationToken, Task<Solution>> createSolution)
            => CodeAction.Create(title, createSolution, title);

        IEnumerable<CodeAction> HandleExistingFieldOrProperty()
        {
            // Found a field/property that this parameter should be assigned to. Just offer the simple assignment to it.
            yield return CreateCodeAction(
                string.Format(fieldOrProperty.Kind == SymbolKind.Field ? FeaturesResources.Initialize_field_0 : FeaturesResources.Initialize_property_0, fieldOrProperty.Name),
                cancellationToken => UpdateExistingMemberAsync(document, parameter, fieldOrProperty, cancellationToken));
        }

        IEnumerable<CodeAction> HandleNoExistingFieldOrProperty()
        {
            // Didn't find a field/prop that this parameter could be assigned to. Offer to create new one and assign to that.

            // Check if the surrounding parameters are assigned to another field in this class.  If so, offer to make
            // this parameter into a field as well.  Otherwise, default to generating a property
            var siblingFieldOrProperty = TryFindSiblingFieldOrProperty();

            var field = CreateField(parameter);
            var property = CreateProperty(parameter);

            var fieldAction = CreateCodeAction(
                string.Format(FeaturesResources.Create_and_assign_field_0, field.Name),
                cancellationToken => AddMultipleMembersAsync(document, typeDeclaration, [parameter], [field], fallbackOptions, cancellationToken));
            var propertyAction = CreateCodeAction(
                string.Format(FeaturesResources.Create_and_assign_property_0, property.Name),
                cancellationToken => AddMultipleMembersAsync(document, typeDeclaration, [parameter], [property], fallbackOptions, cancellationToken));

            yield return siblingFieldOrProperty is IFieldSymbol ? fieldAction : propertyAction;
            yield return siblingFieldOrProperty is IFieldSymbol ? propertyAction : fieldAction;

            var parameters = GetParametersWithoutAssociatedMembers();
            if (parameters.Length >= 2)
            {
                var allFieldsAction = CodeAction.Create(
                    FeaturesResources.Create_and_assign_remaining_as_fields,
                    cancellationToken => AddMultipleMembersAsync(document, typeDeclaration, parameters, parameters.SelectAsArray(CreateField), fallbackOptions, cancellationToken));
                var allPropertiesAction = CodeAction.Create(
                    FeaturesResources.Create_and_assign_remaining_as_properties,
                    cancellationToken => AddMultipleMembersAsync(document, typeDeclaration, parameters, parameters.SelectAsArray(CreateProperty), fallbackOptions, cancellationToken));

                yield return siblingFieldOrProperty is IFieldSymbol ? allFieldsAction : allPropertiesAction;
                yield return siblingFieldOrProperty is IFieldSymbol ? allPropertiesAction : allFieldsAction;
            }
        }

        ISymbol? TryFindSiblingFieldOrProperty()
        {
            foreach (var (siblingParam, _) in InitializeParameterHelpersCore.GetSiblingParameters(parameter))
            {
                var (_, sibling) = TryFindFieldOrPropertyInitializerValue(compilation, siblingParam, cancellationToken);
                if (sibling != null)
                    return sibling;
            }

            return null;
        }

        ImmutableArray<IParameterSymbol> GetParametersWithoutAssociatedMembers()
        {
            using var result = TemporaryArray<IParameterSymbol>.Empty;

            foreach (var parameter in constructor.Parameters)
            {
                var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
                if (parameterNameParts.BaseName == "")
                    continue;

                var (assignmentOp, _) = TryFindFieldOrPropertyInitializerValue(compilation, parameter, cancellationToken);
                if (assignmentOp != null)
                    continue;

                result.Add(parameter);
            }

            return result.ToImmutableAndClear();
        }

        ISymbol CreateField(IParameterSymbol parameter)
        {
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules).BaseNameParts;
            var accessibilityLevel = formattingOptions.AccessibilityModifiersRequired is AccessibilityModifiersRequired.Never or AccessibilityModifiersRequired.OmitIfDefault
                ? Accessibility.NotApplicable
                : Accessibility.Private;

            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(SymbolKind.Field, Accessibility.Private))
                {
                    return CodeGenerationSymbolFactory.CreateFieldSymbol(
                        attributes: default,
                        accessibilityLevel,
                        DeclarationModifiers.ReadOnly,
                        parameter.Type,
                        name: GenerateUniqueName(parameter, parameterNameParts, rule),
                        initializer: IdentifierName(parameter.Name.EscapeIdentifier()));
                }
            }

            // We place a special rule in s_builtInRules that matches all fields.  So we should 
            // always find a matching rule.
            throw ExceptionUtilities.Unreachable();
        }

        ISymbol CreateProperty(IParameterSymbol parameter)
        {
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules).BaseNameParts;

            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(SymbolKind.Property, Accessibility.Public))
                {
                    return CodeGenerationSymbolFactory.CreatePropertySymbol(
                        containingType: null,
                        attributes: default,
                        Accessibility.Public,
                        modifiers: default,
                        parameter.Type,
                        RefKind.None,
                        explicitInterfaceImplementations: default,
                        name: GenerateUniqueName(parameter, parameterNameParts, rule),
                        parameters: default,
                        getMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(
                            attributes: default,
                            Accessibility.Public,
                            statements: default),
                        setMethod: null,
                        initializer: IdentifierName(parameter.Name.EscapeIdentifier()));
                }
            }

            // We place a special rule in s_builtInRules that matches all properties.  So we should 
            // always find a matching rule.
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static (IOperation? initializer, ISymbol? fieldOrProperty) TryFindFieldOrPropertyInitializerValue(
        Compilation compilation,
        IParameterSymbol parameter,
        CancellationToken cancellationToken)
    {
        foreach (var group in parameter.ContainingType.DeclaringSyntaxReferences.GroupBy(r => r.SyntaxTree))
        {
            var semanticModel = compilation.GetSemanticModel(group.Key);
            foreach (var syntaxReference in group)
            {
                if (syntaxReference.GetSyntax(cancellationToken) is TypeDeclarationSyntax typeDeclaration)
                {
                    foreach (var member in typeDeclaration.Members)
                    {
                        if (member is PropertyDeclarationSyntax { Initializer.Value: var propertyInitializer } propertyDeclaration)
                        {
                            var operation = semanticModel.GetOperation(propertyInitializer, cancellationToken);
                            if (IsParameterReferenceOrCoalesceOfParameterReference(operation, parameter))
                                return (operation, semanticModel.GetRequiredDeclaredSymbol(propertyDeclaration, cancellationToken));
                        }
                        else if (member is FieldDeclarationSyntax field)
                        {
                            foreach (var varDecl in field.Declaration.Variables)
                            {
                                if (varDecl is { Initializer.Value: var fieldInitializer })
                                {
                                    var operation = semanticModel.GetOperation(fieldInitializer, cancellationToken);
                                    if (IsParameterReferenceOrCoalesceOfParameterReference(operation, parameter))
                                        return (operation, semanticModel.GetRequiredDeclaredSymbol(varDecl, cancellationToken));
                                }
                            }
                        }
                    }
                }
            }
        }

        return default;
    }
}
