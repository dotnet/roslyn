// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
            var constructors = modifiedMembers.OfType<ConstructorDeclarationSyntax>();
            foreach (var constructor in constructors)
            {
                // check to see if it would override the primary constructor
                var constructorSymbol = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(constructor, cancellationToken);
                var constructorParamTypes = constructorSymbol.Parameters.SelectAsArray(parameter => parameter.Type);
                var positionalParamTypes = propertiesToMove.SelectAsArray(p => p.Symbol.Type);
                if (constructorParamTypes.SequenceEqual(positionalParamTypes))
                {
                    // found a primary constructor override, now check if we are pretty sure we can remove it safely
                    if (IsSimpleConstructor(constructor,
                        propertiesToMove.SelectAsArray(result => result.Symbol),
                        constructorSymbol.Parameters,
                        semanticModel,
                        cancellationToken))
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
            if (equalsOp != null && notEqualsOp != null &&
                IsDefaultEqualsOperator(equalsOp, semanticModel, cancellationToken) &&
                IsDefaultNotEqualsOperator(notEqualsOp, semanticModel, cancellationToken))
            {
                // they both evaluate to what would be the generated implementation
                modifiedMembers.Remove(equalsOp);
                modifiedMembers.Remove(notEqualsOp);
            }

            var methods = modifiedMembers.OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var methodSymbol = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(method, cancellationToken);
                // property default hashcode and equals methods compare all fields
                // when generated, including underlying fields accessed from properties
                var expectedFields = type.GetMembers().OfType<IFieldSymbol>();

                if (methodSymbol.Name == "Clone")
                {
                    // remove clone method as clone is a reserved method name in records
                    modifiedMembers.Remove(method);
                }
                else if (methodSymbol.Name == nameof(GetHashCode) &&
                    methodSymbol.Parameters.IsEmpty &&
                    UseSystemHashCode.Analyzer.TryGetAnalyzer(semanticModel.Compilation, out var analyzer))
                {
                    // Hash Code method, see if it would be a default implementation that we can remove
                    var operation = (IMethodBodyOperation)semanticModel.GetRequiredOperation(method, cancellationToken);
                    var (_, members, _) = analyzer.GetHashedMembers(methodSymbol, operation.BlockBody);
                    if (members != null)
                    {
                        // the user could access a member using either the property or the underlying field
                        // so anytime they access a property instead of the underlying field we convert it to the
                        // corresponding underlying field
                        var actualMembers = members
                            .SelectAsArray(member => member switch
                            {
                                IFieldSymbol field => field,
                                IPropertySymbol property => property.GetBackingFieldIfAny(),
                                _ => null
                            }).WhereNotNull().AsImmutable();

                        if (!actualMembers.HasDuplicates(SymbolEqualityComparer.Default) &&
                            actualMembers.SetEquals(expectedFields))
                        {
                            modifiedMembers.Remove(method);
                        }
                    }
                }
                else if (methodSymbol.Name == nameof(Equals) &&
                    methodSymbol.Parameters.IsSingle())
                {
                    var actualMembers = GetEqualizedMembers(method, methodSymbol, semanticModel, cancellationToken);
                    if (!actualMembers.HasDuplicates(EqualityComparer<IFieldSymbol>.Default) &&
                        actualMembers.SetEquals(expectedFields))
                    {
                        // the Equals method implementation is fundamentally equivalent to the generated one
                        modifiedMembers.Remove(method);
                    }
                }
            }

            var cloneMethod = modifiedMembers
                .FirstOrDefault(member => member is MethodDeclarationSyntax { Identifier.ValueText: "Clone" });

            if (cloneMethod != null)
            {
                modifiedMembers.Remove(cloneMethod);
            }

            return modifiedMembers.ToImmutable();
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
            var operation = (IMethodBodyOperation)semanticModel.GetRequiredOperation(operatorDeclaration, cancellationToken);

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
            var operation = (IMethodBodyOperation)semanticModel.GetRequiredOperation(operatorDeclaration, cancellationToken);

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
            var bottom = arg.WalkDownConversion();
            if (bottom is IParameterReferenceOperation parameterReference)
            {
                return parameterReference.Parameter;
            }
            return null;
        }

        private static ImmutableArray<IFieldSymbol> GetEqualizedMembers(
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var type = methodSymbol.ContainingType;

            var operation = (IMethodBodyOperation)semanticModel.GetRequiredOperation(methodDeclaration, cancellationToken);

            var body = operation.BlockBody ?? operation.ExpressionBody;
            var parameter = methodSymbol.Parameters.First();

            if (body == null || !methodSymbol.Parameters.IsSingle())
            {
                return ImmutableArray<IFieldSymbol>.Empty;
            }

            var bodyOps = body.Operations;
            var _ = ArrayBuilder<IFieldSymbol>.GetInstance(out var members);
            ISymbol? otherC = null;
            IEnumerable<IOperation>? statementsToCheck = null;

            // see whether we are calling on a param of the same type or of object
            if (parameter.Type.Equals(type))
            {
                // we need to check all the statements, and we already have the
                // variable that is used to access the members
                otherC = parameter;
                statementsToCheck = bodyOps;
            }
            else if (parameter.Type.SpecialType == SpecialType.System_Object)
            {
                // we could look for some cast which rebinds the parameter
                // to a local of the type such as any of the following:
                // var otherc = other as C; *null and additional equality checks*
                // if (other is C otherc) { *additional equality checks* } (optional else) return false;
                // if (other is not C otherc) { return false; } (optional else) { *additional equality checks* }
                // if (other is C) { otherc = (C) other;  *additional equality checks* } (optional else) return false;
                // if (other is not C) { return false; } (optional else) { otherc = (C) other;  *additional equality checks* }
                // return other is C otherC && ...
                // return !(other is not C otherC || ...

                // check for single return operation which binds the variable as the first condition in a sequence
                if (bodyOps is [IReturnOperation { ReturnedValue: IOperation value }])
                {
                    otherC = TryAddEqualizedMembersForConditionWithoutBinding(members, value, successRequirement: true, type);
                    if (otherC != null)
                    {
                        // no more statements to check as this was a return operation
                        return members.ToImmutable();
                    }
                }

                // check for the first statement as an explicit cast to a variable declaration
                // like: var otherC = other as C;
                otherC = GetAssignmentToParameterWithExplicitCast(bodyOps.FirstOrDefault(), parameter);
                if (otherC != null)
                {
                    statementsToCheck = bodyOps.Skip(1);
                }
                // check for the first statement as an if statement where the cast check occurs
                // and a variable assignment happens (either in the if or in a following statement
                else if (!TryGetBindingCastInFirstIfStatement(bodyOps, type, members, parameter, out otherC, out statementsToCheck))
                {
                    return ImmutableArray<IFieldSymbol>.Empty;
                }
            }

            if (otherC == null || statementsToCheck == null ||
                !TryAddEqualizedMembersForStatements(statementsToCheck, otherC, type, members))
            {
                // no patterns matched to bind variable or statements didn't match expectation
                return ImmutableArray<IFieldSymbol>.Empty;
            }

            return members.ToImmutable();
        }

        /// <summary>
        /// Matches a pattern where the first statement is an if statement that ensures a cast
        /// of the parameter to the correct type, and either binds it through an "is" pattern
        /// or later assigns it to a local varaiable
        /// </summary>
        /// <param name="bodyOps">method body to search in</param>
        /// <param name="type">type which is being called</param>
        /// <param name="members">members that may have been incidentally checked</param>
        /// <param name="parameter">uncast object parameter</param>
        /// <param name="otherC">if matched, the variable that the cast parameter was assigned to</param>
        /// <param name="statementsToCheck">remaining non-check, non-assignment operations
        /// to look for additional compared members. This can have statements even if there was no binding
        /// as long as it found an if check that checks the correct type</param>
        /// <returns>whether or not the pattern matched</returns>
        private static bool TryGetBindingCastInFirstIfStatement(
            ImmutableArray<IOperation> bodyOps,
            INamedTypeSymbol type,
            ArrayBuilder<IFieldSymbol> members,
            IParameterSymbol parameter,
            [NotNullWhen(true)] out ISymbol? otherC,
            [NotNullWhen(true)] out IEnumerable<IOperation>? statementsToCheck)
        {
            otherC = null;
            statementsToCheck = null;

            // we require the if statement (with a cast) to be the first operation in the body
            if (bodyOps.FirstOrDefault() is not IConditionalOperation
                {
                    Condition: IOperation condition,
                    WhenTrue: IOperation whenTrue,
                    WhenFalse: var whenFalse,
                })
            {
                return false;
            }

            // find out if we return false after the condition is true or false and get the statements
            // corresponding to the other branch
            if (!TryGetSuccessCondition(
                whenTrue, whenFalse, bodyOps.Skip(1).AsImmutable(), out var successRequirement, out var remainingStatments))
            {
                return false;
            }

            // gives us the symbol if the pattern was a binding one
            // (and adds members if they did other checks too)
            otherC = TryAddEqualizedMembersForConditionWithoutBinding(
                members, condition, successRequirement, type);

            // if we have no else block, we could get no remaining statements, in that case we take all the
            // statments after the if condition operation
            statementsToCheck = !remainingStatments.IsEmpty ? remainingStatments : bodyOps.Skip(1);

            if (otherC != null)
            {
                // the if statement contains a cast to a variable binding
                // like: if (other is C otherC)
                return true;
            }

            // either there was no pattern match, or the pattern did not bind to a variable declaration
            // (e.g: if (other is C) or if (other is not C))
            // we will look for a non-binding "is" pattern and if we find one,
            // look for a variable assignment in the appropriate block
            if ((condition is IIsTypeOperation
                {
                    TypeOperand: ITypeSymbol testType1,
                    ValueOperand: IParameterReferenceOperation { Parameter: IParameterSymbol referencedParameter1 }
                } &&
                    successRequirement &&
                    testType1.Equals(type) &&
                    referencedParameter1.Equals(parameter)) ||
                (condition is IIsPatternOperation
                {
                    Value: IParameterReferenceOperation { Parameter: IParameterSymbol referencedParameter2 },
                    Pattern: INegatedPatternOperation
                    {
                        Pattern: ITypePatternOperation
                        {
                            MatchedType: INamedTypeSymbol testType2
                        }
                    }
                } &&
                    !successRequirement &&
                    testType2.Equals(type) &&
                    referencedParameter2.Equals(parameter)))
            {
                // found correct pattern/type check, so we know we have something equivalent to
                // if (other is C) { ... } else return false;
                // we look for an explicit cast to assign a variable like:
                // var otherC = (C)other;
                // var otherC = other as C;
                otherC = GetAssignmentToParameterWithExplicitCast(statementsToCheck.FirstOrDefault(), parameter);
                if (otherC != null)
                {
                    statementsToCheck = statementsToCheck.Skip(1);
                    return true;
                }
            }

            // no explicit cast variable found
            return false;
        }

        /// <summary>
        /// Matches: var otherC = (C) other;
        /// or: var otherC = other as C;
        /// </summary>
        /// <param name="operation">potential variable declaration operation</param>
        /// <param name="parameter">parameter that should be referenced</param>
        /// <returns>symbol of declared variable if found, otw null</returns>
        private static ILocalSymbol? GetAssignmentToParameterWithExplicitCast(IOperation? operation, IParameterSymbol parameter)
        {
            if (operation is IVariableDeclarationGroupOperation
                {
                    Declarations: [IVariableDeclarationOperation
                    {
                        Declarators: [IVariableDeclaratorOperation
                        {
                            Symbol: ILocalSymbol castOther,
                            Initializer: IVariableInitializerOperation
                            {
                                Value: IConversionOperation
                                {
                                    IsImplicit: false,
                                    Operand: IParameterReferenceOperation
                                    {
                                        Parameter: IParameterSymbol referencedParameter1
                                    }
                                }
                            }
                        }]
                    }]
                } && referencedParameter1.Equals(parameter))
            {
                return castOther;
            }
            return null;
        }

        private static bool ReturnsFalseImmediately(ImmutableArray<IOperation> operation)
        {
            return operation.FirstOrDefault() is IReturnOperation
            {
                ReturnedValue: ILiteralOperation
                {
                    ConstantValue.HasValue: true,
                    ConstantValue.Value: false,
                }
            };
        }

        /// <summary>
        /// Match a list of statements and add members that are compared
        /// </summary>
        /// <param name="statementsToCheck">operations which should compare members</param>
        /// <param name="otherC">non-this comparison of the type we're equating</param>
        /// <param name="type">the this symbol</param>
        /// <param name="members">builder for property/field comparisons we encounter</param>
        /// <returns>whether every statment was one of the expected forms</returns>
        private static bool TryAddEqualizedMembersForStatements(
            IEnumerable<IOperation> statementsToCheck,
            ISymbol otherC,
            INamedTypeSymbol type,
            ArrayBuilder<IFieldSymbol> members)
        {
            while (!statementsToCheck.IsEmpty())
            {
                // iterate. We don't use a for loop because we could potentially
                // be changing this list based on what statements we see
                var statement = statementsToCheck.First();
                statementsToCheck = statementsToCheck.Skip(1);
                switch (statement)
                {
                    case IReturnOperation
                    {
                        ReturnedValue: ILiteralOperation
                        {
                            ConstantValue.HasValue: true,
                            ConstantValue.Value: true,
                        }
                    }:
                        // we are done with the comparison, the final statment does no checks
                        // but there should be no more statements
                        return true;
                    case IReturnOperation { ReturnedValue: IOperation value }:
                        return TryAddEqualizedMembersForCondition(members, value, successRequirement: true, type, otherC);
                    case IConditionalOperation
                    {
                        Condition: IOperation condition,
                        WhenTrue: IOperation whenTrue,
                        WhenFalse: var whenFalse,
                    }:
                        // 1. Check structure of if statment, get success requirement
                        // and any potential statments in the non failure block
                        // 2. Check condition for compared members
                        if (!TryGetSuccessCondition(
                            whenTrue, whenFalse, statementsToCheck.AsImmutable(), out var successRequirement, out var remainingStatements) ||
                            !TryAddEqualizedMembersForCondition(
                                members, condition, successRequirement, type, otherC))
                        {
                            return false;
                        }

                        // the statements to check are now all the statements from the branch that doesn't return false
                        statementsToCheck = remainingStatements;
                        break;
                    default:
                        return false;
                }
            }

            // pattern not matched, we should see a return statement before the end of the statements
            return false;
        }

        /// <summary>
        /// Attempts to get information about an if operation in an equals method,
        /// such as whether the condition being true would cause the method to return false
        /// and the remaining statments in the branch not returning false (if any)
        /// </summary>
        /// <param name="whenTrue">"then" branch</param>
        /// <param name="whenFalse">"else" branch (if any)</param>
        /// <param name="successRequirement">whether the condition being true would cause the method to return false
        /// or the condition being false would cause the method to return false</param>
        /// <param name="remainingStatements">Potential remaining statements of the branch that does not return false</param>
        /// <returns>whether the pattern was matched (one of the branches must have a simple "return false")</returns>
        private static bool TryGetSuccessCondition(
            IOperation whenTrue,
            IOperation? whenFalse,
            ImmutableArray<IOperation> otherOps,
            out bool successRequirement,
            out ImmutableArray<IOperation> remainingStatements)
        {
            // this will be changed if we successfully match the pattern
            successRequirement = default;
            // this could be empty even if we match, if there is no else block
            remainingStatements = default;

            // all the operations that would happen after the condition is true or false
            // branches can either be block bodies or single statements
            // each branch is followed by statements outside the branch either way
            var trueOps = ((whenTrue as IBlockOperation)?.Operations ?? ImmutableArray.Create(whenTrue))
                .Concat(otherOps);
            var falseOps = ((whenFalse as IBlockOperation)?.Operations ??
                (whenFalse != null
                    ? ImmutableArray.Create(whenFalse)
                    : ImmutableArray.Create<IOperation>()))
                .Concat(otherOps);

            // We expect one of the true or false branch to have exactly one statement: return false.
            // If we don't find that, it indicates complex behavior such as
            // extra statments, backup equality if one condition fails, or something else.
            // We don't necessarily expect a return true because we could see
            // a final return statement that checks a last set of conditions such as:
            // if (other is C otherC)
            // {
            //     return otherC.A == A;
            // }
            // return false;
            // We should never have a case where both branches could potentially return true;
            // at least one branch must simply return false.
            if (ReturnsFalseImmediately(trueOps) == ReturnsFalseImmediately(falseOps))
            {
                // both or neither fit the return false pattern, this if statement either doesn't do
                // anything or does something too complex for us, so we assume it's not a default.
                return false;
            }

            // when condition succeeds (evaluates to true), we return false
            // so for equality the condition should not succeed
            successRequirement = !ReturnsFalseImmediately(trueOps);
            remainingStatements = successRequirement ? trueOps : falseOps;
            remainingStatements.Concat(otherOps);
            return true;
        }

        /// <summary>
        /// looks just at conditional expressions such as "A == other.A &amp;&amp; B == other.B..."
        /// To determine which members were accessed and compared
        /// </summary>
        /// <param name="builder">Builder to add members to</param>
        /// <param name="condition">Condition to look at, should be a boolean expression</param>
        /// <param name="successRequirement">Whether to look for operators that would indicate equality success
        /// (==, .Equals, &amp;&amp;) or inequality operators (!=, ||)</param>
        /// <param name="currentObject">Symbol that would be referenced with this</param>
        /// <param name="otherObject">symbol representing other object, either from a param or cast as a local</param>
        /// <returns>true if addition was successful, false if we see something odd 
        /// (equality checking in the wrong order, side effects, etc)</returns>
        private static bool TryAddEqualizedMembersForCondition(
            ArrayBuilder<IFieldSymbol> builder,
            IOperation condition,
            bool successRequirement,
            ISymbol currentObject,
            ISymbol otherObject)
        => (successRequirement, condition) switch
        {
            // if we see a not we want to invert the current success status
            // e.g !(A != other.A || B != other.B) is equivalent to (A == other.A && B == other.B)
            // using DeMorgans law. We recurse to see any members accessed
            (_, IUnaryOperation { OperatorKind: UnaryOperatorKind.Not, Operand: IOperation newCondition })
                => TryAddEqualizedMembersForCondition(builder, newCondition, !successRequirement, currentObject, otherObject),
            // We want our equality check to be exhaustive, i.e. all checks must pass for the condition to pass
            // we recurse into each operand to try to find some props to bind
            (true, IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd } andOp)
                => TryAddEqualizedMembersForCondition(builder, andOp.LeftOperand, successRequirement, currentObject, otherObject) &&
                    TryAddEqualizedMembersForCondition(builder, andOp.RightOperand, successRequirement, currentObject, otherObject),
            // Exhaustive binary operator for inverted checks via DeMorgan's law
            // We see an or here, but we're in a context where this being true will return false
            // for example: return !(expr || expr)
            // or: if (expr || expr) return false;
            (false, IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalOr } orOp)
                => TryAddEqualizedMembersForCondition(builder, orOp.LeftOperand, successRequirement, currentObject, otherObject) &&
                    TryAddEqualizedMembersForCondition(builder, orOp.RightOperand, successRequirement, currentObject, otherObject),
            // we are actually comparing two values that are potentially members,
            // e.g: return A == other.A;
            (true, IBinaryOperation
            {
                OperatorKind: BinaryOperatorKind.Equals,
                LeftOperand: IMemberReferenceOperation leftMemberReference,
                RightOperand: IMemberReferenceOperation rightMemberReference,
            }) => TryAddMemberFromComparison(leftMemberReference, rightMemberReference, currentObject, otherObject, builder),
            // we are comparing two potential members, but in a context where if the expression is true, we return false
            // e.g: return !(A != other.A); 
            (false, IBinaryOperation
            {
                OperatorKind: BinaryOperatorKind.NotEquals,
                LeftOperand: IMemberReferenceOperation leftMemberReference,
                RightOperand: IMemberReferenceOperation rightMemberReference,
            }) => TryAddMemberFromComparison(leftMemberReference, rightMemberReference, currentObject, otherObject, builder),
            // equals invocation, something like: A.Equals(other.A)
            (true, IInvocationOperation
            {
                TargetMethod.Name: nameof(Equals),
                Instance: IMemberReferenceOperation invokedOn,
                Arguments: [IMemberReferenceOperation arg]
            }) => TryAddMemberFromComparison(invokedOn, arg, currentObject, otherObject, builder),
            // some other operation, or an incorrect operation (!= when we expect == based on context, etc).
            // If one of the conditions is just a null check on the "otherObject", then it's valid but doesn't check any members
            // Otherwise we fail as it has unknown behavior
            _ => IsNullCheck(condition, successRequirement, otherObject)
        };

        /// <summary>
        /// Same as <see cref="TryAddEqualizedMembersForCondition"/> but we're looking for
        /// a binding through an "is" pattern first/>
        /// </summary>
        /// <returns>the cast parameter symbol if found, null if not</returns>
        private static ISymbol? TryAddEqualizedMembersForConditionWithoutBinding(
            ArrayBuilder<IFieldSymbol> builder,
            IOperation condition,
            bool successRequirement,
            ISymbol currentObject,
            IEnumerable<IOperation>? additionalConditions = null)
        {
            additionalConditions ??= Enumerable.Empty<IOperation>();
            return (successRequirement, condition) switch
            {
                (_, IUnaryOperation { OperatorKind: UnaryOperatorKind.Not, Operand: IOperation newCondition })
                    => TryAddEqualizedMembersForConditionWithoutBinding(
                        builder, newCondition, !successRequirement, currentObject, additionalConditions),
                (true, IBinaryOperation
                {
                    OperatorKind: BinaryOperatorKind.ConditionalAnd,
                    LeftOperand: IOperation leftOperation,
                    RightOperand: IOperation rightOperation,
                }) => TryAddEqualizedMembersForConditionWithoutBinding(
                        builder, leftOperation, successRequirement, currentObject, additionalConditions.Append(rightOperation)),
                (false, IBinaryOperation
                {
                    OperatorKind: BinaryOperatorKind.ConditionalOr,
                    LeftOperand: IOperation leftOperation,
                    RightOperand: IOperation rightOperation,
                }) => TryAddEqualizedMembersForConditionWithoutBinding(
                        builder, leftOperation, successRequirement, currentObject, additionalConditions.Append(rightOperation)),
                (_, IIsPatternOperation
                {
                    Pattern: IPatternOperation isPattern
                }) => TryAddEqualizedMembersForIsPattern(isPattern),
                _ => null,
            };

            ISymbol? TryAddEqualizedMembersForIsPattern(IPatternOperation isPattern)
            {
                // got to the leftmost condition and it is an "is" pattern
                if (!successRequirement)
                {
                    // we could be in an "expect false for successful equality" condition
                    // and so we would want the pattern to be an "is not" pattern instead of an "is" pattern
                    if (isPattern is INegatedPatternOperation negatedPattern)
                    {
                        isPattern = negatedPattern.Pattern;
                    }
                    else
                    {
                        // if we don't see "is not" then the pattern sequence is incorrect
                        return null;
                    }
                }

                if (isPattern is IDeclarationPatternOperation
                    {
                        DeclaredSymbol: ISymbol otherVar,
                        MatchedType: INamedTypeSymbol matchedType,
                    } &&
                    matchedType.Equals(currentObject.GetSymbolType()))
                {
                    // found the correct binding, add any members we equate in the rest of the binary condition
                    // if we were in a binary condition at all
                    foreach (var otherCondition in additionalConditions)
                    {
                        TryAddEqualizedMembersForCondition(builder, otherCondition, successRequirement, currentObject, otherVar);
                    }
                    return otherVar;
                }

                // binding not found, so either something didn't make sense to us or the "is" didn't declare any variable
                return null;
            }
        }

        // checks for binary expressions of the type otherC == null or null == otherC
        // or a pattern against null like otherC is (not) null
        // and "otherC" is a reference to otherObject
        // takes successRequirement so that if we're in a constext where the operation evaluating to true
        // would end up being false within the equals method, we look for != instead
        private static bool IsNullCheck(
            IOperation operation,
            bool successRequirement,
            ISymbol otherObject)
        {
            return operation switch
            {
                IBinaryOperation
                {
                    LeftOperand: IOperation leftOperation,
                    RightOperand: IOperation rightOperation,
                } binaryOperation
                    // if success would return true, then we want the checked object to not be null
                    // so we expect a notEquals operator
                    => binaryOperation.OperatorKind == (successRequirement
                        ? BinaryOperatorKind.NotEquals
                        : BinaryOperatorKind.Equals) &&
                    // one of the objects must be a reference to the "otherObject"
                    // and the other must be a constant null literal
                    ((otherObject.Equals(GetReferencedSymbolObject(leftOperation)) &&
                            rightOperation.WalkDownConversion().IsNullLiteral()) ||
                        (otherObject.Equals(GetReferencedSymbolObject(rightOperation)) &&
                            leftOperation.WalkDownConversion().IsNullLiteral())),
                // matches: otherC is not null
                // equality must fail if this is false so we ensure successRequirement is true
                IIsPatternOperation
                {
                    Value: IOperation patternValue, Pattern: IPatternOperation pattern
                } => otherObject.Equals(GetReferencedSymbolObject(patternValue)) &&
                        // if condition success => return false, then we expect "is null"
                        (!successRequirement &&
                        pattern is IConstantPatternOperation constantPattern1 &&
                        constantPattern1.Value.WalkDownConversion().IsNullLiteral()) ||
                        (successRequirement &&
                        pattern is INegatedPatternOperation { Pattern: IConstantPatternOperation constantPattern2 } &&
                        constantPattern2.Value.WalkDownConversion().IsNullLiteral()),
                _ => false,
            };
        }

        private static bool TryAddMemberFromComparison(
            IMemberReferenceOperation memberReference1,
            IMemberReferenceOperation memberReference2,
            ISymbol currentObject,
            ISymbol otherObject,
            ArrayBuilder<IFieldSymbol> builder)
        {
            var leftObject = GetReferencedSymbolObject(memberReference1.Instance!);
            var rightObject = GetReferencedSymbolObject(memberReference2.Instance!);
            if (memberReference1.Member.Equals(memberReference2.Member) &&
                leftObject != null &&
                rightObject != null &&
                !leftObject.Equals(rightObject) &&
                (leftObject.Equals(currentObject) || leftObject.Equals(otherObject)) &&
                (rightObject.Equals(currentObject) || rightObject.Equals(otherObject)))
            {
                var field = memberReference1.Member is IPropertySymbol property
                    ? property.GetBackingFieldIfAny()
                    : memberReference1.Member as IFieldSymbol;
                if (field == null)
                {
                    // not a field or no backing field for property so member is invalid
                    return false;
                }

                builder.Add(field);
                return true;
            }
            return false;
        }

        private static ISymbol? GetReferencedSymbolObject(IOperation reference)
        {
            return reference.WalkDownConversion() switch
            {
                IInstanceReferenceOperation thisReference => thisReference.Type,
                ILocalReferenceOperation localReference => localReference.Local,
                IParameterReferenceOperation paramReference => paramReference.Parameter,
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
            ImmutableArray<IPropertySymbol> properties,
            ImmutableArray<IParameterSymbol> parameters,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var operation = (IConstructorBodyOperation)semanticModel.GetRequiredOperation(constructor, cancellationToken);

            var body = operation.BlockBody ?? operation.ExpressionBody;

            // We expect the constructor to have exactly one statement per parameter,
            // where the statement is a simple assignment from the parameter to the property
            if (body == null || body.Operations.Length != parameters.Length)
            {
                return false;
            }

            var propertiesAlreadyAssigned = new HashSet<ISymbol>();
            foreach (var bodyOperation in body.Operations)
            {
                if (bodyOperation is IExpressionStatementOperation
                    {
                        Operation: ISimpleAssignmentOperation
                        {
                            Target: IPropertyReferenceOperation { Property: IPropertySymbol property },
                            Value: IParameterReferenceOperation { Parameter: IParameterSymbol parameter }
                        }
                    })
                {
                    var propertyIndex = properties.IndexOf(property);
                    if (propertyIndex != -1 &&
                        !propertiesAlreadyAssigned.Contains(property) &&
                        // make sure the index of the property we assign as it would be placed in the primary constructor
                        // matches the current index of the parameter we use for the explicit constructor
                        propertyIndex == parameters.IndexOf(parameter))
                    {
                        // make sure we don't have duplicate assignment statements to the same property
                        propertiesAlreadyAssigned.Add(property);
                        continue;
                    }
                }
                // either we failed to match the assignment pattern, or we're not assigning to a moved property,
                // or we assigned to the same property more than once
                return false;
            }
            // all conditions passed individually, make sure all properties were assigned to
            return propertiesAlreadyAssigned.Count == properties.Length;
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
