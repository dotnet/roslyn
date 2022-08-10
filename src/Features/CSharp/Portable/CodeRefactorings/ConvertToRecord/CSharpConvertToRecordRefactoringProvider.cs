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
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Features;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToRecord
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRecord), Shared]
    internal sealed class CSharpConvertToRecordRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConvertToRecordRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var typeDeclaration = await context.TryGetRelevantNodeAsync<TypeDeclarationSyntax>().ConfigureAwait(false);
            if (typeDeclaration == null ||
                // any type declared partial requires complex movement, don't offer refactoring
                typeDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
            {
                return;
            }

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
                return;
            }

            var propertyAnalysisResults = PropertyAnalysisResult.AnalyzeProperties(
                typeDeclaration.Members
                    .Where(member => member is PropertyDeclarationSyntax)
                    .Cast<PropertyDeclarationSyntax>()
                    .AsImmutable(),
                type,
                semanticModel,
                cancellationToken);
            if (propertyAnalysisResults.IsEmpty)
            {
                return;
            }

            var positionalTitle = CSharpFeaturesResources.Convert_to_positional_record;

            var positional = CodeAction.Create(
                positionalTitle,
                cancellationToken => ConvertToPositionalRecordAsync(
                    document,
                    type,
                    propertyAnalysisResults,
                    typeDeclaration,
                    cancellationToken),
                nameof(CSharpFeaturesResources.Convert_to_positional_record));
            // note: when adding nested actions, use string.Format(CSharpFeaturesResources.Convert_0_to_record, type.Name) as title string
            context.RegisterRefactoring(positional);
        }

        private static async Task<Document> ConvertToPositionalRecordAsync(
            Document document,
            INamedTypeSymbol originalType,
            ImmutableArray<PropertyAnalysisResult> propertyAnalysisResults,
            TypeDeclarationSyntax originalDeclarationNode,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            // properties to be added to primary constructor parameters
            var propertiesToMove = propertyAnalysisResults.SelectAsArray(result => result.Syntax);

            var lineFormattingOptions = await document
                .GetLineFormattingOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);
            var modifiedClassTrivia = GetModifiedClassTrivia(propertiesToMove, originalDeclarationNode, lineFormattingOptions);

            var propertiesToAddAsParams = propertiesToMove.SelectAsArray(p =>
                SyntaxFactory.Parameter(
                    GetModifiedAttributeListsForProperty(p),
                    modifiers: default,
                    p.Type,
                    p.Identifier,
                    @default: null));

            // remove converted properties and reformat other methods
            var membersToKeep = GetModifiedMembersForPositionalRecord(
                originalDeclarationNode, originalType, semanticModel, propertyAnalysisResults, cancellationToken);

            // if we have a class, move trivia from class keyword to record keyword
            // if struct, split trivia and leading goes to record keyword, trailing goes to struct keyword
            var recordKeyword = SyntaxFactory.Token(SyntaxKind.RecordKeyword);
            recordKeyword = originalType.TypeKind == TypeKind.Class
                ? recordKeyword.WithTriviaFrom(originalDeclarationNode.Keyword)
                : recordKeyword.WithLeadingTrivia(originalDeclarationNode.Keyword.LeadingTrivia);

            // use the trailing trivia of the last item before the constructor parameter list as the param list trivia
            var constructorTrivia = originalDeclarationNode.TypeParameterList?.GetTrailingTrivia() ??
                originalDeclarationNode.Identifier.TrailingTrivia;

            // if we have no members, use semicolon instead of braces
            // use default if we don't want it, otherwise use the original token if it exists or a generated one
            SyntaxToken openBrace, closeBrace, semicolon;
            if (membersToKeep.IsEmpty)
            {
                openBrace = default;
                closeBrace = default;
                semicolon = originalDeclarationNode.SemicolonToken == default
                    ? SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                    : originalDeclarationNode.SemicolonToken;
            }
            else
            {
                openBrace = originalDeclarationNode.OpenBraceToken == default
                    ? SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                    : originalDeclarationNode.OpenBraceToken;
                closeBrace = originalDeclarationNode.CloseBraceToken == default
                    ? SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                    : originalDeclarationNode.CloseBraceToken;
                semicolon = default;
            }

            var changedTypeDeclaration = SyntaxFactory.RecordDeclaration(
                    originalType.TypeKind == TypeKind.Class
                        ? SyntaxKind.RecordDeclaration
                        : SyntaxKind.RecordStructDeclaration,
                    originalDeclarationNode.AttributeLists,
                    originalDeclarationNode.Modifiers,
                    recordKeyword,
                    originalType.TypeKind == TypeKind.Class
                        ? default
                        : originalDeclarationNode.Keyword.WithTrailingTrivia(SyntaxFactory.ElasticMarker),
                    // remove trailing trivia from places where we would want to insert the parameter list before a line break
                    originalDeclarationNode.Identifier.WithTrailingTrivia(SyntaxFactory.ElasticMarker),
                    originalDeclarationNode.TypeParameterList?.WithTrailingTrivia(SyntaxFactory.ElasticMarker),
                    SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(propertiesToAddAsParams))
                        .WithAppendedTrailingTrivia(constructorTrivia),
                    originalDeclarationNode.BaseList,
                    originalDeclarationNode.ConstraintClauses,
                    openBrace,
                    SyntaxFactory.List(membersToKeep),
                    closeBrace,
                    semicolon)
                    .WithLeadingTrivia(modifiedClassTrivia)
                    .WithAdditionalAnnotations(Formatter.Annotation);

            var changedDocument = await document.ReplaceNodeAsync(
                originalDeclarationNode, changedTypeDeclaration, cancellationToken).ConfigureAwait(false);

            return changedDocument;
        }

        private static SyntaxList<AttributeListSyntax> GetModifiedAttributeListsForProperty(PropertyDeclarationSyntax p)
            => SyntaxFactory.List(p.AttributeLists.SelectAsArray(attributeList =>
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

        /// <summary>
        /// Removes or modifies members in preparation of adding to a record with a primary constructor (positional parameters)
        /// Deletes properties that we move to positional params
        /// Deletes methods, constructors, and operators that would be generated by default if we believe they currently have a
        /// similar effect to the generated ones
        /// modifies constructors and some method modifiers to fall in line with record requirements (e.g. this() initializer)
        /// </summary>
        /// <param name="typeDeclaration">Original type declaration</param>
        /// <param name="semanticModel">Semantic model</param>
        /// <param name="propertiesToMove">Properties we decided to move</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// 
        /// <returns>The list of members from the original type, modified and trimmed for a positional record type usage</returns>
        private static ImmutableArray<MemberDeclarationSyntax> GetModifiedMembersForPositionalRecord(
            TypeDeclarationSyntax typeDeclaration,
            INamedTypeSymbol type,
            SemanticModel semanticModel,
            ImmutableArray<PropertyAnalysisResult> propertiesToMove,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<MemberDeclarationSyntax>.GetInstance(out var modifiedMembers);
            modifiedMembers.AddRange(typeDeclaration.Members);

            // generated hashcode and equals methods compare all instance fields, not just properties
            // including underlying fields accessed from properties
            // copy constructor generation also uses all fields when copying
            var expectedFields = type
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(field => !field.IsConst && !field.IsStatic)
                .AsImmutable();

            // remove properties we're bringing up to positional params
            // or keep them as overrides and link the positional param to the original property
            foreach (var result in propertiesToMove)
            {
                var property = result.Syntax;
                if (result.KeepAsOverride)
                {
                    // add an initializer that links the property to the primary constructor parameter
                    modifiedMembers[modifiedMembers.IndexOf(property)] = property
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(property.Identifier.WithoutTrivia())))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                }
                else
                {
                    modifiedMembers.Remove(property);
                }
            }

            // get all the constructors so we can add an initializer to them
            // or potentially delete the primary constructor
            var constructors = modifiedMembers.OfType<ConstructorDeclarationSyntax>().AsImmutable();
            foreach (var constructor in constructors)
            {
                // check to see if it would override the primary constructor
                var constructorSymbol = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(constructor, cancellationToken);
                var constructorOperation = (IMethodBodyOperation)semanticModel.GetRequiredOperation(constructor, cancellationToken);
                var constructorParamTypes = constructorSymbol.Parameters.SelectAsArray(parameter => parameter.Type);
                var positionalParamTypes = propertiesToMove.SelectAsArray(p => p.Symbol.Type);
                if (constructorParamTypes.SequenceEqual(positionalParamTypes))
                {
                    // found a primary constructor override, now check if we are pretty sure we can remove it safely
                    if (CSharpOperationAnalysisHelpers.IsSimplePrimaryConstructor(constructorOperation,
                        propertiesToMove.SelectAsArray(result => result.Symbol),
                        constructorSymbol.Parameters))
                    {
                        modifiedMembers.Remove(constructor);
                    }
                    else
                    {
                        // can't remove it safely, at least add a warning that it will produce an error
                        modifiedMembers[modifiedMembers.IndexOf(constructor)] = constructor
                            .WithAdditionalAnnotations(WarningAnnotation.Create(
                                CSharpFeaturesResources.Warning_constructor_signature_conflicts_with_primary_constructor));
                    }
                }
                // check for copy constructor
                else if (constructorSymbol.Parameters.IsSingle() &&
                    constructorSymbol.Parameters.First().Type.Equals(type))
                {
                    if (CSharpOperationAnalysisHelpers.IsSimpleCopyConstructor(constructorOperation,
                        expectedFields,
                        constructorSymbol.Parameters.First()))
                    {
                        modifiedMembers.Remove(constructor);
                    }
                }
                else
                {
                    // non-primary constructor, add ": this(default, default...)" initializers to each
                    var thisArgs = SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            Enumerable.Repeat(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                                propertiesToMove.Length)));

                    modifiedMembers[modifiedMembers.IndexOf(constructor)] = constructor
                        .WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, thisArgs));
                }
            }

            // get equality operators and potentially remove them
            var equalsOp = (OperatorDeclarationSyntax?)modifiedMembers.FirstOrDefault(member
                => member is OperatorDeclarationSyntax { OperatorToken.RawKind: (int)SyntaxKind.EqualsEqualsToken });
            var notEqualsOp = (OperatorDeclarationSyntax?)modifiedMembers.FirstOrDefault(member
                => member is OperatorDeclarationSyntax { OperatorToken.RawKind: (int)SyntaxKind.ExclamationEqualsToken });
            if (equalsOp != null && notEqualsOp != null)
            {
                var equalsBodyOperation = (IMethodBodyOperation)semanticModel
                    .GetRequiredOperation(equalsOp, cancellationToken);
                var notEqualsBodyOperation = (IMethodBodyOperation)semanticModel
                    .GetRequiredOperation(notEqualsOp, cancellationToken);
                if (CSharpOperationAnalysisHelpers.IsDefaultEqualsOperator(equalsBodyOperation) &&
                    CSharpOperationAnalysisHelpers.IsDefaultNotEqualsOperator(notEqualsBodyOperation))
                {
                    // they both evaluate to what would be the generated implementation
                    modifiedMembers.Remove(equalsOp);
                    modifiedMembers.Remove(notEqualsOp);
                }
            }

            var methods = modifiedMembers.OfType<MethodDeclarationSyntax>().AsImmutable();

            foreach (var method in methods)
            {
                var methodSymbol = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(method, cancellationToken);
                var operation = (IMethodBodyOperation)semanticModel.GetRequiredOperation(method, cancellationToken);

                if (methodSymbol.Name == "Clone")
                {
                    // remove clone method as clone is a reserved method name in records
                    modifiedMembers.Remove(method);
                }
                else if (CSharpOperationAnalysisHelpers.IsSimpleHashCodeMethod(
                    semanticModel.Compilation, methodSymbol, operation, expectedFields))
                {
                    modifiedMembers.Remove(method);
                }
                else if (CSharpOperationAnalysisHelpers.IsSimpleEqualsMethod(
                    semanticModel.Compilation, methodSymbol, operation, expectedFields))
                {
                    // the Equals method implementation is fundamentally equivalent to the generated one
                    modifiedMembers.Remove(method);
                }
            }

            return modifiedMembers.ToImmutable();
        }

        #region TriviaMovement
        // format should be:
        // 1. comments and other trivia from class that were already on class
        // 2. comments from each property
        // 3. Class documentation comment summary
        // 4. Property summary documentation (as param)
        // 5. Rest of class documentation comments
        private static SyntaxTriviaList GetModifiedClassTrivia(
            ImmutableArray<PropertyDeclarationSyntax> properties,
            TypeDeclarationSyntax typeDeclaration,
            LineFormattingOptions lineFormattingOptions)
        {
            var classTrivia = typeDeclaration.GetLeadingTrivia().Where(trivia => !trivia.IsWhitespace()).AsImmutable();

            var propertyNonDocComments = properties
                .SelectMany(p =>
                {
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
                properties.SelectAsArray(GetExteriorTrivia).FirstOrDefault(t => t != null);

            if (exteriorTrivia == null)
            {
                // we didn't find any substantive doc comments, just give the current non-doc comments
                return SyntaxFactory.TriviaList(classTrivia.Concat(propertyNonDocComments).Select(trivia => trivia.AsElastic()));
            }

            var propertyParamComments = CreateParamComments(properties, exteriorTrivia!.Value, lineFormattingOptions);
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
                if (properties
                        .SelectAsArray(p => p.GetLeadingTrivia().FirstOrNull(trivia => trivia.IsDocComment()))
                        .Where(t => t != null)
                        .First()?.GetStructure() is DocumentationCommentTriviaSyntax propDoc &&
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

            // potentially recurse into elements to find the first exterior trivia of the element that is after a newline token
            // since we can only find newlines in TextNodes, we need to look inside element contents for text
            static SyntaxTriviaList? SearchInNodes(SyntaxList<XmlNodeSyntax> nodes)
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
        }

        private static IEnumerable<XmlNodeSyntax> CreateParamComments(
            ImmutableArray<PropertyDeclarationSyntax> properties,
            SyntaxTriviaList exteriorTrivia,
            LineFormattingOptions lineFormattingOptions)
        {
            foreach (var property in properties)
            {
                // get the documentation comment
                var potentialDocComment = property.GetLeadingTrivia().FirstOrNull(trivia => trivia.IsDocComment());
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
                                    tokens.Length >= 2 &&
                                    tokens[0].IsKind(SyntaxKind.XmlTextLiteralNewLineToken))
                                {
                                    // remove the starting line and trivia from the first line
                                    tokens = tokens.RemoveAt(0);
                                    tokens = tokens.Replace(tokens[0], tokens[0].WithoutLeadingTrivia());
                                }

                                if (index == summaryContent.Count - 1 &&
                                    tokens.Length >= 2 &&
                                    tokens[^1].IsKind(SyntaxKind.XmlTextLiteralToken) &&
                                    tokens[^1].Text.GetFirstNonWhitespaceIndexInString() == -1 &&
                                    tokens[^2].IsKind(SyntaxKind.XmlTextLiteralNewLineToken))
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

                // add an extra line and space with the exterior trivia, so that our params start on the next line and each
                // param goes on a new line with the continuation trivia
                // when adding a new line, the continue flag adds a single line documentation trivia, but we don't necessarily want that
                yield return SyntaxFactory.XmlText(
                    SyntaxFactory.XmlTextNewLine(lineFormattingOptions.NewLine, continueXmlDocumentationComment: false),
                    SyntaxFactory.XmlTextLiteral(" ").WithLeadingTrivia(exteriorTrivia));
                yield return SyntaxFactory.XmlParamElement(property.Identifier.ValueText, paramContent.AsArray());
            }
        }
        #endregion
    }
}
