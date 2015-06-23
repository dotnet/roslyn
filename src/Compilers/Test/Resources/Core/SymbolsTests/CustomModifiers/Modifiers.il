
// ilasm /DLL Modifiers.il ModifiersAssembly.il /OUTPUT=Modifiers.DLL
// ilasm /DLL Modifiers.il /OUTPUT=Modifiers.netmodule

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
//.module Modifiers.dll
// MVID: {55A9BA26-3D42-4924-81E7-FE40D4EE92B0}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x003E0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi sealed beforefieldinit Modifiers
       extends [mscorlib]System.Object
{
  .field public static int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) F0

  .method public hidebysig static void  F1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) p) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "F1"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Modifiers::F1

  .method public hidebysig static void  F2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsConst) p) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "F2"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Modifiers::F2

  .method public hidebysig static void  F3(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Int32) p) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "F3"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Modifiers::F2

  .method public hidebysig static void  F4(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modreq([mscorlib]System.Runtime.CompilerServices.IsConst) p) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "F4"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Modifiers::F2

  .method public hidebysig static void modopt([mscorlib]System.Runtime.CompilerServices.IsConst) F5(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] p) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "F5"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Modifiers::F2

  .method public hidebysig static void  F6(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) * p) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "F6"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Modifiers::F2

  .method public hidebysig static string modopt([mscorlib]System.Runtime.CompilerServices.IsConst)  F7() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "F7"
    IL_000c:  ret
  } // end of method Modifiers::F7


  .method public hidebysig static void modopt([mscorlib]System.Runtime.CompilerServices.IsConst) F8() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "F8"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Modifiers::F2

  .method public hidebysig static int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] F9() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "F9"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    ldnull
    ret
  } // end of method Modifiers::F2

  .method public hidebysig static int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) * F10() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    .locals init (int32* V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "F10"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    ldloc.0
    ret
  } // end of method Modifiers::F2

} // end of class Modifiers

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) p) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Base.M1"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Base::M1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base

.class interface public abstract auto ansi I1
{
  .method public hidebysig newslot abstract virtual 
          instance void  M1(int32 p) cil managed
  {
  } // end of method I1::M1

} // end of class I1

.class interface public abstract auto ansi I2
{
  .method public hidebysig newslot abstract virtual 
          instance void  M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsConst) p) cil managed
  {
  } // end of method I2::M1

} // end of class I2

.class interface public abstract auto ansi I3
{
  .method public hidebysig newslot abstract virtual 
          instance void  M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) p) cil managed
  {
  } // end of method I3::M1

} // end of class I3


.class public abstract auto ansi beforefieldinit C3`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance void  M3(!T x) cil managed
  {
  } // end of method C3`1::M3

  .method public hidebysig static !T M4() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    .locals init (!T V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "M4"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    ldloc.0
    ret
  } // end of method Modifiers::F2

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C3`1::.ctor

} // end of class C3`1

.class public abstract auto ansi beforefieldinit C4
       extends class C3`1<string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []>
{
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class C3`1<string[]>::.ctor()
    IL_0006:  ret
  } // end of method C4::.ctor

} // end of class C4


.class private auto ansi beforefieldinit C5
       extends C4
{
  .method public hidebysig virtual instance void 
          M3(string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "C5.M3"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method C5::M3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void C4::.ctor()
    IL_0006:  ret
  } // end of method C5::.ctor

} // end of class C5

.class public auto ansi beforefieldinit Properties
       extends [mscorlib]System.Object
{
  .field private int32 '<Property>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor()
           = {}
  .method public hidebysig specialname instance int32 
          get_Property() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor()
             = {}
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 Properties::'<Property>k__BackingField'
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Properties::get_Property

  .method public hidebysig specialname instance void 
          set_Property(int32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor()
             = {}
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      int32 Properties::'<Property>k__BackingField'
    IL_0007:  ret
  } // end of method Properties::set_Property

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Properties::.ctor

  //11
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Property()
  {
    .get instance int32 Properties::get_Property()
    .set instance void Properties::set_Property(int32)
  } // end of property Properties::Property

  //10
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] Property()
  {
    .get instance int32 Properties::get_Property()
    .set instance void Properties::set_Property(int32)
  } // end of property Properties::Property

  //01
  .property instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Property()
  {
    .get instance int32 Properties::get_Property()
    .set instance void Properties::set_Property(int32)
  } // end of property Properties::Property

  //00
  .property instance int32[] Property()
  {
    .get instance int32 Properties::get_Property()
    .set instance void Properties::set_Property(int32)
  } // end of property Properties::Property
} // end of class Properties

.class public auto ansi beforefieldinit Comparisons
       extends [mscorlib]System.Object
{
  //1111
  //custom modifiers: in return type, on return type, in parameter type, on parameter type
  .method public hidebysig instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //1110
  //custom modifiers: in return type, on return type, in parameter type
  .method public hidebysig instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //1101
  //custom modifiers: in return type, on return type, on parameter type
  .method public hidebysig instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //1100
  //custom modifiers: in return type, on return type, in parameter type, on parameter type
  .method public hidebysig instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method(int32[] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //1011
  //custom modifiers: in return type, in parameter type, on parameter type
  .method public hidebysig instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []
          Method(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //1010
  //custom modifiers: in return type, in parameter type
  .method public hidebysig instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []
          Method(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //1001
  //custom modifiers: in return type, on parameter type
  .method public hidebysig instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []
          Method(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //1000
  //custom modifiers: in return type
  .method public hidebysig instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []
          Method(int32[] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //0111
  //custom modifiers: on return type, in parameter type, on parameter type
  .method public hidebysig instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //0110
  //custom modifiers: on return type, in parameter type
  .method public hidebysig instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //0101
  //custom modifiers: on return type, on parameter type
  .method public hidebysig instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //0100
  //custom modifiers: on return type
  .method public hidebysig instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method(int32[] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //0011
  //custom modifiers: in parameter type, on parameter type
  .method public hidebysig instance int32[]
          Method(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //0010
  //custom modifiers: in parameter type
  .method public hidebysig instance int32[]
          Method(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //0001
  //custom modifiers: on parameter type
  .method public hidebysig instance int32[]
          Method(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method
  
  //0000
  //custom modifiers: none
  .method public hidebysig instance int32[]
          Method(int32[] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method

  //duplicate custom modifier
  //custom modifiers: two identical on parameter type
  .method public hidebysig instance int32[]
          Method(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method

  //extra custom modifier
  //custom modifiers: two distinct on parameter type
  .method public hidebysig instance int32[]
          Method(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Int32) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method

  //custom modifier order
  //custom modifiers: two distinct, but reordered, on parameter type
  .method public hidebysig instance int32[]
          Method(int32[] modopt([mscorlib]System.Int32) modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method Comparisons::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Comparisons::.ctor

} // end of class Comparisons

.class public auto ansi beforefieldinit CustomModifierOverridingA
       extends [mscorlib]System.Object
{
  //different one extra custom modifier
  .method public hidebysig newslot virtual 
          instance void  Method1(int32 modopt([mscorlib]System.Int32) [] x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method CustomModifierOverridingA::Method1

  //exact match
  .method public hidebysig newslot virtual 
          instance void  Method2(int32[] x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method CustomModifierOverridingA::Method2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CustomModifierOverridingA::.ctor

} // end of class CustomModifierOverridingA

.class public auto ansi beforefieldinit CustomModifierOverridingB
       extends CustomModifierOverridingA
{
  //different two extra custom modifiers
  .method public hidebysig newslot virtual 
          instance void  Method1(int32 modopt([mscorlib]System.Int32) [] modopt([mscorlib]System.Int32) x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method CustomModifierOverridingB::Method1

  //different two extra custom modifiers
  .method public hidebysig newslot virtual 
          instance void  Method2(int32 modopt([mscorlib]System.Int32) [] modopt([mscorlib]System.Int32) x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method CustomModifierOverridingB::Method2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void CustomModifierOverridingA::.ctor()
    IL_0006:  ret
  } // end of method CustomModifierOverridingB::.ctor

} // end of class CustomModifierOverridingB

.class public auto ansi beforefieldinit CustomModifierOverridingC
       extends CustomModifierOverridingB
{
  //one extra custom modifier
  .method public hidebysig newslot virtual 
          instance void  Method1(int32[] modopt([mscorlib]System.Int32) x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method CustomModifierOverridingC::Method1

  //one extra custom modifier
  .method public hidebysig newslot virtual 
          instance void  Method2(int32[] modopt([mscorlib]System.Int32) x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method CustomModifierOverridingC::Method2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void CustomModifierOverridingB::.ctor()
    IL_0006:  ret
  } // end of method CustomModifierOverridingC::.ctor

} // end of class CustomModifierOverridingC

.class public auto ansi beforefieldinit CustomModifierOverridingD
       extends CustomModifierOverridingC
{
  //two extra custom modifiers
  .method public hidebysig newslot virtual 
          instance void  Method1(int32[] modopt([mscorlib]System.Int32) modopt([mscorlib]System.Int32) x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method CustomModifierOverridingD::Method1

  //two extra custom modifiers
  .method public hidebysig newslot virtual 
          instance void  Method2(int32[] modopt([mscorlib]System.Int32) modopt([mscorlib]System.Int32) x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method CustomModifierOverridingD::Method2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void CustomModifierOverridingC::.ctor()
    IL_0006:  ret
  } // end of method CustomModifierOverridingD::.ctor

} // end of class CustomModifierOverridingD

.class public auto ansi beforefieldinit MethodCustomModifierCombinations
       extends [mscorlib]System.Object
{
  //custom modifiers: in return type, on return type, in parameter type, on parameter type
  .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method1111(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method1111
  
  //custom modifiers: in return type, on return type, in parameter type
  .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method1110(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method1110
  
  //custom modifiers: in return type, on return type, on parameter type
  .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method1101(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method1101
  
  //custom modifiers: in return type, on return type, in parameter type, on parameter type
  .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method1100(int32[] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method1100
  
  //custom modifiers: in return type, in parameter type, on parameter type
  .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []
          Method1011(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method1011
  
  //custom modifiers: in return type, in parameter type
  .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []
          Method1010(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method1010
  
  //custom modifiers: in return type, on parameter type
  .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []
          Method1001(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method1001
  
  //custom modifiers: in return type
  .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []
          Method1000(int32[] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method1000
  
  //custom modifiers: on return type, in parameter type, on parameter type
  .method public hidebysig newslot virtual instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method0111(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method0111
  
  //custom modifiers: on return type, in parameter type
  .method public hidebysig newslot virtual instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method0110(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method0110
  
  //custom modifiers: on return type, on parameter type
  .method public hidebysig newslot virtual instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method0101(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method0101
  
  //custom modifiers: on return type
  .method public hidebysig newslot virtual instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          Method0100(int32[] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method0100
  
  //custom modifiers: in parameter type, on parameter type
  .method public hidebysig newslot virtual instance int32[]
          Method0011(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method0011
  
  //custom modifiers: in parameter type
  .method public hidebysig newslot virtual instance int32[]
          Method0010(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method0010
  
  //custom modifiers: on parameter type
  .method public hidebysig newslot virtual instance int32[]
          Method0001(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method0001
  
  //custom modifiers: none
  .method public hidebysig newslot virtual instance int32[]
          Method0000(int32[] a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method MethodCustomModifierCombinations::Method0000

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MethodCustomModifierCombinations::.ctor

} // end of class MethodCustomModifierCombinations

.class public auto ansi beforefieldinit PropertyCustomModifierCombinations
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor()
           = {}

  .method public hidebysig newslot specialname virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          get_Property11() cil managed
  {
    // Code size       3 (0x3)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  throw
  } // end of method PropertyCustomModifierCombinations::get_Property11

  .method public hidebysig newslot specialname virtual instance void 
          set_Property11(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 'value') cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method PropertyCustomModifierCombinations::set_Property11

  .method public hidebysig newslot specialname virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []
          get_Property10() cil managed
  {
    // Code size       3 (0x3)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  throw
  } // end of method PropertyCustomModifierCombinations::get_Property10

  .method public hidebysig newslot specialname virtual instance void 
          set_Property10(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] 'value') cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method PropertyCustomModifierCombinations::set_Property10

  .method public hidebysig newslot specialname virtual instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
          get_Property01() cil managed
  {
    // Code size       3 (0x3)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  throw
  } // end of method PropertyCustomModifierCombinations::get_Property01

  .method public hidebysig newslot specialname virtual instance void 
          set_Property01(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 'value') cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method PropertyCustomModifierCombinations::set_Property01

  .method public hidebysig newslot specialname virtual instance int32[]
          get_Property00() cil managed
  {
    // Code size       3 (0x3)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  throw
  } // end of method PropertyCustomModifierCombinations::get_Property00

  .method public hidebysig newslot specialname virtual instance void 
          set_Property00(int32[] 'value') cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method PropertyCustomModifierCombinations::set_Property00

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method PropertyCustomModifierCombinations::.ctor

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Property11()
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) PropertyCustomModifierCombinations::get_Property11()
    .set instance void PropertyCustomModifierCombinations::set_Property11(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
  } // end of property PropertyCustomModifierCombinations::Property11

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] Property10()
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] PropertyCustomModifierCombinations::get_Property10()
    .set instance void PropertyCustomModifierCombinations::set_Property10(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [])
  } // end of property PropertyCustomModifierCombinations::Property10

  .property instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Property01()
  {
    .get instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) PropertyCustomModifierCombinations::get_Property01()
    .set instance void PropertyCustomModifierCombinations::set_Property01(int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
  } // end of property PropertyCustomModifierCombinations::Property01

  .property instance int32[] Property00()
  {
    .get instance int32[] PropertyCustomModifierCombinations::get_Property00()
    .set instance void PropertyCustomModifierCombinations::set_Property00(int32[])
  } // end of property PropertyCustomModifierCombinations::Property00
} // end of class PropertyCustomModifierCombinations

.class public auto ansi beforefieldinit EventCustomModifierCombinations
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          accessor11(class [mscorlib]System.Action`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []> modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          accessor10(class [mscorlib]System.Action`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          accessor01(class [mscorlib]System.Action`1<int32[]> modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          accessor00(class [mscorlib]System.Action`1<int32[]> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  //11
  .event class [mscorlib]System.Action`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []> modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Event11
  {
    .addon instance void EventCustomModifierCombinations::accessor11(class [mscorlib]System.Action`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []> modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
    .removeon instance void EventCustomModifierCombinations::accessor11(class [mscorlib]System.Action`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []> modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
  }

  //10
  .event class [mscorlib]System.Action`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []> Event10
  {
    .addon instance void EventCustomModifierCombinations::accessor10(class [mscorlib]System.Action`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []>)
    .removeon instance void EventCustomModifierCombinations::accessor10(class [mscorlib]System.Action`1<int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) []>)
  }

  //01
  .event class [mscorlib]System.Action`1<int32[]> modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Event01
  {
    .addon instance void EventCustomModifierCombinations::accessor01(class [mscorlib]System.Action`1<int32[]> modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
    .removeon instance void EventCustomModifierCombinations::accessor01(class [mscorlib]System.Action`1<int32[]> modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
  }

  //00
  .event class [mscorlib]System.Action`1<int32[]> Event00
  {
    .addon instance void EventCustomModifierCombinations::accessor00(class [mscorlib]System.Action`1<int32[]>)
    .removeon instance void EventCustomModifierCombinations::accessor00(class [mscorlib]System.Action`1<int32[]>)
  }
} // end of class EventCustomModifierCombinations

.class public auto ansi beforefieldinit FieldCustomModifierCombinations
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor()
           = {}

  .field public int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 'field11'
  .field public int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] 'field10'
  .field public int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 'field01'
  .field public int32[] 'field00'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method FieldCustomModifierCombinations::.ctor

} // end of class FieldCustomModifierCombinations

.class public auto ansi beforefieldinit Outer`1<TOuter>
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Inner`1<TOuter,TInner>
         extends [mscorlib]System.Object
  {
    .method public hidebysig newslot virtual 
            instance void  Method<TMethod>(!TOuter modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x,
                                           !TInner modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) y,
                                           !!TMethod modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) z) cil managed
    {
      // Code size       46 (0x2e)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldstr      "Outer.Inner({0}, {1}, {2})"
      IL_0006:  ldarg.1
      IL_0007:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_000c:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
      IL_0011:  ldarg.2
      IL_0012:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0017:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
      IL_001c:  ldarg.3
      IL_001d:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0022:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
      IL_0027:  call       void [mscorlib]System.Console::WriteLine(string,
                                                                    object,
                                                                    object,
                                                                    object)
      IL_002c:  nop
      IL_002d:  ret
    } // end of method Inner`1::Method

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Inner`1::.ctor

  } // end of class Inner`1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Outer`1::.ctor

} // end of class Outer`1

// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Modifiers.res
