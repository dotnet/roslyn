
// ilasm /DLL /NOAUTOINHERIT FakeMsCorLib.il


// Metadata version: v4.0.30319

.assembly FakeMsCorLib
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module FakeMsCorLib.dll
// MVID: {A85F022E-D90D-4D02-AB90-7DD527AF6E43}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00790000


.class public auto ansi serializable beforefieldinit System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Object::.ctor

  .method public hidebysig newslot virtual 
          instance string  ToString() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldnull
    IL_000b:  ret
  } // end of method Object::ToString

  .method public hidebysig newslot virtual 
          instance bool  Equals(object obj) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0007:  ret
  } // end of method Object::Equals

  .method public hidebysig static bool  Equals(object objA,
                                               object objB) cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    IL_000c:  ldc.i4.0
    IL_000d:  ret
  } // end of method Object::Equals

  .method public hidebysig static bool  ReferenceEquals(object objA,
                                                        object objB) cil managed
  {
    // Code size       5 (0x5)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ceq
    IL_0004:  ret
  } // end of method Object::ReferenceEquals

  .method public hidebysig newslot virtual 
          instance int32  GetHashCode() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0006:  ret
  } // end of method Object::GetHashCode


  .method family hidebysig newslot virtual 
          instance void  Finalize() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Object::Finalize

} // end of class System.Object
