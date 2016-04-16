// ilasm /DLL PropertiesWithByRef.il
//
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly PropertiesWithByRef
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module PropertiesWithByRef.dll
// MVID: {34ED2253-5C3E-4C74-A712-C8B1B8EBDB44}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0002       // WINDOWS_GUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00770000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi PropertiesWithByRef
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method PropertiesWithByRef::.ctor

  .method public specialname instance int32 
          get_P1(int32& x) cil managed
  {
    .maxstack  8
    .locals init (int32 V_0)
    ldstr      "get_P1({0})"
    ldarg.1
    ldind.i4
    box        [mscorlib]System.Int32
    call       void [mscorlib]System.Console::WriteLine(string,
                                                        object)
    IL_0000:  ldarg.1
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  ldc.i4.1
    IL_0004:  add.ovf
    IL_0005:  stind.i4
    IL_0006:  ldarg.1
    IL_0007:  ldind.i4
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method PropertiesWithByRef::get_P1

  .method public specialname instance void 
          set_P1(int32& x,
                 int32 'value') cil managed
  {
    .maxstack  8
    ldstr      "set_P1({0})"
    ldarg.1
    ldind.i4
    box        [mscorlib]System.Int32
    call       void [mscorlib]System.Console::WriteLine(string,
                                                        object)
    IL_0000:  ldarg.1
    IL_0001:  ldarg.1
    IL_0002:  ldind.i4
    IL_0003:  ldc.i4.1
    IL_0004:  add.ovf
    IL_0005:  stind.i4
    IL_0006:  ret
  } // end of method PropertiesWithByRef::set_P1


  .property instance int32 P1(int32&)
  {
    .get instance int32 PropertiesWithByRef::get_P1(int32&)
    .set instance void PropertiesWithByRef::set_P1(int32&,
                                                   int32)
  } // end of property PropertiesWithByRef::P1


  .method public specialname static object 
          get_P2(object& x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] object P2)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method PropertiesWithByRef::get_P2

  .property object P2(object&)
  {
    .get object PropertiesWithByRef::get_P2(object&)
  } // end of property PropertiesWithByRef::P2
} // end of class PropertiesWithByRef


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file PropertiesWithByRef.res
