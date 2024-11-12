// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class ConversionsBase
    {
        private UserDefinedConversionResult AnalyzeExplicitUserDefinedConversions(
           BoundExpression sourceExpression,
           TypeSymbol source,
           TypeSymbol target,
           bool isChecked,
           ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(sourceExpression is null || Compilation is not null);
            Debug.Assert(sourceExpression != null || (object)source != null);
            Debug.Assert((object)target != null);

            // SPEC: A user-defined explicit conversion from type S to type T is processed
            // SPEC: as follows:

            // SPEC: Find the set of types D from which user-defined conversion operators
            // SPEC: will be considered...
            var d = ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol ConstrainedToTypeOpt)>.GetInstance();
            ComputeUserDefinedExplicitConversionTypeSet(source, target, d, ref useSiteInfo);

            // SPEC: Find the set of applicable user-defined and lifted conversion operators, U...
            var ubuild = ArrayBuilder<UserDefinedConversionAnalysis>.GetInstance();
            ComputeApplicableUserDefinedExplicitConversionSet(sourceExpression, source, target, isChecked: isChecked, d, ubuild, ref useSiteInfo);
            d.Free();
            ImmutableArray<UserDefinedConversionAnalysis> u = ubuild.ToImmutableAndFree();

            // SPEC: If U is empty, the conversion is undefined and a compile-time error occurs.
            if (u.Length == 0)
            {
                return UserDefinedConversionResult.NoApplicableOperators(u);
            }

            // SPEC: Find the most specific source type SX of the operators in U...
            TypeSymbol sx = MostSpecificSourceTypeForExplicitUserDefinedConversion(u, sourceExpression, source, ref useSiteInfo);
            if ((object)sx == null)
            {
                return UserDefinedConversionResult.NoBestSourceType(u);
            }

            // SPEC: Find the most specific target type TX of the operators in U...
            TypeSymbol tx = MostSpecificTargetTypeForExplicitUserDefinedConversion(u, target, ref useSiteInfo);
            if ((object)tx == null)
            {
                return UserDefinedConversionResult.NoBestTargetType(u);
            }

            int? best = MostSpecificConversionOperator(sx, tx, u);
            if (best == null)
            {
                return UserDefinedConversionResult.Ambiguous(u);
            }

            return UserDefinedConversionResult.Valid(u, best.Value);
        }

        private static void ComputeUserDefinedExplicitConversionTypeSet(TypeSymbol source, TypeSymbol target, ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol ConstrainedToTypeOpt)> d, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // Spec 6.4.5: User-defined explicit conversions
            //   Find the set of types, D, from which user-defined conversion operators will be considered. 
            //   This set consists of S0 (if S0 is a class or struct), the base classes of S0 (if S0 is a class),
            //   T0 (if T0 is a class or struct), and the base classes of T0 (if T0 is a class).

            AddTypesParticipatingInUserDefinedConversion(d, source, includeBaseTypes: true, useSiteInfo: ref useSiteInfo);
            AddTypesParticipatingInUserDefinedConversion(d, target, includeBaseTypes: true, useSiteInfo: ref useSiteInfo);
        }

        private void ComputeApplicableUserDefinedExplicitConversionSet(
            BoundExpression sourceExpression,
            TypeSymbol source,
            TypeSymbol target,
            bool isChecked,
            ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol ConstrainedToTypeOpt)> d,
            ArrayBuilder<UserDefinedConversionAnalysis> u,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(sourceExpression is null || Compilation is not null);
            Debug.Assert(sourceExpression != null || (object)source != null);
            Debug.Assert((object)target != null);
            Debug.Assert(d != null);
            Debug.Assert(u != null);

            bool haveInterfaces = false;

            foreach ((NamedTypeSymbol declaringType, TypeParameterSymbol constrainedToTypeOpt) in d)
            {
                if (declaringType.IsInterface)
                {
                    Debug.Assert(constrainedToTypeOpt is not null);
                    haveInterfaces = true;
                }
                else
                {
                    addCandidatesFromType(constrainedToTypeOpt: null, declaringType, sourceExpression, source, target, isChecked: isChecked, u, ref useSiteInfo);
                }
            }

            if (u.Count == 0 && haveInterfaces)
            {
                foreach ((NamedTypeSymbol declaringType, TypeParameterSymbol constrainedToTypeOpt) in d)
                {
                    if (declaringType.IsInterface)
                    {
                        addCandidatesFromType(constrainedToTypeOpt: constrainedToTypeOpt, declaringType, sourceExpression, source, target, isChecked: isChecked, u, ref useSiteInfo);
                    }
                }
            }

            void addCandidatesFromType(
                TypeParameterSymbol constrainedToTypeOpt,
                NamedTypeSymbol declaringType,
                BoundExpression sourceExpression,
                TypeSymbol source,
                TypeSymbol target,
                bool isChecked,
                ArrayBuilder<UserDefinedConversionAnalysis> u,
                ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                AddUserDefinedConversionsToExplicitCandidateSet(sourceExpression, source, target, u, constrainedToTypeOpt, declaringType, isExplicit: true, isChecked: isChecked, ref useSiteInfo);
                AddUserDefinedConversionsToExplicitCandidateSet(sourceExpression, source, target, u, constrainedToTypeOpt, declaringType, isExplicit: false, isChecked: isChecked, ref useSiteInfo);
            }
        }

        private void AddUserDefinedConversionsToExplicitCandidateSet(
            BoundExpression sourceExpression,
            TypeSymbol source,
            TypeSymbol target,
            ArrayBuilder<UserDefinedConversionAnalysis> u,
            TypeParameterSymbol constrainedToTypeOpt,
            NamedTypeSymbol declaringType,
            bool isExplicit,
            bool isChecked,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(sourceExpression is null || Compilation is not null);
            Debug.Assert(sourceExpression != null || (object)source != null);
            Debug.Assert((object)target != null);
            Debug.Assert(u != null);
            Debug.Assert((object)declaringType != null);

            // SPEC: Find the set of applicable user-defined and lifted conversion operators, U.
            // SPEC: The set consists of the user-defined and lifted implicit or explicit 
            // SPEC: conversion operators declared by the classes and structs in D that convert 
            // SPEC: from a type encompassing E or encompassed by S (if it exists) to a type
            // SPEC: encompassing or encompassed by T. 

            // DELIBERATE SPEC VIOLATION:
            //
            // The spec here essentially says that we add an applicable "regular" conversion and 
            // an applicable lifted conversion, if there is one, to the candidate set, and then
            // let them duke it out to determine which one is "best".
            //
            // This is not at all what the native compiler does, and attempting to implement
            // the specification, or slight variations on it, produces too many backwards-compatibility
            // breaking changes.
            //
            // The native compiler deviates from the specification in two major ways here.
            // First, it does not add *both* the regular and lifted forms to the candidate set.
            // Second, the way it characterizes a "lifted" form is very, very different from
            // how the specification characterizes a lifted form. 
            //
            // An operation, in this case, X-->Y, is properly said to be "lifted" to X?-->Y? via
            // the rule that X?-->Y? matches the behavior of X-->Y for non-null X, and converts
            // null X to null Y otherwise.
            //
            // The native compiler, by contrast, takes the existing operator and "lifts" either
            // the operator's parameter type or the operator's return type to nullable. For
            // example, a conversion from X?-->Y would be "lifted" to X?-->Y? by making the
            // conversion from X? to Y, and then from Y to Y?.  No "lifting" semantics
            // are imposed; we do not check to see if the X? is null. This operator is not
            // actually "lifted" at all; rather, an implicit conversion is applied to the 
            // output. **The native compiler considers the result type Y? of that standard implicit
            // conversion to be the result type of the "lifted" conversion**, rather than
            // properly considering Y to be the result type of the conversion for the purposes 
            // of computing the best output type.
            //
            // Moreover: the native compiler actually *does* implement nullable lifting semantics
            // in the case where the input type of the user-defined conversion is a non-nullable
            // value type and the output type is a nullable value type **or pointer type, or 
            // reference type**. This is an enormous departure from the specification; the
            // native compiler will take a user-defined conversion from X-->Y? or X-->C and "lift"
            // it to a conversion from X?-->Y? or X?-->C that has nullable semantics.
            // 
            // This is quite confusing. In this code we will classify the conversion as either
            // "normal" or "lifted" on the basis of *whether or not special lifting semantics
            // are to be applied*. That is, whether or not a later rewriting pass is going to
            // need to insert a check to see if the source expression is null, and decide
            // whether or not to call the underlying unlifted conversion or produce a null
            // value without calling the unlifted conversion.
            // DELIBERATE SPEC VIOLATION: See the comment regarding bug 17021 in 
            // UserDefinedImplicitConversions.cs.

            if ((object)source != null && source.IsInterfaceType() || target.IsInterfaceType())
            {
                return;
            }

            if (IgnoreUserDefinedSpanConversions(source, target))
            {
                return;
            }

            ImmutableArray<MethodSymbol> operators = declaringType.GetOperators(
                isExplicit ? (isChecked ? WellKnownMemberNames.CheckedExplicitConversionName : WellKnownMemberNames.ExplicitConversionName) : WellKnownMemberNames.ImplicitConversionName);

            var candidates = ArrayBuilder<MethodSymbol>.GetInstance(operators.Length);
            candidates.AddRange(operators);

            if (isExplicit && isChecked)
            {
                ImmutableArray<MethodSymbol> operators2 = declaringType.GetOperators(WellKnownMemberNames.ExplicitConversionName);

                // Add regular operators as well.
                if (operators.IsEmpty)
                {
                    candidates.AddRange(operators2);
                }
                else
                {
                    foreach (MethodSymbol op2 in operators2)
                    {
                        // Drop operators that have a match among the checked ones.
                        bool add = true;

                        foreach (MethodSymbol op in operators)
                        {
                            if (SourceMemberContainerTypeSymbol.DoOperatorsPair(op, op2))
                            {
                                add = false;
                                break;
                            }
                        }

                        if (add)
                        {
                            candidates.Add(op2);
                        }
                    }
                }
            }

            foreach (MethodSymbol op in candidates)
            {
                // We might have a bad operator and be in an error recovery situation. Ignore it.
                if (op.ReturnsVoid || op.ParameterCount != 1 || op.ReturnType.TypeKind == TypeKind.Error)
                {
                    continue;
                }

                TypeSymbol convertsFrom = op.GetParameterType(0);
                TypeSymbol convertsTo = op.ReturnType;
                Conversion fromConversion = EncompassingExplicitConversion(sourceExpression, source, convertsFrom, ref useSiteInfo);
                Conversion toConversion = EncompassingExplicitConversion(convertsTo, target, ref useSiteInfo);

                // We accept candidates for which the parameter type encompasses the *underlying* source type.
                if (!fromConversion.Exists &&
                    (object)source != null &&
                    source.IsNullableType() &&
                    EncompassingExplicitConversion(source.GetNullableUnderlyingType(), convertsFrom, ref useSiteInfo).Exists)
                {
                    fromConversion = ClassifyBuiltInConversion(source, convertsFrom, isChecked: isChecked, ref useSiteInfo);
                }

                // As in dev11 (and the revised spec), we also accept candidates for which the return type is encompassed by the *stripped* target type.
                if (!toConversion.Exists &&
                    (object)target != null &&
                    target.IsNullableType() &&
                    EncompassingExplicitConversion(convertsTo, target.GetNullableUnderlyingType(), ref useSiteInfo).Exists)
                {
                    toConversion = ClassifyBuiltInConversion(convertsTo, target, isChecked: isChecked, ref useSiteInfo);
                }

                // In the corresponding implicit conversion code we can get away with first 
                // checking to see if standard implicit conversions exist from the source type
                // to the parameter type, and from the return type to the target type. If not,
                // then we can check for a lifted operator.
                //
                // That's not going to cut it in the explicit conversion code. Suppose we have
                // a conversion X-->Y and have source type X? and target type Y?. There *are*
                // standard explicit conversions from X?-->X and Y?-->Y, but we do not want
                // to bind this as an *unlifted* conversion from X? to Y?; we want such a thing
                // to be a *lifted* conversion from X? to Y?, that checks for null on the source
                // and decides to not call the underlying user-defined conversion if it is null.
                //
                // We therefore cannot do what we do in the implicit conversions, where we check
                // to see if the unlifted conversion works, and if it does, then don't add the lifted
                // conversion at all. Rather, we have to see if what we're building here is a 
                // lifted conversion or not.
                //
                // Under what circumstances is this conversion a lifted conversion? (In the 
                // "spec" sense of a lifted conversion; that is, that we check for null
                // and skip the user-defined conversion if necessary).
                //
                // * The source type must be a nullable value type.
                // * The parameter type must be a non-nullable value type.
                // * The target type must be able to take on a null value.

                if (fromConversion.Exists && toConversion.Exists)
                {
                    if ((object)source != null && source.IsNullableType() && convertsFrom.IsValidNullableTypeArgument() && target.CanBeAssignedNull())
                    {
                        TypeSymbol nullableFrom = MakeNullableType(convertsFrom);
                        TypeSymbol nullableTo = convertsTo.IsValidNullableTypeArgument() ? MakeNullableType(convertsTo) : convertsTo;
                        Conversion liftedFromConversion = EncompassingExplicitConversion(sourceExpression, source, nullableFrom, ref useSiteInfo);
                        Conversion liftedToConversion = EncompassingExplicitConversion(nullableTo, target, ref useSiteInfo);
                        Debug.Assert(liftedFromConversion.Exists);
                        Debug.Assert(liftedToConversion.Exists);
                        u.Add(UserDefinedConversionAnalysis.Lifted(constrainedToTypeOpt, op, liftedFromConversion, liftedToConversion, nullableFrom, nullableTo));
                    }
                    else
                    {
                        // There is an additional spec violation in the native compiler. Suppose
                        // we have a conversion from X-->Y and are asked to do "Y? y = new X();"  Clearly
                        // the intention is to convert from X-->Y via the implicit conversion, and then
                        // stick a standard implicit conversion from Y-->Y? on the back end. **In this 
                        // situation, the native compiler treats the conversion as though it were
                        // actually X-->Y? in source for the purposes of determining the best target
                        // type of a set of operators.
                        //
                        // Similarly, if we have a conversion from X-->Y and are asked to do 
                        // an explicit conversion from X? to Y then we treat the conversion as
                        // though it really were X?-->Y for the purposes of determining the best
                        // source type of a set of operators.
                        //
                        // We perpetuate these fictions here, except when X or Y is not a valid
                        // type argument to `Nullable<T>`. 

                        if (target.IsNullableType() && convertsTo.IsValidNullableTypeArgument())
                        {
                            convertsTo = MakeNullableType(convertsTo);
                            toConversion = EncompassingExplicitConversion(convertsTo, target, ref useSiteInfo);
                        }

                        if ((object)source != null && source.IsNullableType() && convertsFrom.IsValidNullableTypeArgument())
                        {
                            convertsFrom = MakeNullableType(convertsFrom);
                            fromConversion = EncompassingExplicitConversion(convertsFrom, source, ref useSiteInfo);
                        }

                        u.Add(UserDefinedConversionAnalysis.Normal(constrainedToTypeOpt, op, fromConversion, toConversion, convertsFrom, convertsTo));
                    }
                }
            }

            candidates.Free();
        }

        private TypeSymbol MostSpecificSourceTypeForExplicitUserDefinedConversion(
            ImmutableArray<UserDefinedConversionAnalysis> u,
            BoundExpression sourceExpression,
            TypeSymbol source,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // SPEC: If any of the operators in U convert from S then SX is S.

            // SPEC: Otherwise, if any of the operators in U convert from types
            // SPEC: that encompass E, then SX is the most encompassed type
            // SPEC: in the combined set of the source types of these operators.
            // SPEC: If no most encompassed type can be found then the
            // SPEC: conversion is ambiguous and a compile-time error occurs.

            // SPEC: Otherwise, SX is the most encompassing type in the combined
            // SPEC: set of the source types of these operators.  If exactly one
            // SPEC: most encompassing type cannot be found then the conversion
            // SPEC: is ambiguous and a compile-time error occurs.

            // DELIBERATE SPEC VIOLATION:
            // The native compiler deviates from the specification in the way it 
            // determines what the "converts from" type is. The specification is pretty
            // clear that the "converts from" type is the actual parameter type of the 
            // conversion operator, or, in the case of a lifted operator, the lifted-to-
            // nullable type. That is, if we have X-->Y then the converts-to type of
            // the operator in its normal form is Y, and the converts-to type of the 
            // operator in its lifted form is Y?. 
            //
            // The native compiler does not do this. Suppose we have a user-defined
            // conversion X-->Y, and the cast (Y)(some_nullable_x). The native
            // compiler will consider the converts-from type of X-->Y to be X?, surprisingly
            // enough. 
            //
            // We have already written the "FromType" into the conversion analysis to
            // perpetuate this fiction.

            if ((object)source != null)
            {
                if (u.Any(static (conv, source) => TypeSymbol.Equals(conv.FromType, source, TypeCompareKind.ConsiderEverything2), source))
                {
                    return source;
                }

                CompoundUseSiteInfo<AssemblySymbol> inLambdaUseSiteInfo = useSiteInfo;
                System.Func<UserDefinedConversionAnalysis, bool> isValid = conv => IsEncompassedBy(sourceExpression, source, conv.FromType, ref inLambdaUseSiteInfo);
                if (u.Any(isValid))
                {
                    var result = MostEncompassedType(u, isValid, conv => conv.FromType, ref inLambdaUseSiteInfo);
                    useSiteInfo = inLambdaUseSiteInfo;
                    return result;
                }

                useSiteInfo = inLambdaUseSiteInfo;
            }

            return MostEncompassingType(u, conv => conv.FromType, ref useSiteInfo);
        }

        private TypeSymbol MostSpecificTargetTypeForExplicitUserDefinedConversion(
            ImmutableArray<UserDefinedConversionAnalysis> u,
            TypeSymbol target,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // SPEC: If any of the operators in U convert to T then TX is T.

            // SPEC: Otherwise, if any of the operators in U convert to types that are
            // SPEC: encompassed by T then TX is the most encompassing type in the combined
            // SPEC: set of target types of those operators. If exactly one most encompassing
            // SPEC: type cannot be found then the conversion is ambiguous and a compile-time
            // SPEC: error occurs.

            // SPEC: Otherwise, Tx is the most encompassed type in the combined set of target 
            // SPEC: types of the operators in U. If no most encompassed type can be found,
            // SPEC: then the conversion is ambiguous and a compile-time error occurs.

            // DELIBERATE SPEC VIOLATION:
            // The native compiler deviates from the specification in the way it 
            // determines what the "converts to" type is. The specification is pretty
            // clear that the "converts to" type is the actual return type of the 
            // conversion operator, or, in the case of a lifted operator, the lifted-to-
            // nullable type. That is, if we have X-->Y then the converts-to type of
            // the operator in its normal form is Y, and the converts-to type of the 
            // operator in its lifted form is Y?. 
            //
            // The native compiler does not do this. Suppose we have a user-defined
            // conversion X-->Y, and the assignment Y? y = new X(); -- the native 
            // compiler will consider the converts-to type of X-->Y to be Y?, surprisingly
            // enough. 
            //
            // We have already written the "ToType" into the conversion analysis to
            // perpetuate this fiction.

            if (u.Any(static (conv, target) => TypeSymbol.Equals(conv.ToType, target, TypeCompareKind.ConsiderEverything2), target))
            {
                return target;
            }

            CompoundUseSiteInfo<AssemblySymbol> inLambdaUseSiteInfo = useSiteInfo;
            System.Func<UserDefinedConversionAnalysis, bool> isValid = conv => IsEncompassedBy(conv.ToType, target, ref inLambdaUseSiteInfo);
            if (u.Any(isValid))
            {
                var result = MostEncompassingType(u, isValid, conv => conv.ToType, ref inLambdaUseSiteInfo);
                useSiteInfo = inLambdaUseSiteInfo;
                return result;
            }

            useSiteInfo = inLambdaUseSiteInfo;
            return MostEncompassedType(u, conv => conv.ToType, ref useSiteInfo);
        }

        private Conversion EncompassingExplicitConversion(BoundExpression expr, TypeSymbol a, TypeSymbol b, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(expr is null || Compilation is not null);
            Debug.Assert(expr != null || (object)a != null);
            Debug.Assert((object)b != null);

            // SPEC: If a standard implicit conversion exists from a type A to a type B
            // SPEC: and if neither A nor B is an interface type then A is said to be
            // SPEC: encompassed by B, and B is said to encompass A.

            // DELIBERATE SPEC VIOLATION: We should be checking to see if A and B are
            // interface types here. See the comment regarding bug 17021 in 
            // UserDefinedImplicitConversions.cs

            // DELIBERATE SPEC VIOLATION: 
            // We do not support an encompassing implicit conversion from a zero constant
            // to an enum type, because the native compiler did not.  It would be a breaking
            // change.

            var result = ClassifyStandardConversion(expr, a, b, ref useSiteInfo);
            return result.IsEnumeration ? Conversion.NoConversion : result;
        }

        private Conversion EncompassingExplicitConversion(TypeSymbol a, TypeSymbol b, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return EncompassingExplicitConversion(expr: null, a, b, ref useSiteInfo);
        }
    }
}
