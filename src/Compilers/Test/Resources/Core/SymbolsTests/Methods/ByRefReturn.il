
// ilasm /DLL ByRefReturn.il

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly ByRefReturn
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module ByRefReturn.dll
// MVID: {02416594-C334-48EC-8716-DBB37259A68B}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x019F0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit ByRefReturn
       extends [mscorlib]System.Object
{
  .field private static int32 f
  .method public hidebysig instance int32& 
          M() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldsflda     int32 ByRefReturn::f
    IL_000a:  ret
  } // end of method ByRefReturn::M

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ByRefReturn::.ctor

} // end of class ByRefReturn


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ByRefReturn.res
