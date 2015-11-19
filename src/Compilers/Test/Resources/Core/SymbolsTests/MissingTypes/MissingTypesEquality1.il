
// ilasm /DLL MissingTypesEquality1.il


//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly extern MDTestLib1
{
  .ver 0:0:0:0
}

.assembly extern MDTestLib2
{
  .ver 0:0:0:0
}

.assembly extern MDTestLib3
{
  .ver 0:0:0:0
}

.assembly extern MDTestLib4
{
  .ver 0:0:0:0
}

.assembly MissingTypesEquality1
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.module MissingTypesEquality1.dll

// MVID: {E8168B7E-08FF-41C9-9993-6AD3D46B903C}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0002       // WINDOWS_GUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00450000


// =============== CLASS MEMBERS DECLARATION ===================

.class private abstract auto ansi C
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor

  .method public newslot abstract strict virtual 
          instance class [MDTestLib1]MissingType1 
          M1() cil managed
  {
  }

  .method public newslot abstract strict virtual 
          instance class [MDTestLib1]MissingType2 
          M2() cil managed
  {
  }

  .method public newslot abstract strict virtual 
          instance class [MDTestLib2]MissingType1 
          M3() cil managed
  {
  }

  .method public newslot abstract strict virtual 
          instance class [MDTestLib3]MissingType1 
          M4() cil managed
  {
  }

  .method public newslot abstract strict virtual 
          instance class [MDTestLib3]MissingType2 
          M5() cil managed
  {
  }

  .method public newslot abstract strict virtual 
          instance class [MDTestLib4]MissingType1 
          M6() cil managed
  {
  }

  .method public newslot abstract strict virtual 
          instance class [MDTestLib1]MissingType1 
          M7() cil managed
  {
  }

  .method public newslot abstract strict virtual 
          instance class [MDTestLib3]MissingType1 
          M8() cil managed
  {
  }
} // end of class C


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file MissingTypesEquality1.res
