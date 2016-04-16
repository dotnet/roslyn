
// ilasm /DLL ConstantFields.il

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly ConstantFields
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module ConstantFields.dll
// MVID: {04FA0578-FD8D-4B46-A552-24F276CC7A05}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0002       // WINDOWS_GUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x008C0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi ConstFields
       extends [mscorlib]System.Object
{
  .field public static literal int64 Int64Field = int64(0x8CD8AA0F4A42FFB)
  .field public static literal valuetype [mscorlib]System.DateTime DateTimeField = int64(0x8CD8AA0F4A42FFB)
  .field public static literal float32 SingleField = float32(9.)
  .field public static literal float64 DoubleField = float64(-10.)
  .field public static literal string StringField = "11"
  .field public static literal string StringNullField = nullref
  .field public static literal int32 ObjectInt32Field = int32(0x0000000C)
  .field public static literal object ObjectNullField = nullref
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ConstFields::.ctor

} // end of class ConstFields

.class public auto ansi sealed ByteEnum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname uint8 value__
  .field public static literal valuetype ByteEnum ByteValue = uint8(0x01)
} // end of class ByteEnum

.class public auto ansi sealed SByteEnum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int8 value__
  .field public static literal valuetype SByteEnum SByteValue = int8(0xFE)
} // end of class SByteEnum

.class public auto ansi sealed UInt16Enum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname uint16 value__
  .field public static literal valuetype UInt16Enum UInt16Value = uint16(0x0003)
} // end of class UInt16Enum

.class public auto ansi sealed Int16Enum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int16 value__
  .field public static literal valuetype Int16Enum Int16Value = int16(0xFFFC)
} // end of class Int16Enum

.class public auto ansi sealed UInt32Enum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname uint32 value__
  .field public static literal valuetype UInt32Enum UInt32Value = uint32(0x00000005)
} // end of class UInt32Enum

.class public auto ansi sealed Int32Enum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Int32Enum Int32Value = int32(0xFFFFFFFA)
} // end of class Int32Enum

.class public auto ansi sealed UInt64Enum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname uint64 value__
  .field public static literal valuetype UInt64Enum UInt64Value = uint64(0x7)
} // end of class UInt64Enum

.class public auto ansi sealed Int64Enum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int64 value__
  .field public static literal valuetype Int64Enum Int64Value = int64(0xFFFFFFFFFFFFFFF8)
} // end of class Int64Enum


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ConstantFields.res
