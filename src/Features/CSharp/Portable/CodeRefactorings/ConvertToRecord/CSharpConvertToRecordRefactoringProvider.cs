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
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToRecord
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRecord), Shared]
    internal class CSharpConvertToRecordRefactoringProvider : CodeRefactoringProvider
    {

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConvertToRecordRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var classDeclaration = await context.TryGetRelevantNodeAsync<TypeDeclarationSyntax>().ConfigureAwait(false);

            if (classDeclaration == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol type ||
                // if type is some enum, interface, delegate, etc we don't want to refactor
                (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct) ||
                // TODO: when adding other options it might make sense to convert a postional record to a non-positional one and vice versa
                // but for now we don't convert records
                type.IsRecord ||
                // records can't be static and so if the class is static we probably shouldn't convert it
                type.IsStatic ||
                // make sure that there is at least one positional parameter we can introduce
                !classDeclaration.Members.Any(m => ShouldConvertProperty(m, type)))
            {
                return;
            }

            var positional = CodeAction.Create(
                "placeholder title",
                cancellationToken => ConvertToParameterRecordAsync(document, type, classDeclaration, semanticModel, cancellationToken),
                "placeholder key");
            var nested = CodeAction.CodeActionWithNestedActions.Create("placeholder nested action", ImmutableArray.Create(positional), isInlinable: false);
            context.RegisterRefactoring(nested);
        }

        private static async Task<Document> ConvertToParameterRecordAsync(
            Document document,
            INamedTypeSymbol originalType,
            TypeDeclarationSyntax originalDeclarationNode,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
            var syntaxGenerator = document.GetRequiredLanguageService<SyntaxGenerator>();

            var propertiesToMove = originalDeclarationNode.Members.Where(m => ShouldConvertProperty(m, originalType)).Cast<PropertyDeclarationSyntax>().AsImmutable();

            var modifiedClassTrivia = GetModifiedClassTrivia(propertiesToMove, originalDeclarationNode, syntaxGenerator);

            var propertiesToAddAsParams = propertiesToMove.SelectAsArray(p =>
                SyntaxFactory.Parameter(
                    new SyntaxList<AttributeListSyntax>(p.AttributeLists.SelectAsArray(attributeList =>
                        attributeList.Target == null
                        // convert attributes attached to the property with no target into "property :" targeted attributes
                        ? attributeList.WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.PropertyKeyword)))
                        : attributeList)),
                    modifiers: default,
                    p.Type,
                    p.Identifier,
                    @default: null));

            // remove converted properties and reformat other methods
            var membersToKeep = GetModifiedMembersForPositionalRecord(originalType, originalDeclarationNode, semanticModel, propertiesToMove, cancellationToken);

            var changedTypeDeclaration = SyntaxFactory.RecordDeclaration(
                originalType.TypeKind == TypeKind.Class
                    ? SyntaxKind.RecordDeclaration
                    : SyntaxKind.RecordStructDeclaration,
                originalDeclarationNode.GetAttributeLists(),
                originalDeclarationNode.GetModifiers(),
                SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                originalType.TypeKind == TypeKind.Class
                    ? default
                    : SyntaxFactory.Token(SyntaxKind.StructKeyword),
                originalDeclarationNode.Identifier,
                originalDeclarationNode.TypeParameterList,
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(propertiesToAddAsParams)),
                // TODO: inheritance
                baseList: null,
                originalDeclarationNode.ConstraintClauses,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                SyntaxFactory.List(membersToKeep),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                semicolonToken: default)
                .WithLeadingTrivia(modifiedClassTrivia)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var changedDocument = await document.ReplaceNodeAsync(originalDeclarationNode, changedTypeDeclaration, cancellationToken).ConfigureAwait(false);
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

            if (containingType.TypeKind == TypeKind.Class || containingType.IsReadOnly)
            {
                if (!accessors.Any(a => a.Kind() == SyntaxKind.InitAccessorDeclaration))
                {
                    return false;
                }
            }
            else
            {
                if (!accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration))
                {
                    return false;
                }
            }

            return true;
        }

        // format should be:
        // 1. comments and other trivia from class that were already on class
        // 2. comments from each property
        // 3. Class documentation comment summary
        // 4. Property summary documentation (as param)
        // 5. Rest of class documentation comments
        private static SyntaxTriviaList GetModifiedClassTrivia(
            ImmutableArray<PropertyDeclarationSyntax> properties,
            TypeDeclarationSyntax typeDeclaration,
            SyntaxGenerator syntaxGenerator)
        {
            var modifiedClassTrivia = typeDeclaration.GetLeadingTrivia().Where(trivia => !trivia.IsDocComment())
                .Concat(properties.SelectMany(p => p.GetLeadingTrivia().Where(trivia => !trivia.IsDocComment() && !trivia.IsWhitespace())));

            var classDocComment = typeDeclaration.GetLeadingTrivia().FirstOrDefault(trivia => trivia.IsDocComment());
            // explicit type args because we want to cast the results (XmlElementSyntax) up to base (XmlNodeSyntax) so we can add things later
            var propertyParamComments = properties.SelectAsArray<PropertyDeclarationSyntax, XmlNodeSyntax>(property =>
            {
                // get the documentation comment
                var potentialDocComment = property.GetLeadingTrivia().FirstOrDefault(trivia => trivia.IsDocComment());
                if (potentialDocComment.GetStructure() is DocumentationCommentTriviaSyntax docComment)
                {
                    // get the summary node if there is one
                    var summaryNode = docComment.Content.FirstOrDefault(node => node is XmlElementSyntax element && element.StartTag?.Name.LocalName.ToString() == "summary");
                    if (summaryNode != null)
                    {
                        // construct a parameter element from the contents of the property summary
                        // right now we throw away all other documentation parts of the property, because we don't really know where they should go
                        return SyntaxFactory.XmlParamElement(property.Identifier.ToString(), ((XmlElementSyntax)summaryNode).Content);
                    }
                }
                return SyntaxFactory.XmlParamElement(property.Identifier.ToString(), new SyntaxList<XmlNodeSyntax>());
            });

            // if we either have a class doc comment or any property has a doc comment
            if (classDocComment != default || properties.Any(property => property.GetLeadingTrivia().Any(trivia => trivia.IsDocComment())))
            {
                DocumentationCommentTriviaSyntax newClassDocComments;
                if (classDocComment != default && classDocComment.GetStructure() is DocumentationCommentTriviaSyntax originalClassDoc)
                {
                    // insert parameters after summary node and the extra newline or at start if no summary
                    var summaryIndex = originalClassDoc.Content.IndexOf(node => node is XmlElementSyntax element && element.StartTag?.Name.LocalName.ToString() == "summary");
                    // if not found, summaryIndex + 2 = -1 + 2 = 1, so our params go to the start (after a first text token)
                    newClassDocComments = (DocumentationCommentTriviaSyntax)syntaxGenerator.DocumentationCommentTriviaWithUpdatedContent(classDocComment, originalClassDoc.Content.InsertRange(summaryIndex + 2, propertyParamComments))!;
                }
                else
                {
                    // no class doc comment, create an empty summary and add params after it
                    newClassDocComments = SyntaxFactory.DocumentationComment(propertyParamComments.Insert(0, SyntaxFactory.XmlSummaryElement()).AsArray<XmlNodeSyntax>());
                }
                // TODO: Rearrange order if the comments were already in a different order
                modifiedClassTrivia = modifiedClassTrivia.Concat(SyntaxFactory.Trivia(newClassDocComments));
            }

            return new SyntaxTriviaList(modifiedClassTrivia.SelectAsArray(trivia => trivia.AsElastic()));
        }

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
                                if (!IsSimpleConstructor(constructor, propertiesToMove, constructor.ParameterList.Parameters, semanticModel, cancellationToken))
                                {
                                    // if it is too complex to remove (param validation, error handling, side effects, not everything assigned)
                                    // we won't delete, and give a warning
                                    modifiedMembers.Add(constructor.WithAdditionalAnnotations(WarningAnnotation.Create("Placeholder Constructor warning")));
                                }
                                continue;
                            }

                            // TODO: Can potentially refactor statements which initialize properties with a simple expression (not using local variables) to be moved into the this args
                            var thisArgs = SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                    Enumerable.Repeat(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                                        propertiesToMove.Length)));

                            // TODO: look to see if there is already initializer (base or this)

                            var modifiedConstructor = constructor.WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, thisArgs));

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
                                    modifiedMembers.Add(method.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))));
                                }

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
                if (ContainsEqualsInvocation(equals, type, semanticModel, cancellationToken))
                {
                    // we'll just assume that the != version always calls the == version or would be equivalent to !this.Equals(other)
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
        private static bool ContainsEqualsInvocation(OperatorDeclarationSyntax operatorDeclaration, INamedTypeSymbol type, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (semanticModel.GetOperation(operatorDeclaration, cancellationToken) is not IMethodBodyOperation operation)
            {
                return false;
            }

            var body = operation.BlockBody ?? operation.ExpressionBody;

            // must look like
            // public static operator ==(C c1, other? c2)
            // {
            //  return c1.Equals(c2);
            // }
            // or
            // public static operator ==(C c1, other? c2) => c1.Equals(c2);
            return body != null &&
                body.Operations.IsSingle() &&
                operation.BlockBody?.Operations[0] is IReturnOperation returnOperation &&
                returnOperation.ChildOperations.FirstOrDefault() is IInvocationOperation invocation &&
                invocation.TargetMethod.Name == nameof(Equals) &&
                invocation.ChildOperations.Count == 2 &&
                invocation.ChildOperations.First() is IParameterReferenceOperation first &&
                first.Parameter.Type.Equals(type);
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

            // We expect the constructor to have exactly one statement per parameter, where the statement is a simple assignment from the parameter to the property
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
                        // TODO(extension): re-order property list so they match
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
    }
}
