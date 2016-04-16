.assembly extern windowsruntime ITest {}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 255:255:255:255
}

.assembly windowsruntime WImpl
{
  .hash algorithm 0x00008004
  .ver 255:255:255:255
}

.module WImpl.winmd

.class public auto ansi windowsruntime sealed WImpl
       extends System.Object
       implements [ITest]ITest
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() runtime managed
    {
    }
}