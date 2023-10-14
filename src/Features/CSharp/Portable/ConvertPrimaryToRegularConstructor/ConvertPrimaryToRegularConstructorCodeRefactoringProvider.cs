// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
                nameof(CSharpFeaturesResources.Convert_to_regular_constructor)),
            triggerSpan);
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
        var compilation = semanticModel.Compilation;
        var namedType = semanticModel.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);

        // We may have to update multiple files (in the case of a partial type).  Use a solution-editor to make that simple.
        var solution = document.Project.Solution;
        var solutionEditor = new SolutionEditor(solution);

        var codeGenService = document.GetRequiredLanguageService<ICodeGenerationService>();
        var syntaxGenerator = CSharpSyntaxGenerator.Instance;
        var parameters = parameterList.Parameters.SelectAsArray(p => semanticModel.GetRequiredDeclaredSymbol(p, cancellationToken));

        var parameterToSynthesizedFields = await GetSynthesizedFieldsAsync().ConfigureAwait(false);
        var parameterReferences = await GetParameterReferencesAsync().ConfigureAwait(false);

        var parameterToExistingFieldOrProperty = await GetExistingAssignedFieldsOrProperties().ConfigureAwait(false);

        var baseType = typeDeclaration.BaseList?.Types is [PrimaryConstructorBaseTypeSyntax type, ..] ? type : null;
        var methodTargetingAttributes = typeDeclaration.AttributeLists.Where(list => list.Target?.Identifier.ValueText == "method");
        var constructorDeclaration = CreateConstructorDeclaration();

        // Now start editing the document
        var mainDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
        var contextInfo = await document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, optionsProvider, cancellationToken).ConfigureAwait(false);

        // Now, update all locations that reference the parameters to reference the new fields.
        await RewriteReferencesToParametersAsync().ConfigureAwait(false);

        // Remove the parameter list, and any base argument passing from the type declaration header itself.
        mainDocumentEditor.RemoveNode(parameterList);
        if (baseType != null)
            mainDocumentEditor.ReplaceNode(baseType, (current, _) => SimpleBaseType(((PrimaryConstructorBaseTypeSyntax)current).Type).WithTriviaFrom(baseType));

        // Remove all the attributes from the type decl that were moved to the constructor.
        foreach (var attributeList in methodTargetingAttributes)
            mainDocumentEditor.RemoveNode(attributeList);

        // Remove all the initializers from existing fields/props the params are assigned to.
        foreach (var (parameter, (fieldOrProperty, initializer)) in parameterToExistingFieldOrProperty)
        {
            if (initializer.Parent is PropertyDeclarationSyntax propertyDeclaration)
            {
                mainDocumentEditor.ReplaceNode(
                    propertyDeclaration,
                    propertyDeclaration
                        .WithInitializer(null)
                        .WithSemicolonToken(default)
                        .WithTrailingTrivia(propertyDeclaration.GetTrailingTrivia()));
            }
            else
            {
                mainDocumentEditor.RemoveNode(initializer);
            }
        }

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
                currentTypeDeclaration = RewriteParamRefNodes(currentTypeDeclaration);

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
                    constructorDeclaration = constructorDeclaration
                        .WithPrependedLeadingTrivia(ElasticCarriageReturnLineFeed, ElasticCarriageReturnLineFeed);

                    return currentTypeDeclaration.WithMembers(
                        currentTypeDeclaration.Members.Insert(lastFieldOrProperty + 1, constructorDeclaration));
                }

                // Nothing at all.  Just place the construct at the top of the type.
                return currentTypeDeclaration.WithMembers(
                    currentTypeDeclaration.Members.Insert(0, constructorDeclaration));
            });

        return solutionEditor.GetChangedSolution();

        async Task<MultiDictionary<IParameterSymbol, IdentifierNameSyntax>> GetParameterReferencesAsync()
        {
            var result = new MultiDictionary<IParameterSymbol, IdentifierNameSyntax>();
            var documentsToSearch = namedType.DeclaringSyntaxReferences
                .Select(r => r.SyntaxTree)
                .Distinct()
                .Select(solution.GetRequiredDocument)
                .ToImmutableHashSet();

            foreach (var parameter in parameters)
            {
                //if (parameterToSynthesizedFields.TryGetValue(parameter, out var field))
                //    continue;

                var references = await SymbolFinder.FindReferencesAsync(
                    parameter, solution, documentsToSearch, cancellationToken).ConfigureAwait(false);
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

                        if (referenceLocation.Location.FindNode(findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken) is not IdentifierNameSyntax identifierName)
                            continue;

                        // Explicitly ignore references in the base-type-list.  Tehse don't need to be rewritten as
                        // they will still reference the parameter in the new constructor when we make the `:
                        // base(...)` initializer.
                        if (identifierName.GetAncestor<PrimaryConstructorBaseTypeSyntax>() != null)
                            continue;

                        // Don't need to update doc comment reference (e.g. `paramref=...`).  These will move to the
                        // new constructor and will still reference the parameters there.
                        if (identifierName.GetAncestor<DocumentationCommentTriviaSyntax>() != null)
                            continue;

                        result.Add(parameter, identifierName);
                    }
                }
            }

            return result;
        }

        async Task RewriteReferencesToParametersAsync()
        {
            foreach (var (parameter, references) in parameterReferences)
            {
                if (!parameterToSynthesizedFields.TryGetValue(parameter, out var field))
                    continue;

                var fieldName = field.Name.ToIdentifierName();

                foreach (var grouping in references.GroupBy(r => r.SyntaxTree))
                {
                    var syntaxTree = grouping.Key;
                    var editor = await solutionEditor.GetDocumentEditorAsync(solution.GetDocumentId(syntaxTree), cancellationToken).ConfigureAwait(false);

                    foreach (var identifierName in grouping)
                        editor.ReplaceNode(identifierName, fieldName.WithTriviaFrom(identifierName));
                }
            }
        }

        async Task<ImmutableDictionary<IParameterSymbol, (ISymbol fieldOrProperty, EqualsValueClauseSyntax initializer)>> GetExistingAssignedFieldsOrProperties()
        {
            using var _1 = PooledDictionary<IParameterSymbol, EqualsValueClauseSyntax>.GetInstance(out var parameterToMemberInitializer);

            foreach (var (parameter, references) in parameterReferences)
            {
                // If this already has a synthesized parameter it is assigned to, there's definitely no field/prop it's assigned to.
                if (parameterToSynthesizedFields.TryGetValue(parameter, out _))
                    continue;

                // only care if there is a single reference to the parameter and it's an initializer.
                var initializers = references
                    .Select(r => r.AncestorsAndSelf().OfType<EqualsValueClauseSyntax>().LastOrDefault())
                    .ToSet();
                if (initializers.Count != 1)
                    continue;

                var initializer = initializers.Single();
                if (initializer is null)
                    continue;

                if (initializer.Parent is not PropertyDeclarationSyntax and not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax } })
                    continue;

                parameterToMemberInitializer.Add(parameter, initializer);
            }

            using var _2 = PooledDictionary<IParameterSymbol, (ISymbol fieldOrProperty, EqualsValueClauseSyntax initializer)>.GetInstance(out var result);
            foreach (var grouping in parameterToMemberInitializer.GroupBy(kvp => kvp.Value.SyntaxTree))
            {
                var syntaxTree = grouping.Key;
                var semanticModel = await solution.GetRequiredDocument(syntaxTree).GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                foreach (var (parameter, initializer) in grouping)
                {
                    var fieldOrProperty = semanticModel.GetRequiredDeclaredSymbol(initializer.GetRequiredParent(), cancellationToken);
                    result.Add(parameter, (fieldOrProperty, initializer));
                }
            }

            return result.ToImmutableDictionary();
        }

        async Task<ImmutableDictionary<IParameterSymbol, IFieldSymbol>> GetSynthesizedFieldsAsync()
        {
            using var _1 = PooledDictionary<Location, IFieldSymbol>.GetInstance(out var locationToField);
            using var _2 = PooledDictionary<IParameterSymbol, IFieldSymbol>.GetInstance(out var result);

            foreach (var member in namedType.GetMembers())
            {
                if (member is IFieldSymbol { IsImplicitlyDeclared: false, Locations: [var location, ..] } field)
                    locationToField[location] = field;
            }

            foreach (var parameter in parameters)
            {
                if (parameter.Locations is [var location, ..] &&
                    locationToField.TryGetValue(location, out var existingField))
                {
                    var synthesizedField = CodeGenerationSymbolFactory.CreateFieldSymbol(
                        existingField,
                        name: await MakeFieldNameAsync(parameter.Name).ConfigureAwait(false));

                    result.Add(parameter, synthesizedField);
                }
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
            var attributes = List(methodTargetingAttributes.Select(a => a.WithTarget(null).WithAdditionalAnnotations(Formatter.Annotation)));
            using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var assignmentStatements);
            foreach (var parameter in parameters)
            {
                if (GetMemberToAssignTo(parameter) is not (var member, var value))
                    continue;

                var fieldName = member.Name.ToIdentifierName();
                var left = parameter.Name == member.Name
                    ? MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), fieldName)
                    : (ExpressionSyntax)fieldName;
                var assignment = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, value);
                assignmentStatements.Add(ExpressionStatement(assignment));
            }

            return ConstructorDeclaration(
                attributes,
                TokenList(Token(SyntaxKind.PublicKeyword).WithAppendedTrailingTrivia(Space)),
                typeDeclaration.Identifier.WithoutTrivia(),
                RewriteParameterDefaults(parameterList).WithoutTrivia(),
                baseType?.ArgumentList is null ? null : ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, baseType.ArgumentList),
                Block(assignmentStatements));
        }

        ParameterListSyntax RewriteParameterDefaults(ParameterListSyntax parameterList)
        {
            return parameterList.ReplaceNodes(
                parameterList.Parameters,
                (parameter, _) => RewriteNestedReferences(parameter));
        }

        TNode RewriteNestedReferences<TNode>(TNode parent) where TNode : SyntaxNode
        {
            return parent.ReplaceNodes(
                parent.DescendantNodes().Where(n => n is MemberAccessExpressionSyntax or QualifiedNameSyntax),
                (node, _) =>
                {
                    if (node is MemberAccessExpressionSyntax memberAccessExpression &&
                        namedType.Equals(semanticModel.GetSymbolInfo(memberAccessExpression.Expression).Symbol))
                    {
                        return memberAccessExpression.Name.WithTriviaFrom(node);
                    }
                    else if (node is QualifiedNameSyntax qualifiedName &&
                        namedType.Equals(semanticModel.GetSymbolInfo(qualifiedName.Left).Symbol))
                    {
                        return qualifiedName.Right.WithTriviaFrom(node);
                    }

                    return node;
                });
        }

        (ISymbol member, ExpressionSyntax value)? GetMemberToAssignTo(IParameterSymbol parameter)
        {
            if (parameterToSynthesizedFields.TryGetValue(parameter, out var field))
                return (field, parameter.Name.ToIdentifierName());

            if (parameterToExistingFieldOrProperty.TryGetValue(parameter, out var member))
                return (member.fieldOrProperty, member.initializer.Value);

            return null;
        }

        TypeDeclarationSyntax RewriteParamRefNodes(TypeDeclarationSyntax typeDeclaration)
        {

        }
    }
}
