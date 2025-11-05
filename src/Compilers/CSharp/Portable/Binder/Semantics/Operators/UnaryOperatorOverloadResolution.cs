// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class OverloadResolution
    {
        private NamedTypeSymbol MakeNullable(TypeSymbol type)
        {
            return Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(type);
        }

        public void UnaryOperatorOverloadResolution(
            UnaryOperatorKind kind,
            bool isChecked,
            string name1,
            string name2Opt,
            BoundExpression operand,
            UnaryOperatorOverloadResolutionResult result,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(operand != null);
            Debug.Assert(result.Results.Count == 0);

            // We can do a table lookup for well-known problems in overload resolution.
            UnaryOperatorEasyOut(kind, operand, result);
            if (result.Results.Count > 0)
            {
                return;
            }

            // SPEC: An operation of the form op x or x op, where op is an overloadable unary operator,
            // SPEC: and x is an expression of type X, is processed as follows:

            // SPEC: The set of candidate user-defined operators provided by X for the operation operator 
            // SPEC: op(x) is determined using the rules of 7.3.5.

            bool hadUserDefinedCandidate = GetUserDefinedOperators(kind, isChecked, name1, name2Opt, operand, result.Results, ref useSiteInfo);

            // SPEC: If the set of candidate user-defined operators is not empty, then this becomes the 
            // SPEC: set of candidate operators for the operation. Otherwise, the predefined unary operator 
            // SPEC: implementations, including their lifted forms, become the set of candidate operators 
            // SPEC: for the operation. 

            if (!hadUserDefinedCandidate)
            {
                result.Results.Clear();
                GetAllBuiltInOperators(kind, isChecked, operand, result.Results, ref useSiteInfo);
            }

            // SPEC: The overload resolution rules of 7.5.3 are applied to the set of candidate operators 
            // SPEC: to select the best operator with respect to the argument list (x), and this operator 
            // SPEC: becomes the result of the overload resolution process. If overload resolution fails 
            // SPEC: to select a single best operator, a binding-time error occurs.

            UnaryOperatorOverloadResolution(operand, result, ref useSiteInfo);
        }

#nullable enable 

        public bool UnaryOperatorExtensionOverloadResolutionInSingleScope(
            ArrayBuilder<Symbol> extensionCandidatesInSingleScope,
            UnaryOperatorKind kind,
            bool isChecked,
            string name1,
            string? name2Opt,
            BoundExpression operand,
            UnaryOperatorOverloadResolutionResult result,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(isChecked || name2Opt is null);
            Debug.Assert(operand.Type is not null);

            var operators = ArrayBuilder<UnaryOperatorSignature>.GetInstance();

            getDeclaredUserDefinedUnaryOperatorsInScope(extensionCandidatesInSingleScope, kind, name1, name2Opt, operators);

            if (operand.Type.IsNullableType()) // Wouldn't be applicable to the receiver type otherwise
            {
                AddLiftedUserDefinedUnaryOperators(constrainedToTypeOpt: null, kind, operators);
            }

            inferTypeArgumentsAndRemoveInapplicableToReceiverType(kind, operand, operators, ref useSiteInfo);

            bool hadApplicableCandidates = false;

            if (!operators.IsEmpty)
            {
                var results = result.Results;
                results.Clear();
                if (CandidateOperators(isChecked, operators, operand, results, ref useSiteInfo))
                {
                    UnaryOperatorOverloadResolution(operand, result, ref useSiteInfo);
                    hadApplicableCandidates = true;
                }
            }

            operators.Free();

            return hadApplicableCandidates;

            static void getDeclaredUserDefinedUnaryOperatorsInScope(ArrayBuilder<Symbol> extensionCandidatesInSingleScope, UnaryOperatorKind kind, string name1, string? name2Opt, ArrayBuilder<UnaryOperatorSignature> operators)
            {
                getDeclaredUserDefinedUnaryOperators(extensionCandidatesInSingleScope, kind, name1, operators);

                if (name2Opt is not null)
                {
                    if (!operators.IsEmpty)
                    {
                        var existing = new HashSet<MethodSymbol>(PairedExtensionOperatorSignatureComparer.Instance);
                        existing.AddRange(operators.Select(static (op) => op.Method));

                        var operators2 = ArrayBuilder<UnaryOperatorSignature>.GetInstance();
                        getDeclaredUserDefinedUnaryOperators(extensionCandidatesInSingleScope, kind, name2Opt, operators2);

                        foreach (var op in operators2)
                        {
                            if (!existing.Contains(op.Method))
                            {
                                operators.Add(op);
                            }
                        }

                        operators2.Free();
                    }
                    else
                    {
                        getDeclaredUserDefinedUnaryOperators(extensionCandidatesInSingleScope, kind, name2Opt, operators);
                    }
                }
            }

            static void getDeclaredUserDefinedUnaryOperators(ArrayBuilder<Symbol> extensionCandidatesInSingleScope, UnaryOperatorKind kind, string name, ArrayBuilder<UnaryOperatorSignature> operators)
            {
                Debug.Assert(extensionCandidatesInSingleScope.All(static m => m.ContainingType.ExtensionParameter is not null));
                var typeOperators = ArrayBuilder<MethodSymbol>.GetInstance();
                NamedTypeSymbol.AddOperators(typeOperators, extensionCandidatesInSingleScope);
                GetDeclaredUserDefinedUnaryOperators(constrainedToTypeOpt: null, typeOperators, kind, name, operators);
                typeOperators.Free();
            }

            void inferTypeArgumentsAndRemoveInapplicableToReceiverType(UnaryOperatorKind kind, BoundExpression operand, ArrayBuilder<UnaryOperatorSignature> operators, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                for (int i = operators.Count - 1; i >= 0; i--)
                {
                    var candidate = operators[i];
                    MethodSymbol method = candidate.Method;
                    NamedTypeSymbol extension = method.ContainingType;

                    if (extension.Arity == 0)
                    {
                        if (isApplicableToReceiver(in candidate, operand, ref useSiteInfo))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // Infer type arguments 
                        var inferenceResult = MethodTypeInferrer.Infer(
                            _binder,
                            this.Conversions,
                            extension.TypeParameters,
                            extension,
                            [TypeWithAnnotations.Create(candidate.OperandType)],
                            method.ParameterRefKinds,
                            [operand],
                            ref useSiteInfo,
                            ordinals: null);

                        if (inferenceResult.Success)
                        {
                            extension = extension.Construct(inferenceResult.InferredTypeArguments);
                            method = method.AsMember(extension);

                            if (!FailsConstraintChecks(method, out ArrayBuilder<TypeParameterDiagnosticInfo> constraintFailureDiagnosticsOpt, template: CompoundUseSiteInfo<AssemblySymbol>.Discarded))
                            {
                                TypeSymbol operandType = method.GetParameterType(0);
                                TypeSymbol resultType = method.ReturnType;

                                UnaryOperatorSignature inferredCandidate;

                                if (candidate.Kind.IsLifted())
                                {
                                    inferredCandidate = new UnaryOperatorSignature(UnaryOperatorKind.Lifted | UnaryOperatorKind.UserDefined | kind, MakeNullable(operandType), MakeNullable(resultType), method, constrainedToTypeOpt: null);
                                }
                                else
                                {
                                    inferredCandidate = new UnaryOperatorSignature(UnaryOperatorKind.UserDefined | kind, operandType, resultType, method, constrainedToTypeOpt: null);
                                }

                                if (isApplicableToReceiver(in inferredCandidate, operand, ref useSiteInfo))
                                {
                                    operators[i] = inferredCandidate;
                                    continue;
                                }
                            }

                            constraintFailureDiagnosticsOpt?.Free();
                        }
                    }

                    operators.RemoveAt(i);
                }
            }

            bool isApplicableToReceiver(in UnaryOperatorSignature candidate, BoundExpression operand, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                Debug.Assert(operand.Type is not null);
                Debug.Assert(candidate.Method.ContainingType.ExtensionParameter is not null);

                if (candidate.Kind.IsLifted())
                {
                    Debug.Assert(operand.Type.IsNullableType());

                    if (!candidate.Method.ContainingType.ExtensionParameter.Type.IsValidNullableTypeArgument() ||
                        !Conversions.ConvertExtensionMethodThisArg(MakeNullable(candidate.Method.ContainingType.ExtensionParameter.Type), operand.Type, ref useSiteInfo, isMethodGroupConversion: false).Exists)
                    {
                        return false;
                    }
                }
                else if (!Conversions.ConvertExtensionMethodThisArg(candidate.Method.ContainingType.ExtensionParameter.Type, operand.Type, ref useSiteInfo, isMethodGroupConversion: false).Exists)
                {
                    return false; // Conversion to 'this' parameter failed
                }

                return true;
            }
        }

        internal class PairedExtensionOperatorSignatureComparer : IEqualityComparer<MethodSymbol>
        {
            public static readonly PairedExtensionOperatorSignatureComparer Instance = new PairedExtensionOperatorSignatureComparer();

            private PairedExtensionOperatorSignatureComparer() { }

            public bool Equals(MethodSymbol? x, MethodSymbol? y)
            {
                Debug.Assert(x is { });
                Debug.Assert(y is { });

                if (x.OriginalDefinition.ContainingType.ContainingType != (object)x.OriginalDefinition.ContainingType.ContainingType)
                {
                    return false;
                }

                var xExtension = x.OriginalDefinition.ContainingType;
                var xGroupingKey = ((SourceNamedTypeSymbol)xExtension).ExtensionGroupingName;
                Debug.Assert(xGroupingKey is not null);
                var yExtension = y.OriginalDefinition.ContainingType;
                var yGroupingKey = ((SourceNamedTypeSymbol)yExtension).ExtensionGroupingName;

                if (!xGroupingKey.Equals(yGroupingKey))
                {
                    return false;
                }

                return SourceMemberContainerTypeSymbol.DoOperatorsPair(
                           x.OriginalDefinition.AsMember(Normalize(xExtension)),
                           y.OriginalDefinition.AsMember(Normalize(yExtension)));
            }

            private static NamedTypeSymbol Normalize(NamedTypeSymbol extension)
            {
                if (extension.Arity != 0)
                {
                    extension = extension.Construct(IndexedTypeParameterSymbol.Take(extension.Arity));
                }

                return extension;
            }

            public int GetHashCode(MethodSymbol op)
            {
                var typeComparer = Symbols.SymbolEqualityComparer.AllIgnoreOptions;

                int result = typeComparer.GetHashCode(op.OriginalDefinition.ContainingType.ContainingType);

                var extension = op.OriginalDefinition.ContainingType;
                var groupingKey = ((SourceNamedTypeSymbol)extension).ExtensionGroupingName;
                Debug.Assert(groupingKey is not null);
                result = Hash.Combine(result, groupingKey.GetHashCode());

                foreach (var parameter in op.OriginalDefinition.AsMember(Normalize(extension)).Parameters)
                {
                    result = Hash.Combine(result, typeComparer.GetHashCode(parameter.Type));
                }

                return result;
            }
        }

#nullable disable

        // Takes a list of candidates and mutates the list to throw out the ones that are worse than
        // another applicable candidate.
        internal void UnaryOperatorOverloadResolution(
            BoundExpression operand,
            UnaryOperatorOverloadResolutionResult result,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // SPEC: Given the set of applicable candidate function members, the best function member in that set is located. 
            // SPEC: If the set contains only one function member, then that function member is the best function member. 

            if (result.SingleValid())
            {
                return;
            }

            var candidates = result.Results;
            RemoveLowerPriorityMembers<UnaryOperatorAnalysisResult, MethodSymbol>(candidates);

            // SPEC: Otherwise, the best function member is the one function member that is better than all other function 
            // SPEC: members with respect to the given argument list, provided that each function member is compared to all 
            // SPEC: other function members using the rules in 7.5.3.2. If there is not exactly one function member that is 
            // SPEC: better than all other function members, then the function member invocation is ambiguous and a binding-time 
            // SPEC: error occurs.

            // Try to find a single best candidate
            int bestIndex = GetTheBestCandidateIndex(operand, candidates, ref useSiteInfo);
            if (bestIndex != -1)
            {
                // Mark all other candidates as worse
                for (int index = 0; index < candidates.Count; ++index)
                {
                    if (candidates[index].Kind != OperatorAnalysisResultKind.Inapplicable && index != bestIndex)
                    {
                        candidates[index] = candidates[index].Worse();
                    }
                }

                return;
            }

            for (int i = 1; i < candidates.Count; ++i)
            {
                if (candidates[i].Kind != OperatorAnalysisResultKind.Applicable)
                {
                    continue;
                }

                // Is this applicable operator better than every other applicable method?
                for (int j = 0; j < i; ++j)
                {
                    if (candidates[j].Kind == OperatorAnalysisResultKind.Inapplicable)
                    {
                        continue;
                    }

                    var better = BetterOperator(candidates[i].Signature, candidates[j].Signature, operand, ref useSiteInfo);
                    if (better == BetterResult.Left)
                    {
                        candidates[j] = candidates[j].Worse();
                    }
                    else if (better == BetterResult.Right)
                    {
                        candidates[i] = candidates[i].Worse();
                    }
                }
            }
        }

        private int GetTheBestCandidateIndex(
            BoundExpression operand,
            ArrayBuilder<UnaryOperatorAnalysisResult> candidates,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            int currentBestIndex = -1;
            for (int index = 0; index < candidates.Count; index++)
            {
                if (candidates[index].Kind != OperatorAnalysisResultKind.Applicable)
                {
                    continue;
                }

                // Assume that the current candidate is the best if we don't have any
                if (currentBestIndex == -1)
                {
                    currentBestIndex = index;
                }
                else
                {
                    var better = BetterOperator(candidates[currentBestIndex].Signature, candidates[index].Signature, operand, ref useSiteInfo);
                    if (better == BetterResult.Right)
                    {
                        // The current best is worse
                        currentBestIndex = index;
                    }
                    else if (better != BetterResult.Left)
                    {
                        // The current best is not better
                        currentBestIndex = -1;
                    }
                }
            }

            // Make sure that every candidate up to the current best is worse
            for (int index = 0; index < currentBestIndex; index++)
            {
                if (candidates[index].Kind == OperatorAnalysisResultKind.Inapplicable)
                {
                    continue;
                }

                var better = BetterOperator(candidates[currentBestIndex].Signature, candidates[index].Signature, operand, ref useSiteInfo);
                if (better != BetterResult.Left)
                {
                    // The current best is not better
                    return -1;
                }
            }

            return currentBestIndex;
        }

        private BetterResult BetterOperator(UnaryOperatorSignature op1, UnaryOperatorSignature op2, BoundExpression operand, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // First we see if the conversion from the operand to one operand type is better than 
            // the conversion to the other.

            BetterResult better = BetterConversionFromExpression(operand, op1.OperandType, op2.OperandType, ref useSiteInfo);

            if (better == BetterResult.Left || better == BetterResult.Right)
            {
                return better;
            }

            // There was no better member on the basis of conversions. Go to the tiebreaking round.

            // SPEC: In case the parameter type sequences P1, P2 and Q1, Q2 are equivalent -- that is, every Pi
            // SPEC: has an identity conversion to the corresponding Qi -- the following tie-breaking rules
            // SPEC: are applied:

            if (Conversions.HasIdentityConversion(op1.OperandType, op2.OperandType))
            {
                // SPEC: If Mp is a non-generic method and Mq is a generic method, then Mp is better than Mq.
                if (op1.Method?.GetMemberArityIncludingExtension() is null or 0)
                {
                    if (op2.Method?.GetMemberArityIncludingExtension() > 0)
                    {
                        return BetterResult.Left;
                    }
                }
                else if (op2.Method?.GetMemberArityIncludingExtension() is null or 0)
                {
                    return BetterResult.Right;
                }

                // SPEC: If Mp has more specific parameter types than Mq then Mp is better than Mq.

                // Under what circumstances can two unary operators with identical signatures be "more specific"
                // than another? With a binary operator you could have C<T>.op+(C<T>, T) and C<T>.op+(C<T>, int).
                // When doing overload resolution on C<int> + int, the latter is more specific. But with a unary
                // operator, the sole operand *must* be the containing type or its nullable type. Therefore
                // if there is an identity conversion, then the parameters really were identical. We therefore
                // skip checking for specificity.

                // SPEC: If one member is a non-lifted operator and the other is a lifted operator,
                // SPEC: the non-lifted one is better.

                bool lifted1 = op1.Kind.IsLifted();
                bool lifted2 = op2.Kind.IsLifted();

                if (lifted1 && !lifted2)
                {
                    return BetterResult.Right;
                }
                else if (!lifted1 && lifted2)
                {
                    return BetterResult.Left;
                }
            }

            // Always prefer operators with val parameters over operators with in parameters:
            if (op1.RefKind == RefKind.None && op2.RefKind == RefKind.In)
            {
                return BetterResult.Left;
            }
            else if (op2.RefKind == RefKind.None && op1.RefKind == RefKind.In)
            {
                return BetterResult.Right;
            }

            return BetterResult.Neither;
        }

        private void GetAllBuiltInOperators(UnaryOperatorKind kind, bool isChecked, BoundExpression operand, ArrayBuilder<UnaryOperatorAnalysisResult> results, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // The spec states that overload resolution is performed upon the infinite set of
            // operators defined on enumerated types, pointers and delegates. Clearly we cannot
            // construct the infinite set; we have to pare it down. Previous implementations of C#
            // implement a much stricter rule; they only add the special operators to the candidate
            // set if one of the operands is of the relevant type. This means that operands
            // involving user-defined implicit conversions from class or struct types to enum,
            // pointer and delegate types do not cause the right candidates to participate in
            // overload resolution. It also presents numerous problems involving delegate variance
            // and conversions from lambdas to delegate types.
            //
            // It is onerous to require the actually specified behavior. We should change the
            // specification to match the previous implementation.

            var operators = ArrayBuilder<UnaryOperatorSignature>.GetInstance();
            this.Compilation.BuiltInOperators.GetSimpleBuiltInOperators(kind, operators, skipNativeIntegerOperators: !operand.Type.IsNativeIntegerOrNullableThereof());

            GetEnumOperations(kind, operand, operators);

            var pointerOperator = GetPointerOperation(kind, operand);
            if (pointerOperator != null)
            {
                operators.Add(pointerOperator.Value);
            }

            CandidateOperators(isChecked, operators, operand, results, ref useSiteInfo);
            operators.Free();
        }

        // Returns true if there were any applicable candidates.
        private bool CandidateOperators(bool isChecked, ArrayBuilder<UnaryOperatorSignature> operators, BoundExpression operand, ArrayBuilder<UnaryOperatorAnalysisResult> results, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            bool anyApplicable = false;
            foreach (var op in operators)
            {
                var conversion = Conversions.ClassifyConversionFromExpression(operand, op.OperandType, isChecked: isChecked, ref useSiteInfo);
                if (conversion.IsImplicit)
                {
                    anyApplicable = true;
                    results.Add(UnaryOperatorAnalysisResult.Applicable(op, conversion));
                }
                else
                {
                    results.Add(UnaryOperatorAnalysisResult.Inapplicable(op, conversion));
                }
            }

            return anyApplicable;
        }

        private void GetEnumOperations(UnaryOperatorKind kind, BoundExpression operand, ArrayBuilder<UnaryOperatorSignature> operators)
        {
            Debug.Assert(operand != null);

            var enumType = operand.Type;
            if ((object)enumType == null)
            {
                return;
            }

            enumType = enumType.StrippedType();
            if (!enumType.IsValidEnumType())
            {
                return;
            }

            var nullableEnum = Compilation.GetOrCreateNullableType(enumType);

            switch (kind)
            {
                case UnaryOperatorKind.PostfixIncrement:
                case UnaryOperatorKind.PostfixDecrement:
                case UnaryOperatorKind.PrefixIncrement:
                case UnaryOperatorKind.PrefixDecrement:
                case UnaryOperatorKind.BitwiseComplement:
                    operators.Add(new UnaryOperatorSignature(kind | UnaryOperatorKind.Enum, enumType, enumType));
                    operators.Add(new UnaryOperatorSignature(kind | UnaryOperatorKind.Lifted | UnaryOperatorKind.Enum, nullableEnum, nullableEnum));
                    break;
            }
        }

        private static UnaryOperatorSignature? GetPointerOperation(UnaryOperatorKind kind, BoundExpression operand)
        {
            Debug.Assert(operand != null);

            var pointerType = operand.Type as PointerTypeSymbol;
            if ((object)pointerType == null)
            {
                return null;
            }

            UnaryOperatorSignature? op = null;
            switch (kind)
            {
                case UnaryOperatorKind.PostfixIncrement:
                case UnaryOperatorKind.PostfixDecrement:
                case UnaryOperatorKind.PrefixIncrement:
                case UnaryOperatorKind.PrefixDecrement:
                    op = new UnaryOperatorSignature(kind | UnaryOperatorKind.Pointer, pointerType, pointerType);
                    break;
            }
            return op;
        }

        // Returns true if there were any applicable candidates.
        private bool GetUserDefinedOperators(
            UnaryOperatorKind kind,
            bool isChecked,
            string name1,
            string name2Opt,
            BoundExpression operand,
            ArrayBuilder<UnaryOperatorAnalysisResult> results,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(operand != null);

            if ((object)operand.Type == null)
            {
                // If the operand has no type -- because it is a null reference or a lambda or a method group --
                // there is no way we can determine what type to search for user-defined operators.
                return false;
            }

            // Spec 7.3.5 Candidate user-defined operators
            // SPEC: Given a type T and an operation op(A) ... the set of candidate user-defined 
            // SPEC: operators provided by T for op(A) is determined as follows:

            // SPEC: If T is a nullable type then T0 is its underlying type; otherwise T0 is T.
            // SPEC: For all operator declarations in T0 and all lifted forms of such operators, if
            // SPEC: at least one operator is applicable with respect to A then the set of candidate
            // SPEC: operators consists of all such applicable operators. Otherwise, if T0 is object
            // SPEC: then the set of candidate operators is empty. Otherwise, the set of candidate
            // SPEC: operators is the set provided by the direct base class of T0, or the effective
            // SPEC: base class of T0 if T0 is a type parameter.

            // https://github.com/dotnet/roslyn/issues/34451: The spec quote should be adjusted to cover operators from interfaces as well.
            // From https://github.com/dotnet/csharplang/blob/main/meetings/2017/LDM-2017-06-27.md:
            // - We only even look for operator implementations in interfaces if one of the operands has a type that is an interface or
            // a type parameter with a non-empty effective base interface list.
            // - The applicable operators from classes / structs shadow those in interfaces.This matters for constrained type parameters:
            // the effective base class can shadow operators from effective base interfaces.
            // - If we find an applicable candidate in an interface, that candidate shadows all applicable operators in base interfaces:
            // we stop looking.

            return GetUserDefinedOperators(operand.Type.StrippedType(), kind, isChecked, name1, name2Opt, operand, results, ref useSiteInfo);
        }

        /// <summary>
        /// Returns true if there were any applicable candidates.
        /// </summary>
        internal bool GetUserDefinedOperators(
            TypeSymbol declaringTypeOrTypeParameter,
            UnaryOperatorKind kind,
            bool isChecked,
            string name1,
            string name2Opt,
            BoundExpression operand,
            ArrayBuilder<UnaryOperatorAnalysisResult> results,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            TypeSymbol constrainedToTypeOpt = declaringTypeOrTypeParameter as TypeParameterSymbol;

            // Searching for user-defined operators is expensive; let's take an early out if we can.
            if (OperatorFacts.DefinitelyHasNoUserDefinedOperators(declaringTypeOrTypeParameter))
            {
                return false;
            }

            var operators = ArrayBuilder<UnaryOperatorSignature>.GetInstance();
            bool hadApplicableCandidates = false;

            NamedTypeSymbol current = declaringTypeOrTypeParameter as NamedTypeSymbol;
            if ((object)current == null)
            {
                current = declaringTypeOrTypeParameter.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
            }

            if ((object)current == null && declaringTypeOrTypeParameter.IsTypeParameter())
            {
                current = ((TypeParameterSymbol)declaringTypeOrTypeParameter).EffectiveBaseClass(ref useSiteInfo);
            }

            for (; (object)current != null; current = current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo))
            {
                operators.Clear();

                GetUserDefinedUnaryOperatorsFromType(constrainedToTypeOpt, current, kind, name1, name2Opt, operators);

                results.Clear();
                if (CandidateOperators(isChecked, operators, operand, results, ref useSiteInfo))
                {
                    hadApplicableCandidates = true;
                    break;
                }
            }

            // Look in base interfaces, or effective interfaces for type parameters  
            if (!hadApplicableCandidates)
            {
                ImmutableArray<NamedTypeSymbol> interfaces = default;
                if (declaringTypeOrTypeParameter.IsInterfaceType())
                {
                    interfaces = declaringTypeOrTypeParameter.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
                }
                else if (declaringTypeOrTypeParameter.IsTypeParameter())
                {
                    interfaces = ((TypeParameterSymbol)declaringTypeOrTypeParameter).AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
                }

                if (!interfaces.IsDefaultOrEmpty)
                {
                    var shadowedInterfaces = PooledHashSet<NamedTypeSymbol>.GetInstance();
                    var resultsFromInterface = ArrayBuilder<UnaryOperatorAnalysisResult>.GetInstance();
                    results.Clear();

                    foreach (NamedTypeSymbol @interface in interfaces)
                    {
                        if (!@interface.IsInterface)
                        {
                            // this code could be reachable in error situations
                            continue;
                        }

                        if (shadowedInterfaces.Contains(@interface))
                        {
                            // this interface is "shadowed" by a derived interface
                            continue;
                        }

                        operators.Clear();
                        resultsFromInterface.Clear();
                        GetUserDefinedUnaryOperatorsFromType(constrainedToTypeOpt, @interface, kind, name1, name2Opt, operators);
                        if (CandidateOperators(isChecked, operators, operand, resultsFromInterface, ref useSiteInfo))
                        {
                            hadApplicableCandidates = true;
                            results.AddRange(resultsFromInterface);

                            // this interface "shadows" all its base interfaces
                            shadowedInterfaces.AddAll(@interface.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo));
                        }
                    }

                    shadowedInterfaces.Free();
                    resultsFromInterface.Free();
                }
            }

            operators.Free();

            return hadApplicableCandidates;
        }

#nullable enable

        internal static void GetStaticUserDefinedUnaryOperatorMethodNames(UnaryOperatorKind kind, bool isChecked, out string name1, out string? name2Opt)
        {
            name1 = OperatorFacts.UnaryOperatorNameFromOperatorKind(kind, isChecked);

            if (isChecked && SyntaxFacts.IsCheckedOperator(name1))
            {
                name2Opt = OperatorFacts.UnaryOperatorNameFromOperatorKind(kind, isChecked: false);
            }
            else
            {
                name2Opt = null;
            }
        }

        private void GetUserDefinedUnaryOperatorsFromType(
            TypeSymbol constrainedToTypeOpt,
            NamedTypeSymbol type,
            UnaryOperatorKind kind,
            string name1,
            string? name2Opt,
            ArrayBuilder<UnaryOperatorSignature> operators)
        {
            Debug.Assert(operators.Count == 0);

            GetDeclaredUserDefinedUnaryOperators(constrainedToTypeOpt, type, kind, name1, operators);

            if (name2Opt is not null)
            {
                var operators2 = ArrayBuilder<UnaryOperatorSignature>.GetInstance();

                // Add regular operators as well.
                GetDeclaredUserDefinedUnaryOperators(constrainedToTypeOpt, type, kind, name2Opt, operators2);

                // Drop operators that have a match among the checked ones.
                if (operators.Count != 0)
                {
                    for (int i = operators2.Count - 1; i >= 0; i--)
                    {
                        foreach (UnaryOperatorSignature signature1 in operators)
                        {
                            if (SourceMemberContainerTypeSymbol.DoOperatorsPair(signature1.Method, operators2[i].Method))
                            {
                                operators2.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                operators.AddRange(operators2);
                operators2.Free();
            }

            AddLiftedUserDefinedUnaryOperators(constrainedToTypeOpt, kind, operators);
        }

        private static void GetDeclaredUserDefinedUnaryOperators(TypeSymbol? constrainedToTypeOpt, NamedTypeSymbol type, UnaryOperatorKind kind, string name, ArrayBuilder<UnaryOperatorSignature> operators)
        {
            var typeOperators = ArrayBuilder<MethodSymbol>.GetInstance();
            type.AddOperators(name, typeOperators);
            GetDeclaredUserDefinedUnaryOperators(constrainedToTypeOpt, typeOperators, kind, name, operators);
            typeOperators.Free();
        }

        private static void GetDeclaredUserDefinedUnaryOperators(TypeSymbol? constrainedToTypeOpt, IEnumerable<MethodSymbol> typeOperators, UnaryOperatorKind kind, string name, ArrayBuilder<UnaryOperatorSignature> operators)
        {
            foreach (MethodSymbol op in typeOperators)
            {
                if (op.Name != name)
                {
                    continue;
                }

                // If we're in error recovery, we might have bad operators. Just ignore it.
                if (!op.IsStatic || op.ParameterCount != 1 || op.ReturnsVoid)
                {
                    continue;
                }

                TypeSymbol operandType = op.GetParameterType(0);
                TypeSymbol resultType = op.ReturnType;

                operators.Add(new UnaryOperatorSignature(UnaryOperatorKind.UserDefined | kind, operandType, resultType, op, constrainedToTypeOpt));
            }
        }

        private void AddLiftedUserDefinedUnaryOperators(TypeSymbol? constrainedToTypeOpt, UnaryOperatorKind kind, ArrayBuilder<UnaryOperatorSignature> operators)
        {
            // SPEC: For the unary operators + ++ - -- ! ~ a lifted form of an operator exists
            // SPEC: if the operand and its result types are both non-nullable value types.
            // SPEC: The lifted form is constructed by adding a single ? modifier to the
            // SPEC: operator and result types. 
            switch (kind)
            {
                case UnaryOperatorKind.UnaryPlus:
                case UnaryOperatorKind.PrefixDecrement:
                case UnaryOperatorKind.PrefixIncrement:
                case UnaryOperatorKind.UnaryMinus:
                case UnaryOperatorKind.PostfixDecrement:
                case UnaryOperatorKind.PostfixIncrement:
                case UnaryOperatorKind.LogicalNegation:
                case UnaryOperatorKind.BitwiseComplement:

                    for (int i = operators.Count - 1; i >= 0; i--)
                    {
                        MethodSymbol op = operators[i].Method;
                        TypeSymbol operandType = op.GetParameterType(0);
                        TypeSymbol resultType = op.ReturnType;

                        if (operandType.IsValidNullableTypeArgument() &&
                            resultType.IsValidNullableTypeArgument())
                        {
                            operators.Add(new UnaryOperatorSignature(
                                UnaryOperatorKind.Lifted | UnaryOperatorKind.UserDefined | kind,
                                MakeNullable(operandType), MakeNullable(resultType), op, constrainedToTypeOpt));
                        }
                    }
                    break;
            }
        }
    }
}
