// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    using static InitializeParameterHelpers;
    using static InitializeParameterHelpersCore;
    using static SyntaxFactory;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InitializeMemberFromPrimaryConstructorParameter), Shared]
    internal sealed partial class CSharpInitializeMemberFromPrimaryConstructorParameterCodeRefactoringProvider : CodeRefactoringProvider
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
            var compilation = semanticModel.Compilation;
            var parameter = (IParameterSymbol)semanticModel.GetRequiredDeclaredSymbol(selectedParameter, cancellationToken);
            if (parameter?.Name is null or "")
                return;

            if (parameter.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.Constructor } constructor)
                return;

            // See if we're already assigning this parameter to a field/property in this type. If so, there's nothing
            // more for us to do.
            var initializerValue = TryFindFieldOrPropertyInitializerValue(compilation, parameter, out _, cancellationToken);
            if (initializerValue != null)
                return;

            // Haven't initialized any fields/properties with this parameter.  Offer to assign to an existing matching
            // field/prop if we can find one, or add a new field/prop if we can't.
            var fallbackOptions = context.Options;
            var rules = await document.GetNamingRulesAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
            if (parameterNameParts.BaseName == "")
                return;

            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);

            var (fieldOrProperty, isThrowNotImplementedProperty) = TryFindMatchingUninitializedFieldOrPropertySymbol();
            var refactorings = fieldOrProperty != null
                ? HandleExistingFieldOrProperty()
                : HandleNoExistingFieldOrProperty();

            context.RegisterRefactorings(refactorings, context.Span);
            return;

            (ISymbol?, bool isThrowNotImplementedProperty) TryFindMatchingUninitializedFieldOrPropertySymbol()
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
                        if (memberWithName is IFieldSymbol field &&
                            !field.IsConst &&
                            InitializeParameterHelpers.IsImplicitConversion(compilation, source: parameter.Type, destination: field.Type) &&
                            field.DeclaringSyntaxReferences is [var syntaxRef1, ..] &&
                            syntaxRef1.GetSyntax(cancellationToken) is VariableDeclaratorSyntax { Initializer: null })
                        {
                            return (field, isThrowNotImplementedProperty: false);
                        }

                        // If it's a writable property that we could assign this parameter to, and it's not already been
                        // assigned to, then this property is a good candidate for us to hook up to.
                        if (memberWithName is IPropertySymbol property &&
                            InitializeParameterHelpers.IsImplicitConversion(compilation, source: parameter.Type, destination: property.Type) &&
                            property.DeclaringSyntaxReferences is [var syntaxRef2, ..] &&
                            syntaxRef2.GetSyntax(cancellationToken) is PropertyDeclarationSyntax { Initializer: null })
                        {
                            // We also allow assigning into a property of the form `=> throw new
                            // NotImplementedException()`. That way users can easily spit out those methods, but then
                            // convert them to be normal properties with ease.
                            if (IsThrowNotImplementedProperty(compilation, property, cancellationToken))
                                return (property, isThrowNotImplementedProperty: true);

                            if (property.IsWritableInConstructor())
                                return (property, isThrowNotImplementedProperty: false);
                        }
                    }
                }

                // Couldn't find any existing member.  Just return nothing so we can offer to create a member for them.
                return default;
            }

            ImmutableArray<CodeAction> HandleExistingFieldOrProperty()
            {
                // Found a field/property that this parameter should be assigned to. Just offer the simple assignment to it.
                var title = string.Format(fieldOrProperty.Kind == SymbolKind.Field
                    ? FeaturesResources.Initialize_field_0
                    : FeaturesResources.Initialize_property_0, fieldOrProperty.Name);

                return ImmutableArray.Create(CodeAction.Create(
                    title,
                    cancellationToken => AddSingleSymbolInitializationAsync(
                        document, typeDeclaration, parameter, fieldOrProperty, isThrowNotImplementedProperty, fallbackOptions, cancellationToken),
                    title));
            }

            ImmutableArray<CodeAction> HandleNoExistingFieldOrProperty()
            {
                // Didn't find a field/prop that this parameter could be assigned to.
                // Offer to create new one and assign to that.
                using var _ = ArrayBuilder<CodeAction>.GetInstance(out var allActions);

                // Check if the surrounding parameters are assigned to another field in this class.  If so, offer to
                // make this parameter into a field as well.  Otherwise, default to generating a property
                var siblingFieldOrProperty = TryFindSiblingFieldOrProperty();
                var (fieldAction, propertyAction) = AddSpecificParameterInitializationActions();

                if (siblingFieldOrProperty is IFieldSymbol)
                {
                    allActions.Add(fieldAction);
                    allActions.Add(propertyAction);
                }
                else
                {
                    allActions.Add(propertyAction);
                    allActions.Add(fieldAction);
                }

                var (allFieldsAction, allPropertiesAction) = AddAllParameterInitializationActions();
                if (allFieldsAction != null && allPropertiesAction != null)
                {
                    if (siblingFieldOrProperty is IFieldSymbol)
                    {
                        allActions.Add(allFieldsAction);
                        allActions.Add(allPropertiesAction);
                    }
                    else
                    {
                        allActions.Add(allPropertiesAction);
                        allActions.Add(allFieldsAction);
                    }
                }

                return allActions.ToImmutable();
            }

            ISymbol? TryFindSiblingFieldOrProperty()
            {
                foreach (var (siblingParam, _) in InitializeParameterHelpersCore.GetSiblingParameters(parameter))
                {
                    TryFindFieldOrPropertyInitializerValue(compilation, siblingParam, out var sibling, cancellationToken);
                    if (sibling != null)
                        return sibling;
                }

                return null;
            }

            (CodeAction fieldAction, CodeAction propertyAction) AddSpecificParameterInitializationActions()
            {
                var field = CreateField(parameter);
                var property = CreateProperty(parameter);

                // we're generating the field or property, so we don't have to handle throwing versions of them.
                var isThrowNotImplementedProperty = false;

                var fieldAction = CodeAction.Create(
                    string.Format(FeaturesResources.Create_and_assign_field_0, field.Name),
                    c => AddSingleSymbolInitializationAsync(document, typeDeclaration, parameter, field, isThrowNotImplementedProperty, fallbackOptions, c),
                    nameof(FeaturesResources.Create_and_assign_field_0) + "_" + field.Name);
                var propertyAction = CodeAction.Create(
                    string.Format(FeaturesResources.Create_and_assign_property_0, property.Name),
                    c => AddSingleSymbolInitializationAsync(document, typeDeclaration, parameter, property, isThrowNotImplementedProperty, fallbackOptions, c),
                    nameof(FeaturesResources.Create_and_assign_property_0) + "_" + property.Name);

                return (fieldAction, propertyAction);
            }

            (CodeAction? fieldAction, CodeAction? propertyAction) AddAllParameterInitializationActions()
            {
                var parameters = GetParametersWithoutAssociatedMembers();
                if (parameters.Length < 2)
                    return default;

                var allFieldsAction = CodeAction.Create(
                    FeaturesResources.Create_and_assign_remaining_as_fields,
                    cancellationToken => AddAllSymbolInitializationsAsync(document, typeDeclaration, parameters, parameters.SelectAsArray(CreateField), fallbackOptions, cancellationToken),
                    nameof(FeaturesResources.Create_and_assign_remaining_as_fields));
                var allPropertiesAction = CodeAction.Create(
                    FeaturesResources.Create_and_assign_remaining_as_properties,
                    cancellationToken => AddAllSymbolInitializationsAsync(document, typeDeclaration, parameters, parameters.SelectAsArray(CreateProperty), fallbackOptions, cancellationToken),
                    nameof(FeaturesResources.Create_and_assign_remaining_as_properties));

                return (allFieldsAction, allPropertiesAction);
            }

            ImmutableArray<IParameterSymbol> GetParametersWithoutAssociatedMembers()
            {
                using var _1 = ArrayBuilder<IParameterSymbol>.GetInstance(out var result);

                foreach (var parameter in constructor.Parameters)
                {
                    var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
                    if (parameterNameParts.BaseName == "")
                        continue;

                    var assignmentOp = TryFindFieldOrPropertyInitializerValue(compilation, parameter, out _, cancellationToken);
                    if (assignmentOp != null)
                        continue;

                    result.Add(parameter);
                }

                return result.ToImmutable();
            }

            ISymbol CreateField(IParameterSymbol parameter)
            {
                var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules).BaseNameParts;

                foreach (var rule in rules)
                {
                    if (rule.SymbolSpecification.AppliesTo(SymbolKind.Field, Accessibility.Private))
                    {
                        var uniqueName = GenerateUniqueName(parameter, parameterNameParts, rule);

                        var accessibilityLevel = formattingOptions.AccessibilityModifiersRequired is AccessibilityModifiersRequired.Never or AccessibilityModifiersRequired.OmitIfDefault
                            ? Accessibility.NotApplicable
                            : Accessibility.Private;

                        return CodeGenerationSymbolFactory.CreateFieldSymbol(
                            attributes: default,
                            accessibilityLevel,
                            DeclarationModifiers.ReadOnly,
                            parameter.Type,
                            uniqueName,
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
                        var uniqueName = GenerateUniqueName(parameter, parameterNameParts, rule);

                        var accessibilityLevel = Accessibility.Public;

                        var getMethod = CodeGenerationSymbolFactory.CreateAccessorSymbol(
                            attributes: default,
                            Accessibility.Public,
                            statements: default);

                        return CodeGenerationSymbolFactory.CreatePropertySymbol(
                            containingType: null,
                            attributes: default,
                            accessibilityLevel,
                            new DeclarationModifiers(),
                            parameter.Type,
                            RefKind.None,
                            explicitInterfaceImplementations: default,
                            name: uniqueName,
                            parameters: default,
                            getMethod: getMethod,
                            setMethod: null,
                            initializer: IdentifierName(parameter.Name.EscapeIdentifier()));
                    }
                }

                // We place a special rule in s_builtInRules that matches all properties.  So we should 
                // always find a matching rule.
                throw ExceptionUtilities.Unreachable();
            }
        }

        private static IOperation? TryFindFieldOrPropertyInitializerValue(
            Compilation compilation,
            IParameterSymbol parameter,
            out ISymbol? fieldOrProperty,
            CancellationToken cancellationToken)
        {
            foreach (var syntaxReference in parameter.ContainingType.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax(cancellationToken) is TypeDeclarationSyntax typeDeclaration)
                {
                    var semanticModel = compilation.GetSemanticModel(syntaxReference.SyntaxTree);

                    foreach (var member in typeDeclaration.Members)
                    {
                        if (member is PropertyDeclarationSyntax { Initializer.Value: var propertyInitializer } propertyDeclaration)
                        {
                            var operation = semanticModel.GetOperation(propertyInitializer, cancellationToken);
                            if (IsParameterReferenceOrCoalesceOfParameterReference(operation, parameter))
                            {
                                fieldOrProperty = semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken);
                                return operation;
                            }
                        }
                        else if (member is FieldDeclarationSyntax field)
                        {
                            foreach (var varDecl in field.Declaration.Variables)
                            {
                                if (varDecl is { Initializer.Value: var fieldInitializer })
                                {
                                    var operation = semanticModel.GetOperation(fieldInitializer, cancellationToken);
                                    if (IsParameterReferenceOrCoalesceOfParameterReference(operation, parameter))
                                    {
                                        fieldOrProperty = semanticModel.GetDeclaredSymbol(varDecl, cancellationToken);
                                        return operation;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            fieldOrProperty = null;
            return null;
        }
    }
}
