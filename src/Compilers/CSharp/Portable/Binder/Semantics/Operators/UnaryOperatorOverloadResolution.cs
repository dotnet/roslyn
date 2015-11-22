// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class OverloadResolution
    {
        private NamedTypeSymbol MakeNullable(TypeSymbol type)
        {
            return Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(type);
        }

        public void UnaryOperatorOverloadResolution(UnaryOperatorKind kind, BoundExpression operand, UnaryOperatorOverloadResolutionResult result, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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

            bool hadUserDefinedCandidate = GetUserDefinedOperators(kind, operand, result.Results, ref useSiteDiagnostics);

            // SPEC: If the set of candidate user-defined operators is not empty, then this becomes the 
            // SPEC: set of candidate operators for the operation. Otherwise, the predefined unary operator 
            // SPEC: implementations, including their lifted forms, become the set of candidate operators 
            // SPEC: for the operation. 

            if (!hadUserDefinedCandidate)
            {
                result.Results.Clear();
                GetAllBuiltInOperators(kind, operand, result.Results, ref useSiteDiagnostics);
            }

            // SPEC: The overload resolution rules of 7.5.3 are applied to the set of candidate operators 
            // SPEC: to select the best operator with respect to the argument list (x), and this operator 
            // SPEC: becomes the result of the overload resolution process. If overload resolution fails 
            // SPEC: to select a single best operator, a binding-time error occurs.

            UnaryOperatorOverloadResolution(operand, result, ref useSiteDiagnostics);
        }



        // Takes a list of candidates and mutates the list to throw out the ones that are worse than
        // another applicable candidate.
        private void UnaryOperatorOverloadResolution(
            BoundExpression operand,
            UnaryOperatorOverloadResolutionResult result,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // SPEC: Given the set of applicable candidate function members, the best function
            // member in that set is located. SPEC: If the set contains only one function member,
            // then that function member is the best function member. 

            if (result.GetValidCount() == 1)
            {
                return;
            }

            // SPEC: Otherwise, the best function member is the one function member that is better than all other function 
            // SPEC: members with respect to the given argument list, provided that each function member is compared to all 
            // SPEC: other function members using the rules in 7.5.3.2. If there is not exactly one function member that is 
            // SPEC: better than all other function members, then the function member invocation is ambiguous and a binding-time 
            // SPEC: error occurs.

            // UNDONE: This is a naive quadratic algorithm; there is a linear algorithm that works. Consider using it.

            var candidates = result.Results;
            for (int i = 0; i < candidates.Count; ++i)
            {
                if (candidates[i].Kind != OperatorAnalysisResultKind.Applicable)
                {
                    continue;
                }

                // Is this applicable operator better than every other applicable method?
                for (int j = 0; j < candidates.Count; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    if (candidates[j].Kind == OperatorAnalysisResultKind.Inapplicable)
                    {
                        continue;
                    }
                    var better = BetterOperator(candidates[i].Signature, candidates[j].Signature, operand, ref useSiteDiagnostics);
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

        private BetterResult BetterOperator(UnaryOperatorSignature op1, UnaryOperatorSignature op2, BoundExpression operand, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // First we see if the conversion from the operand to one operand type is better than 
            // the conversion to the other.

            BetterResult better = BetterConversionFromExpression(operand, op1.OperandType, op2.OperandType, ref useSiteDiagnostics);

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

            return BetterResult.Neither;
        }

        private void GetAllBuiltInOperators(UnaryOperatorKind kind, BoundExpression operand, ArrayBuilder<UnaryOperatorAnalysisResult> results, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
            this.Compilation.builtInOperators.GetSimpleBuiltInOperators(kind, operators);

            GetEnumOperations(kind, operand, operators);

            var pointerOperator = GetPointerOperation(kind, operand);
            if (pointerOperator != null)
            {
                operators.Add(pointerOperator.Value);
            }

            CandidateOperators(operators, operand, results, ref useSiteDiagnostics);
            operators.Free();
        }

        // Returns true if there were any applicable candidates.
        private bool CandidateOperators(ArrayBuilder<UnaryOperatorSignature> operators, BoundExpression operand, ArrayBuilder<UnaryOperatorAnalysisResult> results, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            bool anyApplicable = false;
            foreach (var op in operators)
            {
                var conversion = Conversions.ClassifyConversionFromExpression(operand, op.OperandType, ref useSiteDiagnostics);
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

            var nullableEnum = MakeNullable(enumType);

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
        private bool GetUserDefinedOperators(UnaryOperatorKind kind, BoundExpression operand, ArrayBuilder<UnaryOperatorAnalysisResult> results, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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

            TypeSymbol type0 = operand.Type.StrippedType();

            // Searching for user-defined operators is expensive; let's take an early out if we can.
            if (OperatorFacts.DefinitelyHasNoUserDefinedOperators(type0))
            {
                return false;
            }

            string name = OperatorFacts.UnaryOperatorNameFromOperatorKind(kind);
            var operators = ArrayBuilder<UnaryOperatorSignature>.GetInstance();
            bool hadApplicableCandidates = false;

            NamedTypeSymbol current = type0 as NamedTypeSymbol;
            if ((object)current == null)
            {
                current = type0.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            if ((object)current == null && type0.IsTypeParameter())
            {
                current = ((TypeParameterSymbol)type0).EffectiveBaseClass(ref useSiteDiagnostics);
            }

            for (; (object)current != null; current = current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                operators.Clear();
                GetUserDefinedUnaryOperatorsFromType(current, kind, name, operators);
                results.Clear();
                if (CandidateOperators(operators, operand, results, ref useSiteDiagnostics))
                {
                    hadApplicableCandidates = true;
                    break;
                }
            }

            operators.Free();

            return hadApplicableCandidates;
        }

        private void GetUserDefinedUnaryOperatorsFromType(
            NamedTypeSymbol type,
            UnaryOperatorKind kind,
            string name,
            ArrayBuilder<UnaryOperatorSignature> operators)
        {
            foreach (MethodSymbol op in type.GetOperators(name))
            {
                // If we're in error recovery, we might have bad operators. Just ignore it.
                if (op.ParameterCount != 1 || op.ReturnsVoid)
                {
                    continue;
                }

                TypeSymbol operandType = op.ParameterTypes[0];
                TypeSymbol resultType = op.ReturnType.TypeSymbol;

                operators.Add(new UnaryOperatorSignature(UnaryOperatorKind.UserDefined | kind, operandType, resultType, op));

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
                        if (operandType.IsValueType && !operandType.IsNullableType() &&
                            resultType.IsValueType && !resultType.IsNullableType())
                        {
                            operators.Add(new UnaryOperatorSignature(
                                UnaryOperatorKind.Lifted | UnaryOperatorKind.UserDefined | kind,
                                MakeNullable(operandType), MakeNullable(resultType), op));
                        }
                        break;
                }
            }
        }
    }
}
