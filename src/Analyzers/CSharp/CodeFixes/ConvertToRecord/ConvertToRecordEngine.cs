﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRecord
{
    internal static class ConvertToRecordEngine
    {
        private const SyntaxRemoveOptions RemovalOptions =
            SyntaxRemoveOptions.KeepExteriorTrivia |
            SyntaxRemoveOptions.KeepDirectives |
            SyntaxRemoveOptions.AddElasticMarker;

        public static async Task<CodeAction?> GetCodeActionAsync(
            Document document, TypeDeclarationSyntax typeDeclaration, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            // any type declared partial requires complex movement, don't offer refactoring
            if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                return null;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not INamedTypeSymbol
                {
                    // if type is an interface we don't want to refactor
                    TypeKind: TypeKind.Class or TypeKind.Struct,
                    // no need to convert if it's already a record
                    IsRecord: false,
                    // records can't be static and so if the class is static we probably shouldn't convert it
                    IsStatic: false,
                } type)
            {
                return null;
            }

            var positionalParameterInfos = PositionalParameterInfo.GetPropertiesForPositionalParameters(
                typeDeclaration.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .AsImmutable(),
                type,
                semanticModel,
                cancellationToken);
            if (positionalParameterInfos.IsEmpty)
                return null;

            var positionalTitle = CSharpCodeFixesResources.Convert_to_positional_record;

            var positional = CodeAction.Create(
                positionalTitle,
                cancellationToken => ConvertToPositionalRecordAsync(
                    document,
                    type,
                    positionalParameterInfos,
                    typeDeclaration,
                    fallbackOptions,
                    cancellationToken),
                nameof(CSharpCodeFixesResources.Convert_to_positional_record));
            // note: when adding nested actions, use string.Format(CSharpFeaturesResources.Convert_0_to_record, type.Name) as title string
            return positional;
        }

        private static async Task<Solution> ConvertToPositionalRecordAsync(
            Document document,
            INamedTypeSymbol type,
            ImmutableArray<PositionalParameterInfo> positionalParameterInfos,
            TypeDeclarationSyntax typeDeclaration,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // first see if we need to re-order our primary constructor parameters.
            var propertiesToAssign = positionalParameterInfos.SelectAsArray(info => info.Symbol);
            var primaryConstructor = typeDeclaration.Members
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(constructor =>
                {
                    var constructorSymbol = (IMethodSymbol)semanticModel
                        .GetRequiredDeclaredSymbol(constructor, cancellationToken);
                    var constructorOperation = (IConstructorBodyOperation)semanticModel
                        .GetRequiredOperation(constructor, cancellationToken);
                    // We want to make sure that each type in the parameter list corresponds
                    // to exactly one positional parameter type, but they don't need to be in the same order.
                    // We can't use something like set equality because some parameter types may be duplicate.
                    // So, we order the types in a consistent way (by name) and then compare the lists of types.
                    return constructorSymbol.Parameters.SelectAsArray(parameter => parameter.Type)
                                    .OrderBy(type => type.Name)
                            .SequenceEqual(propertiesToAssign.SelectAsArray(s => s.Type)
                                    .OrderBy(type => type.Name),
                                SymbolEqualityComparer.Default) &&
                        // make sure that we do all the correct assignments. There may be multiple constructors
                        // that meet the parameter condition but only one actually assigns all properties.
                        // If successful, we set propertiesToAssign in the order of the parameters.
                        ConvertToRecordHelpers.IsSimplePrimaryConstructor(
                            constructorOperation, ref propertiesToAssign, constructorSymbol.Parameters);
                });

            var solutionEditor = new SolutionEditor(document.Project.Solution);
            // we must refactor usages first because usages can appear within the class definition and
            // individual members, and changing a parent first invalidates the tracking done on the child
            await RefactorInitializersAsync(type, solutionEditor, propertiesToAssign, cancellationToken)
                .ConfigureAwait(false);

            var documentEditor = await solutionEditor
                .GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

            // generated hashcode and equals methods compare all instance fields
            // including underlying fields accessed from properties
            // copy constructor generation also uses all fields when copying
            // so we track all the fields to make sure the methods we consider deleting
            // would actually perform the same action as an autogenerated one
            var expectedFields = type
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(field => !field.IsConst && !field.IsStatic)
                .AsImmutable();

            // remove properties we're bringing up to positional params
            // or keep them as overrides and link the positional param to the original property
            foreach (var result in positionalParameterInfos)
            {
                if (result.IsInherited)
                {
                    // skip inherited params because they were declared elsewhere.
                    // We don't need to add or remove a declaration
                    continue;
                }

                var property = result.Declaration;
                if (result.KeepAsOverride)
                {
                    // add an initializer that links the property to the primary constructor parameter
                    documentEditor.ReplaceNode(property, property
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(property.Identifier.WithoutTrivia())))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                }
                else
                {
                    documentEditor.RemoveNode(property);
                }
            }

            // We will fill in defaults when we see the primary constructor again if we saw it in the first place
            var defaults = positionalParameterInfos.SelectAsArray(info => (EqualsValueClauseSyntax?)null);

            foreach (var constructor in typeDeclaration.Members.OfType<ConstructorDeclarationSyntax>())
            {
                // we already did the work to find the primary constructor
                if (constructor.Equals(primaryConstructor))
                {
                    // grab parameter defaults and reorder positional param info
                    // to be in order of primary constructor params
                    positionalParameterInfos = propertiesToAssign
                        .SelectAsArray(symbol => positionalParameterInfos
                            .First(info => info.Symbol.Equals(symbol)));
                    defaults = constructor.ParameterList.Parameters.SelectAsArray(param => param.Default);
                    documentEditor.RemoveNode(constructor);
                }
                else
                {
                    var constructorSymbol = (IMethodSymbol)semanticModel
                        .GetRequiredDeclaredSymbol(constructor, cancellationToken);
                    var constructorOperation = (IConstructorBodyOperation)semanticModel
                        .GetRequiredOperation(constructor, cancellationToken);

                    // check for copy constructor
                    if (constructorSymbol.Parameters.Length == 1 &&
                        constructorSymbol.Parameters[0].Type.Equals(type))
                    {
                        if (ConvertToRecordHelpers.IsSimpleCopyConstructor(
                            constructorOperation, expectedFields, constructorSymbol.Parameters.First()))
                        {
                            documentEditor.RemoveNode(constructor);
                        }
                    }
                    // ignore any constructor that has the same signature as the primary constructor.
                    // If it wasn't already processed as the primary, it's too complex, and will
                    // already produce an error as the signatures conflict. Better to leave as is and show errors.
                    else if (!constructorSymbol.Parameters.Select(parameter => parameter.Type)
                        .SequenceEqual(propertiesToAssign.Select(property => property.Type)))
                    {
                        // non-primary, non-copy constructor, add ": this(...)" initializers to each
                        // and try to use assignments in the body to determine the values, otherwise default or null
                        var expressions = ConvertToRecordHelpers
                            .GetAssignmentValuesForNonPrimaryConstructor(constructorOperation, propertiesToAssign);

                        // go up to the ExpressionStatementSyntax so we take the semicolon and not just the assignment
                        var expressionStatementsToRemove = expressions
                            .Select(expression =>
                                (expression.Parent as AssignmentExpressionSyntax)?.Parent as ExpressionStatementSyntax)
                            .WhereNotNull()
                            .AsImmutable();

                        var modifiedConstructor = constructor
                            .RemoveNodes(expressionStatementsToRemove, RemovalOptions)!
                            .WithInitializer(SyntaxFactory.ConstructorInitializer(
                                    SyntaxKind.ThisConstructorInitializer,
                                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                                    expressions.Select(SyntaxFactory.Argument)))));

                        documentEditor.ReplaceNode(constructor, modifiedConstructor);
                    }
                }
            }

            // get equality operators and potentially remove them
            var equalsOp = (OperatorDeclarationSyntax?)typeDeclaration.Members.FirstOrDefault(member
                => member is OperatorDeclarationSyntax { OperatorToken.RawKind: (int)SyntaxKind.EqualsEqualsToken });
            var notEqualsOp = (OperatorDeclarationSyntax?)typeDeclaration.Members.FirstOrDefault(member
                => member is OperatorDeclarationSyntax { OperatorToken.RawKind: (int)SyntaxKind.ExclamationEqualsToken });
            if (equalsOp != null && notEqualsOp != null)
            {
                var equalsBodyOperation = (IMethodBodyOperation)semanticModel
                    .GetRequiredOperation(equalsOp, cancellationToken);
                var notEqualsBodyOperation = (IMethodBodyOperation)semanticModel
                    .GetRequiredOperation(notEqualsOp, cancellationToken);
                if (ConvertToRecordHelpers.IsDefaultEqualsOperator(equalsBodyOperation) &&
                    ConvertToRecordHelpers.IsDefaultNotEqualsOperator(notEqualsBodyOperation))
                {
                    // they both evaluate to what would be the generated implementation
                    documentEditor.RemoveNode(equalsOp);
                    documentEditor.RemoveNode(notEqualsOp);
                }
            }

            foreach (var method in typeDeclaration.Members.OfType<MethodDeclarationSyntax>())
            {
                var methodSymbol = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(method, cancellationToken);
                var operation = (IMethodBodyOperation)semanticModel.GetRequiredOperation(method, cancellationToken);

                if (methodSymbol.Name == "Clone")
                {
                    // remove clone method as clone is a reserved method name in records
                    documentEditor.RemoveNode(method);
                }
                else if (ConvertToRecordHelpers.IsSimpleHashCodeMethod(
                    semanticModel.Compilation, methodSymbol, operation, expectedFields))
                {
                    documentEditor.RemoveNode(method);
                }
                else if (ConvertToRecordHelpers.IsSimpleEqualsMethod(
                    semanticModel.Compilation, methodSymbol, operation, expectedFields))
                {
                    // the Equals method implementation is fundamentally equivalent to the generated one
                    documentEditor.RemoveNode(method);
                }
            }

            var optionsProvider = await document.GetCodeFixOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var lineFormattingOptions = optionsProvider.GetLineFormattingOptions();

            var modifiedClassTrivia = GetModifiedClassTrivia(
                positionalParameterInfos, typeDeclaration, lineFormattingOptions);

            var propertiesToAddAsParams = positionalParameterInfos.Zip(defaults, (result, @default) =>
            {
                // if inherited we generate nodes and tokens for the type and identifier
                var type = result.IsInherited
                    ? result.Symbol.Type.GenerateTypeSyntax()
                    : result.Declaration.Type;
                var identifier = result.IsInherited
                    ? SyntaxFactory.Identifier(result.Symbol.Name)
                    : result.Declaration.Identifier;

                return SyntaxFactory.Parameter(
                    GetModifiedAttributeListsForProperty(result),
                    modifiers: default,
                    type,
                    identifier,
                    @default: @default);
            });

            // if we have a class, move trivia from class keyword to record keyword
            // if struct, split trivia and leading goes to record keyword, trailing goes to struct keyword
            var recordKeyword = SyntaxFactory.Token(SyntaxKind.RecordKeyword);
            recordKeyword = type.TypeKind == TypeKind.Class
                ? recordKeyword.WithTriviaFrom(typeDeclaration.Keyword)
                : recordKeyword.WithLeadingTrivia(typeDeclaration.Keyword.LeadingTrivia);

            // use the trailing trivia of the last item before the constructor parameter list as the param list trivia
            var constructorTrivia = typeDeclaration.TypeParameterList?.GetTrailingTrivia() ??
                typeDeclaration.Identifier.TrailingTrivia;

            // delete IEquatable if it's explicit because it is implicit on records
            var iEquatable = ConvertToRecordHelpers.GetIEquatableType(semanticModel.Compilation, type);
            var baseList = typeDeclaration.BaseList;
            if (baseList != null)
            {
                var typeList = baseList.Types;

                if (iEquatable != null)
                {
                    var iEquatableItem = typeList.FirstOrDefault(baseItem
                        => iEquatable.Equals(semanticModel.GetTypeInfo(baseItem.Type, cancellationToken).Type));
                    if (iEquatableItem != null)
                    {
                        typeList = typeList.Remove(iEquatableItem);
                    }
                }

                if (typeList.IsEmpty())
                {
                    baseList = null;
                }
                else
                {
                    if (positionalParameterInfos.Any(info => info.IsInherited))
                    {
                        // if we have an inherited param, then we know we're inheriting from
                        // a record with a primary constructor.
                        // something like: public class C : B {...}
                        // where B is: public record B(int Foo, bool Bar);
                        // We created a parameter list with all the properties that shadow the inherited ones.
                        // Now we need to associate the parameters declared in the class
                        // with the ones the base record uses.
                        // Example: public record C(int Foo, int Bar, int OtherProp) : B(Foo, Bar) {...}
                        var baseRecord = typeList.First();
                        var baseTrailingTrivia = baseRecord.Type.GetTrailingTrivia();
                        // get the positional parameters in the order they are declared from the base record
                        var inheritedPositionalParams = PositionalParameterInfo
                            .GetInheritedPositionalParams(type, cancellationToken)
                            .SelectAsArray(prop =>
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(prop.Name)));

                        typeList = typeList.Replace(baseRecord,
                            SyntaxFactory.PrimaryConstructorBaseType(baseRecord.Type.WithoutTrailingTrivia(),
                                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(inheritedPositionalParams))
                                .WithTrailingTrivia(baseTrailingTrivia)));
                    }

                    baseList = baseList.WithTypes(typeList);
                }
            }

            documentEditor.ReplaceNode(typeDeclaration, (declaration, _) =>
                CreateRecordDeclaration(type, (TypeDeclarationSyntax)declaration, modifiedClassTrivia,
                    propertiesToAddAsParams, recordKeyword, constructorTrivia, baseList));

            return solutionEditor.GetChangedSolution();
        }

        private static RecordDeclarationSyntax CreateRecordDeclaration(
            INamedTypeSymbol type,
            TypeDeclarationSyntax typeDeclaration,
            SyntaxTriviaList modifiedClassTrivia,
            IEnumerable<ParameterSyntax> propertiesToAddAsParams,
            SyntaxToken recordKeyword,
            SyntaxTriviaList constructorTrivia,
            BaseListSyntax? baseList)
        {
            // if we have no members, use semicolon instead of braces
            // use default if we don't want it, otherwise use the original token if it exists or a generated one
            SyntaxToken openBrace, closeBrace, semicolon;
            if (typeDeclaration.Members.IsEmpty())
            {
                openBrace = default;
                closeBrace = default;
                semicolon = typeDeclaration.SemicolonToken == default
                    ? SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                    : typeDeclaration.SemicolonToken;
            }
            else
            {
                openBrace = typeDeclaration.OpenBraceToken == default
                    ? SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                    : typeDeclaration.OpenBraceToken;
                closeBrace = typeDeclaration.CloseBraceToken == default
                    ? SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                    : typeDeclaration.CloseBraceToken;
                semicolon = default;

                // remove any potential leading blank lines right after the class declaration, as we could have
                // something like a method which was spaced out from the previous properties, but now shouldn't
                // have that leading space
                typeDeclaration = typeDeclaration.ReplaceNode(
                    typeDeclaration.Members[0], typeDeclaration.Members[0].GetNodeWithoutLeadingBlankLines());
            }

            return SyntaxFactory.RecordDeclaration(
                type.TypeKind == TypeKind.Class
                    ? SyntaxKind.RecordDeclaration
                    : SyntaxKind.RecordStructDeclaration,
                typeDeclaration.AttributeLists,
                typeDeclaration.Modifiers,
                recordKeyword,
                type.TypeKind == TypeKind.Class
                    ? default
                    : typeDeclaration.Keyword.WithTrailingTrivia(SyntaxFactory.ElasticMarker),
                // remove trailing trivia from places where we would want to insert the parameter list before a line break
                typeDeclaration.Identifier.WithTrailingTrivia(SyntaxFactory.ElasticMarker),
                typeDeclaration.TypeParameterList?.WithTrailingTrivia(SyntaxFactory.ElasticMarker),
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(propertiesToAddAsParams))
                    .WithAppendedTrailingTrivia(constructorTrivia),
                baseList,
                typeDeclaration.ConstraintClauses,
                openBrace,
                typeDeclaration.Members,
                closeBrace,
                semicolon)
                .WithLeadingTrivia(modifiedClassTrivia)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static SyntaxList<AttributeListSyntax> GetModifiedAttributeListsForProperty(PositionalParameterInfo result)
        {
            if (result.IsInherited || result.KeepAsOverride)
            {
                // if the property is declared elsewhere (base class or because we keep the property definition),
                // then any attributes associated with the property don't need to be redeclared
                // on the primary constructor parameter because the primary constructor parameter is no longer the
                // only/first definition. So we can just have an empty attribute list.
                // For example, if we want to move:
                // [SomeAttribute]
                // public int Foo { get; private set; }
                // but then decide that we want to keep the definition, then the attribute can stay on the original
                // definition, and our primary constructor param can associate that attribute when we add:
                // public int Foo { get; private set; } = Foo;
                return SyntaxFactory.List<AttributeListSyntax>();
            }

            return SyntaxFactory.List(result.Declaration.AttributeLists.SelectAsArray(attributeList =>
            {
                if (attributeList.Target == null)
                {
                    // convert attributes attached to the property with no target into "property :" targeted attributes
                    return attributeList
                        .WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.PropertyKeyword)))
                        .WithoutTrivia();
                }
                else
                {
                    return attributeList.WithoutTrivia();
                }
            }));
        }

        private static async Task RefactorInitializersAsync(
            INamedTypeSymbol type,
            SolutionEditor solutionEditor,
            ImmutableArray<IPropertySymbol> positionalParameters,
            CancellationToken cancellationToken)
        {
            var symbolReferences = await SymbolFinder
                .FindReferencesAsync(type, solutionEditor.OriginalSolution, cancellationToken).ConfigureAwait(false);
            var referenceLocations = symbolReferences.SelectMany(reference => reference.Locations);
            var documentLookup = referenceLocations.ToLookup(refLoc => refLoc.Document.Id);
            foreach (var (documentID, documentLocations) in documentLookup)
            {
                var documentEditor = await solutionEditor
                        .GetDocumentEditorAsync(documentID, cancellationToken).ConfigureAwait(false);
                if (documentEditor.OriginalDocument.Project.Language != LanguageNames.CSharp)
                {
                    // since this is a CSharp-dependent file, we need to have specific VB support.
                    // for now skip VB usages.
                    // https://github.com/dotnet/roslyn/issues/63756
                    continue;
                }

                var objectCreationExpressions = documentLocations
                    // we should find the identifier node of an object creation expression
                    .Select(referenceLocations => referenceLocations.Location.FindNode(cancellationToken).Parent)
                    .OfType<ObjectCreationExpressionSyntax>()
                    // order by smaller spans first so in the nested case we don't overwrite our previous changes
                    .OrderBy(node => (node.FullWidth(), node.SpanStart));

                foreach (var objectCreationExpression in objectCreationExpressions)
                {
                    var objectCreationOperation = (IObjectCreationOperation)documentEditor.SemanticModel
                        .GetRequiredOperation(objectCreationExpression, cancellationToken);

                    var expressions = ConvertToRecordHelpers.GetAssignmentValuesFromObjectCreation(
                        objectCreationOperation, positionalParameters);
                    if (expressions.IsEmpty)
                    {
                        continue;
                    }

                    var expressionIndices = expressions.SelectAsArray(
                        // if initializer was null we wouldn't have found expressions
                        // any constructed nodes (default/null) should give -1 because parent is null
                        expression => objectCreationExpression.Initializer!.Expressions.IndexOf(expression.Parent));

                    documentEditor.ReplaceNode(objectCreationExpression, (node, generator) =>
                    {
                        var updatedObjectCreation = (ObjectCreationExpressionSyntax)node;
                        var newInitializer = (InitializerExpressionSyntax)generator
                            .RemoveNodes(updatedObjectCreation.Initializer!,
                                expressionIndices
                                    .Where(i => i != -1)
                                    .Select(i => updatedObjectCreation.Initializer!.Expressions[i]));

                        // if there are no more assignments other than the ones that
                        // could go in the primary constructor, we can remove the block entirely
                        if (newInitializer.Expressions.IsEmpty())
                        {
                            newInitializer = null;
                        }

                        // note: index here is the position in the initializer assignment list of the expression
                        // if it was found at all. The expressions are actually in order of how they should be
                        // supplied as arguments for the primary constructor. 
                        var updatedExpressions = expressions.Zip(expressionIndices, (expression, index) =>
                        {
                            if (index == -1)
                            {
                                // default/null constructed expression
                                return expression;
                            }
                            else
                            {
                                // corresponds to a real node, need to get the updated one
                                var assignmentExpression = (AssignmentExpressionSyntax)
                                    updatedObjectCreation.Initializer!.Expressions[index];
                                return assignmentExpression.Right;
                            }
                        });

                        // replace: new C { Foo = 0; Bar = false; };
                        // with: new C(0, false);
                        return SyntaxFactory.ObjectCreationExpression(
                            updatedObjectCreation.NewKeyword,
                            updatedObjectCreation.Type.WithoutTrailingTrivia(),
                            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(updatedExpressions
                                .Select(expression => SyntaxFactory.Argument(expression.WithoutTrivia())))),
                            newInitializer);
                    });
                }
            }
        }

        #region TriviaMovement
        // format should be:
        // 1. comments and other trivia from class that were already on class
        // 2. comments from each property
        // 3. Class documentation comment summary
        // 4. Property summary documentation (as param)
        // 5. Rest of class documentation comments
        private static SyntaxTriviaList GetModifiedClassTrivia(
            ImmutableArray<PositionalParameterInfo> propertyResults,
            TypeDeclarationSyntax typeDeclaration,
            LineFormattingOptions lineFormattingOptions)
        {
            var classTrivia = typeDeclaration.GetLeadingTrivia().Where(trivia => !trivia.IsWhitespace()).AsImmutable();

            var propertyNonDocComments = propertyResults
                .SelectMany(result =>
                {
                    if (result.IsInherited)
                    {
                        return ImmutableArray<SyntaxTrivia>.Empty;
                    }

                    var p = result.Declaration;
                    var leadingPropTrivia = p.GetLeadingTrivia()
                        .Where(trivia => !trivia.IsDocComment() && !trivia.IsWhitespace());
                    // since we remove attributes and reformat, we want to take any comments
                    // in between attribute and declaration
                    if (!p.AttributeLists.IsEmpty())
                    {
                        // get the leading trivia of the node/token right after
                        // the attribute lists (either modifier or type of property)
                        leadingPropTrivia = leadingPropTrivia.Concat(p.Modifiers.IsEmpty()
                            ? p.Type.GetLeadingTrivia()
                            : p.Modifiers.First().LeadingTrivia);
                    }
                    return leadingPropTrivia;
                })
                .AsImmutable();

            // we use the class doc comment to see if we use single line doc comments or multi line doc comments
            // if the class one isn't found, then we find the first property with a doc comment
            // this variable doubles as a flag to see if we need to generate doc comments at all, as
            // if it is still null, we found no meaningful doc comments anywhere
            var exteriorTrivia = GetExteriorTrivia(typeDeclaration) ??
                propertyResults
                .Where(result => !result.IsInherited)
                .Select(result => GetExteriorTrivia(result.Declaration!))
                .FirstOrDefault(trivia => trivia != null);

            if (exteriorTrivia == null)
            {
                // we didn't find any substantive doc comments, just give the current non-doc comments
                return SyntaxFactory.TriviaList(classTrivia.Concat(propertyNonDocComments).Select(trivia => trivia.AsElastic()));
            }

            var propertyParamComments = CreateParamComments(propertyResults, exteriorTrivia!.Value, lineFormattingOptions);
            var classDocComment = classTrivia.FirstOrNull(trivia => trivia.IsDocComment());
            DocumentationCommentTriviaSyntax newClassDocComment;

            if (classDocComment?.GetStructure() is DocumentationCommentTriviaSyntax originalClassDoc)
            {
                // insert parameters after summary node and the extra newline or at start if no summary
                var summaryIndex = originalClassDoc.Content.IndexOf(node =>
                    node is XmlElementSyntax element &&
                    element.StartTag?.Name.LocalName.ValueText == DocumentationCommentXmlNames.SummaryElementName);

                // if not found, summaryIndex + 1 = -1 + 1 = 0, so our params go to the start
                newClassDocComment = originalClassDoc.WithContent(originalClassDoc.Content
                    .Replace(originalClassDoc.Content[0], originalClassDoc.Content[0])
                    .InsertRange(summaryIndex + 1, propertyParamComments));
            }
            else
            {
                // no class doc comment, if we have non-single line parameter comments we need a start and end
                // we must have had at least one property with a doc comment
                if (propertyResults
                        .SelectAsArray(result => !result.IsInherited,
                            result => result.Declaration!.GetLeadingTrivia().FirstOrNull(trivia => trivia.IsDocComment()))
                        .FirstOrDefault(t => t != null)?.GetStructure() is DocumentationCommentTriviaSyntax propDoc &&
                    propDoc.IsMultilineDocComment())
                {
                    // add /** and */
                    newClassDocComment = SyntaxFactory.DocumentationCommentTrivia(
                        SyntaxKind.MultiLineDocumentationCommentTrivia,
                        // Our parameter method gives a newline (without leading trivia) to start
                        // because we assume we're following some other comment, we replace that newline to add
                        // the start of comment leading trivia as well since we're not following another comment
                        SyntaxFactory.List(propertyParamComments.Skip(1)
                            .Prepend(SyntaxFactory.XmlText(SyntaxFactory.XmlTextNewLine(lineFormattingOptions.NewLine, continueXmlDocumentationComment: false)
                                .WithLeadingTrivia(SyntaxFactory.DocumentationCommentExterior("/**"))
                                .WithTrailingTrivia(exteriorTrivia)))
                            .Append(SyntaxFactory.XmlText(SyntaxFactory.XmlTextNewLine(lineFormattingOptions.NewLine, continueXmlDocumentationComment: false)))),
                            SyntaxFactory.Token(SyntaxKind.EndOfDocumentationCommentToken)
                                .WithTrailingTrivia(SyntaxFactory.DocumentationCommentExterior("*/"), SyntaxFactory.ElasticCarriageReturnLineFeed));
                }
                else
                {
                    // add extra line at end to end doc comment
                    // also skip first newline and replace with non-newline
                    newClassDocComment = SyntaxFactory.DocumentationCommentTrivia(
                        SyntaxKind.MultiLineDocumentationCommentTrivia,
                        SyntaxFactory.List(propertyParamComments.Skip(1)
                            .Prepend(SyntaxFactory.XmlText(SyntaxFactory.XmlTextLiteral(" ").WithLeadingTrivia(exteriorTrivia)))))
                        .WithAppendedTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
                }
            }

            var lastComment = classTrivia.LastOrDefault(trivia => trivia.IsRegularOrDocComment());
            if (classDocComment == null || lastComment == classDocComment)
            {
                // doc comment was last non-whitespace/newline trivia or there was no class doc comment originally
                return SyntaxFactory.TriviaList(classTrivia
                    .Where(trivia => !trivia.IsDocComment())
                    .Concat(propertyNonDocComments)
                    .Append(SyntaxFactory.Trivia(newClassDocComment))
                    .Select(trivia => trivia.AsElastic()));
            }
            else
            {
                // there were comments after doc comment
                return SyntaxFactory.TriviaList(classTrivia
                    .Replace(classDocComment.Value, SyntaxFactory.Trivia(newClassDocComment))
                    .Concat(propertyNonDocComments)
                    .Select(trivia => trivia.AsElastic()));
            }
        }

        private static SyntaxTriviaList? GetExteriorTrivia(SyntaxNode declaration)
        {
            var potentialDocComment = declaration.GetLeadingTrivia().FirstOrNull(trivia => trivia.IsDocComment());

            if (potentialDocComment?.GetStructure() is DocumentationCommentTriviaSyntax docComment)
            {
                // if single line, we return a normal single line trivia, we can format it fine later
                if (docComment.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    // first token of comment should have correct trivia
                    return docComment.GetLeadingTrivia();
                }
                else
                {
                    // for multiline comments, the continuation trivia (usually "*") doesn't get formatted correctly
                    // so we want to keep whitespace alignment across the entire comment
                    return SearchInNodes(docComment.Content);
                }
            }
            return null;
        }

        // potentially recurse into elements to find the first exterior trivia of the element that is after a newline token
        // since we can only find newlines in TextNodes, we need to look inside element contents for text
        private static SyntaxTriviaList? SearchInNodes(SyntaxList<XmlNodeSyntax> nodes)
        {
            foreach (var node in nodes)
            {
                switch (node)
                {
                    case XmlElementSyntax element:
                        var potentialResult = SearchInNodes(element.Content);
                        if (potentialResult != null)
                        {
                            return potentialResult;
                        }
                        break;
                    case XmlTextSyntax text:
                        SyntaxToken prevToken = default;
                        // find first text token after a newline
                        foreach (var token in text.TextTokens)
                        {
                            if (prevToken.IsKind(SyntaxKind.XmlTextLiteralNewLineToken))
                            {
                                return token.LeadingTrivia;
                            }
                            prevToken = token;
                        }
                        break;
                    default:
                        break;
                }
            }
            return null;
        }

        private static IEnumerable<XmlNodeSyntax> CreateParamComments(
            ImmutableArray<PositionalParameterInfo> propertyResults,
            SyntaxTriviaList exteriorTrivia,
            LineFormattingOptions lineFormattingOptions)
        {
            foreach (var result in propertyResults)
            {
                // add an extra line and space with the exterior trivia, so that our params start on the next line and each
                // param goes on a new line with the continuation trivia
                // when adding a new line, the continue flag adds a single line documentation trivia, but we don't necessarily want that
                yield return SyntaxFactory.XmlText(
                    SyntaxFactory.XmlTextNewLine(lineFormattingOptions.NewLine, continueXmlDocumentationComment: false),
                    SyntaxFactory.XmlTextLiteral(" ").WithLeadingTrivia(exteriorTrivia));

                if (result.IsInherited)
                {
                    // generate a param comment with an inherited doc
                    yield return SyntaxFactory.XmlParamElement(result.Symbol.Name, SyntaxFactory.XmlEmptyElement(
                            SyntaxFactory.XmlName(DocumentationCommentXmlNames.InheritdocElementName)));
                }
                else
                {
                    // get the documentation comment
                    var potentialDocComment = result.Declaration.GetLeadingTrivia().FirstOrNull(trivia => trivia.IsDocComment());
                    var paramContent = ImmutableArray<XmlNodeSyntax>.Empty;
                    if (potentialDocComment?.GetStructure() is DocumentationCommentTriviaSyntax docComment)
                    {
                        // get the summary node if there is one
                        var summaryNode = docComment.Content.FirstOrDefault(node =>
                            node is XmlElementSyntax element &&
                            element.StartTag?.Name.LocalName.ValueText == DocumentationCommentXmlNames.SummaryElementName);

                        if (summaryNode != null)
                        {
                            // construct a parameter element from the contents of the property summary
                            // right now we throw away all other documentation parts of the property, because we don't really know where they should go
                            var summaryContent = ((XmlElementSyntax)summaryNode).Content;
                            paramContent = summaryContent.Select((node, index) =>
                            {
                                if (node is XmlTextSyntax text)
                                {
                                    // any text token that is not on it's own line should have replaced trivia
                                    var tokens = text.TextTokens.SelectAsArray(token =>
                                        token.IsKind(SyntaxKind.XmlTextLiteralToken)
                                            ? token.WithLeadingTrivia(exteriorTrivia)
                                            : token);

                                    if (index == 0 &&
                                        tokens is [(kind: SyntaxKind.XmlTextLiteralNewLineToken), _, ..])
                                    {
                                        // remove the starting line and trivia from the first line
                                        tokens = tokens.RemoveAt(0);
                                    }

                                    // remove trivia from first statement because it should never be on a separate line
                                    tokens = tokens.Replace(tokens[0], tokens[0].WithoutLeadingTrivia());

                                    if (index == summaryContent.Count - 1 &&
                                        tokens is [.., (kind: SyntaxKind.XmlTextLiteralNewLineToken), (kind: SyntaxKind.XmlTextLiteralToken) textLiteral] &&
                                        textLiteral.Text.GetFirstNonWhitespaceIndexInString() == -1)
                                    {
                                        // the last text token contains a new line, then a whitespace only text (which would start the closing tag)
                                        // remove the new line and the trivia from the extra text
                                        tokens = tokens.RemoveAt(tokens.Length - 2);
                                        tokens = tokens.Replace(tokens[^1], tokens[^1].WithoutLeadingTrivia());
                                    }

                                    return text.WithTextTokens(SyntaxFactory.TokenList(tokens));
                                }
                                return node;
                            }).AsImmutable();
                        }
                    }

                    yield return SyntaxFactory.XmlParamElement(result.Declaration.Identifier.ValueText, paramContent.AsArray());
                }
            }
        }
        #endregion
    }
}
