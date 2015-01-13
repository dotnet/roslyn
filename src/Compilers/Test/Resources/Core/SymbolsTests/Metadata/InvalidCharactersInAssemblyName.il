.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 4:0:0:0
}

.assembly 'xx 	\r\n  !"#$%&\'()*+,-./0123456789:;<=>? '
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}

.module 'a,b.dll'
.corflags 0x00000001    


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit lib.Class1
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Class1::.ctor

} // end of class lib.Class1
