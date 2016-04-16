//ilasm /dll

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly explicitProperties
{
  .custom instance void [mscorlib]System.Reflection.AssemblyDescriptionAttribute::.ctor(string)
           = {string('')}
  .custom instance void [mscorlib]System.Reflection.AssemblyConfigurationAttribute::.ctor(string)
           = {string('')}
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor()
           = {property bool 'WrapNonExceptionThrows' = bool(true)}
  .custom instance void [mscorlib]System.Reflection.AssemblyProductAttribute::.ctor(string)
           = {string('explicitProperties')}

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes)
  //           = {int32(263)}

  .custom instance void [mscorlib]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string)
           = {string('.NETFramework,Version=v4.0')
              property string 'FrameworkDisplayName' = string('.NET Framework 4')}
  .custom instance void [mscorlib]System.Reflection.AssemblyTitleAttribute::.ctor(string)
           = {string('explicitProperties')}
  .custom instance void [mscorlib]System.Reflection.AssemblyCompanyAttribute::.ctor(string)
           = {string('Microsoft')}
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32)
           = {int32(8)}
  .custom instance void [mscorlib]System.Reflection.AssemblyCopyrightAttribute::.ctor(string)
           = {string('Copyright c Microsoft 2011')}
  .custom instance void [mscorlib]System.Reflection.AssemblyTrademarkAttribute::.ctor(string)
           = {string('')}
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool)
           = {bool(false)}
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string)
           = {string('4e429279-6bcf-4caf-8c64-4d20a0834e1d')}
  .custom instance void [mscorlib]System.Reflection.AssemblyFileVersionAttribute::.ctor(string)
           = {string('1.0.0.0')}
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module explicitProperties.dll
// MVID: {7EF2325E-5BC8-4EC5-9923-DB26EF61501C}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00260000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi Interface
{
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Property1() cil managed
  {
  } // end of method Interface::get_Property1

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Property1(int32 'value') cil managed
  {
  } // end of method Interface::set_Property1

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Property2() cil managed
  {
  } // end of method Interface::get_Property2

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Property2(int32 'value') cil managed
  {
  } // end of method Interface::set_Property2

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Property3() cil managed
  {
  } // end of method Interface::get_Property3

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Property3(int32 'value') cil managed
  {
  } // end of method Interface::set_Property3

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Property4() cil managed
  {
  } // end of method Interface::get_Property4

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Property5(int32 'value') cil managed
  {
  } // end of method Interface::set_Property5

  .property instance int32 Property1()
  {
    .get instance int32 Interface::get_Property1()
    .set instance void Interface::set_Property1(int32)
  } // end of property Interface::Property1
  .property instance int32 Property2()
  {
    .set instance void Interface::set_Property2(int32)
    .get instance int32 Interface::get_Property2()
  } // end of property Interface::Property2
  .property instance int32 Property3()
  {
    .get instance int32 Interface::get_Property3()
    .set instance void Interface::set_Property3(int32)
  } // end of property Interface::Property3
  .property instance int32 Property4()
  {
    .get instance int32 Interface::get_Property4()
  } // end of property Interface::Property4
  .property instance int32 Property5()
  {
    .set instance void Interface::set_Property5(int32)
  } // end of property Interface::Property5
} // end of class Interface

.class public auto ansi beforefieldinit Class
       extends [mscorlib]System.Object
       implements Interface
{
  .method private hidebysig newslot specialname virtual final 
          instance int32  getter() cil managed
  {
    .override Interface::get_Property1
    .override Interface::get_Property2
    .override Interface::get_Property4
    // Code size       3 (0x3)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  throw
  } // end of method Class::getter

  .method private hidebysig newslot specialname virtual final 
          instance void  setter(int32 'value') cil managed
  {
    .override Interface::set_Property1
    .override Interface::set_Property3
    .override Interface::set_Property5
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Class::setter

  // There has to be an implementation of Interface::get_Property3 somewhere
  // in this class if we want the type to load.
  .method private hidebysig newslot specialname virtual final 
          instance int32  getter2() cil managed
  {
    .override Interface::get_Property3
    // Code size       3 (0x3)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  throw
  } // end of method Class::getter2

  // There has to be an implementation of Interface::set_Property2 somewhere
  // in this class if we want the type to load.
  .method private hidebysig newslot specialname virtual final 
          instance void  setter2(int32 'value') cil managed
  {
    .override Interface::set_Property2
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Class::setter2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Class::.ctor

  .property instance int32 Property1()
  {
    .get instance int32 Class::getter()
    .set instance void Class::setter(int32)
  } // end of property Class::Property1

  .property instance int32 Property2()
  {
    .get instance int32 Class::getter2()
    .set instance void Class::setter2(int32)
  } // end of property Class::Property2
} // end of class Class


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
