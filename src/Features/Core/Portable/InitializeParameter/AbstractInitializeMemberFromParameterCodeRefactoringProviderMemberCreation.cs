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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InitializeParameter
{
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
        protected abstract SyntaxNode? TryGetLastStatement(IBlockOperation? blockStatementOpt);
        protected abstract Accessibility DetermineDefaultFieldAccessibility(INamedTypeSymbol containingType);
        protected abstract Accessibility DetermineDefaultPropertyAccessibility();

        protected override bool SupportsRecords(ParseOptions options)
            => false;

        protected override Task<ImmutableArray<CodeAction>> GetRefactoringsForAllParametersAsync(
            Document document, SyntaxNode functionDeclaration, IMethodSymbol method, IBlockOperation? blockStatementOpt,
            ImmutableArray<SyntaxNode> listOfParameterNodes, TextSpan parameterSpan, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<CodeAction>();
        }

        protected override async Task<ImmutableArray<CodeAction>> GetRefactoringsForSingleParameterAsync(
            Document document,
            TParameterSyntax parameterSyntax,
            IParameterSymbol parameter,
            SyntaxNode constructorDeclaration,
            IMethodSymbol method,
            IBlockOperation? blockStatementOpt,
            CodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            // Only supported for constructor parameters.
            if (method.MethodKind != MethodKind.Constructor)
                return ImmutableArray<CodeAction>.Empty;

            var typeDeclaration = constructorDeclaration.GetAncestor<TTypeDeclarationSyntax>();
            if (typeDeclaration == null)
                return ImmutableArray<CodeAction>.Empty;

            // See if we're already assigning this parameter to a field/property in this type. If so, there's nothing
            // more for us to do.
            var assignmentStatement = TryFindFieldOrPropertyAssignmentStatement(parameter, blockStatementOpt);
            if (assignmentStatement != null)
                return ImmutableArray<CodeAction>.Empty;

            // Haven't initialized any fields/properties with this parameter.  Offer to assign
            // to an existing matching field/prop if we can find one, or add a new field/prop
            // if we can't.

            var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
            if (parameterNameParts.BaseName == "")
                return ImmutableArray<CodeAction>.Empty;

            var fieldOrProperty = await TryFindMatchingUninitializedFieldOrPropertySymbolAsync(
                document, parameter, blockStatementOpt, rules, parameterNameParts.BaseNameParts, cancellationToken).ConfigureAwait(false);

            if (fieldOrProperty != null)
            {
                return HandleExistingFieldOrProperty(
                    document, parameter, constructorDeclaration,
                    blockStatementOpt, fieldOrProperty, fallbackOptions);
            }

            return await HandleNoExistingFieldOrPropertyAsync(
                document, parameter, constructorDeclaration,
                method, blockStatementOpt, rules, fallbackOptions, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<CodeAction>> HandleNoExistingFieldOrPropertyAsync(
            Document document,
            IParameterSymbol parameter,
            SyntaxNode constructorDeclaration,
            IMethodSymbol method,
            IBlockOperation? blockStatementOpt,
            ImmutableArray<NamingRule> rules,
            CodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            // Didn't find a field/prop that this parameter could be assigned to.
            // Offer to create new one and assign to that.
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var allActions);

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var (fieldAction, propertyAction) = AddSpecificParameterInitializationActions(
                document, parameter, constructorDeclaration, blockStatementOpt, rules, options, fallbackOptions);

            // Check if the surrounding parameters are assigned to another field in this class.  If so, offer to
            // make this parameter into a field as well.  Otherwise, default to generating a property
            var siblingFieldOrProperty = TryFindSiblingFieldOrProperty(parameter, blockStatementOpt);
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
                document, constructorDeclaration, method, blockStatementOpt, rules, options, fallbackOptions);

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

        private (CodeAction? fieldAction, CodeAction? propertyAction) AddAllParameterInitializationActions(
            Document document,
            SyntaxNode constructorDeclaration,
            IMethodSymbol method,
            IBlockOperation? blockStatementOpt,
            ImmutableArray<NamingRule> rules,
            DocumentOptionSet options,
            CodeGenerationOptionsProvider fallbackOptions)
        {
            if (blockStatementOpt == null)
                return default;

            var parameters = GetParametersWithoutAssociatedMembers(blockStatementOpt, rules, method);

            if (parameters.Length < 2)
                return default;

            var fields = parameters.SelectAsArray(p => (ISymbol)CreateField(p, options, rules));
            var properties = parameters.SelectAsArray(p => (ISymbol)CreateProperty(p, options, rules));

            var allFieldsAction = CodeAction.Create(
                FeaturesResources.Create_and_assign_remaining_as_fields,
                c => AddAllSymbolInitializationsAsync(
                    document, constructorDeclaration, blockStatementOpt, parameters, fields, fallbackOptions, c),
                nameof(FeaturesResources.Create_and_assign_remaining_as_fields));
            var allPropertiesAction = CodeAction.Create(
                FeaturesResources.Create_and_assign_remaining_as_properties,
                c => AddAllSymbolInitializationsAsync(
                    document, constructorDeclaration, blockStatementOpt, parameters, properties, fallbackOptions, c),
                nameof(FeaturesResources.Create_and_assign_remaining_as_properties));

            return (allFieldsAction, allPropertiesAction);
        }

        private (CodeAction fieldAction, CodeAction propertyAction) AddSpecificParameterInitializationActions(
            Document document,
            IParameterSymbol parameter,
            SyntaxNode constructorDeclaration,
            IBlockOperation? blockStatementOpt,
            ImmutableArray<NamingRule> rules,
            DocumentOptionSet options,
            CodeGenerationOptionsProvider fallbackOptions)
        {
            var field = CreateField(parameter, options, rules);
            var property = CreateProperty(parameter, options, rules);
            var fieldAction = CodeAction.Create(
                string.Format(FeaturesResources.Create_and_assign_field_0, field.Name),
                c => AddSingleSymbolInitializationAsync(document, constructorDeclaration, blockStatementOpt, parameter, field, fallbackOptions, c),
                nameof(FeaturesResources.Create_and_assign_field_0) + "_" + field.Name);
            var propertyAction = CodeAction.Create(
                string.Format(FeaturesResources.Create_and_assign_property_0, property.Name),
                c => AddSingleSymbolInitializationAsync(document, constructorDeclaration, blockStatementOpt, parameter, property, fallbackOptions, c),
                nameof(FeaturesResources.Create_and_assign_property_0) + "_" + property.Name);

            return (fieldAction, propertyAction);
        }

        private static ImmutableArray<IParameterSymbol> GetParametersWithoutAssociatedMembers(
            IBlockOperation? blockStatementOpt,
            ImmutableArray<NamingRule> rules,
            IMethodSymbol method)
        {
            using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var result);

            foreach (var parameter in method.Parameters)
            {
                var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
                if (parameterNameParts.BaseName == "")
                    continue;

                var assignmentOp = TryFindFieldOrPropertyAssignmentStatement(parameter, blockStatementOpt);
                if (assignmentOp != null)
                    continue;

                result.Add(parameter);
            }

            return result.ToImmutable();
        }

        private ImmutableArray<CodeAction> HandleExistingFieldOrProperty(
            Document document, IParameterSymbol parameter, SyntaxNode functionDeclaration, IBlockOperation? blockStatementOpt, ISymbol fieldOrProperty, CodeGenerationOptionsProvider fallbackOptions)
        {
            // Found a field/property that this parameter should be assigned to.
            // Just offer the simple assignment to it.

            var resource = fieldOrProperty.Kind == SymbolKind.Field
                ? FeaturesResources.Initialize_field_0
                : FeaturesResources.Initialize_property_0;

            var title = string.Format(resource, fieldOrProperty.Name);

            return ImmutableArray.Create(CodeAction.Create(
                title,
                c => AddSingleSymbolInitializationAsync(
                    document, functionDeclaration, blockStatementOpt, parameter, fieldOrProperty, fallbackOptions, c),
                title));
        }

        private static ISymbol? TryFindSiblingFieldOrProperty(IParameterSymbol parameter, IBlockOperation? blockStatementOpt)
        {
            foreach (var (siblingParam, _) in GetSiblingParameters(parameter))
            {
                TryFindFieldOrPropertyAssignmentStatement(siblingParam, blockStatementOpt, out var sibling);
                if (sibling != null)
                    return sibling;
            }

            return null;
        }

        private IFieldSymbol CreateField(
            IParameterSymbol parameter,
            DocumentOptionSet options,
            ImmutableArray<NamingRule> rules)
        {
            var requireAccessibilityModifiers = options.GetOption(CodeStyleOptions2.RequireAccessibilityModifiers);
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules).BaseNameParts;

            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(SymbolKind.Field, Accessibility.Private))
                {
                    var uniqueName = GenerateUniqueName(parameter, parameterNameParts, rule);

                    var accessibilityLevel = Accessibility.Private;
                    if (requireAccessibilityModifiers.Value is AccessibilityModifiersRequired.Never or AccessibilityModifiersRequired.OmitIfDefault)
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
            throw ExceptionUtilities.Unreachable;
        }

        private static string GenerateUniqueName(IParameterSymbol parameter, ImmutableArray<string> parameterNameParts, NamingRule rule)
        {
            // Determine an appropriate name to call the new field.
            var containingType = parameter.ContainingType;
            var baseName = rule.NamingStyle.CreateName(parameterNameParts);

            // Ensure that the name is unique in the containing type so we
            // don't stomp on an existing member.
            var uniqueName = NameGenerator.GenerateUniqueName(
                baseName, n => containingType.GetMembers(n).IsEmpty);
            return uniqueName;
        }

        private IPropertySymbol CreateProperty(
            IParameterSymbol parameter,
            DocumentOptionSet options,
            ImmutableArray<NamingRule> rules)
        {
            var requireAccessibilityModifiers = options.GetOption(CodeStyleOptions2.RequireAccessibilityModifiers);
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules).BaseNameParts;

            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(SymbolKind.Property, Accessibility.Public))
                {
                    var uniqueName = GenerateUniqueName(parameter, parameterNameParts, rule);

                    var accessibilityLevel = Accessibility.Public;
                    if (requireAccessibilityModifiers.Value is AccessibilityModifiersRequired.Never or AccessibilityModifiersRequired.OmitIfDefault)
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
                        new DeclarationModifiers(),
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
            throw ExceptionUtilities.Unreachable;
        }

        private async Task<Document> AddAllSymbolInitializationsAsync(
            Document document,
            SyntaxNode constructorDeclaration,
            IBlockOperation? blockStatementOpt,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<ISymbol> fieldsOrProperties,
            CodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(parameters.Length >= 2);
            Debug.Assert(fieldsOrProperties.Length > 0);
            Debug.Assert(parameters.Length == fieldsOrProperties.Length);

            // Process each param+field/prop in order.  Apply the pair to the document getting the updated document.
            // Then find all the current data in that updated document and move onto the next pair.

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodesToTrack = new List<SyntaxNode> { constructorDeclaration };
            if (blockStatementOpt != null)
                nodesToTrack.Add(blockStatementOpt.Syntax);

            var trackedRoot = root.TrackNodes(nodesToTrack);
            var currentDocument = document.WithSyntaxRoot(trackedRoot);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var fieldOrProperty = fieldsOrProperties[i];

                var currentSemanticModel = await currentDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var currentCompilation = currentSemanticModel.Compilation;
                var currentRoot = await currentDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var currentConstructorDeclaration = currentRoot.GetCurrentNode(constructorDeclaration);
                if (currentConstructorDeclaration == null)
                    continue;

                IBlockOperation? currentBlockStatementOpt = null;
                if (blockStatementOpt != null)
                {
                    currentBlockStatementOpt = (IBlockOperation?)currentSemanticModel.GetOperation(currentRoot.GetCurrentNode(blockStatementOpt.Syntax)!, cancellationToken);
                    if (currentBlockStatementOpt == null)
                        continue;
                }

                var currentParameter = (IParameterSymbol?)parameter.GetSymbolKey(cancellationToken).Resolve(currentCompilation, cancellationToken: cancellationToken).GetAnySymbol();
                if (currentParameter == null)
                    continue;

                // fieldOrProperty is a new member.  So we don't have to track it to this edit we're making.

                currentDocument = await AddSingleSymbolInitializationAsync(
                    currentDocument,
                    currentConstructorDeclaration,
                    currentBlockStatementOpt,
                    currentParameter,
                    fieldOrProperty,
                    fallbackOptions,
                    cancellationToken).ConfigureAwait(false);
            }

            return currentDocument;
        }

        private async Task<Document> AddSingleSymbolInitializationAsync(
            Document document,
            SyntaxNode constructorDeclaration,
            IBlockOperation? blockStatementOpt,
            IParameterSymbol parameter,
            ISymbol fieldOrProperty,
            CodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var services = document.Project.Solution.Workspace.Services;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, services);
            var generator = editor.Generator;
            var options = await document.GetCodeGenerationOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var codeGenerator = document.GetRequiredLanguageService<ICodeGenerationService>();

            if (fieldOrProperty.ContainingType == null)
            {
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
                                options.GetInfo(GetAddContext<IPropertySymbol>(parameter, blockStatementOpt, typeDeclaration, cancellationToken), document.Project),
                                cancellationToken);
                        }
                        else if (fieldOrProperty is IFieldSymbol field)
                        {
                            return codeGenerator.AddField(
                                currentTypeDecl, field,
                                options.GetInfo(GetAddContext<IFieldSymbol>(parameter, blockStatementOpt, typeDeclaration, cancellationToken), document.Project),
                                cancellationToken);
                        }
                        else
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    });
            }

            // Now that we've added any potential members, create an assignment between it
            // and the parameter.
            var initializationStatement = (TStatementSyntax)generator.ExpressionStatement(
                generator.AssignmentStatement(
                    generator.MemberAccessExpression(
                        generator.ThisExpression(),
                        generator.IdentifierName(fieldOrProperty.Name)),
                    generator.IdentifierName(parameter.Name)));

            // Attempt to place the initialization in a good location in the constructor
            // We'll want to keep initialization statements in the same order as we see
            // parameters for the constructor.
            var statementToAddAfterOpt = TryGetStatementToAddInitializationAfter(parameter, blockStatementOpt);

            InsertStatement(editor, constructorDeclaration, returnsVoid: true, statementToAddAfterOpt, initializationStatement);

            return document.WithSyntaxRoot(editor.GetChangedRoot());
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

        private static ImmutableArray<(IParameterSymbol parameter, bool before)> GetSiblingParameters(IParameterSymbol parameter)
        {
            using var _ = ArrayBuilder<(IParameterSymbol, bool before)>.GetInstance(out var siblings);

            if (parameter.ContainingSymbol is IMethodSymbol method)
            {
                var parameterIndex = method.Parameters.IndexOf(parameter);

                // look for an existing assignment for a parameter that comes before us.
                // If we find one, we'll add ourselves after that parameter check.
                for (var i = parameterIndex - 1; i >= 0; i--)
                    siblings.Add((method.Parameters[i], before: true));

                // look for an existing check for a parameter that comes before us.
                // If we find one, we'll add ourselves after that parameter check.
                for (var i = parameterIndex + 1; i < method.Parameters.Length; i++)
                    siblings.Add((method.Parameters[i], before: false));
            }

            return siblings.ToImmutable();
        }

        private SyntaxNode? TryGetStatementToAddInitializationAfter(
            IParameterSymbol parameter, IBlockOperation? blockStatementOpt)
        {
            // look for an existing assignment for a parameter that comes before/after us.
            // If we find one, we'll add ourselves before/after that parameter check.
            foreach (var (sibling, before) in GetSiblingParameters(parameter))
            {
                var statement = TryFindFieldOrPropertyAssignmentStatement(sibling, blockStatementOpt);
                if (statement != null)
                {
                    if (before)
                    {
                        return statement.Syntax;
                    }
                    else
                    {
                        var statementIndex = blockStatementOpt!.Operations.IndexOf(statement);
                        return statementIndex > 0 ? blockStatementOpt.Operations[statementIndex - 1].Syntax : null;
                    }
                }
            }

            // We couldn't find a reasonable location for the new initialization statement.
            // Just place ourselves after the last statement in the constructor.
            return TryGetLastStatement(blockStatementOpt);
        }

        private static IOperation? TryFindFieldOrPropertyAssignmentStatement(IParameterSymbol parameter, IBlockOperation? blockStatementOpt)
            => TryFindFieldOrPropertyAssignmentStatement(parameter, blockStatementOpt, out _);

        private static IOperation? TryFindFieldOrPropertyAssignmentStatement(
            IParameterSymbol parameter, IBlockOperation? blockStatementOpt, out ISymbol? fieldOrProperty)
        {
            if (blockStatementOpt != null)
            {
                var containingType = parameter.ContainingType;
                foreach (var statement in blockStatementOpt.Operations)
                {
                    // look for something of the form:  "this.s = s" or "this.s = s ?? ..."
                    if (IsFieldOrPropertyAssignment(statement, containingType, out var assignmentExpression, out fieldOrProperty) &&
                        IsParameterReferenceOrCoalesceOfParameterReference(assignmentExpression, parameter))
                    {
                        return statement;
                    }
                }
            }

            fieldOrProperty = null;
            return null;
        }

        private static bool IsParameterReferenceOrCoalesceOfParameterReference(
           IAssignmentOperation assignmentExpression, IParameterSymbol parameter)
        {
            if (IsParameterReference(assignmentExpression.Value, parameter))
            {
                // We already have a member initialized with this parameter like:
                //      this.field = parameter
                return true;
            }

            if (UnwrapImplicitConversion(assignmentExpression.Value) is ICoalesceOperation coalesceExpression &&
                IsParameterReference(coalesceExpression.Value, parameter))
            {
                // We already have a member initialized with this parameter like:
                //      this.field = parameter ?? ...
                return true;
            }

            return false;
        }

        private async Task<ISymbol?> TryFindMatchingUninitializedFieldOrPropertySymbolAsync(
            Document document, IParameterSymbol parameter, IBlockOperation? blockStatementOpt, ImmutableArray<NamingRule> rules, ImmutableArray<string> parameterWords, CancellationToken cancellationToken)
        {
            // Look for a field/property that really looks like it corresponds to this parameter.
            // Use a variety of heuristics around the name/type to see if this is a match.

            var containingType = parameter.ContainingType;
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Walk through the naming rules against this parameter's name to see what
            // name the user would like for it as a member in this type.  Note that we
            // have some fallback rules that use the standard conventions around 
            // properties /fields so that can still find things even if the user has no
            // naming preferences set.

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
                        !ContainsMemberAssignment(blockStatementOpt, field))
                    {
                        return field;
                    }

                    // If it's a writable property that we could assign this parameter to, and it's
                    // not already been assigned to, then this property is a good candidate for us to
                    // hook up to.
                    if (memberWithName is IPropertySymbol property &&
                        property.IsWritableInConstructor() &&
                        IsImplicitConversion(compilation, source: parameter.Type, destination: property.Type) &&
                        !ContainsMemberAssignment(blockStatementOpt, property))
                    {
                        return property;
                    }
                }
            }

            // Couldn't find any existing member.  Just return nothing so we can offer to
            // create a member for them.
            return null;
        }

        private static bool ContainsMemberAssignment(
            IBlockOperation? blockStatementOpt, ISymbol member)
        {
            if (blockStatementOpt != null)
            {
                foreach (var statement in blockStatementOpt.Operations)
                {
                    if (IsFieldOrPropertyAssignment(statement, member.ContainingType, out var assignmentExpression) &&
                        UnwrapImplicitConversion(assignmentExpression.Target) is IMemberReferenceOperation memberReference &&
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
