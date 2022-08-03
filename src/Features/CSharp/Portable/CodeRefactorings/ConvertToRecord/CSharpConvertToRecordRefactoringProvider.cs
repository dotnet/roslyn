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
                    // no need to convert if it's already a record
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
                nameof(CSharpFeaturesResources.Convert_to_positional_record));
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

            RecordDeclarationSyntax changedTypeDeclaration;
            // if we have no members, use semicolon instead of braces
            if (membersToKeep.IsEmpty)
            {
                changedTypeDeclaration = SyntaxFactory.RecordDeclaration(
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
                    originalDeclarationNode.BaseList,
                    originalDeclarationNode.ConstraintClauses,
                    openBraceToken: default,
                    SyntaxFactory.List(membersToKeep),
                    closeBraceToken: default,
                    semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    .WithLeadingTrivia(modifiedClassTrivia)
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }
            else
            {
                changedTypeDeclaration = SyntaxFactory.RecordDeclaration(
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
                    originalDeclarationNode.BaseList,
                    originalDeclarationNode.ConstraintClauses,
                    originalDeclarationNode.OpenBraceToken,
                    SyntaxFactory.List(membersToKeep),
                    originalDeclarationNode.CloseBraceToken,
                    semicolonToken: default)
                    .WithLeadingTrivia(modifiedClassTrivia)
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }

            var changedDocument = await document.ReplaceNodeAsync(
                originalDeclarationNode, changedTypeDeclaration, cancellationToken).ConfigureAwait(false);

            return changedDocument;
        }

        private static bool ShouldConvertProperty(MemberDeclarationSyntax member, INamedTypeSymbol containingType)
        {
            if (member is not PropertyDeclarationSyntax
                {
                    Initializer: null,
                    ExpressionBody: null,
                } property)
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

            // When we convert to primary constructor parameters, the auto-generated properties have default behavior
            // in this iteration, we will only move if it would not change the default behavior.
            // - No accessors can have any explicit implementation or modifiers
            //   - This is because it would indicate complex functionality or explicit hiding which is not default 
            // - class records and readonly struct records must have:
            //   - public get accessor (with no explicit impl)
            //   - optionally a public init accessor (again w/ no impl)
            //     - note: if this is not provided the user can still initialize the property in the constructor,
            //             so it's like init but without the user ability to initialize outside the constructor
            // - for non-readonly structs, we must have:
            //   - public get accessor (with no explicit impl)
            //   - public set accessor (with no explicit impl)
            var accessors = property.AccessorList.Accessors;
            if (accessors.Any(a => a.Body != null || a.ExpressionBody != null) ||
                !accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration && a.Modifiers.IsEmpty()))
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
                // either we are a class or readonly struct, we only want to convert init setters, not normal ones
                if (accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)))
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

        /// <summary>
        /// Removes or modifies members in preparation of adding to a record with a primary constructor (positional parameters)
        /// Deletes properties that we move to positional params
        /// Deletes methods, constructors, and operators that would be generated by default if we believe they currently have a
        /// similar effect to the generated ones
        /// modifies constructors and some method modifiers to fall in line with record requirements (e.g. this() initializer)
        /// </summary>
        /// <param name="type">Original type symbol</param>
        /// <param name="typeDeclaration">Original type declaration</param>
        /// <param name="semanticModel">Semantic model</param>
        /// <param name="propertiesToMove">Properties we decided to move</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The list of members from the original type, modified and trimmed for a positional record type usage</returns>
        private static ImmutableArray<MemberDeclarationSyntax> GetModifiedMembersForPositionalRecord(
            INamedTypeSymbol type,
            TypeDeclarationSyntax typeDeclaration,
            SemanticModel semanticModel,
            ImmutableArray<PropertyDeclarationSyntax> propertiesToMove,
            CancellationToken cancellationToken)
        {
            var modifiedMembers = typeDeclaration.Members.AsImmutable();

            // remove properties we're bringing up to positional params
            modifiedMembers = modifiedMembers.RemoveRange(propertiesToMove);

            // get all the constructors so we can add an initializer to them
            // or potentially delete the primary constructor
            var constructors = modifiedMembers
                .Where(member => member is ConstructorDeclarationSyntax)
                // for some reason, neither of the following work for downcasts, even if each individual cast will succeed:
                // As<ConstructorDeclarationSyntax>()
                // CastArray<ConstructorDeclarationSyntax>()
                .Cast<ConstructorDeclarationSyntax>()
                .AsImmutable();
            foreach (var constructor in constructors)
            {
                // check to see if it would override the primary constructor
                var constructorParamTypes = constructor.ParameterList.Parameters.SelectAsArray(p => p.Type!.ToString());
                var positionalParamTypes = propertiesToMove.SelectAsArray(p => p.Type!.ToString());
                if (constructorParamTypes.SequenceEqual(positionalParamTypes))
                {
                    // found a primary constructor override, now check if we are pretty sure we can remove it safely
                    if (IsSimpleConstructor(
                        constructor, propertiesToMove, constructor.ParameterList.Parameters, semanticModel, cancellationToken))
                    {
                        modifiedMembers = modifiedMembers.Remove(constructor);
                    }
                    else
                    {
                        // can't remove it safely, at least add a warning that it will produce an error
                        modifiedMembers = modifiedMembers.Replace(constructor,
                            constructor.WithAdditionalAnnotations(WarningAnnotation.Create(
                                CSharpFeaturesResources.Warning_constructor_signature_conflicts_with_primary_constructor)));
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

                    modifiedMembers = modifiedMembers.Replace(constructor, constructor
                        .WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, thisArgs)));
                }
            }

            // get equality operators and potentially remove them
            var equalsOp = (OperatorDeclarationSyntax?)modifiedMembers.FirstOrDefault(member
                => member is OperatorDeclarationSyntax { OperatorToken.RawKind: (int)SyntaxKind.EqualsEqualsToken });
            var notEqualsOp = (OperatorDeclarationSyntax?)modifiedMembers.FirstOrDefault(member
                => member is OperatorDeclarationSyntax { OperatorToken.RawKind: (int)SyntaxKind.ExclamationEqualsToken });
            if (equalsOp != null && notEqualsOp != null &&
                IsDefaultEqualsOperator(equalsOp, semanticModel, cancellationToken) &&
                IsDefaultNotEqualsOperator(notEqualsOp, semanticModel, cancellationToken))
            {
                // they both evaluate to what would be the generated implementation
                modifiedMembers = modifiedMembers.Remove(equalsOp);
                modifiedMembers = modifiedMembers.Remove(notEqualsOp);
            }

            var methods = modifiedMembers
                .Where(member => member is MethodDeclarationSyntax)
                .Cast<MethodDeclarationSyntax>()
                .AsImmutable();
            foreach (var method in methods)
            {
                var methodSymbol = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(method, cancellationToken);
                if (methodSymbol.Name == "Clone")
                {
                    // remove clone method as clone is a reserved method name in records
                    modifiedMembers = modifiedMembers.Remove(method);
                }
                else if (methodSymbol is IMethodSymbol
                {
                    Name: "PrintMembers",
                    ReturnType.SpecialType: SpecialType.System_Boolean,
                    Parameters: [IParameterSymbol { Type.Name: nameof(System.Text.StringBuilder) }]
                })
                {
                    // PrintMembers must have a secific set of modifiers or else there is an error
                    if (type.TypeKind == TypeKind.Class && !type.IsSealed)
                    {
                        // ensure virtual and protected modifiers
                        modifiedMembers = modifiedMembers.Replace(method, method.WithModifiers(SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                            SyntaxFactory.Token(SyntaxKind.VirtualKeyword))));
                    }
                    else
                    {
                        // ensure private member
                        modifiedMembers = modifiedMembers.Replace(method, method.WithModifiers(
                            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))));
                    }
                }
            }

            return modifiedMembers;
        }

        /// <summary>
        /// Returns true if the method contents match a simple reference to the equals method
        /// which would be the compiler generated implementation
        /// </summary>
        private static bool IsDefaultEqualsOperator(
            OperatorDeclarationSyntax operatorDeclaration,
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
            return body is IBlockOperation
            {
                // look for only one operation, a return operation that consists of an equals invocation
                Operations: [IReturnOperation { ReturnedValue: IOperation returnedValue }]
            } &&
            IsDotEqualsInvocation(returnedValue);
        }

        // must be of form !(c1 == c2) or !c1.Equals(c2)
        private static bool IsDefaultNotEqualsOperator(
            OperatorDeclarationSyntax operatorDeclaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (semanticModel.GetOperation(operatorDeclaration, cancellationToken) is not IMethodBodyOperation operation)
            {
                return false;
            }

            var body = operation.BlockBody ?? operation.ExpressionBody;
            // looking for:
            // return !(operand);
            // or:
            // => !(operand);
            if (body is not IBlockOperation
                {
                    Operations: [IReturnOperation
                    {
                        ReturnedValue: IUnaryOperation
                        {
                            OperatorKind: UnaryOperatorKind.Not,
                            Operand: IOperation operand
                        }
                    }]
                })
            {
                return false;
            }

            // check to see if operand is an equals invocation that references the parameters
            if (IsDotEqualsInvocation(operand))
            {
                return true;
            }

            // we accept an == operator, for example
            // return !(obj1 == obj2);
            // since this would call our == operator, which would in turn call .Equals (or equivalent)
            // but we need to make sure that the operands are parameter references
            if (operand is not IBinaryOperation
                {
                    OperatorKind: BinaryOperatorKind.Equals,
                    LeftOperand: IOperation leftOperand,
                    RightOperand: IOperation rightOperand,
                })
            {
                return false;
            }

            // now we know we have an == comparison, but we want to make sure these actually reference parameters
            var left = GetParamFromArgument(leftOperand);
            var right = GetParamFromArgument(rightOperand);
            // make sure we're not referencing the same parameter twice
            return (left != null && right != null && !left.Equals(right));
        }

        // matches form
        // c1.Equals(c2)
        // where c1 and c2 are parameter references
        private static bool IsDotEqualsInvocation(IOperation operation)
        {
            // must be called on one of the parameters
            if (operation is not IInvocationOperation
                {
                    TargetMethod.Name: nameof(Equals),
                    Instance: IOperation instance,
                    Arguments: [IArgumentOperation { Value: IOperation arg }]
                })
            {
                return false;
            }

            // get the (potential) parameters, uwrapping any potential implicit casts
            var invokedOn = GetParamFromArgument(instance);
            var param = GetParamFromArgument(arg);
            // make sure we're not referencing the same parameter twice
            return param != null && invokedOn != null && !invokedOn.Equals(param);
        }

        /// <summary>
        /// Get the referenced parameter (and unwraps implicit cast if necessary) or null if a parameter wasn't referenced
        /// </summary>
        /// <param name="arg">The operation for which to get the parameter</param>
        /// <returns>the referenced parameter or null if unable to find</returns>
        private static IParameterSymbol? GetParamFromArgument(IOperation arg)
        {
            return arg switch
            {
                IParameterReferenceOperation directParameterReference => directParameterReference.Parameter,
                // if the invocation parameter was an object and the argument was the type, there is an implicit cast
                IConversionOperation
                {
                    IsImplicit: true,
                    Operand: IParameterReferenceOperation castParameterReference
                } => castParameterReference.Parameter,
                _ => null,
            };
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
                if (bodyOperation is IExpressionStatementOperation
                    {
                        Operation: ISimpleAssignmentOperation
                        {
                            Target: IPropertyReferenceOperation propertyReference,
                            Value: IParameterReferenceOperation parameterReference
                        }
                    })
                {
                    var propertyName = propertyReference.Property.Name;
                    var propertyIndex = propertyNames.IndexOf(propertyName);
                    if (propertyIndex != -1 &&
                        !propertyNamesAlreadyAssigned.Contains(propertyName) &&
                        // make sure the index of the property we assign as it would be placed in the primary constructor
                        // matches the current index of the parameter we use for the explicit constructor
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
