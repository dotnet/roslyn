// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
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
    internal sealed class CSharpInitializeMemberFromPrimaryConstructorParameterCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInitializeMemberFromPrimaryConstructorParameterCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            // TODO: One could try to retrieve TParameterList and then filter out parameters that intersect with
            // textSpan and use that as `parameterNodes`, where `selectedParameter` would be the first one.

            var selectedParameter = await context.TryGetRelevantNodeAsync<ParameterSyntax>().ConfigureAwait(false);
            if (selectedParameter == null)
                return;

            if (selectedParameter.Parent is not ParameterListSyntax { Parent: TypeDeclarationSyntax typeDeclaration })
                return;

            var generator = SyntaxGenerator.GetGenerator(document);
            // var parameterNodes = generator.GetParameters(functionDeclaration);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // we can't just call GetDeclaredSymbol on functionDeclaration because it could an anonymous function,
            // so first we have to get the parameter symbol and then its containing method symbol
            var parameter = (IParameterSymbol)semanticModel.GetRequiredDeclaredSymbol(selectedParameter, cancellationToken);
            if (parameter?.Name is null or "")
                return;

            if (parameter.ContainingSymbol is not IMethodSymbol methodSymbol ||
                methodSymbol.IsAbstract ||
                methodSymbol.IsExtern ||
                methodSymbol.PartialImplementationPart != null ||
                methodSymbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                return;
            }

            //// We shouldn't offer a refactoring if the compilation doesn't contain the ArgumentNullException type,
            //// as we use it later on in our computations.
            //var argumentNullExceptionType = typeof(ArgumentNullException).FullName;
            //if (argumentNullExceptionType is null || semanticModel.Compilation.GetTypeByMetadataName(argumentNullExceptionType) is null)
            //    return;

            //if (CanOfferRefactoring(functionDeclaration, semanticModel, syntaxFacts, cancellationToken, out var blockStatementOpt))
            //{
            // Ok.  Looks like the selected parameter could be refactored. Defer to subclass to 
            // actually determine if there are any viable refactorings here.
            var refactorings = await GetRefactoringsForSingleParameterAsync(
                document, typeDeclaration, selectedParameter, parameter, methodSymbol, context.Options, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(refactorings, context.Span);
            // }

            //// List with parameterNodes that pass all checks
            //using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var listOfPotentiallyValidParametersNodes);
            //foreach (var parameterNode in parameterNodes)
            //{
            //    if (!TryGetParameterSymbol(parameterNode, semanticModel, out parameter, cancellationToken))
            //        return;

            //    // Update the list of valid parameter nodes
            //    listOfPotentiallyValidParametersNodes.Add(parameterNode);
            //}

            //if (listOfPotentiallyValidParametersNodes.Count > 1)
            //{
            //    // Looks like we can offer a refactoring for more than one parameter. Defer to subclass to 
            //    // actually determine if there are any viable refactorings here.
            //    var refactorings = await GetRefactoringsForAllParametersAsync(
            //        document, functionDeclaration, methodSymbol, blockStatementOpt,
            //        listOfPotentiallyValidParametersNodes.ToImmutable(), selectedParameter.Span, context.Options, cancellationToken).ConfigureAwait(false);
            //    context.RegisterRefactorings(refactorings, context.Span);
            //}

            return;

            //static bool TryGetParameterSymbol(
            //    SyntaxNode parameterNode,
            //    SemanticModel semanticModel,
            //    [NotNullWhen(true)] out IParameterSymbol? parameter,
            //    CancellationToken cancellationToken)
            //{
            //    parameter = (IParameterSymbol?)semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);

            //    return parameter is { Name: not "" };
            //}
        }

        private async Task<ImmutableArray<CodeAction>> GetRefactoringsForSingleParameterAsync(
            Document document,
            TypeDeclarationSyntax typeDeclaration,
            ParameterSyntax parameterSyntax,
            IParameterSymbol parameter,
            IMethodSymbol method,
            CleanCodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            // Only supported for constructor parameters.
            if (method.MethodKind != MethodKind.Constructor)
                return ImmutableArray<CodeAction>.Empty;

            // See if we're already assigning this parameter to a field/property in this type. If so, there's nothing
            // more for us to do.
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var assignmentExpression = TryFindFieldOrPropertyInitializerValue(semanticModel.Compilation, parameter, cancellationToken);
            if (assignmentExpression != null)
                return ImmutableArray<CodeAction>.Empty;

            // Haven't initialized any fields/properties with this parameter.  Offer to assign
            // to an existing matching field/prop if we can find one, or add a new field/prop
            // if we can't.

            var rules = await document.GetNamingRulesAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
            if (parameterNameParts.BaseName == "")
                return ImmutableArray<CodeAction>.Empty;

            var (fieldOrProperty, isThrowNotImplementedProperty) = await TryFindMatchingUninitializedFieldOrPropertySymbolAsync(
                document, parameter, rules, parameterNameParts.BaseNameParts, cancellationToken).ConfigureAwait(false);

            if (fieldOrProperty != null)
            {
                return HandleExistingFieldOrProperty(
                    document, parameter, fieldOrProperty, isThrowNotImplementedProperty, fallbackOptions);
            }
            else
            {
                return await HandleNoExistingFieldOrPropertyAsync(
                    document, typeDeclaration, parameter, method, rules, fallbackOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<ImmutableArray<CodeAction>> HandleNoExistingFieldOrPropertyAsync(
            Document document,
            TypeDeclarationSyntax typeDeclaration,
            IParameterSymbol parameter,
            IMethodSymbol method,
            ImmutableArray<NamingRule> rules,
            CleanCodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            // Didn't find a field/prop that this parameter could be assigned to.
            // Offer to create new one and assign to that.
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var allActions);

            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);

            var (fieldAction, propertyAction) = AddSpecificParameterInitializationActions(
                document, parameter, constructorDeclaration, blockStatement, rules, formattingOptions.AccessibilityModifiersRequired, fallbackOptions);

            // Check if the surrounding parameters are assigned to another field in this class.  If so, offer to
            // make this parameter into a field as well.  Otherwise, default to generating a property
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var siblingFieldOrProperty = TryFindSiblingFieldOrProperty(compilation, parameter, cancellationToken);
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
                document, compilation, method, rules, formattingOptions.AccessibilityModifiersRequired, fallbackOptions, cancellationToken);

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
            Compilation compilation,
            IMethodSymbol method,
            ImmutableArray<NamingRule> rules,
            AccessibilityModifiersRequired accessibilityModifiersRequired,
            CodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var parameters = GetParametersWithoutAssociatedMembers(
                compilation, rules, method, cancellationToken);

            if (parameters.Length < 2)
                return default;

            var fields = parameters.SelectAsArray(p => (ISymbol)CreateField(p, accessibilityModifiersRequired, rules));
            var properties = parameters.SelectAsArray(p => (ISymbol)CreateProperty(p, rules));

            var allFieldsAction = CodeAction.Create(
                FeaturesResources.Create_and_assign_remaining_as_fields,
                c => AddAllSymbolInitializationsAsync(
                    document, constructorDeclaration, blockStatement, parameters, fields, fallbackOptions, c),
                nameof(FeaturesResources.Create_and_assign_remaining_as_fields));
            var allPropertiesAction = CodeAction.Create(
                FeaturesResources.Create_and_assign_remaining_as_properties,
                c => AddAllSymbolInitializationsAsync(
                    document, constructorDeclaration, blockStatement, parameters, properties, fallbackOptions, c),
                nameof(FeaturesResources.Create_and_assign_remaining_as_properties));

            return (allFieldsAction, allPropertiesAction);
        }

        private (CodeAction fieldAction, CodeAction propertyAction) AddSpecificParameterInitializationActions(
            Document document,
            IParameterSymbol parameter,
            SyntaxNode constructorDeclaration,
            IBlockOperation? blockStatement,
            ImmutableArray<NamingRule> rules,
            AccessibilityModifiersRequired accessibilityModifiersRequired,
            CodeGenerationOptionsProvider fallbackOptions)
        {
            var field = CreateField(parameter, accessibilityModifiersRequired, rules);
            var property = CreateProperty(parameter, rules);

            // we're generating the field or property, so we don't have to handle throwing versions of them.
            var isThrowNotImplementedProperty = false;

            var fieldAction = CodeAction.Create(
                string.Format(FeaturesResources.Create_and_assign_field_0, field.Name),
                c => AddSingleSymbolInitializationAsync(document, constructorDeclaration, blockStatement, parameter, field, isThrowNotImplementedProperty, fallbackOptions, c),
                nameof(FeaturesResources.Create_and_assign_field_0) + "_" + field.Name);
            var propertyAction = CodeAction.Create(
                string.Format(FeaturesResources.Create_and_assign_property_0, property.Name),
                c => AddSingleSymbolInitializationAsync(document, constructorDeclaration, blockStatement, parameter, property, isThrowNotImplementedProperty, fallbackOptions, c),
                nameof(FeaturesResources.Create_and_assign_property_0) + "_" + property.Name);

            return (fieldAction, propertyAction);
        }

        private static ImmutableArray<IParameterSymbol> GetParametersWithoutAssociatedMembers(
            Compilation compilation,
            ImmutableArray<NamingRule> rules,
            IMethodSymbol method,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var result);

            foreach (var parameter in method.Parameters)
            {
                var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules);
                if (parameterNameParts.BaseName == "")
                    continue;

                var assignmentOp = TryFindFieldOrPropertyInitializerValue(compilation, parameter, cancellationToken);
                if (assignmentOp != null)
                    continue;

                result.Add(parameter);
            }

            return result.ToImmutable();
        }

        private ImmutableArray<CodeAction> HandleExistingFieldOrProperty(
            Document document,
            IParameterSymbol parameter,
            ISymbol fieldOrProperty,
            bool isThrowNotImplementedProperty,
            CodeGenerationOptionsProvider fallbackOptions)
        {
            // Found a field/property that this parameter should be assigned to.
            // Just offer the simple assignment to it.

            var resource = fieldOrProperty.Kind == SymbolKind.Field
                ? FeaturesResources.Initialize_field_0
                : FeaturesResources.Initialize_property_0;

            var title = string.Format(resource, fieldOrProperty.Name);

            return ImmutableArray.Create(CodeAction.Create(
                title,
                cancellationToken => AddSingleSymbolInitializationAsync(
                    document, parameter, fieldOrProperty, isThrowNotImplementedProperty, fallbackOptions, cancellationToken),
                title));
        }

        private static ISymbol? TryFindSiblingFieldOrProperty(
            Compilation compilation,
            IParameterSymbol parameter,
            CancellationToken cancellationToken)
        {
            foreach (var (siblingParam, _) in InitializeParameterHelpersCore.GetSiblingParameters(parameter))
            {
                TryFindFieldOrPropertyInitializerValue(compilation, siblingParam, out var sibling, cancellationToken);
                if (sibling != null)
                    return sibling;
            }

            return null;
        }

        private static IFieldSymbol CreateField(
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

                    var accessibilityLevel = accessibilityModifiersRequired is AccessibilityModifiersRequired.Never or AccessibilityModifiersRequired.OmitIfDefault
                        ? Accessibility.NotApplicable
                        : Accessibility.Private;
                    //if (accessibilityModifiersRequired is AccessibilityModifiersRequired.Never or AccessibilityModifiersRequired.OmitIfDefault)
                    //{
                    //    var defaultAccessibility = DetermineDefaultFieldAccessibility(parameter.ContainingType);
                    //    if (defaultAccessibility == Accessibility.Private)
                    //    {
                    //        accessibilityLevel = Accessibility.NotApplicable;
                    //    }
                    //}

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
            ImmutableArray<NamingRule> rules)
        {
            var parameterNameParts = IdentifierNameParts.CreateIdentifierNameParts(parameter, rules).BaseNameParts;

            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(SymbolKind.Property, Accessibility.Public))
                {
                    var uniqueName = GenerateUniqueName(parameter, parameterNameParts, rule);

                    var accessibilityLevel = Accessibility.Public;

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
            throw ExceptionUtilities.Unreachable();
        }

        private static async Task<Solution> AddAllSymbolInitializationsAsync(
            Document document,
            TypeDeclarationSyntax typeDeclaration,
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

            var trackedRoot = root.TrackNodes(typeDeclaration);
            var currentSolution = document.WithSyntaxRoot(trackedRoot).Project.Solution;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var fieldOrProperty = fieldsOrProperties[i];

                var currentDocument = currentSolution.GetRequiredDocument(document.Id);
                var currentSemanticModel = await currentDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var currentCompilation = currentSemanticModel.Compilation;
                var currentRoot = await currentDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var currentTypeDeclaration = currentRoot.GetCurrentNode(typeDeclaration);
                if (currentTypeDeclaration == null)
                    continue;

                var currentParameter = (IParameterSymbol?)parameter.GetSymbolKey(cancellationToken).Resolve(currentCompilation, cancellationToken: cancellationToken).GetAnySymbol();
                if (currentParameter == null)
                    continue;

                // fieldOrProperty is a new member.  So we don't have to track it to this edit we're making.

                currentSolution = await AddSingleSymbolInitializationAsync(
                    currentDocument,
                    currentTypeDeclaration,
                    currentParameter,
                    fieldOrProperty,
                    isThrowNotImplementedProperty: false,
                    fallbackOptions,
                    cancellationToken).ConfigureAwait(false);
            }

            return currentSolution;
        }

        private static async Task<Solution> AddSingleSymbolInitializationAsync(
            Document document,
            TypeDeclarationSyntax typeDeclaration,
            IParameterSymbol parameter,
            ISymbol fieldOrProperty,
            bool isThrowNotImplementedProperty,
            CodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var solution = project.Solution;
            var services = solution.Services;

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var solutionEditor = new SolutionEditor(solution);
            var options = await document.GetCodeGenerationOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var codeGenerator = document.GetRequiredLanguageService<ICodeGenerationService>();

            // var mainEditor = await editor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
            if (fieldOrProperty.ContainingType == null)
            {
                // We're generating a new field/property.  Place into the containing type, ideally before/after a
                // relevant existing member.
                var (sibling, siblingSyntax, addContext) = fieldOrProperty switch
                {
                    IPropertySymbol => GetAddContext<IPropertySymbol>(compilation, parameter, cancellationToken),
                    IFieldSymbol => GetAddContext<IFieldSymbol>(compilation, parameter, cancellationToken),
                    _ => throw ExceptionUtilities.UnexpectedValue(fieldOrProperty),
                };

                var preferredTypeDeclaration = siblingSyntax?.GetAncestorOrThis<TypeDeclarationSyntax>() ?? typeDeclaration;

                var editingDocument = solution.GetRequiredDocument(typeDeclaration.SyntaxTree);
                var editor = await solutionEditor.GetDocumentEditorAsync(editingDocument.Id, cancellationToken).ConfigureAwait(false);
                editor.ReplaceNode(
                    typeDeclaration,
                    (currentTypeDecl, _) =>
                    {
                        if (fieldOrProperty is IPropertySymbol property)
                        {
                            return codeGenerator.AddProperty(
                                currentTypeDecl, property,
                                codeGenerator.GetInfo(addContext, options, root.SyntaxTree.Options),
                                cancellationToken);
                        }
                        else if (fieldOrProperty is IFieldSymbol field)
                        {
                            return codeGenerator.AddField(
                                currentTypeDecl, field,
                                codeGenerator.GetInfo(addContext, options, root.SyntaxTree.Options),
                                cancellationToken);
                        }
                        else
                        {
                            throw ExceptionUtilities.Unreachable();
                        }
                    });
            }
            else
            {
                // We're updating an exiting field/prop.
                if (fieldOrProperty is IPropertySymbol property)
                {
                    foreach (var syntaxRef in property.DeclaringSyntaxReferences)
                    {
                        if (syntaxRef.GetSyntax(cancellationToken) is PropertyDeclarationSyntax propertyDeclaration)
                        {
                            var editingDocument = solution.GetRequiredDocument(propertyDeclaration.SyntaxTree);
                            var editor = await solutionEditor.GetDocumentEditorAsync(editingDocument.Id, cancellationToken).ConfigureAwait(false);

                            // If the user had a property that has 'throw NotImplementedException' in it, then remove those throws.
                            var newPropertyDeclaration = isThrowNotImplementedProperty ? RemoveThrowNotImplemented(propertyDeclaration) : propertyDeclaration;
                            editor.ReplaceNode(
                                propertyDeclaration,
                                newPropertyDeclaration.WithInitializer(EqualsValueClause(IdentifierName(parameter.Name.EscapeIdentifier()))));
                        }
                    }
                }
                else if (fieldOrProperty is IFieldSymbol field)
                {
                    foreach (var syntaxRef in field.DeclaringSyntaxReferences)
                    {
                        if (syntaxRef.GetSyntax(cancellationToken) is VariableDeclaratorSyntax variableDeclarator)
                        {
                            var editingDocument = solution.GetRequiredDocument(variableDeclarator.SyntaxTree);
                            var editor = await solutionEditor.GetDocumentEditorAsync(editingDocument.Id, cancellationToken).ConfigureAwait(false);
                            editor.ReplaceNode(
                                variableDeclarator,
                                variableDeclarator.WithInitializer(EqualsValueClause(IdentifierName(parameter.Name.EscapeIdentifier()))));
                            break;
                        }
                    }
                }
                else
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            return solutionEditor.GetChangedSolution();
        }

        //private void AddAssignment(
        //    IParameterSymbol parameter,
        //    ISymbol fieldOrProperty,
        //    SyntaxEditor editor)
        //{
        //    //// First see if the user has `(_x, y) = (x, y);` and attempt to update that. 
        //    //if (TryUpdateTupleAssignment(blockStatement, parameter, fieldOrProperty, editor))
        //    //    return;

        //    var generator = editor.Generator;

        //    // Now that we've added any potential members, create an assignment between it
        //    // and the parameter.
        //    var initializationStatement = (StatementSyntax)generator.ExpressionStatement(
        //        generator.AssignmentStatement(
        //            generator.MemberAccessExpression(
        //                generator.ThisExpression(),
        //                generator.IdentifierName(fieldOrProperty.Name)),
        //            generator.IdentifierName(parameter.Name)));

        //    // Attempt to place the initialization in a good location in the constructor
        //    // We'll want to keep initialization statements in the same order as we see
        //    // parameters for the constructor.
        //    var statementToAddAfter = TryGetStatementToAddInitializationAfter(parameter, blockStatement);

        //    InsertStatement(editor, constructorDeclaration, returnsVoid: true, statementToAddAfter, initializationStatement);
        //}

        private static (ISymbol? symbol, SyntaxNode? syntax, CodeGenerationContext context) GetAddContext<TSymbol>(
            Compilation compilation,
            IParameterSymbol parameter,
            CancellationToken cancellationToken)
            where TSymbol : class, ISymbol
        {
            foreach (var (sibling, before) in GetSiblingParameters(parameter))
            {
                var initializer = TryFindFieldOrPropertyInitializerValue(
                    compilation, sibling, out var fieldOrProperty, cancellationToken);

                if (initializer != null &&
                    fieldOrProperty is TSymbol { DeclaringSyntaxReferences: [var syntaxReference, ..] } symbol)
                {
                    var syntax = syntaxReference.GetSyntax(cancellationToken);
                    return (symbol, syntax, before
                        ? new CodeGenerationContext(afterThisLocation: syntax.GetLocation())
                        : new CodeGenerationContext(beforeThisLocation: syntax.GetLocation()));
                }
            }

            return (symbol: null, syntax: null, CodeGenerationContext.Default);
        }

        //private SyntaxNode? TryGetStatementToAddInitializationAfter(
        //    IParameterSymbol parameter, IBlockOperation? blockStatement)
        //{
        //    // look for an existing assignment for a parameter that comes before/after us.
        //    // If we find one, we'll add ourselves before/after that parameter check.
        //    foreach (var (sibling, before) in GetSiblingParameters(parameter))
        //    {
        //        var statement = TryFindFieldOrPropertyAssignmentStatement(sibling, blockStatement);
        //        if (statement != null)
        //        {
        //            if (before)
        //            {
        //                return statement.Syntax;
        //            }
        //            else
        //            {
        //                var statementIndex = blockStatement!.Operations.IndexOf(statement);
        //                return statementIndex > 0 ? blockStatement.Operations[statementIndex - 1].Syntax : null;
        //            }
        //        }
        //    }

        //    // We couldn't find a reasonable location for the new initialization statement.
        //    // Just place ourselves after the last statement in the constructor.
        //    return TryGetLastStatement(blockStatement);
        //}

        private static IOperation? TryFindFieldOrPropertyInitializerValue(
            Compilation compilation,
            IParameterSymbol parameter,
            CancellationToken cancellationToken)
            => TryFindFieldOrPropertyInitializerValue(compilation, parameter, out _, cancellationToken);

        //protected static bool TryGetPartsOfTupleAssignmentOperation(
        //    IOperation operation,
        //    [NotNullWhen(true)] out ITupleOperation? targetTuple,
        //    [NotNullWhen(true)] out ITupleOperation? valueTuple)
        //{
        //    if (operation is IExpressionStatementOperation
        //        {
        //            Operation: IDeconstructionAssignmentOperation
        //            {
        //                Target: ITupleOperation targetTupleTemp,
        //                Value: IConversionOperation { Operand: ITupleOperation valueTupleTemp },
        //            }
        //        } &&
        //        targetTupleTemp.Elements.Length == valueTupleTemp.Elements.Length)
        //    {
        //        targetTuple = targetTupleTemp;
        //        valueTuple = valueTupleTemp;
        //        return true;
        //    }

        //    targetTuple = null;
        //    valueTuple = null;
        //    return false;
        //}

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

            //if (blockStatement != null)
            //{
            //    var containingType = parameter.ContainingType;
            //    foreach (var statement in blockStatement.Operations)
            //    {
            //        // look for something of the form:  "this.s = s" or "this.s = s ?? ..."
            //        if (IsFieldOrPropertyAssignment(statement, containingType, out var assignmentExpression, out fieldOrProperty) &&
            //            IsParameterReferenceOrCoalesceOfParameterReference(assignmentExpression, parameter))
            //        {
            //            return statement;
            //        }

            //        //// look inside the form `(this.s, this.t) = (s, t)`
            //        //if (TryGetPartsOfTupleAssignmentOperation(statement, out var targetTuple, out var valueTuple))
            //        //{
            //        //    for (int i = 0, n = targetTuple.Elements.Length; i < n; i++)
            //        //    {
            //        //        var target = targetTuple.Elements[i];
            //        //        var value = valueTuple.Elements[i];

            //        //        if (IsFieldOrPropertyReference(target, containingType, out fieldOrProperty) &&
            //        //            IsParameterReference(value, parameter))
            //        //        {
            //        //            return statement;
            //        //        }
            //        //    }
            //        //}
            //    }
            //}

            fieldOrProperty = null;
            return null;
        }

        private async Task<(ISymbol?, bool isThrowNotImplementedProperty)> TryFindMatchingUninitializedFieldOrPropertySymbolAsync(
            Document document, IParameterSymbol parameter, ImmutableArray<NamingRule> rules, ImmutableArray<string> parameterWords, CancellationToken cancellationToken)
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
                        InitializeParameterHelpers.IsImplicitConversion(compilation, source: parameter.Type, destination: field.Type) &&
                        field.DeclaringSyntaxReferences is [var syntaxRef1, ..] &&
                        syntaxRef1.GetSyntax(cancellationToken) is VariableDeclaratorSyntax { Initializer: null })
                    {
                        return (field, isThrowNotImplementedProperty: false);
                    }

                    // If it's a writable property that we could assign this parameter to, and it's
                    // not already been assigned to, then this property is a good candidate for us to
                    // hook up to.
                    if (memberWithName is IPropertySymbol property &&
                        InitializeParameterHelpers.IsImplicitConversion(compilation, source: parameter.Type, destination: property.Type) &&
                        property.DeclaringSyntaxReferences is [var syntaxRef2, ..] &&
                        syntaxRef2.GetSyntax(cancellationToken) is PropertyDeclarationSyntax { Initializer: null })
                    {
                        // We also allow assigning into a property of the form `=> throw new NotImplementedException()`.
                        // That way users can easily spit out those methods, but then convert them to be normal
                        // properties with ease.
                        if (IsThrowNotImplementedProperty(compilation, property, cancellationToken))
                            return (property, isThrowNotImplementedProperty: true);

                        if (property.IsWritableInConstructor())
                            return (property, isThrowNotImplementedProperty: false);
                    }
                }
            }

            // Couldn't find any existing member.  Just return nothing so we can offer to
            // create a member for them.
            return default;

            //static bool ContainsMemberAssignment(IBlockOperation? blockStatement, ISymbol member)
            //{
            //    if (blockStatement != null)
            //    {
            //        foreach (var statement in blockStatement.Operations)
            //        {
            //            if (IsFieldOrPropertyAssignment(statement, member.ContainingType, out var assignmentExpression) &&
            //                assignmentExpression.Target.UnwrapImplicitConversion() is IMemberReferenceOperation memberReference &&
            //                member.Equals(memberReference.Member))
            //            {
            //                return true;
            //            }
            //        }
            //    }

            //    return false;
            //}

            //bool IsThrowNotImplementedProperty(IPropertySymbol property)
            //{
            //    using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var accessors);

            //    if (property.GetMethod != null)
            //        accessors.AddIfNotNull(InitializeParameterHelpers.GetAccessorBody(property.GetMethod, cancellationToken));

            //    if (property.SetMethod != null)
            //        accessors.AddIfNotNull(InitializeParameterHelpers.GetAccessorBody(property.SetMethod, cancellationToken));

            //    if (accessors.Count == 0)
            //        return false;

            //    foreach (var group in accessors.GroupBy(node => node.SyntaxTree))
            //    {
            //        var semanticModel = compilation.GetSemanticModel(group.Key);
            //        foreach (var accessorBody in accessors)
            //        {
            //            var operation = semanticModel.GetOperation(accessorBody, cancellationToken);
            //            if (operation is null)
            //                return false;

            //            if (!operation.IsSingleThrowNotImplementedOperation())
            //                return false;
            //        }
            //    }

            //    return true;
            //}
        }
    }
}
