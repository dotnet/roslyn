
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly ModoptTests
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module ModoptTests.dll
// MVID: {0CF92859-F4B6-4C06-BD1D-5233137FE44F}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00240000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Metadata.LeastModoptsWinAmbiguous
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance uint8  M(uint8 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t,
                            uint8 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) v) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  1
    .locals init (uint8 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.s   22
    IL_0003:  stloc.0
    IL_0004:  br.s       IL_0006

    IL_0006:  ldloc.0
    IL_0007:  ret
  } // end of method LeastModoptsWinAmbiguous::M

  .method public hidebysig newslot virtual 
          instance uint8 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) M(uint8 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t,
                            uint8 v) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  1
    .locals init (uint8 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.s   33
    IL_0003:  stloc.0
    IL_0004:  br.s       IL_0006

    IL_0006:  ldloc.0
    IL_0007:  ret
  } // end of method LeastModoptsWinAmbiguous::M

  .method public hidebysig newslot virtual 
          instance uint8 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) GetByte() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (uint8 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.6
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method LeastModoptsWinAmbiguous::GetByte

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method LeastModoptsWinAmbiguous::.ctor

} // end of class Metadata.LeastModoptsWinAmbiguous

.class public auto ansi beforefieldinit Metadata.LeastModoptsWin
       extends Metadata.LeastModoptsWinAmbiguous
{
  .method public hidebysig newslot virtual 
          instance uint8 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsLong)
 			  M(uint8 t,
                            uint8 v) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  1
    .locals init (uint8 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.s   11
    IL_0003:  stloc.0
    IL_0004:  br.s       IL_0006

    IL_0006:  ldloc.0
    IL_0007:  ret
  } // end of method LeastModoptsWin::M

  .method public hidebysig newslot virtual 
          instance uint8 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
                          M(uint8 t,
                            uint8 v) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  1
    .locals init (uint8 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.s   51
    IL_0003:  stloc.0
    IL_0004:  br.s       IL_0006

    IL_0006:  ldloc.0
    IL_0007:  ret
  } // end of method LeastModoptsWin::M

  .method public hidebysig newslot virtual 
          instance uint8 modreq([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) M(uint8 t,
                             uint8 v) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  1
    .locals init (uint8 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.s   44
    IL_0003:  stloc.0
    IL_0004:  br.s       IL_0006

    IL_0006:  ldloc.0
    IL_0007:  ret
  } // end of method LeastModoptsWin::M

   .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Metadata.LeastModoptsWinAmbiguous::.ctor()
    IL_0006:  ret
  } // end of method LeastModoptsWin::.ctor

} // end of class Metadata.LeastModoptsWin

.class public auto ansi beforefieldinit Metadata.ModoptPropAmbiguous
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual 
          instance string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "2 modopts"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method ModoptPropAmbiguous::get_P

  .method public hidebysig newslot specialname virtual 
          instance string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) get_P() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "1 modopt"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method ModoptPropAmbiguous::get_P

  .method public hidebysig newslot specialname virtual 
          instance string modreq([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) get_P() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "1 modreq"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method ModoptPropAmbiguous::get_P

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ModoptPropAmbiguous::.ctor

  .property instance string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P()
  {
    .get instance string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Metadata.ModoptPropAmbiguous::get_P()
  } // end of property ModoptPropAmbiguous::P

  .property instance string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) P()
  {
    .get instance string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) Metadata.ModoptPropAmbiguous::get_P()
  } // end of property ModoptPropAmbiguous::P

  .property instance string modreq([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) P()
  {
    .get instance string modreq([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) Metadata.ModoptPropAmbiguous::get_P()
  } // end of property ModoptPropAmbiguous::P

} // end of class Metadata.ModoptPropAmbiguous

.class interface public abstract auto ansi Metadata.IFooAmbiguous`2<T,R>
{
  .method public hidebysig newslot abstract virtual 
          instance !R  M(!T modreq([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) t) cil managed
  {
  } // end of method IFooAmbiguous`2::M

  .method public hidebysig newslot abstract virtual 
          instance !R modopt([mscorlib]System.Runtime.CompilerServices.IsConst) M(!T t) cil managed
  {
  } // end of method IFooAmbiguous`2::M

  .method public hidebysig newslot abstract virtual 
          instance !R  M(!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t) cil managed
  {
  } // end of method IFooAmbiguous`2::M

} // end of class Metadata.IFooAmbiguous`2

.class interface public abstract auto ansi Metadata.IFoo
{
  .method public hidebysig newslot abstract virtual 
          instance string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) M<T>(!!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t) cil managed
  {
  } // end of method IFoo::M

  .method public hidebysig newslot abstract virtual 
          instance string modopt([mscorlib]System.Runtime.CompilerServices.IsConst) M<T>(!!T t) cil managed
  {
  } // end of method IFoo::M

} // end of class Metadata.IFoo

.class public auto ansi beforefieldinit Metadata.Modreq
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void modreq([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) M(uint32 x) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       void [mscorlib]System.Console::Write(uint32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method Modreq::M

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Modreq::.ctor

} // end of class Metadata.Modreq

// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ModoptTests.res
