// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRecord;

internal static class ConvertToRecordHelpers
{
    public static bool IsSimpleEqualsMethod(
        Compilation compilation,
        IMethodSymbol methodSymbol,
        IMethodBodyOperation methodBodyOperation,
        ImmutableArray<IFieldSymbol> expectedComparedFields)
    {
        if (methodSymbol.Name == nameof(Equals) &&
            methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean &&
            methodSymbol.Parameters.IsSingle())
        {
            var type = methodSymbol.ContainingType;
            var equatableType = GetIEquatableType(compilation, type);
            if (OverridesEquals(compilation, methodSymbol, equatableType))
            {
                if (equatableType != null &&
                    methodSymbol.Parameters.First().Type.SpecialType == SpecialType.System_Object &&
                    GetBlockOfMethodBody(methodBodyOperation) is IBlockOperation
                    {
                        Operations: [IReturnOperation
                        {
                            ReturnedValue: IInvocationOperation
                            {
                                Instance: IInstanceReferenceOperation,
                                TargetMethod: IMethodSymbol { Name: nameof(Equals) },
                                Arguments: [IArgumentOperation { Value: IOperation arg }]
                            }
                        }]
                    } && arg.WalkDownConversion() is IParameterReferenceOperation { Parameter: IParameterSymbol param }
                    && param.Equals(methodSymbol.Parameters.First()))
                {
                    // in this case where we have an Equals(C? other) from IEquatable but the current one
                    // is Equals(object? other), we accept something of the form:
                    // return Equals(other as C);
                    return true;
                }

                // otherwise we check to see which fields are compared (either by themselves or through properties)
                var actualFields = GetEqualizedFields(methodBodyOperation, methodSymbol);
                return actualFields.SetEquals(expectedComparedFields);
            }
        }

        return false;
    }

    public static INamedTypeSymbol? GetIEquatableType(Compilation compilation, INamedTypeSymbol containingType)
    {
        // can't use nameof since it's generic and we need the type parameter
        var equatable = compilation.GetBestTypeByMetadataName("System.IEquatable`1")?.Construct(containingType);
        return containingType.Interfaces.FirstOrDefault(iface => iface.Equals(equatable));
    }

    public static bool IsSimpleHashCodeMethod(
        Compilation compilation,
        IMethodSymbol methodSymbol,
        IMethodBodyOperation methodOperation,
        ImmutableArray<IFieldSymbol> expectedHashedFields)
    {
        if (methodSymbol.Name == nameof(GetHashCode) &&
            methodSymbol.Parameters.IsEmpty &&
            HashCodeAnalyzer.TryGetAnalyzer(compilation, out var analyzer))
        {
            // Hash Code method, see if it would be a default implementation that we can remove
            var (_, members, _) = analyzer.GetHashedMembers(
                methodSymbol, methodOperation.BlockBody ?? methodOperation.ExpressionBody);
            if (members != default)
            {
                // the user could access a member using either the property or the underlying field
                // so anytime they access a property instead of the underlying field we convert it to the
                // corresponding underlying field
                var actualMembers = members
                    .SelectAsArray(UnwrapPropertyToField).WhereNotNull().AsImmutable();

                return actualMembers.SetEquals(expectedHashedFields);
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if the method contents match a simple reference to the equals method
    /// which would be the compiler generated implementation
    /// </summary>
    public static bool IsDefaultEqualsOperator(IMethodBodyOperation operation)
    {
        // must look like
        // public static operator ==(C c1, object? c2)
        // {
        //  return c1.Equals(c2);
        // }
        // or
        // public static operator ==(C c1, object? c2) => c1.Equals(c2);
        return GetBlockOfMethodBody(operation) is IBlockOperation
        {
            // look for only one operation, a return operation that consists of an equals invocation
            Operations: [IReturnOperation { ReturnedValue: IOperation returnedValue }]
        } &&
        IsDotEqualsInvocation(returnedValue);
    }

    /// <summary>
    /// Whether the method simply returns !(equals), where "equals" is
    /// c1 == c2 or c1.Equals(c2)
    /// </summary>
    internal static bool IsDefaultNotEqualsOperator(
        IMethodBodyOperation operation)
    {
        // looking for:
        // return !(operand);
        // or:
        // => !(operand);
        if (GetBlockOfMethodBody(operation) is not IBlockOperation
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
            return true;

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
        return left != null && right != null && !left.Equals(right);
    }

    /// <summary>
    /// Matches constructors where each statement simply assigns one of the provided parameters to one of the provided properties
    /// with no duplicate assignment or any other type of statement
    /// </summary>
    /// <param name="operation">Constructor body</param>
    /// <param name="properties">Properties expected to be assigned (would be replaced with positional constructor).
    /// Will re-order this list to match parameter order if successful.</param>
    /// <param name="parameters">Constructor parameters</param>
    /// <returns>Whether the constructor body matches the pattern described</returns>
    public static bool IsSimplePrimaryConstructor(
        IConstructorBodyOperation operation,
        ImmutableArray<IPropertySymbol> properties,
        ImmutableArray<IParameterSymbol> parameters,
        out ImmutableArray<IPropertySymbol> orderedProperties)
    {
        orderedProperties = default;
        if (GetBlockOfMethodBody(operation) is not { Operations.Length: int opLength } ||
            opLength != properties.Length)
        {
            return false;
        }

        var assignmentValues = GetAssignmentValuesForConstructor(operation,
            assignment => (assignment as IParameterReferenceOperation)?.Parameter);

        // we must assign to all the properties (keys) and use all the parameters (values)
        if (!assignmentValues.Keys.SetEquals(properties) ||
            !assignmentValues.Values.SetEquals(parameters))
        {
            return false;
        }

        // order properties in order of the parameters that they were assigned to
        // e.g if we originally have Properties: [int Y, int X]
        // and constructor:
        // public C(int x, int y)
        // {
        //     X = x;
        //     Y = y;
        // }
        // then we would re-order the properties to: [int X, int Y]
        orderedProperties = properties
            .OrderBy(property => parameters.IndexOf(assignmentValues[property]))
            .AsImmutable();
        return true;
    }

    /// <summary>
    /// Checks to see if all fields/properties were assigned from the parameter
    /// </summary>
    /// <param name="operation">constructor body</param>
    /// <param name="fields">all instance fields, including backing fields of constructors</param>
    /// <param name="parameter">parameter to copy constructor</param>
    public static bool IsSimpleCopyConstructor(
        IConstructorBodyOperation operation,
        ImmutableArray<IFieldSymbol> fields,
        IParameterSymbol parameter)
    {
        if (GetBlockOfMethodBody(operation) is not { Operations.Length: int opLength } ||
            opLength != fields.Length)
        {
            return false;
        }

        var assignmentValues = GetAssignmentValuesForConstructor(operation,
            assignment => assignment switch
            {
                IPropertyReferenceOperation
                {
                    Instance: IParameterReferenceOperation { Parameter: IParameterSymbol referencedParameter },
                    Property: IPropertySymbol referencedProperty
                } =>
                    referencedParameter.Equals(parameter) ? referencedProperty.GetBackingFieldIfAny() : null,
                IFieldReferenceOperation
                {
                    Instance: IParameterReferenceOperation { Parameter: IParameterSymbol referencedParameter },
                    Field: IFieldSymbol referencedField
                } =>
                   referencedParameter.Equals(parameter) ? referencedField : null,
                _ => null
            });

        // left hand side of each assignment
        var assignedUnderlyingFields = assignmentValues.Keys.SelectAsArray(UnwrapPropertyToField);

        // Each right hand assignment should assign the same property.
        // All assigned properties should be equal (in potentially a different order)
        // to all the properties we would be moving
        return assignedUnderlyingFields.SequenceEqual(assignmentValues.Values) &&
            assignedUnderlyingFields.SetEquals(fields);
    }

    /// <summary>
    /// Given a non-primary, non-copy constructor, get expressions that are assigned to
    /// primary constructor properties via simple assignment.
    /// </summary>
    /// <param name="operation">The constructor body operation</param>
    /// <param name="positionalParams">the primary constructor parameters</param>
    /// <returns>
    /// Expressions that were assigned to a primary constructor property in the constructor,
    /// or default/null if there wasn't an assignment found. Returned in order of primary parameters.
    /// </returns>
    /// <remarks>
    /// Example (assume we decided on positional parameters int Foo, bool Bar, int Baz):
    /// <code>
    /// public C(int foo, bool bar)
    /// {
    ///     Bar = bar;
    ///     Foo = foo;
    ///     Mumble = 0;
    /// }
    /// </code>
    /// we would return: [foo, bar, default]
    /// where foo and bar are the nodes in the assignment, and default is factory constructed.
    /// </remarks>
    public static ImmutableArray<ExpressionSyntax> GetAssignmentValuesForNonPrimaryConstructor(
        IConstructorBodyOperation operation,
        ImmutableArray<IPropertySymbol> positionalParams)
    {
        // make sure the assignment wouldn't reference local variables we may have declared
        var assignmentValues = GetAssignmentValuesForConstructor(operation,
            assignment => IsSafeAssignment(assignment)
                ? assignment.Syntax as ExpressionSyntax
                : null);

        if (operation.Initializer is
            IInvocationOperation { Arguments: ImmutableArray<IArgumentOperation> args })
        {
            var additionalValuesBuilder = assignmentValues.ToBuilder();
            // in a "base" or "this" initializer
            foreach (var arg in args)
            {
                if (arg is { Parameter: IParameterSymbol param, Value.Syntax: ExpressionSyntax captured })
                {
                    // We're looking to see if this initializer is a primary constructor,
                    // i.e. the parameters are declared as auto-implemented properties in the record definition.
                    // Since there's no way to associate positional parameters (from the primary constructor)
                    // to the properties that they declare other than by comparing their declaration locations,
                    // we have to do this rather convoluted comparison.
                    // Note: We can use AssociatedSymbol once this is implemented:
                    // https://github.com/dotnet/roslyn/issues/54286
                    var positionalParam = param.ContainingSymbol.ContainingType.GetMembers().FirstOrDefault(member
                            => member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() ==
                                param.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax());
                    if (positionalParam is IPropertySymbol property)
                    {
                        if (additionalValuesBuilder.ContainsKey(property))
                        {
                            // don't allow assignment to the same property more than once
                            return [];
                        }

                        additionalValuesBuilder.Add(property, captured);
                    }
                }
            }

            assignmentValues = additionalValuesBuilder.ToImmutable();
        }

        var expressions = GetAssignmentExpressionsFromValuesMap(positionalParams, assignmentValues);

        return expressions;
    }

    /// <summary>
    /// Given an object creation with a block initializer and a parameterless constructor declaration,
    /// finds values that were assigned to the primary constructor parameters
    /// </summary>
    /// <param name="operation">Object creation expression operation</param>
    /// <param name="positionalParams">primary constructor parameters</param>
    /// <returns>
    /// values that were assigned to primary constructor parameters, in order of the passed in primary constructor
    /// </returns>
    /// <remarks>
    ///  Example (assume we decided on positional parameters int Foo, bool Bar, int Baz):
    /// <code>
    /// var c = new C
    /// {
    ///     Bar = true;
    ///     Foo = 10;
    ///     Mumble = 0;
    /// };
    /// </code>
    /// We would return [10, true, default]
    /// where 10 and true are the actual nodes and default was a constructed node
    /// </remarks>
    public static ImmutableArray<ExpressionSyntax> GetAssignmentValuesFromObjectCreation(
        IObjectCreationOperation operation,
        ImmutableArray<IPropertySymbol> positionalParams)
    {
        // we want to be very careful about when we refactor because if the user has a constructor
        // and initializer it could be what they intend. Since we gave initializers to all non-primary
        // constructors they already have, any calls to an explicit constructor with additional block initialization
        // still work absolutely fine. Further, we can't necessarily associate their constructor args to
        // primary constructor args or any other constructor args. Therefore,
        // the only time we want to actually make a change is if they use the default no-param constructor,
        // and a block initializer.
        if (operation is IObjectCreationOperation
            {
                Arguments: ImmutableArray<IArgumentOperation> { IsEmpty: true },
                Initializer: IObjectOrCollectionInitializerOperation initializer,
                Constructor: IMethodSymbol { IsImplicitlyDeclared: true }
            })
        {
            var dictionaryBuilder = ImmutableDictionary<ISymbol, ExpressionSyntax>.Empty.ToBuilder();

            foreach (var assignment in initializer.Initializers)
            {
                if (assignment is ISimpleAssignmentOperation
                    {
                        Target: IPropertyReferenceOperation { Property: IPropertySymbol property },
                        Value: IOperation { Syntax: ExpressionSyntax syntax }
                    })
                {
                    dictionaryBuilder.Add(property, syntax);
                }
            }

            var expressions = GetAssignmentExpressionsFromValuesMap(positionalParams, dictionaryBuilder.ToImmutable());

            return expressions;
        }

        // no initializer or uses explicit constructor, no need to make a change
        return [];
    }

    private static ImmutableArray<ExpressionSyntax> GetAssignmentExpressionsFromValuesMap(
        ImmutableArray<IPropertySymbol> positionalParams,
        ImmutableDictionary<ISymbol, ExpressionSyntax> assignmentValues)
    => positionalParams.SelectAsArray(property =>
    {
        if (assignmentValues.ContainsKey(property))
        {
            return assignmentValues[property];
        }
        else
        {
            return SyntaxFactory.LiteralExpression(
                property.Type.NullableAnnotation == NullableAnnotation.Annotated
                    ? SyntaxKind.NullLiteralExpression
                    : SyntaxKind.DefaultLiteralExpression);
        }
    });

    private static ImmutableDictionary<ISymbol, T> GetAssignmentValuesForConstructor<T>(
        IConstructorBodyOperation constructorOperation,
        Func<IOperation, T?> captureAssignedSymbol)
    {
        var body = GetBlockOfMethodBody(constructorOperation);
        var dictionaryBuilder = ImmutableDictionary<ISymbol, T>.Empty.ToBuilder();

        // We expect the constructor to have exactly one statement per property,
        // where the statement is a simple assignment from the parameter's property to the property
        if (body == null)
        {
            return ImmutableDictionary<ISymbol, T>.Empty;
        }

        foreach (var operation in body.Operations)
        {
            if (operation is IExpressionStatementOperation
                {
                    Operation: ISimpleAssignmentOperation
                    {
                        Target: IOperation assignee,
                        Value: IOperation assignment
                    }
                } &&
                captureAssignedSymbol(assignment) is T captured)
            {
                ISymbol? symbol = assignee switch
                {
                    IFieldReferenceOperation
                    { Instance: IInstanceReferenceOperation, Field: IFieldSymbol field }
                        => field,
                    IPropertyReferenceOperation
                    { Instance: IInstanceReferenceOperation, Property: IPropertySymbol property }
                        => property,
                    _ => null,
                };

                if (symbol != null)
                {
                    if (dictionaryBuilder.ContainsKey(symbol))
                    {
                        // don't allow assignment to the same property more than once
                        return ImmutableDictionary<ISymbol, T>.Empty;
                    }

                    dictionaryBuilder.Add(symbol, captured);
                }
            }
        }

        return dictionaryBuilder.ToImmutable();
    }

    /// <summary>
    /// Determines whether the operation is safe to move into the "this(...)" initializer
    /// i.e. Doesn't reference any other created variables but the parameters
    /// </summary>
    private static bool IsSafeAssignment(IOperation operation)
    {
        if (operation is ILocalReferenceOperation)
        {
            return false;
        }

        return operation.ChildOperations.All(IsSafeAssignment);
    }

    /// <summary>
    /// Get all the fields (including implicit fields underlying properties) that this
    /// equals method compares
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="methodSymbol">the symbol of the equals method</param>
    /// <returns></returns>
    private static ImmutableArray<IFieldSymbol> GetEqualizedFields(
        IMethodBodyOperation operation,
        IMethodSymbol methodSymbol)
    {
        var type = methodSymbol.ContainingType;

        var body = GetBlockOfMethodBody(operation);

        if (body == null || !methodSymbol.Parameters.IsSingle())
            return [];

        var bodyOps = body.Operations;
        var parameter = methodSymbol.Parameters.First();
        using var _1 = ArrayBuilder<IFieldSymbol>.GetInstance(out var fields);
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
            if (bodyOps is [IReturnOperation { ReturnedValue: IOperation value }] &&
                TryAddEqualizedFieldsForConditionWithoutTypedVariable(
                    value, successRequirement: true, type, fields, out var _2))
            {
                // we're done, no more statements to check
                return fields.ToImmutable();
            }
            // check for the first statement as an explicit cast to a variable declaration
            // like: var otherC = other as C;
            else if (TryGetAssignmentFromParameterWithExplicitCast(bodyOps.FirstOrDefault(), parameter, out otherC))
            {
                // ignore the first statement as we just ensured it was a cast
                statementsToCheck = bodyOps.Skip(1);
            }
            // check for the first statement as an if statement where the cast check occurs
            // and a variable assignment happens (either in the if or in a following statement)
            else if (!TryGetBindingCastInFirstIfStatement(bodyOps, parameter, type, fields, out otherC, out statementsToCheck))
            {
                return [];
            }
        }

        if (otherC == null || statementsToCheck == null ||
            !TryAddEqualizedFieldsForStatements(statementsToCheck, otherC, type, fields))
        {
            // no patterns matched to bind variable or statements didn't match expectation
            return [];
        }

        return fields.ToImmutable();
    }

    /// <summary>
    /// Matches: var otherC = (C) other;
    /// or: var otherC = other as C;
    /// </summary>
    private static bool TryGetAssignmentFromParameterWithExplicitCast(
        IOperation? operation,
        IParameterSymbol parameter,
        [NotNullWhen(true)] out ISymbol? assignedVariable)
    {
        assignedVariable = null;
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
            assignedVariable = castOther;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get the referenced parameter (and unwraps implicit cast if necessary) or null if a parameter wasn't referenced
    /// </summary>
    /// <param name="operation">The operation for which to get the parameter</param>
    /// <returns>the referenced parameter or null if unable to find</returns>
    private static IParameterSymbol? GetParamFromArgument(IOperation operation)
        => (operation.WalkDownConversion() as IParameterReferenceOperation)?.Parameter;

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
    /// matches form:
    /// c1.Equals(c2)
    /// where c1 and c2 are parameter references
    /// </summary>
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
    /// checks for binary expressions of the type otherC == null or null == otherC
    /// or a pattern against null like otherC is (not) null
    /// and "otherC" is a reference to otherObject.
    /// </summary>
    /// <param name="operation">Operation to check for</param>
    /// <param name="successRequirement">if we're in a context where the operation evaluating to true
    /// would end up being false within the equals method, we look for != instead</param>
    /// <param name="otherObject">Object to be compared to null</param>
    private static bool IsNullCheck(
        IOperation operation,
        bool successRequirement,
        ISymbol otherObject)
    {
        if (operation is IBinaryOperation binOp)
        {
            // if success would return true, then we want the checked object to not be null
            // so we expect a notEquals operator
            var expectedKind = successRequirement
                ? BinaryOperatorKind.NotEquals
                : BinaryOperatorKind.Equals;

            return binOp.OperatorKind == expectedKind &&
                // one of the objects must be a reference to the "otherObject"
                // and the other must be a constant null literal
                AreConditionsSatisfiedEitherOrder(binOp.LeftOperand, binOp.RightOperand,
                    op => op.WalkDownConversion().IsNullLiteral(),
                    op => otherObject.Equals(GetReferencedSymbolObject(op)));
        }
        else if (operation is IIsPatternOperation patternOp)
        {
            // matches: otherC is null
            // or: otherC is not null
            // based on successRequirement
            IConstantPatternOperation? constantPattern;
            if (successRequirement)
            {
                constantPattern = (patternOp.Pattern as INegatedPatternOperation)?.
                    Pattern as IConstantPatternOperation;
            }
            else
            {
                constantPattern = patternOp.Pattern as IConstantPatternOperation;
            }

            return constantPattern != null &&
                otherObject.Equals(GetReferencedSymbolObject(patternOp.Value)) &&
                constantPattern.Value.WalkDownConversion().IsNullLiteral();
        }

        // neither of the expected forms
        return false;
    }

    private static bool ReturnsFalseImmediately(IEnumerable<IOperation> operation)
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
    /// looks just at conditional expressions such as "A == other.A &amp;&amp; B == other.B..."
    /// To determine which members were accessed and compared
    /// </summary>
    /// <param name="condition">Condition to look at, should be a boolean expression</param>
    /// <param name="successRequirement">Whether to look for operators that would indicate equality success
    /// (==, .Equals, &amp;&amp;) or inequality operators (!=, ||)</param>
    /// <param name="currentObject">Symbol that would be referenced with this</param>
    /// <param name="otherObject">symbol representing other object, either from a param or cast as a local</param>
    /// <param name="builder">Builder to add members to</param>
    /// <returns>true if addition was successful, false if we see something odd 
    /// (equality checking in the wrong order, side effects, etc)</returns>
    private static bool TryAddEqualizedFieldsForCondition(
        IOperation condition,
        bool successRequirement,
        ISymbol currentObject,
        ISymbol otherObject,
        ArrayBuilder<IFieldSymbol> builder)
    => (successRequirement, condition) switch
    {
        // if we see a not we want to invert the current success status
        // e.g !(A != other.A || B != other.B) is equivalent to (A == other.A && B == other.B)
        // using DeMorgans law. We recurse to see any members accessed
        (_, IUnaryOperation { OperatorKind: UnaryOperatorKind.Not, Operand: IOperation newCondition })
            => TryAddEqualizedFieldsForCondition(newCondition, !successRequirement, currentObject, otherObject, builder),
        // We want our equality check to be exhaustive, i.e. all checks must pass for the condition to pass
        // we recurse into each operand to try to find some props to bind
        (true, IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd } andOp)
            => TryAddEqualizedFieldsForCondition(andOp.LeftOperand, successRequirement, currentObject, otherObject, builder) &&
                TryAddEqualizedFieldsForCondition(andOp.RightOperand, successRequirement, currentObject, otherObject, builder),
        // Exhaustive binary operator for inverted checks via DeMorgan's law
        // We see an or here, but we're in a context where this being true will return false
        // for example: return !(expr || expr)
        // or: if (expr || expr) return false;
        (false, IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalOr } orOp)
            => TryAddEqualizedFieldsForCondition(orOp.LeftOperand, successRequirement, currentObject, otherObject, builder) &&
                TryAddEqualizedFieldsForCondition(orOp.RightOperand, successRequirement, currentObject, otherObject, builder),
        // we are actually comparing two values that are potentially members,
        // e.g: return A == other.A;
        (true, IBinaryOperation
        {
            OperatorKind: BinaryOperatorKind.Equals,
            LeftOperand: IMemberReferenceOperation leftMemberReference,
            RightOperand: IMemberReferenceOperation rightMemberReference,
        }) => TryAddFieldFromComparison(leftMemberReference, rightMemberReference, currentObject, otherObject, builder),
        // we are comparing two potential members, but in a context where if the expression is true, we return false
        // e.g: return !(A != other.A); 
        (false, IBinaryOperation
        {
            OperatorKind: BinaryOperatorKind.NotEquals,
            LeftOperand: IMemberReferenceOperation leftMemberReference,
            RightOperand: IMemberReferenceOperation rightMemberReference,
        }) => TryAddFieldFromComparison(leftMemberReference, rightMemberReference, currentObject, otherObject, builder),
        // equals invocation, something like: A.Equals(other.A)
        (true, IInvocationOperation
        {
            TargetMethod.Name: nameof(Equals),
            Instance: IMemberReferenceOperation invokedOn,
            Arguments: [IMemberReferenceOperation arg]
        }) => TryAddFieldFromComparison(invokedOn, arg, currentObject, otherObject, builder),
        // some other operation, or an incorrect operation (!= when we expect == based on context, etc).
        // If one of the conditions is just a null check on the "otherObject", then it's valid but doesn't check any members
        // Otherwise we fail as it has unknown behavior
        _ => IsNullCheck(condition, successRequirement, otherObject)
    };

    /// <summary>
    /// Same as <see cref="TryAddEqualizedFieldsForCondition"/> but we're looking for
    /// a variable binding through an "is" pattern first/>
    /// </summary>
    /// <returns>the cast parameter symbol if found, null if not</returns>
    private static bool TryAddEqualizedFieldsForConditionWithoutTypedVariable(
        IOperation condition,
        bool successRequirement,
        ISymbol currentObject,
        ArrayBuilder<IFieldSymbol> builder,
        [NotNullWhen(true)] out ISymbol? boundVariable,
        IEnumerable<IOperation>? additionalConditions = null)
    {
        boundVariable = null;
        additionalConditions ??= [];
        return (successRequirement, condition) switch
        {
            (_, IUnaryOperation { OperatorKind: UnaryOperatorKind.Not, Operand: IOperation newCondition })
                => TryAddEqualizedFieldsForConditionWithoutTypedVariable(
                    newCondition,
                    !successRequirement,
                    currentObject,
                    builder,
                    out boundVariable,
                    additionalConditions),
            (true, IBinaryOperation
            {
                OperatorKind: BinaryOperatorKind.ConditionalAnd,
                LeftOperand: IOperation leftOperation,
                RightOperand: IOperation rightOperation,
            }) => TryAddEqualizedFieldsForConditionWithoutTypedVariable(
                    leftOperation,
                    successRequirement,
                    currentObject,
                    builder,
                    out boundVariable,
                    additionalConditions.Append(rightOperation)),
            (false, IBinaryOperation
            {
                OperatorKind: BinaryOperatorKind.ConditionalOr,
                LeftOperand: IOperation leftOperation,
                RightOperand: IOperation rightOperation,
            }) => TryAddEqualizedFieldsForConditionWithoutTypedVariable(
                leftOperation,
                successRequirement,
                currentObject,
                builder,
                out boundVariable,
                additionalConditions.Append(rightOperation)),
            (_, IIsPatternOperation
            {
                Pattern: IPatternOperation isPattern
            }) => TryGetBoundVariableForIsPattern(isPattern, out boundVariable),
            _ => false,
        };

        bool TryGetBoundVariableForIsPattern(IPatternOperation isPattern, [NotNullWhen(true)] out ISymbol? boundVariable)
        {
            boundVariable = null;
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
                    return false;
                }
            }

            if (isPattern is IDeclarationPatternOperation
                {
                    DeclaredSymbol: ISymbol otherVar,
                    MatchedType: INamedTypeSymbol matchedType,
                } &&
                matchedType.Equals(currentObject.GetSymbolType()) &&
                // found the correct binding, add any members we equate in the rest of the binary condition
                // if we were in a binary condition at all, and signal failure if any condition is bad
                additionalConditions.All(otherCondition => TryAddEqualizedFieldsForCondition(
                    otherCondition, successRequirement, currentObject, otherVar, builder)))
            {
                boundVariable = otherVar;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Match a list of statements and add members that are compared
    /// </summary>
    /// <param name="statementsToCheck">operations which should compare members</param>
    /// <param name="otherC">non-this comparison of the type we're equating</param>
    /// <param name="type">the this symbol</param>
    /// <param name="builder">builder for property/field comparisons we encounter</param>
    /// <returns>whether every statment was one of the expected forms</returns>
    private static bool TryAddEqualizedFieldsForStatements(
        IEnumerable<IOperation> statementsToCheck,
        ISymbol otherC,
        INamedTypeSymbol type,
        ArrayBuilder<IFieldSymbol> builder)
        => statementsToCheck.FirstOrDefault() switch
        {
            IReturnOperation
            {
                ReturnedValue: ILiteralOperation
                {
                    ConstantValue.HasValue: true,
                    ConstantValue.Value: true,
                }
            }
                // we are done with the comparison, the final statment does no checks
                => true,
            IReturnOperation { ReturnedValue: IOperation value } => TryAddEqualizedFieldsForCondition(
                value, successRequirement: true, currentObject: type, otherObject: otherC, builder: builder),
            IConditionalOperation
            {
                Condition: IOperation condition,
                WhenTrue: IOperation whenTrue,
                WhenFalse: var whenFalse,
            }
                // 1. Check structure of if statment, get success requirement
                // and any potential statments in the non failure block
                // 2. Check condition for compared members
                // 3. Check remaining members in non failure block
                => TryGetSuccessCondition(whenTrue, whenFalse, statementsToCheck.Skip(1),
                    out var successRequirement, out var remainingStatements) &&
                TryAddEqualizedFieldsForCondition(
                        condition, successRequirement, type, otherC, builder) &&
                TryAddEqualizedFieldsForStatements(remainingStatements, otherC, type, builder),
            _ => false
        };

    private static bool TryAddFieldFromComparison(
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
            AreConditionsSatisfiedEitherOrder(leftObject, rightObject, currentObject.Equals, otherObject.Equals))
        {
            var field = UnwrapPropertyToField(memberReference1.Member);
            if (field == null)
                // not a field or no backing field for property so member is invalid
                return false;

            builder.Add(field);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Matches a pattern where the first statement is an if statement that ensures a cast
    /// of the parameter to the correct type, and either binds it through an "is" pattern
    /// or later assigns it to a local varaiable
    /// </summary>
    /// <param name="bodyOps">method body to search in</param>
    /// <param name="parameter">uncast object parameter</param>
    /// <param name="type">type which is being called</param>
    /// <param name="builder">members that may have been incidentally checked</param>
    /// <param name="otherC">if matched, the variable that the cast parameter was assigned to</param>
    /// <param name="statementsToCheck">remaining non-check, non-assignment operations
    /// to look for additional compared members. This can have statements even if there was no binding
    /// as long as it found an if check that checks the correct type</param>
    /// <returns>whether or not the pattern matched</returns>
    private static bool TryGetBindingCastInFirstIfStatement(
        ImmutableArray<IOperation> bodyOps,
        IParameterSymbol parameter,
        INamedTypeSymbol type,
        ArrayBuilder<IFieldSymbol> builder,
        [NotNullWhen(true)] out ISymbol? otherC,
        [NotNullWhen(true)] out IEnumerable<IOperation>? statementsToCheck)
    {
        otherC = null;
        statementsToCheck = null;

        // we require the if statement (with or without a cast) to be the first operation in the body
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

        // if we have no else block, we could get no remaining statements, in that case we take all the
        // statments after the if condition operation
        statementsToCheck = !remainingStatments.IsEmpty() ? remainingStatments : bodyOps.Skip(1);

        // checks for simple "is" or "is not" statement without a variable binding
        ITypeSymbol? testType = null;
        IParameterSymbol? referencedParameter = null;
        if (successRequirement)
        {
            if (condition is IIsTypeOperation typeCondition)
            {
                testType = typeCondition.TypeOperand;
                referencedParameter = (typeCondition.ValueOperand as IParameterReferenceOperation)?.Parameter;
            }
        }
        else
        {
            if (condition is IIsPatternOperation
                {
                    Value: IParameterReferenceOperation parameterReference,
                    Pattern: INegatedPatternOperation
                    {
                        Pattern: ITypePatternOperation typePattern
                    }
                })
            {
                testType = typePattern.MatchedType;
                referencedParameter = parameterReference.Parameter;
            }
        }

        if (testType != null && referencedParameter != null &&
            testType.Equals(type) && referencedParameter.Equals(parameter))
        {
            // found correct pattern/type check, so we know we have something equivalent to
            // if (other is C) { ... } else return false;
            // we look for an explicit cast to assign a variable like:
            // var otherC = (C)other;
            // var otherC = other as C;
            if (TryGetAssignmentFromParameterWithExplicitCast(statementsToCheck.FirstOrDefault(), parameter, out otherC))
            {
                statementsToCheck = statementsToCheck.Skip(1);
                return true;
            }

            return false;
        }
        // look for the condition to also contain a binding to a variable and optionally additional
        // checks based on that assigned variable
        return TryAddEqualizedFieldsForConditionWithoutTypedVariable(
            condition, successRequirement, type, builder, out otherC);
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
        IEnumerable<IOperation> otherOps,
        out bool successRequirement,
        out IEnumerable<IOperation> remainingStatements)
    {
        // this will be changed if we successfully match the pattern
        successRequirement = default;
        // this could be empty even if we match, if there is no else block
        remainingStatements = [];

        // all the operations that would happen after the condition is true or false
        // branches can either be block bodies or single statements
        // each branch is followed by statements outside the branch either way
        var trueOps = ((whenTrue as IBlockOperation)?.Operations ?? [whenTrue])
            .Concat(otherOps);
        var falseOps = ((whenFalse as IBlockOperation)?.Operations ??
            (whenFalse != null
                ? [whenFalse]
                : ImmutableArray<IOperation>.Empty))
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
            // both or neither fit the return false pattern, this if statement either doesn't do
            // anything or does something too complex for us, so we assume it's not a default.
            return false;

        // when condition succeeds (evaluates to true), we return false
        // so for equality the condition should not succeed
        successRequirement = !ReturnsFalseImmediately(trueOps);
        remainingStatements = successRequirement ? trueOps : falseOps;
        return true;
    }

    /// <summary>
    /// Whether the equals method overrides object or IEquatable Equals method
    /// </summary>
    private static bool OverridesEquals(Compilation compilation, IMethodSymbol equals, INamedTypeSymbol? equatableType)
    {
        if (equatableType != null &&
            equatableType.GetMembers(nameof(Equals)).FirstOrDefault() is IMethodSymbol equatableEquals &&
            equals.Equals(equals.ContainingType.FindImplementationForInterfaceMember(equatableEquals)))
        {
            return true;
        }

        var objectType = compilation.GetSpecialType(SpecialType.System_Object);
        var objectEquals = objectType?.GetMembers(nameof(Equals)).FirstOrDefault() as IMethodSymbol;
        var curr = equals;
        while (curr != null)
        {
            if (curr.Equals(objectEquals))
                return true;
            curr = curr.OverriddenMethod;
        }

        return false;
    }

    private static IBlockOperation? GetBlockOfMethodBody(IMethodBodyBaseOperation body)
        => body.BlockBody ?? body.ExpressionBody;

    private static IFieldSymbol? UnwrapPropertyToField(ISymbol propertyOrField)
        => propertyOrField switch
        {
            IPropertySymbol prop => prop.GetBackingFieldIfAny(),
            IFieldSymbol field => field,
            _ => null
        };

    private static bool AreConditionsSatisfiedEitherOrder<T>(T firstItem, T secondItem,
        Func<T, bool> firstCondition, Func<T, bool> secondCondition)
    {
        return (firstCondition(firstItem) && secondCondition(secondItem))
            || (firstCondition(secondItem) && secondCondition(firstItem));
    }
}
