// build using:
// ilasm DelegatesWithoutInvoke.il /output=DelegatesWithoutInvoke.dll /dll

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly DelegatesWithoutInvoke
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module DelegatesWithoutInvoke.dll
// MVID: {23AC8B14-AEA4-4D44-A317-2721A45F95B7}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0002       // WINDOWS_GUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x001C0000

.class public auto ansi DelegateWithoutInvoke
       extends [mscorlib]System.Object
{
  .class auto ansi sealed nested public DelegateSubWithoutInvoke
         extends [mscorlib]System.MulticastDelegate
  {
    .method public specialname rtspecialname 
            instance void  .ctor(object TargetObject,
                                 native int TargetMethod) runtime managed
    {
    } // end of method DelegateSubWithoutInvoke::.ctor

    .method public newslot strict virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(string p,
                        class [mscorlib]System.AsyncCallback DelegateCallback,
                        object DelegateAsyncState) runtime managed
    {
    } // end of method DelegateSubWithoutInvoke::BeginInvoke

    .method public newslot strict virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
    {
    } // end of method DelegateSubWithoutInvoke::EndInvoke

//    .method public newslot strict virtual 
//            instance void  Invoke(string p) runtime managed
//    {
//    }  end of method DelegateSubWithoutInvoke::Invoke

  } // end of class DelegateSubWithoutInvoke

  .class auto ansi sealed nested public DelegateFunctionWithoutInvoke
         extends [mscorlib]System.MulticastDelegate
  {
    .method public specialname rtspecialname 
            instance void  .ctor(object TargetObject,
                                 native int TargetMethod) runtime managed
    {
    } // end of method DelegateFunctionWithoutInvoke::.ctor

    .method public newslot strict virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(string p,
                        class [mscorlib]System.AsyncCallback DelegateCallback,
                        object DelegateAsyncState) runtime managed
    {
    } // end of method DelegateFunctionWithoutInvoke::BeginInvoke

    .method public newslot strict virtual 
            instance string  EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
    {
    } // end of method DelegateFunctionWithoutInvoke::EndInvoke

//    .method public newslot strict virtual 
//            instance string  Invoke(string p) runtime managed
//    {
//    } 

  } // end of class DelegateFunctionWithoutInvoke

  .class auto ansi sealed nested public DelegateGenericFunctionWithoutInvoke`1<T>
         extends [mscorlib]System.MulticastDelegate
  {
    .method public specialname rtspecialname 
            instance void  .ctor(object TargetObject,
                                 native int TargetMethod) runtime managed
    {
    } // end of method DelegateGenericFunctionWithoutInvoke`1::.ctor

    .method public newslot strict virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(!T p,
                        class [mscorlib]System.AsyncCallback DelegateCallback,
                        object DelegateAsyncState) runtime managed
    {
    } // end of method DelegateGenericFunctionWithoutInvoke`1::BeginInvoke

    .method public newslot strict virtual 
            instance !T  EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
    {
    } // end of method DelegateGenericFunctionWithoutInvoke`1::EndInvoke

  } // end of class DelegateGenericFunctionWithoutInvoke`1

  .field public class DelegateWithoutInvoke/DelegateSubWithoutInvoke SubDel
  .field public class DelegateWithoutInvoke/DelegateFunctionWithoutInvoke FuncDel
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       43 (0x2b)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldnull
    IL_0008:  ldftn      void DelegateWithoutInvoke::DelSubImpl(string)
    IL_000e:  newobj     instance void DelegateWithoutInvoke/DelegateSubWithoutInvoke::.ctor(object,
                                                                                             native int)
    IL_0013:  stfld      class DelegateWithoutInvoke/DelegateSubWithoutInvoke DelegateWithoutInvoke::SubDel
    IL_0018:  ldarg.0
    IL_0019:  ldnull
    IL_001a:  ldftn      string DelegateWithoutInvoke::DelFuncImpl(string)
    IL_0020:  newobj     instance void DelegateWithoutInvoke/DelegateFunctionWithoutInvoke::.ctor(object,
                                                                                                  native int)
    IL_0025:  stfld      class DelegateWithoutInvoke/DelegateFunctionWithoutInvoke DelegateWithoutInvoke::FuncDel
    IL_002a:  ret
  } // end of method DelegateWithoutInvoke::.ctor

  .method public static void  DelSubImpl(string p) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  ldstr      "DelegateWithoutInvoke.DelSubImpl called "
    IL_0005:  ldarg.0
    IL_0006:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000b:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0010:  ret
  } // end of method DelegateWithoutInvoke::DelSubImpl

  .method public static string  DelFuncImpl(string p) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  ldstr      "DelegateWithoutInvoke.DelFuncImpl called "
    IL_0005:  ldarg.0
    IL_0006:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method DelegateWithoutInvoke::DelFuncImpl

} // end of class DelegateWithoutInvoke


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file test.res
