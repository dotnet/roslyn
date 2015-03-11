
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.33440
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING

// Only shown items having accessibility: Public Family


// Metadata version: v2.0.50727
.module extern dummy
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly Microsoft.VisualStudio.Debugger.Metadata
{
  .permissionset reqmin
             = {class 'System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' = {property bool 'SkipVerification' = bool(true)}}
  .publickey = (00 24 00 00 04 80 00 00 94 00 00 00 06 02 00 00   // .$..............
                00 24 00 00 52 53 41 31 00 04 00 00 01 00 01 00   // .$..RSA1........
                07 D1 FA 57 C4 AE D9 F0 A3 2E 84 AA 0F AE FD 0D   // ...W............
                E9 E8 FD 6A EC 8F 87 FB 03 76 6C 83 4C 99 92 1E   // ...j.....vl.L...
                B2 3B E7 9A D9 D5 DC C1 DD 9A D2 36 13 21 02 90   // .;.........6.!..
                0B 72 3C F9 80 95 7F C4 E1 77 10 8F C6 07 77 4F   // .r<......w....wO
                29 E8 32 0E 92 EA 05 EC E4 E8 21 C0 A5 EF E8 F1   // ).2.......!.....
                64 5C 4C 0C 93 C1 AB 99 28 5D 62 2C AA 65 2C 1D   // d\L.....(]b,.e,.
                FA D6 3D 74 5D 6F 2D E5 F1 7E 5E AF 0F C4 96 3D   // ..=t]o-..~^....=
                26 1C 8A 12 43 65 18 20 6D C0 93 34 4D 5A D2 93 ) // &...Ce. m..4MZ..
  .hash algorithm 0x00008004
  .ver 14:0:0:0
}
.module Microsoft.VisualStudio.Debugger.Metadata.dll
// MVID: {B8BC98E7-85E4-411C-B5FE-076AE11026F2}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02550000


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.Type
       extends Microsoft.VisualStudio.Debugger.Metadata.MemberInfo
{
  .class auto ansi nested public ExplicitInterfaceInfo
         extends [mscorlib]System.Object
  {
  } // end of class ExplicitInterfaceInfo

  .field public static initonly char Delimiter
  .field public static initonly class Microsoft.VisualStudio.Debugger.Metadata.Type[] EmptyTypes
  .method public hidebysig newslot specialname abstract virtual 
          instance string  get_FullName() cil managed
  {
  } // end of method Type::get_FullName

  .method public hidebysig newslot specialname abstract virtual 
          instance string  get_Namespace() cil managed
  {
  } // end of method Type::get_Namespace

  .method public hidebysig newslot specialname abstract virtual 
          instance string  get_AssemblyQualifiedName() cil managed
  {
  } // end of method Type::get_AssemblyQualifiedName

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Assembly 
          get_Assembly() cil managed
  {
  } // end of method Type::get_Assembly

  .method public hidebysig specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Module 
          get_Module() cil managed
  {
  } // end of method Type::get_Module

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_BaseType() cil managed
  {
  } // end of method Type::get_BaseType

  .method public hidebysig specialname instance bool 
          get_IsClass() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsClass

  .method public hidebysig specialname instance bool 
          get_IsInterface() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsInterface

  .method public hidebysig abstract virtual 
          instance bool  Equals(object objOther) cil managed
  {
  } // end of method Type::Equals

  .method public hidebysig virtual instance int32 
          GetHashCode() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetHashCode

  .method public hidebysig newslot abstract virtual 
          instance bool  Equals(class Microsoft.VisualStudio.Debugger.Metadata.Type o) cil managed
  {
  } // end of method Type::Equals

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          MakeArrayType() cil managed
  {
  } // end of method Type::MakeArrayType

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          MakeArrayType(int32 rank) cil managed
  {
  } // end of method Type::MakeArrayType

  .method public hidebysig newslot abstract virtual 
          instance int32  GetArrayRank() cil managed
  {
  } // end of method Type::GetArrayRank

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          MakeByRefType() cil managed
  {
  } // end of method Type::MakeByRefType

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          MakePointerType() cil managed
  {
  } // end of method Type::MakePointerType

  .method family hidebysig newslot abstract virtual 
          instance bool  IsArrayImpl() cil managed
  {
  } // end of method Type::IsArrayImpl

  .method public hidebysig specialname instance bool 
          get_IsArray() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsArray

  .method public hidebysig specialname instance bool 
          get_IsByRef() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsByRef

  .method family hidebysig newslot abstract virtual 
          instance bool  IsByRefImpl() cil managed
  {
  } // end of method Type::IsByRefImpl

  .method public hidebysig newslot specialname virtual 
          instance bool  get_IsEnum() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsEnum

  .method public hidebysig specialname instance bool 
          get_IsPointer() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsPointer

  .method family hidebysig newslot abstract virtual 
          instance bool  IsPointerImpl() cil managed
  {
  } // end of method Type::IsPointerImpl

  .method public hidebysig specialname instance bool 
          get_IsValueType() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsValueType

  .method family hidebysig newslot virtual 
          instance bool  IsValueTypeImpl() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::IsValueTypeImpl

  .method public hidebysig newslot specialname virtual 
          instance bool  get_IsSerializable() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsSerializable

  .method public hidebysig specialname instance bool 
          get_IsNested() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsNested

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetElementType() cil managed
  {
  } // end of method Type::GetElementType

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetGenericArguments() cil managed
  {
  } // end of method Type::GetGenericArguments

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetGenericParameterConstraints() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetGenericParameterConstraints

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetGenericTypeDefinition() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetGenericTypeDefinition

  .method public hidebysig newslot specialname virtual 
          instance bool  get_IsGenericTypeDefinition() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsGenericTypeDefinition

  .method public hidebysig newslot specialname virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodBase 
          get_DeclaringMethod() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_DeclaringMethod

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo[] 
          GetMethods(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags 'flags') cil managed
  {
  } // end of method Type::GetMethods

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo[] 
          GetMethods() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMethods

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type/ExplicitInterfaceInfo[] 
          GetExplicitInterfaceImplementations() cil managed
  {
  } // end of method Type::GetExplicitInterfaceImplementations

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethod(string name,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                    class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                    valuetype [mscorlib]System.Reflection.CallingConventions callConvention,
                    class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethod(string name,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                    class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                    class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethod(string name,
                    class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethod(string name,
                    class Microsoft.VisualStudio.Debugger.Metadata.Type[] types) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethod(string name,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethod(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMethod

  .method family hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethodImpl(string name,
                        valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                        class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                        valuetype [mscorlib]System.Reflection.CallingConventions callConvention,
                        class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                        valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
  } // end of method Type::GetMethodImpl

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo[] 
          GetFields(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags 'flags') cil managed
  {
  } // end of method Type::GetFields

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo[] 
          GetFields() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetFields

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo 
          GetField(string name,
                   valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
  } // end of method Type::GetField

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo 
          GetField(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetField

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo[] 
          GetProperties(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags 'flags') cil managed
  {
  } // end of method Type::GetProperties

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo[] 
          GetProperties() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetProperties

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo 
          GetProperty(string name,
                      valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                      class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type returnType,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                      valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetProperty

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo 
          GetProperty(string name,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type returnType,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                      valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetProperty

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo 
          GetProperty(string name,
                      valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetProperty

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo 
          GetProperty(string name,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type returnType,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type[] types) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetProperty

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo 
          GetProperty(string name,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type[] types) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetProperty

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo 
          GetProperty(string name,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type returnType) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetProperty

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo 
          GetProperty(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetProperty

  .method family hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo 
          GetPropertyImpl(string name,
                          valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                          class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                          class Microsoft.VisualStudio.Debugger.Metadata.Type returnType,
                          class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                          valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
  } // end of method Type::GetPropertyImpl

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.EventInfo[] 
          GetEvents(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags 'flags') cil managed
  {
  } // end of method Type::GetEvents

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.EventInfo[] 
          GetEvents() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetEvents

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.EventInfo 
          GetEvent(string name,
                   valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags 'flags') cil managed
  {
  } // end of method Type::GetEvent

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.EventInfo 
          GetEvent(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetEvent

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_UnderlyingSystemType() cil managed
  {
  } // end of method Type::get_UnderlyingSystemType

  .method public hidebysig newslot abstract virtual 
          instance bool  IsAssignableFrom(class Microsoft.VisualStudio.Debugger.Metadata.Type c) cil managed
  {
  } // end of method Type::IsAssignableFrom

  .method public hidebysig newslot abstract virtual 
          instance bool  IsInstanceOfType(object o) cil managed
  {
  } // end of method Type::IsInstanceOfType

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          MakeGenericType(class Microsoft.VisualStudio.Debugger.Metadata.Type[] argTypes) cil managed
  {
  } // end of method Type::MakeGenericType

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetNestedTypes() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetNestedTypes

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetNestedTypes(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
  } // end of method Type::GetNestedTypes

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetNestedType(string name,
                        valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
  } // end of method Type::GetNestedType

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetNestedType(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetNestedType

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo[] 
          GetConstructors(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
  } // end of method Type::GetConstructors

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo[] 
          GetConstructors() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetConstructors

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetInterface(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetInterface

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetInterface(string name,
                       bool ignoreCase) cil managed
  {
  } // end of method Type::GetInterface

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetInterfaces() cil managed
  {
  } // end of method Type::GetInterfaces

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetInterfacesOnType() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetInterfacesOnType

  .method public hidebysig newslot virtual 
          instance valuetype [mscorlib]System.Reflection.InterfaceMapping 
          GetInterfaceMap(class Microsoft.VisualStudio.Debugger.Metadata.Type interfaceType) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetInterfaceMap

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          FindInterfaces(class Microsoft.VisualStudio.Debugger.Metadata.TypeFilter 'filter',
                         object filterCriteria) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::FindInterfaces

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo 
          GetConstructor(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                         class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                         valuetype [mscorlib]System.Reflection.CallingConventions callConvention,
                         class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                         valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetConstructor

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo 
          GetConstructor(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                         class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                         class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                         valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetConstructor

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo 
          GetConstructor(class Microsoft.VisualStudio.Debugger.Metadata.Type[] types) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetConstructor

  .method family hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo 
          GetConstructorImpl(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                             class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                             valuetype [mscorlib]System.Reflection.CallingConventions callConvention,
                             class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                             valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
  } // end of method Type::GetConstructorImpl

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo[] 
          GetMembers() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMembers

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo[] 
          GetMembers(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
  } // end of method Type::GetMembers

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo[] 
          GetMember(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMember

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo[] 
          GetMember(string name,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMember

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo[] 
          GetMember(string name,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes 'type',
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetMember

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo[] 
          GetDefaultMembers() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetDefaultMembers

  .method public hidebysig specialname instance valuetype [mscorlib]System.Reflection.TypeAttributes 
          get_Attributes() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_Attributes

  .method family hidebysig newslot abstract virtual 
          instance valuetype [mscorlib]System.Reflection.TypeAttributes 
          GetAttributeFlagsImpl() cil managed
  {
  } // end of method Type::GetAttributeFlagsImpl

  .method public hidebysig specialname instance bool 
          get_IsAbstract() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsAbstract

  .method public hidebysig specialname instance bool 
          get_IsSealed() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsSealed

  .method public hidebysig specialname instance bool 
          get_IsSpecialName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsSpecialName

  .method public hidebysig specialname instance bool 
          get_IsImport() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsImport

  .method public hidebysig specialname instance bool 
          get_IsAnsiClass() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsAnsiClass

  .method public hidebysig specialname instance bool 
          get_IsUnicodeClass() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsUnicodeClass

  .method public hidebysig specialname instance bool 
          get_IsAutoClass() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsAutoClass

  .method public hidebysig specialname instance bool 
          get_IsNotPublic() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsNotPublic

  .method public hidebysig specialname instance bool 
          get_IsPublic() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsPublic

  .method public hidebysig specialname instance bool 
          get_IsNestedPublic() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsNestedPublic

  .method public hidebysig specialname instance bool 
          get_IsNestedPrivate() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsNestedPrivate

  .method public hidebysig specialname instance bool 
          get_IsNestedFamily() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsNestedFamily

  .method public hidebysig specialname instance bool 
          get_IsNestedAssembly() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsNestedAssembly

  .method public hidebysig specialname instance bool 
          get_IsNestedFamANDAssem() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsNestedFamANDAssem

  .method public hidebysig specialname instance bool 
          get_IsNestedFamORAssem() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsNestedFamORAssem

  .method public hidebysig specialname instance bool 
          get_IsAutoLayout() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsAutoLayout

  .method family hidebysig newslot abstract virtual 
          instance bool  IsPrimitiveImpl() cil managed
  {
  } // end of method Type::IsPrimitiveImpl

  .method public hidebysig specialname instance bool 
          get_IsPrimitive() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsPrimitive

  .method public hidebysig newslot specialname virtual 
          instance bool  get_IsGenericType() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsGenericType

  .method public hidebysig newslot abstract virtual 
          instance bool  IsSubclassOf(class Microsoft.VisualStudio.Debugger.Metadata.Type c) cil managed
  {
  } // end of method Type::IsSubclassOf

  .method public hidebysig newslot specialname virtual 
          instance bool  get_IsGenericParameter() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsGenericParameter

  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_ContainsGenericParameters() cil managed
  {
  } // end of method Type::get_ContainsGenericParameters

  .method public hidebysig specialname instance bool 
          get_IsCOMObject() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsCOMObject

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Guid 
          get_GUID() cil managed
  {
  } // end of method Type::get_GUID

  .method public hidebysig specialname instance bool 
          get_HasElementType() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_HasElementType

  .method family hidebysig newslot abstract virtual 
          instance bool  HasElementTypeImpl() cil managed
  {
  } // end of method Type::HasElementTypeImpl

  .method family hidebysig newslot abstract virtual 
          instance bool  IsCOMObjectImpl() cil managed
  {
  } // end of method Type::IsCOMObjectImpl

  .method public hidebysig specialname instance bool 
          get_IsContextful() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsContextful

  .method public hidebysig specialname instance bool 
          get_IsMarshalByRef() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsMarshalByRef

  .method family hidebysig newslot abstract virtual 
          instance bool  IsContextfulImpl() cil managed
  {
  } // end of method Type::IsContextfulImpl

  .method family hidebysig newslot abstract virtual 
          instance bool  IsMarshalByRefImpl() cil managed
  {
  } // end of method Type::IsMarshalByRefImpl

  .method public hidebysig newslot abstract virtual 
          instance object  InvokeMember(string name,
                                        valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags invokeAttr,
                                        class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                                        object target,
                                        object[] args,
                                        valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers,
                                        class [mscorlib]System.Globalization.CultureInfo culture,
                                        string[] namedParameters) cil managed
  {
  } // end of method Type::InvokeMember

  .method public hidebysig newslot specialname virtual 
          instance class [mscorlib]System.Runtime.InteropServices.StructLayoutAttribute 
          get_StructLayoutAttribute() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_StructLayoutAttribute

  .method public hidebysig static valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode 
          GetTypeCode(class Microsoft.VisualStudio.Debugger.Metadata.Type 'type') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::GetTypeCode

  .method family hidebysig newslot abstract virtual 
          instance valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode 
          GetTypeCodeImpl() cil managed
  {
  } // end of method Type::GetTypeCodeImpl

  .method public hidebysig newslot specialname virtual 
          instance valuetype [mscorlib]System.RuntimeTypeHandle 
          get_TypeHandle() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_TypeHandle

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo[] 
          FindMembers(valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes memberType,
                      valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                      class Microsoft.VisualStudio.Debugger.Metadata.MemberFilter 'filter',
                      object filterCriteria) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::FindMembers

  .method public hidebysig specialname instance bool 
          get_IsExplicitLayout() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsExplicitLayout

  .method public hidebysig specialname instance bool 
          get_IsLayoutSequential() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsLayoutSequential

  .method public hidebysig specialname instance bool 
          get_IsVisible() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_IsVisible

  .method public hidebysig newslot specialname virtual 
          instance valuetype [mscorlib]System.Reflection.GenericParameterAttributes 
          get_GenericParameterAttributes() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_GenericParameterAttributes

  .method public hidebysig newslot specialname virtual 
          instance int32  get_GenericParameterPosition() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::get_GenericParameterPosition

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Type::.ctor

  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Assembly
          Assembly()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Assembly Microsoft.VisualStudio.Debugger.Metadata.Type::get_Assembly()
  } // end of property Type::Assembly
  .property instance string AssemblyQualifiedName()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Type::get_AssemblyQualifiedName()
  } // end of property Type::AssemblyQualifiedName
  .property instance valuetype [mscorlib]System.Reflection.TypeAttributes
          Attributes()
  {
    .get instance valuetype [mscorlib]System.Reflection.TypeAttributes Microsoft.VisualStudio.Debugger.Metadata.Type::get_Attributes()
  } // end of property Type::Attributes
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          BaseType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.Type::get_BaseType()
  } // end of property Type::BaseType
  .property instance bool ContainsGenericParameters()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_ContainsGenericParameters()
  } // end of property Type::ContainsGenericParameters
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.MethodBase
          DeclaringMethod()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.MethodBase Microsoft.VisualStudio.Debugger.Metadata.Type::get_DeclaringMethod()
  } // end of property Type::DeclaringMethod
  .property instance string FullName()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Type::get_FullName()
  } // end of property Type::FullName
  .property instance valuetype [mscorlib]System.Guid
          GUID()
  {
    .get instance valuetype [mscorlib]System.Guid Microsoft.VisualStudio.Debugger.Metadata.Type::get_GUID()
  } // end of property Type::GUID
  .property instance valuetype [mscorlib]System.Reflection.GenericParameterAttributes
          GenericParameterAttributes()
  {
    .get instance valuetype [mscorlib]System.Reflection.GenericParameterAttributes Microsoft.VisualStudio.Debugger.Metadata.Type::get_GenericParameterAttributes()
  } // end of property Type::GenericParameterAttributes
  .property instance int32 GenericParameterPosition()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.Type::get_GenericParameterPosition()
  } // end of property Type::GenericParameterPosition
  .property instance bool HasElementType()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_HasElementType()
  } // end of property Type::HasElementType
  .property instance bool IsAbstract()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsAbstract()
  } // end of property Type::IsAbstract
  .property instance bool IsAnsiClass()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsAnsiClass()
  } // end of property Type::IsAnsiClass
  .property instance bool IsArray()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsArray()
  } // end of property Type::IsArray
  .property instance bool IsAutoClass()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsAutoClass()
  } // end of property Type::IsAutoClass
  .property instance bool IsAutoLayout()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsAutoLayout()
  } // end of property Type::IsAutoLayout
  .property instance bool IsByRef()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsByRef()
  } // end of property Type::IsByRef
  .property instance bool IsCOMObject()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsCOMObject()
  } // end of property Type::IsCOMObject
  .property instance bool IsClass()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsClass()
  } // end of property Type::IsClass
  .property instance bool IsContextful()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsContextful()
  } // end of property Type::IsContextful
  .property instance bool IsEnum()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsEnum()
  } // end of property Type::IsEnum
  .property instance bool IsExplicitLayout()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsExplicitLayout()
  } // end of property Type::IsExplicitLayout
  .property instance bool IsGenericParameter()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsGenericParameter()
  } // end of property Type::IsGenericParameter
  .property instance bool IsGenericType()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsGenericType()
  } // end of property Type::IsGenericType
  .property instance bool IsGenericTypeDefinition()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsGenericTypeDefinition()
  } // end of property Type::IsGenericTypeDefinition
  .property instance bool IsImport()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsImport()
  } // end of property Type::IsImport
  .property instance bool IsInterface()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsInterface()
  } // end of property Type::IsInterface
  .property instance bool IsLayoutSequential()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsLayoutSequential()
  } // end of property Type::IsLayoutSequential
  .property instance bool IsMarshalByRef()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsMarshalByRef()
  } // end of property Type::IsMarshalByRef
  .property instance bool IsNested()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsNested()
  } // end of property Type::IsNested
  .property instance bool IsNestedAssembly()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsNestedAssembly()
  } // end of property Type::IsNestedAssembly
  .property instance bool IsNestedFamANDAssem()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsNestedFamANDAssem()
  } // end of property Type::IsNestedFamANDAssem
  .property instance bool IsNestedFamORAssem()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsNestedFamORAssem()
  } // end of property Type::IsNestedFamORAssem
  .property instance bool IsNestedFamily()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsNestedFamily()
  } // end of property Type::IsNestedFamily
  .property instance bool IsNestedPrivate()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsNestedPrivate()
  } // end of property Type::IsNestedPrivate
  .property instance bool IsNestedPublic()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsNestedPublic()
  } // end of property Type::IsNestedPublic
  .property instance bool IsNotPublic()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsNotPublic()
  } // end of property Type::IsNotPublic
  .property instance bool IsPointer()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsPointer()
  } // end of property Type::IsPointer
  .property instance bool IsPrimitive()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsPrimitive()
  } // end of property Type::IsPrimitive
  .property instance bool IsPublic()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsPublic()
  } // end of property Type::IsPublic
  .property instance bool IsSealed()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsSealed()
  } // end of property Type::IsSealed
  .property instance bool IsSerializable()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsSerializable()
  } // end of property Type::IsSerializable
  .property instance bool IsSpecialName()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsSpecialName()
  } // end of property Type::IsSpecialName
  .property instance bool IsUnicodeClass()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsUnicodeClass()
  } // end of property Type::IsUnicodeClass
  .property instance bool IsValueType()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsValueType()
  } // end of property Type::IsValueType
  .property instance bool IsVisible()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Type::get_IsVisible()
  } // end of property Type::IsVisible
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Module
          Module()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Module Microsoft.VisualStudio.Debugger.Metadata.Type::get_Module()
  } // end of property Type::Module
  .property instance string Namespace()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Type::get_Namespace()
  } // end of property Type::Namespace
  .property instance class [mscorlib]System.Runtime.InteropServices.StructLayoutAttribute
          StructLayoutAttribute()
  {
    .get instance class [mscorlib]System.Runtime.InteropServices.StructLayoutAttribute Microsoft.VisualStudio.Debugger.Metadata.Type::get_StructLayoutAttribute()
  } // end of property Type::StructLayoutAttribute
  .property instance valuetype [mscorlib]System.RuntimeTypeHandle
          TypeHandle()
  {
    .get instance valuetype [mscorlib]System.RuntimeTypeHandle Microsoft.VisualStudio.Debugger.Metadata.Type::get_TypeHandle()
  } // end of property Type::TypeHandle
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          UnderlyingSystemType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.Type::get_UnderlyingSystemType()
  } // end of property Type::UnderlyingSystemType
} // end of class Microsoft.VisualStudio.Debugger.Metadata.Type

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Reflection.ParameterAttributes 
          get_Attributes() cil managed
  {
  } // end of method ParameterInfo::get_Attributes

  .method public hidebysig newslot specialname abstract virtual 
          instance string  get_Name() cil managed
  {
  } // end of method ParameterInfo::get_Name

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Position() cil managed
  {
  } // end of method ParameterInfo::get_Position

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_ParameterType() cil managed
  {
  } // end of method ParameterInfo::get_ParameterType

  .method public hidebysig newslot specialname virtual 
          instance int32  get_MetadataToken() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ParameterInfo::get_MetadataToken

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo 
          get_Member() cil managed
  {
  } // end of method ParameterInfo::get_Member

  .method public hidebysig specialname instance bool 
          get_IsIn() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ParameterInfo::get_IsIn

  .method public hidebysig specialname instance bool 
          get_IsLcid() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ParameterInfo::get_IsLcid

  .method public hidebysig specialname instance bool 
          get_IsOptional() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ParameterInfo::get_IsOptional

  .method public hidebysig specialname instance bool 
          get_IsOut() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ParameterInfo::get_IsOut

  .method public hidebysig specialname instance bool 
          get_IsRetval() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ParameterInfo::get_IsRetval

  .method public hidebysig newslot specialname abstract virtual 
          instance object  get_DefaultValue() cil managed
  {
  } // end of method ParameterInfo::get_DefaultValue

  .method public hidebysig newslot specialname abstract virtual 
          instance object  get_RawDefaultValue() cil managed
  {
  } // end of method ParameterInfo::get_RawDefaultValue

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData> 
          GetCustomAttributesData() cil managed
  {
  } // end of method ParameterInfo::GetCustomAttributesData

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetOptionalCustomModifiers() cil managed
  {
  } // end of method ParameterInfo::GetOptionalCustomModifiers

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetRequiredCustomModifiers() cil managed
  {
  } // end of method ParameterInfo::GetRequiredCustomModifiers

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ParameterInfo::.ctor

  .property instance valuetype [mscorlib]System.Reflection.ParameterAttributes
          Attributes()
  {
    .get instance valuetype [mscorlib]System.Reflection.ParameterAttributes Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_Attributes()
  } // end of property ParameterInfo::Attributes
  .property instance object DefaultValue()
  {
    .get instance object Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_DefaultValue()
  } // end of property ParameterInfo::DefaultValue
  .property instance bool IsIn()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_IsIn()
  } // end of property ParameterInfo::IsIn
  .property instance bool IsLcid()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_IsLcid()
  } // end of property ParameterInfo::IsLcid
  .property instance bool IsOptional()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_IsOptional()
  } // end of property ParameterInfo::IsOptional
  .property instance bool IsOut()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_IsOut()
  } // end of property ParameterInfo::IsOut
  .property instance bool IsRetval()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_IsRetval()
  } // end of property ParameterInfo::IsRetval
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo
          Member()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_Member()
  } // end of property ParameterInfo::Member
  .property instance int32 MetadataToken()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_MetadataToken()
  } // end of property ParameterInfo::MetadataToken
  .property instance string Name()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_Name()
  } // end of property ParameterInfo::Name
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          ParameterType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_ParameterType()
  } // end of property ParameterInfo::ParameterType
  .property instance int32 Position()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_Position()
  } // end of property ParameterInfo::Position
  .property instance object RawDefaultValue()
  {
    .get instance object Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo::get_RawDefaultValue()
  } // end of property ParameterInfo::RawDefaultValue
} // end of class Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.MemberInfo
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes 
          get_MemberType() cil managed
  {
  } // end of method MemberInfo::get_MemberType

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_DeclaringType() cil managed
  {
  } // end of method MemberInfo::get_DeclaringType

  .method public hidebysig newslot specialname abstract virtual 
          instance string  get_Name() cil managed
  {
  } // end of method MemberInfo::get_Name

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_MetadataToken() cil managed
  {
  } // end of method MemberInfo::get_MetadataToken

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Module 
          get_Module() cil managed
  {
  } // end of method MemberInfo::get_Module

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_ReflectedType() cil managed
  {
  } // end of method MemberInfo::get_ReflectedType

  .method public hidebysig newslot abstract virtual 
          instance object[]  GetCustomAttributes(bool inherit) cil managed
  {
  } // end of method MemberInfo::GetCustomAttributes

  .method public hidebysig newslot abstract virtual 
          instance object[]  GetCustomAttributes(class Microsoft.VisualStudio.Debugger.Metadata.Type attributeType,
                                                 bool inherit) cil managed
  {
  } // end of method MemberInfo::GetCustomAttributes

  .method public hidebysig newslot abstract virtual 
          instance bool  IsDefined(class Microsoft.VisualStudio.Debugger.Metadata.Type attributeType,
                                   bool inherit) cil managed
  {
  } // end of method MemberInfo::IsDefined

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData> 
          GetCustomAttributesData() cil managed
  {
  } // end of method MemberInfo::GetCustomAttributesData

  .method public hidebysig newslot abstract virtual 
          instance bool  IsEquivalentTo(class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo other) cil managed
  {
  } // end of method MemberInfo::IsEquivalentTo

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MemberInfo::.ctor

  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          DeclaringType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.MemberInfo::get_DeclaringType()
  } // end of property MemberInfo::DeclaringType
  .property instance valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes
          MemberType()
  {
    .get instance valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes Microsoft.VisualStudio.Debugger.Metadata.MemberInfo::get_MemberType()
  } // end of property MemberInfo::MemberType
  .property instance int32 MetadataToken()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.MemberInfo::get_MetadataToken()
  } // end of property MemberInfo::MetadataToken
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Module
          Module()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Module Microsoft.VisualStudio.Debugger.Metadata.MemberInfo::get_Module()
  } // end of property MemberInfo::Module
  .property instance string Name()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.MemberInfo::get_Name()
  } // end of property MemberInfo::Name
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          ReflectedType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.MemberInfo::get_ReflectedType()
  } // end of property MemberInfo::ReflectedType
} // end of class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.MethodInfo
       extends Microsoft.VisualStudio.Debugger.Metadata.MethodBase
{
  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_ReturnType() cil managed
  {
  } // end of method MethodInfo::get_ReturnType

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          MakeGenericMethod(class Microsoft.VisualStudio.Debugger.Metadata.Type[] types) cil managed
  {
  } // end of method MethodInfo::MakeGenericMethod

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetGenericMethodDefinition() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodInfo::GetGenericMethodDefinition

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetBaseDefinition() cil managed
  {
  } // end of method MethodInfo::GetBaseDefinition

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo 
          get_ReturnParameter() cil managed
  {
  } // end of method MethodInfo::get_ReturnParameter

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo[] 
          GetExplicitInterfacesImplemented() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodInfo::GetExplicitInterfacesImplemented

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodInfo::.ctor

  .property instance class Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo
          ReturnParameter()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo Microsoft.VisualStudio.Debugger.Metadata.MethodInfo::get_ReturnParameter()
  } // end of property MethodInfo::ReturnParameter
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          ReturnType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.MethodInfo::get_ReturnType()
  } // end of property MethodInfo::ReturnType
} // end of class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.MethodBody
       extends [mscorlib]System.Object
{
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBody::.ctor

  .method public hidebysig newslot specialname virtual 
          instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause> 
          get_ExceptionHandlingClauses() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBody::get_ExceptionHandlingClauses

  .method public hidebysig newslot specialname virtual 
          instance bool  get_InitLocals() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBody::get_InitLocals

  .method public hidebysig newslot specialname virtual 
          instance int32  get_LocalSignatureMetadataToken() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBody::get_LocalSignatureMetadataToken

  .method public hidebysig newslot specialname virtual 
          instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.LocalVariableInfo> 
          get_LocalVariables() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBody::get_LocalVariables

  .method public hidebysig newslot specialname virtual 
          instance int32  get_MaxStackSize() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBody::get_MaxStackSize

  .method public hidebysig newslot virtual 
          instance uint8[]  GetILAsByteArray() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBody::GetILAsByteArray

  .property instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause>
          ExceptionHandlingClauses()
  {
    .get instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause> Microsoft.VisualStudio.Debugger.Metadata.MethodBody::get_ExceptionHandlingClauses()
  } // end of property MethodBody::ExceptionHandlingClauses
  .property instance bool InitLocals()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBody::get_InitLocals()
  } // end of property MethodBody::InitLocals
  .property instance int32 LocalSignatureMetadataToken()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.MethodBody::get_LocalSignatureMetadataToken()
  } // end of property MethodBody::LocalSignatureMetadataToken
  .property instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.LocalVariableInfo>
          LocalVariables()
  {
    .get instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.LocalVariableInfo> Microsoft.VisualStudio.Debugger.Metadata.MethodBody::get_LocalVariables()
  } // end of property MethodBody::LocalVariables
  .property instance int32 MaxStackSize()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.MethodBody::get_MaxStackSize()
  } // end of property MethodBody::MaxStackSize
} // end of class Microsoft.VisualStudio.Debugger.Metadata.MethodBody

.class public auto ansi serializable sealed Microsoft.VisualStudio.Debugger.Metadata.BindingFlags
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags CreateInstance = int32(0x00000200)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags DeclaredOnly = int32(0x00000002)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags Default = int32(0x00000000)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags ExactBinding = int32(0x00010000)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags FlattenHierarchy = int32(0x00000040)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags GetField = int32(0x00000400)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags GetProperty = int32(0x00001000)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags IgnoreCase = int32(0x00000001)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags Instance = int32(0x00000004)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags InvokeMethod = int32(0x00000100)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags NonPublic = int32(0x00000020)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags Public = int32(0x00000010)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags SetField = int32(0x00000800)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags SetProperty = int32(0x00002000)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags Static = int32(0x00000008)
} // end of class Microsoft.VisualStudio.Debugger.Metadata.BindingFlags

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.Binder
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodBase 
          BindToMethod(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                       class Microsoft.VisualStudio.Debugger.Metadata.MethodBase[] match,
                       object[]& args,
                       valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers,
                       class [mscorlib]System.Globalization.CultureInfo culture,
                       string[] names,
                       [out] object& state) cil managed
  {
  } // end of method Binder::BindToMethod

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo 
          BindToField(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                      class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo[] match,
                      object 'value',
                      class [mscorlib]System.Globalization.CultureInfo culture) cil managed
  {
  } // end of method Binder::BindToField

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodBase 
          SelectMethod(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                       class Microsoft.VisualStudio.Debugger.Metadata.MethodBase[] match,
                       class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                       valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
  } // end of method Binder::SelectMethod

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo 
          SelectProperty(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                         class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo[] match,
                         class Microsoft.VisualStudio.Debugger.Metadata.Type returnType,
                         class Microsoft.VisualStudio.Debugger.Metadata.Type[] indexes,
                         valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
  } // end of method Binder::SelectProperty

  .method public hidebysig newslot abstract virtual 
          instance object  ChangeType(object 'value',
                                      class Microsoft.VisualStudio.Debugger.Metadata.Type 'type',
                                      class [mscorlib]System.Globalization.CultureInfo culture) cil managed
  {
  } // end of method Binder::ChangeType

  .method public hidebysig newslot abstract virtual 
          instance void  ReorderArgumentArray(object[]& args,
                                              object state) cil managed
  {
  } // end of method Binder::ReorderArgumentArray

  .method public hidebysig newslot virtual 
          instance bool  CanChangeType(object 'value',
                                       class Microsoft.VisualStudio.Debugger.Metadata.Type 'type',
                                       class [mscorlib]System.Globalization.CultureInfo culture) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Binder::CanChangeType

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Binder::.ctor

} // end of class Microsoft.VisualStudio.Debugger.Metadata.Binder

.class public auto ansi serializable sealed Microsoft.VisualStudio.Debugger.Metadata.MemberTypes
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes All = int32(0x000000BF)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes Constructor = int32(0x00000001)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes Custom = int32(0x00000040)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes Event = int32(0x00000002)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes Field = int32(0x00000004)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes Method = int32(0x00000008)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes NestedType = int32(0x00000080)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes Property = int32(0x00000010)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.MemberTypes TypeInfo = int32(0x00000020)
} // end of class Microsoft.VisualStudio.Debugger.Metadata.MemberTypes

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.Module
       extends [mscorlib]System.Object
{
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::.ctor

  .method public hidebysig newslot specialname virtual 
          instance string  get_Name() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::get_Name

  .method public hidebysig newslot virtual 
          instance void  GetPEKind([out] valuetype Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds& peKind,
                                   [out] valuetype Microsoft.VisualStudio.Debugger.Metadata.ImageFileMachine& machine) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetPEKind

  .method public hidebysig newslot specialname virtual 
          instance int32  get_MDStreamVersion() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::get_MDStreamVersion

  .method public hidebysig newslot specialname virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Assembly 
          get_Assembly() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::get_Assembly

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetType(string className) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetType

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetType(string className,
                  bool ignoreCase) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetType

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetType(string className,
                  bool throwOnError,
                  bool ignoreCase) cil managed
  {
  } // end of method Module::GetType

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetTypes() cil managed
  {
  } // end of method Module::GetTypes

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          FindTypes(class Microsoft.VisualStudio.Debugger.Metadata.TypeFilter 'filter',
                    object filterCriteria) cil managed
  {
  } // end of method Module::FindTypes

  .method public hidebysig newslot specialname virtual 
          instance string  get_ScopeName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::get_ScopeName

  .method public hidebysig newslot specialname virtual 
          instance valuetype [mscorlib]System.Guid 
          get_ModuleVersionId() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::get_ModuleVersionId

  .method public hidebysig newslot specialname virtual 
          instance int32  get_MetadataToken() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::get_MetadataToken

  .method public hidebysig newslot virtual 
          instance bool  IsResource() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::IsResource

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo 
          GetField(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetField

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo 
          GetField(string name,
                   valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetField

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo[] 
          GetFields() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetFields

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo[] 
          GetFields(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingFlags) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetFields

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethod(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethod(string name,
                    class Microsoft.VisualStudio.Debugger.Metadata.Type[] types) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethod(string name,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                    class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                    valuetype [mscorlib]System.Reflection.CallingConventions callConvention,
                    class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                    valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetMethod

  .method family hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetMethodImpl(string name,
                        valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingAttr,
                        class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                        valuetype [mscorlib]System.Reflection.CallingConventions callConvention,
                        class Microsoft.VisualStudio.Debugger.Metadata.Type[] types,
                        valuetype Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier[] modifiers) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetMethodImpl

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo[] 
          GetMethods() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetMethods

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo[] 
          GetMethods(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags bindingFlags) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::GetMethods

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo 
          ResolveMember(int32 metadataToken) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveMember

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo 
          ResolveMember(int32 metadataToken,
                        class Microsoft.VisualStudio.Debugger.Metadata.Type[] genericTypeArguments,
                        class Microsoft.VisualStudio.Debugger.Metadata.Type[] genericMethodArguments) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveMember

  .method public hidebysig newslot virtual 
          instance uint8[]  ResolveSignature(int32 metadataToken) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveSignature

  .method public hidebysig newslot virtual 
          instance string  ResolveString(int32 metadataToken) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveString

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          ResolveType(int32 metadataToken) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveType

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          ResolveType(int32 metadataToken,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type[] genericTypeArguments,
                      class Microsoft.VisualStudio.Debugger.Metadata.Type[] genericMethodArguments) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveType

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo 
          ResolveField(int32 metadataToken) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveField

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo 
          ResolveField(int32 metadataToken,
                       class Microsoft.VisualStudio.Debugger.Metadata.Type[] genericTypeArguments,
                       class Microsoft.VisualStudio.Debugger.Metadata.Type[] genericMethodArguments) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveField

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodBase 
          ResolveMethod(int32 metadataToken) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveMethod

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodBase 
          ResolveMethod(int32 metadataToken,
                        class Microsoft.VisualStudio.Debugger.Metadata.Type[] genericTypeArguments,
                        class Microsoft.VisualStudio.Debugger.Metadata.Type[] genericMethodArguments) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::ResolveMethod

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData> 
          GetCustomAttributesData() cil managed
  {
  } // end of method Module::GetCustomAttributesData

  .method public hidebysig newslot specialname virtual 
          instance string  get_FullyQualifiedName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Module::get_FullyQualifiedName

  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Assembly
          Assembly()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Assembly Microsoft.VisualStudio.Debugger.Metadata.Module::get_Assembly()
  } // end of property Module::Assembly
  .property instance string FullyQualifiedName()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Module::get_FullyQualifiedName()
  } // end of property Module::FullyQualifiedName
  .property instance int32 MDStreamVersion()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.Module::get_MDStreamVersion()
  } // end of property Module::MDStreamVersion
  .property instance int32 MetadataToken()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.Module::get_MetadataToken()
  } // end of property Module::MetadataToken
  .property instance valuetype [mscorlib]System.Guid
          ModuleVersionId()
  {
    .get instance valuetype [mscorlib]System.Guid Microsoft.VisualStudio.Debugger.Metadata.Module::get_ModuleVersionId()
  } // end of property Module::ModuleVersionId
  .property instance string Name()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Module::get_Name()
  } // end of property Module::Name
  .property instance string ScopeName()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Module::get_ScopeName()
  } // end of property Module::ScopeName
} // end of class Microsoft.VisualStudio.Debugger.Metadata.Module

.class public auto ansi Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData
       extends [mscorlib]System.Object
{
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeData::.ctor

  .method public hidebysig newslot specialname virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo 
          get_Constructor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeData::get_Constructor

  .method public hidebysig newslot specialname virtual 
          instance class [mscorlib]System.Collections.Generic.IList`1<valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument> 
          get_ConstructorArguments() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeData::get_ConstructorArguments

  .method public hidebysig newslot specialname virtual 
          instance class [mscorlib]System.Collections.Generic.IList`1<valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeNamedArgument> 
          get_NamedArguments() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeData::get_NamedArguments

  .property instance class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo
          Constructor()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData::get_Constructor()
  } // end of property CustomAttributeData::Constructor
  .property instance class [mscorlib]System.Collections.Generic.IList`1<valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument>
          ConstructorArguments()
  {
    .get instance class [mscorlib]System.Collections.Generic.IList`1<valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument> Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData::get_ConstructorArguments()
  } // end of property CustomAttributeData::ConstructorArguments
  .property instance class [mscorlib]System.Collections.Generic.IList`1<valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeNamedArgument>
          NamedArguments()
  {
    .get instance class [mscorlib]System.Collections.Generic.IList`1<valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeNamedArgument> Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData::get_NamedArguments()
  } // end of property CustomAttributeData::NamedArguments
} // end of class Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.Assembly
       extends [mscorlib]System.Object
{
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::.ctor

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.AssemblyName 
          GetName() cil managed
  {
  } // end of method Assembly::GetName

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.AssemblyName 
          GetName(bool copiedName) cil managed
  {
  } // end of method Assembly::GetName

  .method public hidebysig newslot specialname abstract virtual 
          instance string  get_FullName() cil managed
  {
  } // end of method Assembly::get_FullName

  .method public hidebysig newslot specialname abstract virtual 
          instance string  get_Location() cil managed
  {
  } // end of method Assembly::get_Location

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetExportedTypes() cil managed
  {
  } // end of method Assembly::GetExportedTypes

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetType(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetType

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetType(string name,
                  bool throwOnError) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetType

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          GetType(string name,
                  bool throwOnError,
                  bool ignoreCase) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetType

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetTypes() cil managed
  {
  } // end of method Assembly::GetTypes

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Module 
          GetModule(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetModule

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.Module[] 
          GetLoadedModules() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetLoadedModules

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Module[] 
          GetLoadedModules(bool getResourceModules) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetLoadedModules

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.Module[] 
          GetModules() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetModules

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Module[] 
          GetModules(bool getResourceModules) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetModules

  .method public hidebysig newslot specialname virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Module 
          get_ManifestModule() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::get_ManifestModule

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          get_EntryPoint() cil managed
  {
  } // end of method Assembly::get_EntryPoint

  .method public hidebysig newslot specialname virtual 
          instance string  get_CodeBase() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::get_CodeBase

  .method public hidebysig newslot specialname virtual 
          instance string  get_EscapedCodeBase() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::get_EscapedCodeBase

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.Generic.IList`1<class Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData> 
          GetCustomAttributesData() cil managed
  {
  } // end of method Assembly::GetCustomAttributesData

  .method public hidebysig newslot virtual 
          instance object[]  GetCustomAttributes(bool inherit) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetCustomAttributes

  .method public hidebysig newslot virtual 
          instance object[]  GetCustomAttributes(class Microsoft.VisualStudio.Debugger.Metadata.Type attributeType,
                                                 bool inherit) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetCustomAttributes

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.AssemblyName[] 
          GetReferencedAssemblies() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetReferencedAssemblies

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Assembly 
          GetSatelliteAssembly(class [mscorlib]System.Globalization.CultureInfo culture) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetSatelliteAssembly

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Assembly 
          GetSatelliteAssembly(class [mscorlib]System.Globalization.CultureInfo culture,
                               class [mscorlib]System.Version version) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetSatelliteAssembly

  .method public hidebysig newslot specialname virtual 
          instance bool  get_GlobalAssemblyCache() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::get_GlobalAssemblyCache

  .method public hidebysig newslot specialname virtual 
          instance int64  get_HostContext() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::get_HostContext

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IO.Stream 
          GetManifestResourceStream(class Microsoft.VisualStudio.Debugger.Metadata.Type 'type',
                                    string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetManifestResourceStream

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IO.Stream 
          GetManifestResourceStream(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetManifestResourceStream

  .method public hidebysig newslot virtual 
          instance string[]  GetManifestResourceNames() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetManifestResourceNames

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.Reflection.ManifestResourceInfo 
          GetManifestResourceInfo(string resourceName) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::GetManifestResourceInfo

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Assembly::ToString

  .property instance string CodeBase()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Assembly::get_CodeBase()
  } // end of property Assembly::CodeBase
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo
          EntryPoint()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo Microsoft.VisualStudio.Debugger.Metadata.Assembly::get_EntryPoint()
  } // end of property Assembly::EntryPoint
  .property instance string EscapedCodeBase()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Assembly::get_EscapedCodeBase()
  } // end of property Assembly::EscapedCodeBase
  .property instance string FullName()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Assembly::get_FullName()
  } // end of property Assembly::FullName
  .property instance bool GlobalAssemblyCache()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.Assembly::get_GlobalAssemblyCache()
  } // end of property Assembly::GlobalAssemblyCache
  .property instance int64 HostContext()
  {
    .get instance int64 Microsoft.VisualStudio.Debugger.Metadata.Assembly::get_HostContext()
  } // end of property Assembly::HostContext
  .property instance string Location()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.Assembly::get_Location()
  } // end of property Assembly::Location
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Module
          ManifestModule()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Module Microsoft.VisualStudio.Debugger.Metadata.Assembly::get_ManifestModule()
  } // end of property Assembly::ManifestModule
} // end of class Microsoft.VisualStudio.Debugger.Metadata.Assembly

.class public auto ansi System.Reflection.Adds.MetadataFile
       extends [mscorlib]System.Object
       implements [mscorlib]System.IDisposable
{
  .permissionset demand
             = {class 'System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' = {property bool 'UnmanagedCode' = bool(true)}}
  .method family hidebysig virtual instance void 
          Finalize() cil managed
  {
    .override [mscorlib]System.Object::Finalize
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::Finalize

  .method public hidebysig newslot virtual final 
          instance void  Dispose() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::Dispose

  .method family hidebysig newslot virtual 
          instance void  Dispose(bool disposing) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::Dispose

  .method public hidebysig newslot specialname virtual 
          instance string  get_FilePath() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::get_FilePath

  .method public hidebysig newslot virtual 
          instance uint8[]  ReadRva(int64 rva,
                                    int32 countBytes) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::ReadRva

  .method public hidebysig newslot virtual 
          instance uint8[]  ReadResource(int64 offset) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::ReadResource

  .method public hidebysig instance uint8[] 
          ReadEmbeddedBlob(valuetype System.Reflection.Adds.EmbeddedBlobPointer pointer,
                           int32 countBytes) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::ReadEmbeddedBlob

  .method family hidebysig newslot virtual 
          instance void  ValidateRange(native int ptr,
                                       int32 countBytes) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::ValidateRange

  .method public hidebysig newslot virtual 
          instance valuetype System.Reflection.Adds.Token 
          ReadEntryPointToken() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::ReadEntryPointToken

  .method public hidebysig newslot virtual 
          instance !!T  ReadRvaStruct<.ctor T>(int64 rva) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::ReadRvaStruct

  .method family hidebysig instance void 
          EnsureNotDispose() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MetadataFile::EnsureNotDispose

  .property instance string FilePath()
  {
    .get instance string System.Reflection.Adds.MetadataFile::get_FilePath()
  } // end of property MetadataFile::FilePath
} // end of class System.Reflection.Adds.MetadataFile

.class public sequential ansi sealed Microsoft.MetadataReader.AssemblyMetaData
       extends [mscorlib]System.ValueType
       implements [mscorlib]System.IDisposable
{
  .field public uint16 majorVersion
  .field public uint16 minorVersion
  .field public uint16 buildNumber
  .field public uint16 revisionNumber
  .field public native int szLocale
  .field public uint32 cbLocale
  .field public valuetype Microsoft.MetadataReader.UnusedIntPtr rdwProcessor
  .field public uint32 ulProcessor
  .field public valuetype Microsoft.MetadataReader.UnusedIntPtr rOS
  .field public uint32 ulOS
  .method public hidebysig instance void 
          Init() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyMetaData::Init

  .method public hidebysig specialname instance class [mscorlib]System.Version 
          get_Version() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyMetaData::get_Version

  .method public hidebysig specialname instance string 
          get_LocaleString() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyMetaData::get_LocaleString

  .method public hidebysig specialname instance class [mscorlib]System.Globalization.CultureInfo 
          get_Locale() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyMetaData::get_Locale

  .method public hidebysig newslot virtual final 
          instance void  Dispose() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyMetaData::Dispose

  .property instance class [mscorlib]System.Globalization.CultureInfo
          Locale()
  {
    .get instance class [mscorlib]System.Globalization.CultureInfo Microsoft.MetadataReader.AssemblyMetaData::get_Locale()
  } // end of property AssemblyMetaData::Locale
  .property instance string LocaleString()
  {
    .get instance string Microsoft.MetadataReader.AssemblyMetaData::get_LocaleString()
  } // end of property AssemblyMetaData::LocaleString
  .property instance class [mscorlib]System.Version
          Version()
  {
    .get instance class [mscorlib]System.Version Microsoft.MetadataReader.AssemblyMetaData::get_Version()
  } // end of property AssemblyMetaData::Version
} // end of class Microsoft.MetadataReader.AssemblyMetaData

.class public sequential ansi sealed Microsoft.MetadataReader.UnusedIntPtr
       extends [mscorlib]System.ValueType
{
  .method public hidebysig specialname static 
          valuetype Microsoft.MetadataReader.UnusedIntPtr 
          get_Zero() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method UnusedIntPtr::get_Zero

  .property valuetype Microsoft.MetadataReader.UnusedIntPtr
          Zero()
  {
    .get valuetype Microsoft.MetadataReader.UnusedIntPtr Microsoft.MetadataReader.UnusedIntPtr::get_Zero()
  } // end of property UnusedIntPtr::Zero
} // end of class Microsoft.MetadataReader.UnusedIntPtr

.class public auto ansi sealed Microsoft.VisualStudio.Debugger.Metadata.AssemblyName
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class [mscorlib]System.Reflection.AssemblyName assemblyName) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::.ctor

  .method public hidebysig specialname instance string 
          get_Name() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_Name

  .method public hidebysig specialname instance void 
          set_Name(string 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::set_Name

  .method public hidebysig specialname instance class [mscorlib]System.Version 
          get_Version() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_Version

  .method public hidebysig specialname instance void 
          set_Version(class [mscorlib]System.Version 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::set_Version

  .method public hidebysig specialname instance class [mscorlib]System.Globalization.CultureInfo 
          get_CultureInfo() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_CultureInfo

  .method public hidebysig specialname instance void 
          set_CultureInfo(class [mscorlib]System.Globalization.CultureInfo 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::set_CultureInfo

  .method public hidebysig specialname instance string 
          get_CultureName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_CultureName

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.AssemblyName 
          Clone() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::Clone

  .method public hidebysig specialname instance string 
          get_CodeBase() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_CodeBase

  .method public hidebysig specialname instance void 
          set_CodeBase(string 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::set_CodeBase

  .method public hidebysig specialname instance valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture 
          get_ProcessorArchitecture() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_ProcessorArchitecture

  .method public hidebysig specialname instance void 
          set_ProcessorArchitecture(valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::set_ProcessorArchitecture

  .method public hidebysig specialname instance valuetype Microsoft.VisualStudio.Debugger.Metadata.AssemblyContentType 
          get_ContentType() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_ContentType

  .method public hidebysig specialname instance void 
          set_ContentType(valuetype Microsoft.VisualStudio.Debugger.Metadata.AssemblyContentType 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::set_ContentType

  .method public hidebysig instance uint8[] 
          GetPublicKey() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::GetPublicKey

  .method public hidebysig instance void 
          SetPublicKey(uint8[] publicKey) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::SetPublicKey

  .method public hidebysig instance uint8[] 
          GetPublicKeyToken() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::GetPublicKeyToken

  .method public hidebysig instance void 
          SetPublicKeyToken(uint8[] publicKeyToken) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::SetPublicKeyToken

  .method public hidebysig specialname instance valuetype [mscorlib]System.Reflection.AssemblyNameFlags 
          get_Flags() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_Flags

  .method public hidebysig specialname instance void 
          set_Flags(valuetype [mscorlib]System.Reflection.AssemblyNameFlags 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::set_Flags

  .method public hidebysig specialname instance valuetype [mscorlib]System.Configuration.Assemblies.AssemblyHashAlgorithm 
          get_HashAlgorithm() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_HashAlgorithm

  .method public hidebysig specialname instance void 
          set_HashAlgorithm(valuetype [mscorlib]System.Configuration.Assemblies.AssemblyHashAlgorithm 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::set_HashAlgorithm

  .method public hidebysig specialname instance string 
          get_FullName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_FullName

  .method public hidebysig specialname instance class [mscorlib]System.Reflection.StrongNameKeyPair 
          get_KeyPair() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::get_KeyPair

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::ToString

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string assemblyName) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::.ctor

  .method public hidebysig static bool  ReferenceMatchesDefinition(class Microsoft.VisualStudio.Debugger.Metadata.AssemblyName reference,
                                                                   class Microsoft.VisualStudio.Debugger.Metadata.AssemblyName definition) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyName::ReferenceMatchesDefinition

  .property instance string CodeBase()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_CodeBase()
    .set instance void Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::set_CodeBase(string)
  } // end of property AssemblyName::CodeBase
  .property instance valuetype Microsoft.VisualStudio.Debugger.Metadata.AssemblyContentType
          ContentType()
  {
    .set instance void Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::set_ContentType(valuetype Microsoft.VisualStudio.Debugger.Metadata.AssemblyContentType)
    .get instance valuetype Microsoft.VisualStudio.Debugger.Metadata.AssemblyContentType Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_ContentType()
  } // end of property AssemblyName::ContentType
  .property instance class [mscorlib]System.Globalization.CultureInfo
          CultureInfo()
  {
    .get instance class [mscorlib]System.Globalization.CultureInfo Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_CultureInfo()
    .set instance void Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::set_CultureInfo(class [mscorlib]System.Globalization.CultureInfo)
  } // end of property AssemblyName::CultureInfo
  .property instance string CultureName()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_CultureName()
  } // end of property AssemblyName::CultureName
  .property instance valuetype [mscorlib]System.Reflection.AssemblyNameFlags
          Flags()
  {
    .get instance valuetype [mscorlib]System.Reflection.AssemblyNameFlags Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_Flags()
    .set instance void Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::set_Flags(valuetype [mscorlib]System.Reflection.AssemblyNameFlags)
  } // end of property AssemblyName::Flags
  .property instance string FullName()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_FullName()
  } // end of property AssemblyName::FullName
  .property instance valuetype [mscorlib]System.Configuration.Assemblies.AssemblyHashAlgorithm
          HashAlgorithm()
  {
    .get instance valuetype [mscorlib]System.Configuration.Assemblies.AssemblyHashAlgorithm Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_HashAlgorithm()
    .set instance void Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::set_HashAlgorithm(valuetype [mscorlib]System.Configuration.Assemblies.AssemblyHashAlgorithm)
  } // end of property AssemblyName::HashAlgorithm
  .property instance class [mscorlib]System.Reflection.StrongNameKeyPair
          KeyPair()
  {
    .get instance class [mscorlib]System.Reflection.StrongNameKeyPair Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_KeyPair()
  } // end of property AssemblyName::KeyPair
  .property instance string Name()
  {
    .set instance void Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::set_Name(string)
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_Name()
  } // end of property AssemblyName::Name
  .property instance valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture
          ProcessorArchitecture()
  {
    .get instance valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_ProcessorArchitecture()
    .set instance void Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::set_ProcessorArchitecture(valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture)
  } // end of property AssemblyName::ProcessorArchitecture
  .property instance class [mscorlib]System.Version
          Version()
  {
    .get instance class [mscorlib]System.Version Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::get_Version()
    .set instance void Microsoft.VisualStudio.Debugger.Metadata.AssemblyName::set_Version(class [mscorlib]System.Version)
  } // end of property AssemblyName::Version
} // end of class Microsoft.VisualStudio.Debugger.Metadata.AssemblyName

.class interface public abstract auto ansi Microsoft.MetadataReader.IMetadataImport
{
  .method public hidebysig newslot abstract virtual 
          instance void  CloseEnum(native int hEnum) cil managed preservesig
  {
  } // end of method IMetadataImport::CloseEnum

  .method public hidebysig newslot abstract virtual 
          instance int32  CountEnum(valuetype Microsoft.MetadataReader.HCORENUM hEnum,
                                    [out] int32& pulCount) cil managed preservesig
  {
  } // end of method IMetadataImport::CountEnum

  .method public hidebysig newslot abstract virtual 
          instance int32  ResetEnum(valuetype Microsoft.MetadataReader.HCORENUM hEnum,
                                    int32 ulPos) cil managed preservesig
  {
  } // end of method IMetadataImport::ResetEnum

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumTypeDefs(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                       [out] int32& rTypeDefs,
                                       uint32 cMax,
                                       [out] uint32& pcTypeDefs) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumTypeDefs

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumInterfaceImpls(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                             int32 td,
                                             [out] int32& rImpls,
                                             int32 cMax,
                                             int32& pcImpls) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumInterfaceImpls

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumTypeRefs(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                       int32[] td,
                                       uint32 cMax,
                                       uint32 pcTypeRefs) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumTypeRefs

  .method public hidebysig newslot abstract virtual 
          instance int32  FindTypeDefByName([in] string  marshal( lpwstr) szTypeDef,
                                            [in] int32 tkEnclosingClass,
                                            [out] int32& token) cil managed preservesig
  {
  } // end of method IMetadataImport::FindTypeDefByName

  .method public hidebysig newslot abstract virtual 
          instance int32  GetScopeProps([out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                        [in] int32 cchName,
                                        [out] int32& pchName,
                                        [out] valuetype [mscorlib]System.Guid& mvid) cil managed preservesig
  {
  } // end of method IMetadataImport::GetScopeProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetModuleFromScope([out] int32& mdModule) cil managed preservesig
  {
  } // end of method IMetadataImport::GetModuleFromScope

  .method public hidebysig newslot abstract virtual 
          instance int32  GetTypeDefProps([in] int32 td,
                                          [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szTypeDef,
                                          [in] int32 cchTypeDef,
                                          [out] int32& pchTypeDef,
                                          [out] valuetype [mscorlib]System.Reflection.TypeAttributes&  marshal( unsigned int32) pdwTypeDefFlags,
                                          [out] int32& ptkExtends) cil managed preservesig
  {
  } // end of method IMetadataImport::GetTypeDefProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetInterfaceImplProps(int32 iiImpl,
                                                [out] int32& pClass,
                                                [out] int32& ptkIface) cil managed preservesig
  {
  } // end of method IMetadataImport::GetInterfaceImplProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetTypeRefProps(int32 tr,
                                          [out] int32& ptkResolutionScope,
                                          [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                          [in] int32 cchName,
                                          [out] int32& pchName) cil managed preservesig
  {
  } // end of method IMetadataImport::GetTypeRefProps

  .method public hidebysig newslot abstract virtual 
          instance int32  ResolveTypeRef(int32 tr,
                                         [in] valuetype [mscorlib]System.Guid& riid,
                                         [out] object&  marshal( interface ) ppIScope) cil managed preservesig
  {
  } // end of method IMetadataImport::ResolveTypeRef

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMembers([in][out] uint32& phEnum,
                                      [in] uint32 cl,
                                      uint32[]  marshal([ + 3]) rMembers,
                                      [in] uint32 cMax,
                                      [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumMembers

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMembersWithName([in][out] uint32& phEnum,
                                              uint32 cl,
                                              string szName,
                                              uint32[]  marshal([ + 4]) rMembers,
                                              [in] uint32 cMax,
                                              [in][out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumMembersWithName

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMethods(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                      int32 cl,
                                      [out] int32& mdMethodDef,
                                      int32 cMax,
                                      [out] int32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumMethods

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMethodsWithName(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                              int32 cl,
                                              [in] string  marshal( lpwstr) szName,
                                              [out] int32& mdMethodDef,
                                              int32 cMax,
                                              [out] int32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumMethodsWithName

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumFields(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                     int32 cl,
                                     [out] int32& mdFieldDef,
                                     int32 cMax,
                                     [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumFields

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumFieldsWithName([in][out] uint32& phEnum,
                                             [in] uint32 cl,
                                             [in] string szName,
                                             [in] uint32[]  marshal([ + 4]) rFields,
                                             [in] uint32 cMax,
                                             [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumFieldsWithName

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumParams(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                     int32 mdMethodDef,
                                     int32[]  marshal([]) rParams,
                                     int32 cMax,
                                     [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumParams

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMemberRefs([in][out] uint32& phEnum,
                                         uint32 tkParent,
                                         uint32[]  marshal([ + 3]) rMemberRefs,
                                         uint32 cMax,
                                         [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumMemberRefs

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMethodImpls(valuetype Microsoft.MetadataReader.HCORENUM& hEnum,
                                          valuetype System.Reflection.Adds.Token typeDef,
                                          [out] valuetype System.Reflection.Adds.Token& methodBody,
                                          [out] valuetype System.Reflection.Adds.Token& methodDecl,
                                          int32 cMax,
                                          [out] int32& cTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumMethodImpls

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumPermissionSets([in][out] valuetype Microsoft.MetadataReader.HCORENUM& hEnum,
                                             [in] uint32 tk,
                                             [in] uint32 dwActions,
                                             uint32[]  marshal([ + 4]) rPermission,
                                             [in][out] uint32& cMax) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumPermissionSets

  .method public hidebysig newslot abstract virtual 
          instance int32  FindMember([in] int32 typeDefToken,
                                     [in] string  marshal( lpwstr) szName,
                                     [in] uint8[] pvSigBlob,
                                     [in] int32 cbSigBlob,
                                     [out] int32& memberDefToken) cil managed preservesig
  {
  } // end of method IMetadataImport::FindMember

  .method public hidebysig newslot abstract virtual 
          instance int32  FindMethod([in] int32 typeDef,
                                     [in] string  marshal( lpwstr) szName,
                                     [in] valuetype System.Reflection.Adds.EmbeddedBlobPointer pvSigBlob,
                                     [in] int32 cbSigBlob,
                                     [out] int32& methodDef) cil managed preservesig
  {
  } // end of method IMetadataImport::FindMethod

  .method public hidebysig newslot abstract virtual 
          instance int32  FindField([in] int32 typeDef,
                                    [in] string  marshal( lpwstr) szName,
                                    [in] uint8[] pvSigBlob,
                                    [in] int32 cbSigBlob,
                                    [out] int32& fieldDef) cil managed preservesig
  {
  } // end of method IMetadataImport::FindField

  .method public hidebysig newslot abstract virtual 
          instance int32  FindMemberRef([in] int32 typeRef,
                                        [in] string  marshal( lpwstr) szName,
                                        [in] uint8[] pvSigBlob,
                                        [in] int32 cbSigBlob,
                                        [out] int32& result) cil managed preservesig
  {
  } // end of method IMetadataImport::FindMemberRef

  .method public hidebysig newslot abstract virtual 
          instance int32  GetMethodProps([in] uint32 md,
                                         [out] int32& pClass,
                                         [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szMethod,
                                         [in] int32 cchMethod,
                                         [out] uint32& pchMethod,
                                         [out] valuetype [mscorlib]System.Reflection.MethodAttributes& pdwAttr,
                                         [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppvSigBlob,
                                         [out] uint32& pcbSigBlob,
                                         [out] uint32& pulCodeRVA,
                                         [out] uint32& pdwImplFlags) cil managed preservesig
  {
  } // end of method IMetadataImport::GetMethodProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetMemberRefProps([in] valuetype System.Reflection.Adds.Token mr,
                                            [out] valuetype System.Reflection.Adds.Token& ptk,
                                            [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szMember,
                                            [in] int32 cchMember,
                                            [out] uint32& pchMember,
                                            [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppvSigBlob,
                                            [out] uint32& pbSig) cil managed preservesig
  {
  } // end of method IMetadataImport::GetMemberRefProps

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumProperties(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                         int32 td,
                                         [out] int32& mdFieldDef,
                                         int32 cMax,
                                         [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumProperties

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumEvents(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                     int32 td,
                                     [out] int32& mdFieldDef,
                                     int32 cMax,
                                     [out] uint32& pcEvents) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumEvents

  .method public hidebysig newslot abstract virtual 
          instance int32  GetEventProps(int32 ev,
                                        [out] int32& pClass,
                                        [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szEvent,
                                        int32 cchEvent,
                                        [out] int32& pchEvent,
                                        [out] int32& pdwEventFlags,
                                        [out] int32& ptkEventType,
                                        [out] int32& pmdAddOn,
                                        [out] int32& pmdRemoveOn,
                                        [out] int32& pmdFire,
                                        [out] int32& rmdOtherMethod,
                                        uint32 cMax,
                                        [out] uint32& pcOtherMethod) cil managed preservesig
  {
  } // end of method IMetadataImport::GetEventProps

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMethodSemantics([in][out] uint32& phEnum,
                                              [in] uint32 mb,
                                              [out] uint32[]  marshal([ + 3]) rEventProp,
                                              [in] uint32 cMax,
                                              [out] uint32& pcEventProp) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumMethodSemantics

  .method public hidebysig newslot abstract virtual 
          instance int32  GetMethodSemantics([in] uint32 mb,
                                             [in] uint32 tkEventProp,
                                             [out] uint32& pdwSemanticsFlags) cil managed preservesig
  {
  } // end of method IMetadataImport::GetMethodSemantics

  .method public hidebysig newslot abstract virtual 
          instance int32  GetClassLayout(int32 typeDef,
                                         [out] uint32& dwPackSize,
                                         valuetype Microsoft.MetadataReader.UnusedIntPtr zeroPtr,
                                         uint32 zeroCount,
                                         valuetype Microsoft.MetadataReader.UnusedIntPtr zeroPtr2,
                                         uint32& ulClassSize) cil managed preservesig
  {
  } // end of method IMetadataImport::GetClassLayout

  .method public hidebysig newslot abstract virtual 
          instance int32  GetFieldMarshal(int32 tk,
                                          [out] native int& ppvNativeType,
                                          [out] int32& pcbNativeType) cil managed preservesig
  {
  } // end of method IMetadataImport::GetFieldMarshal

  .method public hidebysig newslot abstract virtual 
          instance int32  GetRVA(int32 token,
                                 [out] uint32& rva,
                                 [out] uint32& 'flags') cil managed preservesig
  {
  } // end of method IMetadataImport::GetRVA

  .method public hidebysig newslot abstract virtual 
          instance int32  GetPermissionSetProps(uint32 pm,
                                                [out] uint32& pdwAction,
                                                [out] native int& ppvPermission,
                                                [out] int32& pcbPermission) cil managed preservesig
  {
  } // end of method IMetadataImport::GetPermissionSetProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetSigFromToken(int32 token,
                                          [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& pSig,
                                          [out] int32& cbSig) cil managed preservesig
  {
  } // end of method IMetadataImport::GetSigFromToken

  .method public hidebysig newslot abstract virtual 
          instance int32  GetModuleRefProps(int32 mur,
                                            [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                            int32 cchName,
                                            [out] int32& pchName) cil managed preservesig
  {
  } // end of method IMetadataImport::GetModuleRefProps

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumModuleRefs(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                         [out] int32& mdModuleRef,
                                         int32 cMax,
                                         [out] uint32& pcModuleRefs) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumModuleRefs

  .method public hidebysig newslot abstract virtual 
          instance int32  GetTypeSpecFromToken(valuetype System.Reflection.Adds.Token typeSpec,
                                               [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& pSig,
                                               [out] int32& cbSig) cil managed preservesig
  {
  } // end of method IMetadataImport::GetTypeSpecFromToken

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNameFromToken(uint32 tk,
                                           [out] string  marshal( lpstr) pszUtf8NamePtr) cil managed preservesig
  {
  } // end of method IMetadataImport::GetNameFromToken

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumUnresolvedMethods(uint32& phEnum,
                                                uint32[]  marshal([ + 2]) rMethods,
                                                uint32 cMax,
                                                [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumUnresolvedMethods

  .method public hidebysig newslot abstract virtual 
          instance int32  GetUserString([in] int32 stk,
                                        [out] char[]  marshal([ + 2]) szString,
                                        [in] int32 cchString,
                                        [out] int32& pchString) cil managed preservesig
  {
  } // end of method IMetadataImport::GetUserString

  .method public hidebysig newslot abstract virtual 
          instance int32  GetPinvokeMap(uint32 tk,
                                        [out] uint32& pdwMappingFlags,
                                        class [mscorlib]System.Text.StringBuilder szImportName,
                                        uint32 cchImportName,
                                        [out] uint32& pchImportName,
                                        [out] int32& pmrImportDLL) cil managed preservesig
  {
  } // end of method IMetadataImport::GetPinvokeMap

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumSignatures(uint32& phEnum,
                                         uint32[]  marshal([ + 2]) rSignatures,
                                         uint32 cmax,
                                         [out] uint32& pcSignatures) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumSignatures

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumTypeSpecs(uint32& phEnum,
                                        uint32[]  marshal([ + 2]) rTypeSpecs,
                                        uint32 cmax,
                                        [out] uint32& pcTypeSpecs) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumTypeSpecs

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumUserStrings(uint32& phEnum,
                                          uint32[]  marshal([ + 2]) rStrings,
                                          uint32 cmax,
                                          [out] uint32& pcStrings) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumUserStrings

  .method public hidebysig newslot abstract virtual 
          instance int32  GetParamForMethodIndex(uint32 md,
                                                 uint32 ulParamSeq,
                                                 [out] uint32& pParam,
                                                 [out] int32& ppd) cil managed preservesig
  {
  } // end of method IMetadataImport::GetParamForMethodIndex

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumCustomAttributes(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                               int32 tk,
                                               int32 tkType,
                                               [out] valuetype System.Reflection.Adds.Token& mdCustomAttribute,
                                               uint32 cMax,
                                               [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport::EnumCustomAttributes

  .method public hidebysig newslot abstract virtual 
          instance int32  GetCustomAttributeProps([in] valuetype System.Reflection.Adds.Token cv,
                                                  [out] valuetype System.Reflection.Adds.Token& tkObj,
                                                  [out] valuetype System.Reflection.Adds.Token& tkType,
                                                  [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& 'blob',
                                                  [out] int32& cbSize) cil managed preservesig
  {
  } // end of method IMetadataImport::GetCustomAttributeProps

  .method public hidebysig newslot abstract virtual 
          instance int32  FindTypeRef([in] int32 tkResolutionScope,
                                      [in] string  marshal( lpwstr) szName,
                                      [out] int32& typeRef) cil managed preservesig
  {
  } // end of method IMetadataImport::FindTypeRef

  .method public hidebysig newslot abstract virtual 
          instance int32  GetMemberProps(uint32 mb,
                                         [out] uint32& pClass,
                                         class [mscorlib]System.Text.StringBuilder szMember,
                                         uint32 cchMember,
                                         [out] uint32& pchMember,
                                         [out] uint32& pdwAttr,
                                         [out] native int& ppvSigBlob,
                                         [out] uint32& pcbSigBlob,
                                         [out] uint32& pulCodeRVA,
                                         [out] uint32& pdwImplFlags,
                                         [out] uint32& pdwCPlusTypeFlag,
                                         [out] native int& ppValue,
                                         [out] uint32& pcchValue) cil managed preservesig
  {
  } // end of method IMetadataImport::GetMemberProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetFieldProps(int32 mb,
                                        [out] int32& mdTypeDef,
                                        [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szField,
                                        int32 cchField,
                                        [out] int32& pchField,
                                        [out] valuetype [mscorlib]System.Reflection.FieldAttributes& pdwAttr,
                                        [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppvSigBlob,
                                        [out] int32& pcbSigBlob,
                                        [out] int32& pdwCPlusTypeFlab,
                                        [out] native int& ppValue,
                                        [out] int32& pcchValue) cil managed preservesig
  {
  } // end of method IMetadataImport::GetFieldProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetPropertyProps(valuetype System.Reflection.Adds.Token prop,
                                           [out] valuetype System.Reflection.Adds.Token& pClass,
                                           [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szProperty,
                                           int32 cchProperty,
                                           [out] int32& pchProperty,
                                           [out] valuetype [mscorlib]System.Reflection.PropertyAttributes& pdwPropFlags,
                                           [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppvSig,
                                           [out] int32& pbSig,
                                           [out] int32& pdwCPlusTypeFlag,
                                           [out] valuetype Microsoft.MetadataReader.UnusedIntPtr& ppDefaultValue,
                                           [out] int32& pcchDefaultValue,
                                           [out] valuetype System.Reflection.Adds.Token& pmdSetter,
                                           [out] valuetype System.Reflection.Adds.Token& pmdGetter,
                                           [out] valuetype System.Reflection.Adds.Token& rmdOtherMethod,
                                           uint32 cMax,
                                           [out] uint32& pcOtherMethod) cil managed preservesig
  {
  } // end of method IMetadataImport::GetPropertyProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetParamProps(int32 tk,
                                        [out] int32& pmd,
                                        [out] uint32& pulSequence,
                                        [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                        uint32 cchName,
                                        [out] uint32& pchName,
                                        [out] uint32& pdwAttr,
                                        [out] uint32& pdwCPlusTypeFlag,
                                        [out] valuetype Microsoft.MetadataReader.UnusedIntPtr& ppValue,
                                        [out] uint32& pcchValue) cil managed preservesig
  {
  } // end of method IMetadataImport::GetParamProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetCustomAttributeByName(int32 tkObj,
                                                   string  marshal( lpwstr) szName,
                                                   [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppData,
                                                   [out] uint32& pcbData) cil managed preservesig
  {
  } // end of method IMetadataImport::GetCustomAttributeByName

  .method public hidebysig newslot abstract virtual 
          instance bool  IsValidToken([in] uint32  marshal( unsigned int32) tk) cil managed preservesig
  {
  } // end of method IMetadataImport::IsValidToken

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNestedClassProps(int32 tdNestedClass,
                                              [out] int32& tdEnclosingClass) cil managed preservesig
  {
  } // end of method IMetadataImport::GetNestedClassProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNativeCallConvFromSig(native int pvSig,
                                                   uint32 cbSig,
                                                   [out] uint32& pCallConv) cil managed preservesig
  {
  } // end of method IMetadataImport::GetNativeCallConvFromSig

  .method public hidebysig newslot abstract virtual 
          instance int32  IsGlobal(uint32 pd,
                                   [out] int32& pbGlobal) cil managed preservesig
  {
  } // end of method IMetadataImport::IsGlobal

} // end of class Microsoft.MetadataReader.IMetadataImport

.class public sequential ansi sealed System.Reflection.Adds.Token
       extends [mscorlib]System.ValueType
{
  .field public static initonly valuetype System.Reflection.Adds.Token Nil
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(valuetype System.Reflection.Adds.TokenType 'type',
                               int32 rid) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(uint32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::.ctor

  .method public hidebysig specialname instance int32 
          get_Value() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::get_Value

  .method public hidebysig specialname instance valuetype System.Reflection.Adds.TokenType 
          get_TokenType() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::get_TokenType

  .method public hidebysig specialname instance int32 
          get_Index() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::get_Index

  .method public hidebysig specialname instance bool 
          get_IsNil() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::get_IsNil

  .method public hidebysig specialname static 
          int32  op_Implicit(valuetype System.Reflection.Adds.Token token) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::op_Implicit

  .method public hidebysig specialname static 
          bool  op_Equality(valuetype System.Reflection.Adds.Token token1,
                            valuetype System.Reflection.Adds.Token token2) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::op_Equality

  .method public hidebysig specialname static 
          bool  op_Inequality(valuetype System.Reflection.Adds.Token token1,
                              valuetype System.Reflection.Adds.Token token2) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::op_Inequality

  .method public hidebysig specialname static 
          bool  op_Equality(valuetype System.Reflection.Adds.Token token1,
                            int32 token2) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::op_Equality

  .method public hidebysig specialname static 
          bool  op_Inequality(valuetype System.Reflection.Adds.Token token1,
                              int32 token2) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::op_Inequality

  .method public hidebysig specialname static 
          bool  op_Equality(int32 token1,
                            valuetype System.Reflection.Adds.Token token2) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::op_Equality

  .method public hidebysig specialname static 
          bool  op_Inequality(int32 token1,
                              valuetype System.Reflection.Adds.Token token2) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::op_Inequality

  .method public hidebysig static bool  IsType(int32 token,
                                               valuetype System.Reflection.Adds.TokenType[] types) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::IsType

  .method public hidebysig instance bool 
          IsType(valuetype System.Reflection.Adds.TokenType 'type') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::IsType

  .method public hidebysig virtual instance bool 
          Equals(object obj) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::Equals

  .method public hidebysig virtual instance int32 
          GetHashCode() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::GetHashCode

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Token::ToString

  .property instance int32 Index()
  {
    .get instance int32 System.Reflection.Adds.Token::get_Index()
  } // end of property Token::Index
  .property instance bool IsNil()
  {
    .get instance bool System.Reflection.Adds.Token::get_IsNil()
  } // end of property Token::IsNil
  .property instance valuetype System.Reflection.Adds.TokenType
          TokenType()
  {
    .get instance valuetype System.Reflection.Adds.TokenType System.Reflection.Adds.Token::get_TokenType()
  } // end of property Token::TokenType
  .property instance int32 Value()
  {
    .get instance int32 System.Reflection.Adds.Token::get_Value()
  } // end of property Token::Value
} // end of class System.Reflection.Adds.Token

.class interface public abstract auto ansi Microsoft.MetadataReader.IMetadataImport2
       implements Microsoft.MetadataReader.IMetadataImport
{
  .method public hidebysig newslot abstract virtual 
          instance void  CloseEnum(native int hEnum) cil managed preservesig
  {
  } // end of method IMetadataImport2::CloseEnum

  .method public hidebysig newslot abstract virtual 
          instance int32  CountEnum(valuetype Microsoft.MetadataReader.HCORENUM hEnum,
                                    [out] int32& pulCount) cil managed preservesig
  {
  } // end of method IMetadataImport2::CountEnum

  .method public hidebysig newslot abstract virtual 
          instance int32  ResetEnum(valuetype Microsoft.MetadataReader.HCORENUM hEnum,
                                    int32 ulPos) cil managed preservesig
  {
  } // end of method IMetadataImport2::ResetEnum

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumTypeDefs(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                       [out] int32& rTypeDefs,
                                       uint32 cMax,
                                       [out] uint32& pcTypeDefs) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumTypeDefs

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumInterfaceImpls(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                             int32 td,
                                             [out] int32& rImpls,
                                             int32 cMax,
                                             int32& pcImpls) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumInterfaceImpls

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumTypeRefs(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                       int32[] td,
                                       uint32 cMax,
                                       uint32 pcTypeRefs) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumTypeRefs

  .method public hidebysig newslot abstract virtual 
          instance int32  FindTypeDefByName([in] string  marshal( lpwstr) szTypeDef,
                                            [in] int32 tkEnclosingClass,
                                            [out] int32& token) cil managed preservesig
  {
  } // end of method IMetadataImport2::FindTypeDefByName

  .method public hidebysig newslot abstract virtual 
          instance int32  GetScopeProps([out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                        [in] int32 cchName,
                                        [out] int32& pchName,
                                        [out] valuetype [mscorlib]System.Guid& mvid) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetScopeProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetModuleFromScope([out] int32& mdModule) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetModuleFromScope

  .method public hidebysig newslot abstract virtual 
          instance int32  GetTypeDefProps([in] int32 td,
                                          [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szTypeDef,
                                          [in] int32 cchTypeDef,
                                          [out] int32& pchTypeDef,
                                          [out] valuetype [mscorlib]System.Reflection.TypeAttributes&  marshal( unsigned int32) pdwTypeDefFlags,
                                          [out] int32& ptkExtends) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetTypeDefProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetInterfaceImplProps(int32 iiImpl,
                                                [out] int32& pClass,
                                                [out] int32& ptkIface) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetInterfaceImplProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetTypeRefProps(int32 tr,
                                          [out] int32& ptkResolutionScope,
                                          [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                          [in] int32 cchName,
                                          [out] int32& pchName) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetTypeRefProps

  .method public hidebysig newslot abstract virtual 
          instance int32  ResolveTypeRef(int32 tr,
                                         [in] valuetype [mscorlib]System.Guid& riid,
                                         [out] object&  marshal( interface ) ppIScope) cil managed preservesig
  {
  } // end of method IMetadataImport2::ResolveTypeRef

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMembers([in][out] uint32& phEnum,
                                      [in] uint32 cl,
                                      uint32[]  marshal([ + 3]) rMembers,
                                      [in] uint32 cMax,
                                      [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumMembers

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMembersWithName([in][out] uint32& phEnum,
                                              uint32 cl,
                                              string szName,
                                              uint32[]  marshal([ + 4]) rMembers,
                                              [in] uint32 cMax,
                                              [in][out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumMembersWithName

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMethods(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                      int32 cl,
                                      [out] int32& mdMethodDef,
                                      int32 cMax,
                                      [out] int32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumMethods

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMethodsWithName(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                              int32 cl,
                                              [in] string  marshal( lpwstr) szName,
                                              [out] int32& mdMethodDef,
                                              int32 cMax,
                                              [out] int32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumMethodsWithName

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumFields(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                     int32 cl,
                                     [out] int32& mdFieldDef,
                                     int32 cMax,
                                     [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumFields

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumFieldsWithName([in][out] uint32& phEnum,
                                             [in] uint32 cl,
                                             [in] string szName,
                                             [in] uint32[]  marshal([ + 4]) rFields,
                                             [in] uint32 cMax,
                                             [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumFieldsWithName

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumParams(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                     int32 mdMethodDef,
                                     int32[]  marshal([]) rParams,
                                     int32 cMax,
                                     [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumParams

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMemberRefs([in][out] uint32& phEnum,
                                         uint32 tkParent,
                                         uint32[]  marshal([ + 3]) rMemberRefs,
                                         uint32 cMax,
                                         [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumMemberRefs

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMethodImpls(valuetype Microsoft.MetadataReader.HCORENUM& hEnum,
                                          valuetype System.Reflection.Adds.Token typeDef,
                                          [out] valuetype System.Reflection.Adds.Token& methodBody,
                                          [out] valuetype System.Reflection.Adds.Token& methodDecl,
                                          int32 cMax,
                                          [out] int32& cTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumMethodImpls

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumPermissionSets([in][out] valuetype Microsoft.MetadataReader.HCORENUM& hEnum,
                                             [in] uint32 tk,
                                             [in] uint32 dwActions,
                                             uint32[]  marshal([ + 4]) rPermission,
                                             [in][out] uint32& cMax) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumPermissionSets

  .method public hidebysig newslot abstract virtual 
          instance int32  FindMember([in] int32 typeDefToken,
                                     [in] string  marshal( lpwstr) szName,
                                     [in] uint8[] pvSigBlob,
                                     [in] int32 cbSigBlob,
                                     [out] int32& memberDefToken) cil managed preservesig
  {
  } // end of method IMetadataImport2::FindMember

  .method public hidebysig newslot abstract virtual 
          instance int32  FindMethod([in] int32 typeDef,
                                     [in] string  marshal( lpwstr) szName,
                                     [in] valuetype System.Reflection.Adds.EmbeddedBlobPointer pvSigBlob,
                                     [in] int32 cbSigBlob,
                                     [out] int32& methodDef) cil managed preservesig
  {
  } // end of method IMetadataImport2::FindMethod

  .method public hidebysig newslot abstract virtual 
          instance int32  FindField([in] int32 typeDef,
                                    [in] string  marshal( lpwstr) szName,
                                    [in] uint8[] pvSigBlob,
                                    [in] int32 cbSigBlob,
                                    [out] int32& fieldDef) cil managed preservesig
  {
  } // end of method IMetadataImport2::FindField

  .method public hidebysig newslot abstract virtual 
          instance int32  FindMemberRef([in] int32 typeRef,
                                        [in] string  marshal( lpwstr) szName,
                                        [in] uint8[] pvSigBlob,
                                        [in] int32 cbSigBlob,
                                        [out] int32& result) cil managed preservesig
  {
  } // end of method IMetadataImport2::FindMemberRef

  .method public hidebysig newslot abstract virtual 
          instance int32  GetMethodProps([in] uint32 md,
                                         [out] int32& pClass,
                                         [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szMethod,
                                         [in] int32 cchMethod,
                                         [out] uint32& pchMethod,
                                         [out] valuetype [mscorlib]System.Reflection.MethodAttributes& pdwAttr,
                                         [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppvSigBlob,
                                         [out] uint32& pcbSigBlob,
                                         [out] uint32& pulCodeRVA,
                                         [out] uint32& pdwImplFlags) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetMethodProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetMemberRefProps([in] valuetype System.Reflection.Adds.Token mr,
                                            [out] valuetype System.Reflection.Adds.Token& ptk,
                                            [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szMember,
                                            [in] int32 cchMember,
                                            [out] uint32& pchMember,
                                            [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppvSigBlob,
                                            [out] uint32& pbSig) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetMemberRefProps

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumProperties(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                         int32 td,
                                         [out] int32& mdFieldDef,
                                         int32 cMax,
                                         [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumProperties

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumEvents(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                     int32 td,
                                     [out] int32& mdFieldDef,
                                     int32 cMax,
                                     [out] uint32& pcEvents) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumEvents

  .method public hidebysig newslot abstract virtual 
          instance int32  GetEventProps(int32 ev,
                                        [out] int32& pClass,
                                        [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szEvent,
                                        int32 cchEvent,
                                        [out] int32& pchEvent,
                                        [out] int32& pdwEventFlags,
                                        [out] int32& ptkEventType,
                                        [out] int32& pmdAddOn,
                                        [out] int32& pmdRemoveOn,
                                        [out] int32& pmdFire,
                                        [out] int32& rmdOtherMethod,
                                        uint32 cMax,
                                        [out] uint32& pcOtherMethod) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetEventProps

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMethodSemantics([in][out] uint32& phEnum,
                                              [in] uint32 mb,
                                              [out] uint32[]  marshal([ + 3]) rEventProp,
                                              [in] uint32 cMax,
                                              [out] uint32& pcEventProp) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumMethodSemantics

  .method public hidebysig newslot abstract virtual 
          instance int32  GetMethodSemantics([in] uint32 mb,
                                             [in] uint32 tkEventProp,
                                             [out] uint32& pdwSemanticsFlags) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetMethodSemantics

  .method public hidebysig newslot abstract virtual 
          instance int32  GetClassLayout(int32 typeDef,
                                         [out] uint32& dwPackSize,
                                         valuetype Microsoft.MetadataReader.UnusedIntPtr zeroPtr,
                                         uint32 zeroCount,
                                         valuetype Microsoft.MetadataReader.UnusedIntPtr zeroPtr2,
                                         uint32& ulClassSize) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetClassLayout

  .method public hidebysig newslot abstract virtual 
          instance int32  GetFieldMarshal(int32 tk,
                                          [out] native int& ppvNativeType,
                                          [out] int32& pcbNativeType) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetFieldMarshal

  .method public hidebysig newslot abstract virtual 
          instance int32  GetRVA(int32 token,
                                 [out] uint32& rva,
                                 [out] uint32& 'flags') cil managed preservesig
  {
  } // end of method IMetadataImport2::GetRVA

  .method public hidebysig newslot abstract virtual 
          instance int32  GetPermissionSetProps(uint32 pm,
                                                [out] uint32& pdwAction,
                                                [out] native int& ppvPermission,
                                                [out] int32& pcbPermission) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetPermissionSetProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetSigFromToken(int32 token,
                                          [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& pSig,
                                          [out] int32& cbSig) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetSigFromToken

  .method public hidebysig newslot abstract virtual 
          instance int32  GetModuleRefProps(int32 mur,
                                            [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                            int32 cchName,
                                            [out] int32& pchName) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetModuleRefProps

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumModuleRefs(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                         [out] int32& mdModuleRef,
                                         int32 cMax,
                                         [out] uint32& pcModuleRefs) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumModuleRefs

  .method public hidebysig newslot abstract virtual 
          instance int32  GetTypeSpecFromToken(valuetype System.Reflection.Adds.Token typeSpec,
                                               [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& pSig,
                                               [out] int32& cbSig) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetTypeSpecFromToken

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNameFromToken(uint32 tk,
                                           [out] string  marshal( lpstr) pszUtf8NamePtr) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetNameFromToken

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumUnresolvedMethods(uint32& phEnum,
                                                uint32[]  marshal([ + 2]) rMethods,
                                                uint32 cMax,
                                                [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumUnresolvedMethods

  .method public hidebysig newslot abstract virtual 
          instance int32  GetUserString([in] int32 stk,
                                        [out] char[]  marshal([ + 2]) szString,
                                        [in] int32 cchString,
                                        [out] int32& pchString) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetUserString

  .method public hidebysig newslot abstract virtual 
          instance int32  GetPinvokeMap(uint32 tk,
                                        [out] uint32& pdwMappingFlags,
                                        class [mscorlib]System.Text.StringBuilder szImportName,
                                        uint32 cchImportName,
                                        [out] uint32& pchImportName,
                                        [out] int32& pmrImportDLL) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetPinvokeMap

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumSignatures(uint32& phEnum,
                                         uint32[]  marshal([ + 2]) rSignatures,
                                         uint32 cmax,
                                         [out] uint32& pcSignatures) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumSignatures

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumTypeSpecs(uint32& phEnum,
                                        uint32[]  marshal([ + 2]) rTypeSpecs,
                                        uint32 cmax,
                                        [out] uint32& pcTypeSpecs) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumTypeSpecs

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumUserStrings(uint32& phEnum,
                                          uint32[]  marshal([ + 2]) rStrings,
                                          uint32 cmax,
                                          [out] uint32& pcStrings) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumUserStrings

  .method public hidebysig newslot abstract virtual 
          instance int32  GetParamForMethodIndex(uint32 md,
                                                 uint32 ulParamSeq,
                                                 [out] uint32& pParam,
                                                 [out] int32& ppd) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetParamForMethodIndex

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumCustomAttributes(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                               int32 tk,
                                               int32 tkType,
                                               [out] valuetype System.Reflection.Adds.Token& mdCustomAttribute,
                                               uint32 cMax,
                                               [out] uint32& pcTokens) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumCustomAttributes

  .method public hidebysig newslot abstract virtual 
          instance int32  GetCustomAttributeProps([in] valuetype System.Reflection.Adds.Token cv,
                                                  [out] valuetype System.Reflection.Adds.Token& tkObj,
                                                  [out] valuetype System.Reflection.Adds.Token& tkType,
                                                  [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& 'blob',
                                                  [out] int32& cbSize) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetCustomAttributeProps

  .method public hidebysig newslot abstract virtual 
          instance int32  FindTypeRef([in] int32 tkResolutionScope,
                                      [in] string  marshal( lpwstr) szName,
                                      [out] int32& typeRef) cil managed preservesig
  {
  } // end of method IMetadataImport2::FindTypeRef

  .method public hidebysig newslot abstract virtual 
          instance int32  GetMemberProps(uint32 mb,
                                         [out] uint32& pClass,
                                         class [mscorlib]System.Text.StringBuilder szMember,
                                         uint32 cchMember,
                                         [out] uint32& pchMember,
                                         [out] uint32& pdwAttr,
                                         [out] native int& ppvSigBlob,
                                         [out] uint32& pcbSigBlob,
                                         [out] uint32& pulCodeRVA,
                                         [out] uint32& pdwImplFlags,
                                         [out] uint32& pdwCPlusTypeFlag,
                                         [out] native int& ppValue,
                                         [out] uint32& pcchValue) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetMemberProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetFieldProps(int32 mb,
                                        [out] int32& mdTypeDef,
                                        [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szField,
                                        int32 cchField,
                                        [out] int32& pchField,
                                        [out] valuetype [mscorlib]System.Reflection.FieldAttributes& pdwAttr,
                                        [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppvSigBlob,
                                        [out] int32& pcbSigBlob,
                                        [out] int32& pdwCPlusTypeFlab,
                                        [out] native int& ppValue,
                                        [out] int32& pcchValue) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetFieldProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetPropertyProps(valuetype System.Reflection.Adds.Token prop,
                                           [out] valuetype System.Reflection.Adds.Token& pClass,
                                           [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szProperty,
                                           int32 cchProperty,
                                           [out] int32& pchProperty,
                                           [out] valuetype [mscorlib]System.Reflection.PropertyAttributes& pdwPropFlags,
                                           [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppvSig,
                                           [out] int32& pbSig,
                                           [out] int32& pdwCPlusTypeFlag,
                                           [out] valuetype Microsoft.MetadataReader.UnusedIntPtr& ppDefaultValue,
                                           [out] int32& pcchDefaultValue,
                                           [out] valuetype System.Reflection.Adds.Token& pmdSetter,
                                           [out] valuetype System.Reflection.Adds.Token& pmdGetter,
                                           [out] valuetype System.Reflection.Adds.Token& rmdOtherMethod,
                                           uint32 cMax,
                                           [out] uint32& pcOtherMethod) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetPropertyProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetParamProps(int32 tk,
                                        [out] int32& pmd,
                                        [out] uint32& pulSequence,
                                        [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                        uint32 cchName,
                                        [out] uint32& pchName,
                                        [out] uint32& pdwAttr,
                                        [out] uint32& pdwCPlusTypeFlag,
                                        [out] valuetype Microsoft.MetadataReader.UnusedIntPtr& ppValue,
                                        [out] uint32& pcchValue) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetParamProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetCustomAttributeByName(int32 tkObj,
                                                   string  marshal( lpwstr) szName,
                                                   [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppData,
                                                   [out] uint32& pcbData) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetCustomAttributeByName

  .method public hidebysig newslot abstract virtual 
          instance bool  IsValidToken([in] uint32  marshal( unsigned int32) tk) cil managed preservesig
  {
  } // end of method IMetadataImport2::IsValidToken

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNestedClassProps(int32 tdNestedClass,
                                              [out] int32& tdEnclosingClass) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetNestedClassProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNativeCallConvFromSig(native int pvSig,
                                                   uint32 cbSig,
                                                   [out] uint32& pCallConv) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetNativeCallConvFromSig

  .method public hidebysig newslot abstract virtual 
          instance int32  IsGlobal(uint32 pd,
                                   [out] int32& pbGlobal) cil managed preservesig
  {
  } // end of method IMetadataImport2::IsGlobal

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumGenericParams(valuetype Microsoft.MetadataReader.HCORENUM& hEnum,
                                            int32 tk,
                                            [out] int32& rGenericParams,
                                            uint32 cMax,
                                            [out] uint32& pcGenericParams) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumGenericParams

  .method public hidebysig newslot abstract virtual 
          instance int32  GetGenericParamProps(int32 gp,
                                               [out] uint32& pulParamSeq,
                                               [out] int32& pdwParamFlags,
                                               [out] int32& ptOwner,
                                               [out] int32& ptkKind,
                                               [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) wzName,
                                               uint32 cchName,
                                               [out] uint32& pchName) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetGenericParamProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetMethodSpecProps(valuetype System.Reflection.Adds.Token mi,
                                             [out] valuetype System.Reflection.Adds.Token& tkParent,
                                             [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& ppvSigBlob,
                                             [out] int32& pcbSigBlob) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetMethodSpecProps

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumGenericParamConstraints(valuetype Microsoft.MetadataReader.HCORENUM& hEnum,
                                                      int32 tk,
                                                      [out] int32& rGenericParamConstraints,
                                                      uint32 cMax,
                                                      [out] uint32& pcGenericParams) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumGenericParamConstraints

  .method public hidebysig newslot abstract virtual 
          instance int32  GetGenericParamConstraintProps(int32 gpc,
                                                         [out] int32& ptGenericParam,
                                                         [out] int32& ptkConstraintType) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetGenericParamConstraintProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetPEKind([out] valuetype Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds& dwPEKind,
                                    [out] valuetype Microsoft.VisualStudio.Debugger.Metadata.ImageFileMachine& pdwMachine) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetPEKind

  .method public hidebysig newslot abstract virtual 
          instance int32  GetVersionString([out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                           [in] int32 cchName,
                                           [out] int32& pchName) cil managed preservesig
  {
  } // end of method IMetadataImport2::GetVersionString

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumMethodSpecs(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                          int32 tk,
                                          [in] uint32[]  marshal([ + 3]) rMethodSpecs,
                                          uint32 cMax,
                                          [out] uint32& pcMethodSpecs) cil managed preservesig
  {
  } // end of method IMetadataImport2::EnumMethodSpecs

} // end of class Microsoft.MetadataReader.IMetadataImport2

.class interface public abstract auto ansi Microsoft.MetadataReader.IMetadataAssemblyImport
{
  .method public hidebysig newslot abstract virtual 
          instance int32  GetAssemblyProps([in] valuetype System.Reflection.Adds.Token assemblyToken,
                                           [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& pPublicKey,
                                           [out] int32& cbPublicKey,
                                           [out] int32& hashAlgId,
                                           [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                           [in] int32 cchName,
                                           [out] int32& pchName,
                                           [in][out] valuetype Microsoft.MetadataReader.AssemblyMetaData& pMetaData,
                                           [out] valuetype [mscorlib]System.Reflection.AssemblyNameFlags& 'flags') cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::GetAssemblyProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetAssemblyRefProps([in] valuetype System.Reflection.Adds.Token token,
                                              [out] valuetype System.Reflection.Adds.EmbeddedBlobPointer& pPublicKey,
                                              [out] int32& cbPublicKey,
                                              [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                              [in] int32 cchName,
                                              [out] int32& pchName,
                                              [in][out] valuetype Microsoft.MetadataReader.AssemblyMetaData& pMetaData,
                                              [out] valuetype Microsoft.MetadataReader.UnusedIntPtr& ppbHashValue,
                                              [out] uint32& pcbHashValue,
                                              [out] valuetype [mscorlib]System.Reflection.AssemblyNameFlags& dwAssemblyRefFlags) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::GetAssemblyRefProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetFileProps([in] int32 token,
                                       [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                       [in] int32 cchName,
                                       [out] int32& pchName,
                                       [out] valuetype Microsoft.MetadataReader.UnusedIntPtr& ppbHashValue,
                                       [out] uint32& pcbHashValue,
                                       [out] valuetype Microsoft.MetadataReader.CorFileFlags& dwFileFlags) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::GetFileProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetExportedTypeProps(int32 mdct,
                                               [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                               int32 cchName,
                                               [out] int32& pchName,
                                               [out] int32& ptkImplementation,
                                               [out] int32& ptkTypeDef,
                                               [out] valuetype Microsoft.MetadataReader.CorTypeAttr& pdwExportedTypeFlags) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::GetExportedTypeProps

  .method public hidebysig newslot abstract virtual 
          instance int32  GetManifestResourceProps([in] int32 mdmr,
                                                   [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName,
                                                   [in] int32 cchName,
                                                   [out] int32& pchName,
                                                   [out] int32& ptkImplementation,
                                                   [out] uint32& pdwOffset,
                                                   [out] valuetype Microsoft.MetadataReader.CorManifestResourceFlags& pdwResourceFlags) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::GetManifestResourceProps

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumAssemblyRefs(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                           [out] valuetype System.Reflection.Adds.Token& assemblyRefs,
                                           int32 cMax,
                                           [out] int32& cTokens) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::EnumAssemblyRefs

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumFiles(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                    [out] int32& files,
                                    int32 cMax,
                                    [out] int32& cTokens) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::EnumFiles

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumExportedTypes(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                            [out] int32& rExportedTypes,
                                            int32 cMax,
                                            [out] uint32& cTokens) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::EnumExportedTypes

  .method public hidebysig newslot abstract virtual 
          instance int32  EnumManifestResources(valuetype Microsoft.MetadataReader.HCORENUM& phEnum,
                                                [out] int32& rManifestResources,
                                                int32 cMax,
                                                [out] int32& cTokens) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::EnumManifestResources

  .method public hidebysig newslot abstract virtual 
          instance int32  GetAssemblyFromScope([out] int32& assemblyToken) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::GetAssemblyFromScope

  .method public hidebysig newslot abstract virtual 
          instance int32  FindExportedTypeByName([in] string  marshal( lpwstr) szName,
                                                 int32 mdtExportedType,
                                                 [out] int32& ptkExportedType) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::FindExportedTypeByName

  .method public hidebysig newslot abstract virtual 
          instance int32  FindManifestResourceByName(string  marshal( lpwstr) szName,
                                                     [out] int32& ptkManifestResource) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::FindManifestResourceByName

  .method public hidebysig newslot abstract virtual 
          instance int32  CloseEnum(native int hEnum) cil managed preservesig
  {
  } // end of method IMetadataAssemblyImport::CloseEnum

} // end of class Microsoft.MetadataReader.IMetadataAssemblyImport

.class public auto ansi sealed Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture Amd64 = int32(0x00000004)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture Arm = int32(0x00000005)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture IA64 = int32(0x00000003)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture MSIL = int32(0x00000001)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture None = int32(0x00000000)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture X86 = int32(0x00000002)
} // end of class Microsoft.VisualStudio.Debugger.Metadata.ProcessorArchitecture

.class public auto ansi sealed Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds ILOnly = int32(0x00000001)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds NotAPortableExecutableImage = int32(0x00000000)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds PE32Plus = int32(0x00000004)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds Preferred32Bit = int32(0x00000010)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds Required32Bit = int32(0x00000002)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds Unmanaged32Bit = int32(0x00000008)
} // end of class Microsoft.VisualStudio.Debugger.Metadata.PortableExecutableKinds

.class public auto ansi sealed Microsoft.VisualStudio.Debugger.Metadata.ImageFileMachine
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ImageFileMachine AMD64 = int32(0x00008664)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ImageFileMachine ARM = int32(0x000001C4)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ImageFileMachine I386 = int32(0x0000014C)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ImageFileMachine IA64 = int32(0x00000200)
} // end of class Microsoft.VisualStudio.Debugger.Metadata.ImageFileMachine

.class public auto ansi Microsoft.MetadataReader.AssemblyResolutionFailureException
       extends Microsoft.MetadataReader.BaseResolutionFailureException
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyResolutionFailureException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string message) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AssemblyResolutionFailureException::.ctor

} // end of class Microsoft.MetadataReader.AssemblyResolutionFailureException

.class public auto ansi Microsoft.MetadataReader.BaseResolutionFailureException
       extends [mscorlib]System.Exception
{
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor(string message) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method BaseResolutionFailureException::.ctor

} // end of class Microsoft.MetadataReader.BaseResolutionFailureException

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo
       extends Microsoft.VisualStudio.Debugger.Metadata.MethodBase
{
  .method public hidebysig newslot abstract virtual 
          instance object  Invoke(valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags invokeAttr,
                                  class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                                  object[] parameters,
                                  class [mscorlib]System.Globalization.CultureInfo culture) cil managed
  {
  } // end of method ConstructorInfo::Invoke

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ConstructorInfo::.ctor

} // end of class Microsoft.VisualStudio.Debugger.Metadata.ConstructorInfo

.class public auto ansi sealed Microsoft.MetadataReader.CorCallingConvention
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention Default = int32(0x00000000)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention ExplicitThis = int32(0x00000040)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention Field = int32(0x00000006)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention Generic = int32(0x00000010)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention GenericInst = int32(0x0000000A)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention HasThis = int32(0x00000020)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention LocalSig = int32(0x00000007)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention Mask = int32(0x0000000F)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention NativeVarArg = int32(0x0000000B)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention Property = int32(0x00000008)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention Unmanaged = int32(0x00000009)
  .field public static literal valuetype Microsoft.MetadataReader.CorCallingConvention VarArg = int32(0x00000005)
} // end of class Microsoft.MetadataReader.CorCallingConvention

.class public auto ansi sealed Microsoft.MetadataReader.CorFileFlags
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.MetadataReader.CorFileFlags ContainsMetaData = int32(0x00000000)
  .field public static literal valuetype Microsoft.MetadataReader.CorFileFlags ContainsNoMetaData = int32(0x00000001)
} // end of class Microsoft.MetadataReader.CorFileFlags

.class public auto ansi sealed Microsoft.MetadataReader.CorManifestResourceFlags
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.MetadataReader.CorManifestResourceFlags mrPrivate = int32(0x00000002)
  .field public static literal valuetype Microsoft.MetadataReader.CorManifestResourceFlags mrPublic = int32(0x00000001)
  .field public static literal valuetype Microsoft.MetadataReader.CorManifestResourceFlags mrVisibilityMask = int32(0x00000007)
} // end of class Microsoft.MetadataReader.CorManifestResourceFlags

.class public auto ansi sealed Microsoft.MetadataReader.CorTypeAttr
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdAbstract = int32(0x00000080)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdAnsiClass = int32(0x00000000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdAutoClass = int32(0x00020000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdAutoLayout = int32(0x00000000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdBeforeFieldInit = int32(0x00100000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdClass = int32(0x00000000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdClassSemanticsMask = int32(0x00000020)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdCustomFormatClass = int32(0x00030000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdCustomFormatMask = int32(0x00C00000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdExplicitLayout = int32(0x00000010)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdForwarder = int32(0x00200000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdHasSecurity = int32(0x00040000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdImport = int32(0x00001000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdInterface = int32(0x00000020)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdLayoutMask = int32(0x00000018)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdNestedAssembly = int32(0x00000005)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdNestedFamANDAssem = int32(0x00000006)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdNestedFamORAssem = int32(0x00000007)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdNestedFamily = int32(0x00000004)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdNestedPrivate = int32(0x00000003)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdNestedPublic = int32(0x00000002)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdNotPublic = int32(0x00000000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdPublic = int32(0x00000001)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdRTSpecialName = int32(0x00000800)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdReservedMask = int32(0x00040800)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdSealed = int32(0x00000100)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdSequentialLayout = int32(0x00000008)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdSerializable = int32(0x00002000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdSpecialName = int32(0x00000400)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdStringFormatMask = int32(0x00030000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdUnicodeClass = int32(0x00010000)
  .field public static literal valuetype Microsoft.MetadataReader.CorTypeAttr tdVisibilityMask = int32(0x00000007)
} // end of class Microsoft.MetadataReader.CorTypeAttr

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.MethodBase
       extends Microsoft.VisualStudio.Debugger.Metadata.MemberInfo
{
  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Reflection.MethodAttributes 
          get_Attributes() cil managed
  {
  } // end of method MethodBase::get_Attributes

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Reflection.CallingConventions 
          get_CallingConvention() cil managed
  {
  } // end of method MethodBase::get_CallingConvention

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo[] 
          GetParameters() cil managed
  {
  } // end of method MethodBase::GetParameters

  .method public hidebysig specialname instance bool 
          get_IsPublic() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsPublic

  .method public hidebysig specialname instance bool 
          get_IsPrivate() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsPrivate

  .method public hidebysig specialname instance bool 
          get_IsFamily() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsFamily

  .method public hidebysig specialname instance bool 
          get_IsAssembly() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsAssembly

  .method public hidebysig specialname instance bool 
          get_IsFamilyAndAssembly() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsFamilyAndAssembly

  .method public hidebysig specialname instance bool 
          get_IsFamilyOrAssembly() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsFamilyOrAssembly

  .method public hidebysig specialname instance bool 
          get_IsStatic() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsStatic

  .method public hidebysig specialname instance bool 
          get_IsFinal() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsFinal

  .method public hidebysig specialname instance bool 
          get_IsVirtual() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsVirtual

  .method public hidebysig specialname instance bool 
          get_IsHideBySig() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsHideBySig

  .method public hidebysig specialname instance bool 
          get_IsAbstract() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsAbstract

  .method public hidebysig specialname instance bool 
          get_IsSpecialName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsSpecialName

  .method public hidebysig specialname instance bool 
          get_IsConstructor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsConstructor

  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_IsGenericMethodDefinition() cil managed
  {
  } // end of method MethodBase::get_IsGenericMethodDefinition

  .method public hidebysig newslot specialname virtual 
          instance bool  get_IsGenericMethod() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_IsGenericMethod

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetGenericArguments() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::GetGenericArguments

  .method public hidebysig newslot specialname virtual 
          instance bool  get_ContainsGenericParameters() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_ContainsGenericParameters

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodBody 
          GetMethodBody() cil managed
  {
  } // end of method MethodBase::GetMethodBody

  .method public hidebysig newslot abstract virtual 
          instance valuetype [mscorlib]System.Reflection.MethodImplAttributes 
          GetMethodImplementationFlags() cil managed
  {
  } // end of method MethodBase::GetMethodImplementationFlags

  .method public hidebysig newslot abstract virtual 
          instance object  Invoke(object obj,
                                  valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags invokeAttr,
                                  class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                                  object[] parameters,
                                  class [mscorlib]System.Globalization.CultureInfo culture) cil managed
  {
  } // end of method MethodBase::Invoke

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.RuntimeMethodHandle 
          get_MethodHandle() cil managed
  {
  } // end of method MethodBase::get_MethodHandle

  .method public hidebysig specialname instance string 
          get_FullName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::get_FullName

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MethodBase::.ctor

  .property instance valuetype [mscorlib]System.Reflection.MethodAttributes
          Attributes()
  {
    .get instance valuetype [mscorlib]System.Reflection.MethodAttributes Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_Attributes()
  } // end of property MethodBase::Attributes
  .property instance valuetype [mscorlib]System.Reflection.CallingConventions
          CallingConvention()
  {
    .get instance valuetype [mscorlib]System.Reflection.CallingConventions Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_CallingConvention()
  } // end of property MethodBase::CallingConvention
  .property instance bool ContainsGenericParameters()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_ContainsGenericParameters()
  } // end of property MethodBase::ContainsGenericParameters
  .property instance string FullName()
  {
    .get instance string Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_FullName()
  } // end of property MethodBase::FullName
  .property instance bool IsAbstract()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsAbstract()
  } // end of property MethodBase::IsAbstract
  .property instance bool IsAssembly()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsAssembly()
  } // end of property MethodBase::IsAssembly
  .property instance bool IsConstructor()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsConstructor()
  } // end of property MethodBase::IsConstructor
  .property instance bool IsFamily()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsFamily()
  } // end of property MethodBase::IsFamily
  .property instance bool IsFamilyAndAssembly()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsFamilyAndAssembly()
  } // end of property MethodBase::IsFamilyAndAssembly
  .property instance bool IsFamilyOrAssembly()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsFamilyOrAssembly()
  } // end of property MethodBase::IsFamilyOrAssembly
  .property instance bool IsFinal()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsFinal()
  } // end of property MethodBase::IsFinal
  .property instance bool IsGenericMethod()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsGenericMethod()
  } // end of property MethodBase::IsGenericMethod
  .property instance bool IsGenericMethodDefinition()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsGenericMethodDefinition()
  } // end of property MethodBase::IsGenericMethodDefinition
  .property instance bool IsHideBySig()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsHideBySig()
  } // end of property MethodBase::IsHideBySig
  .property instance bool IsPrivate()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsPrivate()
  } // end of property MethodBase::IsPrivate
  .property instance bool IsPublic()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsPublic()
  } // end of property MethodBase::IsPublic
  .property instance bool IsSpecialName()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsSpecialName()
  } // end of property MethodBase::IsSpecialName
  .property instance bool IsStatic()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsStatic()
  } // end of property MethodBase::IsStatic
  .property instance bool IsVirtual()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_IsVirtual()
  } // end of property MethodBase::IsVirtual
  .property instance valuetype [mscorlib]System.RuntimeMethodHandle
          MethodHandle()
  {
    .get instance valuetype [mscorlib]System.RuntimeMethodHandle Microsoft.VisualStudio.Debugger.Metadata.MethodBase::get_MethodHandle()
  } // end of property MethodBase::MethodHandle
} // end of class Microsoft.VisualStudio.Debugger.Metadata.MethodBase

.class public sequential ansi sealed Microsoft.MetadataReader.HCORENUM
       extends [mscorlib]System.ValueType
{
  .method public hidebysig instance void 
          Close(class Microsoft.MetadataReader.IMetadataImport 'import') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method HCORENUM::Close

  .method public hidebysig instance void 
          Close(class Microsoft.MetadataReader.IMetadataImport2 'import') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method HCORENUM::Close

  .method public hidebysig instance void 
          Close(class Microsoft.MetadataReader.IMetadataAssemblyImport 'import') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method HCORENUM::Close

} // end of class Microsoft.MetadataReader.HCORENUM

.class public sequential ansi sealed System.Reflection.Adds.EmbeddedBlobPointer
       extends [mscorlib]System.ValueType
{
} // end of class System.Reflection.Adds.EmbeddedBlobPointer

.class interface public abstract auto ansi Microsoft.MetadataReader.IMetadataTables
{
  .method public hidebysig newslot abstract virtual 
          instance int32  GetStringHeapSize([out] uint32& countBytesStrings) cil managed preservesig
  {
  } // end of method IMetadataTables::GetStringHeapSize

  .method public hidebysig newslot abstract virtual 
          instance int32  GetBlobHeapSize([out] uint32& countBytesBlobs) cil managed preservesig
  {
  } // end of method IMetadataTables::GetBlobHeapSize

  .method public hidebysig newslot abstract virtual 
          instance int32  GetGuidHeapSize([out] uint32& countBytesGuids) cil managed preservesig
  {
  } // end of method IMetadataTables::GetGuidHeapSize

  .method public hidebysig newslot abstract virtual 
          instance int32  GetUserStringHeapSize([out] uint32& countByteBlobs) cil managed preservesig
  {
  } // end of method IMetadataTables::GetUserStringHeapSize

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNumTables([out] uint32& countTables) cil managed preservesig
  {
  } // end of method IMetadataTables::GetNumTables

  .method public hidebysig newslot abstract virtual 
          instance int32  GetTableIndex(uint32 token,
                                        [out] uint32& tableIndex) cil managed preservesig
  {
  } // end of method IMetadataTables::GetTableIndex

  .method public hidebysig newslot abstract virtual 
          instance int32  GetTableInfo(valuetype System.Reflection.Adds.MetadataTable tableIndex,
                                       [out] int32& countByteRows,
                                       [out] int32& countRows,
                                       [out] int32& countColumns,
                                       [out] int32& columnPrimaryKey,
                                       [out] valuetype Microsoft.MetadataReader.UnusedIntPtr& name) cil managed preservesig
  {
  } // end of method IMetadataTables::GetTableInfo

  .method public hidebysig newslot abstract virtual 
          instance int32  GetColumnInfo(uint32 ixTbl,
                                        uint32 ixCol,
                                        [out] uint32& poCol,
                                        [out] uint32& pType,
                                        [out] uint32& pcbCol,
                                        [out] native int& ppName) cil managed preservesig
  {
  } // end of method IMetadataTables::GetColumnInfo

  .method public hidebysig newslot abstract virtual 
          instance int32  GetCodedTokenInfo(uint32 ixCdTkn,
                                            [out] uint32& pcTokens,
                                            [out] native int& ppTokens,
                                            [out] native int& ppName) cil managed preservesig
  {
  } // end of method IMetadataTables::GetCodedTokenInfo

  .method public hidebysig newslot abstract virtual 
          instance int32  GetRow(uint32 ixTbl,
                                 uint32 rid,
                                 [out] native int& ppRow) cil managed preservesig
  {
  } // end of method IMetadataTables::GetRow

  .method public hidebysig newslot abstract virtual 
          instance int32  GetColumn(uint32 ixTbl,
                                    uint32 ixCol,
                                    uint32 rid,
                                    [out] native int& pVal) cil managed preservesig
  {
  } // end of method IMetadataTables::GetColumn

  .method public hidebysig newslot abstract virtual 
          instance int32  GetString(uint32 ixString,
                                    [out] native int& ppString) cil managed preservesig
  {
  } // end of method IMetadataTables::GetString

  .method public hidebysig newslot abstract virtual 
          instance int32  GetBlob(uint32 ixBlob,
                                  [out] uint32& pcbData,
                                  [out] native int& ppData) cil managed preservesig
  {
  } // end of method IMetadataTables::GetBlob

  .method public hidebysig newslot abstract virtual 
          instance int32  GetGuid(uint32 ixGuid,
                                  [out] native int& ppGUID) cil managed preservesig
  {
  } // end of method IMetadataTables::GetGuid

  .method public hidebysig newslot abstract virtual 
          instance int32  GetUserString(uint32 ixUserString,
                                        uint32 pcbData,
                                        [out] native int& ppData) cil managed preservesig
  {
  } // end of method IMetadataTables::GetUserString

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNextString(uint32 ixString,
                                        [out] uint32& pNext) cil managed preservesig
  {
  } // end of method IMetadataTables::GetNextString

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNextBlob(uint32 ixBlob,
                                      [out] uint32& pNext) cil managed preservesig
  {
  } // end of method IMetadataTables::GetNextBlob

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNextGuid(uint32 ixGuid,
                                      [out] uint32& pNext) cil managed preservesig
  {
  } // end of method IMetadataTables::GetNextGuid

  .method public hidebysig newslot abstract virtual 
          instance int32  GetNextUserString(uint32 ixUserString,
                                            [out] uint32& pNext) cil managed preservesig
  {
  } // end of method IMetadataTables::GetNextUserString

} // end of class Microsoft.MetadataReader.IMetadataTables

.class public auto ansi sealed System.Reflection.Adds.MetadataTable
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype System.Reflection.Adds.MetadataTable Assembly = int32(0x00000020)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable AssemblyRef = int32(0x00000023)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable CustomAttribute = int32(0x0000000C)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable Event = int32(0x00000014)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable ExportedType = int32(0x00000027)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable FieldDef = int32(0x00000004)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable File = int32(0x00000026)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable GenericPar = int32(0x0000002A)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable InterfaceImpl = int32(0x00000009)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable ManifestResource = int32(0x00000028)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable MemberRef = int32(0x0000000A)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable MethodDef = int32(0x00000006)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable MethodSpec = int32(0x0000002B)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable Module = int32(0x00000000)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable ModuleRef = int32(0x0000001A)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable ParamDef = int32(0x00000008)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable Permission = int32(0x0000000E)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable Property = int32(0x00000017)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable Signature = int32(0x00000011)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable TypeDef = int32(0x00000002)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable TypeRef = int32(0x00000001)
  .field public static literal valuetype System.Reflection.Adds.MetadataTable TypeSpec = int32(0x0000001B)
} // end of class System.Reflection.Adds.MetadataTable

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.FieldInfo
       extends Microsoft.VisualStudio.Debugger.Metadata.MemberInfo
{
  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Reflection.FieldAttributes 
          get_Attributes() cil managed
  {
  } // end of method FieldInfo::get_Attributes

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_FieldType() cil managed
  {
  } // end of method FieldInfo::get_FieldType

  .method public hidebysig specialname instance bool 
          get_IsPublic() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsPublic

  .method public hidebysig specialname instance bool 
          get_IsPrivate() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsPrivate

  .method public hidebysig specialname instance bool 
          get_IsFamily() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsFamily

  .method public hidebysig specialname instance bool 
          get_IsAssembly() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsAssembly

  .method public hidebysig specialname instance bool 
          get_IsFamilyAndAssembly() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsFamilyAndAssembly

  .method public hidebysig specialname instance bool 
          get_IsFamilyOrAssembly() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsFamilyOrAssembly

  .method public hidebysig specialname instance bool 
          get_IsStatic() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsStatic

  .method public hidebysig specialname instance bool 
          get_IsInitOnly() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsInitOnly

  .method public hidebysig specialname instance bool 
          get_IsLiteral() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsLiteral

  .method public hidebysig specialname instance bool 
          get_IsNotSerialized() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsNotSerialized

  .method public hidebysig specialname instance bool 
          get_IsSpecialName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsSpecialName

  .method public hidebysig specialname instance bool 
          get_IsPinvokeImpl() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::get_IsPinvokeImpl

  .method public hidebysig newslot abstract virtual 
          instance object  GetValue(object obj) cil managed
  {
  } // end of method FieldInfo::GetValue

  .method public hidebysig newslot abstract virtual 
          instance object  GetRawConstantValue() cil managed
  {
  } // end of method FieldInfo::GetRawConstantValue

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.RuntimeFieldHandle 
          get_FieldHandle() cil managed
  {
  } // end of method FieldInfo::get_FieldHandle

  .method public hidebysig newslot abstract virtual 
          instance void  SetValue(object obj,
                                  object 'value',
                                  valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags invokeAttr,
                                  class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                                  class [mscorlib]System.Globalization.CultureInfo culture) cil managed
  {
  } // end of method FieldInfo::SetValue

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetOptionalCustomModifiers() cil managed
  {
  } // end of method FieldInfo::GetOptionalCustomModifiers

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type[] 
          GetRequiredCustomModifiers() cil managed
  {
  } // end of method FieldInfo::GetRequiredCustomModifiers

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FieldInfo::.ctor

  .property instance valuetype [mscorlib]System.Reflection.FieldAttributes
          Attributes()
  {
    .get instance valuetype [mscorlib]System.Reflection.FieldAttributes Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_Attributes()
  } // end of property FieldInfo::Attributes
  .property instance valuetype [mscorlib]System.RuntimeFieldHandle
          FieldHandle()
  {
    .get instance valuetype [mscorlib]System.RuntimeFieldHandle Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_FieldHandle()
  } // end of property FieldInfo::FieldHandle
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          FieldType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_FieldType()
  } // end of property FieldInfo::FieldType
  .property instance bool IsAssembly()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsAssembly()
  } // end of property FieldInfo::IsAssembly
  .property instance bool IsFamily()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsFamily()
  } // end of property FieldInfo::IsFamily
  .property instance bool IsFamilyAndAssembly()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsFamilyAndAssembly()
  } // end of property FieldInfo::IsFamilyAndAssembly
  .property instance bool IsFamilyOrAssembly()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsFamilyOrAssembly()
  } // end of property FieldInfo::IsFamilyOrAssembly
  .property instance bool IsInitOnly()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsInitOnly()
  } // end of property FieldInfo::IsInitOnly
  .property instance bool IsLiteral()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsLiteral()
  } // end of property FieldInfo::IsLiteral
  .property instance bool IsNotSerialized()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsNotSerialized()
  } // end of property FieldInfo::IsNotSerialized
  .property instance bool IsPinvokeImpl()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsPinvokeImpl()
  } // end of property FieldInfo::IsPinvokeImpl
  .property instance bool IsPrivate()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsPrivate()
  } // end of property FieldInfo::IsPrivate
  .property instance bool IsPublic()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsPublic()
  } // end of property FieldInfo::IsPublic
  .property instance bool IsSpecialName()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsSpecialName()
  } // end of property FieldInfo::IsSpecialName
  .property instance bool IsStatic()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.FieldInfo::get_IsStatic()
  } // end of property FieldInfo::IsStatic
} // end of class Microsoft.VisualStudio.Debugger.Metadata.FieldInfo

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.EventInfo
       extends Microsoft.VisualStudio.Debugger.Metadata.MemberInfo
{
  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Reflection.EventAttributes 
          get_Attributes() cil managed
  {
  } // end of method EventInfo::get_Attributes

  .method public hidebysig newslot specialname virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_EventHandlerType() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::get_EventHandlerType

  .method public hidebysig specialname instance bool 
          get_IsMulticast() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::get_IsMulticast

  .method family hidebysig newslot virtual 
          instance bool  GetIsMulticastImpl() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::GetIsMulticastImpl

  .method public hidebysig specialname instance bool 
          get_IsSpecialName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::get_IsSpecialName

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetAddMethod() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::GetAddMethod

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetAddMethod(bool nonPublic) cil managed
  {
  } // end of method EventInfo::GetAddMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetRaiseMethod() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::GetRaiseMethod

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetRaiseMethod(bool nonPublic) cil managed
  {
  } // end of method EventInfo::GetRaiseMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetRemoveMethod() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::GetRemoveMethod

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetRemoveMethod(bool nonPublic) cil managed
  {
  } // end of method EventInfo::GetRemoveMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo[] 
          GetOtherMethods() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::GetOtherMethods

  .method public hidebysig newslot virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo[] 
          GetOtherMethods(bool nonPublic) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::GetOtherMethods

  .method public hidebysig instance void 
          AddEventHandler(object target,
                          class [mscorlib]System.Delegate 'handler') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::AddEventHandler

  .method family hidebysig newslot virtual 
          instance void  AddEventHandlerImpl(object target,
                                             class [mscorlib]System.Delegate 'handler') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::AddEventHandlerImpl

  .method public hidebysig instance void 
          RemoveEventHandler(object target,
                             class [mscorlib]System.Delegate 'handler') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::RemoveEventHandler

  .method family hidebysig newslot virtual 
          instance void  RemoveEventHandlerImpl(object target,
                                                class [mscorlib]System.Delegate 'handler') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::RemoveEventHandlerImpl

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method EventInfo::.ctor

  .property instance valuetype [mscorlib]System.Reflection.EventAttributes
          Attributes()
  {
    .get instance valuetype [mscorlib]System.Reflection.EventAttributes Microsoft.VisualStudio.Debugger.Metadata.EventInfo::get_Attributes()
  } // end of property EventInfo::Attributes
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          EventHandlerType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.EventInfo::get_EventHandlerType()
  } // end of property EventInfo::EventHandlerType
  .property instance bool IsMulticast()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.EventInfo::get_IsMulticast()
  } // end of property EventInfo::IsMulticast
  .property instance bool IsSpecialName()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.EventInfo::get_IsSpecialName()
  } // end of property EventInfo::IsSpecialName
} // end of class Microsoft.VisualStudio.Debugger.Metadata.EventInfo

.class public sequential ansi sealed Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier
       extends [mscorlib]System.ValueType
{
} // end of class Microsoft.VisualStudio.Debugger.Metadata.ParameterModifier

.class public auto ansi serializable sealed Microsoft.VisualStudio.Debugger.Metadata.TypeCode
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Boolean = int32(0x00000003)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Byte = int32(0x00000006)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Char = int32(0x00000004)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode DBNull = int32(0x00000002)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode DateTime = int32(0x00000010)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Decimal = int32(0x0000000F)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Double = int32(0x0000000E)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Empty = int32(0x00000000)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Int16 = int32(0x00000007)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Int32 = int32(0x00000009)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Int64 = int32(0x0000000B)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Object = int32(0x00000001)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode SByte = int32(0x00000005)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode Single = int32(0x0000000D)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode String = int32(0x00000012)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode UInt16 = int32(0x00000008)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode UInt32 = int32(0x0000000A)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.TypeCode UInt64 = int32(0x0000000C)
} // end of class Microsoft.VisualStudio.Debugger.Metadata.TypeCode

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo
       extends Microsoft.VisualStudio.Debugger.Metadata.MemberInfo
{
  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Reflection.PropertyAttributes 
          get_Attributes() cil managed
  {
  } // end of method PropertyInfo::get_Attributes

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_PropertyType() cil managed
  {
  } // end of method PropertyInfo::get_PropertyType

  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_CanRead() cil managed
  {
  } // end of method PropertyInfo::get_CanRead

  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_CanWrite() cil managed
  {
  } // end of method PropertyInfo::get_CanWrite

  .method public hidebysig specialname instance bool 
          get_IsSpecialName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PropertyInfo::get_IsSpecialName

  .method public hidebysig newslot abstract virtual 
          instance object  GetConstantValue() cil managed
  {
  } // end of method PropertyInfo::GetConstantValue

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo[] 
          GetAccessors() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PropertyInfo::GetAccessors

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo[] 
          GetAccessors(bool nonPublic) cil managed
  {
  } // end of method PropertyInfo::GetAccessors

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetGetMethod() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PropertyInfo::GetGetMethod

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetGetMethod(bool nonPublic) cil managed
  {
  } // end of method PropertyInfo::GetGetMethod

  .method public hidebysig instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetSetMethod() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PropertyInfo::GetSetMethod

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.MethodInfo 
          GetSetMethod(bool nonPublic) cil managed
  {
  } // end of method PropertyInfo::GetSetMethod

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo[] 
          GetIndexParameters() cil managed
  {
  } // end of method PropertyInfo::GetIndexParameters

  .method public hidebysig newslot abstract virtual 
          instance object  GetValue(object obj,
                                    valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags invokeAttr,
                                    class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                                    object[] index,
                                    class [mscorlib]System.Globalization.CultureInfo culture) cil managed
  {
  } // end of method PropertyInfo::GetValue

  .method public hidebysig newslot abstract virtual 
          instance void  SetValue(object obj,
                                  object 'value',
                                  valuetype Microsoft.VisualStudio.Debugger.Metadata.BindingFlags invokeAttr,
                                  class Microsoft.VisualStudio.Debugger.Metadata.Binder binder,
                                  object[] index,
                                  class [mscorlib]System.Globalization.CultureInfo culture) cil managed
  {
  } // end of method PropertyInfo::SetValue

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PropertyInfo::.ctor

  .property instance valuetype [mscorlib]System.Reflection.PropertyAttributes
          Attributes()
  {
    .get instance valuetype [mscorlib]System.Reflection.PropertyAttributes Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo::get_Attributes()
  } // end of property PropertyInfo::Attributes
  .property instance bool CanRead()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo::get_CanRead()
  } // end of property PropertyInfo::CanRead
  .property instance bool CanWrite()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo::get_CanWrite()
  } // end of property PropertyInfo::CanWrite
  .property instance bool IsSpecialName()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo::get_IsSpecialName()
  } // end of property PropertyInfo::IsSpecialName
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          PropertyType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo::get_PropertyType()
  } // end of property PropertyInfo::PropertyType
} // end of class Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo

.class public sequential ansi sealed Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument
       extends [mscorlib]System.ValueType
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class Microsoft.VisualStudio.Debugger.Metadata.Type argumentType,
                               object 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeTypedArgument::.ctor

  .method public hidebysig specialname instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_ArgumentType() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeTypedArgument::get_ArgumentType

  .method public hidebysig specialname instance object 
          get_Value() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeTypedArgument::get_Value

  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          ArgumentType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument::get_ArgumentType()
  } // end of property CustomAttributeTypedArgument::ArgumentType
  .property instance object Value()
  {
    .get instance object Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument::get_Value()
  } // end of property CustomAttributeTypedArgument::Value
} // end of class Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument

.class public sequential ansi sealed Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeNamedArgument
       extends [mscorlib]System.ValueType
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo memberInfo,
                               valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument typedValue) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeNamedArgument::.ctor

  .method public hidebysig specialname instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo 
          get_MemberInfo() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeNamedArgument::get_MemberInfo

  .method public hidebysig specialname instance valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument 
          get_TypedValue() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method CustomAttributeNamedArgument::get_TypedValue

  .property instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo
          MemberInfo()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeNamedArgument::get_MemberInfo()
  } // end of property CustomAttributeNamedArgument::MemberInfo
  .property instance valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument
          TypedValue()
  {
    .get instance valuetype Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeTypedArgument Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeNamedArgument::get_TypedValue()
  } // end of property CustomAttributeNamedArgument::TypedValue
} // end of class Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeNamedArgument

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.LocalVariableInfo
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_IsPinned() cil managed
  {
  } // end of method LocalVariableInfo::get_IsPinned

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_LocalIndex() cil managed
  {
  } // end of method LocalVariableInfo::get_LocalIndex

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_LocalType() cil managed
  {
  } // end of method LocalVariableInfo::get_LocalType

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LocalVariableInfo::ToString

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LocalVariableInfo::.ctor

  .property instance bool IsPinned()
  {
    .get instance bool Microsoft.VisualStudio.Debugger.Metadata.LocalVariableInfo::get_IsPinned()
  } // end of property LocalVariableInfo::IsPinned
  .property instance int32 LocalIndex()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.LocalVariableInfo::get_LocalIndex()
  } // end of property LocalVariableInfo::LocalIndex
  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          LocalType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.LocalVariableInfo::get_LocalType()
  } // end of property LocalVariableInfo::LocalType
} // end of class Microsoft.VisualStudio.Debugger.Metadata.LocalVariableInfo

.class public abstract auto ansi Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause
       extends [mscorlib]System.Object
{
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ExceptionHandlingClause::ToString

  .method public hidebysig newslot specialname virtual 
          instance class Microsoft.VisualStudio.Debugger.Metadata.Type 
          get_CatchType() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ExceptionHandlingClause::get_CatchType

  .method public hidebysig newslot specialname virtual 
          instance int32  get_FilterOffset() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ExceptionHandlingClause::get_FilterOffset

  .method public hidebysig newslot specialname virtual 
          instance valuetype Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClauseOptions 
          get_Flags() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ExceptionHandlingClause::get_Flags

  .method public hidebysig newslot specialname virtual 
          instance int32  get_HandlerLength() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ExceptionHandlingClause::get_HandlerLength

  .method public hidebysig newslot specialname virtual 
          instance int32  get_HandlerOffset() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ExceptionHandlingClause::get_HandlerOffset

  .method public hidebysig newslot specialname virtual 
          instance int32  get_TryLength() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ExceptionHandlingClause::get_TryLength

  .method public hidebysig newslot specialname virtual 
          instance int32  get_TryOffset() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ExceptionHandlingClause::get_TryOffset

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ExceptionHandlingClause::.ctor

  .property instance class Microsoft.VisualStudio.Debugger.Metadata.Type
          CatchType()
  {
    .get instance class Microsoft.VisualStudio.Debugger.Metadata.Type Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause::get_CatchType()
  } // end of property ExceptionHandlingClause::CatchType
  .property instance int32 FilterOffset()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause::get_FilterOffset()
  } // end of property ExceptionHandlingClause::FilterOffset
  .property instance valuetype Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClauseOptions
          Flags()
  {
    .get instance valuetype Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClauseOptions Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause::get_Flags()
  } // end of property ExceptionHandlingClause::Flags
  .property instance int32 HandlerLength()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause::get_HandlerLength()
  } // end of property ExceptionHandlingClause::HandlerLength
  .property instance int32 HandlerOffset()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause::get_HandlerOffset()
  } // end of property ExceptionHandlingClause::HandlerOffset
  .property instance int32 TryLength()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause::get_TryLength()
  } // end of property ExceptionHandlingClause::TryLength
  .property instance int32 TryOffset()
  {
    .get instance int32 Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause::get_TryOffset()
  } // end of property ExceptionHandlingClause::TryOffset
} // end of class Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClause

.class public auto ansi sealed System.Reflection.Adds.CorElementType
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype System.Reflection.Adds.CorElementType Array = int32(0x00000014)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Bool = int32(0x00000002)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Byref = int32(0x00000010)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Byte = int32(0x00000005)
  .field public static literal valuetype System.Reflection.Adds.CorElementType CModOpt = int32(0x00000020)
  .field public static literal valuetype System.Reflection.Adds.CorElementType CModReqd = int32(0x0000001F)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Char = int32(0x00000003)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Class = int32(0x00000012)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Double = int32(0x0000000D)
  .field public static literal valuetype System.Reflection.Adds.CorElementType End = int32(0x00000000)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Enum = int32(0x00000055)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Float = int32(0x0000000C)
  .field public static literal valuetype System.Reflection.Adds.CorElementType FnPtr = int32(0x0000001B)
  .field public static literal valuetype System.Reflection.Adds.CorElementType GenericInstantiation = int32(0x00000015)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Int = int32(0x00000008)
  .field public static literal valuetype System.Reflection.Adds.CorElementType IntPtr = int32(0x00000018)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Internal = int32(0x00000021)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Long = int32(0x0000000A)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Max = int32(0x00000022)
  .field public static literal valuetype System.Reflection.Adds.CorElementType MethodVar = int32(0x0000001E)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Modifier = int32(0x00000040)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Object = int32(0x0000001C)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Pinned = int32(0x00000045)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Pointer = int32(0x0000000F)
  .field public static literal valuetype System.Reflection.Adds.CorElementType SByte = int32(0x00000004)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Sentinel = int32(0x00000041)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Short = int32(0x00000006)
  .field public static literal valuetype System.Reflection.Adds.CorElementType String = int32(0x0000000E)
  .field public static literal valuetype System.Reflection.Adds.CorElementType SzArray = int32(0x0000001D)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Type = int32(0x00000050)
  .field public static literal valuetype System.Reflection.Adds.CorElementType TypeVar = int32(0x00000013)
  .field public static literal valuetype System.Reflection.Adds.CorElementType TypedByRef = int32(0x00000016)
  .field public static literal valuetype System.Reflection.Adds.CorElementType UInt = int32(0x00000009)
  .field public static literal valuetype System.Reflection.Adds.CorElementType UIntPtr = int32(0x00000019)
  .field public static literal valuetype System.Reflection.Adds.CorElementType ULong = int32(0x0000000B)
  .field public static literal valuetype System.Reflection.Adds.CorElementType UShort = int32(0x00000007)
  .field public static literal valuetype System.Reflection.Adds.CorElementType ValueType = int32(0x00000011)
  .field public static literal valuetype System.Reflection.Adds.CorElementType Void = int32(0x00000001)
} // end of class System.Reflection.Adds.CorElementType

.class public auto ansi sealed Microsoft.VisualStudio.Debugger.Metadata.TypeFilter
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method TypeFilter::.ctor

  .method public hidebysig newslot virtual 
          instance bool  Invoke(class Microsoft.VisualStudio.Debugger.Metadata.Type t,
                                object filterCriteria) runtime managed
  {
  } // end of method TypeFilter::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class Microsoft.VisualStudio.Debugger.Metadata.Type t,
                      object filterCriteria,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method TypeFilter::BeginInvoke

  .method public hidebysig newslot virtual 
          instance bool  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method TypeFilter::EndInvoke

} // end of class Microsoft.VisualStudio.Debugger.Metadata.TypeFilter

.class public auto ansi Microsoft.MetadataReader.ModuleResolutionFailureException
       extends Microsoft.MetadataReader.BaseResolutionFailureException
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ModuleResolutionFailureException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string message) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ModuleResolutionFailureException::.ctor

} // end of class Microsoft.MetadataReader.ModuleResolutionFailureException

.class public auto ansi sealed Microsoft.VisualStudio.Debugger.Metadata.MemberFilter
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method MemberFilter::.ctor

  .method public hidebysig newslot virtual 
          instance bool  Invoke(class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo m,
                                object filterCriteria) runtime managed
  {
  } // end of method MemberFilter::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class Microsoft.VisualStudio.Debugger.Metadata.MemberInfo m,
                      object filterCriteria,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method MemberFilter::BeginInvoke

  .method public hidebysig newslot virtual 
          instance bool  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method MemberFilter::EndInvoke

} // end of class Microsoft.VisualStudio.Debugger.Metadata.MemberFilter

.class public auto ansi sealed Microsoft.MetadataReader.enum_CorMethodAttr
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdAbstract = int32(0x00000400)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdAssem = int32(0x00000003)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdCheckAccessOnOverride = int32(0x00000200)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdFamANDAssem = int32(0x00000002)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdFamORAssem = int32(0x00000005)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdFamily = int32(0x00000004)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdFinal = int32(0x00000020)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdHasSecurity = int32(0x00004000)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdHideBySig = int32(0x00000080)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdMemberAccessMask = int32(0x00000007)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdNewSlot = int32(0x00000100)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdPinvokeImpl = int32(0x00002000)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdPrivate = int32(0x00000001)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdPrivateScope = int32(0x00000000)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdPublic = int32(0x00000006)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdRTSpecialName = int32(0x00001000)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdRequireSecObject = int32(0x00008000)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdReservedMask = int32(0x0000D000)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdReuseSlot = int32(0x00000000)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdSpecialName = int32(0x00000800)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdStatic = int32(0x00000010)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdUnmanagedExport = int32(0x00000008)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdVirtual = int32(0x00000040)
  .field public static literal valuetype Microsoft.MetadataReader.enum_CorMethodAttr mdVtableLayoutMask = int32(0x00000100)
} // end of class Microsoft.MetadataReader.enum_CorMethodAttr

.class public auto ansi sealed Microsoft.VisualStudio.Debugger.Metadata.AssemblyContentType
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.AssemblyContentType Default = int32(0x00000000)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.AssemblyContentType WindowsRuntime = int32(0x00000001)
} // end of class Microsoft.VisualStudio.Debugger.Metadata.AssemblyContentType

.class public auto ansi sealed Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClauseOptions
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClauseOptions Clause = int32(0x00000000)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClauseOptions Fault = int32(0x00000004)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClauseOptions Filter = int32(0x00000001)
  .field public static literal valuetype Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClauseOptions Finally = int32(0x00000002)
} // end of class Microsoft.VisualStudio.Debugger.Metadata.ExceptionHandlingClauseOptions

.class public auto ansi sealed System.Reflection.Adds.TokenType
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype System.Reflection.Adds.TokenType Assembly = int32(0x20000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType AssemblyRef = int32(0x23000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType BaseType = int32(0x72000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType CustomAttribute = int32(0x0C000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType Event = int32(0x14000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType ExportedType = int32(0x27000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType FieldDef = int32(0x04000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType File = int32(0x26000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType GenericPar = int32(0x2A000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType InterfaceImpl = int32(0x09000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType Invalid = int32(0x7FFFFFFF)
  .field public static literal valuetype System.Reflection.Adds.TokenType ManifestResource = int32(0x28000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType MemberRef = int32(0x0A000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType MethodDef = int32(0x06000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType MethodSpec = int32(0x2B000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType Module = int32(0x00000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType ModuleRef = int32(0x1A000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType Name = int32(0x71000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType ParamDef = int32(0x08000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType Permission = int32(0x0E000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType Property = int32(0x17000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType Signature = int32(0x11000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType String = int32(0x70000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType TypeDef = int32(0x02000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType TypeRef = int32(0x01000000)
  .field public static literal valuetype System.Reflection.Adds.TokenType TypeSpec = int32(0x1B000000)
} // end of class System.Reflection.Adds.TokenType


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
