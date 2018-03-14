﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.RuntimeMembers;

namespace Microsoft.CodeAnalysis
{
    internal static class SpecialMembers
    {
        private readonly static ImmutableArray<MemberDescriptor> s_descriptors;

        static SpecialMembers()
        {
            byte[] initializationBytes = new byte[]
            {
                // System_String__CtorSZArrayChar
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,

                // System_String__ConcatStringString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_String__ConcatStringStringString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_String__ConcatStringStringStringString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_String__ConcatStringArray
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_String__ConcatObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_String__ConcatObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_String__ConcatObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_String__ConcatObjectArray
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_String__op_Equality
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_String__op_Inequality
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_String__Length
                (byte)MemberFlags.PropertyGet,                                                                              // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_String__Chars
                (byte)MemberFlags.PropertyGet,                                                                              // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_String__Format
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Double__IsNaN
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Double,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Single__IsNaN
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Single,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Delegate__Combine
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Delegate,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,

                // System_Delegate__Remove
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Delegate,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,

                // System_Delegate__op_Equality
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Delegate,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,

                // System_Delegate__op_Inequality
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Delegate,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,

                // System_Decimal__Zero
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,                                   // Field Signature

                // System_Decimal__MinusOne
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,                                   // Field Signature

                // System_Decimal__One
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,                                   // Field Signature

                // System_Decimal__CtorInt32
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Decimal__CtorUInt32
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,

                // System_Decimal__CtorInt64
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // System_Decimal__CtorUInt64
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,

                // System_Decimal__CtorSingle
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Decimal__CtorDouble
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Decimal__CtorInt32Int32Int32BooleanByte
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    5,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,

                // System_Decimal__op_Addition
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Subtraction
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Multiply
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Division
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Modulus
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_UnaryNegation
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Increment
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Decrement
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__NegateDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__RemainderDecimalDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__AddDecimalDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__SubtractDecimalDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__MultiplyDecimalDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__DivideDecimalDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__ModuloDecimalDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__CompareDecimalDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Equality
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Inequality
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_GreaterThan
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_GreaterThanOrEqual
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_LessThan
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_LessThanOrEqual
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Implicit_FromByte
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,

                // System_Decimal__op_Implicit_FromChar
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,

                // System_Decimal__op_Implicit_FromInt16
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,

                // System_Decimal__op_Implicit_FromInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Decimal__op_Implicit_FromInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // System_Decimal__op_Implicit_FromSByte
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte,

                // System_Decimal__op_Implicit_FromUInt16
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16,

                // System_Decimal__op_Implicit_FromUInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,

                // System_Decimal__op_Implicit_FromUInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,

                // System_Decimal__op_Explicit_ToByte
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToUInt16
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToSByte
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToInt16
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToChar
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToUInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToUInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_ToInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Decimal__op_Explicit_FromDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Decimal__op_Explicit_FromSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Decimal,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_DateTime__MinValue
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)SpecialType.System_DateTime,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,                                  // Field Signature

                // System_DateTime__CtorInt64
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_DateTime,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // System_DateTime__CompareDateTimeDateTime
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_DateTime,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,

                // System_DateTime__op_Equality
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_DateTime,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,

                // System_DateTime__op_Inequality
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_DateTime,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,

                // System_DateTime__op_GreaterThan
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_DateTime,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,

                // System_DateTime__op_GreaterThanOrEqual
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_DateTime,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,

                // System_DateTime__op_LessThan
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_DateTime,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,

                // System_DateTime__op_LessThanOrEqual
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_DateTime,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,

                // System_Collections_IEnumerable__GetEnumerator
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)SpecialType.System_Collections_IEnumerable,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_IEnumerator,

                // System_Collections_IEnumerator__Current
                (byte)(MemberFlags.Property | MemberFlags.Virtual),                                                         // Flags
                (byte)SpecialType.System_Collections_IEnumerator,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Collections_IEnumerator__get_Current
                (byte)(MemberFlags.PropertyGet | MemberFlags.Virtual),                                                      // Flags
                (byte)SpecialType.System_Collections_IEnumerator,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Collections_IEnumerator__MoveNext
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)SpecialType.System_Collections_IEnumerator,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Collections_IEnumerator__Reset
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)SpecialType.System_Collections_IEnumerator,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Collections_Generic_IEnumerable_T__GetEnumerator
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)SpecialType.System_Collections_Generic_IEnumerable_T,                                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerator_T,
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_IEnumerator_T__Current
                (byte)(MemberFlags.Property | MemberFlags.Virtual),                                                         // Flags
                (byte)SpecialType.System_Collections_Generic_IEnumerator_T,                                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_IEnumerator_T__get_Current
                (byte)(MemberFlags.PropertyGet | MemberFlags.Virtual),                                                      // Flags
                (byte)SpecialType.System_Collections_Generic_IEnumerator_T,                                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_IDisposable__Dispose
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)SpecialType.System_IDisposable,                                                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Array__Length
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)SpecialType.System_Array,                                                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Array__LongLength
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)SpecialType.System_Array,                                                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // System_Array__GetLowerBound
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)SpecialType.System_Array,                                                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Array__GetUpperBound
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)SpecialType.System_Array,                                                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Object__GetHashCode
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)SpecialType.System_Object,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Object__Equals
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)SpecialType.System_Object,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Object__ToString
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)SpecialType.System_Object,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Object__ReferenceEquals
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Object,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_IntPtr__op_Explicit_ToPointer
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_IntPtr,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_IntPtr,

                // System_IntPtr__op_Explicit_ToInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_IntPtr,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_IntPtr,

                // System_IntPtr__op_Explicit_ToInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_IntPtr,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_IntPtr,

                // System_IntPtr__op_Explicit_FromPointer
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_IntPtr,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_IntPtr,
                    (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_IntPtr__op_Explicit_FromInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_IntPtr,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_IntPtr,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_IntPtr__op_Explicit_FromInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_IntPtr,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_IntPtr,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // System_UIntPtr__op_Explicit_ToPointer
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_UIntPtr,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UIntPtr,

                // System_UIntPtr__op_Explicit_ToUInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_UIntPtr,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UIntPtr,

                // System_UIntPtr__op_Explicit_ToUInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_UIntPtr,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UIntPtr,

                // System_UIntPtr__op_Explicit_FromPointer
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_UIntPtr,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UIntPtr,
                    (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_UIntPtr__op_Explicit_FromUInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_UIntPtr,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UIntPtr,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,

                // System_UIntPtr__op_Explicit_FromUInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_UIntPtr,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UIntPtr,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,

                // System_Nullable_T_GetValueOrDefault
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)SpecialType.System_Nullable_T,                                                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Nullable_T_get_Value
                (byte)MemberFlags.PropertyGet,                                                                              // Flags
                (byte)SpecialType.System_Nullable_T,                                                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Nullable_T_get_HasValue
                (byte)MemberFlags.PropertyGet,                                                                              // Flags
                (byte)SpecialType.System_Nullable_T,                                                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Nullable_T__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)SpecialType.System_Nullable_T,                                                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Nullable_T__op_Implicit_FromT
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Nullable_T,                                                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Nullable_T,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Nullable_T__op_Explicit_ToT
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Nullable_T,                                                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Nullable_T,
            };

            string[] allNames = new string[(int)SpecialMember.Count]
            {
                ".ctor",                                    // System_String__CtorSZArrayChar
                "Concat",                                   // System_String__ConcatStringString
                "Concat",                                   // System_String__ConcatStringStringString
                "Concat",                                   // System_String__ConcatStringStringStringString
                "Concat",                                   // System_String__ConcatStringArray
                "Concat",                                   // System_String__ConcatObject
                "Concat",                                   // System_String__ConcatObjectObject
                "Concat",                                   // System_String__ConcatObjectObjectObject
                "Concat",                                   // System_String__ConcatObjectArray
                "op_Equality",                              // System_String__op_Equality
                "op_Inequality",                            // System_String__op_Inequality
                "get_Length",                               // System_String__Length
                "get_Chars",                                // System_String__Chars
                "Format",                                   // System_String__Format
                "IsNaN",                                    // System_Double__IsNaN
                "IsNaN",                                    // System_Single__IsNaN
                "Combine",                                  // System_Delegate__Combine
                "Remove",                                   // System_Delegate__Remove
                "op_Equality",                              // System_Delegate__op_Equality
                "op_Inequality",                            // System_Delegate__op_Inequality
                "Zero",                                     // System_Decimal__Zero
                "MinusOne",                                 // System_Decimal__MinusOne
                "One",                                      // System_Decimal__One
                ".ctor",                                    // System_Decimal__CtorInt32
                ".ctor",                                    // System_Decimal__CtorUInt32
                ".ctor",                                    // System_Decimal__CtorInt64
                ".ctor",                                    // System_Decimal__CtorUInt64
                ".ctor",                                    // System_Decimal__CtorSingle
                ".ctor",                                    // System_Decimal__CtorDouble
                ".ctor",                                    // System_Decimal__CtorInt32Int32Int32BooleanByte
                "op_Addition",                              // System_Decimal__op_Addition
                "op_Subtraction",                           // System_Decimal__op_Subtraction
                "op_Multiply",                              // System_Decimal__op_Multiply
                "op_Division",                              // System_Decimal__op_Division
                "op_Modulus",                               // System_Decimal__op_Modulus
                "op_UnaryNegation",                         // System_Decimal__op_UnaryNegation
                "op_Increment",                             // System_Decimal__op_Increment
                "op_Decrement",                             // System_Decimal__op_Decrement
                "Negate",                                   // System_Decimal__NegateDecimal
                "Remainder",                                // System_Decimal__RemainderDecimalDecimal
                "Add",                                      // System_Decimal__AddDecimalDecimal
                "Subtract",                                 // System_Decimal__SubtractDecimalDecimal
                "Multiply",                                 // System_Decimal__MultiplyDecimalDecimal
                "Divide",                                   // System_Decimal__DivideDecimalDecimal
                "Remainder",                                // System_Decimal__ModuloDecimalDecimal
                "Compare",                                  // System_Decimal__CompareDecimalDecimal
                "op_Equality",                              // System_Decimal__op_Equality
                "op_Inequality",                            // System_Decimal__op_Inequality
                "op_GreaterThan",                           // System_Decimal__op_GreaterThan
                "op_GreaterThanOrEqual",                    // System_Decimal__op_GreaterThanOrEqual
                "op_LessThan",                              // System_Decimal__op_LessThan
                "op_LessThanOrEqual",                       // System_Decimal__op_LessThanOrEqual
                "op_Implicit",                              // System_Decimal__op_Implicit_FromByte
                "op_Implicit",                              // System_Decimal__op_Implicit_FromChar
                "op_Implicit",                              // System_Decimal__op_Implicit_FromInt16
                "op_Implicit",                              // System_Decimal__op_Implicit_FromInt32
                "op_Implicit",                              // System_Decimal__op_Implicit_FromInt64
                "op_Implicit",                              // System_Decimal__op_Implicit_FromSByte
                "op_Implicit",                              // System_Decimal__op_Implicit_FromUInt16
                "op_Implicit",                              // System_Decimal__op_Implicit_FromUInt32
                "op_Implicit",                              // System_Decimal__op_Implicit_FromUInt64
                "op_Explicit",                              // System_Decimal__op_Explicit_ToByte
                "op_Explicit",                              // System_Decimal__op_Explicit_ToUInt16
                "op_Explicit",                              // System_Decimal__op_Explicit_ToSByte
                "op_Explicit",                              // System_Decimal__op_Explicit_ToInt16
                "op_Explicit",                              // System_Decimal__op_Explicit_ToSingle
                "op_Explicit",                              // System_Decimal__op_Explicit_ToDouble
                "op_Explicit",                              // System_Decimal__op_Explicit_ToChar
                "op_Explicit",                              // System_Decimal__op_Explicit_ToUInt64
                "op_Explicit",                              // System_Decimal__op_Explicit_ToInt32
                "op_Explicit",                              // System_Decimal__op_Explicit_ToUInt32
                "op_Explicit",                              // System_Decimal__op_Explicit_ToInt64
                "op_Explicit",                              // System_Decimal__op_Explicit_FromDouble
                "op_Explicit",                              // System_Decimal__op_Explicit_FromSingle
                "MinValue",                                 // System_DateTime__MinValue
                ".ctor",                                    // System_DateTime__CtorInt64
                "Compare",                                  // System_DateTime__CompareDateTimeDateTime
                "op_Equality",                              // System_DateTime__op_Equality
                "op_Inequality",                            // System_DateTime__op_Inequality
                "op_GreaterThan",                           // System_DateTime__op_GreaterThan
                "op_GreaterThanOrEqual",                    // System_DateTime__op_GreaterThanOrEqual
                "op_LessThan",                              // System_DateTime__op_LessThan
                "op_LessThanOrEqual",                       // System_DateTime__op_LessThanOrEqual
                "GetEnumerator",                            // System_Collections_IEnumerable__GetEnumerator
                "Current",                                  // System_Collections_IEnumerator__Current
                "get_Current",                              // System_Collections_IEnumerator__get_Current
                "MoveNext",                                 // System_Collections_IEnumerator__MoveNext
                "Reset",                                    // System_Collections_IEnumerator__Reset
                "GetEnumerator",                            // System_Collections_Generic_IEnumerable_T__GetEnumerator
                "Current",                                  // System_Collections_Generic_IEnumerator_T__Current
                "get_Current",                              // System_Collections_Generic_IEnumerator_T__get_Current
                "Dispose",                                  // System_IDisposable__Dispose
                "Length",                                   // System_Array__Length
                "LongLength",                               // System_Array__LongLength
                "GetLowerBound",                            // System_Array__GetLowerBound
                "GetUpperBound",                            // System_Array__GetUpperBound
                "GetHashCode",                              // System_Object__GetHashCode
                "Equals",                                   // System_Object__Equals
                "ToString",                                 // System_Object__ToString
                "ReferenceEquals",                          // System_Object__ReferenceEquals
                "op_Explicit",                              // System_IntPtr__op_Explicit_ToPointer
                "op_Explicit",                              // System_IntPtr__op_Explicit_ToInt32
                "op_Explicit",                              // System_IntPtr__op_Explicit_ToInt64
                "op_Explicit",                              // System_IntPtr__op_Explicit_FromPointer
                "op_Explicit",                              // System_IntPtr__op_Explicit_FromInt32
                "op_Explicit",                              // System_IntPtr__op_Explicit_FromInt64
                "op_Explicit",                              // System_UIntPtr__op_Explicit_ToPointer
                "op_Explicit",                              // System_UIntPtr__op_Explicit_ToUInt32
                "op_Explicit",                              // System_UIntPtr__op_Explicit_ToUInt64
                "op_Explicit",                              // System_UIntPtr__op_Explicit_FromPointer
                "op_Explicit",                              // System_UIntPtr__op_Explicit_FromUInt32
                "op_Explicit",                              // System_UIntPtr__op_Explicit_FromUInt64
                "GetValueOrDefault",                        // System_Nullable_T_GetValueOrDefault
                "get_Value",                                // System_Nullable_T_get_Value
                "get_HasValue",                             // System_Nullable_T_get_HasValue
                ".ctor",                                    // System_Nullable_T__ctor
                "op_Implicit",                              // System_Nullable_T__op_Implicit_FromT
                "op_Explicit",                              // System_Nullable_T__op_Explicit_ToT
            };

            s_descriptors = MemberDescriptor.InitializeFromStream(new System.IO.MemoryStream(initializationBytes, writable: false), allNames);
        }

        public static MemberDescriptor GetDescriptor(SpecialMember member)
        {
            return s_descriptors[(int)member];
        }
    }
}
