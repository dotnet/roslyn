// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
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
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertPrimaryToRegularConstructor;

using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertPrimaryToRegularConstructor), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class ConvertPrimaryToRegularConstructorCodeRefactoringProvider()
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

        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var semanticModels = new ConcurrentSet<SemanticModel>();

        var semanticModel = await GetSemanticModelAsync(document).ConfigureAwait(false);
        var namedType = semanticModel.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);

        // We may have to update multiple files (in the case of a partial type).  Use a solution-editor to make that simple.
        var solution = document.Project.Solution;
        var solutionEditor = new SolutionEditor(solution);

        var codeGenService = document.GetRequiredLanguageService<ICodeGenerationService>();
        var syntaxGenerator = CSharpSyntaxGenerator.Instance;
        var parameters = parameterList.Parameters.SelectAsArray(p => semanticModel.GetRequiredDeclaredSymbol(p, cancellationToken));

        // Compiler already knows which primary constructor parameters ended up becoming fields.  So just defer to it.  We'll
        // create real fields for all these cases.
        var parameterToSynthesizedFields = await GetSynthesizedFieldsAsync().ConfigureAwait(false);

        var parameterReferences = await GetParameterReferencesAsync().ConfigureAwait(false);

        // Find any field/properties whose initializer references a primary constructor parameter.  These initializers
        // will have to move inside the constructor we generate.
        var initializedFieldsAndProperties = await GetExistingAssignedFieldsOrProperties().ConfigureAwait(false);

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
        foreach (var (_, initializer) in initializedFieldsAndProperties)
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
            else if (initializer.Parent is VariableDeclaratorSyntax)
            {
                mainDocumentEditor.RemoveNode(initializer);
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
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
                currentTypeDeclaration = RemoveParamXmlElements(currentTypeDeclaration);

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
                        .WithPrependedLeadingTrivia(ElasticCarriageReturnLineFeed);

                    return currentTypeDeclaration.WithMembers(
                        currentTypeDeclaration.Members.Insert(lastFieldOrProperty + 1, constructorDeclaration));
                }

                // Nothing at all.  Just place the construct at the top of the type.
                return currentTypeDeclaration.WithMembers(
                    currentTypeDeclaration.Members.Insert(0, constructorDeclaration));
            });

        return solutionEditor.GetChangedSolution();

        async ValueTask<SemanticModel> GetSemanticModelAsync(Document document)
        {
            // Ensure that if we get a semantic model for another document this named type is contained in, that we only
            // produce that semantic model once.
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            semanticModels.Add(semanticModel);
            return semanticModel;
        }

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

                        // Explicitly ignore references in the base-type-list.  These don't need to be rewritten as
                        // they will still reference the parameter in the new constructor when we make the `:
                        // base(...)` initializer.
                        if (identifierName.GetAncestor<PrimaryConstructorBaseTypeSyntax>() != null)
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
                    {
                        var xmlElement = identifierName.AncestorsAndSelf().OfType<XmlEmptyElementSyntax>().FirstOrDefault();
                        if (xmlElement is { Name.LocalName.ValueText: "paramref" })
                        {
                            var seeTag = xmlElement
                                .ReplaceToken(xmlElement.Name.LocalName, Identifier("see").WithTriviaFrom(xmlElement.Name.LocalName))
                                .WithAttributes(SingletonList<XmlAttributeSyntax>(XmlCrefAttribute(
                                    TypeCref(fieldName))));

                            editor.ReplaceNode(xmlElement, seeTag);
                        }
                        else
                        {
                            editor.ReplaceNode(identifierName, fieldName.WithTriviaFrom(identifierName));
                        }
                    }
                }
            }
        }

        async Task<ImmutableHashSet<(ISymbol fieldOrProperty, EqualsValueClauseSyntax initializer)>> GetExistingAssignedFieldsOrProperties()
        {
            using var _1 = PooledHashSet<EqualsValueClauseSyntax>.GetInstance(out var initializers);
            foreach (var (parameter, references) in parameterReferences)
            {
                foreach (var reference in references)
                {
                    var initializer = reference.AncestorsAndSelf().OfType<EqualsValueClauseSyntax>().LastOrDefault();
                    if (initializer is null)
                        continue;

                    if (initializer.Parent is not PropertyDeclarationSyntax and not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax } })
                        continue;

                    initializers.Add(initializer);
                }
            }

            using var _2 = PooledHashSet<(ISymbol fieldOrProperty, EqualsValueClauseSyntax initializer)>.GetInstance(out var result);
            foreach (var grouping in initializers.GroupBy(kvp => kvp.Value.SyntaxTree))
            {
                var syntaxTree = grouping.Key;
                var semanticModel = await GetSemanticModelAsync(solution.GetRequiredDocument(syntaxTree)).ConfigureAwait(false);

                foreach (var initializer in grouping)
                {
                    var fieldOrProperty = semanticModel.GetRequiredDeclaredSymbol(initializer.GetRequiredParent(), cancellationToken);
                    result.Add((fieldOrProperty, initializer));
                }
            }

            return result.ToImmutableHashSet();
        }

        async Task<ImmutableDictionary<IParameterSymbol, IFieldSymbol>> GetSynthesizedFieldsAsync()
        {
            using var _1 = PooledDictionary<Location, IFieldSymbol>.GetInstance(out var locationToField);
            using var _2 = PooledDictionary<IParameterSymbol, IFieldSymbol>.GetInstance(out var result);

            foreach (var member in namedType.GetMembers())
            {
                if (member is IFieldSymbol { IsImplicitlyDeclared: true, Locations: [var location, ..] } field)
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
            using var _1 = ArrayBuilder<StatementSyntax>.GetInstance(out var assignmentStatements);

            // First, if we're making a real field for a primary constructor parameter, assign the parameter to it.
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

            // Next, actually assign to all the fields/properties that were previously referencing any primary
            // constructor parameters.
            foreach (var (fieldOrProperty, initializer) in initializedFieldsAndProperties.OrderBy(i => i.initializer.SpanStart))
            {
                var left = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), fieldOrProperty.Name.ToIdentifierName())
                    .WithAdditionalAnnotations(Simplifier.Annotation);
                var assignment = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, initializer.EqualsToken, initializer.Value);
                assignmentStatements.Add(ExpressionStatement(assignment));
            }

            var constructorDeclaration = ConstructorDeclaration(
                List(methodTargetingAttributes.Select(a => a.WithTarget(null).WithoutTrivia().WithAdditionalAnnotations(Formatter.Annotation))),
                TokenList(Token(SyntaxKind.PublicKeyword).WithAppendedTrailingTrivia(Space)),
                typeDeclaration.Identifier.WithoutTrivia(),
                RewriteParameterDefaults(parameterList).WithoutTrivia(),
                baseType?.ArgumentList is null ? null : ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, baseType.ArgumentList),
                Block(assignmentStatements));

            // Now move the param tags on the type decl over to the constructor.
            var triviaList = typeDeclaration.GetLeadingTrivia();
            var trivia = GetDocComment(triviaList);
            var docComment = GetDocCommentStructure(trivia);
            if (docComment != null)
            {
                using var _2 = ArrayBuilder<XmlNodeSyntax>.GetInstance(out var content);

                for (int i = 0, n = docComment.Content.Count; i < n; i++)
                {
                    var node = docComment.Content[i];
                    if (IsXmlElement(node, "param", out var paramElement))
                    {
                        content.Add(node);

                        // if the param tag is followed with a newline, then preserve that when transferring over.
                        if (i + 1 < docComment.Content.Count && IsDocCommentNewLine(docComment.Content[i + 1]))
                            content.Add(docComment.Content[i + 1]);
                    }
                }

                if (content.Count > 0)
                {
                    if (!content[0].GetLeadingTrivia().Any(SyntaxKind.DocumentationCommentExteriorTrivia))
                        content[0] = content[0].WithLeadingTrivia(DocumentationCommentExterior("/// "));

                    content[^1] = content[^1].WithTrailingTrivia(EndOfLine(""));

                    var finalTrivia = DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, List(content));
                    return constructorDeclaration.WithLeadingTrivia(Trivia(finalTrivia));
                }
            }

            return constructorDeclaration;
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

            return null;
        }
    }
}
