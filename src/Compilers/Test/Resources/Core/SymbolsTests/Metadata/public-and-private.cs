// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This relates to VSO bug 217681 https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217681
// The code below is compiled with a modified compiler such that MetadataWriter will set both public and private flags on M, F, C2 and M2.
// But the constructors should be left public.
// You can also see the resulting meta info at the bottom

public class C
{
   public void M() { System.Console.WriteLine("I am C!"); }
   public int F = 42;

   public class C2
   {
      public void M2() { System.Console.WriteLine("I am C2!"); }
   }
}

string metainfo = @"
===========================================================
ScopeName : public-and-private.dll
MVID      : {3C7CF08B-32C8-4C36-BD58-85DEB7FA5843}
===========================================================
Global functions
-------------------------------------------------------

Global fields
-------------------------------------------------------

Global MemberRefs
-------------------------------------------------------

TypeDef #1 (02000002)
-------------------------------------------------------
	TypDefName: C  (02000002)
	Flags     : [Public] [AutoLayout] [Class] [AnsiClass] [BeforeFieldInit]  (00100001)
	Extends   : 01000005 [TypeRef] System.Object
	Field #1 (04000001)
	-------------------------------------------------------
		Field Name: F (04000001)
		Flags     : [none] (00000007)
		CallCnvntn: [FIELD]
		Field type:  I4

	Method #1 (06000001)
	-------------------------------------------------------
		MethodName: M (06000001)
		Flags     : [HideBySig] [ReuseSlot]  (00000087)
		RVA       : 0x00002050
		ImplFlags : [IL] [Managed]  (00000000)
		CallCnvntn: [DEFAULT]
		hasThis
		ReturnType: Void
		No arguments.

	Method #2 (06000002)
	-------------------------------------------------------
		MethodName: .ctor (06000002)
		Flags     : [Public] [HideBySig] [ReuseSlot] [SpecialName] [RTSpecialName] [.ctor]  (00001886)
		RVA       : 0x0000205e
		ImplFlags : [IL] [Managed]  (00000000)
		CallCnvntn: [DEFAULT]
		hasThis
		ReturnType: Void
		No arguments.


TypeDef #2 (02000003)
-------------------------------------------------------
	TypDefName: C2  (02000003)
	Flags     : [NestedFamORAssem] [AutoLayout] [Class] [AnsiClass] [BeforeFieldInit]  (00100007)
	Extends   : 01000005 [TypeRef] System.Object
	EnclosingClass : C (02000002)
	Method #1 (06000003)
	-------------------------------------------------------
		MethodName: M2 (06000003)
		Flags     : [HideBySig] [ReuseSlot]  (00000087)
		RVA       : 0x0000206f
		ImplFlags : [IL] [Managed]  (00000000)
		CallCnvntn: [DEFAULT]
		hasThis
		ReturnType: Void
		No arguments.

	Method #2 (06000004)
	-------------------------------------------------------
		MethodName: .ctor (06000004)
		Flags     : [Public] [HideBySig] [ReuseSlot] [SpecialName] [RTSpecialName] [.ctor]  (00001886)
		RVA       : 0x0000207d
		ImplFlags : [IL] [Managed]  (00000000)
		CallCnvntn: [DEFAULT]
		hasThis
		ReturnType: Void
		No arguments.


TypeRef #1 (01000001)
-------------------------------------------------------
Token:             0x01000001
ResolutionScope:   0x23000001
TypeRefName:       System.Runtime.CompilerServices.CompilationRelaxationsAttribute
	MemberRef #1 (0a000001)
	-------------------------------------------------------
		Member: (0a000001) .ctor:
		CallCnvntn: [DEFAULT]
		hasThis
		ReturnType: Void
		1 Arguments
			Argument #1:  I4

TypeRef #2 (01000002)
-------------------------------------------------------
Token:             0x01000002
ResolutionScope:   0x23000001
TypeRefName:       System.Runtime.CompilerServices.RuntimeCompatibilityAttribute
	MemberRef #1 (0a000002)
	-------------------------------------------------------
		Member: (0a000002) .ctor:
		CallCnvntn: [DEFAULT]
		hasThis
		ReturnType: Void
		No arguments.

TypeRef #3 (01000003)
-------------------------------------------------------
Token:             0x01000003
ResolutionScope:   0x23000001
TypeRefName:       System.Diagnostics.DebuggableAttribute
	MemberRef #1 (0a000003)
	-------------------------------------------------------
		Member: (0a000003) .ctor:
		CallCnvntn: [DEFAULT]
		hasThis
		ReturnType: Void
		1 Arguments
			Argument #1:  ValueClass DebuggingModes

TypeRef #4 (01000004)
-------------------------------------------------------
Token:             0x01000004
ResolutionScope:   0x01000003
TypeRefName:       DebuggingModes

TypeRef #5 (01000005)
-------------------------------------------------------
Token:             0x01000005
ResolutionScope:   0x23000001
TypeRefName:       System.Object
	MemberRef #1 (0a000005)
	-------------------------------------------------------
		Member: (0a000005) .ctor:
		CallCnvntn: [DEFAULT]
		hasThis
		ReturnType: Void
		No arguments.

TypeRef #6 (01000006)
-------------------------------------------------------
Token:             0x01000006
ResolutionScope:   0x23000001
TypeRefName:       System.Console
	MemberRef #1 (0a000004)
	-------------------------------------------------------
		Member: (0a000004) WriteLine:
		CallCnvntn: [DEFAULT]
		ReturnType: Void
		1 Arguments
			Argument #1:  String

Assembly
-------------------------------------------------------
	Token: 0x20000001
	Name : public-and-private
	Public Key    :
	Hash Algorithm : 0x00008004
	Version: 0.0.0.0
	Major Version: 0x00000000
	Minor Version: 0x00000000
	Build Number: 0x00000000
	Revision Number: 0x00000000
	Locale: <null>
	Flags : [none] (00000000)
	CustomAttribute #1 (0c000001)
	-------------------------------------------------------
		CustomAttribute Type: 0a000001
		CustomAttributeName: System.Runtime.CompilerServices.CompilationRelaxationsAttribute :: instance void .ctor(int32)
		Length: 8
		Value : 01 00 08 00 00 00 00 00                          >                <
		ctor args: (8)

	CustomAttribute #2 (0c000002)
	-------------------------------------------------------
		CustomAttribute Type: 0a000002
		CustomAttributeName: System.Runtime.CompilerServices.RuntimeCompatibilityAttribute :: instance void .ctor()
		Length: 30
		Value : 01 00 01 00 54 02 16 57  72 61 70 4e 6f 6e 45 78 >    T  WrapNonEx<
                      : 63 65 70 74 69 6f 6e 54  68 72 6f 77 73 01       >ceptionThrows   <
		ctor args: ()

	CustomAttribute #3 (0c000003)
	-------------------------------------------------------
		CustomAttribute Type: 0a000003
		CustomAttributeName: System.Diagnostics.DebuggableAttribute :: instance void .ctor(value class DebuggingModes)
		Length: 8
		Value : 01 00 07 01 00 00 00 00                          >                <
		ctor args: ( <can not decode> )


AssemblyRef #1 (23000001)
-------------------------------------------------------
	Token: 0x23000001
	Public Key or Token: b7 7a 5c 56 19 34 e0 89
	Name: mscorlib
	Version: 4.0.0.0
	Major Version: 0x00000004
	Minor Version: 0x00000000
	Build Number: 0x00000000
	Revision Number: 0x00000000
	Locale: <null>
	HashValue Blob:
	Flags: [none] (00000000)


User Strings
-------------------------------------------------------
70000001 : ( 7) L""I am C!""
70000011 : ( 8) L""I am C2!""


Coff symbol name overhead:  0
===========================================================
===========================================================
===========================================================

";
