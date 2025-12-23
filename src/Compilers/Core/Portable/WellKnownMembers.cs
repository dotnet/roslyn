// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.RuntimeMembers;

namespace Microsoft.CodeAnalysis
{
    internal static class WellKnownMembers
    {
        private static readonly ImmutableArray<MemberDescriptor> s_descriptors;

        static WellKnownMembers()
        {
            byte[] initializationBytes = new byte[]
            {
                // System_Math__RoundDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Math,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Math__PowDoubleDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Math,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToBooleanDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToBooleanInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Convert__ToBooleanUInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,

                // System_Convert__ToBooleanInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // System_Convert__ToBooleanUInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,

                // System_Convert__ToBooleanSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToBooleanDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToSByteDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToSByteDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToSByteSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToByteDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToByteDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToByteSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToInt16Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToInt16Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToInt16Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToUInt16Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToUInt16Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToUInt16Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToInt32Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToInt32Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToInt32Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToUInt32Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToUInt32Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToUInt32Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToInt64Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToInt64Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToInt64Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToUInt64Decimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToUInt64Double
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Convert__ToUInt64Single
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // System_Convert__ToSingleDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_Convert__ToDoubleDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Convert,                                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // System_CLSCompliantAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_CLSCompliantAttribute,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_FlagsAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_FlagsAttribute,                                                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Guid__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Guid,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Type__GetTypeFromCLSID
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Type,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Guid,

                // System_Type__GetTypeFromHandle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Type,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_RuntimeTypeHandle,

                // System_Type__Missing
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Type,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,                                    // Field Signature

                // System_Type__op_Equality
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Type,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Reflection_AssemblyKeyFileAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Reflection_AssemblyKeyFileAttribute,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodBase, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_RuntimeMethodHandle,

                // System_Reflection_MethodBase__GetMethodFromHandle2
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Reflection_MethodBase,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodBase, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_RuntimeMethodHandle,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_RuntimeTypeHandle,

                // System_Reflection_MethodInfo__CreateDelegate
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Reflection_MethodInfo,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Reflection_FieldInfo__GetFieldFromHandle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Reflection_FieldInfo,                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_FieldInfo, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_RuntimeFieldHandle,

                // System_Reflection_FieldInfo__GetFieldFromHandle2
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Reflection_FieldInfo,                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_FieldInfo, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_RuntimeFieldHandle,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_RuntimeTypeHandle,

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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_IEqualityComparer_T__Equals
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Collections_Generic_IEqualityComparer_T - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_EqualityComparer_T__Equals
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_Generic_EqualityComparer_T,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_EqualityComparer_T__GetHashCode
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_Generic_EqualityComparer_T,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_EqualityComparer_T__get_Default
                (byte)(MemberFlags.PropertyGet | MemberFlags.Static),                                                       // Flags
                (byte)WellKnownType.System_Collections_Generic_EqualityComparer_T,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Collections_Generic_EqualityComparer_T,  // Return Type

                // System_AttributeUsageAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_AttributeUsageAttribute,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, 0,

                // System_AttributeUsageAttribute__AllowMultiple
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_AttributeUsageAttribute,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type

                // System_AttributeUsageAttribute__Inherited
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_AttributeUsageAttribute,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type

                // System_ParamArrayAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ParamArrayAttribute,                                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_STAThreadAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_STAThreadAttribute,                                                              // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Reflection_DefaultMemberAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Reflection_DefaultMemberAttribute,                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Diagnostics_Debugger__Break
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Diagnostics_Debugger,                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Diagnostics_DebuggerDisplayAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerDisplayAttribute,                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Diagnostics_DebuggerDisplayAttribute__Type
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerDisplayAttribute,                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type

                // System_Diagnostics_DebuggerNonUserCodeAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerNonUserCodeAttribute,                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Diagnostics_DebuggerHiddenAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerHiddenAttribute,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Diagnostics_DebuggerBrowsableAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerBrowsableAttribute,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Diagnostics_DebuggerBrowsableState,

                // System_Diagnostics_DebuggerStepThroughAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggerStepThroughAttribute,                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Diagnostics_DebuggableAttribute__ctorDebuggingModes
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Diagnostics_DebuggableAttribute,                                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_InteropServices_DispatchWrapper__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_DispatchWrapper,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_InteropServices_ClassInterfaceAttribute__ctorClassInterfaceType
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ClassInterfaceAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_ClassInterfaceType,

                // System_Runtime_InteropServices_CoClassAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_CoClassAttribute,                                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Runtime_InteropServices_ComAwareEventInfo__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComAwareEventInfo,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_InteropServices_ComAwareEventInfo__AddEventHandler
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComAwareEventInfo,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,

                // System_Runtime_InteropServices_ComAwareEventInfo__RemoveEventHandler
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComAwareEventInfo,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Delegate,

                // System_Runtime_InteropServices_ComEventInterfaceAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComEventInterfaceAttribute,                              // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Runtime_InteropServices_ComSourceInterfacesAttribute__ctorString
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComSourceInterfacesAttribute,                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_InteropServices_ComVisibleAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_ComVisibleAttribute,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Runtime_InteropServices_DispIdAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_DispIdAttribute,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_InteropServices_GuidAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_GuidAttribute,                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_InteropServices_InterfaceTypeAttribute__ctorComInterfaceType
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_InterfaceTypeAttribute,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_ComInterfaceType,

                // System_Runtime_InteropServices_InterfaceTypeAttribute__ctorInt16
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_InterfaceTypeAttribute,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,

                // System_Runtime_InteropServices_Marshal__GetTypeFromCLSID
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_Marshal,                                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Guid,

                // System_Runtime_InteropServices_TypeIdentifierAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_TypeIdentifierAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_InteropServices_TypeIdentifierAttribute__ctorStringString
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_TypeIdentifierAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_InteropServices_BestFitMappingAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_BestFitMappingAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Runtime_InteropServices_DefaultParameterValueAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_DefaultParameterValueAttribute,                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_InteropServices_LCIDConversionAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_LCIDConversionAttribute,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute,                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_CallingConvention,

                // System_Runtime_InteropServices_MemoryMarshal__CreateSpan
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_InteropServices_MemoryMarshal - WellKnownType.ExtSentinel), // DeclaringTypeId
                1,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),  // Return Type
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_InteropServices_MemoryMarshal - WellKnownType.ExtSentinel), // DeclaringTypeId
                1,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),  // Return Type
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

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
                    (byte)SignatureTypeCode.GenericTypeInstance, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,

                // System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__AddEventHandler_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal,                    // DeclaringTypeId
                1,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // System_Runtime_CompilerServices_DecimalConstantAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    5,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute,                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_AccessedThroughPropertyAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AccessedThroughPropertyAttribute,                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_CompilerServices_CompilationRelaxationsAttribute__ctorInt32
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_CompilationRelaxationsAttribute,                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute,                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__WrapNonExceptionThrows
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute,                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type

                // System_Runtime_CompilerServices_UnsafeValueTypeAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_UnsafeValueTypeAttribute,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_FixedBufferAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_FixedBufferAttribute,                                   // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_CompilerServices_DynamicAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_DynamicAttribute,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_DynamicAttribute,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Runtime_CompilerServices_CallSite_T__Create
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_CallSite_T,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSite_T,
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_CallSiteBinder,

                // System_Runtime_CompilerServices_CallSite_T__Target
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_CallSite_T,                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,                                         // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,                                                            // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_RuntimeFieldHandle,

                // System_Runtime_CompilerServices_RuntimeHelpers__GetObjectValueObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_RuntimeHelpers__InitializeArrayArrayRuntimeFieldHandle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Array,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_RuntimeFieldHandle,

                // System_Runtime_CompilerServices_RuntimeHelpers__get_OffsetToStringData
                (byte)(MemberFlags.PropertyGet | MemberFlags.Static),                                                       // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type

                // System_Runtime_CompilerServices_RuntimeHelpers__GetSubArray_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,                                         // DeclaringTypeId
                1,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericMethodParameter, 0, // Return type
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),

                // System_Runtime_CompilerServices_RuntimeHelpers__EnsureSufficientExecutionStack
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_RuntimeHelpers,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_Unsafe__Add_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_Unsafe - WellKnownType.ExtSentinel), // DeclaringTypeId
                1,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,                 // Return type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_CompilerServices_Unsafe__As_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_Unsafe - WellKnownType.ExtSentinel), // DeclaringTypeId
                2,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 1,                 // Return type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_CompilerServices_Unsafe__AsRef_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_Unsafe - WellKnownType.ExtSentinel), // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,                 // Return type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,

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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Security_UnverifiableCodeAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Security_UnverifiableCodeAttribute,                                              // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Security_Permissions_SecurityAction,

                // System_Security_Permissions_SecurityPermissionAttribute__SkipVerification
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Security_Permissions_SecurityPermissionAttribute,                                // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type

                // System_Activator__CreateInstance
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Activator,                                                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Activator__CreateInstance_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Activator,                                                                       // DeclaringTypeId
                1,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericMethodParameter, 0, // Return Type

                // System_Threading_Interlocked__CompareExchange
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Threading_Interlocked,                                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Threading_Interlocked__CompareExchange_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Threading_Interlocked,                                                           // DeclaringTypeId
                1,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericMethodParameter, 0, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Threading_Monitor__Enter
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Threading_Monitor,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Threading_Monitor__Enter2
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Threading_Monitor,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Threading_Monitor__Exit
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Threading_Monitor,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Threading_Thread__CurrentThread
                (byte)(MemberFlags.Property | MemberFlags.Static),                                                          // Flags
                (byte)WellKnownType.System_Threading_Thread,                                                                // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Threading_Thread, // Return Type

                // System_Threading_Thread__ManagedThreadId
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Threading_Thread,                                                                // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type

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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToByteString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToShortString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToLongString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToULongString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDateString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharString
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringByte
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt64
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDecimal
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDateTime
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringChar
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToStringObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_SByte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToByteObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToShortObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToLongObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToULongObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToDateObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_DateTime, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ToCharObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char, // Return Type
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
                    (byte)SignatureTypeCode.GenericMethodParameter, 0, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Conversions__ChangeType
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // Microsoft_VisualBasic_CompilerServices_Operators__PlusObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__NegateObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__NotObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__AndObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__OrObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__XorObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__AddObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__SubtractObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__MultiplyObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__DivideObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__ExponentObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__ModObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__IntDivideObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__LeftShiftObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__RightShiftObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConcatenateObjectObjectObject
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectNotEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectNotEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterEqualObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterObjectObjectBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Operators,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSet
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSetComplex
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding,                                  // DeclaringTypeId
                0,                                                                                                          // Arity
                    5,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag,                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_IncompleteInitialization__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_IncompleteInitialization,                        // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // Microsoft_VisualBasic_Embedded__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Embedded,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // Microsoft_VisualBasic_CompilerServices_Utils__CopyArray
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Utils,                                           // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Array, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Array,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Array,

                // Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeStringStringStringCompareMethod
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_LikeOperator,                                    // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_VisualBasic_CompareMethod,

                // Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeObjectObjectObjectCompareMethod
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_LikeOperator,                                    // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_VisualBasic_CompareMethod,

                // Microsoft_VisualBasic_CompilerServices_ProjectData__CreateProjectError
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,

                // Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError_Int32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // Microsoft_VisualBasic_CompilerServices_ProjectData__EndApp
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl,                // DeclaringTypeId
                0,                                                                                                          // Arity
                    6,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_ObjectFlowControl__CheckForSyncLockOnValueType
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Versioned__CallByName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_VisualBasic_CallType,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Versioned__IsNumeric
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Versioned__SystemTypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_CompilerServices_Versioned__TypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_CompilerServices_Versioned__VbTypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned,                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_Information__IsNumeric
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Information,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_Information__SystemTypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Information,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_Information__TypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Information,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // Microsoft_VisualBasic_Information__VbTypeName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Information,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_Interaction__CallByName
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Interaction,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.Microsoft_VisualBasic_CallType,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_IAsyncStateMachine_MoveNext
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_IAsyncStateMachine_SetStateMachine
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetResult
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__AwaitUnsafeOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__Start_T
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_CompilerServices_AsyncVoidMethodBuilder__SetStateMachine
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetResult
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__AwaitUnsafeOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Start_T
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__SetStateMachine
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Task
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder,                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Threading_Tasks_Task, // Return Type

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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetResult
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__AwaitUnsafeOnCompleted
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                2,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Start_T
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__SetStateMachine
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine,

                // System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T__Task
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Threading_Tasks_Task_T,
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_AsyncStateMachineAttribute,                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Runtime_CompilerServices_IteratorStateMachineAttribute,                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // Microsoft_VisualBasic_Strings__AscCharInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,

                // Microsoft_VisualBasic_Strings__AscStringInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_VisualBasic_Strings__AscWCharInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,

                // Microsoft_VisualBasic_Strings__AscWStringInt32
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.Microsoft_VisualBasic_Strings,                                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Xml_Linq_XElement__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Xml_Linq_XElement,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Xml_Linq_XName,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Xml_Linq_XElement__ctor2
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Xml_Linq_XElement,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Xml_Linq_XName,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Xml_Linq_XNamespace__Get
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Xml_Linq_XNamespace,                                                             // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Xml_Linq_XNamespace, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Windows_Forms_Application__RunForm
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Windows_Forms_Application - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Windows_Forms_Form - WellKnownType.ExtSentinel),

                // System_Environment__CurrentManagedThreadId
                (byte)(MemberFlags.Property | MemberFlags.Static),                                                          // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Environment - WellKnownType.ExtSentinel),      // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type

                // System_ComponentModel_EditorBrowsableAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_ComponentModel_EditorBrowsableAttribute,                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_ComponentModel_EditorBrowsableState,

                // System_Runtime_GCLatencyMode__SustainedLowLatency
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_GCLatencyMode - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_GCLatencyMode - WellKnownType.ExtSentinel), // Field Signature

                // System_ValueTuple_T1__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T1 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T2__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T2 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T2__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T2 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T3__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T3 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T3__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T3 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T3__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T3 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_T4__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T4 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T4__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T4 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T4__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T4 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_T4__Item4
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T4 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,                                                        // Field Signature

                // System_ValueTuple_T5__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T5 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T5__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T5 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T5__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T5 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_T5__Item4
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T5 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,                                                        // Field Signature

                // System_ValueTuple_T5__Item5
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T5 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,                                                        // Field Signature

                // System_ValueTuple_T6__Item1
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T6 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,                                                        // Field Signature

                // System_ValueTuple_T6__Item2
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T6 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,                                                        // Field Signature

                // System_ValueTuple_T6__Item3
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T6 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,                                                        // Field Signature

                // System_ValueTuple_T6__Item4
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T6 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,                                                        // Field Signature

                // System_ValueTuple_T6__Item5
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T6 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,                                                        // Field Signature

                // System_ValueTuple_T6__Item6
                (byte)MemberFlags.Field,                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T6 - WellKnownType.ExtSentinel),    // DeclaringTypeId
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
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T1 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_ValueTuple_T2__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T2 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,

                // System_ValueTuple_T3__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T3 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,

                 // System_ValueTuple_T4__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T4 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,

                // System_ValueTuple_T_T2_T3_T4_T5__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T5 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    5,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeParameter, 1,
                    (byte)SignatureTypeCode.GenericTypeParameter, 2,
                    (byte)SignatureTypeCode.GenericTypeParameter, 3,
                    (byte)SignatureTypeCode.GenericTypeParameter, 4,

                // System_ValueTuple_T6__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ValueTuple_T6 - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    6,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
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
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningSingleFile
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_Instrumentation - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                  // Arity
                    5,                                                                                                                              // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
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
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Guid,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogMethodEntry
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                         // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                       // Arity
                    1,                                                                                                                                   // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),    // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,      // Method id

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLambdaEntry
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                         // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                       // Arity
                    2,                                                                                                                                   // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),    // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,      // Method id
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,      // Lambda id

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogStateMachineMethodEntry
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                         // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                       // Arity
                    2,                                                                                                                                   // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),    // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,      // Method id
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,     // State machine instance id

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogStateMachineLambdaEntry
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                         // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                       // Arity
                    3,                                                                                                                                   // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),    // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,      // Method id
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,      // Lambda id
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,     // State machine instance id

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogReturn
                (byte)MemberFlags.Method,                                                                                         // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                       // Arity
                    0,                                                                                                                                   // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,    // Return Type

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__GetNewStateMachineInstanceId
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                         // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                       // Arity
                    0,                                                                                                                                   // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64,    // Return Type

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreBoolean
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,    // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreByte
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,  // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,  // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt16
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt32
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt64
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreSingle
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreDouble
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreDecimal
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreString
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreObject
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStorePointer
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUnmanaged
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    3,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,                      // Return Type
                    (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Value address
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,                     // Size
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,                     // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreParameterAlias
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,                    // Source parameter index
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,                     // Target local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreBoolean
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,// Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreByte
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,  // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,  // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUInt16
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,    // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt16,  // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,   // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUInt32
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,    // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt32,  // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,   // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUInt64
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_UInt64, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreSingle
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreDouble
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreDecimal
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Decimal, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreString
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreObject
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStorePointer
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Value
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Local index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUnmanaged
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    3,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,                       // Return Type
                    (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Value address
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,                     // Size
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,                     // Parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreParameterAlias
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Source parameter index
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,  // Target parameter index

                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreLocalAlias
                (byte)MemberFlags.Method,                                                                                                             // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                    // Arity
                    2,                                                                                                                                // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,   // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,                     // Source local index
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,                     // Target local index

                // System_Runtime_CompilerServices_NullableAttribute__ctorByte
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_NullableAttribute - WellKnownType.ExtSentinel),                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,

                // System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_NullableAttribute - WellKnownType.ExtSentinel),                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,
                    
                // System_Runtime_CompilerServices_NullableContextAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_NullableContextAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,

                    
                // System_Runtime_CompilerServices_NullablePublicOnlyAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_NullablePublicOnlyAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Runtime_CompilerServices_ReferenceAssemblyAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                                                                  // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_ReferenceAssemblyAttribute - WellKnownType.ExtSentinel),  // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    0,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                 // System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute - WellKnownType.ExtSentinel),        // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                
                 // System_Runtime_CompilerServices_RequiresLocationAttribute__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_RequiresLocationAttribute - WellKnownType.ExtSentinel),  // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                 // System_Runtime_CompilerServices_IsByRefLikeAttribute__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute - WellKnownType.ExtSentinel),       // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                 // System_ObsoleteAttribute__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ObsoleteAttribute - WellKnownType.ExtSentinel),                                   // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     2,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                     
                 // System_Span_T__ctor_Pointer
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     2,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                     (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                     
                 // System_Span_T__ctor_Array
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     1,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                     (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericTypeParameter, 0,
                     
                 // System_Span_T__ctor_ref_T
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     1,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                     (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericTypeParameter, 0,

                 // System_Span_T__get_Item
                 (byte)(MemberFlags.PropertyGet),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericTypeParameter, 0, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_Span_T__get_Length
                 (byte)(MemberFlags.PropertyGet),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    0,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type

                 // System_Span_T__Slice_Int_Int
                 (byte)(MemberFlags.Method),                                                                                                                    // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                              // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    2,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_Span_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, (byte)0,              // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_ReadOnlySpan_T__ctor_Pointer
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     2,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                     (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_ReadOnlySpan_T__ctor_Array
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     1,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                     (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericTypeParameter, 0,

                 // System_ReadOnlySpan_T__ctor_Array_Start_Length
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     3,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                     (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericTypeParameter, 0,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_ReadOnlySpan_T__ctor_ref_readonly_T
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     1,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                     (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericTypeParameter, 0,

                 // System_ReadOnlySpan_T__get_Item
                 (byte)(MemberFlags.PropertyGet),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericTypeParameter, 0, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_ReadOnlySpan_T__get_Length
                 (byte)(MemberFlags.PropertyGet),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    0,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type

                 // System_ReadOnlySpan_T__Slice_Int_Int
                 (byte)(MemberFlags.Method),                                                                                                                    // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    2,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, (byte)0,              // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_IsUnmanagedAttribute - WellKnownType.ExtSentinel),       // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                 // Microsoft_VisualBasic_Conversion__FixSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_VisualBasic_Conversion - WellKnownType.ExtSentinel),                // DeclaringTypeId
                0,                                                                                                                                  // Arity
                    1,                                                                                                                              // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single, // Return type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single, // Number As System.Single

                // Microsoft_VisualBasic_Conversion__FixDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_VisualBasic_Conversion - WellKnownType.ExtSentinel),                // DeclaringTypeId
                0,                                                                                                                                  // Arity
                    1,                                                                                                                              // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Number As System.Double

                 // Microsoft_VisualBasic_Conversion__IntSingle
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_VisualBasic_Conversion - WellKnownType.ExtSentinel),                // DeclaringTypeId
                0,                                                                                                                                  // Arity
                    1,                                                                                                                              // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single, // Return type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Single, // Number As System.Single

                // Microsoft_VisualBasic_Conversion__IntDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                    // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.Microsoft_VisualBasic_Conversion - WellKnownType.ExtSentinel),                // DeclaringTypeId
                0,                                                                                                                                  // Arity
                    1,                                                                                                                              // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Number As System.Double

                // System_Math__CeilingDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Math,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Math__FloorDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Math,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                // System_Math__TruncateDouble
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Math,                                                                            // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Double,

                 // System_Index__ctor
                 (byte)(MemberFlags.Constructor),                                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Index - WellKnownType.ExtSentinel),                                               // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     2,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                 // System_Index__GetOffset
                 (byte)MemberFlags.Method,                                                                                                                      // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Index - WellKnownType.ExtSentinel),                                               // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     1,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_Range__ctor
                 (byte)(MemberFlags.Constructor),
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),                                               // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     2,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                     (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Index - WellKnownType.ExtSentinel),
                     (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Index - WellKnownType.ExtSentinel),

                 // System_Range__StartAt
                 (byte)(MemberFlags.Method | MemberFlags.Static),
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),                                               // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     1,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),
                     (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Index - WellKnownType.ExtSentinel),

                 // System_Range__EndAt
                 (byte)(MemberFlags.Method | MemberFlags.Static),
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),                                               // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     1,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),
                     (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Index - WellKnownType.ExtSentinel),

                 // System_Range__get_All
                 (byte)(MemberFlags.PropertyGet | MemberFlags.Static),
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),                                               // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),

                 // System_Range__get_Start
                 (byte)MemberFlags.PropertyGet,
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),                                               // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Index - WellKnownType.ExtSentinel),

                 // System_Range__get_End
                 (byte)MemberFlags.PropertyGet,
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Range - WellKnownType.ExtSentinel),                                               // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                     0,                                                                                                                                         // Method Signature
                     (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Index - WellKnownType.ExtSentinel),

                // System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_IAsyncDisposable__DisposeAsync
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_IAsyncDisposable - WellKnownType.ExtSentinel),                                    // DeclaringTypeId
                0,                                                                                                                                             // Arity
                    0,                                                                                                                                         // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_ValueTask - WellKnownType.ExtSentinel), // Return Type: ValueTask

                // System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T - WellKnownType.ExtSentinel),               // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, // Return Type: IAsyncEnumerator<T>
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T - WellKnownType.ExtSentinel),
                        1,
                        (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, // Argument: CancellationToken
                        (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationToken - WellKnownType.ExtSentinel),

                // System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T - WellKnownType.ExtSentinel),               // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    0,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, // Return Type: ValueTask<bool>
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_ValueTask_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Collections_Generic_IAsyncEnumerator_T__get_Current
                (byte)(MemberFlags.PropertyGet | MemberFlags.Virtual),                                                                                          // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T - WellKnownType.ExtSentinel),               // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    0,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0, // Return Type: T

                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetResult,
                (byte)MemberFlags.Method,                                                                                                                       // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0, // Return Type: T
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument: short

                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetStatus,
                (byte)MemberFlags.Method,                                                                                                                       // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument: short

                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__OnCompleted,
                (byte)MemberFlags.Method,                                                                                                                       // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    4,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeInstance, // Argument: Action<object>
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Action_T,
                    1,
                        (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Argument
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags - WellKnownType.ExtSentinel), // Argument

                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__Reset
                (byte)MemberFlags.Method,                                                                                                                       // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    0,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetException,
                (byte)MemberFlags.Method,                                                                                                                       // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Exception, // Argument

                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetResult,
                (byte)MemberFlags.Method,                                                                                                                       // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0, // Argument: T

                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__get_Version,
                (byte)MemberFlags.PropertyGet,                                                                                                                  // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T - WellKnownType.ExtSentinel),     // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    0,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16,

                // System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult,
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T - WellKnownType.ExtSentinel),           // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0, // Return Type: T
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument: short

                // System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus,
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T - WellKnownType.ExtSentinel),           // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument: short

                // System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted,
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T - WellKnownType.ExtSentinel),           // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    4,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeInstance, // Argument: Action<object>
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Action_T,
                    1,
                        (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Argument
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags - WellKnownType.ExtSentinel), // Argument

                // System_Threading_Tasks_Sources_IValueTaskSource__GetResult,
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource - WellKnownType.ExtSentinel),             // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument: short

                // System_Threading_Tasks_Sources_IValueTaskSource__GetStatus,
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource - WellKnownType.ExtSentinel),             // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument: short

                // System_Threading_Tasks_Sources_IValueTaskSource__OnCompleted,
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource - WellKnownType.ExtSentinel),             // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    4,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeInstance, // Argument: Action<object>
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Action_T,
                    1,
                        (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Argument
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags - WellKnownType.ExtSentinel), // Argument

                // System_Threading_Tasks_ValueTask_T__ctorSourceAndToken
                (byte)MemberFlags.Constructor,                                                                                                                  // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_ValueTask_T - WellKnownType.ExtSentinel),                          // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    2,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeInstance, // Argument: IValueTaskSource<T>
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T - WellKnownType.ExtSentinel),
                    1,
                        (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument

                // System_Threading_Tasks_ValueTask_T__ctorValue
                (byte)MemberFlags.Constructor,                                                                                                                  // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_ValueTask_T - WellKnownType.ExtSentinel),                          // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0, // Argument: T

                // System_Threading_Tasks_ValueTask__ctor
                (byte)MemberFlags.Constructor,                                                                                                                  // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_ValueTask - WellKnownType.ExtSentinel),                            // DeclaringTypeId
                0,                                                                                                                                              // Arity
                    2,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource - WellKnownType.ExtSentinel), // Argument: IValueTaskSource
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int16, // Argument

                // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Create
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                                                             // Arity
                    0,                                                                                                                                         // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder - WellKnownType.ExtSentinel),

                // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Complete
                (byte)MemberFlags.Method,                                                                                                                      // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                                                             // Arity
                    0,                                                                                                                                         // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitOnCompleted
                (byte)MemberFlags.Method,                                                                                                                      // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder - WellKnownType.ExtSentinel), // DeclaringTypeId
                2,                                                                                                                                             // Arity
                    2,                                                                                                                                         // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitUnsafeOnCompleted
                (byte)MemberFlags.Method,                                                                                                                      // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder - WellKnownType.ExtSentinel), // DeclaringTypeId
                2,                                                                                                                                             // Arity
                    2,                                                                                                                                         // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, (byte)SpecialType.System_Object,

                // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__MoveNext_T
                (byte)MemberFlags.Method,                                                                                                                      // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder - WellKnownType.ExtSentinel), // DeclaringTypeId
                1,                                                                                                                                             // Arity
                    1,                                                                                                                                         // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.ByReference, (byte)SignatureTypeCode.GenericMethodParameter, 0,

                 // System_Runtime_CompilerServices_ITuple__get_Item
                 (byte)(MemberFlags.PropertyGet | MemberFlags.Virtual),                                                                       // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_ITuple - WellKnownType.ExtSentinel),   // DeclaringTypeId
                 0,                                                                                                                           // Arity
                    1,                                                                                                                        // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                 // System_Runtime_CompilerServices_ITuple__get_Length
                 (byte)(MemberFlags.PropertyGet | MemberFlags.Virtual),                                                                       // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_ITuple - WellKnownType.ExtSentinel),   // DeclaringTypeId
                 0,                                                                                                                           // Arity
                    0,                                                                                                                        // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type

                 // System_Exception__ctorString
                 (byte)MemberFlags.Constructor,                                                                                               // Flags
                 (byte)WellKnownType.System_Exception,                                                                                        // DeclaringTypeId
                 0,                                                                                                                           // Arity
                    1,                                                                                                                        // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                 // System_InvalidOperationException__ctor
                 (byte)MemberFlags.Constructor,                                                                                               // Flags
                  (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_InvalidOperationException - WellKnownType.ExtSentinel),        // DeclaringTypeId
                 0,                                                                                                                           // Arity
                    0,                                                                                                                        // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                 // System_Runtime_CompilerServices_SwitchExpressionException__ctor
                 (byte)MemberFlags.Constructor,                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_SwitchExpressionException - WellKnownType.ExtSentinel),// DeclaringTypeId
                 0,                                                                                                                           // Arity
                    0,                                                                                                                        // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                 // System_Runtime_CompilerServices_SwitchExpressionException__ctorObject
                 (byte)MemberFlags.Constructor,                                                                                               // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_SwitchExpressionException - WellKnownType.ExtSentinel),// DeclaringTypeId
                 0,                                                                                                                           // Arity
                    1,                                                                                                                        // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Threading_CancellationToken__Equals
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationToken - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationToken - WellKnownType.ExtSentinel), // Argument: CancellationToken
                    
                // System_Threading_CancellationToken__ThrowIfCancellationRequested
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationToken - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Threading_CancellationTokenSource__CreateLinkedTokenSource
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationTokenSource - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationTokenSource - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationToken - WellKnownType.ExtSentinel), // Argument: CancellationToken
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationToken - WellKnownType.ExtSentinel), // Argument: CancellationToken

                // System_Threading_CancellationTokenSource__Token
                (byte)MemberFlags.Property,                                                                                 // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationTokenSource - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationToken - WellKnownType.ExtSentinel), // Return Type

                // System_Threading_CancellationTokenSource__Dispose
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Threading_CancellationTokenSource - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type,

                // System_ArgumentNullException__ctorString
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ArgumentNullException - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,                                      // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,                                    // Argument

                // System_Runtime_CompilerServices_NativeIntegerAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_NativeIntegerAttribute - WellKnownType.ExtSentinel),                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_NativeIntegerAttribute__ctorTransformFlags
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_NativeIntegerAttribute - WellKnownType.ExtSentinel),                                       // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,

                // System_Text_StringBuilder__AppendString
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_StringBuilder - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_StringBuilder - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    
                // System_Text_StringBuilder__AppendChar
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_StringBuilder - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_StringBuilder - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,

                // System_Text_StringBuilder__AppendObject
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_StringBuilder - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_StringBuilder - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Text_StringBuilder__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_StringBuilder - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    
                // System_Runtime_CompilerServices_DefaultInterpolatedStringHandler__ToStringAndClear
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_DefaultInterpolatedStringHandler - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String, // Return Type

                // System_Runtime_CompilerServices_RequiredMemberAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_RequiredMemberAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Diagnostics_CodeAnalysis_SetsRequiredMembersAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Diagnostics_CodeAnalysis_SetsRequiredMembersAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_ScopedRefAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_ScopedRefAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_RefSafetyRulesAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_RefSafetyRulesAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_MemoryExtensions__SequenceEqual_Span_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_MemoryExtensions - WellKnownType.ExtSentinel),    // DeclaringTypeId
                1,                                                                                                             // Arity
                    2,                                                                                                         // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_Span_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericMethodParameter, (byte)0,
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericMethodParameter, (byte)0,

                // System_MemoryExtensions__SequenceEqual_ReadOnlySpan_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_MemoryExtensions - WellKnownType.ExtSentinel),    // DeclaringTypeId
                1,                                                                                                             // Arity
                    2,                                                                                                         // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericMethodParameter, (byte)0,
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericMethodParameter, (byte)0,

                // System_MemoryExtensions__AsSpan_String
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_MemoryExtensions - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                             // Arity
                    1,                                                                                                         // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Char,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    
                // System_Diagnostics_CodeAnalysis_UnscopedRefAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Diagnostics_CodeAnalysis_UnscopedRefAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_NotSupportedException__ctor
                (byte)MemberFlags.Constructor,                                                                                               // Flags
                (byte)WellKnownType.System_NotSupportedException,            // DeclaringTypeId
                 0,                                                                                                                          // Arity
                    0,                                                                                                                       // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_IndexOutOfRangeException__ctor
                (byte)MemberFlags.Constructor,                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_IndexOutOfRangeException - WellKnownType.ExtSentinel),                                                                         // DeclaringTypeId
                0,                                                                                                                           // Arity
                    0,                                                                                                                       // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Runtime_CompilerServices_HotReloadException_ctorStringInt32
                (byte)MemberFlags.Constructor,                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_HotReloadException - WellKnownType.ExtSentinel),            // DeclaringTypeId
                0,                                                                                                                           // Arity
                    2,                                                                                                                       // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // MetadataUpdateOriginalTypeAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_MetadataUpdateOriginalTypeAttribute - WellKnownType.ExtSentinel),            // DeclaringTypeId
                 0,                                                                                                                          // Arity
                    1,                                                                                                                       // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,                                                       // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // MetadataUpdateDeletedAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                                               // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_MetadataUpdateDeletedAttribute - WellKnownType.ExtSentinel),            // DeclaringTypeId
                 0,                                                                                                                          // Arity
                    0,                                                                                                                       // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,                                                       // Return Type

                // System_Collections_ICollection__Count
                (byte)(MemberFlags.Property | MemberFlags.Virtual),                                                         // Flags
                (byte)WellKnownType.System_Collections_ICollection,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type

                // System_Collections_ICollection__IsSynchronized
                (byte)(MemberFlags.Property | MemberFlags.Virtual),                                                         // Flags
                (byte)WellKnownType.System_Collections_ICollection,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type

                // System_Collections_ICollection__SyncRoot
                (byte)(MemberFlags.Property | MemberFlags.Virtual),                                                         // Flags
                (byte)WellKnownType.System_Collections_ICollection,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type

                // System_Collections_ICollection__CopyTo
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_ICollection,                                                         // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Array,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Collections_IList__get_Item
                (byte)(MemberFlags.PropertyGet | MemberFlags.Virtual),                                                      // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Collections_IList__IsFixedSize
                (byte)(MemberFlags.Property | MemberFlags.Virtual),                                                         // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type

                // System_Collections_IList__IsReadOnly
                (byte)(MemberFlags.Property | MemberFlags.Virtual),                                                         // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type

                // System_Collections_IList__Add
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Collections_IList__Clear
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Collections_IList__Contains
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Collections_IList__IndexOf
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Collections_IList__Insert
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Collections_IList__Remove
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,

                // System_Collections_IList__RemoveAt
                (byte)(MemberFlags.Method | MemberFlags.Virtual),                                                           // Flags
                (byte)WellKnownType.System_Collections_IList,                                                               // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Collections_Generic_List_T__ctor
                (byte)MemberFlags.Constructor,                                                                                               // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                      // DeclaringTypeId
                 0,                                                                                                                           // Arity
                    0,                                                                                                                        // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,

                // System_Collections_Generic_List_T__ctorInt32
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                      // DeclaringTypeId
                 0,                                                                                                         // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Collections_Generic_List_T__Add
                (byte)MemberFlags.Method,                                                           // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                 // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_List_T__Count
                (byte)MemberFlags.Property,                                                         // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type

                // System_Collections_Generic_List_T__Contains
                (byte)MemberFlags.Method,                                                           // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_List_T__CopyTo
                (byte)MemberFlags.Method,                                                           // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericTypeParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Collections_Generic_List_T__get_Item
                (byte)MemberFlags.PropertyGet,                                                      // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeParameter, 0, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Collections_Generic_List_T__IndexOf
                (byte)MemberFlags.Method,                                                           // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32, // Return Type
                    (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_List_T__ToArray
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericTypeParameter, 0, // Return Type

                // System_Runtime_InteropServices_CollectionsMarshal__AsSpan_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_InteropServices_CollectionsMarshal - WellKnownType.ExtSentinel), // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),  // Return Type
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Collections_Generic_List_T,
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Runtime_InteropServices_CollectionsMarshal__SetCount_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_InteropServices_CollectionsMarshal - WellKnownType.ExtSentinel), // DeclaringTypeId
                1,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,                                                       // Return Type
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Collections_Generic_List_T,
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Runtime_InteropServices_ImmutableCollectionsMarshal__AsImmutableArray_T
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_InteropServices_ImmutableCollectionsMarshal - WellKnownType.ExtSentinel), // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,                                                            // Return Type
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Collections_Immutable_ImmutableArray_T,
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericMethodParameter, 0,                       // Parameter Type

                // System_Span_T__ToArray
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),           // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericTypeParameter, 0, // Return Type

                // System_ReadOnlySpan_T__ToArray
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),   // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericTypeParameter, 0, // Return Type

                // System_Span_T__CopyTo_Span_T
                (byte)MemberFlags.Method,                                                           // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),           // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),
                        1,
                        (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_ReadOnlySpan_T__CopyTo_Span_T
                (byte)MemberFlags.Method,                                                           // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),   // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),
                        1,
                        (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Immutable_ImmutableArray_T__AsSpan
                (byte)MemberFlags.Method,                                                           // Flags
                (byte)WellKnownType.System_Collections_Immutable_ImmutableArray_T,         // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),  // Return Type
                        1,
                        (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Immutable_ImmutableArray_T__Empty
                (byte)(MemberFlags.Field | MemberFlags.Static),                                                             // Flags
                (byte)WellKnownType.System_Collections_Immutable_ImmutableArray_T,                                          // DeclaringTypeId
                0,                                                                                                          // Arity
                    (byte)SignatureTypeCode.GenericTypeInstance,                                                            // Field Signature
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Collections_Immutable_ImmutableArray_T,
                        1,
                        (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Collections_Generic_List_T__AddRange
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.System_Collections_Generic_List_T,                                                      // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void,                                      // Return Type
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                        1,
                        (byte)SignatureTypeCode.GenericTypeParameter, 0,

                // System_Runtime_CompilerServices_ParamCollectionAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_ParamCollectionAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type

                // System_Runtime_CompilerServices_ExtensionMarkerAttribute__ctor
                (byte)MemberFlags.Constructor,                                                                              // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Runtime_CompilerServices_ExtensionMarkerAttribute - WellKnownType.ExtSentinel), // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Void, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Linq_Enumerable__ToList
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Enumerable,                                                                 // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, // Return Type
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Collections_Generic_List_T,
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Linq_Enumerable__ToArray
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Enumerable,                                                                 // DeclaringTypeId
                1,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.GenericMethodParameter, 0, // Return Type
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,

                // System_Linq_Expressions_Expression__ArrayIndex_Expression_Expression,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__ArrayIndex_Expression_Expressions,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MethodCallExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Constant,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_ConstantExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Object,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Linq_Expressions_Expression__UnaryPlus,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Negate_Expression,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Negate_Expression_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__NegateChecked_Expression,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__NegateChecked_Expression_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Not_Expression,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Not_Expression_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__New_Type,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_NewExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Linq_Expressions_Expression__New_ConstructorInfo_IEnumerableExpressions,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_NewExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_ConstructorInfo,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                        1,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__New_ConstructorInfo_ArrayExpressions,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_NewExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_ConstructorInfo,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__New_ConstructorInfo_Expressions_MemberInfos,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_NewExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_ConstructorInfo,
                    (byte)SignatureTypeCode.GenericTypeInstance,
                        (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Collections_Generic_IEnumerable_T,
                        1,
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MemberInfo,

                // System_Linq_Expressions_Expression__Property,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MemberExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__MemberBind_MemberInfo
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MemberMemberBinding - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MemberInfo,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_MemberBinding,

                // System_Linq_Expressions_Expression__MemberBind_MethodInfo
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MemberMemberBinding - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_MemberBinding,

                // System_Linq_Expressions_Expression__Bind_MemberInfo
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MemberAssignment - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MemberInfo,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Bind_MethodInfo
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MemberAssignment - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__ListBind_MemberInfo
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MemberListBinding - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MemberInfo,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_ElementInit,

                // System_Linq_Expressions_Expression__ListBind_MethodInfo
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MemberListBinding - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_ElementInit,

                // System_Linq_Expressions_Expression__ElementInit
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_ElementInit, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__ListInit
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_ListInitExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_NewExpression - WellKnownType.ExtSentinel),
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_ElementInit,

                // System_Linq_Expressions_Expression__MemberInit
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MemberInitExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_NewExpression - WellKnownType.ExtSentinel),
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_MemberBinding,

                // System_Linq_Expressions_Expression__Lambda
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_LambdaExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_ParameterExpression,

                // System_Linq_Expressions_Expression__Lambda_OfTDelegate
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                1,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, // Return Type
                        (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression_T,
                        1,
                        (byte)SignatureTypeCode.GenericMethodParameter, 0,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_ParameterExpression,

                // System_Linq_Expressions_Expression__Parameter
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_ParameterExpression, // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,

                // System_Linq_Expressions_Expression__Coalesce,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Coalesce_Lambda,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_LambdaExpression - WellKnownType.ExtSentinel),

                // System_Linq_Expressions_Expression__Quote
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__TypeIs
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_TypeBinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Linq_Expressions_Expression__Field
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MemberExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_FieldInfo,

                // System_Linq_Expressions_Expression__Convert
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Linq_Expressions_Expression__Convert_MethodInfo
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__ConvertChecked
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Linq_Expressions_Expression__ConvertChecked_MethodInfo
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Condition
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_ConditionalExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Call
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_MethodCallExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Invoke
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_InvocationExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__TypeAs
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Linq_Expressions_Expression__ArrayLength
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_UnaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__NewArrayBounds
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_NewArrayExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__NewArrayInit
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_NewArrayExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,
                    (byte)SignatureTypeCode.SZArray, (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Add,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Add_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__AddChecked,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__AddChecked_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Multiply,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Multiply_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__MultiplyChecked,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__MultiplyChecked_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Subtract,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Subtract_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__SubtractChecked,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__SubtractChecked_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Divide,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Divide_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Modulo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Modulo_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__And,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__And_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__AndAlso,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__AndAlso_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__ExclusiveOr,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__ExclusiveOr_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Or,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Or_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__OrElse,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__OrElse_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__LeftShift,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__LeftShift_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__RightShift,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__RightShift_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Equal,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__Equal_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__NotEqual,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__NotEqual_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__LessThan,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__LessThan_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__LessThanOrEqual,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__LessThanOrEqual_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__GreaterThan,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__GreaterThan_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__GreaterThanOrEqual,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,

                // System_Linq_Expressions_Expression__GreaterThanOrEqual_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    4,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Boolean,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Linq_Expressions_Expression__Default
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    1,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_DefaultExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Type,

                // System_Linq_Expressions_Expression__Power_MethodInfo,
                (byte)(MemberFlags.Method | MemberFlags.Static),                                                            // Flags
                (byte)WellKnownType.System_Linq_Expressions_Expression,                                                     // DeclaringTypeId
                0,                                                                                                          // Arity
                    3,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Linq_Expressions_BinaryExpression - WellKnownType.ExtSentinel), // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Linq_Expressions_Expression,
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.System_Reflection_MethodInfo,

                // System_Text_Encoding__get_UTF8
                (byte)(MemberFlags.PropertyGet | MemberFlags.Static),                                                       // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_Encoding - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    0,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_Encoding - WellKnownType.ExtSentinel), // Return Type

                // System_Text_Encoding__GetString
                (byte)MemberFlags.Method,                                                                                   // Flags
                (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Text_Encoding - WellKnownType.ExtSentinel),    // DeclaringTypeId
                0,                                                                                                          // Arity
                    2,                                                                                                      // Method Signature
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_String,                                    // Return Type
                    (byte)SignatureTypeCode.Pointer, (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Byte,     // Argument: byte*
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,                                     // Argument: int

                // System_Span_T__Slice_Int
                (byte)(MemberFlags.Method),                                                                                                                    // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Span_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_Span_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, (byte)0,              // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_ReadOnlySpan_T__Slice_Int
                (byte)(MemberFlags.Method),                                                                                                                    // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_ReadOnlySpan_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, (byte)0,              // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Memory_T__Slice_Int_Int
                 (byte)(MemberFlags.Method),                                                                                                                    // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Memory_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    2,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_Memory_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, (byte)0,              // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_ReadOnlyMemory_T__Slice_Int_Int
                 (byte)(MemberFlags.Method),                                                                                                                    // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlyMemory_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    2,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_ReadOnlyMemory_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, (byte)0,              // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_Memory_T__Slice_Int
                (byte)(MemberFlags.Method),                                                                                                                    // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_Memory_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_Memory_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, (byte)0,              // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,

                // System_ReadOnlyMemory_T__Slice_Int
                (byte)(MemberFlags.Method),                                                                                                                    // Flags
                 (byte)WellKnownType.ExtSentinel, (byte)(WellKnownType.System_ReadOnlyMemory_T - WellKnownType.ExtSentinel),                                      // DeclaringTypeId
                 0,                                                                                                                                             // Arity
                    1,                                                                                                                                          // Method Signature
                    (byte)SignatureTypeCode.GenericTypeInstance, (byte)SignatureTypeCode.TypeHandle,
                    (byte)WellKnownType.ExtSentinel, (WellKnownType.System_ReadOnlyMemory_T - WellKnownType.ExtSentinel),
                    1,
                    (byte)SignatureTypeCode.GenericTypeParameter, (byte)0,              // Return Type
                    (byte)SignatureTypeCode.TypeHandle, (byte)SpecialType.System_Int32,
            };

            string[] allNames = new string[(int)WellKnownMember.Count]
            {
                "Round",                                    // System_Math__RoundDouble
                "Pow",                                      // System_Math__PowDoubleDouble
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
                WellKnownMemberNames.EqualityOperatorName,  // System_Type__op_Equality
                ".ctor",                                    // System_Reflection_AssemblyKeyFileAttribute__ctor
                ".ctor",                                    // System_Reflection_AssemblyKeyNameAttribute__ctor
                "GetMethodFromHandle",                      // System_Reflection_MethodBase__GetMethodFromHandle
                "GetMethodFromHandle",                      // System_Reflection_MethodBase__GetMethodFromHandle2
                "CreateDelegate",                           // System_Reflection_MethodInfo__CreateDelegate
                "GetFieldFromHandle",                       // System_Reflection_FieldInfo__GetFieldFromHandle
                "GetFieldFromHandle",                       // System_Reflection_FieldInfo__GetFieldFromHandle2
                "Value",                                    // System_Reflection_Missing__Value
                "Equals",                                   // System_IEquatable_T__Equals
                "Equals",                                   // System_Collections_Generic_IEqualityComparer_T__Equals
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
                "CreateSpan",                               // System_Runtime_InteropServices_MemoryMarshal__CreateSpan,
                "CreateReadOnlySpan",                       // System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan,
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
                "CreateSpan",                               // System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle
                "GetObjectValue",                           // System_Runtime_CompilerServices_RuntimeHelpers__GetObjectValueObject
                "InitializeArray",                          // System_Runtime_CompilerServices_RuntimeHelpers__InitializeArrayArrayRuntimeFieldHandle
                "get_OffsetToStringData",                   // System_Runtime_CompilerServices_RuntimeHelpers__get_OffsetToStringData
                "GetSubArray",                              // System_Runtime_CompilerServices_RuntimeHelpers__GetSubArray_T
                "EnsureSufficientExecutionStack",           // System_Runtime_CompilerServices_RuntimeHelpers__EnsureSufficientExecutionStack
                "Add",                                      // System_Runtime_CompilerServices_Unsafe__Add_T
                "As",                                       // System_Runtime_CompilerServices_Unsafe__As_T,
                "AsRef",                                    // System_Runtime_CompilerServices_Unsafe__AsRef_T,
                "Capture",                                  // System_Runtime_ExceptionServices_ExceptionDispatchInfo__Capture
                "Throw",                                    // System_Runtime_ExceptionServices_ExceptionDispatchInfo__Throw
                ".ctor",                                    // System_Security_UnverifiableCodeAttribute__ctor
                "RequestMinimum",                           // System_Security_Permissions_SecurityAction__RequestMinimum
                ".ctor",                                    // System_Security_Permissions_SecurityPermissionAttribute__ctor
                "SkipVerification",                         // System_Security_Permissions_SecurityPermissionAttribute__SkipVerification
                "CreateInstance",                           // System_Activator__CreateInstance
                "CreateInstance",                           // System_Activator__CreateInstance_T
                "CompareExchange",                          // System_Threading_Interlocked__CompareExchange
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

                "CreatePayload",                            // Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningSingleFile
                "CreatePayload",                            // Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningMultipleFiles

                "LogMethodEntry",                           // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogMethodEntry
                "LogLambdaEntry",                           // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLambdaEntry
                "LogStateMachineMethodEntry",               // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogStateMachineMethodEntry
                "LogStateMachineLambdaEntry",               // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogStateMachineLambdaEntry
                "LogReturn",                                // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogReturn
                "GetNewStateMachineInstanceId",             // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__GetNewStateMachineInstanceId
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreBoolean
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreByte
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt16
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt32
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt64
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreSingle
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreDouble
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreDecimal
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreString
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreObject
                "LogLocalStore",                            // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStorePointer
                "LogLocalStoreUnmanaged",                   // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUnmanaged
                "LogLocalStoreParameterAlias",              // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreParameterAlias
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreBoolean
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreByte
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUInt16
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUInt32
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUInt64
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreSingle
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreDouble
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreDecimal
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreString
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreObject
                "LogParameterStore",                        // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStorePointer
                "LogParameterStoreUnmanaged",               // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreUnmanaged
                "LogParameterStoreParameterAlias",          // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreParameterAlias
                "LogLocalStoreLocalAlias",                  // Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreLocalAlias

                ".ctor",                                    // System_Runtime_CompilerServices_NullableAttribute__ctorByte
                ".ctor",                                    // System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags
                ".ctor",                                    // System_Runtime_CompilerServices_NullableContextAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_NullablePublicOnlyAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_ReferenceAssemblyAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_RequiresLocationAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_IsByRefLikeAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_ObsoleteAttribute__ctor
                ".ctor",                                    // System_Span_T__ctor_Pointer
                ".ctor",                                    // System_Span_T__ctor_Array
                ".ctor",                                    // System_Span_T__ctor_ref_T
                "get_Item",                                 // System_Span_T__get_Item
                "get_Length",                               // System_Span_T__get_Length
                "Slice",                                    // System_Span_T__Slice_Int_Int
                ".ctor",                                    // System_ReadOnlySpan_T__ctor_Pointer
                ".ctor",                                    // System_ReadOnlySpan_T__ctor_Array
                ".ctor",                                    // System_ReadOnlySpan_T__ctor_Array_Start_Length
                ".ctor",                                    // System_ReadOnlySpan_T__ctor_ref_readonly_T
                "get_Item",                                 // System_ReadOnlySpan_T__get_Item
                "get_Length",                               // System_ReadOnlySpan_T__get_Length
                "Slice",                                    // System_ReadOnlySpan_T__Slice_Int_Int
                ".ctor",                                    // System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor

                "Fix",                                      // Microsoft_VisualBasic_Conversion__FixSingle
                "Fix",                                      // Microsoft_VisualBasic_Conversion__FixDouble
                "Int",                                      // Microsoft_VisualBasic_Conversion__IntSingle
                "Int",                                      // Microsoft_VisualBasic_Conversion__IntDouble
                "Ceiling",                                  // System_Math__CeilingDouble
                "Floor",                                    // System_Math__FloorDouble
                "Truncate",                                 // System_Math__TruncateDouble

                ".ctor",                                    // System_Index__ctor
                "GetOffset",                                // System_Index__GetOffset
                ".ctor",                                    // System_Range__ctor
                "StartAt",                                  // System_Range__StartAt
                "EndAt",                                    // System_Range__EndAt
                "get_All",                                  // System_Range__get_All
                "get_Start",                                // System_Range__get_Start
                "get_End",                                  // System_Range__get_End

                ".ctor",                                    // System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute__ctor

                "DisposeAsync",                             // System_IAsyncDisposable__DisposeAsync
                "GetAsyncEnumerator",                       // System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator
                "MoveNextAsync",                            // System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync
                "get_Current",                              // System_Collections_Generic_IAsyncEnumerator_T__get_Current

                "GetResult",                                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetResult
                "GetStatus",                                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetStatus
                "OnCompleted",                              // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__OnCompleted
                "Reset",                                    // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__Reset
                "SetException",                             // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetException
                "SetResult",                                // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetResult
                "get_Version",                              // System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__get_Version
                "GetResult",                                // System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult
                "GetStatus",                                // System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus
                "OnCompleted",                              // System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted
                "GetResult",                                // System_Threading_Tasks_Sources_IValueTaskSource__GetResult
                "GetStatus",                                // System_Threading_Tasks_Sources_IValueTaskSource__GetStatus
                "OnCompleted",                              // System_Threading_Tasks_Sources_IValueTaskSource__OnCompleted
                ".ctor",                                    // System_Threading_Tasks_ValueTask_T__ctor
                ".ctor",                                    // System_Threading_Tasks_ValueTask_T__ctorValue
                ".ctor",                                    // System_Threading_Tasks_ValueTask__ctor
                "Create",                                   // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Create
                "Complete",                                 // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__Complete
                "AwaitOnCompleted",                         // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitOnCompleted
                "AwaitUnsafeOnCompleted",                   // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__AwaitUnsafeOnCompleted
                "MoveNext",                                 // System_Runtime_CompilerServices_AsyncIteratorMethodBuilder__MoveNext_T
                "get_Item",                                 // System_Runtime_CompilerServices_ITuple__get_Item
                "get_Length",                               // System_Runtime_CompilerServices_ITuple__get_Length
                ".ctor",                                    // System_Exception__ctorString
                ".ctor",                                    // System_InvalidOperationException__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_SwitchExpressionException__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_SwitchExpressionException__ctorObject
                "Equals",                                   // System_Threading_CancellationToken__Equals
                "ThrowIfCancellationRequested",             // System_Threading_CancellationToken__ThrowIfCancellationRequested
                "CreateLinkedTokenSource",                  // System_Threading_CancellationTokenSource__CreateLinkedTokenSource
                "Token",                                    // System_Threading_CancellationTokenSource__Token
                "Dispose",                                  // System_Threading_CancellationTokenSource__Dispose
                ".ctor",                                    // System_ArgumentNullException__ctorString
                ".ctor",                                    // System_Runtime_CompilerServices_NativeIntegerAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_NativeIntegerAttribute__ctorTransformFlags
                "Append",                                   // System_Text_StringBuilder__AppendString
                "Append",                                   // System_Text_StringBuilder__AppendChar
                "Append",                                   // System_Text_StringBuilder__AppendObject
                ".ctor",                                    // System_Text_StringBuilder__ctor
                "ToStringAndClear",                         // System_Runtime_CompilerServices_DefaultInterpolatedStringHandler__ToStringAndClear
                ".ctor",                                    // System_Runtime_CompilerServices_RequiredMemberAttribute__ctor
                ".ctor",                                    // System_Diagnostics_CodeAnalysis_SetsRequiredMembersAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_ScopedRefAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_RefSafetyRulesAttribute__ctor
                "SequenceEqual",                            // System_MemoryExtensions__SequenceEqual_Span_T
                "SequenceEqual",                            // System_MemoryExtensions__SequenceEqual_ReadOnlySpan_T
                "AsSpan",                                   // System_MemoryExtensions__AsSpan_String
                ".ctor",                                    // System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute_ctor
                ".ctor",                                    // System_Diagnostics_CodeAnalysis_UnscopedRefAttribute__ctor
                ".ctor",                                    // System_NotSupportedException__ctor
                ".ctor",                                    // System_IndexOutOfRangeException__ctor
                ".ctor",                                    // System_MissingMethodException__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_MetadataUpdateOriginalTypeAttribute
                ".ctor",                                    // System_Runtime_CompilerServices_MetadataUpdateDeletedAttribute
                "Count",                                    // System_Collections_ICollection__Count,
                "IsSynchronized",                           // System_Collections_ICollection__IsSynchronized,
                "SyncRoot",                                 // System_Collections_ICollection__SyncRoot,
                "CopyTo",                                   // System_Collections_ICollection__CopyTo,
                "get_Item",                                 // System_Collections_IList__get_Item,
                "IsFixedSize",                              // System_Collections_IList__IsFixedSize,
                "IsReadOnly",                               // System_Collections_IList__IsReadOnly,
                "Add",                                      // System_Collections_IList__Add,
                "Clear",                                    // System_Collections_IList__Clear,
                "Contains",                                 // System_Collections_IList__Contains,
                "IndexOf",                                  // System_Collections_IList__IndexOf,
                "Insert",                                   // System_Collections_IList__Insert,
                "Remove",                                   // System_Collections_IList__Remove,
                "RemoveAt",                                 // System_Collections_IList__RemoveAt,
                ".ctor",                                    // System_Collections_Generic_List_T__ctor,
                ".ctor",                                    // System_Collections_Generic_List_T__ctorInt32,
                "Add",                                      // System_Collections_Generic_List_T__Add
                "Count",                                    // System_Collections_Generic_List_T__Count,
                "Contains",                                 // System_Collections_Generic_List_T__Contains,
                "CopyTo",                                   // System_Collections_Generic_List_T__CopyTo,
                "get_Item",                                 // System_Collections_Generic_List_T__get_Item,
                "IndexOf",                                  // System_Collections_Generic_List_T__IndexOf,
                "ToArray",                                  // System_Collections_Generic_List_T__ToArray
                "AsSpan",                                   // System_Runtime_InteropServices_CollectionsMarshal__AsSpan_T
                "SetCount",                                 // System_Runtime_InteropServices_CollectionsMarshal__SetCount_T
                "AsImmutableArray",                         // System_Runtime_InteropServices_ImmutableCollectionsMarshal__AsImmutableArray_T
                "ToArray",                                  // System_Span_T__ToArray
                "ToArray",                                  // System_ReadOnlySpan_T__ToArray
                "CopyTo",                                   // System_Span_T__CopyTo_Span_T
                "CopyTo",                                   // System_ReadOnlySpan_T__CopyTo_Span_T
                "AsSpan",                                   // System_Collections_Immutable_ImmutableArray_T__AsSpan
                "Empty",                                    // System_Collections_Immutable_ImmutableArray_T__Empty
                "AddRange",                                 // System_Collections_Generic_List_T__AddRange
                ".ctor",                                    // System_Runtime_CompilerServices_ParamCollectionAttribute__ctor
                ".ctor",                                    // System_Runtime_CompilerServices_ExtensionMarkerAttribute__ctor
                "ToList",                                   // System_Linq_Enumerable__ToList
                "ToArray",                                  // System_Linq_Enumerable__ToArray
                "ArrayIndex",                               // System_Linq_Expressions_Expression__ArrayIndex_Expression_Expression
                "ArrayIndex",                               // System_Linq_Expressions_Expression__ArrayIndex_Expression_Expressions
                "Constant",                                 // System_Linq_Expressions_Expression__Constant
                "UnaryPlus",                                // System_Linq_Expressions_Expression__UnaryPlus
                "Negate",                                   // System_Linq_Expressions_Expression__Negate_Expression
                "Negate",                                   // System_Linq_Expressions_Expression__Negate_Expression_MethodInfo
                "NegateChecked",                            // System_Linq_Expressions_Expression__NegateChecked_Expression
                "NegateChecked",                            // System_Linq_Expressions_Expression__NegateChecked_Expression_MethodInfo
                "Not",                                      // System_Linq_Expressions_Expression__Not_Expression
                "Not",                                      // System_Linq_Expressions_Expression__Not_Expression_MethodInfo
                "New",                                      // System_Linq_Expressions_Expression__New_Type
                "New",                                      // System_Linq_Expressions_Expression__New_ConstructorInfo_IEnumerableExpressions
                "New",                                      // System_Linq_Expressions_Expression__New_ConstructorInfo_ArrayExpressions
                "New",                                      // System_Linq_Expressions_Expression__New_ConstructorInfo_Expressions_MemberInfos
                "Property",                                 // System_Linq_Expressions_Expression__Property
                "MemberBind",                               // System_Linq_Expressions_Expression__MemberBind_MemberInfo
                "MemberBind",                               // System_Linq_Expressions_Expression__MemberBind_MethodInfo
                "Bind",                                     // System_Linq_Expressions_Expression__Bind_MemberInfo
                "Bind",                                     // System_Linq_Expressions_Expression__Bind_MethodInfo
                "ListBind",                                 // System_Linq_Expressions_Expression__ListBind_MemberInfo
                "ListBind",                                 // System_Linq_Expressions_Expression__ListBind_MethodInfo
                "ElementInit",                              // System_Linq_Expressions_Expression__ElementInit
                "ListInit",                                 // System_Linq_Expressions_Expression__ListInit
                "MemberInit",                               // System_Linq_Expressions_Expression__MemberInit
                "Lambda",                                   // System_Linq_Expressions_Expression__Lambda
                "Lambda",                                   // System_Linq_Expressions_Expression__Lambda_OfTDelegate
                "Parameter",                                // System_Linq_Expressions_Expression__Parameter
                "Coalesce",                                 // System_Linq_Expressions_Expression__Coalesce
                "Coalesce",                                 // System_Linq_Expressions_Expression__Coalesce_Lambda
                "Quote",                                    // System_Linq_Expressions_Expression__Quote
                "TypeIs",                                   // System_Linq_Expressions_Expression__TypeIs
                "Field",                                    // System_Linq_Expressions_Expression__Field
                "Convert",                                  // System_Linq_Expressions_Expression__Convert
                "Convert",                                  // System_Linq_Expressions_Expression__Convert_MethodInfo
                "ConvertChecked",                           // System_Linq_Expressions_Expression__ConvertChecked
                "ConvertChecked",                           // System_Linq_Expressions_Expression__ConvertChecked_MethodInfo
                "Condition",                                // System_Linq_Expressions_Expression__Condition
                "Call",                                     // System_Linq_Expressions_Expression__Call
                "Invoke",                                   // System_Linq_Expressions_Expression__Invoke
                "TypeAs",                                   // System_Linq_Expressions_Expression__TypeAs
                "ArrayLength",                              // System_Linq_Expressions_Expression__ArrayLength
                "NewArrayBounds",                           // System_Linq_Expressions_Expression__NewArrayBounds
                "NewArrayInit",                             // System_Linq_Expressions_Expression__NewArrayInit
                "Add",                                      // System_Linq_Expressions_Expression__Add,
                "Add",                                      // System_Linq_Expressions_Expression__Add_MethodInfo,
                "AddChecked",                               // System_Linq_Expressions_Expression__AddChecked,
                "AddChecked",                               // System_Linq_Expressions_Expression__AddChecked_MethodInfo,
                "Multiply",                                 // System_Linq_Expressions_Expression__Multiply,
                "Multiply",                                 // System_Linq_Expressions_Expression__Multiply_MethodInfo,
                "MultiplyChecked",                          // System_Linq_Expressions_Expression__MultiplyChecked,
                "MultiplyChecked",                          // System_Linq_Expressions_Expression__MultiplyChecked_MethodInfo,
                "Subtract",                                 // System_Linq_Expressions_Expression__Subtract,
                "Subtract",                                 // System_Linq_Expressions_Expression__Subtract_MethodInfo,
                "SubtractChecked",                          // System_Linq_Expressions_Expression__SubtractChecked,
                "SubtractChecked",                          // System_Linq_Expressions_Expression__SubtractChecked_MethodInfo,
                "Divide",                                   // System_Linq_Expressions_Expression__Divide,
                "Divide",                                   // System_Linq_Expressions_Expression__Divide_MethodInfo,
                "Modulo",                                   // System_Linq_Expressions_Expression__Modulo,
                "Modulo",                                   // System_Linq_Expressions_Expression__Modulo_MethodInfo,
                "And",                                      // System_Linq_Expressions_Expression__And,
                "And",                                      // System_Linq_Expressions_Expression__And_MethodInfo,
                "AndAlso",                                  // System_Linq_Expressions_Expression__AndAlso,
                "AndAlso",                                  // System_Linq_Expressions_Expression__AndAlso_MethodInfo,
                "ExclusiveOr",                              // System_Linq_Expressions_Expression__ExclusiveOr,
                "ExclusiveOr",                              // System_Linq_Expressions_Expression__ExclusiveOr_MethodInfo,
                "Or",                                       // System_Linq_Expressions_Expression__Or,
                "Or",                                       // System_Linq_Expressions_Expression__Or_MethodInfo,
                "OrElse",                                   // System_Linq_Expressions_Expression__OrElse,
                "OrElse",                                   // System_Linq_Expressions_Expression__OrElse_MethodInfo,
                "LeftShift",                                // System_Linq_Expressions_Expression__LeftShift,
                "LeftShift",                                // System_Linq_Expressions_Expression__LeftShift_MethodInfo,
                "RightShift",                               // System_Linq_Expressions_Expression__RightShift,
                "RightShift",                               // System_Linq_Expressions_Expression__RightShift_MethodInfo,
                "Equal",                                    // System_Linq_Expressions_Expression__Equal,
                "Equal",                                    // System_Linq_Expressions_Expression__Equal_MethodInfo,
                "NotEqual",                                 // System_Linq_Expressions_Expression__NotEqual,
                "NotEqual",                                 // System_Linq_Expressions_Expression__NotEqual_MethodInfo,
                "LessThan",                                 // System_Linq_Expressions_Expression__LessThan,
                "LessThan",                                 // System_Linq_Expressions_Expression__LessThan_MethodInfo,
                "LessThanOrEqual",                          // System_Linq_Expressions_Expression__LessThanOrEqual,
                "LessThanOrEqual",                          // System_Linq_Expressions_Expression__LessThanOrEqual_MethodInfo,
                "GreaterThan",                              // System_Linq_Expressions_Expression__GreaterThan,
                "GreaterThan",                              // System_Linq_Expressions_Expression__GreaterThan_MethodInfo,
                "GreaterThanOrEqual",                       // System_Linq_Expressions_Expression__GreaterThanOrEqual,
                "GreaterThanOrEqual",                       // System_Linq_Expressions_Expression__GreaterThanOrEqual_MethodInfo,
                "Default",                                  // System_Linq_Expressions_Expression__Default
                "Power",                                    // System_Linq_Expressions_Expression__Power_MethodInfo,
                "get_UTF8",                                 // System_Text_Encoding__get_UTF8
                "GetString",                                // System_Text_Encoding__GetString
                "Slice",                                    // System_Span_T__Slice_Int
                "Slice",                                    // System_ReadOnlySpan_T__Slice_Int
                "Slice",                                    // System_Memory_T__Slice_Int_Int
                "Slice",                                    // System_ReadOnlyMemory_T__Slice_Int_Int
                "Slice",                                    // System_Memory_T__Slice_Int
                "Slice",                                    // System_ReadOnlyMemory_T__Slice_Int
            };

            s_descriptors = MemberDescriptor.InitializeFromStream(new System.IO.MemoryStream(initializationBytes, writable: false), allNames);

#if DEBUG
            foreach (var descriptor in s_descriptors)
            {
                Debug.Assert(!descriptor.IsSpecialTypeMember); // Members of types from core library should be in the SpecialMember set instead.
            }
#endif
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
                case WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute__ctor:
                case WellKnownMember.System_Runtime_CompilerServices_MetadataUpdateDeletedAttribute__ctor:
                    return true;

                default:
                    return false;
            }
        }
    }
}
