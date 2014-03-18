// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly cycledstructs
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module cycledstructs.dll
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0002       // WINDOWS_GUI
.corflags 0x00000001    //  ILONLY

.class public sequential ansi sealed beforefieldinit S1
       extends [mscorlib]System.ValueType
{
  .field public valuetype S1 fld
}

.class public sequential ansi sealed beforefieldinit S2
       extends [mscorlib]System.ValueType
{
  .field public valuetype S3 fld
}

.class public sequential ansi sealed beforefieldinit S3
       extends [mscorlib]System.ValueType
{
  .field public valuetype S2 fld
}