﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.RuntimeMembers;

namespace Microsoft.CodeAnalysis
{
    internal static class WellKnownMembers
    {
        private readonly static ImmutableArray<MemberDescriptor> s_descriptors;

        static WellKnownMembers()
        {
            byte[] initializationBytes = new byte[]
            {
                // System_Math__RoundDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Math,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Math__PowDoubleDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Math,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Array__get_Length
                (byte)MemberFlags.PropertyGet,                                                                              // Flags
                (byte)WellKnownType.System_Array,                                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Array__Empty
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Array,                                                                           // DeclaringTypeId
                1,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.SZArray,
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Convert__ToBooleanDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToBooleanInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Convert__ToBooleanUInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,

                // System_Convert__ToBooleanInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // System_Convert__ToBooleanUInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,

                // System_Convert__ToBooleanSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToBooleanDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToSByteDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToSByteDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToSByteSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToByteDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToByteDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToByteSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToInt16Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToInt16Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToInt16Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToUInt16Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToUInt16Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToUInt16Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToInt32Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToInt32Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToInt32Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToUInt32Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToUInt32Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToUInt32Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToInt64Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToInt64Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToInt64Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToUInt64Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToUInt64Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToUInt64Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToSingleDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToDoubleDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_CLSCompliantAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_CLSCompliantAttribute,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_FlagsAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_FlagsAttribute,                                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Guid__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Guid,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Type__GetTypeFromCLSID
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Type,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Guid,

                // System_Type__GetTypeFromHandle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Type,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_RuntimeTypeHandle,

                // System_Type__Missing
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Type,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,                                    // Field Signature

                // System_Reflection_AssemblyKeyFileAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Reflection_AssemblyKeyFileAttribute,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Reflection_AssemblyKeyNameAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Reflection_AssemblyKeyNameAttribute,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Reflection_MethodBase__GetMethodFromHandle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Reflection_MethodBase,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodBase,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_RuntimeMethodHandle,

                // System_Reflection_MethodBase__GetMethodFromHandle2
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Reflection_MethodBase,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodBase,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_RuntimeMethodHandle,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_RuntimeTypeHandle,

                // System_Reflection_MethodInfo__CreateDelegate
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Reflection_MethodInfo,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Delegate__CreateDelegate
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Delegate,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Delegate__CreateDelegate4
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_Delegate,                                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Reflection_FieldInfo__GetFieldFromHandle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Reflection_FieldInfo,                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_FieldInfo,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_RuntimeFieldHandle,

                // System_Reflection_FieldInfo__GetFieldFromHandle2
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Reflection_FieldInfo,                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_FieldInfo,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_RuntimeFieldHandle,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_RuntimeTypeHandle,

                // System_Reflection_Missing__Value
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Reflection_Missing,                                                              // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_Missing,                      // Field Signature

                // System_IEquatable_T__Equals
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_IEquatable_T,                                                                    // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_EqualityComparer_T__Equals
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_Generic_EqualityComparer_T,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_EqualityComparer_T__GetHashCode
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_Generic_EqualityComparer_T,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_EqualityComparer_T__get_Default
                (byte)(MemberFlags.PropertyGet | MemberFlags.Static),                                                       // Flags
                (byte)WellKnownType.System_Collections_Generic_EqualityComparer_T,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Collections_Generic_EqualityComparer_T,

                // System_AttributeUsageAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_AttributeUsageAttribute,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, 0,

                // System_AttributeUsageAttribute__AllowMultiple
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_AttributeUsageAttribute,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_AttributeUsageAttribute__Inherited
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_AttributeUsageAttribute,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_ParamArrayAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ParamArrayAttribute,                                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_STAThreadAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_STAThreadAttribute,                                                              // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Reflection_DefaultMemberAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Reflection_DefaultMemberAttribute,                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Diagnostics_Debugger__Break
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Diagnostics_Debugger,                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Diagnostics_DebuggerDisplayAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerDisplayAttribute,                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Diagnostics_DebuggerDisplayAttribute__Type
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerDisplayAttribute,                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Diagnostics_DebuggerNonUserCodeAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerNonUserCodeAttribute,                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Diagnostics_DebuggerHiddenAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerHiddenAttribute,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Diagnostics_DebuggerBrowsableAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerBrowsableAttribute,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Diagnostics_DebuggerBrowsableState,

                // System_Diagnostics_DebuggerStepThroughAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerStepThroughAttribute,                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Diagnostics_DebuggableAttribute__ctorDebuggingModes
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggableAttribute,                                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes,

                // System_Diagnostics_DebuggableAttribute_DebuggingModes__Default
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes, // Field Signature

                // System_Diagnostics_DebuggableAttribute_DebuggingModes__DisableOptimizations
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes, // Field Signature

                // System_Diagnostics_DebuggableAttribute_DebuggingModes__EnableEditAndContinue
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes, // Field Signature

                // System_Diagnostics_DebuggableAttribute_DebuggingModes__IgnoreSymbolStoreSequencePoints
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes, // Field Signature

                // System_Runtime_InteropServices_UnknownWrapper__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_UnknownWrapper,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_InteropServices_DispatchWrapper__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_DispatchWrapper,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_InteropServices_ClassInterfaceAttribute__ctorClassInterfaceType
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ClassInterfaceAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_ClassInterfaceType,

                // System_Runtime_InteropServices_CoClassAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_CoClassAttribute,                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Runtime_InteropServices_ComAwareEventInfo__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComAwareEventInfo,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_InteropServices_ComAwareEventInfo__AddEventHandler
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComAwareEventInfo,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,

                // System_Runtime_InteropServices_ComAwareEventInfo__RemoveEventHandler
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComAwareEventInfo,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,

                // System_Runtime_InteropServices_ComEventInterfaceAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComEventInterfaceAttribute,                              // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Runtime_InteropServices_ComSourceInterfacesAttribute__ctorString
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComSourceInterfacesAttribute,                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_InteropServices_ComVisibleAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComVisibleAttribute,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Runtime_InteropServices_DispIdAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_DispIdAttribute,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_InteropServices_GuidAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_GuidAttribute,                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_InteropServices_InterfaceTypeAttribute__ctorComInterfaceType
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_InterfaceTypeAttribute,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_ComInterfaceType,

                // System_Runtime_InteropServices_InterfaceTypeAttribute__ctorInt16
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_InterfaceTypeAttribute,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,

                // System_Runtime_InteropServices_Marshal__GetTypeFromCLSID
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_Marshal,                                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Guid,

                // System_Runtime_InteropServices_TypeIdentifierAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_TypeIdentifierAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_InteropServices_TypeIdentifierAttribute__ctorStringString
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_TypeIdentifierAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_InteropServices_BestFitMappingAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_BestFitMappingAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Runtime_InteropServices_DefaultParameterValueAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_DefaultParameterValueAttribute,                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_InteropServices_LCIDConversionAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_LCIDConversionAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute,                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_CallingConvention,

                // System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__AddEventHandler
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T,            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__GetOrCreateEventRegistrationTokenTable
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T,            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T,
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T,
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__InvocationList
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T,            // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__RemoveEventHandler
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T,            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,

                // System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__AddEventHandler_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal,                    // DeclaringTypeId
                1,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Func_T2,
                    2,
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Action_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__RemoveAllEventHandlers
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal,                    // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Action_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,

                // System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__RemoveEventHandler_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal,                    // DeclaringTypeId
                1,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Action_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute,                              // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // System_Runtime_CompilerServices_DecimalConstantAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    5,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,

                // System_Runtime_CompilerServices_DecimalConstantAttribute__ctorByteByteInt32Int32Int32
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    5,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_CompilerServices_ExtensionAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_ExtensionAttribute,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute,                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_CompilerServices_AccessedThroughPropertyAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AccessedThroughPropertyAttribute,                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_CompilerServices_CompilationRelaxationsAttribute__ctorInt32
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_CompilationRelaxationsAttribute,                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute,                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__WrapNonExceptionThrows
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute,                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Runtime_CompilerServices_UnsafeValueTypeAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_UnsafeValueTypeAttribute,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_CompilerServices_FixedBufferAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_FixedBufferAttribute,                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_CompilerServices_DynamicAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_DynamicAttribute,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_DynamicAttribute,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Runtime_CompilerServices_CallSite_T__Create
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_CallSite_T,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSite_T,
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,

                // System_Runtime_CompilerServices_CallSite_T__Target
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_CallSite_T,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_Runtime_CompilerServices_RuntimeHelpers__GetObjectValueObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_RuntimeHelpers__InitializeArrayArrayRuntimeFieldHandle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Array,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_RuntimeFieldHandle,

                // System_Runtime_CompilerServices_RuntimeHelpers__get_OffsetToStringData
                (byte)(MemberFlags.PropertyGet | MemberFlags.Static),                                                       // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_ExceptionServices_ExceptionDispatchInfo__Capture
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_ExceptionServices_ExceptionDispatchInfo,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_ExceptionServices_ExceptionDispatchInfo,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,

                // System_Runtime_ExceptionServices_ExceptionDispatchInfo__Throw
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_ExceptionServices_ExceptionDispatchInfo,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Security_UnverifiableCodeAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Security_UnverifiableCodeAttribute,                                              // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Security_Permissions_SecurityAction__RequestMinimum
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Security_Permissions_SecurityAction,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Security_Permissions_SecurityAction,     // Field Signature

                // System_Security_Permissions_SecurityPermissionAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Security_Permissions_SecurityPermissionAttribute,                                // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Security_Permissions_SecurityAction,

                // System_Security_Permissions_SecurityPermissionAttribute__SkipVerification
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Security_Permissions_SecurityPermissionAttribute,                                // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Activator__CreateInstance
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Activator,                                                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Activator__CreateInstance_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Activator,                                                                       // DeclaringTypeId
                1,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Threading_Interlocked__CompareExchange_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Threading_Interlocked,                                                           // DeclaringTypeId
                1,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Threading_Monitor__Enter
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Threading_Monitor,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Threading_Monitor__Enter2
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Threading_Monitor,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Threading_Monitor__Exit
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Threading_Monitor,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Threading_Thread__CurrentThread
                (byte)(MemberFlags.Property | MemberFlags.Static),                                                          // Flags
                (byte)WellKnownType.System_Threading_Thread,                                                                // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Threading_Thread,

                // System_Threading_Thread__ManagedThreadId
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Threading_Thread,                                                                // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_CSharp_RuntimeBinder_Binder__BinaryOperation
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_ExpressionType,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,

                // Microsoft_CSharp_RuntimeBinder_Binder__Convert
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // Microsoft_CSharp_RuntimeBinder_Binder__GetIndex
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,

                // Microsoft_CSharp_RuntimeBinder_Binder__GetMember
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,

                // Microsoft_CSharp_RuntimeBinder_Binder__Invoke
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,

                // Microsoft_CSharp_RuntimeBinder_Binder__InvokeConstructor
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,

                // Microsoft_CSharp_RuntimeBinder_Binder__InvokeMember
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    5,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,

                // Microsoft_CSharp_RuntimeBinder_Binder__IsEvent
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // Microsoft_CSharp_RuntimeBinder_Binder__SetIndex
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,

                // Microsoft_CSharp_RuntimeBinder_Binder__SetMember
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,

                // Microsoft_CSharp_RuntimeBinder_Binder__UnaryOperation
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_Binder,                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_ExpressionType,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,

                // Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo__Create
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfoFlags,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToByteString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToShortString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToLongString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToULongString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDateString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringByte
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDateTime
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringChar
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToByteObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToShortObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToLongObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToULongObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDateObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToGenericParameter_T_Object
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ChangeType
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // Microsoft_VisualBasic_CompilerServices_Operators__PlusObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__NegateObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__NotObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__AndObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__OrObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__XorObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__AddObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__SubtractObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__MultiplyObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__DivideObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__ExponentObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__ModObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__IntDivideObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__LeftShiftObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__RightShiftObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConcatenateObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectNotEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectNotEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateCall
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    8,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateGet
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    7,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateSet
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    6,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateSetComplex
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    8,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexGet
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSet
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSetComplex
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    5,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute,                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag,                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag__State
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag,                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,                                     // Field Signature

                // Microsoft_VisualBasic_CompilerServices_StringType__MidStmtStr
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_StringType,                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_IncompleteInitialization__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_IncompleteInitialization,                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // Microsoft_VisualBasic_Embedded__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Embedded,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // Microsoft_VisualBasic_CompilerServices_Utils__CopyArray
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Utils,                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Array,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Array,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Array,

                // Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeStringStringStringCompareMethod
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_LikeOperator,                                    // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_VisualBasic_CompareMethod,

                // Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeObjectObjectObjectCompareMethod
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_LikeOperator,                                    // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_VisualBasic_CompareMethod,

                // Microsoft_VisualBasic_CompilerServices_ProjectData__CreateProjectError
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,

                // Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError_Int32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // Microsoft_VisualBasic_CompilerServices_ProjectData__EndApp
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl,                // DeclaringTypeId
                0,                                                                                                          // Arity
                    6,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForNextCheckObj
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl,                // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_ObjectFlowControl__CheckForSyncLockOnValueType
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Versioned__CallByName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_VisualBasic_CallType,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Versioned__IsNumeric
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Versioned__SystemTypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Versioned__TypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Versioned__VbTypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_Information__IsNumeric
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Information,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_Information__SystemTypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Information,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_Information__TypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Information,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_Information__VbTypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Information,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_Interaction__CallByName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Interaction,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_VisualBasic_CallType,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_IAsyncStateMachine_MoveNext
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_CompilerServices_IAsyncStateMachine_SetStateMachine
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Create
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetException
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetResult
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitUnsafeOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Start_T
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetStateMachine
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Create
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetException
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetResult
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitUnsafeOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Start_T
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetStateMachine
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Task
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Threading_Tasks_Task,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Create
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetException
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetResult
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitUnsafeOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Start_T
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetStateMachine
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Task
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Threading_Tasks_Task_T,
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncStateMachineAttribute,                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_IteratorStateMachineAttribute,                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // Microsoft_VisualBasic_Strings__AscCharInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,

                // Microsoft_VisualBasic_Strings__AscStringInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_Strings__AscWCharInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,

                // Microsoft_VisualBasic_Strings__AscWStringInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_Strings__ChrInt32Char
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_VisualBasic_Strings__ChrWInt32Char
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Xml_Linq_XElement__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Xml_Linq_XElement,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Xml_Linq_XName,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Xml_Linq_XElement__ctor2
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Xml_Linq_XElement,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Xml_Linq_XName,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Xml_Linq_XNamespace__Get
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Xml_Linq_XNamespace,                                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Xml_Linq_XNamespace,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Windows_Forms_Application__RunForm
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Windows_Forms_Application,                                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Windows_Forms_Form,

                // System_Environment__CurrentManagedThreadId
                (byte)(MemberFlags.Property | MemberFlags.Static),                                                          // Flags
                (byte)WellKnownType.System_Environment,                                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_ComponentModel_EditorBrowsableAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ComponentModel_EditorBrowsableAttribute,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_ComponentModel_EditorBrowsableState,

                // System_Runtime_GCLatencyMode__SustainedLowLatency
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Runtime_GCLatencyMode,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_GCLatencyMode,                   // Field Signature

                // System_ValueTuple_T1__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T1,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T2__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T2,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T2__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T2,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T3__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T3,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T3__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T3,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T3__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T3,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_T4__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T4,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T4__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T4,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T4__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T4,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_T4__Item4
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T4,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,                                                        // Field Signature

                // System_ValueTuple_T5__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T5,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T5__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T5,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T5__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T5,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_T5__Item4
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T5,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,                                                        // Field Signature

                // System_ValueTuple_T5__Item5
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T5,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,                                                        // Field Signature

                // System_ValueTuple_T6__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T6,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T6__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T6,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T6__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T6,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_T6__Item4
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T6,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,                                                        // Field Signature

                // System_ValueTuple_T6__Item5
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T6,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,                                                        // Field Signature

                // System_ValueTuple_T6__Item6
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_ValueTuple_T6,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 5,                                                        // Field Signature

                // System_ValueTuple_T7__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T7 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T7__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T7 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T7__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T7 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_T7__Item4
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T7 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,                                                        // Field Signature

                // System_ValueTuple_T7__Item5
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T7 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,                                                        // Field Signature

                // System_ValueTuple_T7__Item6
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T7 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 5,                                                        // Field Signature

                // System_ValueTuple_T7__Item7
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T7 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 6,                                                        // Field Signature

                // System_ValueTuple_TRest__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_TRest - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_TRest__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_TRest - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_TRest__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_TRest - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_TRest__Item4
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_TRest - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,                                                        // Field Signature

                // System_ValueTuple_TRest__Item5
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_TRest - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,                                                        // Field Signature

                // System_ValueTuple_TRest__Item6
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_TRest - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 5,                                                        // Field Signature

                // System_ValueTuple_TRest__Item7
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_TRest - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 6,                                                        // Field Signature

                // System_ValueTuple_TRest__Rest
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_TRest - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 7,                                                        // Field Signature

                // System_ValueTuple_T1__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ValueTuple_T1,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_ValueTuple_T2__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ValueTuple_T2,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,

                // System_ValueTuple_T3__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ValueTuple_T3,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,

                 // System_ValueTuple_T4__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ValueTuple_T4,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,

                // System_ValueTuple_T_T2_T3_T4_T5__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ValueTuple_T5,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    5,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,

                // System_ValueTuple_T6__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ValueTuple_T6,                                                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    6,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,
                    (byte)SignatureTypeCode.GenericTypeParameter, 5,

                // System_ValueTuple_T7__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T7 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    7,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,
                    (byte)SignatureTypeCode.GenericTypeParameter, 5,
                    (byte)SignatureTypeCode.GenericTypeParameter, 6,

                // System_ValueTuple_TRest__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_TRest - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                          // Arity
                    8,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,
                    (byte)SignatureTypeCode.GenericTypeParameter, 5,
                    (byte)SignatureTypeCode.GenericTypeParameter, 6,
                    (byte)SignatureTypeCode.GenericTypeParameter, 7,

                // System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames
                (byte)MemberFlags.Constructor,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_TupleElementNamesAttribute // DeclaringTypeId
                                                        - WellKnownType.ExtSentinel),
                0,                                                                                                               // Arity
                    1,                                                                                                           // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_String__Format_IFormatProvider
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)SpecialType.System_String,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_IFormatProvider,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningSingleFile
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_Instrumentation - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                  // Arity
                    5,                                                                                                                              // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Guid,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningMultipleFiles
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_Instrumentation - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                  // Arity
                    5,                                                                                                                              // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Guid,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_CompilerServices_ReferenceAssemblyAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                                                      // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_ReferenceAssemblyAttribute - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                  // Arity
                    0,                                                                                                                              // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                 // System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute - WellKnownType.ExtSentinel),        // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                 // System_Runtime_CompilerServices_IsByRefLikeAttribute__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute - WellKnownType.ExtSentinel),       // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                 // System_ObsoleteAttribute__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ObsoleteAttribute - WellKnownType.ExtSentinel),                                   // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     2,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                     
                 // System_Span__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     2,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_Span__get_Item
                 (byte)(MemberFlags.PropertyGet),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_Span__get_Length
                 (byte)(MemberFlags.PropertyGet),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    0,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_ReadOnlySpan__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     2,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_ReadOnlySpan__get_Item
                 (byte)(MemberFlags.PropertyGet),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_ReadOnlySpan__get_Length
                 (byte)(MemberFlags.PropertyGet),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    0,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    
                 // System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_IsUnmanagedAttribute - WellKnownType.ExtSentinel),       // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

            };

            string[] allNames = new string[(int)WellKnownMember.Count]
            {
                "Round",                                    // System_Math__RoundDouble
                "Pow",                                      // System_Math__PowDoubleDouble
                "get_Length",                               // System_Array__get_Length
                "Empty",                                    // System_Array__Empty
                "ToBoolean",                                // System_Convert__ToBooleanDecimal
                "ToBoolean",                                // System_Convert__ToBooleanInt32
                "ToBoolean",                                // System_Convert__ToBooleanUInt32
                "ToBoolean",                                // System_Convert__ToBooleanInt64
                "ToBoolean",                                // System_Convert__ToBooleanUInt64
                "ToBoolean",                                // System_Convert__ToBooleanSingle
                "ToBoolean",                                // System_Convert__ToBooleanDouble
                "ToSByte",                                  // System_Convert__ToSByteDecimal
                "ToSByte",                                  // System_Convert__ToSByteDouble
                "ToSByte",                                  // System_Convert__ToSByteSingle
                "ToByte",                                   // System_Convert__ToByteDecimal
                "ToByte",                                   // System_Convert__ToByteDouble
                "ToByte",                                   // System_Convert__ToByteSingle
                "ToInt16",                                  // System_Convert__ToInt16Decimal
                "ToInt16",                                  // System_Convert__ToInt16Double
                "ToInt16",                                  // System_Convert__ToInt16Single
                "ToUInt16",                                 // System_Convert__ToUInt16Decimal
                "ToUInt16",                                 // System_Convert__ToUInt16Double
                "ToUInt16",                                 // System_Convert__ToUInt16Single
                "ToInt32",                                  // System_Convert__ToInt32Decimal
                "ToInt32",                                  // System_Convert__ToInt32Double
                "ToInt32",                                  // System_Convert__ToInt32Single
                "ToUInt32",                                 // System_Convert__ToUInt32Decimal
                "ToUInt32",                                 // System_Convert__ToUInt32Double
                "ToUInt32",                                 // System_Convert__ToUInt32Single
                "ToInt64",                                  // System_Convert__ToInt64Decimal
                "ToInt64",                                  // System_Convert__ToInt64Double
                "ToInt64",                                  // System_Convert__ToInt64Single
                "ToUInt64",                                 // System_Convert__ToUInt64Decimal
                "ToUInt64",                                 // System_Convert__ToUInt64Double
                "ToUInt64",                                 // System_Convert__ToUInt64Single
                "ToSingle",                                 // System_Convert__ToSingleDecimal
                "ToDouble",                                 // System_Convert__ToDoubleDecimal
                ".ctor",                                    // System_CLSCompliantAttribute__ctor
                ".ctor",                                    // System_FlagsAttribute__ctor
                ".ctor",                                    // System_Guid__ctor
                "GetTypeFromCLSID",                         // System_Type__GetTypeFromCLSID
                "GetTypeFromHandle",                        // System_Type__GetTypeFromHandle
                "Missing",                                  // System_Type__Missing
                ".ctor",                                    // System_Reflection_AssemblyKeyFileAttribute__ctor
                ".ctor",                                    // System_Reflection_AssemblyKeyNameAttribute__ctor
                "GetMethodFromHandle",                      // System_Reflection_MethodBase__GetMethodFromHandle
                "GetMethodFromHandle",                      // System_Reflection_MethodBase__GetMethodFromHandle2
                "CreateDelegate",                           // System_Reflection_MethodInfo__CreateDelegate
                "CreateDelegate",                           // System_Delegate__CreateDelegate
                "CreateDelegate",                           // System_Delegate__CreateDelegate4
                "GetFieldFromHandle",                       // System_Reflection_FieldInfo__GetFieldFromHandle
                "GetFieldFromHandle",                       // System_Reflection_FieldInfo__GetFieldFromHandle2
                "Value",                                    // System_Reflection_Missing__Value
                "Equals",                                   // System_IEquatable_T__Equals
                "Equals",                                   // System_Collections_Generic_EqualityComparer_T__Equals
                "GetHashCode",                              // System_Collections_Generic_EqualityComparer_T__GetHashCode
                "get_Default",                              // System_Collections_Generic_EqualityComparer_T__get_Default
                ".ctor",                                    // System_AttributeUsageAttribute__ctor
                "AllowMultiple",                            // System_AttributeUsageAttribute__AllowMultiple
                "Inherited",                                // System_AttributeUsageAttribute__Inherited
                ".ctor",                                    // System_ParamArrayAttribute__ctor
                ".ctor",                                    // System_STAThreadAttribute__ctor
                ".ctor",                                    // System_Reflection_DefaultMemberAttribute__ctor
                "Break",                                    // System_Diagnostics_Debugger__Break
                ".ctor",                                    // System_Diagnostics_DebuggerDisplayAttribute__ctor
                "Type",                                     // System_Diagnostics_DebuggerDisplayAttribute__Type
                ".ctor",                                    // System_Diagnostics_DebuggerNonUserCodeAttribute__ctor
                ".ctor",                                    // System_Diagnostics_DebuggerHiddenAttribute__ctor
                ".ctor",                                    // System_Diagnostics_DebuggerBrowsableAttribute__ctor
                ".ctor",                                    // System_Diagnostics_DebuggerStepThroughAttribute__ctor
                ".ctor",                                    // System_Diagnostics_DebuggableAttribute__ctorDebuggingModes
                "Default",                                  // System_Diagnostics_DebuggableAttribute_DebuggingModes__Default
                "DisableOptimizations",                     // System_Diagnostics_DebuggableAttribute_DebuggingModes__DisableOptimizations
                "EnableEditAndContinue",                    // System_Diagnostics_DebuggableAttribute_DebuggingModes__EnableEditAndContinue
                "IgnoreSymbolStoreSequencePoints",          // System_Diagnostics_DebuggableAttribute_DebuggingModes__IgnoreSymbolStoreSequencePoints
                ".ctor",                                    // System_Runtime_InteropServices_UnknownWrapper__ctor
                ".ctor",                                    // System_Runtime_InteropServices_DispatchWrapper__ctor
                ".ctor",                                    // System_Runtime_InteropServices_ClassInterfaceAttribute__ctorClassInterfaceType
                ".ctor",                                    // System_Runtime_InteropServices_CoClassAttribute__ctor
                ".ctor",                                    // System_Runtime_InteropServices_ComAwareEventInfo__ctor
                "AddEventHandler",                          // System_Runtime_InteropServices_ComAwareEventInfo__AddEventHandler
                "RemoveEventHandler",                       // System_Runtime_InteropServices_ComAwareEventInfo__RemoveEventHandler
                ".ctor",                                    // System_Runtime_InteropServices_ComEventInterfaceAttribute__ctor
                ".ctor",                                    // System_Runtime_InteropServices_ComSourceInterfacesAttribute__ctorString
                ".ctor",                                    // System_Runtime_InteropServices_ComVisibleAttribute__ctor
                ".ctor",                                    // System_Runtime_InteropServices_DispIdAttribute__ctor
                ".ctor",                                    // System_Runtime_InteropServices_GuidAttribute__ctor
                ".ctor",                                    // System_Runtime_InteropServices_InterfaceTypeAttribute__ctorComInterfaceType
                ".ctor",                                    // System_Runtime_InteropServices_InterfaceTypeAttribute__ctorInt16
                "GetTypeFromCLSID",                         // System_Runtime_InteropServices_Marshal__GetTypeFromCLSID
                ".ctor",                                    // System_Runtime_InteropServices_TypeIdentifierAttribute__ctor
                ".ctor",                                    // System_Runtime_InteropServices_TypeIdentifierAttribute__ctorStringString
                ".ctor",                                    // System_Runtime_InteropServices_BestFitMappingAttribute__ctor
                ".ctor",                                    // System_Runtime_InteropServices_DefaultParameterValueAttribute__ctor
                ".ctor",                                    // System_Runtime_InteropServices_LCIDConversionAttribute__ctor
                ".ctor",                                    // System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute__ctor
                "AddEventHandler",                          // System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__AddEventHandler
                "GetOrCreateEventRegistrationTokenTable",   // System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__GetOrCreateEventRegistrationTokenTable
                "InvocationList",                           // System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__InvocationList
                "RemoveEventHandler",                       // System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__RemoveEventHandler
                "AddEventHandler",                          // System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__AddEventHandler_T
                "RemoveAllEventHandlers",                   // System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__RemoveAllEventHandlers
                "RemoveEventHandler",                       // System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__RemoveEventHandler_T
                ".ctor",                                    // System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_DecimalConstantAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_DecimalConstantAttribute__ctorByteByteInt32Int32Int32
                ".ctor",                                    // System_Runtime_CompilerServices_ExtensionAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_AccessedThroughPropertyAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_CompilationRelaxationsAttribute__ctorInt32
                ".ctor",                                    // System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__ctor
                "WrapNonExceptionThrows",                   // System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__WrapNonExceptionThrows
                ".ctor",                                    // System_Runtime_CompilerServices_UnsafeValueTypeAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_FixedBufferAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_DynamicAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags
                "Create",                                   // System_Runtime_CompilerServices_CallSite_T__Create
                "Target",                                   // System_Runtime_CompilerServices_CallSite_T__Target
                "GetObjectValue",                           // System_Runtime_CompilerServices_RuntimeHelpers__GetObjectValueObject
                "InitializeArray",                          // System_Runtime_CompilerServices_RuntimeHelpers__InitializeArrayArrayRuntimeFieldHandle
                "get_OffsetToStringData",                   // System_Runtime_CompilerServices_RuntimeHelpers__get_OffsetToStringData
                "Capture",                                  // System_Runtime_ExceptionServices_ExceptionDispatchInfo__Capture
                "Throw",                                    // System_Runtime_ExceptionServices_ExceptionDispatchInfo__Throw
                ".ctor",                                    // System_Security_UnverifiableCodeAttribute__ctor
                "RequestMinimum",                           // System_Security_Permissions_SecurityAction__RequestMinimum
                ".ctor",                                    // System_Security_Permissions_SecurityPermissionAttribute__ctor
                "SkipVerification",                         // System_Security_Permissions_SecurityPermissionAttribute__SkipVerification
                "CreateInstance",                           // System_Activator__CreateInstance
                "CreateInstance",                           // System_Activator__CreateInstance_T
                "CompareExchange",                          // System_Threading_Interlocked__CompareExchange_T
                "Enter",                                    // System_Threading_Monitor__Enter
                "Enter",                                    // System_Threading_Monitor__Enter2
                "Exit",                                     // System_Threading_Monitor__Exit
                "CurrentThread",                            // System_Threading_Thread__CurrentThread
                "ManagedThreadId",                          // System_Threading_Thread__ManagedThreadId
                "BinaryOperation",                          // Microsoft_CSharp_RuntimeBinder_Binder__BinaryOperation
                "Convert",                                  // Microsoft_CSharp_RuntimeBinder_Binder__Convert
                "GetIndex",                                 // Microsoft_CSharp_RuntimeBinder_Binder__GetIndex
                "GetMember",                                // Microsoft_CSharp_RuntimeBinder_Binder__GetMember
                "Invoke",                                   // Microsoft_CSharp_RuntimeBinder_Binder__Invoke
                "InvokeConstructor",                        // Microsoft_CSharp_RuntimeBinder_Binder__InvokeConstructor
                "InvokeMember",                             // Microsoft_CSharp_RuntimeBinder_Binder__InvokeMember
                "IsEvent",                                  // Microsoft_CSharp_RuntimeBinder_Binder__IsEvent
                "SetIndex",                                 // Microsoft_CSharp_RuntimeBinder_Binder__SetIndex
                "SetMember",                                // Microsoft_CSharp_RuntimeBinder_Binder__SetMember
                "UnaryOperation",                           // Microsoft_CSharp_RuntimeBinder_Binder__UnaryOperation
                "Create",                                   // Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo__Create
                "ToDecimal",                                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalBoolean
                "ToBoolean",                                // Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanString
                "ToSByte",                                  // Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteString
                "ToByte",                                   // Microsoft_VisualBasic_CompilerServices_Conversions__ToByteString
                "ToShort",                                  // Microsoft_VisualBasic_CompilerServices_Conversions__ToShortString
                "ToUShort",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortString
                "ToInteger",                                // Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerString
                "ToUInteger",                               // Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerString
                "ToLong",                                   // Microsoft_VisualBasic_CompilerServices_Conversions__ToLongString
                "ToULong",                                  // Microsoft_VisualBasic_CompilerServices_Conversions__ToULongString
                "ToSingle",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleString
                "ToDouble",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleString
                "ToDecimal",                                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalString
                "ToDate",                                   // Microsoft_VisualBasic_CompilerServices_Conversions__ToDateString
                "ToChar",                                   // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharString
                "ToCharArrayRankOne",                       // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneString
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringBoolean
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt32
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringByte
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt32
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt64
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt64
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringSingle
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDouble
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDecimal
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDateTime
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringChar
                "ToString",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringObject
                "ToBoolean",                                // Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanObject
                "ToSByte",                                  // Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteObject
                "ToByte",                                   // Microsoft_VisualBasic_CompilerServices_Conversions__ToByteObject
                "ToShort",                                  // Microsoft_VisualBasic_CompilerServices_Conversions__ToShortObject
                "ToUShort",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortObject
                "ToInteger",                                // Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerObject
                "ToUInteger",                               // Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerObject
                "ToLong",                                   // Microsoft_VisualBasic_CompilerServices_Conversions__ToLongObject
                "ToULong",                                  // Microsoft_VisualBasic_CompilerServices_Conversions__ToULongObject
                "ToSingle",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleObject
                "ToDouble",                                 // Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleObject
                "ToDecimal",                                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalObject
                "ToDate",                                   // Microsoft_VisualBasic_CompilerServices_Conversions__ToDateObject
                "ToChar",                                   // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharObject
                "ToCharArrayRankOne",                       // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneObject
                "ToGenericParameter",                       // Microsoft_VisualBasic_CompilerServices_Conversions__ToGenericParameter_T_Object
                "ChangeType",                               // Microsoft_VisualBasic_CompilerServices_Conversions__ChangeType
                "PlusObject",                               // Microsoft_VisualBasic_CompilerServices_Operators__PlusObjectObject
                "NegateObject",                             // Microsoft_VisualBasic_CompilerServices_Operators__NegateObjectObject
                "NotObject",                                // Microsoft_VisualBasic_CompilerServices_Operators__NotObjectObject
                "AndObject",                                // Microsoft_VisualBasic_CompilerServices_Operators__AndObjectObjectObject
                "OrObject",                                 // Microsoft_VisualBasic_CompilerServices_Operators__OrObjectObjectObject
                "XorObject",                                // Microsoft_VisualBasic_CompilerServices_Operators__XorObjectObjectObject
                "AddObject",                                // Microsoft_VisualBasic_CompilerServices_Operators__AddObjectObjectObject
                "SubtractObject",                           // Microsoft_VisualBasic_CompilerServices_Operators__SubtractObjectObjectObject
                "MultiplyObject",                           // Microsoft_VisualBasic_CompilerServices_Operators__MultiplyObjectObjectObject
                "DivideObject",                             // Microsoft_VisualBasic_CompilerServices_Operators__DivideObjectObjectObject
                "ExponentObject",                           // Microsoft_VisualBasic_CompilerServices_Operators__ExponentObjectObjectObject
                "ModObject",                                // Microsoft_VisualBasic_CompilerServices_Operators__ModObjectObjectObject
                "IntDivideObject",                          // Microsoft_VisualBasic_CompilerServices_Operators__IntDivideObjectObjectObject
                "LeftShiftObject",                          // Microsoft_VisualBasic_CompilerServices_Operators__LeftShiftObjectObjectObject
                "RightShiftObject",                         // Microsoft_VisualBasic_CompilerServices_Operators__RightShiftObjectObjectObject
                "ConcatenateObject",                        // Microsoft_VisualBasic_CompilerServices_Operators__ConcatenateObjectObjectObject
                "CompareObjectEqual",                       // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectEqualObjectObjectBoolean
                "CompareObjectNotEqual",                    // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectNotEqualObjectObjectBoolean
                "CompareObjectLess",                        // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessObjectObjectBoolean
                "CompareObjectLessEqual",                   // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessEqualObjectObjectBoolean
                "CompareObjectGreaterEqual",                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterEqualObjectObjectBoolean
                "CompareObjectGreater",                     // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterObjectObjectBoolean
                "ConditionalCompareObjectEqual",            // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectEqualObjectObjectBoolean
                "ConditionalCompareObjectNotEqual",         // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectNotEqualObjectObjectBoolean
                "ConditionalCompareObjectLess",             // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessObjectObjectBoolean
                "ConditionalCompareObjectLessEqual",        // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessEqualObjectObjectBoolean
                "ConditionalCompareObjectGreaterEqual",     // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterEqualObjectObjectBoolean
                "ConditionalCompareObjectGreater",          // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterObjectObjectBoolean
                "CompareString",                            // Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean
                "CompareString",                            // Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean
                "LateCall",                                 // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateCall
                "LateGet",                                  // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateGet
                "LateSet",                                  // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateSet
                "LateSetComplex",                           // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateSetComplex
                "LateIndexGet",                             // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexGet
                "LateIndexSet",                             // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSet
                "LateIndexSetComplex",                      // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSetComplex
                ".ctor",                                    // Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute__ctor
                ".ctor",                                    // Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag__ctor
                "State",                                    // Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag__State
                "MidStmtStr",                               // Microsoft_VisualBasic_CompilerServices_StringType__MidStmtStr
                ".ctor",                                    // Microsoft_VisualBasic_CompilerServices_IncompleteInitialization__ctor
                ".ctor",                                    // Microsoft_VisualBasic_Embedded__ctor
                "CopyArray",                                // Microsoft_VisualBasic_CompilerServices_Utils__CopyArray
                "LikeString",                               // Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeStringStringStringCompareMethod
                "LikeObject",                               // Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeObjectObjectObjectCompareMethod
                "CreateProjectError",                       // Microsoft_VisualBasic_CompilerServices_ProjectData__CreateProjectError
                "SetProjectError",                          // Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError
                "SetProjectError",                          // Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError_Int32
                "ClearProjectError",                        // Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError
                "EndApp",                                   // Microsoft_VisualBasic_CompilerServices_ProjectData__EndApp
                "ForLoopInitObj",                           // Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj
                "ForNextCheckObj",                          // Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForNextCheckObj
                "CheckForSyncLockOnValueType",              // Microsoft_VisualBasic_CompilerServices_ObjectFlowControl__CheckForSyncLockOnValueType
                "CallByName",                               // Microsoft_VisualBasic_CompilerServices_Versioned__CallByName
                "IsNumeric",                                // Microsoft_VisualBasic_CompilerServices_Versioned__IsNumeric
                "SystemTypeName",                           // Microsoft_VisualBasic_CompilerServices_Versioned__SystemTypeName
                "TypeName",                                 // Microsoft_VisualBasic_CompilerServices_Versioned__TypeName
                "VbTypeName",                               // Microsoft_VisualBasic_CompilerServices_Versioned__VbTypeName
                "IsNumeric",                                // Microsoft_VisualBasic_Information__IsNumeric
                "SystemTypeName",                           // Microsoft_VisualBasic_Information__SystemTypeName
                "TypeName",                                 // Microsoft_VisualBasic_Information__TypeName
                "VbTypeName",                               // Microsoft_VisualBasic_Information__VbTypeName
                "CallByName",                               // Microsoft_VisualBasic_Interaction__CallByName
                "MoveNext",                                 // System_Runtime_CompilerServices_IAsyncStateMachine_MoveNext
                "SetStateMachine",                          // System_Runtime_CompilerServices_IAsyncStateMachine_SetStateMachine
                "Create",                                   // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Create
                "SetException",                             // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetException
                "SetResult",                                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetResult
                "AwaitOnCompleted",                         // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitOnCompleted
                "AwaitUnsafeOnCompleted",                   // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitUnsafeOnCompleted
                "Start",                                    // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Start_T
                "SetStateMachine",                          // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetStateMachine
                "Create",                                   // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Create
                "SetException",                             // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetException
                "SetResult",                                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetResult
                "AwaitOnCompleted",                         // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitOnCompleted
                "AwaitUnsafeOnCompleted",                   // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitUnsafeOnCompleted
                "Start",                                    // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Start_T
                "SetStateMachine",                          // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetStateMachine
                "Task",                                     // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Task
                "Create",                                   // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Create
                "SetException",                             // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetException
                "SetResult",                                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetResult
                "AwaitOnCompleted",                         // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitOnCompleted
                "AwaitUnsafeOnCompleted",                   // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitUnsafeOnCompleted
                "Start",                                    // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Start_T
                "SetStateMachine",                          // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetStateMachine
                "Task",                                     // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Task
                ".ctor",                                    // System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor
                "Asc",                                      // Microsoft_VisualBasic_Strings__AscCharInt32
                "Asc",                                      // Microsoft_VisualBasic_Strings__AscStringInt32
                "AscW",                                     // Microsoft_VisualBasic_Strings__AscWCharInt32
                "AscW",                                     // Microsoft_VisualBasic_Strings__AscWStringInt32
                "Chr",                                      // Microsoft_VisualBasic_Strings__ChrInt32Char
                "ChrW",                                     // Microsoft_VisualBasic_Strings__ChrWInt32Char
                ".ctor",                                    // System_Xml_Linq_XElement__ctor
                ".ctor",                                    // System_Xml_Linq_XElement__ctor2
                "Get",                                      // System_Xml_Linq_XNamespace__Get
                "Run",                                      // System_Windows_Forms_Application__RunForm
                "CurrentManagedThreadId",                   // System_Environment__CurrentManagedThreadId
                ".ctor",                                    // System_ComponentModel_EditorBrowsableAttribute__ctor
                "SustainedLowLatency",                      // System_Runtime_GCLatencyMode__SustainedLowLatency

                "Item1",                                    // System_ValueTuple_T1__Item1

                "Item1",                                    // System_ValueTuple_T2__Item1
                "Item2",                                    // System_ValueTuple_T2__Item2

                "Item1",                                    // System_ValueTuple_T3__Item1
                "Item2",                                    // System_ValueTuple_T3__Item2
                "Item3",                                    // System_ValueTuple_T3__Item3

                "Item1",                                    // System_ValueTuple_T4__Item1
                "Item2",                                    // System_ValueTuple_T4__Item2
                "Item3",                                    // System_ValueTuple_T4__Item3
                "Item4",                                    // System_ValueTuple_T4__Item4

                "Item1",                                    // System_ValueTuple_T5__Item1
                "Item2",                                    // System_ValueTuple_T5__Item2
                "Item3",                                    // System_ValueTuple_T5__Item3
                "Item4",                                    // System_ValueTuple_T5__Item4
                "Item5",                                    // System_ValueTuple_T5__Item5

                "Item1",                                    // System_ValueTuple_T6__Item1
                "Item2",                                    // System_ValueTuple_T6__Item2
                "Item3",                                    // System_ValueTuple_T6__Item3
                "Item4",                                    // System_ValueTuple_T6__Item4
                "Item5",                                    // System_ValueTuple_T6__Item5
                "Item6",                                    // System_ValueTuple_T6__Item6

                "Item1",                                    // System_ValueTuple_T7__Item1
                "Item2",                                    // System_ValueTuple_T7__Item2
                "Item3",                                    // System_ValueTuple_T7__Item3
                "Item4",                                    // System_ValueTuple_T7__Item4
                "Item5",                                    // System_ValueTuple_T7__Item5
                "Item6",                                    // System_ValueTuple_T7__Item6
                "Item7",                                    // System_ValueTuple_T7__Item7

                "Item1",                                    // System_ValueTuple_TRest__Item1
                "Item2",                                    // System_ValueTuple_TRest__Item2
                "Item3",                                    // System_ValueTuple_TRest__Item3
                "Item4",                                    // System_ValueTuple_TRest__Item4
                "Item5",                                    // System_ValueTuple_TRest__Item5
                "Item6",                                    // System_ValueTuple_TRest__Item6
                "Item7",                                    // System_ValueTuple_TRest__Item7
                "Rest",                                     // System_ValueTuple_TRest__Rest

                ".ctor",                                    // System_ValueTuple_T1__ctor
                ".ctor",                                    // System_ValueTuple_T2__ctor
                ".ctor",                                    // System_ValueTuple_T3__ctor
                ".ctor",                                    // System_ValueTuple_T4__ctor
                ".ctor",                                    // System_ValueTuple_T5__ctor
                ".ctor",                                    // System_ValueTuple_T6__ctor
                ".ctor",                                    // System_ValueTuple_T7__ctor
                ".ctor",                                    // System_ValueTuple_TRest__ctor

                ".ctor",                                    // System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames

                "Format",                                   // System_String__Format_IFormatProvider
                "CreatePayload",                            // Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningSingleFile
                "CreatePayload",                            // Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningMultipleFiles

                ".ctor",                                    // System_Runtime_CompilerServices_ReferenceAssemblyAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_IsByRefLikeAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_ObsoleteAttribute__ctor
                ".ctor",                                    // System_Span__ctor
                "get_Item",                                 // System_Span__get_Item
                "get_Length",                               // System_Span__get_Length
                ".ctor",                                    // System_ReadOnlySpan__ctor
                "get_Item",                                 // System_ReadOnlySpan__get_Item
                "get_Length",                               // System_ReadOnlySpan__get_Length
                ".ctor",                                    // System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor
            };

            s_descriptors = MemberDescriptor.InitializeFromStream(new System.IO.MemoryStream(initializationBytes, writable: false), allNames);
        }

        public static MemberDescriptor GetDescriptor(WellKnownMember member)
        {
            return s_descriptors[(int)member];
        }

        /// <summary>
        /// This function defines whether an attribute is optional or not.
        /// </summary>
        /// <param name="attributeMember">The attribute member.</param>
        internal static bool IsSynthesizedAttributeOptional(WellKnownMember attributeMember)
        {
            switch (attributeMember)
            {
                case WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor:
                case WellKnownMember.System_Diagnostics_DebuggableAttribute__ctorDebuggingModes:
                case WellKnownMember.System_Diagnostics_DebuggerBrowsableAttribute__ctor:
                case WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor:
                case WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor:
                case WellKnownMember.System_Diagnostics_DebuggerStepThroughAttribute__ctor:
                case WellKnownMember.System_Diagnostics_DebuggerNonUserCodeAttribute__ctor:
                case WellKnownMember.System_STAThreadAttribute__ctor:
                case WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor:
                case WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor:
                    return true;

                default:
                    return false;
            }
        }
    }
}
