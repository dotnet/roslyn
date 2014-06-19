using System.Diagnostics;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    internal struct UnaryOperatorSignature
    {
        public static UnaryOperatorSignature Error = default(UnaryOperatorSignature);

        public UnaryOperatorKind Kind { get; private set; }
        public TypeSymbol OperandType { get; private set; }
        public TypeSymbol ReturnType { get; private set; }
        public MethodSymbol Method { get; private set; }

        public UnaryOperatorSignature(UnaryOperatorKind kind, TypeSymbol operandType, TypeSymbol returnType, MethodSymbol method = null)
            : this()
        {
            this.Kind = kind;
            this.OperandType = operandType;
            this.ReturnType = returnType;
            this.Method = method;
        }

        public override string ToString()
        {
            return string.Format("kind: {0} operand: {1} return: {2}",
                this.Kind, this.OperandType, this.ReturnType);
        }
    }

    internal struct BinaryOperatorSignature
    {
        public static BinaryOperatorSignature Error = default(BinaryOperatorSignature);

        public BinaryOperatorKind Kind { get; private set; }
        public TypeSymbol LeftType { get; private set; }
        public TypeSymbol RightType { get; private set; }
        public TypeSymbol ReturnType { get; private set; }
        public MethodSymbol Method { get; private set; }

        public BinaryOperatorSignature(BinaryOperatorKind kind, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol returnType, MethodSymbol method = null)
            : this()
        {
            this.Kind = kind;
            this.LeftType = leftType;
            this.RightType = rightType;
            this.ReturnType = returnType;
            this.Method = method;
        }

        public override string ToString()
        {
            return string.Format("kind: {0} left: {1} right: {2} return: {3}",
                this.Kind, this.LeftType, this.RightType, this.ReturnType);
        }

        public static bool operator ==(BinaryOperatorSignature s1, BinaryOperatorSignature s2)
        {
            return s1.Kind == s2.Kind && s1.LeftType == s2.LeftType &&
                s1.RightType == s2.RightType && s1.ReturnType == s2.ReturnType &&
                s1.Method == s2.Method;
        }

        public static bool operator !=(BinaryOperatorSignature s1, BinaryOperatorSignature s2)
        {
            return !(s1 == s2);
        }

        public override bool Equals(object obj)
        {
            return this == obj as BinaryOperatorSignature?;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int code = (int)Kind;
                code += ReturnType == null ? 0 : ReturnType.GetHashCode();
                code += LeftType == null ? 0 : LeftType.GetHashCode();
                code += RightType == null ? 0 : RightType.GetHashCode();
                code += Method == null ? 0 : Method.GetHashCode();
                return code;
            }
        }
    }


    internal sealed partial class OverloadResolution
    {
        private TypeSymbol TypeOrUnderlyingType(TypeSymbol type)
        {
            if (type.IsNullableType())
            {
                return type.GetNullableUnderlyingType();
            }

            return type;
        }

        public void UnaryOperatorOverloadResolution(UnaryOperatorKind kind, BoundExpression operand, UnaryOperatorOverloadResolutionResult result)
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

            // NOTE: In fact, for a unary operator to apply the operand must always have a type. There are no
            // NOTE: unary operators that operate on lambdas, anonymous methods, method groups or null.

            // SPEC: The set of candidate user-defined operators provided by X for the operation operator 
            // SPEC: op(x) is determined using the rules of 7.3.5.

            GetUserDefinedOperators(kind, operand, result.Results);

            // SPEC: If the set of candidate user-defined operators is not empty, then this becomes the 
            // SPEC: set of candidate operators for the operation. Otherwise, the predefined unary operator 
            // SPEC: implementations, including their lifted forms, become the set of candidate operators 
            // SPEC: for the operation. 

            var operators = result.Results;
            if (!result.AnyValid())
            {
                GetAllBuiltInOperators(kind, operand, result.Results);
            }

            // SPEC: The overload resolution rules of 7.5.3 are applied to the set of candidate operators 
            // SPEC: to select the best operator with respect to the argument list (x), and this operator 
            // SPEC: becomes the result of the overload resolution process. If overload resolution fails 
            // SPEC: to select a single best operator, a binding-time error occurs.

            // reduce candidates
            UnaryOperatorOverloadResolution(operand, result);
        }

        private void UnaryOperatorEasyOut(UnaryOperatorKind kind, BoundExpression operand, UnaryOperatorOverloadResolutionResult result)
        {
            var operandType = operand.Type;
            if (operandType == null)
            {
                return;
            }

            var easyOut = UnopEasyOut.OpKind(kind, operandType);

            if (easyOut == UnaryOperatorKind.Error)
            {
                return;
            }

            UnaryOperatorSignature signature = this.Compilation.builtInOperators.GetSignature(easyOut);

            Conversion? conversion = Conversions.FastClassifyConversion(operandType, signature.OperandType);

            Debug.Assert(conversion.HasValue && conversion.Value.IsImplicit);

            result.Results.Add(UnaryOperatorAnalysisResult.Applicable(signature, conversion.Value));
        }

        // Takes a list of candidates and mutates the list to throw out the ones that are worse than
        // another applicable candidate.
        private void UnaryOperatorOverloadResolution(
            BoundExpression operand,
            UnaryOperatorOverloadResolutionResult result)
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
                    var better = BetterConversionFromExpression(operand, candidates[i].Signature.OperandType, candidates[j].Signature.OperandType);
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

        private void GetAllBuiltInOperators(UnaryOperatorKind kind, BoundExpression operand, ArrayBuilder<UnaryOperatorAnalysisResult> results)
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

            CandidateOperators(operators, operand, results);
            operators.Free();
        }

        private void CandidateOperators(ArrayBuilder<UnaryOperatorSignature> operators, BoundExpression operand, ArrayBuilder<UnaryOperatorAnalysisResult> results)
        {
            foreach (var op in operators)
            {
                var conversion = Conversions.ClassifyConversion(operand, op.OperandType);
                if (conversion.IsImplicit)
                {
                    results.Add(UnaryOperatorAnalysisResult.Applicable(op, conversion));
                }
                else
                {
                    results.Add(UnaryOperatorAnalysisResult.Inapplicable(op, conversion));
                }
            }
        }

        private void GetEnumOperations(UnaryOperatorKind kind, BoundExpression operand, ArrayBuilder<UnaryOperatorSignature> operators)
        {
            Debug.Assert(operand != null);

            var enumType = operand.Type;
            if (enumType == null)
            {
                return;
            }

            enumType = TypeOrUnderlyingType(enumType);
            if (!enumType.IsValidEnumType())
            {
                return;
            }

            var nullableEnum = Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(enumType);

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

        private UnaryOperatorSignature? GetPointerOperation(UnaryOperatorKind kind, BoundExpression operand)
        {
            Debug.Assert(operand != null);

            var pointerType = operand.Type as PointerTypeSymbol;
            if (pointerType == null)
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

        private static class UnopEasyOut
        {
            const UnaryOperatorKind ERR = UnaryOperatorKind.Error;

            const UnaryOperatorKind BOL = UnaryOperatorKind.Bool;
            const UnaryOperatorKind CHR = UnaryOperatorKind.Char;
            const UnaryOperatorKind I08 = UnaryOperatorKind.SByte;
            const UnaryOperatorKind U08 = UnaryOperatorKind.Byte;
            const UnaryOperatorKind I16 = UnaryOperatorKind.Short;
            const UnaryOperatorKind U16 = UnaryOperatorKind.UShort;
            const UnaryOperatorKind I32 = UnaryOperatorKind.Int;
            const UnaryOperatorKind U32 = UnaryOperatorKind.UInt;
            const UnaryOperatorKind I64 = UnaryOperatorKind.Long;
            const UnaryOperatorKind U64 = UnaryOperatorKind.ULong;
            const UnaryOperatorKind R32 = UnaryOperatorKind.Float;
            const UnaryOperatorKind R64 = UnaryOperatorKind.Double;
            const UnaryOperatorKind DEC = UnaryOperatorKind.Decimal;
            const UnaryOperatorKind LBOL = UnaryOperatorKind.Lifted | UnaryOperatorKind.Bool;
            const UnaryOperatorKind LCHR = UnaryOperatorKind.Lifted | UnaryOperatorKind.Char;
            const UnaryOperatorKind LI08 = UnaryOperatorKind.Lifted | UnaryOperatorKind.SByte;
            const UnaryOperatorKind LU08 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Byte;
            const UnaryOperatorKind LI16 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Short;
            const UnaryOperatorKind LU16 = UnaryOperatorKind.Lifted | UnaryOperatorKind.UShort;
            const UnaryOperatorKind LI32 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Int;
            const UnaryOperatorKind LU32 = UnaryOperatorKind.Lifted | UnaryOperatorKind.UInt;
            const UnaryOperatorKind LI64 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Long;
            const UnaryOperatorKind LU64 = UnaryOperatorKind.Lifted | UnaryOperatorKind.ULong;
            const UnaryOperatorKind LR32 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Float;
            const UnaryOperatorKind LR64 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Double;
            const UnaryOperatorKind LDEC = UnaryOperatorKind.Lifted | UnaryOperatorKind.Decimal;


            private static readonly UnaryOperatorKind[] increment =
            //obj  str  bool chr   i08   i16   i32   i64   u08   u16   u32   u64   r32   r64   dec  
                { ERR, ERR, ERR, CHR,  I08,  I16,  I32,  I64,  U08,  U16,  U32,  U64,  R32,  R64,  DEC,
                            /* lifted */
                            ERR, LCHR, LI08, LI16, LI32, LI64, LU08, LU16, LU32, LU64, LR32, LR64, LDEC };

            private static readonly UnaryOperatorKind[] plus =
            //obj  str  bool chr   i08   i16   i32   i64   u08   u16   u32   u64   r32   r64   dec  
                { ERR, ERR, ERR, I32,  I32,  I32,  I32,  I64,  I32,  I32,  U32,  U64,  R32,  R64,  DEC,
                            /* lifted */
                            ERR, LI32, LI32, LI32, LI32, LI64, LI32, LI32, LU32, LU64, LR32, LR64, LDEC };

            private static readonly UnaryOperatorKind[] minus =
            //obj  str  bool chr   i08   i16   i32   i64   u08   u16   u32   u64   r32   r64   dec  
                { ERR, ERR, ERR, I32,  I32,  I32,  I32,  I64,  I32,  I32,  I64,  ERR,  R32,  R64,  DEC,
                            /* lifted */
                            ERR, LI32, LI32, LI32, LI32, LI64, LI32, LI32, LI64, ERR,  LR32, LR64, LDEC };

            private static readonly UnaryOperatorKind[] logicalNegation =
            //obj  str  bool  chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                { ERR, ERR, BOL,  ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR,
                            /* lifted */
                            LBOL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR };

            private static readonly UnaryOperatorKind[] bitwiseComplement =
            //obj  str  bool chr   i08   i16   i32   i64   u08   u16   u32   u64   r32  r64  dec  
                { ERR, ERR, ERR, I32,  I32,  I32,  I32,  I64,  I32,  I32,  U32,  U64,  ERR, ERR, ERR,
                            /* lifted */
                            ERR, LI32, LI32, LI32, LI32, LI64, LI32, LI32, LU32, LU64, ERR, ERR, ERR  };

            private static readonly UnaryOperatorKind[][] opkind =
            {
                /* ++ */  increment,
                /* -- */  increment,
                /* ++ */  increment,
                /* -- */  increment,
                /* +  */  plus,
                /* -  */  minus,
                /* !  */  logicalNegation,
                /* ~  */  bitwiseComplement
            };

            // UNDONE: This code is repeated in a bunch of places.
            private static int? TypeToIndex(TypeSymbol type)
            {
                switch (type.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Object: return 0;
                    case SpecialType.System_String: return 1;
                    case SpecialType.System_Boolean: return 2;
                    case SpecialType.System_Char: return 3;
                    case SpecialType.System_SByte: return 4;
                    case SpecialType.System_Int16: return 5;
                    case SpecialType.System_Int32: return 6;
                    case SpecialType.System_Int64: return 7;
                    case SpecialType.System_Byte: return 8;
                    case SpecialType.System_UInt16: return 9;
                    case SpecialType.System_UInt32: return 10;
                    case SpecialType.System_UInt64: return 11;
                    case SpecialType.System_Single: return 12;
                    case SpecialType.System_Double: return 13;
                    case SpecialType.System_Decimal: return 14;

                    case SpecialType.None:
                        if (type != null && type.IsNullableType())
                        {
                            TypeSymbol underlyingType = type.GetNullableUnderlyingType();

                            switch (underlyingType.GetSpecialTypeSafe())
                            {
                                case SpecialType.System_Boolean: return 15;
                                case SpecialType.System_Char: return 16;
                                case SpecialType.System_SByte: return 17;
                                case SpecialType.System_Int16: return 18;
                                case SpecialType.System_Int32: return 19;
                                case SpecialType.System_Int64: return 20;
                                case SpecialType.System_Byte: return 21;
                                case SpecialType.System_UInt16: return 22;
                                case SpecialType.System_UInt32: return 23;
                                case SpecialType.System_UInt64: return 24;
                                case SpecialType.System_Single: return 25;
                                case SpecialType.System_Double: return 26;
                                case SpecialType.System_Decimal: return 27;
                            }
                        }

                        // fall through
                        goto default;

                    default: return null;
                }
            }

            public static UnaryOperatorKind OpKind(UnaryOperatorKind kind, TypeSymbol operand)
            {
                int? index = TypeToIndex(operand);
                if (index == null)
                {
                    return UnaryOperatorKind.Error;
                }
                var result = opkind[kind.OperatorIndex()][index.Value];
                return result == UnaryOperatorKind.Error ? result : result | kind;
            }
        }

        public void BinaryOperatorOverloadResolution(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, BinaryOperatorOverloadResolutionResult result)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(result.Results.Count == 0);

            // We can do a table lookup for well-known problems in overload resolution.

            BinaryOperatorEasyOut(kind, left, right, result);
            if (result.Results.Count > 0)
            {
                return;
            }

            // The following is a slight rewording of the specification to emphasize that not all
            // operands of a binary operation need to have a type.

            // SPEC: An operation of the form x op y, where op is an overloadable binary operator is processed as follows:
            // SPEC: The set of candidate user-defined operators provided by the types (if any) of x and y for the 
            // SPEC operation operator op(x, y) is determined. 

            GetUserDefinedOperators(kind, left, right, result.Results);

            // SPEC: If the set of candidate user-defined operators is not empty, then this becomes the set of candidate 
            // SPEC: operators for the operation. Otherwise, the predefined binary operator op implementations, including 
            // SPEC: their lifted forms, become the set of candidate operators for the operation. 

            if (!result.AnyValid())
            {
                GetAllBuiltInOperators(kind, left, right, result.Results);
            }

            // SPEC: The overload resolution rules of 7.5.3 are applied to the set of candidate operators to select the best 
            // SPEC: operator with respect to the argument list (x, y), and this operator becomes the result of the overload 
            // SPEC: resolution process. If overload resolution fails to select a single best operator, a binding-time 
            // SPEC: error occurs.

            BinaryOperatorOverloadResolution(left, right, result);
        }

        private static class BinopEasyOut
        {
            private const BinaryOperatorKind ERR = BinaryOperatorKind.Error;
            private const BinaryOperatorKind OBJ = BinaryOperatorKind.Object;
            private const BinaryOperatorKind STR = BinaryOperatorKind.String;
            private const BinaryOperatorKind OSC = BinaryOperatorKind.ObjectAndString;
            private const BinaryOperatorKind SOC = BinaryOperatorKind.StringAndObject;
            private const BinaryOperatorKind INT = BinaryOperatorKind.Int;
            private const BinaryOperatorKind UIN = BinaryOperatorKind.UInt;
            private const BinaryOperatorKind LNG = BinaryOperatorKind.Long;
            private const BinaryOperatorKind ULG = BinaryOperatorKind.ULong;
            private const BinaryOperatorKind FLT = BinaryOperatorKind.Float;
            private const BinaryOperatorKind DBL = BinaryOperatorKind.Double;
            private const BinaryOperatorKind DEC = BinaryOperatorKind.Decimal;
            private const BinaryOperatorKind BOL = BinaryOperatorKind.Bool;
            private const BinaryOperatorKind LIN = BinaryOperatorKind.Lifted | BinaryOperatorKind.Int;
            private const BinaryOperatorKind LUN = BinaryOperatorKind.Lifted | BinaryOperatorKind.UInt;
            private const BinaryOperatorKind LLG = BinaryOperatorKind.Lifted | BinaryOperatorKind.Long;
            private const BinaryOperatorKind LUL = BinaryOperatorKind.Lifted | BinaryOperatorKind.ULong;
            private const BinaryOperatorKind LFL = BinaryOperatorKind.Lifted | BinaryOperatorKind.Float;
            private const BinaryOperatorKind LDB = BinaryOperatorKind.Lifted | BinaryOperatorKind.Double;
            private const BinaryOperatorKind LDC = BinaryOperatorKind.Lifted | BinaryOperatorKind.Decimal;
            private const BinaryOperatorKind LBL = BinaryOperatorKind.Lifted | BinaryOperatorKind.Bool;

            // UNDONE: The lifted bits make these tables very redundant. We could make them smaller (and slower)
            // UNDONE: by having just the unlifted table and doing type manipulation. 

            // UNDONE: We could also make these tables smaller by special-casing string and object.

            // Overload resolution for Y * / - % < > <= >= X
            private static readonly BinaryOperatorKind[,] arithmetic =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                      /*  obj */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  str */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* bool */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  chr */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /*  i08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /*  i16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /*  i32 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /*  i64 */
                      { ERR, ERR, ERR, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, FLT, DBL, DEC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                      /*  u08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /*  u16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /*  u32 */
                      { ERR, ERR, ERR, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, FLT, DBL, DEC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                      /*  u64 */
                      { ERR, ERR, ERR, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, FLT, DBL, DEC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                      /*  r32 */
                      { ERR, ERR, ERR, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, DBL, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                      /*  r64 */
                      { ERR, ERR, ERR, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                      /*  dec */
                      { ERR, ERR, ERR, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, ERR, ERR, DEC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
                      /*nbool */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* nchr */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /* ni08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /* ni16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /* ni32 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /* ni64 */
                      { ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                      /* nu08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /* nu16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /* nu32 */
                      { ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                      /* nu64 */
                      { ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                      /* nr32 */
                      { ERR, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                      /* nr64 */
                      { ERR, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                      /* ndec */
                      { ERR, ERR, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
            };

            // Overload resolution for Y + X
            private static readonly BinaryOperatorKind[,] addition =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                      /*  obj */
                      { ERR, OSC, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  str */
                      { SOC, STR, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC },
                      /* bool */
                      { ERR, OSC, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  chr */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /*  i08 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /*  i16 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /*  i32 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /*  i64 */
                      { ERR, OSC, ERR, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, FLT, DBL, DEC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                      /*  u08 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /*  u16 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /*  u32 */
                      { ERR, OSC, ERR, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, FLT, DBL, DEC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                      /*  u64 */
                      { ERR, OSC, ERR, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, FLT, DBL, DEC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                      /*  r32 */
                      { ERR, OSC, ERR, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, DBL, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                      /*  r64 */
                      { ERR, OSC, ERR, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                      /*  dec */
                      { ERR, OSC, ERR, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, ERR, ERR, DEC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
                      /*nbool */
                      { ERR, OSC, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* nchr */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /* ni08 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /* ni16 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /* ni32 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /* ni64 */
                      { ERR, OSC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                      /* nu08 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /* nu16 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /* nu32 */
                      { ERR, OSC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                      /* nu64 */
                      { ERR, OSC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                      /* nr32 */
                      { ERR, OSC, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                      /* nr64 */
                      { ERR, OSC, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                      /* ndec */
                      { ERR, OSC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
            };

            // Overload resolution for Y << >> X
            private static readonly BinaryOperatorKind[,] shift =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                      /*  obj */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  str */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* bool */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  chr */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /*  i08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /*  i16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /*  i32 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /*  i64 */
                      { ERR, ERR, ERR, LNG, LNG, LNG, LNG, ERR, LNG, LNG, ERR, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, ERR, LLG, LLG, ERR, ERR, ERR, ERR, ERR },
                      /*  u08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /*  u16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /*  u32 */
                      { ERR, ERR, ERR, UIN, UIN, UIN, UIN, ERR, UIN, UIN, ERR, ERR, ERR, ERR, ERR, ERR, LUN, LUN, LUN, LUN, ERR, LUN, LUN, ERR, ERR, ERR, ERR, ERR },
                      /*  u64 */
                      { ERR, ERR, ERR, ULG, ULG, ULG, ULG, ERR, ULG, ULG, ERR, ERR, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LUL, ERR, ERR, ERR, ERR, ERR },
                      /*  r32 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  r64 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  dec */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*nbool */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* nchr */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /* ni08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /* ni16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /* ni32 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /* ni64 */
                      { ERR, ERR, ERR, LLG, LLG, LLG, LLG, ERR, LLG, LLG, ERR, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, ERR, LLG, LLG, ERR, ERR, ERR, ERR, ERR },
                      /* nu08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /* nu16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                      /* nu32 */
                      { ERR, ERR, ERR, LUN, LUN, LUN, LUN, ERR, LUN, LUN, ERR, ERR, ERR, ERR, ERR, ERR, LUN, LUN, LUN, LUN, ERR, LUN, LUN, ERR, ERR, ERR, ERR, ERR },
                      /* nu64 */
                      { ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LUL, ERR, ERR, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LUL, ERR, ERR, ERR, ERR, ERR },
                      /* nr32 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* nr64 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* ndec */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
            };

            // Overload resolution for Y == != X
            // Note that these are the overload resolution rules; overload resolution might pick an invalid operator.
            // For example, overload resolution on object == decimal chooses the object/object overload, which then
            // is not legal because decimal must be a reference type. But we don't know to give that error *until*
            // overload resolution has chosen the reference equality operator.
            private static readonly BinaryOperatorKind[,] equality =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                      /*  obj */
                      { OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                      /*  str */
                      { OBJ, STR, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                      /* bool */
                      { OBJ, OBJ, BOL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, LBL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                      /*  chr */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /*  i08 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /*  i16 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /*  i32 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /*  i64 */
                      { OBJ, OBJ, OBJ, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, FLT, DBL, DEC, OBJ, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                      /*  u08 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /*  u16 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /*  u32 */
                      { OBJ, OBJ, OBJ, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, FLT, DBL, DEC, OBJ, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                      /*  u64 */
                      { OBJ, OBJ, OBJ, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, FLT, DBL, DEC, OBJ, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                      /*  r32 */
                      { OBJ, OBJ, OBJ, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, DBL, OBJ, OBJ, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, OBJ },
                      /*  r64 */
                      { OBJ, OBJ, OBJ, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, OBJ, OBJ, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, OBJ },
                      /*  dec */
                      { OBJ, OBJ, OBJ, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, OBJ, OBJ, DEC, OBJ, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, OBJ, OBJ, LDC },
                      /*nbool */
                      { OBJ, OBJ, LBL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, LBL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                      /* nchr */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /* ni08 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /* ni16 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /* ni32 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                      /* ni64 */
                      { OBJ, OBJ, OBJ, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC, OBJ, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                      /* nu08 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /* nu16 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                      /* nu32 */
                      { OBJ, OBJ, OBJ, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC, OBJ, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                      /* nu64 */
                      { OBJ, OBJ, OBJ, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC, OBJ, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                      /* nr32 */
                      { OBJ, OBJ, OBJ, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, OBJ, OBJ, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, OBJ },
                      /* nr64 */
                      { OBJ, OBJ, OBJ, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, OBJ, OBJ, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, OBJ },
                      /* ndec */
                      { OBJ, OBJ, OBJ, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, OBJ, OBJ, LDC, OBJ, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, OBJ, OBJ, LDC },
            };

            // Overload resolution for Y | & ^ || && X
            private static readonly BinaryOperatorKind[,] logical =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                      /*  obj */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  str */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* bool */
                      { ERR, ERR, BOL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LBL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  chr */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                      /*  i08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                      /*  i16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                      /*  i32 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                      /*  i64 */
                      { ERR, ERR, ERR, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, ERR, ERR, ERR },
                      /*  u08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                      /*  u16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                      /*  u32 */
                      { ERR, ERR, ERR, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, ERR, ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, ERR, ERR, ERR },
                      /*  u64 */
                      { ERR, ERR, ERR, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, ERR, ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, ERR, ERR },
                      /*  r32 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  r64 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*  dec */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /*nbool */
                      { ERR, ERR, LBL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LBL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* nchr */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                      /* ni08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                      /* ni16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                      /* ni32 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                      /* ni64 */
                      { ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, ERR, ERR, ERR },
                      /* nu08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                      /* nu16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                      /* nu32 */
                      { ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, ERR, ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, ERR, ERR, ERR },
                      /* nu64 */
                      { ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, ERR, ERR },
                      /* nr32 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* nr64 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                      /* ndec */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
            };

            private static readonly BinaryOperatorKind[][,] opkind =
            {
                /* *  */ arithmetic,
                /* +  */ addition,
                /* -  */ arithmetic,
                /* /  */ arithmetic,
                /* %  */ arithmetic,
                /* >> */ shift,
                /* << */ shift,
                /* == */ equality,
                /* != */ equality,
                /* >  */ arithmetic,
                /* <  */ arithmetic,
                /* >= */ arithmetic,
                /* <= */ arithmetic,
                /* &  */ logical,
                /* |  */ logical,
                /* ^  */ logical,
            };

            private static int? TypeToIndex(TypeSymbol type)
            {
                switch (type.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Object: return 0;
                    case SpecialType.System_String: return 1;
                    case SpecialType.System_Boolean: return 2;
                    case SpecialType.System_Char: return 3;
                    case SpecialType.System_SByte: return 4;
                    case SpecialType.System_Int16: return 5;
                    case SpecialType.System_Int32: return 6;
                    case SpecialType.System_Int64: return 7;
                    case SpecialType.System_Byte: return 8;
                    case SpecialType.System_UInt16: return 9;
                    case SpecialType.System_UInt32: return 10;
                    case SpecialType.System_UInt64: return 11;
                    case SpecialType.System_Single: return 12;
                    case SpecialType.System_Double: return 13;
                    case SpecialType.System_Decimal: return 14;

                    case SpecialType.None:
                        if (type != null && type.IsNullableType())
                        {
                            TypeSymbol underlyingType = type.GetNullableUnderlyingType();

                            switch (underlyingType.GetSpecialTypeSafe())
                            {
                                case SpecialType.System_Boolean: return 15;
                                case SpecialType.System_Char: return 16;
                                case SpecialType.System_SByte: return 17;
                                case SpecialType.System_Int16: return 18;
                                case SpecialType.System_Int32: return 19;
                                case SpecialType.System_Int64: return 20;
                                case SpecialType.System_Byte: return 21;
                                case SpecialType.System_UInt16: return 22;
                                case SpecialType.System_UInt32: return 23;
                                case SpecialType.System_UInt64: return 24;
                                case SpecialType.System_Single: return 25;
                                case SpecialType.System_Double: return 26;
                                case SpecialType.System_Decimal: return 27;
                            }
                        }

                        // fall through
                        goto default;

                    default: return null;
                }
            }

            public static BinaryOperatorKind OpKind(BinaryOperatorKind kind, TypeSymbol left, TypeSymbol right)
            {
                int? leftIndex = TypeToIndex(left);
                if (leftIndex == null)
                {
                    return BinaryOperatorKind.Error;
                }
                int? rightIndex = TypeToIndex(right);
                if (rightIndex == null)
                {
                    return BinaryOperatorKind.Error;
                }

                var result = BinaryOperatorKind.Error;

                // kind.OperatorIndex() collapses '&' and '&&' (and '|' and '||').  To correct
                // this problem, we handle kinds satisfying IsLogical() separately.  Fortunately,
                // such operators only work on boolean types, so there's no need to write out
                // a whole new table.
                //
                // Example: int & int is legal, but int && int is not, so we can't use the same
                // table for both operators.
                if (!kind.IsLogical() || (leftIndex == (int)BinaryOperatorKind.Bool && rightIndex == (int)BinaryOperatorKind.Bool))
                {
                    result = opkind[kind.OperatorIndex()][leftIndex.Value, rightIndex.Value];
                }

                return result == BinaryOperatorKind.Error ? result : result | kind;
            }
        }

        private void BinaryOperatorEasyOut(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, BinaryOperatorOverloadResolutionResult result)
        {
            var leftType = left.Type;
            if (leftType == null)
            {
                return;
            }

            var rightType = right.Type;
            if (rightType == null)
            {
                return;
            }

            if (PossiblyUnusualConstantOperation(left, right))
            {
                return;
            }

            var easyOut = BinopEasyOut.OpKind(kind, leftType, rightType);

            if (easyOut == BinaryOperatorKind.Error)
            {
                return;
            }

            BinaryOperatorSignature signature = this.Compilation.builtInOperators.GetSignature(easyOut);

            Conversion? leftConversion = Conversions.FastClassifyConversion(left.Type, signature.LeftType);
            Conversion? rightConversion = Conversions.FastClassifyConversion(right.Type, signature.RightType);

            Debug.Assert(leftConversion.HasValue && leftConversion.Value.IsImplicit);
            Debug.Assert(rightConversion.HasValue && rightConversion.Value.IsImplicit);

            result.Results.Add(BinaryOperatorAnalysisResult.Applicable(signature, leftConversion.Value, rightConversion.Value));
        }

        private static bool PossiblyUnusualConstantOperation(BoundExpression left, BoundExpression right)
        {
            Debug.Assert(left != null);
            Debug.Assert(left.Type != null);
            Debug.Assert(right != null);
            Debug.Assert(right.Type != null);

            // If there are "special" conversions available on either expression
            // then the early out is not accurate. For example, "myuint + myint" 
            // would normally be determined by the easy out as "long + long". But
            // "myuint + 1" does not choose that overload because there is a special
            // conversion from 1 to uint. 

            // If we have one or more constants, then both operands have to be 
            // int, both have to be bool, or both have to be string. Otherwise
            // we skip the easy out and go for the slow path.

            if (left.ConstantValue == null && right.ConstantValue == null)
            {
                // Neither is constant. Go for the easy out.
                return false;
            }
            
            // One or both operands are constants. See if they are both int, bool or string.

            if (left.Type.SpecialType != right.Type.SpecialType)
            {
                // They are unequal types. Go for the slow path.
                return true;
            }

            if (left.Type.SpecialType == SpecialType.System_Int32 ||
                left.Type.SpecialType == SpecialType.System_Boolean ||
                left.Type.SpecialType == SpecialType.System_String)
            {
                // They are both int, both bool, or both string. Go for the fast path.
                return false;
            }

            // We don't know what's going on. Go for the slow path.
            return true;
        }

        private void AddDelegateOperation(BinaryOperatorKind kind, TypeSymbol delegateType,
            ArrayBuilder<BinaryOperatorSignature> operators)
        {
            switch (kind)
            {
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Delegate, delegateType, delegateType, Compilation.GetSpecialType(SpecialType.System_Boolean)));
                    break;
                case BinaryOperatorKind.Addition:
                case BinaryOperatorKind.Subtraction:
                default:
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Delegate, delegateType, delegateType, delegateType));
                    break;
            }
        }

        private void GetDelegateOperations(BinaryOperatorKind kind, BoundExpression left, BoundExpression right,
            ArrayBuilder<BinaryOperatorSignature> operators)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            switch (kind)
            {
                case BinaryOperatorKind.Multiplication:
                case BinaryOperatorKind.Division:
                case BinaryOperatorKind.Remainder:
                case BinaryOperatorKind.RightShift:
                case BinaryOperatorKind.LeftShift:
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                case BinaryOperatorKind.LogicalAnd:
                case BinaryOperatorKind.LogicalOr:

                    return;
                case BinaryOperatorKind.Addition:
                case BinaryOperatorKind.Subtraction:
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    break;
                default:
                    Debug.Fail("Unhandled bin op kind in get delegate operation");
                    return;
            }

            var leftType = left.Type;
            var leftDelegate = leftType != null && leftType.IsDelegateType();
            var rightType = right.Type;
            var rightDelegate = rightType != null && rightType.IsDelegateType();

            // If no operands have delegate types then add nothing.
            if (!leftDelegate && !rightDelegate)
            {
                return;
            }

            // We might have a situation like
            //
            // Func<string> + Func<object>
            // 
            // in which case overload resolution should consider both 
            //
            // Func<string> + Func<string>
            // Func<object> + Func<object>
            //
            // are candidates (and it will pick Func<object>). Similarly,
            // we might have something like:
            //
            // Func<object> + Func<dynamic>
            // 
            // in which case neither candidate is better than the other,
            // resulting in an error.
            //
            // We could as an optimization say that if you are adding two completely
            // dissimilar delegate types D1 and D2, that neither is added to the candidate
            // set because neither can possibly be applicable, but let's not go there.
            // Let's just add them to the set and let overload resolution (and the 
            // error recovery heuristics) have at the real candidate set.
            //
            // However, we will take a spec violation for this scenario:
            //
            // SPEC VIOLATION:
            //
            // Technically the spec implies that we ought to be able to compare 
            // 
            // Func<int> x = whatever;
            // bool y = x == ()=>1;
            //
            // The native compiler does not allow this, since it is weird. I see no
            // reason why we ought to allow this. However, a good question is whether
            // the violation ought to be here, where we are determining the operator
            // candidate set, or in overload resolution where we are determining applicability.
            // In the native compiler we did it during candidate set determination, 
            // so let's stick with that.

            if (leftDelegate && rightDelegate)
            {
                // They are both delegate types. Add them both if they are different types.
                AddDelegateOperation(kind, leftType, operators);
                if (leftType != rightType)
                {
                    AddDelegateOperation(kind, rightType, operators);
                }
                return;
            }

            // One of them is a delegate, the other is not.
            TypeSymbol delegateType = leftDelegate ? leftType : rightType;
            BoundExpression nonDelegate = leftDelegate ? right : left;

            if ((kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual)
                && nonDelegate.Kind == BoundKind.UnboundLambda)
            {
                return;
            }

            AddDelegateOperation(kind, delegateType, operators);
        }

        private void GetEnumOperation(BinaryOperatorKind kind, TypeSymbol enumType, ArrayBuilder<BinaryOperatorSignature> operators)
        {
            Debug.Assert(enumType != null);

            if (!enumType.IsValidEnumType())
            {
                return;
            }

            var underlying = enumType.GetEnumUnderlyingType();
            Debug.Assert(underlying != null);
            Debug.Assert(underlying.SpecialType != SpecialType.None);

            var nullable = Compilation.GetSpecialType(SpecialType.System_Nullable_T);
            var nullableEnum = nullable.Construct(enumType);
            var nullableUnderlying = nullable.Construct(underlying);

            switch (kind)
            {
                case BinaryOperatorKind.Addition:
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.EnumAndUnderlyingAddition, enumType, underlying, enumType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.UnderlyingAndEnumAddition, underlying, enumType, enumType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedEnumAndUnderlyingAddition, nullableEnum, nullableUnderlying, nullableEnum));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedUnderlyingAndEnumAddition, nullableUnderlying, nullableEnum, nullableEnum));
                    break;
                case BinaryOperatorKind.Subtraction:
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.EnumSubtraction, enumType, enumType, underlying));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.EnumAndUnderlyingSubtraction, enumType, underlying, enumType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedEnumSubtraction, nullableEnum, nullableEnum, nullableUnderlying));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedEnumAndUnderlyingSubtraction, nullableEnum, nullableUnderlying, nullableEnum));
                    break;
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    var boolean = Compilation.GetSpecialType(SpecialType.System_Boolean);
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Enum, enumType, enumType, boolean));
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Lifted | BinaryOperatorKind.Enum, nullableEnum, nullableEnum, boolean));
                    break;
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Enum, enumType, enumType, enumType));
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Lifted | BinaryOperatorKind.Enum, nullableEnum, nullableEnum, nullableEnum));
                    break;
            }
        }

        private void GetPointerOperation(BinaryOperatorKind kind, TypeSymbol type, ArrayBuilder<BinaryOperatorSignature> operators)
        {
            Debug.Assert(type != null);

            var pointerType = type as PointerTypeSymbol;
            if (pointerType == null)
            {
                return;
            }

            switch (kind)
            {
                case BinaryOperatorKind.Addition:
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndIntAddition, pointerType, Compilation.GetSpecialType(SpecialType.System_Int32), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndUIntAddition, pointerType, Compilation.GetSpecialType(SpecialType.System_UInt32), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndLongAddition, pointerType, Compilation.GetSpecialType(SpecialType.System_Int64), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndULongAddition, pointerType, Compilation.GetSpecialType(SpecialType.System_UInt64), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.IntAndPointerAddition, Compilation.GetSpecialType(SpecialType.System_Int32), pointerType, pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.UIntAndPointerAddition, Compilation.GetSpecialType(SpecialType.System_UInt32), pointerType, pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LongAndPointerAddition, Compilation.GetSpecialType(SpecialType.System_Int64), pointerType, pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.ULongAndPointerAddition, Compilation.GetSpecialType(SpecialType.System_UInt64), pointerType, pointerType));
                    break;
                case BinaryOperatorKind.Subtraction:
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndIntSubtraction, pointerType, Compilation.GetSpecialType(SpecialType.System_Int32), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndUIntSubtraction, pointerType, Compilation.GetSpecialType(SpecialType.System_UInt32), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndLongSubtraction, pointerType, Compilation.GetSpecialType(SpecialType.System_Int64), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndULongSubtraction, pointerType, Compilation.GetSpecialType(SpecialType.System_UInt64), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerSubtraction, pointerType, pointerType, Compilation.GetSpecialType(SpecialType.System_Int64)));
                    break;
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Pointer, pointerType, pointerType, Compilation.GetSpecialType(SpecialType.System_Boolean)));
                    break;
            }
        }

        private void GetEnumOperations(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, ArrayBuilder<BinaryOperatorSignature> results)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            // First take some easy outs:
            switch (kind)
            {
                case BinaryOperatorKind.Multiplication:
                case BinaryOperatorKind.Division:
                case BinaryOperatorKind.Remainder:
                case BinaryOperatorKind.RightShift:
                case BinaryOperatorKind.LeftShift:
                case BinaryOperatorKind.LogicalAnd:
                case BinaryOperatorKind.LogicalOr:
                    return;
            }

            var leftType = left.Type;
            if (leftType != null)
            {
                leftType = TypeOrUnderlyingType(leftType);
            }

            var rightType = right.Type;
            if (rightType != null)
            {
                rightType = TypeOrUnderlyingType(rightType);
            }

            switch (kind)
            {
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                case BinaryOperatorKind.Addition:
                case BinaryOperatorKind.Subtraction:
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    if (leftType != null)
                    {
                        GetEnumOperation(kind, leftType, results);
                    }
                    if (rightType != null)
                    {
                        if (rightType != leftType)
                        {
                            GetEnumOperation(kind, rightType, results);
                        }
                    }
                    break;
                default:
                    Debug.Fail("Unhandled bin op kind in get enum operations");
                    return;
            }
        }

        private void GetPointerOperations(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, ArrayBuilder<BinaryOperatorSignature> results)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            // First take some easy outs:
            switch (kind)
            {
                case BinaryOperatorKind.Multiplication:
                case BinaryOperatorKind.Division:
                case BinaryOperatorKind.Remainder:
                case BinaryOperatorKind.RightShift:
                case BinaryOperatorKind.LeftShift:
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                case BinaryOperatorKind.LogicalAnd:
                case BinaryOperatorKind.LogicalOr:
                    return;
                case BinaryOperatorKind.Addition:
                case BinaryOperatorKind.Subtraction:
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    break;
                default:
                    Debug.Fail("Unhandled bin op kind in get pointer operation");
                    return;
            }

            var leftType = left.Type;
            var rightType = right.Type;

            if (leftType != null)
            {
                GetPointerOperation(kind, leftType, results);
            }

            if (rightType != null && rightType != leftType)
            {
                GetPointerOperation(kind, rightType, results);
            }
        }

        private void GetAllBuiltInOperators(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, ArrayBuilder<BinaryOperatorAnalysisResult> results)
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

            var operators = ArrayBuilder<BinaryOperatorSignature>.GetInstance();
            this.Compilation.builtInOperators.GetSimpleBuiltInOperators(kind, operators);
            GetDelegateOperations(kind, left, right, operators);
            GetEnumOperations(kind, left, right, operators);
            GetPointerOperations(kind, left, right, operators);

            CandidateOperators(operators, left, right, results);
            operators.Free();
        }

        private void CandidateOperators(ArrayBuilder<BinaryOperatorSignature> operators, BoundExpression left, BoundExpression right, ArrayBuilder<BinaryOperatorAnalysisResult> results)
        {
            foreach (var op in operators)
            {
                var convLeft = Conversions.ClassifyConversion(left, op.LeftType);
                var convRight = Conversions.ClassifyConversion(right, op.RightType);
                if (convLeft.IsImplicit && convRight.IsImplicit)
                {
                    results.Add(BinaryOperatorAnalysisResult.Applicable(op, convLeft, convRight));
                }
                else
                {
                    results.Add(BinaryOperatorAnalysisResult.Inapplicable(op, convLeft, convRight));
                }
            }
        }

        // Returns an analysis of every matching user-defined binary operator, including whether the
        // operator is applicable or not.

        private void GetUserDefinedOperators(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, ArrayBuilder<BinaryOperatorAnalysisResult> results)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            // The following is a slight rewording of the specification to emphasize that not all
            // operands of a binary operation need to have a type.

            // SPEC: An operation of the form x op y, where op is an overloadable binary operator is processed as follows:
            // SPEC: The set of candidate user-defined operators provided by the types (if any) of x and y for the 
            // SPEC: operation operator op(x, y) is determined. The set consists of the union of the candidate operators
            // SPEC: provided by the type of x (if any) and the candidate operators provided by the type of y (if any), 
            // SPEC: each determined using the rules of 7.3.5. Candidate operators only occur in the combined set once.

            var operators = ArrayBuilder<BinaryOperatorAnalysisResult>.GetInstance();
            var leftType = left.Type;
            if (leftType != null)
            {
                GetUserDefinedOperators(kind, leftType, left, right, operators);
            }

            var rightType = right.Type;
            if (rightType != null)
            {
                var rightOperators = ArrayBuilder<BinaryOperatorAnalysisResult>.GetInstance();
                GetUserDefinedOperators(kind, rightType, left, right, rightOperators);
                operators.UnionWith(rightOperators);
                rightOperators.Free();
            }

            results.AddRange(operators);
            operators.Free();
        }

        // Returns an analysis of every matching user-defined binary operator, including whether the
        // operator is applicable or not.

        private void GetUserDefinedOperators(UnaryOperatorKind kind, BoundExpression operand, ArrayBuilder<UnaryOperatorAnalysisResult> results)
        {
            Debug.Assert(operand != null);

            // UNDONE: Quote spec;
            // UNDONE: Make effiecient
            var operandType = operand.Type;
            if (operandType != null)
            {
                GetUserDefinedOperators(kind, operandType, operand, results);
            }
        }

        private void GetUserDefinedOperators(UnaryOperatorKind kind, TypeSymbol type, BoundExpression operand, ArrayBuilder<UnaryOperatorAnalysisResult> results)
        {
            // UNDONE: Quote spec
            var underlyingType = TypeOrUnderlyingType(type);

            for (var t = underlyingType; t != null; t = t.BaseType)
            {
                // UNDONE: Quote spec                
            }
        }

        private void GetUserDefinedOperators(BinaryOperatorKind kind, TypeSymbol type, BoundExpression left, BoundExpression right, ArrayBuilder<BinaryOperatorAnalysisResult> results)
        {
            // SPEC: Given a type T and an operation operator op(A), where op is an overloadable 
            // SPEC: operator and A is an argument list, the set of candidate user-defined operators 
            // SPEC: provided by T for operator op(A) is determined as follows:

            // SPEC: Determine the type T0. If T is a nullable type, T0 is its underlying type, 
            // SPEC: otherwise T0 is equal to T.

            var underlyingType = TypeOrUnderlyingType(type);

            for (var t = underlyingType; t != null; t = t.BaseType)
            {
                // UNDONE SPEC: For all operator op declarations in T0 and all lifted forms of such operators, 
                // UNDONE SPEC: if at least one operator is applicable (7.5.3.1) with respect to the argument 
                // UNDONE SPEC: list A, then the set of candidate operators consists of all such applicable 
                // UNDONE SPEC: operators in T0.

                // UNDONE SPEC: Otherwise, if T0 is object, the set of candidate operators is empty.

                // UNDONE SPEC: Otherwise, the set of candidate operators provided by T0 is the set of candidate 
                // UNDONE SPEC: operators provided by the direct base class of T0, or the effective base class of
                // UNDONE SPEC: T0 if T0 is a type parameter.
            }
        }

        // Takes a list of candidates and mutates the list to throw out the ones that are worse than
        // another applicable candidate.
        private void BinaryOperatorOverloadResolution(
            BoundExpression left,
            BoundExpression right,
            BinaryOperatorOverloadResolutionResult result
            )
        {
            // SPEC: Given the set of applicable candidate function members, the best function member in that set is located. 
            // SPEC: If the set contains only one function member, then that function member is the best function member. 

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
                    var better = BetterOperator(candidates[i].Signature, candidates[j].Signature, left, right);
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

        private bool IsApplicable(BinaryOperatorSignature binaryOperator, BoundExpression left, BoundExpression right)
        {
            return
                Conversions.ClassifyConversion(left, binaryOperator.LeftType).IsImplicit &&
                Conversions.ClassifyConversion(right, binaryOperator.RightType).IsImplicit;
        }

        private BetterResult BetterOperator(BinaryOperatorSignature op1, BinaryOperatorSignature op2, BoundExpression left, BoundExpression right)
        {
            var leftBetter = BetterConversionFromExpression(left, op1.LeftType, op2.LeftType);
            var rightBetter = BetterConversionFromExpression(right, op1.RightType, op2.RightType);

            if (leftBetter == BetterResult.Neither)
            {
                return rightBetter;
            }

            if (rightBetter == BetterResult.Neither)
            {
                return leftBetter;
            }

            if (leftBetter == rightBetter)
            {
                return leftBetter;
            }

            return BetterResult.Neither;
        }
    }

    internal enum OperatorAnalysisResultKind
    {
        Inapplicable,
        Worse,
        Applicable,
    }

    internal struct BinaryOperatorAnalysisResult
    {
        public bool IsValid { get { return this.Kind == OperatorAnalysisResultKind.Applicable; } }
        public OperatorAnalysisResultKind Kind { get; private set; }
        public BinaryOperatorSignature Signature { get; private set; }
        public Conversion LeftConversion { get; private set; }
        public Conversion RightConversion { get; private set; }

        private BinaryOperatorAnalysisResult(OperatorAnalysisResultKind kind, BinaryOperatorSignature signature, Conversion leftConversion, Conversion rightConversion)
            : this()
        {
            this.Kind = kind;
            this.Signature = signature;
            this.LeftConversion = leftConversion;
            this.RightConversion = rightConversion;
        }

        public static BinaryOperatorAnalysisResult Applicable(BinaryOperatorSignature signature, Conversion leftConversion, Conversion rightConversion)
        {
            return new BinaryOperatorAnalysisResult(OperatorAnalysisResultKind.Applicable, signature, leftConversion, rightConversion);
        }

        public static BinaryOperatorAnalysisResult Inapplicable(BinaryOperatorSignature signature, Conversion leftConversion, Conversion rightConversion)
        {
            return new BinaryOperatorAnalysisResult(OperatorAnalysisResultKind.Inapplicable, signature, leftConversion, rightConversion);
        }

        public BinaryOperatorAnalysisResult Worse()
        {
            return new BinaryOperatorAnalysisResult(OperatorAnalysisResultKind.Worse, this.Signature, this.LeftConversion, this.RightConversion);
        }
    }

    internal sealed class BinaryOperatorOverloadResolutionResult
    {
        public ArrayBuilder<BinaryOperatorAnalysisResult> Results { get; private set; }

        private BinaryOperatorOverloadResolutionResult()
        {
            this.Results = new ArrayBuilder<BinaryOperatorAnalysisResult>(10);
        }

        public bool AnyValid()
        {
            return GetValidCount() > 0;
        }

        public int GetValidCount()
        {
            int count = 0;

            for (int i = 0, n = Results.Count; i < n; i++)
            {
                if (Results[i].IsValid)
                {
                    count++;
                }
            }

            return count;
        }

        public BinaryOperatorAnalysisResult? Best
        {
            get
            {
                BinaryOperatorAnalysisResult? best = null;
                foreach (var result in Results)
                {
                    if (result.IsValid)
                    {
                        if (best != null)
                        {
                            // More than one best applicable method
                            return null;
                        }

                        best = result;
                    }
                }

                return best;
            }
        }

#if DEBUG
        public string Dump()
        {
            if (Results.Count == 0)
            {
                return "Overload resolution failed because there were no candidate operators.";
            }

            var sb = new StringBuilder();
            if (this.Best != null)
            {
                sb.AppendLine("Overload resolution succeeded and chose " + this.Best.Value.Signature.ToString());
            }
            else if (CountKind(OperatorAnalysisResultKind.Applicable) > 1)
            {
                sb.AppendLine("Overload resolution failed because of ambiguous possible best operators.");
            }
            else
            {
                sb.AppendLine("Overload resolution failed because no operator was applicable.");
            }

            sb.AppendLine("Detailed results:");
            foreach (var result in Results)
            {
                sb.AppendFormat("operator: {0} reason: {1}\n", result.Signature.ToString(), result.Kind.ToString());
            }

            return sb.ToString();
        }

        int CountKind(OperatorAnalysisResultKind kind)
        {
            int count = 0;
            for (int i = 0, n = this.Results.Count; i < n; i++)
            {
                if (this.Results[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }
#endif

        #region "Poolable"

        public static BinaryOperatorOverloadResolutionResult GetInstance()
        {
            return Pool.Allocate();
        }

        public void Free()
        {
            this.Results.Clear();
            Pool.Free(this);
        }

        public static readonly ObjectPool<BinaryOperatorOverloadResolutionResult> Pool = CreatePool();

        private static ObjectPool<BinaryOperatorOverloadResolutionResult> CreatePool()
        {
            ObjectPool<BinaryOperatorOverloadResolutionResult> pool = null;
            pool = new ObjectPool<BinaryOperatorOverloadResolutionResult>(() => new BinaryOperatorOverloadResolutionResult(), 10);
            return pool;
        }

        #endregion
    }

    internal struct UnaryOperatorAnalysisResult
    {
        public bool IsValid { get { return this.Kind == OperatorAnalysisResultKind.Applicable; } }
        public OperatorAnalysisResultKind Kind { get; private set; }
        public UnaryOperatorSignature Signature { get; private set; }
        public Conversion Conversion { get; private set; }

        private UnaryOperatorAnalysisResult(OperatorAnalysisResultKind kind, UnaryOperatorSignature signature, Conversion conversion)
            : this()
        {
            this.Kind = kind;
            this.Signature = signature;
            this.Conversion = conversion;
        }

        public static UnaryOperatorAnalysisResult Applicable(UnaryOperatorSignature signature, Conversion conversion)
        {
            return new UnaryOperatorAnalysisResult(OperatorAnalysisResultKind.Applicable, signature, conversion);
        }

        public static UnaryOperatorAnalysisResult Inapplicable(UnaryOperatorSignature signature, Conversion conversion)
        {
            return new UnaryOperatorAnalysisResult(OperatorAnalysisResultKind.Inapplicable, signature, conversion);
        }

        public UnaryOperatorAnalysisResult Worse()
        {
            return new UnaryOperatorAnalysisResult(OperatorAnalysisResultKind.Worse, this.Signature, this.Conversion);
        }
    }

    internal sealed class UnaryOperatorOverloadResolutionResult
    {
        public ArrayBuilder<UnaryOperatorAnalysisResult> Results { get; private set; }

        public UnaryOperatorOverloadResolutionResult()
        {
            this.Results = new ArrayBuilder<UnaryOperatorAnalysisResult>(10);
        }

        public bool AnyValid()
        {
            return GetValidCount() > 0;
        }

        public int GetValidCount()
        {
            int count = 0;

            for (int i = 0, n = Results.Count; i < n; i++)
            {
                if (Results[i].IsValid)
                {
                    count++;
                }
            }

            return count;
        }

        public UnaryOperatorAnalysisResult? Best
        {
            get
            {
                UnaryOperatorAnalysisResult? best = null;
                foreach (var result in Results)
                {
                    if (result.IsValid)
                    {
                        if (best != null)
                        {
                            // More than one best applicable method
                            return null;
                        }
                        best = result;
                    }
                }
                return best;
            }
        }

#if DEBUG
        public string Dump()
        {
            if (Results.Count == 0)
            {
                return "Overload resolution failed because there were no candidate operators.";
            }

            var sb = new StringBuilder();
            if (this.Best != null)
            {
                sb.AppendLine("Overload resolution succeeded and chose " + this.Best.Value.Signature.ToString());
            }
            else if (CountKind(OperatorAnalysisResultKind.Applicable) > 1)
            {
                sb.AppendLine("Overload resolution failed because of ambiguous possible best operators.");
            }
            else
            {
                sb.AppendLine("Overload resolution failed because no operator was applicable.");
            }

            sb.AppendLine("Detailed results:");
            foreach (var result in Results)
            {
                sb.AppendFormat("operator: {0} reason: {1}\n", result.Signature.ToString(), result.Kind.ToString());
            }

            return sb.ToString();
        }

        int CountKind(OperatorAnalysisResultKind kind)
        {
            int count = 0;
            for (int i = 0, n = this.Results.Count; i < n; i++)
            {
                if (this.Results[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }
#endif

        #region "Poolable"

        public static UnaryOperatorOverloadResolutionResult GetInstance()
        {
            return Pool.Allocate();
        }

        public void Free()
        {
            this.Results.Clear();
            Pool.Free(this);
        }

        //2) Expose the pool or the way to create a pool or the way to get an instance.
        //       for now we will expose both and figure which way works better
        public static readonly ObjectPool<UnaryOperatorOverloadResolutionResult> Pool = CreatePool();

        private static ObjectPool<UnaryOperatorOverloadResolutionResult> CreatePool()
        {
            ObjectPool<UnaryOperatorOverloadResolutionResult> pool = null;
            pool = new ObjectPool<UnaryOperatorOverloadResolutionResult>(() => new UnaryOperatorOverloadResolutionResult(), 10);
            return pool;
        }

        #endregion
    }

    internal static partial class Extensions
    {
        public static int OperatorIndex(this UnaryOperatorKind kind)
        {
            return ((int)kind.Operator() >> 8) - 16;
        }

        public static UnaryOperatorKind Operator(this UnaryOperatorKind kind)
        {
            return kind & UnaryOperatorKind.OpMask;
        }

        public static bool IsLifted(this UnaryOperatorKind kind)
        {
            return 0 != (kind & UnaryOperatorKind.Lifted);
        }

        public static UnaryOperatorKind OperandTypes(this UnaryOperatorKind kind)
        {
            return kind & UnaryOperatorKind.TypeMask;
        }

        public static int OperatorIndex(this BinaryOperatorKind kind)
        {
            return ((int)kind.Operator() >> 8) - 16;
        }

        public static BinaryOperatorKind Operator(this BinaryOperatorKind kind)
        {
            return kind & BinaryOperatorKind.OpMask;
        }

        public static BinaryOperatorKind OperatorWithLogical(this BinaryOperatorKind kind)
        {
            return kind & (BinaryOperatorKind.OpMask | BinaryOperatorKind.Logical);
        }

        public static BinaryOperatorKind WithType(this BinaryOperatorKind kind, SpecialType type)
        {
            Debug.Assert(kind == (kind & ~BinaryOperatorKind.TypeMask));
            switch (type)
            {
                case SpecialType.System_Int32:
                    return kind | BinaryOperatorKind.Int;
                case SpecialType.System_UInt32:
                    return kind | BinaryOperatorKind.UInt;
                case SpecialType.System_Int64:
                    return kind | BinaryOperatorKind.Long;
                case SpecialType.System_UInt64:
                    return kind | BinaryOperatorKind.ULong;
                default:
                    Debug.Fail("Unexpected binary operator type.");
                    return kind;
            }
        }

        public static bool IsLifted(this BinaryOperatorKind kind)
        {
            return 0 != (kind & BinaryOperatorKind.Lifted);
        }

        public static bool IsEnum(this BinaryOperatorKind kind)
        {
            switch (kind.OperandTypes())
            {
                case BinaryOperatorKind.Enum:
                case BinaryOperatorKind.EnumAndUnderlying:
                case BinaryOperatorKind.UnderlyingAndEnum:
                    return true;
            }

            return false;
        }

        public static bool IsLogical(this BinaryOperatorKind kind)
        {
            return 0 != (kind & BinaryOperatorKind.Logical);
        }

        public static BinaryOperatorKind OperandTypes(this BinaryOperatorKind kind)
        {
            return kind & BinaryOperatorKind.TypeMask;
        }

        public static bool IsUserDefined(this BinaryOperatorKind kind)
        {
            return (kind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;
        }

        public static bool IsShift(this BinaryOperatorKind kind)
        {
            BinaryOperatorKind type = kind & BinaryOperatorKind.TypeMask;
            return type == BinaryOperatorKind.LeftShift || type == BinaryOperatorKind.RightShift;
        }
    }
}
