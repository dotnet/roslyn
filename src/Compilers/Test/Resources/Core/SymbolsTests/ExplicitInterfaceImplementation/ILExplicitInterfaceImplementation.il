//ilasm /dll

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly 'explicit'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module explicit.dll
// MVID: {D1CBBFEA-E97B-4300-891A-16D10C48A508}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x003F0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi I1
{
  .method public hidebysig newslot abstract virtual 
          instance void  Method1() cil managed
  {
  } // end of method I1::Method1

} // end of class I1

.class interface private abstract auto ansi I2
{
  .method public hidebysig newslot abstract virtual 
          instance void  Method2() cil managed
  {
  } // end of method I2::Method2

} // end of class I2

.class private auto ansi beforefieldinit C
       extends [mscorlib]System.Object
       implements I1,
                  I2
{
  .method private hidebysig newslot virtual final 
          instance void  Method() cil managed
  { //explicitly implements two different interface methods
    .override I1::Method1
    .override I2::Method2
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method C::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor

} // end of class C

.class interface private abstract auto ansi ImplementsSelf
       implements ImplementsSelf
{
  .method public hidebysig newslot abstract virtual 
          instance void  Method() cil managed
  {
  } // end of method ImplementsSelf::Method

} // end of class ImplementsSelf

//implements an interface with a cycle, but does correctly explicitly implement an interface method
.class private auto ansi beforefieldinit InterfaceCycleSuccess
       extends [mscorlib]System.Object
       implements ImplementsSelf,
	              I1
{
  .method private hidebysig newslot virtual final 
          instance void  Method() cil managed
  { //explicitly implements two different interface methods
    .override I1::Method1
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method InterfaceCycleSuccess::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method InterfaceCycleSuccess::.ctor

} // end of class InterfaceCycleSuccess

//implements an interface with a cycle, and does not correctly explicitly implement an interface method
.class private auto ansi beforefieldinit InterfaceCycleFailure
       extends [mscorlib]System.Object
       implements ImplementsSelf
{
  .method private hidebysig newslot virtual final 
          instance void  Method() cil managed
  { //explicitly implements two different interface methods
    .override I1::Method1
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method InterfaceCycleFailure::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method InterfaceCycleFailure::.ctor

} // end of class InterfaceCycleFailure


.class private auto ansi beforefieldinit ExplicitlyImplementedClass
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  Method() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method ExplicitlyImplementedClass::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ExplicitlyImplementedClass::.ctor

} // end of class ExplicitlyImplementedClass

.class private auto ansi beforefieldinit ExplicitlyImplementsAClass
       extends ExplicitlyImplementedClass
{
  .method private hidebysig newslot virtual final 
          instance void  Method() cil managed
  { //explicitly implements a class method
    .override ExplicitlyImplementedClass::Method
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method ExplicitlyImplementsAClass::Method1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ExplicitlyImplementedClass::.ctor()
    IL_0006:  ret
  } // end of method ExplicitlyImplementsAClass::.ctor

} // end of class ExplicitlyImplementsAClass

.class interface private abstract auto ansi IUnrelated
{
  .method public hidebysig newslot abstract virtual 
          instance void  Method() cil managed
  {
  } // end of method IUnrelated::Method

} // end of class IUnrelated

.class interface private abstract auto ansi IUnrelated`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance void  Method() cil managed
  {
  } // end of method IUnrelated`1::Method

} // end of class IUnrelated`1

.class private auto ansi beforefieldinit ExplicitlyImplementsUnrelatedInterfaceMethods
       extends [mscorlib]System.Object
{
  .method private hidebysig newslot virtual final 
          instance void  Method1() cil managed
  { //explicitly implements a class method
    .override IUnrelated::Method
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method ExplicitlyImplementsUnrelatedInterfaceMethods::Method1

  .method private hidebysig newslot virtual final 
          instance void  Method2() cil managed
  {
    .override  method instance void class IUnrelated`1<int32>::Method()
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method ExplicitlyImplementsUnrelatedInterfaceMethods::Method2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ExplicitlyImplementsUnrelatedInterfaceMethods::.ctor

} // end of class ExplicitlyImplementsUnrelatedInterfaceMethods

.class private auto ansi beforefieldinit BaseDeclaresInterface
       extends [mscorlib]System.Object
       implements I1
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BaseDeclaresInterface::.ctor

} // end of class BaseDeclaresInterface

.class private auto ansi beforefieldinit DerivedExplicitlyImplementsInterface
       extends BaseDeclaresInterface
{
  .method private hidebysig newslot virtual final 
          instance void  I1.Method1() cil managed
  {
    .override I1::Method1
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method DerivedExplicitlyImplementsInterface::I1.Method1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseDeclaresInterface::.ctor()
    IL_0006:  ret
  } // end of method DerivedExplicitlyImplementsInterface::.ctor

} // end of class DerivedExplicitlyImplementsInterface

// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file explicit.res
