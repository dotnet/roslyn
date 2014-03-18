// ilasm /dll Indexers.il /out=Indexers.dll

.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly Properties {}

.class interface public abstract auto ansi IRefIndexer
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Item(int32& x) cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Item(int32& x, int32 'value') cil managed
  {
  }

  .property instance int32 Item(int32&)
  {
    .get instance int32 IRefIndexer::get_Item(int32&)
    .set instance void IRefIndexer::set_Item(int32&, int32)
  }
}

.class public auto ansi beforefieldinit RefIndexer
       extends [mscorlib]System.Object
       implements IRefIndexer
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}
  .method public hidebysig newslot specialname virtual 
          instance int32  get_Item(int32& x) cil managed
  {
    ldc.i4.2
	dup
    starg.s    x
	ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  set_Item(int32& x, int32 'value') cil managed
  {
    ldarg.2
    starg.s    x
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Item(int32&)
  {
    .get instance int32 RefIndexer::get_Item(int32&)
    .set instance void RefIndexer::set_Item(int32&, int32)
  }
}