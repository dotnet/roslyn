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
        Document document, TypeDeclarationSyntax typeDeclaration, ParameterListSyntax parameterList, CodeActionOptionsProvider options, CancellationToken cancellationToken)
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

        var syntaxGenerator = CSharpSyntaxGenerator.Instance;
        var parameters = parameterList.Parameters.SelectAsArray(p => semanticModel.GetRequiredDeclaredSymbol(p, cancellationToken));

        var parameterToNonDocCommentLocations = await GetParameterLocationsAsync(includeDocCommentLocations: false).ConfigureAwait(false);
        var parameterToSynthesizedFields = await GetSynthesizedFieldsAsync().ConfigureAwait(false);

        var baseType = typeDeclaration.BaseList?.Types is [PrimaryConstructorBaseTypeSyntax type, ..] ? type : null;
        var methodTargetingAttributes = typeDeclaration.AttributeLists.Where(list => list.Target?.Identifier.ValueText == "method");
        var constructorDeclaration = CreateConstructorDeclaration();

        // Now start editing the document
        var mainDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

        mainDocumentEditor.RemoveNode(parameterList);
        if (baseType != null)
            mainDocumentEditor.ReplaceNode(baseType, SimpleBaseType(baseType.Type).WithTriviaFrom(baseType));

        foreach (var attributeList in methodTargetingAttributes)
            mainDocumentEditor.RemoveNode(attributeList);


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
                    foreach (var referenceLocation in reference.Locations.Distinct(LinkedFileReferenceLocationEqualityComparer.Instance))
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
                //var referencedLocations = parameterToNonDocCommentLocations[parameter];
                //if (referencedLocations.Count == 0)
                //    continue;

                //// if the parameter is only referenced in a single location, and that location is initializing a
                //// field/property already, we don't need to create a field for it.
                //if (referencedLocations.Count == 1 && referencedLocations.Single().assignedFieldOrProperty != null)
                //    continue;

                //// it was referenced outside of a field/prop initializer.  Need to synthesize a field for it.
                //result.Add(
                //    parameter,
                //    CodeGenerationSymbolFactory.CreateFieldSymbol(
                //        )

            }

            return result.ToImmutableDictionary();
        }

        async Task<string> MakeFieldNameAsync(string parameterName)
        {
            var rule = await document.GetApplicableNamingRuleAsync(
                new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
                DeclarationModifiers.None,
                Accessibility.Private,
                options,
                cancellationToken).ConfigureAwait(false);

            var fieldName = rule.NamingStyle.MakeCompliant(parameterName).First();
            return NameGenerator.GenerateUniqueName(fieldName, n => namedType.Name != n && !namedType.GetMembers(n).Any());
        }

        ConstructorDeclarationSyntax CreateConstructorDeclaration()
        {
            var attributes = List(methodTargetingAttributes);
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
