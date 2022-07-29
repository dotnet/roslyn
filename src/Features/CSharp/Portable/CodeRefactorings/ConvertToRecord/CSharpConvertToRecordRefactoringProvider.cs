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
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
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
            if (typeDeclaration == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not INamedTypeSymbol
                {
                    // if type is an interface we don't want to refactor
                    TypeKind: TypeKind.Class or TypeKind.Struct,
                    // no need to convert, already a record
                    // TODO: when adding other options it might make sense to convert a postional record to a non-positional one and vice versa
                    IsRecord: false,
                    // records can't be static and so if the class is static we probably shouldn't convert it
                    IsStatic: false

                } type ||
                // make sure that there is at least one positional parameter we can introduce
                !typeDeclaration.Members.Any(m => ShouldConvertProperty(m, type)))
            {
                return;
            }

            var positionalTitle = CSharpFeaturesResources.Convert_to_positional_record;

            var positional = CodeAction.Create(
                positionalTitle,
                cancellationToken => ConvertToParameterRecordAsync(document, type, typeDeclaration, cancellationToken),
                positionalTitle);
            // note: when adding nested actions, use string.Format(CSharpFeaturesResources.Convert_0_to_record, type.Name) as title string
            context.RegisterRefactoring(positional);
        }

        private static async Task<Document> ConvertToParameterRecordAsync(
            Document document,
            INamedTypeSymbol originalType,
            TypeDeclarationSyntax originalDeclarationNode,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var propertiesToMove = originalDeclarationNode.Members
                .Where(m => ShouldConvertProperty(m, originalType))
                .Cast<PropertyDeclarationSyntax>()
                .AsImmutable();

            var modifiedClassTrivia = GetModifiedClassTrivia(propertiesToMove, originalDeclarationNode);

            var propertiesToAddAsParams = propertiesToMove.SelectAsArray(p =>
                SyntaxFactory.Parameter(
                    GetModifiedAttributeListsForProperty(p),
                    modifiers: default,
                    p.Type,
                    p.Identifier,
                    @default: null));

            // remove converted properties and reformat other methods
            var membersToKeep = GetModifiedMembersForPositionalRecord(
                originalType, originalDeclarationNode, semanticModel, propertiesToMove, cancellationToken);

            // if we have a class, move trivia from class keyword to record keyword
            // if struct, split trivia and leading goes to record, trailing goes to struct
            var recordKeyword = SyntaxFactory.Token(SyntaxKind.RecordKeyword);
            recordKeyword = originalType.TypeKind == TypeKind.Class
                    ? recordKeyword.WithTriviaFrom(originalDeclarationNode.Keyword)
                    : recordKeyword.WithLeadingTrivia(originalDeclarationNode.Keyword.LeadingTrivia);

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
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(propertiesToAddAsParams)),
                // TODO: inheritance in the case where we inherit from a record (because we previously activated on one)
                originalDeclarationNode.BaseList,
                originalDeclarationNode.ConstraintClauses,
                originalDeclarationNode.OpenBraceToken,
                SyntaxFactory.List(membersToKeep),
                originalDeclarationNode.CloseBraceToken,
                semicolonToken: default)
                .NormalizeWhitespace()
                .WithLeadingTrivia(modifiedClassTrivia)
                .WithAdditionalAnnotations(Formatter.Annotation);

            // if we have no members, use semicolon instead
            if (membersToKeep.IsEmpty)
            {
                changedTypeDeclaration = changedTypeDeclaration
                    .WithOpenBraceToken(default)
                    .WithCloseBraceToken(default)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            var changedDocument = await document.ReplaceNodeAsync(
                originalDeclarationNode, changedTypeDeclaration, cancellationToken).ConfigureAwait(false);

            return changedDocument;
        }

        private static bool ShouldConvertProperty(MemberDeclarationSyntax member, INamedTypeSymbol containingType)
        {
            if (member is not PropertyDeclarationSyntax property)
            {
                return false;
            }

            if (property.Initializer != null || property.ExpressionBody != null)
            {
                return false;
            }

            var propAccessibility = CSharpAccessibilityFacts.Instance.GetAccessibility(member);

            // more restrictive than internal (protected, private, private protected, or unspecified (private by default))
            if (propAccessibility < Accessibility.Internal)
            {
                return false;
            }

            // no accessors declared
            if (property.AccessorList == null)
            {
                return false;
            }

            // for class records and readonly struct records, properties should be get; init; only
            // for non-readonly structs, they should be get; set;
            // neither should have bodies (as it indicates more complex functionality)
            var accessors = property.AccessorList.Accessors;

            if (accessors.Any(a => a.Body != null || a.ExpressionBody != null) &&
                !accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration))
            {
                return false;
            }

            if (containingType.TypeKind == TypeKind.Struct && !containingType.IsReadOnly)
            {
                if (!accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)))
                {
                    return false;
                }
            }
            else
            {
                // either we are a class or readonly struct
                if (!accessors.Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)))
                {
                    return false;
                }
            }

            return true;
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

        private static ImmutableArray<MemberDeclarationSyntax> GetModifiedMembersForPositionalRecord(
            INamedTypeSymbol type,
            TypeDeclarationSyntax classDeclaration,
            SemanticModel semanticModel,
            ImmutableArray<PropertyDeclarationSyntax> propertiesToMove,
            CancellationToken cancellationToken)
        {
            var oldMembers = classDeclaration.Members;
            // create capture variables for the equality operators, since we want to check both of them at the same time
            // to make sure they can be deleted
            OperatorDeclarationSyntax? equals = null;
            OperatorDeclarationSyntax? notEquals = null;

            using var _ = ArrayBuilder<MemberDeclarationSyntax>.GetInstance(out var modifiedMembers);
            foreach (var member in oldMembers)
            {
                switch (member)
                {
                    case PropertyDeclarationSyntax property:
                        {
                            if (propertiesToMove.Contains(property))
                            {
                                continue;
                            }
                            break;
                        }

                    case ConstructorDeclarationSyntax constructor:
                        {
                            // remove the constructor with the same parameter types in the same order as the positional parameters
                            var constructorParamTypes = constructor.ParameterList.Parameters.SelectAsArray(p => p.Type!.ToString());
                            var positionalParamTypes = propertiesToMove.SelectAsArray(p => p.Type!.ToString());
                            if (constructorParamTypes.SequenceEqual(positionalParamTypes))
                            {
                                if (!IsSimpleConstructor(
                                    constructor, propertiesToMove, constructor.ParameterList.Parameters, semanticModel, cancellationToken))
                                {
                                    // if it is too complex to remove (param validation, error handling, side effects, not everything assigned)
                                    // we won't delete, and give a warning
                                    // TODO: Change warning string
                                    modifiedMembers.Add(
                                        constructor.WithAdditionalAnnotations(WarningAnnotation.Create("Placeholder Constructor warning")));
                                }
                                continue;
                            }

                            if (constructorParamTypes.Length == 1 &&
                                constructorParamTypes.First() == classDeclaration.Identifier.ToString())
                            {
                                // TODO: warn that we're deleting 
                            }

                            // TODO: Can potentially refactor statements which initialize properties with a simple expression
                            // (not using local variables) to be moved into the this args
                            var thisArgs = SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                    Enumerable.Repeat(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                                        propertiesToMove.Length)));

                            // TODO: look to see if there is already initializer (base or this)

                            var modifiedConstructor = constructor
                                .WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, thisArgs));

                            modifiedMembers.Add(modifiedConstructor);
                            continue;
                        }

                    case OperatorDeclarationSyntax op:
                        {
                            // keep track of equality overloads so we can potentially remove them later
                            var opKind = op.OperatorToken.Kind();
                            if (opKind == SyntaxKind.EqualsEqualsToken)
                            {
                                equals = op;
                            }
                            else if (opKind == SyntaxKind.ExclamationEqualsToken)
                            {
                                notEquals = op;
                            }

                            break;
                        }

                    case MethodDeclarationSyntax method:
                        {
                            if (method.Identifier.Text == "Clone")
                            {
                                // delete any 'clone' method as it is reserved in records
                                continue;
                            }

                            // TODO: Ensure the single param is a StringBuilder
                            if (method.Identifier.Text == "PrintMembers" &&
                                method.ReturnType == SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)) &&
                                method.ParameterList.Parameters.IsSingle())
                            {
                                if (type.TypeKind == TypeKind.Class && !type.IsSealed)
                                {
                                    // ensure virtual and protected modifiers
                                    modifiedMembers.Add(method.WithModifiers(SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                                        SyntaxFactory.Token(SyntaxKind.VirtualKeyword))));
                                }
                                else
                                {
                                    // ensure private member
                                    modifiedMembers.Add(method.WithModifiers(
                                        SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))));
                                }

                                continue;
                            }

                            if (method.Identifier.Text == nameof(GetHashCode) &&
                                method.ReturnType == SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)) &&
                                method.ParameterList.Parameters.IsEmpty() &&
                                UseSystemHashCode.Analyzer.TryGetAnalyzer(semanticModel.Compilation, out var analyzer))
                            {
                                var methodSymbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);
                                var methodOperation = semanticModel.GetOperation(method, cancellationToken);
                                if (methodSymbol != null && methodOperation is IMethodBodyOperation methodBodyOperation)
                                {
                                    var (accessesBase, members, statements) = analyzer.GetHashedMembers(methodSymbol, methodOperation);
                                    // TODO: check to see if the properties are the same as we would use and delete
                                }

                                // TODO: Add method back with a warning
                                continue;
                            }

                            break;
                        }
                }

                // any other members we didn't change or modify we just keep
                modifiedMembers.Add(member);
            }

            if (equals != null && notEquals != null)
            {
                if (IsDefaultEqualsOperator(equals, type, semanticModel, cancellationToken) &&
                    IsDefaultNotEqualsOperator(notEquals, type, semanticModel, cancellationToken))
                {
                    modifiedMembers.Remove(equals);
                    modifiedMembers.Remove(notEquals);
                }
            }

            return modifiedMembers.ToImmutable();
        }

        /// <summary>
        /// Returns true if the method contents match a simple reference to the equals method
        /// which would be the compiler generated implementation
        /// </summary>
        private static bool IsDefaultEqualsOperator(
            OperatorDeclarationSyntax operatorDeclaration,
            INamedTypeSymbol type,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (semanticModel.GetOperation(operatorDeclaration, cancellationToken) is not IMethodBodyOperation operation)
            {
                return false;
            }

            var body = operation.BlockBody ?? operation.ExpressionBody;

            // must look like
            // public static operator ==(C c1, object? c2)
            // {
            //  return c1.Equals(c2);
            // }
            // or
            // public static operator ==(C c1, object? c2) => c1.Equals(c2);
            // can have either object or class type as type for second param
            return body != null &&
                body.Operations.IsSingle() &&
                operation.BlockBody?.Operations[0] is IReturnOperation returnOperation &&
                returnOperation.ChildOperations.Count == 1 &&
                IsDotEqualsInvocation(returnOperation.ChildOperations.First(), type);
        }

        // must be of form !(c1 == c2) or !c1.Equals(c2)
        private static bool IsDefaultNotEqualsOperator(
            OperatorDeclarationSyntax operatorDeclaration,
            INamedTypeSymbol type,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (semanticModel.GetOperation(operatorDeclaration, cancellationToken) is not IMethodBodyOperation operation)
            {
                return false;
            }

            var body = operation.BlockBody ?? operation.ExpressionBody;
            // if any of these conditions are false we short-circuit false
            if (!(body != null &&
                body.Operations.IsSingle() &&
                operation.BlockBody?.Operations[0] is IReturnOperation returnOperation &&
                returnOperation.ChildOperations.Count == 1 &&
                returnOperation.ChildOperations.First() is IUnaryOperation notOp &&
                notOp.Syntax.IsKind(SyntaxKind.LogicalNotExpression) &&
                notOp.ChildOperations.Count == 1))
            {
                return false;
            }

            if (IsDotEqualsInvocation(notOp.ChildOperations.First(), type))
            {
                return true;
            }

            if (!(notOp.ChildOperations.First() is IBinaryOperation equalsOp &&
                equalsOp.Syntax.IsKind(SyntaxKind.EqualsExpression)))
            {
                return false;
            }

            // either could potentially be a param reference or an implicitly cast param reference
            // based on the definition of the == operator
            var left = GetParamFromArgument(equalsOp.LeftOperand);
            var right = GetParamFromArgument(equalsOp.RightOperand);
            return (left != null && right != null && !left.Equals(right));
        }


        // matches form
        // c1.Equals(c2)
        // where one of c1 or c2 is the given type
        // and the other is the same type or object
        private static bool IsDotEqualsInvocation(IOperation operation, INamedTypeSymbol type)
        {
            // any of these being false will short-circuit false
            if (!(operation is IInvocationOperation invocation &&
                invocation.TargetMethod.Name == nameof(Equals) &&
                invocation.ChildOperations.Count == 2 &&
                invocation.ChildOperations.First() is IParameterReferenceOperation { Parameter: var invokedOn } &&
                invocation.Arguments.Length == 1))
            {
                return false;
            }

            var param = GetParamFromArgument(invocation.Arguments[0].Value);

            return param != null && !invokedOn.Equals(param);
        }

        private static IParameterSymbol? GetParamFromArgument(IOperation arg)
        {
            if (arg is IParameterReferenceOperation directParameterReference)
            {
                return directParameterReference.Parameter;
            }
            // if the invocation parameter was an object and the argument was the type, there is an implicit cast
            else if (arg is IConversionOperation
            {
                IsImplicit: true,
                Operand: IParameterReferenceOperation castParameterReference
            })
            {
                return castParameterReference.Parameter;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Matches constructors where each statement simply assigns one of the provided parameters to one of the provided properties
        /// with no duplicate assignment or any other type of statement
        /// </summary>
        /// <param name="constructor">Constructor Declaration</param>
        /// <param name="properties">Properties expected to be assigned (would be replaced with positional constructor)</param>
        /// <param name="parameters">Constructor parameters</param>
        /// <returns>Whether the constructor body matches the pattern described</returns>
        private static bool IsSimpleConstructor(
            ConstructorDeclarationSyntax constructor,
            ImmutableArray<PropertyDeclarationSyntax> properties,
            SeparatedSyntaxList<ParameterSyntax> parameters,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (semanticModel.GetOperation(constructor, cancellationToken) is not IConstructorBodyOperation operation)
            {
                return false;
            }

            // We expect the constructor to have exactly one statement per parameter,
            // where the statement is a simple assignment from the parameter to the property
            if (operation.BlockBody == null || operation.BlockBody.Operations.Length != parameters.Count)
            {
                return false;
            }

            var propertyNames = properties.SelectAsArray(p => p.Identifier.ToString());
            var propertyNamesAlreadyAssigned = new HashSet<string>();
            var parameterNames = parameters.SelectAsArray(p => p.Identifier.ToString());

            foreach (var bodyOperation in operation.BlockBody.Operations)
            {
                if (bodyOperation is IExpressionStatementOperation statement &&
                    statement.Operation is ISimpleAssignmentOperation assignment &&
                    assignment.Target is IPropertyReferenceOperation propertyReference &&
                    assignment.Value is IParameterReferenceOperation parameterReference)
                {
                    var propertyName = propertyReference.Property.Name;
                    var propertyIndex = propertyNames.IndexOf(propertyName);
                    if (propertyIndex != -1 &&
                        !propertyNamesAlreadyAssigned.Contains(propertyName) &&
                        // make sure the index of the property we assign as it would be placed in the primary constructor
                        // matches the current index of the parameter we use for the explicit constructor
                        // TODO(extension): re-order property list or keep track of which parameters correspond to which properties
                        // so that when we refactor usages of this constructor we can assign the right vars to the right names
                        propertyIndex == parameterNames.IndexOf(parameterReference.Parameter.Name))
                    {
                        // make sure we don't have duplicate assignment statements to the same property
                        propertyNamesAlreadyAssigned.Add(propertyName);
                        continue;
                    }
                }
                // one of the conditions failed
                return false;
            }
            // all conditions passed individually, make sure all properties were assigned to
            return propertyNamesAlreadyAssigned.Count == properties.Length;
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
            TypeDeclarationSyntax typeDeclaration)
        {
            var classTrivia = typeDeclaration.GetLeadingTrivia().Where(trivia => !trivia.IsWhitespace()).AsImmutable();

            var propertyNonDocComments = properties
                .SelectMany(p => p.GetLeadingTrivia().Where(trivia => !trivia.IsDocComment() && !trivia.IsWhitespace()))
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

            var propertyParamComments = CreateParamComments(properties, exteriorTrivia!.Value);
            var classDocComment = classTrivia.FirstOrNull(trivia => trivia.IsDocComment());
            DocumentationCommentTriviaSyntax newClassDocComment;

            if (classDocComment?.GetStructure() is DocumentationCommentTriviaSyntax originalClassDoc)
            {
                // insert parameters after summary node and the extra newline or at start if no summary
                var summaryIndex = originalClassDoc.Content.IndexOf(node =>
                    node is XmlElementSyntax element &&
                    element.StartTag?.Name.LocalName.ToString() == DocumentationCommentXmlNames.SummaryElementName);

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
                            .Prepend(SyntaxFactory.XmlText(SyntaxFactory.XmlTextNewLine("\r\n", continueXmlDocumentationComment: false)
                                .WithLeadingTrivia(SyntaxFactory.DocumentationCommentExterior("/**"))
                                .WithTrailingTrivia(exteriorTrivia)))
                            .Append(SyntaxFactory.XmlText(SyntaxFactory.XmlTextNewLine("\r\n", continueXmlDocumentationComment: false)))),
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
            SyntaxTriviaList exteriorTrivia)
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
                        element.StartTag?.Name.LocalName.ToString() == DocumentationCommentXmlNames.SummaryElementName);

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
                    SyntaxFactory.XmlTextNewLine("\r\n", continueXmlDocumentationComment: false),
                    SyntaxFactory.XmlTextLiteral(" ").WithLeadingTrivia(exteriorTrivia));
                yield return SyntaxFactory.XmlParamElement(property.Identifier.ToString(), paramContent.AsArray());
            }
        }
        #endregion
    }
}
