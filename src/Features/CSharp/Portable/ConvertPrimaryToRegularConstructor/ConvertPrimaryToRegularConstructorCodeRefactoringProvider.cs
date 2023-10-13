// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertPrimaryToRegularConstructor;

using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertPrimaryToRegularConstructor), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ConvertPrimaryToRegularConstructorCodeRefactoringProvider()
    : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;
        var typeDeclaration = await context.TryGetRelevantNodeAsync<TypeDeclarationSyntax>().ConfigureAwait(false);
        if (typeDeclaration?.ParameterList is null)
            return;

        // Converting a record to a non-primary-constructor form is a lot more work (for example, having to synthesize a
        // Deconstruct method, and figure out how to specify properties, etc.).  We can consider adding support for that
        // scenario later if desired.
        if (typeDeclaration is RecordDeclarationSyntax)
            return;

        var triggerSpan = TextSpan.FromBounds(typeDeclaration.SpanStart, typeDeclaration.ParameterList.FullSpan.End);
        if (!triggerSpan.Contains(span))
            return;

        context.RegisterRefactoring(CodeAction.Create(
            CSharpFeaturesResources.Convert_to_regular_constructor,
            cancellationToken => ConvertAsync(document, typeDeclaration, typeDeclaration.ParameterList, context.Options, cancellationToken),
            nameof(CSharpFeaturesResources.Convert_to_regular_constructor)));
    }

    private static async Task<Solution> ConvertAsync(
        Document document,
        TypeDeclarationSyntax typeDeclaration,
        ParameterListSyntax parameterList,
        CodeActionOptionsProvider optionsProvider,
        CancellationToken cancellationToken)
    {
        // 1. Create constructor
        // 2. Remove base arguments
        // 3. Add fields if necessary
        // 4. Update references to parameters to be references to fields
        // 5. Format as appropriate

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var namedType = semanticModel.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);

        // We may have to update multiple files (in the case of a partial type).  Use a solution-editor to make that simple.
        var solution = document.Project.Solution;
        var solutionEditor = new SolutionEditor(solution);

        var codeGenService = document.GetRequiredLanguageService<ICodeGenerationService>();
        var syntaxGenerator = CSharpSyntaxGenerator.Instance;
        var parameters = parameterList.Parameters.SelectAsArray(p => semanticModel.GetRequiredDeclaredSymbol(p, cancellationToken));

        var parameterToNonDocCommentLocations = await GetParameterLocationsAsync(includeDocCommentLocations: false).ConfigureAwait(false);
        var parameterToSynthesizedFields = await GetSynthesizedFieldsAsync().ConfigureAwait(false);

        var baseType = typeDeclaration.BaseList?.Types is [PrimaryConstructorBaseTypeSyntax type, ..] ? type : null;
        var methodTargetingAttributes = typeDeclaration.AttributeLists.Where(list => list.Target?.Identifier.ValueText == "method");
        var constructorDeclaration = CreateConstructorDeclaration();

        // Now start editing the document
        var mainDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
        var contextInfo = await document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, optionsProvider, cancellationToken).ConfigureAwait(false);

        // Now, update all locations that reference the parameters to reference the new fields.

        // Remove the parameter list, and any base argument passing from the type declaration header itself.
        mainDocumentEditor.RemoveNode(parameterList);
        if (baseType != null)
            mainDocumentEditor.ReplaceNode(baseType, (current, _) => SimpleBaseType(((PrimaryConstructorBaseType)current).Type).WithTriviaFrom(baseType));

        // Remove all the attributes from the type decl that were moved to the constructor.
        foreach (var attributeList in methodTargetingAttributes)
            mainDocumentEditor.RemoveNode(attributeList);

        // Now add all the fields.
        mainDocumentEditor.ReplaceNode(
            typeDeclaration,
            (current, _) =>
            {
                var currentTypeDeclaration = (TypeDeclarationSyntax)current;
                var fieldsInOrder = parameters
                    .Select(p => parameterToSynthesizedFields.TryGetValue(p, out var field) ? field : null)
                    .WhereNotNull();
                return codeGenService.AddMembers(
                    currentTypeDeclaration, fieldsInOrder, contextInfo, cancellationToken);
            });

        // Now add the constructor
        mainDocumentEditor.ReplaceNode(
            typeDeclaration,
            (current, _) =>
            {
                // If there is an existing non-static constructor, place it before that
                var currentTypeDeclaration = (TypeDeclarationSyntax)current;
                var firstConstructorIndex = currentTypeDeclaration.Members.IndexOf(m => m is ConstructorDeclarationSyntax c && !c.Modifiers.Any(SyntaxKind.StaticKeyword));
                if (firstConstructorIndex >= 0)
                {
                    return currentTypeDeclaration.WithMembers(
                        currentTypeDeclaration.Members.Insert(firstConstructorIndex, constructorDeclaration));
                }

                // No constructors.  Place after any fields if present, or any properties if there are no fields.
                var lastFieldOrProperty = currentTypeDeclaration.Members.LastIndexOf(m => m is FieldDeclarationSyntax);
                if (lastFieldOrProperty < 0)
                    lastFieldOrProperty = currentTypeDeclaration.Members.LastIndexOf(m => m is PropertyDeclarationSyntax);

                if (lastFieldOrProperty >= 0)
                {
                    return currentTypeDeclaration.WithMembers(
                        currentTypeDeclaration.Members.Insert(lastFieldOrProperty + 1, constructorDeclaration));
                }

                // Nothing at all.  Just place the construct at the top of the type.
                return currentTypeDeclaration.WithMembers(
                    currentTypeDeclaration.Members.Insert(0, constructorDeclaration));
            });

        return solutionEditor.GetChangedSolution();

        async Task<MultiDictionary<IParameterSymbol, (IdentifierNameSyntax referencedLocation, ISymbol? assignedFieldOrProperty)>> GetParameterLocationsAsync(bool includeDocCommentLocations)
        {
            var result = new MultiDictionary<IParameterSymbol, (IdentifierNameSyntax referencedLocation, ISymbol? assignedFieldOrProperty)>();

            foreach (var parameter in parameters)
            {
                var references = await SymbolFinder.FindReferencesAsync(parameter, solution, cancellationToken).ConfigureAwait(false);
                foreach (var reference in references)
                {
                    // We may hit a location multiple times due to how we do FAR for linked symbols, but each linked symbol
                    // is allowed to report the entire set of references it think it is compatible with.  So ensure we're 
                    // hitting each location only once.
                    // 
                    // Note Use DistinctBy (.Net6) once available.
                    foreach (var grouping in reference.Locations.Distinct(LinkedFileReferenceLocationEqualityComparer.Instance).GroupBy(loc => loc.Location.SourceTree))
                    {
                        var syntaxTree = grouping.Key;
                        var editor = await solutionEditor.GetDocumentEditorAsync(solution.GetDocumentId(syntaxTree), cancellationToken).ConfigureAwait(false);

                        foreach (var referenceLocation in grouping)
                        {
                            if (referenceLocation.IsImplicit)
                                continue;

                            if (referenceLocation.Location.FindNode(cancellationToken) is not IdentifierNameSyntax identifierName)
                                continue;

                            // Explicitly ignore references to the base-constructor.  We don't need to generate fields for these.
                            if (identifierName.GetAncestor<PrimaryConstructorBaseTypeSyntax>() != null)
                                continue;

                            if (!includeDocCommentLocations && identifierName.GetAncestor<DocumentationCommentTriviaSyntax>() != null)
                                continue;

                            var expr = identifierName.WalkUpParentheses();
                            var assignedFieldOrProperty = GetAssignedFieldOrProperty(identifierName);
                            result.Add(parameter, (identifierName, assignedFieldOrProperty));
                        }
                    }
                }
            }

            return result;
        }

        // See if this is a reference to the parameter that is just initializing an existing field or property.
        ISymbol? GetAssignedFieldOrProperty(IdentifierNameSyntax identifierName)
        {
            var expr = identifierName.WalkUpParentheses();
            if (expr.Parent is EqualsValueClauseSyntax equalsValue)
            {
                return equalsValue.Parent is PropertyDeclarationSyntax or VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax } }
                    ? semanticModel.GetRequiredDeclaredSymbol(equalsValue.Parent, cancellationToken)
                    : null;
            }
            else if (expr.Parent is ArrowExpressionClauseSyntax arrowExpression)
            {
                return arrowExpression.Parent is PropertyDeclarationSyntax
                    ? semanticModel.GetRequiredDeclaredSymbol(arrowExpression.Parent, cancellationToken)
                    : arrowExpression.Parent is AccessorDeclarationSyntax { Parent: PropertyDeclarationSyntax }
                        ? semanticModel.GetRequiredDeclaredSymbol(arrowExpression.Parent.Parent, cancellationToken)
                        : null;
            }
            else
            {
                return null;
            }
        }

        async Task<ImmutableDictionary<IParameterSymbol, IFieldSymbol>> GetSynthesizedFieldsAsync()
        {
            using var _ = PooledDictionary<IParameterSymbol, IFieldSymbol>.GetInstance(out var result);

            foreach (var parameter in parameters)
            {
                var existingField = namedType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(
                    f => f.IsImplicitlyDeclared && parameter.Locations.Contains(f.Locations.FirstOrDefault()!));
                if (existingField == null)
                    continue;

                var synthesizedField = CodeGenerationSymbolFactory.CreateFieldSymbol(
                    existingField,
                    name: await MakeFieldNameAsync(parameter.Name).ConfigureAwait(false));

                result.Add(parameter, synthesizedField);
            }

            return result.ToImmutableDictionary();
        }

        async Task<string> MakeFieldNameAsync(string parameterName)
        {
            var rule = await document.GetApplicableNamingRuleAsync(
                new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
                DeclarationModifiers.None,
                Accessibility.Private,
                optionsProvider,
                cancellationToken).ConfigureAwait(false);

            var fieldName = rule.NamingStyle.MakeCompliant(parameterName).First();
            return NameGenerator.GenerateUniqueName(fieldName, n => namedType.Name != n && !namedType.GetMembers(n).Any());
        }

        ConstructorDeclarationSyntax CreateConstructorDeclaration()
        {
            var attributes = List(methodTargetingAttributes.Select(a => a.WithTarget(null)));
            var modifiers = typeDeclaration.Modifiers
                .Where(m => SyntaxFacts.IsAccessibilityModifier(m.Kind()))
                .Select(m => m.WithoutTrivia().WithAppendedTrailingTrivia(Space));

            using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var assignmentStatements);
            foreach (var parameter in parameters)
            {
                if (!parameterToSynthesizedFields.TryGetValue(parameter, out var field))
                    continue;

                var fieldName = field.Name.ToIdentifierName();
                var left = parameter.Name == field.Name
                    ? MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), fieldName)
                    : (ExpressionSyntax)fieldName;
                var assignment = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, parameter.Name.ToIdentifierName());
                assignmentStatements.Add(ExpressionStatement(assignment));
            }

            return ConstructorDeclaration(
                attributes,
                TokenList(modifiers),
                typeDeclaration.Identifier.WithoutTrivia().WithAppendedTrailingTrivia(Space),
                parameterList.WithoutTrivia(),
                baseType?.ArgumentList is null ? null : ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, baseType.ArgumentList),
                Block(assignmentStatements));
        }
    }
}
