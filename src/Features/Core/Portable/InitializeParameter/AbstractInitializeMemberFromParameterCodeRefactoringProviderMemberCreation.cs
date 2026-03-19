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
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InitializeParameter;

using static InitializeParameterHelpersCore;

internal abstract partial class AbstractInitializeMemberFromParameterCodeRefactoringProvider<
    TTypeDeclarationSyntax,
    TParameterSyntax,
    TStatementSyntax,
    TExpressionSyntax> : AbstractInitializeParameterCodeRefactoringProvider<
        TTypeDeclarationSyntax,
        TParameterSyntax,
        TStatementSyntax,
        TExpressionSyntax>
    where TTypeDeclarationSyntax : SyntaxNode
    where TParameterSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected abstract Accessibility DetermineDefaultFieldAccessibility(INamedTypeSymbol containingType);
    protected abstract Accessibility DetermineDefaultPropertyAccessibility();
    protected abstract SyntaxNode RemoveThrowNotImplemented(SyntaxNode propertySyntax);

    protected sealed override Task<ImmutableArray<CodeAction>> GetRefactoringsForAllParametersAsync(
        Document document, SyntaxNode functionDeclaration, IMethodSymbol method, IBlockOperation? blockStatementOpt,
        ImmutableArray<SyntaxNode> listOfParameterNodes, TextSpan parameterSpan,
        CancellationToken cancellationToken)
    {
        return SpecializedTasks.EmptyImmutableArray<CodeAction>();
    }

    protected sealed override async Task<ImmutableArray<CodeAction>> GetRefactoringsForSingleParameterAsync(
        Document document,
        TParameterSyntax parameterSyntax,
        IParameterSymbol parameter,
        SyntaxNode constructorDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        CancellationToken cancellationToken)
    {
        // Only supported for constructor parameters.
        if (method.MethodKind != MethodKind.Constructor)
            return [];

        var typeDeclaration = constructorDeclaration.GetAncestor<TTypeDeclarationSyntax>();
        if (typeDeclaration == null)
            return [];

        // See if we're already assigning this parameter to a field/property in this type. If so, there's nothing
        // more for us to do.
        var assignmentStatement = InitializeParameterHelpersCore.TryFindFieldOrPropertyAssignmentStatement(parameter, blockStatement);
        if (assignmentStatement != null)
            return [];

        // Haven't initialized any fields/properties with this parameter.  Offer to assign to an existing matching
        // field/prop if we can find one, or add a new field/prop if we can't.

        var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
        var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
        if (parameterNameParts.BaseName == "")
            return [];

        var (fieldOrProperty, isThrowNotImplementedProperty) = await TryFindMatchingUninitializedFieldOrPropertySymbolAsync(
            document, parameter, blockStatement, rules, parameterNameParts.BaseNameParts, cancellationToken).ConfigureAwait(false);

        if (fieldOrProperty != null)
        {
            return HandleExistingFieldOrProperty(
                document, parameter, constructorDeclaration, blockStatement, fieldOrProperty, isThrowNotImplementedProperty);
        }

        return await HandleNoExistingFieldOrPropertyAsync(
            document, parameter, constructorDeclaration,
            method, blockStatement, rules, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<CodeAction>> HandleNoExistingFieldOrPropertyAsync(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode constructorDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        ImmutableArray<NamingRule> rules,
        CancellationToken cancellationToken)
    {
        // Didn't find a field/prop that this parameter could be assigned to.
        // Offer to create new one and assign to that.
        using var _ = ArrayBuilder<CodeAction>.GetInstance(out var allActions);

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        var (fieldAction, propertyAction) = AddSpecificParameterInitializationActions(
            document, parameter, constructorDeclaration, blockStatement, rules, formattingOptions.AccessibilityModifiersRequired);

        // Check if the surrounding parameters are assigned to another field in this class.  If so, offer to
        // make this parameter into a field as well.  Otherwise, default to generating a property
        var siblingFieldOrProperty = TryFindSiblingFieldOrProperty(parameter, blockStatement);
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

        var (allFieldsAction, allPropertiesAction) = AddAllParameterInitializationActions(
            document, constructorDeclaration, method, blockStatement, rules, formattingOptions.AccessibilityModifiersRequired);

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

        return allActions.ToImmutableAndClear();
    }

    private (CodeAction? fieldAction, CodeAction? propertyAction) AddAllParameterInitializationActions(
        Document document,
        SyntaxNode constructorDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        ImmutableArray<NamingRule> rules,
        AccessibilityModifiersRequired accessibilityModifiersRequired)
    {
        if (blockStatement == null)
            return default;

        var parameters = GetParametersWithoutAssociatedMembers(blockStatement, rules, method);

        if (parameters.Length < 2)
            return default;

        var fields = parameters.SelectAsArray(p => (ISymbol)CreateField(p, accessibilityModifiersRequired, rules));
        var properties = parameters.SelectAsArray(p => (ISymbol)CreateProperty(p, accessibilityModifiersRequired, rules));

        var allFieldsAction = CodeAction.Create(
            FeaturesResources.Create_and_assign_remaining_as_fields,
            c => AddAllSymbolInitializationsAsync(
                document, constructorDeclaration, blockStatement, parameters, fields, c),
            nameof(FeaturesResources.Create_and_assign_remaining_as_fields));
        var allPropertiesAction = CodeAction.Create(
            FeaturesResources.Create_and_assign_remaining_as_properties,
            c => AddAllSymbolInitializationsAsync(
                document, constructorDeclaration, blockStatement, parameters, properties, c),
            nameof(FeaturesResources.Create_and_assign_remaining_as_properties));

        return (allFieldsAction, allPropertiesAction);
    }

    private (CodeAction fieldAction, CodeAction propertyAction) AddSpecificParameterInitializationActions(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode constructorDeclaration,
        IBlockOperation? blockStatement,
        ImmutableArray<NamingRule> rules,
        AccessibilityModifiersRequired accessibilityModifiersRequired)
    {
        var field = CreateField(parameter, accessibilityModifiersRequired, rules);
        var property = CreateProperty(parameter, accessibilityModifiersRequired, rules);

        // we're generating the field or property, so we don't have to handle throwing versions of them.
        var isThrowNotImplementedProperty = false;

        var fieldAction = CodeAction.Create(
            string.Format(FeaturesResources.Create_and_assign_field_0, field.Name),
            cancellationToken => AddSingleSymbolInitializationAsync(document, constructorDeclaration, blockStatement, parameter, field, isThrowNotImplementedProperty, cancellationToken),
            nameof(FeaturesResources.Create_and_assign_field_0) + "_" + field.Name);
        var propertyAction = CodeAction.Create(
            string.Format(FeaturesResources.Create_and_assign_property_0, property.Name),
            cancellationToken => AddSingleSymbolInitializationAsync(document, constructorDeclaration, blockStatement, parameter, property, isThrowNotImplementedProperty, cancellationToken),
            nameof(FeaturesResources.Create_and_assign_property_0) + "_" + property.Name);

        return (fieldAction, propertyAction);
    }

    private static ImmutableArray<IParameterSymbol> GetParametersWithoutAssociatedMembers(
        IBlockOperation? blockStatement,
        ImmutableArray<NamingRule> rules,
        IMethodSymbol method)
    {
        using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var result);

        foreach (var parameter in method.Parameters)
        {
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
            if (parameterNameParts.BaseName == "")
                continue;

            var assignmentOp = InitializeParameterHelpersCore.TryFindFieldOrPropertyAssignmentStatement(parameter, blockStatement);
            if (assignmentOp != null)
                continue;

            result.Add(parameter);
        }

        return result.ToImmutableAndClear();
    }

    private ImmutableArray<CodeAction> HandleExistingFieldOrProperty(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IBlockOperation? blockStatement,
        ISymbol fieldOrProperty,
        bool isThrowNotImplementedProperty)
    {
        // Found a field/property that this parameter should be assigned to.
        // Just offer the simple assignment to it.

        var resource = fieldOrProperty.Kind == SymbolKind.Field
            ? FeaturesResources.Initialize_field_0
            : FeaturesResources.Initialize_property_0;

        var title = string.Format(resource, fieldOrProperty.Name);

        return [CodeAction.Create(
            title,
            cancellationToken => AddSingleSymbolInitializationAsync(
                document, functionDeclaration, blockStatement, parameter, fieldOrProperty, isThrowNotImplementedProperty, cancellationToken),
            title)];
    }

    private static ISymbol? TryFindSiblingFieldOrProperty(
        IParameterSymbol parameter, IBlockOperation? blockStatement)
    {
        foreach (var (siblingParam, _) in GetSiblingParameters(parameter))
        {
            TryFindFieldOrPropertyAssignmentStatement(siblingParam, blockStatement, out var sibling);
            if (sibling != null)
                return sibling;
        }

        return null;
    }

    private IFieldSymbol CreateField(
        IParameterSymbol parameter,
        AccessibilityModifiersRequired accessibilityModifiersRequired,
        ImmutableArray<NamingRule> rules)
    {
        var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules).BaseNameParts;

        foreach (var rule in rules)
        {
            if (rule.SymbolSpecification.AppliesTo(SymbolKind.Field, Accessibility.Private))
            {
                var uniqueName = GenerateUniqueName(parameter, parameterNameParts, rule);

                var accessibilityLevel = Accessibility.Private;
                if (accessibilityModifiersRequired is AccessibilityModifiersRequired.Never or AccessibilityModifiersRequired.OmitIfDefault)
                {
                    var defaultAccessibility = DetermineDefaultFieldAccessibility(parameter.ContainingType);
                    if (defaultAccessibility == Accessibility.Private)
                    {
                        accessibilityLevel = Accessibility.NotApplicable;
                    }
                }

                return CodeGenerationSymbolFactory.CreateFieldSymbol(
                    default,
                    accessibilityLevel,
                    DeclarationModifiers.ReadOnly,
                    parameter.Type, uniqueName);
            }
        }

        // We place a special rule in s_builtInRules that matches all fields.  So we should 
        // always find a matching rule.
        throw ExceptionUtilities.Unreachable();
    }

    private IPropertySymbol CreateProperty(
        IParameterSymbol parameter,
        AccessibilityModifiersRequired accessibilityModifiersRequired,
        ImmutableArray<NamingRule> rules)
    {
        var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules).BaseNameParts;

        foreach (var rule in rules)
        {
            if (rule.SymbolSpecification.AppliesTo(SymbolKind.Property, Accessibility.Public))
            {
                var uniqueName = GenerateUniqueName(parameter, parameterNameParts, rule);

                var accessibilityLevel = Accessibility.Public;
                if (accessibilityModifiersRequired is AccessibilityModifiersRequired.Never or AccessibilityModifiersRequired.OmitIfDefault)
                {
                    var defaultAccessibility = DetermineDefaultPropertyAccessibility();
                    if (defaultAccessibility == Accessibility.Public)
                    {
                        accessibilityLevel = Accessibility.NotApplicable;
                    }
                }

                var getMethod = CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    default,
                    Accessibility.Public,
                    default);

                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                    default,
                    accessibilityLevel,
                    DeclarationModifiers.None,
                    parameter.Type,
                    RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: uniqueName,
                    parameters: default,
                    getMethod: getMethod,
                    setMethod: null);
            }
        }

        // We place a special rule in s_builtInRules that matches all properties.  So we should 
        // always find a matching rule.
        throw ExceptionUtilities.Unreachable();
    }

    private async Task<Solution> AddAllSymbolInitializationsAsync(
        Document document,
        SyntaxNode constructorDeclaration,
        IBlockOperation? blockStatement,
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<ISymbol> fieldsOrProperties,
        CancellationToken cancellationToken)
    {
        Debug.Assert(parameters.Length >= 2);
        Debug.Assert(fieldsOrProperties.Length > 0);
        Debug.Assert(parameters.Length == fieldsOrProperties.Length);

        // Process each param+field/prop in order.  Apply the pair to the document getting the updated document.
        // Then find all the current data in that updated document and move onto the next pair.

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var nodesToTrack = new List<SyntaxNode> { constructorDeclaration };
        if (blockStatement != null)
            nodesToTrack.Add(blockStatement.Syntax);

        var trackedRoot = root.TrackNodes(nodesToTrack);
        var currentSolution = document.WithSyntaxRoot(trackedRoot).Project.Solution;

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var fieldOrProperty = fieldsOrProperties[i];

            var currentDocument = currentSolution.GetRequiredDocument(document.Id);
            var currentSemanticModel = await currentDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var currentCompilation = currentSemanticModel.Compilation;
            var currentRoot = await currentDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var currentConstructorDeclaration = currentRoot.GetCurrentNode(constructorDeclaration);
            if (currentConstructorDeclaration == null)
                continue;

            IBlockOperation? currentBlockStatement = null;
            if (blockStatement != null)
            {
                currentBlockStatement = (IBlockOperation?)currentSemanticModel.GetOperation(currentRoot.GetCurrentNode(blockStatement.Syntax)!, cancellationToken);
                if (currentBlockStatement == null)
                    continue;
            }

            var currentParameter = (IParameterSymbol?)parameter.GetSymbolKey(cancellationToken).Resolve(currentCompilation, cancellationToken: cancellationToken).GetAnySymbol();
            if (currentParameter == null)
                continue;

            // fieldOrProperty is a new member.  So we don't have to track it to this edit we're making.

            currentSolution = await AddSingleSymbolInitializationAsync(
                currentDocument,
                currentConstructorDeclaration,
                currentBlockStatement,
                currentParameter,
                fieldOrProperty,
                isThrowNotImplementedProperty: false,
                cancellationToken).ConfigureAwait(false);
        }

        return currentSolution;
    }

    private async Task<Solution> AddSingleSymbolInitializationAsync(
        Document document,
        SyntaxNode constructorDeclaration,
        IBlockOperation? blockStatement,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        bool isThrowNotImplementedProperty,
        CancellationToken cancellationToken)
    {
        // First, add the field/property to the solution if they're a newly generated member and not a pre-existing one.
        var (documentWithMemberAdded, currentParameter, currentFieldOrProperty) = await AddMissingFieldOrPropertyAsync(
            document, constructorDeclaration, blockStatement, parameter, fieldOrProperty, cancellationToken).ConfigureAwait(false);

        var initializeParameterService = document.GetRequiredLanguageService<IInitializeParameterService>();

        var solutionWithAssignmentAdded = documentWithMemberAdded.Project.Solution;
        if (currentParameter != null && currentFieldOrProperty != null)
        {
            // Now, attempt to assign the parameter to that field/property.
            solutionWithAssignmentAdded = await initializeParameterService.AddAssignmentAsync(
               documentWithMemberAdded, currentParameter, currentFieldOrProperty, cancellationToken).ConfigureAwait(false);
        }

        // If the user had a property that has 'throw NotImplementedException' in it, then remove those throws as the
        // constructor is now initializing the property properly with a value.
        var finalSolution = solutionWithAssignmentAdded;
        if (isThrowNotImplementedProperty && currentFieldOrProperty != null)
        {
            var compilation = await finalSolution.GetRequiredProject(documentWithMemberAdded.Project.Id).GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var finalFieldOrProperty = SymbolFinder.FindSimilarSymbols(currentFieldOrProperty, compilation, cancellationToken).FirstOrDefault();
            if (finalFieldOrProperty != null)
            {
                var declarationService = document.GetRequiredLanguageService<ISymbolDeclarationService>();
                var propertySyntax = await declarationService.GetDeclarations(finalFieldOrProperty)[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                var withoutThrowNotImplemented = RemoveThrowNotImplemented(propertySyntax);

                var otherDocument = finalSolution.GetDocument(propertySyntax.SyntaxTree);
                if (otherDocument != null)
                {
                    var otherRoot = await propertySyntax.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    finalSolution = finalSolution.WithDocumentSyntaxRoot(
                        otherDocument.Id, otherRoot.ReplaceNode(propertySyntax, withoutThrowNotImplemented));
                }
            }
        }

        return finalSolution;
    }

    private static async Task<(Document documentWithMemberAdded, IParameterSymbol? currentParameter, ISymbol? currentFieldOrProperty)> AddMissingFieldOrPropertyAsync(
        Document document,
        SyntaxNode constructorDeclaration,
        IBlockOperation? blockStatement,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        CancellationToken cancellationToken)
    {
        if (fieldOrProperty.ContainingType is not null)
            return (document, parameter, fieldOrProperty);

        var services = document.Project.Solution.Services;
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var editor = new SyntaxEditor(root, services);
        var generator = editor.Generator;
        var options = await document.GetCodeGenerationOptionsAsync(cancellationToken).ConfigureAwait(false);
        var codeGenerator = document.GetRequiredLanguageService<ICodeGenerationService>();

        // We're generating a new field/property.  Place into the containing type,
        // ideally before/after a relevant existing member.
        // First, look for the right containing type (As a type may be partial). 
        // We want the type-block that this constructor is contained within.
        var typeDeclaration = constructorDeclaration.GetAncestor<TTypeDeclarationSyntax>()!;

        // Now add the field/property to this type.  Use the 'ReplaceNode+callback' form
        // so that nodes will be appropriate tracked and so we can then update the constructor
        // below even after we've replaced the whole type with a new type.
        //
        // Note: We'll pass the appropriate options so that the new field/property 
        // is appropriate placed before/after an existing field/property.  We'll try
        // to preserve the same order for fields/properties that we have for the constructor
        // parameters.
        editor.ReplaceNode(
            typeDeclaration,
            (currentTypeDecl, _) =>
            {
                if (fieldOrProperty is IPropertySymbol property)
                {
                    return codeGenerator.AddProperty(
                        currentTypeDecl, property,
                        codeGenerator.GetInfo(GetAddContext<IPropertySymbol>(parameter, blockStatement, typeDeclaration, cancellationToken), options, root.SyntaxTree.Options),
                        cancellationToken);
                }
                else if (fieldOrProperty is IFieldSymbol field)
                {
                    return codeGenerator.AddField(
                        currentTypeDecl, field,
                        codeGenerator.GetInfo(GetAddContext<IFieldSymbol>(parameter, blockStatement, typeDeclaration, cancellationToken), options, root.SyntaxTree.Options),
                        cancellationToken);
                }
                else
                {
                    throw ExceptionUtilities.Unreachable();
                }
            });

        var documentWithMemberAdded = document.WithSyntaxRoot(editor.GetChangedRoot());
        var compilation = await documentWithMemberAdded.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

        var currentParameter = SymbolFinder.FindSimilarSymbols(parameter, compilation, cancellationToken).FirstOrDefault();

        var currentFieldOrProperty = currentParameter?.ContainingType
            .GetMembers(fieldOrProperty.Name)
            .Where(m => m.Kind == fieldOrProperty.Kind)
            .FirstOrDefault();

        return (documentWithMemberAdded, currentParameter, currentFieldOrProperty);
    }

    private static CodeGenerationContext GetAddContext<TSymbol>(
        IParameterSymbol parameter, IBlockOperation? blockStatement,
        SyntaxNode typeDeclaration, CancellationToken cancellationToken)
        where TSymbol : ISymbol
    {
        foreach (var (sibling, before) in GetSiblingParameters(parameter))
        {
            var statement = TryFindFieldOrPropertyAssignmentStatement(
                sibling, blockStatement, out var fieldOrProperty);

            if (statement != null &&
                fieldOrProperty is TSymbol symbol)
            {
                var symbolSyntax = symbol.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                if (symbolSyntax.Ancestors().Contains(typeDeclaration))
                {
                    if (before)
                    {
                        // Found an existing field/property that corresponds to a preceding parameter.
                        // Place ourselves directly after it.
                        return new CodeGenerationContext(afterThisLocation: symbolSyntax.GetLocation());
                    }
                    else
                    {
                        // Found an existing field/property that corresponds to a following parameter.
                        // Place ourselves directly before it.
                        return new CodeGenerationContext(beforeThisLocation: symbolSyntax.GetLocation());
                    }
                }
            }
        }

        return CodeGenerationContext.Default;
    }

    private static IOperation? TryFindFieldOrPropertyAssignmentStatement(
        IParameterSymbol parameter, IBlockOperation? blockStatement, out ISymbol? fieldOrProperty)
    {
        if (blockStatement != null)
        {
            var containingType = parameter.ContainingType;
            foreach (var statement in blockStatement.Operations)
            {
                // look for something of the form:  "this.s = s" or "this.s = s ?? ..."
                if (IsFieldOrPropertyAssignment(statement, containingType, out var assignmentExpression, out fieldOrProperty) &&
                    IsParameterReferenceOrCoalesceOfParameterReference(assignmentExpression, parameter))
                {
                    return statement;
                }

                // look inside the form `(this.s, this.t) = (s, t)`
                if (TryGetPartsOfTupleAssignmentOperation(statement, out var targetTuple, out var valueTuple))
                {
                    for (int i = 0, n = targetTuple.Elements.Length; i < n; i++)
                    {
                        var target = targetTuple.Elements[i];
                        var value = valueTuple.Elements[i];

                        if (IsFieldOrPropertyReference(target, containingType, out fieldOrProperty) &&
                            IsParameterReference(value, parameter))
                        {
                            return statement;
                        }
                    }
                }
            }
        }

        fieldOrProperty = null;
        return null;
    }

    private static bool IsParameterReferenceOrCoalesceOfParameterReference(
        IAssignmentOperation assignmentExpression, IParameterSymbol parameter)
        => InitializeParameterHelpersCore.IsParameterReferenceOrCoalesceOfParameterReference(assignmentExpression.Value, parameter);

    private async Task<(ISymbol?, bool isThrowNotImplementedProperty)> TryFindMatchingUninitializedFieldOrPropertySymbolAsync(
        Document document, IParameterSymbol parameter, IBlockOperation? blockStatement, ImmutableArray<NamingRule> rules, ImmutableArray<string> parameterWords, CancellationToken cancellationToken)
    {
        // Look for a field/property that really looks like it corresponds to this parameter.
        // Use a variety of heuristics around the name/type to see if this is a match.

        var containingType = parameter.ContainingType;
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var initializeParameterService = document.GetRequiredLanguageService<IInitializeParameterService>();

        // Walk through the naming rules against this parameter's name to see what name the user would like for it as a
        // member in this type.  Note that we have some fallback rules that use the standard conventions around
        // properties /fields so that can still find things even if the user has no naming preferences set.

        foreach (var rule in rules)
        {
            var memberName = rule.NamingStyle.CreateName(parameterWords);
            foreach (var memberWithName in containingType.GetMembers(memberName))
            {
                // We found members in our type with that name.  If it's a writable
                // field that we could assign this parameter to, and it's not already
                // been assigned to, then this field is a good candidate for us to
                // hook up to.
                if (memberWithName is IFieldSymbol field &&
                    !field.IsConst &&
                    IsImplicitConversion(compilation, source: parameter.Type, destination: field.Type) &&
                    !ContainsMemberAssignment(blockStatement, field))
                {
                    return (field, isThrowNotImplementedProperty: false);
                }

                // If it's a writable property that we could assign this parameter to, and it's
                // not already been assigned to, then this property is a good candidate for us to
                // hook up to.
                if (memberWithName is IPropertySymbol property &&
                    IsImplicitConversion(compilation, source: parameter.Type, destination: property.Type) &&
                    !ContainsMemberAssignment(blockStatement, property))
                {
                    // We also allow assigning into a property of the form `=> throw new NotImplementedException()`.
                    // That way users can easily spit out those methods, but then convert them to be normal
                    // properties with ease.
                    if (initializeParameterService.IsThrowNotImplementedProperty(compilation, property, cancellationToken))
                        return (property, isThrowNotImplementedProperty: true);

                    if (property.IsWritableInConstructor())
                        return (property, isThrowNotImplementedProperty: false);
                }
            }
        }

        // Couldn't find any existing member.  Just return nothing so we can offer to
        // create a member for them.
        return default;

        static bool ContainsMemberAssignment(IBlockOperation? blockStatement, ISymbol member)
        {
            if (blockStatement != null)
            {
                foreach (var statement in blockStatement.Operations)
                {
                    if (IsFieldOrPropertyAssignment(statement, member.ContainingType, out var assignmentExpression) &&
                        assignmentExpression.Target.UnwrapImplicitConversion() is IMemberReferenceOperation memberReference &&
                        member.Equals(memberReference.Member))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
