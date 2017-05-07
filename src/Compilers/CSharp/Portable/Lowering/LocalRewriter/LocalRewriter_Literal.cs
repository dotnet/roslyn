// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitLiteral(BoundLiteral node)
        {
            return MakeLiteral(node.Syntax, node.ConstantValue, node.Type, oldNodeOpt: node);
        }

        private BoundExpression MakeLiteral(SyntaxNode syntax, ConstantValue constantValue, TypeSymbol type, BoundLiteral oldNodeOpt = null)
        {
            return MakeLiteral(syntax, constantValue, type, _compilation, _factory.CurrentMethod, _inExpressionLambda, oldNodeOpt);
        }

        /// <summary>
        /// Pass in 'currentMethod' and 'inExpressionLambda' if this is called during lowering, otherwise set 'currentMethod' to null (i.e. for IOperation purposes) 
        /// </summary>
        private static BoundExpression MakeLiteral(SyntaxNode syntax, ConstantValue constantValue, TypeSymbol type, CSharpCompilation compilation, MethodSymbol currentMethod, bool inExpressionLambda)
        {
            return MakeLiteral(syntax, constantValue, type, compilation, currentMethod, inExpressionLambda, null);
        }

        private static BoundExpression MakeLiteral(SyntaxNode syntax, ConstantValue constantValue, TypeSymbol type, CSharpCompilation compilation, MethodSymbol currentMethod, bool inExpressionLambda, BoundLiteral oldNodeOpt)
        {
            Debug.Assert(constantValue != null);

            if (constantValue.IsDecimal)
            {
                //  Rewrite decimal literal
                Debug.Assert((object)type != null);
                Debug.Assert(type.SpecialType == SpecialType.System_Decimal);

                return MakeDecimalLiteral(syntax, constantValue, compilation, currentMethod, inExpressionLambda);
            }
            else if (constantValue.IsDateTime)
            {
                // C# does not support DateTime constants but VB does; we might have obtained a 
                // DateTime constant by calling a method with an optional parameter with a DateTime
                // for its default value.
                Debug.Assert((object)type != null);
                Debug.Assert(type.SpecialType == SpecialType.System_DateTime);
                return MakeDateTimeLiteral(syntax, constantValue, compilation);
            }
            else if (oldNodeOpt != null)
            {
                return oldNodeOpt.Update(constantValue, type);
            }
            else
            {
                return new BoundLiteral(syntax, constantValue, type, hasErrors: constantValue.IsBad);
            }
        }

        /// <summary>
        /// If 'currentMethod' is null, we will skip optimization of calling a simpler constructor, or using a predefined constant
        /// </summary>
        private static BoundExpression MakeDecimalLiteral(SyntaxNode syntax, ConstantValue constantValue, CSharpCompilation compilation, MethodSymbol currentMethod, bool inExpressionLambda)
        {
            Debug.Assert(constantValue != null);
            Debug.Assert(constantValue.IsDecimal);

            var value = constantValue.DecimalValue;
            bool isNegative;
            byte scale;
            uint low, mid, high;
            value.GetBits(out isNegative, out scale, out low, out mid, out high);

            var arguments = new ArrayBuilder<BoundExpression>();
            SpecialMember member;

            // check if we can call a simpler constructor, or use a predefined constant
            if (scale == 0 && int.MinValue <= value && value <= int.MaxValue && currentMethod != null)
            {
                // If we are building static constructor of System.Decimal, accessing static fields 
                // would be bad.
                if ((currentMethod.MethodKind != MethodKind.SharedConstructor ||
                   currentMethod.ContainingType.SpecialType != SpecialType.System_Decimal) &&
                   !inExpressionLambda)
                {
                    Symbol useField = null;

                    if (value == decimal.Zero)
                    {
                        useField = compilation.GetSpecialTypeMember(SpecialMember.System_Decimal__Zero);
                    }
                    else if (value == decimal.One)
                    {
                        useField = compilation.GetSpecialTypeMember(SpecialMember.System_Decimal__One);
                    }
                    else if (value == decimal.MinusOne)
                    {
                        useField = compilation.GetSpecialTypeMember(SpecialMember.System_Decimal__MinusOne);
                    }

                    if ((object)useField != null &&
                        !useField.HasUseSiteError &&
                        !useField.ContainingType.HasUseSiteError)
                    {
                        var fieldSymbol = (FieldSymbol)useField;
                        return new BoundFieldAccess(syntax, null, fieldSymbol, constantValue);
                    }
                }

                //new decimal(int);
                member = SpecialMember.System_Decimal__CtorInt32;
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create((int)value), compilation.GetSpecialType(SpecialType.System_Int32)));
            }
            else if (scale == 0 && uint.MinValue <= value && value <= uint.MaxValue)
            {
                //new decimal(uint);
                member = SpecialMember.System_Decimal__CtorUInt32;
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create((uint)value), compilation.GetSpecialType(SpecialType.System_UInt32)));
            }
            else if (scale == 0 && long.MinValue <= value && value <= long.MaxValue)
            {
                //new decimal(long);
                member = SpecialMember.System_Decimal__CtorInt64;
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create((long)value), compilation.GetSpecialType(SpecialType.System_Int64)));
            }
            else if (scale == 0 && ulong.MinValue <= value && value <= ulong.MaxValue)
            {
                //new decimal(ulong);
                member = SpecialMember.System_Decimal__CtorUInt64;
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create((ulong)value), compilation.GetSpecialType(SpecialType.System_UInt64)));
            }
            else
            {
                //new decimal(int low, int mid, int high, bool isNegative, byte scale);
                member = SpecialMember.System_Decimal__CtorInt32Int32Int32BooleanByte;
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(low), compilation.GetSpecialType(SpecialType.System_Int32)));
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(mid), compilation.GetSpecialType(SpecialType.System_Int32)));
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(high), compilation.GetSpecialType(SpecialType.System_Int32)));
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(isNegative), compilation.GetSpecialType(SpecialType.System_Boolean)));
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(scale), compilation.GetSpecialType(SpecialType.System_Byte)));
            }

            var ctor = (MethodSymbol)compilation.Assembly.GetSpecialTypeMember(member);
            Debug.Assert((object)ctor != null);
            Debug.Assert(ctor.ContainingType.SpecialType == SpecialType.System_Decimal);

            return new BoundObjectCreationExpression(
                syntax, ctor, arguments.ToImmutableAndFree(),
                default(ImmutableArray<string>), default(ImmutableArray<RefKind>), false, default(ImmutableArray<int>),
                constantValue, null, null, ctor.ContainingType);
        }

        private static BoundExpression MakeDateTimeLiteral(SyntaxNode syntax, ConstantValue constantValue, CSharpCompilation compilation)
        {
            Debug.Assert(constantValue != null);
            Debug.Assert(constantValue.IsDateTime);

            var arguments = new ArrayBuilder<BoundExpression>();
            arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(constantValue.DateTimeValue.Ticks), compilation.GetSpecialType(SpecialType.System_Int64)));

            var ctor = (MethodSymbol)compilation.Assembly.GetSpecialTypeMember(SpecialMember.System_DateTime__CtorInt64);
            Debug.Assert((object)ctor != null);
            Debug.Assert(ctor.ContainingType.SpecialType == SpecialType.System_DateTime);

            // This is not a constant from C#'s perspective, so do not mark it as one.
            return new BoundObjectCreationExpression(
                syntax, ctor, arguments.ToImmutableAndFree(),
                default(ImmutableArray<string>), default(ImmutableArray<RefKind>), false, default(ImmutableArray<int>),
                ConstantValue.NotAvailable, null, null, ctor.ContainingType);
        }
    }
}
