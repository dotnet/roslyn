//
// compile using: "ilasm DelegateByRefParamArray.il /dll /outpath=DelegateByRefParamArray.dll"
//

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly DelegateByRefParamArray
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module DelegateByRefParamArray.dll
// MVID: {F3FF8E7C-DF1B-4FF2-B197-D90006C23F11}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0002       // WINDOWS_GUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00480000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi DelegateByRefParamArray_Base
       extends [mscorlib]System.Object
{
  .field public int32 'field'
  .method public specialname rtspecialname 
          instance void  .ctor(int32 p) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 DelegateByRefParamArray_Base::'field'
    IL_000d:  ret
  } // end of method DelegateByRefParamArray_Base::.ctor

} // end of class DelegateByRefParamArray_Base

.class public auto ansi DelegateByRefParamArray_Derived
       extends DelegateByRefParamArray_Base
{
  .method public specialname rtspecialname 
          instance void  .ctor(int32 p) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void DelegateByRefParamArray_Base::.ctor(int32)
    IL_0007:  ret
  } // end of method DelegateByRefParamArray_Derived::.ctor

} // end of class DelegateByRefParamArray_Derived

.class public auto ansi DelegateByRefParamArray
       extends [mscorlib]System.Object
{
  .class auto ansi sealed nested public DelegateSubWithParamArrayOfReferenceTypes
         extends [mscorlib]System.MulticastDelegate
  {
    .method public specialname rtspecialname 
            instance void  .ctor(object TargetObject,
                                 native int TargetMethod) runtime managed
    {
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::.ctor

    .method public newslot strict virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(class DelegateByRefParamArray_Base[] p,
                        class [mscorlib]System.AsyncCallback DelegateCallback,
                        object DelegateAsyncState) runtime managed
    {
        .param [1]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::BeginInvoke

    .method public newslot strict virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
    {
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::EndInvoke

    .method public newslot strict virtual 
            instance void  Invoke(class DelegateByRefParamArray_Base[] p) runtime managed
    {
        .param [1]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::Invoke

  } // end of class DelegateSubWithParamArrayOfReferenceTypes

  .class auto ansi sealed nested public DelegateSubWithParamArrayOfReferenceTypesDerived
         extends [mscorlib]System.MulticastDelegate
  {
    .method public specialname rtspecialname 
            instance void  .ctor(object TargetObject,
                                 native int TargetMethod) runtime managed
    {
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::.ctor

    .method public newslot strict virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(class DelegateByRefParamArray_Derived[] p,
                        class [mscorlib]System.AsyncCallback DelegateCallback,
                        object DelegateAsyncState) runtime managed
    {
        .param [1]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::BeginInvoke

    .method public newslot strict virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
    {
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::EndInvoke

    .method public newslot strict virtual 
            instance void  Invoke(class DelegateByRefParamArray_Derived[] p) runtime managed
    {
        .param [1]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::Invoke

  } // end of class DelegateSubWithParamArrayOfReferenceTypes

  .class auto ansi sealed nested public DelegateSubWithRefParamArrayOfReferenceTypesDerived
         extends [mscorlib]System.MulticastDelegate
  {
    .method public specialname rtspecialname 
            instance void  .ctor(object TargetObject,
                                 native int TargetMethod) runtime managed
    {
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::.ctor

    .method public newslot strict virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(class DelegateByRefParamArray_Derived[]& p,
                        class [mscorlib]System.AsyncCallback DelegateCallback,
                        object DelegateAsyncState) runtime managed
    {
        .param [1]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::BeginInvoke

    .method public newslot strict virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
    {
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::EndInvoke

    .method public newslot strict virtual 
            instance void  Invoke(class DelegateByRefParamArray_Derived[]& p) runtime managed
    {
        .param [1]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithParamArrayOfReferenceTypes::Invoke

  } // end of class DelegateSubWithParamArrayOfReferenceTypes

  .class auto ansi sealed nested public DelegateSubWithParamAndParamArrayOfReferenceTypes
         extends [mscorlib]System.MulticastDelegate
  {
    .method public specialname rtspecialname 
            instance void  .ctor(object TargetObject,
                                 native int TargetMethod) runtime managed
    {
    } // end of method DelegateSubWithParamAndParamArrayOfReferenceTypes::.ctor

    .method public newslot strict virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(int32,
                        class DelegateByRefParamArray_Base[],
                        class [mscorlib]System.AsyncCallback DelegateCallback,
                        object DelegateAsyncState) runtime managed
    {
        .param [2]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithParamAndParamArrayOfReferenceTypes::BeginInvoke

    .method public newslot strict virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
    {
    } // end of method DelegateSubWithParamAndParamArrayOfReferenceTypes::EndInvoke

    .method public newslot strict virtual 
            instance void  Invoke(int32,
                                  class DelegateByRefParamArray_Base[]) runtime managed
    {
        .param [2]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithParamAndParamArrayOfReferenceTypes::Invoke

  } // end of class DelegateSubWithParamAndParamArrayOfReferenceTypes

  .class auto ansi sealed nested public DelegateSubWithByRefParamArrayOfReferenceTypes
         extends [mscorlib]System.MulticastDelegate
  {
    .method public specialname rtspecialname 
            instance void  .ctor(object TargetObject,
                                 native int TargetMethod) runtime managed
    {
    } // end of method DelegateSubWithByRefParamArrayOfReferenceTypes::.ctor

    .method public newslot strict virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(class DelegateByRefParamArray_Base[]& p,
                        class [mscorlib]System.AsyncCallback DelegateCallback,
                        object DelegateAsyncState) runtime managed
    {
        .param [1]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithByRefParamArrayOfReferenceTypes::BeginInvoke

    .method public newslot strict virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
    {
    } // end of method DelegateSubWithByRefParamArrayOfReferenceTypes::EndInvoke

    .method public newslot strict virtual 
            instance void  Invoke(class DelegateByRefParamArray_Base[]& p) runtime managed
    {
        .param [1]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  
    } // end of method DelegateSubWithByRefParamArrayOfReferenceTypes::Invoke

  } // end of class DelegateSubWithByRefParamArrayOfReferenceTypes

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method DelegateByRefParamArray::.ctor

  .method public static void  SubWithByRefParamArrayOfReferenceTypes_Widening(class DelegateByRefParamArray_Derived[]& p) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  

    // Code size       43 (0x2b)
    .maxstack  3
    .locals init (class DelegateByRefParamArray_Derived[] V_0,
             class DelegateByRefParamArray_Derived[] V_1)
    IL_0000:  ldstr      "Called SubWithByRefParamArrayOfReferenceTypes_Wide"
    + "ning."
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.2
    IL_000b:  newarr     DelegateByRefParamArray_Derived
    IL_0010:  stloc.1
    IL_0011:  ldloc.1
    IL_0012:  ldc.i4.0
    IL_0013:  ldc.i4.s   23
    IL_0015:  newobj     instance void DelegateByRefParamArray_Derived::.ctor(int32)
    IL_001a:  stelem.ref
    IL_001b:  ldloc.1
    IL_001c:  ldc.i4.1
    IL_001d:  ldc.i4.s   42
    IL_001f:  newobj     instance void DelegateByRefParamArray_Derived::.ctor(int32)
    IL_0024:  stelem.ref
    IL_0025:  ldloc.1
    IL_0026:  stloc.0
    IL_0027:  ldarg.0
    IL_0028:  ldloc.0
    IL_0029:  stind.ref
    IL_002a:  ret
  } // end of method DelegateByRefParamArray::SubWithByRefParamArrayOfReferenceTypes_Widening

  .method public static void ByRefParamArraySubOfBase(class DelegateByRefParamArray_Base[]& x) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  

    // Code size       31 (0x1f)
    .maxstack  3
    .locals init (class DelegateByRefParamArray_Base[] V_0)
    IL_0000:  ldc.i4.2
    IL_0001:  newarr     DelegateByRefParamArray_Base
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  ldc.i4.s   23
    IL_000b:  newobj     instance void DelegateByRefParamArray_Base::.ctor(int32)
    IL_0010:  stelem.ref
    IL_0011:  ldloc.0
    IL_0012:  ldc.i4.1
    IL_0013:  ldc.i4.s   42
    IL_0015:  newobj     instance void DelegateByRefParamArray_Base::.ctor(int32)
    IL_001a:  stelem.ref
    IL_001b:  ldarg.0
    IL_001c:  ldloc.0
    IL_001d:  stind.ref
    IL_001e:  ret
  } // end of method M1::ByRefArray

  .method public static void  ByRefParamAndParamArraySubOfBase(class [DelegateByRefParamArray]DelegateByRefParamArray_Base& a,
                                                             class [DelegateByRefParamArray]DelegateByRefParamArray_Base[]& b) cil managed
{
    .param [2]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  

  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (class [DelegateByRefParamArray]DelegateByRefParamArray_Derived[] V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   23
  IL_0003:  newobj     instance void [DelegateByRefParamArray]DelegateByRefParamArray_Derived::.ctor(int32)
  IL_0008:  stind.ref
  IL_0009:  ldc.i4.2
  IL_000a:  newarr     [DelegateByRefParamArray]DelegateByRefParamArray_Derived
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.0
  IL_0012:  ldc.i4.s   42
  IL_0014:  newobj     instance void [DelegateByRefParamArray]DelegateByRefParamArray_Derived::.ctor(int32)
  IL_0019:  stelem.ref
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.1
  IL_001c:  ldc.i4     0x7da
  IL_0021:  newobj     instance void [DelegateByRefParamArray]DelegateByRefParamArray_Derived::.ctor(int32)
  IL_0026:  stelem.ref
  IL_0027:  ldarg.1
  IL_0028:  ldloc.0
  IL_0029:  stind.ref
  IL_002a:  ret
} // end of method Program::ByRefParamAndParamArraySubOfBase

  .method public static void  SubWithByRefParamArrayOfReferenceTypes_Identify_1(class DelegateByRefParamArray_Base[]& p) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )  

    // Code size       43 (0x2b)
    .maxstack  3
    .locals init (class DelegateByRefParamArray_Base[] V_0,
             class DelegateByRefParamArray_Base[] V_1)
    IL_0000:  ldstr      "Called SubWithByRefParamArrayOfReferenceTypes_Iden"
    + "tify_1."
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.2
    IL_000b:  newarr     DelegateByRefParamArray_Base
    IL_0010:  stloc.1
    IL_0011:  ldloc.1
    IL_0012:  ldc.i4.0
    IL_0013:  ldc.i4.s   23
    IL_0015:  newobj     instance void DelegateByRefParamArray_Derived::.ctor(int32)
    IL_001a:  stelem.ref
    IL_001b:  ldloc.1
    IL_001c:  ldc.i4.1
    IL_001d:  ldc.i4.s   42
    IL_001f:  newobj     instance void DelegateByRefParamArray_Derived::.ctor(int32)
    IL_0024:  stelem.ref
    IL_0025:  ldloc.1
    IL_0026:  stloc.0
    IL_0027:  ldarg.0
    IL_0028:  ldloc.0
    IL_0029:  stind.ref
    IL_002a:  ret
  } // end of method DelegateByRefParamArray::SubWithByRefParamArrayOfReferenceTypes_Identify_1

  .method public static void  SubWithByRefParamArrayOfReferenceTypes_Identify_2(class DelegateByRefParamArray_Base[]& p) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    
    // Code size       43 (0x2b)
    .maxstack  3
    .locals init (class DelegateByRefParamArray_Base[] V_0,
             class DelegateByRefParamArray_Base[] V_1)
    IL_0000:  ldstr      "Called SubWithByRefParamArrayOfReferenceTypes_Iden"
    + "tify_2."
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.2
    IL_000b:  newarr     DelegateByRefParamArray_Base
    IL_0010:  stloc.1
    IL_0011:  ldloc.1
    IL_0012:  ldc.i4.0
    IL_0013:  ldc.i4.s   23
    IL_0015:  newobj     instance void DelegateByRefParamArray_Base::.ctor(int32)
    IL_001a:  stelem.ref
    IL_001b:  ldloc.1
    IL_001c:  ldc.i4.1
    IL_001d:  ldc.i4.s   42
    IL_001f:  newobj     instance void DelegateByRefParamArray_Base::.ctor(int32)
    IL_0024:  stelem.ref
    IL_0025:  ldloc.1
    IL_0026:  stloc.0
    IL_0027:  ldarg.0
    IL_0028:  ldloc.0
    IL_0029:  stind.ref
    IL_002a:  ret
  } // end of method DelegateByRefParamArray::SubWithByRefParamArrayOfReferenceTypes_Identify_2

} // end of class DelegateByRefParamArray


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file DelegateByRefParamArray.res
