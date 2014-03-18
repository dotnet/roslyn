// ilasm /dll IndexerWithByRefParam.il

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly IndexerWithByRefParam
{
}

.class interface public abstract auto ansi import B
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 30 39 35 45 2D 30 30 30 30 
                                                                                                  2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30 
                                                                                                  30 30 30 30 30 34 36 00 00 )                    
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )
                         
  .method public hidebysig newslot specialname abstract virtual instance int32  get_Item(int32& a) cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void  set_Item(int32& a, int32 'value') cil managed
  {
  }

  .property instance int32 Item(int32&)
  {
    .get instance int32 B::get_Item(int32&)
    .set instance void B::set_Item(int32&, int32)
  } 
}

