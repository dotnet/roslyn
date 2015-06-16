
// ilasm /DLL ILMethods.il

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly ILMethods
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module ILMethods.dll
// MVID: {02416594-C334-48EC-8716-DBB37259A68B}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x019F0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi beforefieldinit MetadataModifiers
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          M00() cil managed
  {
    ret
  }

  // ERROR: newslot without virtual
  .method public hidebysig newslot instance void 
          M01() cil managed
  {
    ret
  }
  
  // ERROR: abstract without virtual
  .method public hidebysig abstract instance void 
          M02() cil managed
  {
  }

  .method public hidebysig virtual instance void 
          M03() cil managed
  {
    ret
  }
  
  // ERROR: final without virtual
  .method public hidebysig final instance void 
          M04() cil managed
  {
    ret
  }
  
  // ERROR: newslot and abstract without virtual
  .method public hidebysig newslot abstract instance void 
          M05() cil managed
  {
  }

  .method public hidebysig newslot virtual instance void 
          M06() cil managed
  {
    ret
  }
  
  // ERROR: newslot and final without virtual
  .method public hidebysig newslot final instance void 
          M07() cil managed
  {
    ret
  }

  .method public hidebysig abstract virtual instance void 
          M08() cil managed
  {
  }
  
  // ERROR: abstract and final without virtual, abstract and final
  .method public hidebysig abstract final instance void 
          M09() cil managed
  {
  }

  .method public hidebysig virtual final instance void 
          M10() cil managed
  {
    ret
  }

  .method public hidebysig newslot abstract virtual instance void 
          M11() cil managed
  {
  }
  
  // ERROR: newslot abstract and final without virtual, abstract and final
  .method public hidebysig newslot abstract final instance void 
          M12() cil managed
  {
  }

  .method public hidebysig newslot virtual final instance void 
          M13() cil managed
  {
    ret
  }
  
  // ERROR: abstract and final
  .method public hidebysig abstract virtual final instance void 
          M14() cil managed
  {
  }
  
  // ERROR: abstract and final
  .method public hidebysig newslot abstract virtual final instance void 
          M15() cil managed
  {
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class MetadataModifiers


.class public auto ansi beforefieldinit BaseVirtual
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  M() cil managed
  {
    ldstr      "BaseVirtual"
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class BaseVirtual

.class public auto ansi beforefieldinit DerivedNonVirtual
       extends BaseVirtual
{
  .method public hidebysig instance void 
          M() cil managed
  {
    ldstr      "DerivedNonVirtual"
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void BaseVirtual::.ctor()
    ret
  }

} // end of class DerivedNonVirtual

.class public auto ansi beforefieldinit Derived2Override
       extends DerivedNonVirtual
{
  .method public hidebysig virtual instance void 
          M() cil managed
  {
    ldstr      "Derived2Override"
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void DerivedNonVirtual::.ctor()
    ret
  }

} // end of class Derived2Override