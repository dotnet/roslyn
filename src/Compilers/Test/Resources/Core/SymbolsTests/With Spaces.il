// ilasm /DLL "With Spaces.il" "With Spaces Assembly.il" /OUTPUT="With Spaces.dll"
// ilasm /DLL "With Spaces.il" /OUTPUT="With Spaces.netmodule"

// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

// MVID: {786E848A-390B-4A5E-8D1C-2F736DF718E4}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00650000

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class C