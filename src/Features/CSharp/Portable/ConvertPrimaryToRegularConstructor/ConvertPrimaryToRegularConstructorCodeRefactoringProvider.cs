// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
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
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var semanticModels = new ConcurrentSet<SemanticModel>();

        var semanticModel = await GetSemanticModelAsync(document).ConfigureAwait(false);

        var contextInfo = await document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, optionsProvider, cancellationToken).ConfigureAwait(false);

        // The naming rule we need to follow if we synthesize new private fields.
        var fieldNameRule = await document.GetApplicableNamingRuleAsync(
            new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
            DeclarationModifiers.None,
            Accessibility.Private,
            optionsProvider,
            cancellationToken).ConfigureAwait(false);

        // Get the named type and all its parameters for use during the rewrite.
        var namedType = semanticModel.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);
        var parameters = parameterList.Parameters.SelectAsArray(p => semanticModel.GetRequiredDeclaredSymbol(p, cancellationToken));

        // We may have to update multiple files (in the case of a partial type).  Use a solution-editor to make that
        // simple.  We will insert the regular constructor into the partial part containing the primary constructor.
        var solution = document.Project.Solution;
        var solutionEditor = new SolutionEditor(solution);
        var mainDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

        var baseType = typeDeclaration.BaseList?.Types is [PrimaryConstructorBaseTypeSyntax type, ..] ? type : null;
        var methodTargetingAttributes = typeDeclaration.AttributeLists.Where(list => list.Target?.Identifier.ValueText == "method");

        // Find the references to all the parameters.  This will help us determine how they're used and what change we
        // may need to make.
        var parameterReferences = await GetParameterReferencesAsync().ConfigureAwait(false);

        // Determine the fields we'll need to synthesize for each parameter.
        var parameterToSynthesizedFields = CreateSynthesizedFields();

        // Find any field/properties whose initializer references a primary constructor parameter.  These initializers
        // will have to move inside the constructor we generate.
        var initializedFieldsAndProperties = await GetExistingAssignedFieldsOrPropertiesAsync().ConfigureAwait(false);

        var constructorAnnotation = new SyntaxAnnotation();

        // Now go do the entire transformation.

        RemovePrimaryConstructorParameterList();
        RemovePrimaryConstructorBaseTypeArgumentList();
        RemovePrimaryConstructorTargetingAttributes();
        RemoveDirectFieldAndPropertyAssignments();
        AddNewFields();
        AddConstructorDeclaration();
        await RewritePrimaryConstructorParameterReferencesAsync().ConfigureAwait(false);
        FixParameterAndBaseArgumentListIndentation();

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

        ImmutableDictionary<IParameterSymbol, IFieldSymbol> CreateSynthesizedFields()
        {
            using var _1 = PooledDictionary<Location, IFieldSymbol>.GetInstance(out var locationToField);
            using var _2 = PooledDictionary<IParameterSymbol, IFieldSymbol>.GetInstance(out var result);

            // Compiler already knows which primary constructor parameters ended up becoming fields.  So just defer to it.  We'll
            // create real fields for all these cases.

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
                    var baseFieldName = fieldNameRule.NamingStyle.MakeCompliant(parameter.Name).First();
                    var fieldName = NameGenerator.GenerateUniqueName(baseFieldName, n => namedType.Name != n && !namedType.GetMembers(n).Any());

                    var isWrittenTo = parameterReferences[parameter].Any(r => r.IsWrittenTo(semanticModel, cancellationToken));
                    var synthesizedField = CodeGenerationSymbolFactory.CreateFieldSymbol(
                        existingField,
                        modifiers: isWrittenTo ? existingField.GetSymbolModifiers() : existingField.GetSymbolModifiers().WithIsReadOnly(true),
                        name: fieldName);

                    result.Add(parameter, synthesizedField);
                }
            }

            return result.ToImmutableDictionary();
        }

        async Task<ImmutableHashSet<(ISymbol fieldOrProperty, EqualsValueClauseSyntax initializer)>> GetExistingAssignedFieldsOrPropertiesAsync()
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

        void RemovePrimaryConstructorParameterList()
        {
            mainDocumentEditor.RemoveNode(parameterList);
        }

        void RemovePrimaryConstructorBaseTypeArgumentList()
        {
            if (baseType != null)
                mainDocumentEditor.ReplaceNode(baseType, (current, _) => SimpleBaseType(((PrimaryConstructorBaseTypeSyntax)current).Type).WithTriviaFrom(baseType));
        }

        void RemovePrimaryConstructorTargetingAttributes()
        {
            // Remove all the attributes from the type decl that we're moving to the constructor.
            foreach (var attributeList in methodTargetingAttributes)
                mainDocumentEditor.RemoveNode(attributeList);
        }

        void RemoveDirectFieldAndPropertyAssignments()
        {
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
        }

        void AddNewFields()
        {
            mainDocumentEditor.ReplaceNode(
                typeDeclaration,
                (current, _) =>
                {
                    var currentTypeDeclaration = (TypeDeclarationSyntax)current;
                    var fieldsInOrder = parameters
                        .Select(p => parameterToSynthesizedFields.TryGetValue(p, out var field) ? field : null)
                        .WhereNotNull();
                    var codeGenService = document.GetRequiredLanguageService<ICodeGenerationService>();
                    return codeGenService.AddMembers(
                        currentTypeDeclaration, fieldsInOrder, contextInfo, cancellationToken);
                });
        }

        void AddConstructorDeclaration()
        {
            mainDocumentEditor.ReplaceNode(
                typeDeclaration,
                (current, _) =>
                {
                    // Move any <param> tags from the type decl to the constructor decl.
                    var currentTypeDeclaration = (TypeDeclarationSyntax)current;
                    currentTypeDeclaration = RemoveParamXmlElements(currentTypeDeclaration);

                    var constructorDeclaration = CreateConstructorDeclaration().WithAdditionalAnnotations(constructorAnnotation);

                    // If there is an existing non-static constructor, place it before that
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
        }

        async Task RewritePrimaryConstructorParameterReferencesAsync()
        {
            foreach (var (parameter, references) in parameterReferences)
            {
                // Only have to update references if we're synthesizing a field for this parameter.
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

        void FixParameterAndBaseArgumentListIndentation()
        {
            var currentRoot = mainDocumentEditor.GetChangedRoot();
            var formattingOptions = optionsProvider.GetOptions(document.Project.Services).CleanupOptions.FormattingOptions;
            var indentationOptions = new IndentationOptions(formattingOptions);

            var formattedRoot = Formatter.Format(currentRoot, SyntaxAnnotation.ElasticAnnotation, solution.Services, formattingOptions, cancellationToken);

            var constructor = (ConstructorDeclarationSyntax)formattedRoot.GetAnnotatedNodes(constructorAnnotation).Single();

            var rewrittenParameterList = AddElementIndentation(typeDeclaration, constructor, constructor.ParameterList, static list => list.Parameters);
            var initializer = constructor.Initializer;
            var rewrittenInitializer = initializer?.WithArgumentList(AddElementIndentation(typeDeclaration, constructor, initializer.ArgumentList, static list => list.Arguments));

            var rewrittenConstructor = constructor
                .WithParameterList(rewrittenParameterList)
                .WithInitializer(rewrittenInitializer);

            var rewrittenRoot = formattedRoot.ReplaceNode(constructor, rewrittenConstructor);
            mainDocumentEditor.ReplaceNode(mainDocumentEditor.OriginalRoot, rewrittenRoot);
        }

        static TListSyntax AddElementIndentation<TListSyntax>(
            TypeDeclarationSyntax typeDeclaration,
            ConstructorDeclarationSyntax constructorDeclaration,
            TListSyntax list,
            Func<TListSyntax, IEnumerable<SyntaxNode>> getElements)
            where TListSyntax : SyntaxNode
        {
            // Since we're moving parameters from the constructor to the type, attempt to dedent them if appropriate.

            var typeLeadingWhitespace = GetLeadingWhitespace(typeDeclaration);
            var constructorLeadingWhitespace = GetLeadingWhitespace(constructorDeclaration);

            if (constructorLeadingWhitespace.Length > typeLeadingWhitespace.Length &&
                constructorLeadingWhitespace.StartsWith(typeLeadingWhitespace))
            {
                var indentation = constructorLeadingWhitespace[typeLeadingWhitespace.Length..];
                return list.ReplaceNodes(
                    getElements(list),
                    (p, _) =>
                    {
                        var elementLeadingWhitespace = GetLeadingWhitespace(p);
                        if (elementLeadingWhitespace.Length > 0 && elementLeadingWhitespace.StartsWith(typeLeadingWhitespace))
                        {
                            var leadingTrivia = p.GetLeadingTrivia();
                            return p.WithLeadingTrivia(
                                leadingTrivia.Concat(Whitespace(indentation)));
                        }

                        return p;
                    });
            }

            return list;
        }

        static string GetLeadingWhitespace(SyntaxNode node)
            => node.GetLeadingTrivia() is [.., (kind: SyntaxKind.WhitespaceTrivia) whitespace] ? whitespace.ToString() : "";

        ConstructorDeclarationSyntax CreateConstructorDeclaration()
        {
            using var _1 = ArrayBuilder<StatementSyntax>.GetInstance(out var assignmentStatements);

            // First, if we're making a real field for a primary constructor parameter, assign the parameter to it.
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

            // Next, actually assign to all the fields/properties that were previously referencing any primary
            // constructor parameters.
            foreach (var (fieldOrProperty, initializer) in initializedFieldsAndProperties.OrderBy(i => i.initializer.SpanStart))
            {
                var left = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), fieldOrProperty.Name.ToIdentifierName())
                    .WithAdditionalAnnotations(Simplifier.Annotation);
                var assignment = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, initializer.EqualsToken, initializer.Value);
                assignmentStatements.Add(ExpressionStatement(assignment));
            }

            var rewrittenParameters = parameterList.ReplaceNodes(
                parameterList.Parameters,
                (parameter, _) => RewriteNestedReferences(parameter));

            var constructorDeclaration = ConstructorDeclaration(
                List(methodTargetingAttributes.Select(a => a.WithTarget(null).WithoutTrivia().WithAdditionalAnnotations(Formatter.Annotation))),
                TokenList(Token(SyntaxKind.PublicKeyword).WithAppendedTrailingTrivia(Space)),
                typeDeclaration.Identifier.WithoutTrivia(),
                rewrittenParameters.WithoutTrivia(),
                baseType?.ArgumentList is null ? null : ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, baseType.ArgumentList),
                Block(assignmentStatements));

            return WithTypeDeclarationParamDocComments(typeDeclaration, constructorDeclaration);
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
    }
}
