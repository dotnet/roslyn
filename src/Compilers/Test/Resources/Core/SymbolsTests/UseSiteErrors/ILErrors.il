// Depends on Unavailable.dll
// ilasm /DLL ILErrors.il /OUTPUT=ILErrors.dll

.assembly extern Unavailable
{
  .ver 0:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly ILErrors
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.class public auto ansi beforefieldinit ILErrors.ClassMethods
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance int32 modopt([Unavailable]UnavailableClass)  ReturnType1() cil managed
  {
    ldc.i4.0
	ret
  }

  .method public hidebysig newslot virtual 
          instance int32 modopt([Unavailable]UnavailableClass) []  ReturnType2() cil managed
  {
    ldnull
	ret
  }

  .method public hidebysig newslot virtual 
          instance void  ParameterType1(int32 modopt([Unavailable]UnavailableClass) u) cil managed
  {
	ret
  }

  .method public hidebysig newslot virtual 
          instance void  ParameterType2(int32 modopt([Unavailable]UnavailableClass) [] u) cil managed
  {
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class ClassMethods

.class interface public abstract auto ansi ILErrors.InterfaceMethods
{
  .method public hidebysig newslot abstract virtual 
          instance int32 modopt([Unavailable]UnavailableClass)  ReturnType1() cil managed
  {
  }

  .method public hidebysig newslot abstract virtual 
          instance int32 modopt([Unavailable]UnavailableClass) []  ReturnType2() cil managed
  {
  }

  .method public hidebysig newslot abstract virtual 
          instance void  ParameterType1(int32 modopt([Unavailable]UnavailableClass) u) cil managed
  {
  }

  .method public hidebysig newslot abstract virtual 
          instance void  ParameterType2(int32 modopt([Unavailable]UnavailableClass) [] u) cil managed
  {
  }

} // end of class InterfaceMethods

.class public auto ansi beforefieldinit ILErrors.ClassProperties
       extends [mscorlib]System.Object
{

  .method public hidebysig newslot specialname virtual 
          instance int32 modopt([Unavailable]UnavailableClass)  get_GetSet1() cil managed
  {
    ldc.i4.0
	ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  set_GetSet1(int32 modopt([Unavailable]UnavailableClass) 'value') cil managed
  {
	ret
  }

  .method public hidebysig newslot specialname virtual 
          instance int32 modopt([Unavailable]UnavailableClass) []  get_GetSet2() cil managed
  {
    ldnull
	ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  set_GetSet2(int32 modopt([Unavailable]UnavailableClass) [] 'value') cil managed
  {
	ret
  }

  .method public hidebysig newslot specialname virtual 
          instance int32 modopt([Unavailable]UnavailableClass)  get_Get1() cil managed
  {
    ldc.i4.0
	ret
  }

  .method public hidebysig newslot specialname virtual 
          instance int32 modopt([Unavailable]UnavailableClass) []  get_Get2() cil managed
  {
    ldnull
	ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  set_Set1(int32 modopt([Unavailable]UnavailableClass) 'value') cil managed
  {
	ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  set_Set2(int32 modopt([Unavailable]UnavailableClass) [] 'value') cil managed
  {
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) GetSet1()
  {
    .get instance int32 modopt([Unavailable]UnavailableClass) ILErrors.ClassProperties::get_GetSet1()
    .set instance void ILErrors.ClassProperties::set_GetSet1(int32 modopt([Unavailable]UnavailableClass))
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) [] GetSet2()
  {
    .set instance void ILErrors.ClassProperties::set_GetSet2(int32 modopt([Unavailable]UnavailableClass) [])
    .get instance int32 modopt([Unavailable]UnavailableClass) [] ILErrors.ClassProperties::get_GetSet2()
  }

  .property instance int32 GetSet3()
  {
    .get instance int32 modopt([Unavailable]UnavailableClass) ILErrors.ClassProperties::get_GetSet1()
    .set instance void ILErrors.ClassProperties::set_GetSet1(int32 modopt([Unavailable]UnavailableClass))
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) Get1()
  {
    .get instance int32 modopt([Unavailable]UnavailableClass) ILErrors.ClassProperties::get_Get1()
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) [] Get2()
  {
    .get instance int32 modopt([Unavailable]UnavailableClass) [] ILErrors.ClassProperties::get_Get2()
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) Set1()
  {
    .set instance void ILErrors.ClassProperties::set_Set1(int32 modopt([Unavailable]UnavailableClass))
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) [] Set2()
  {
    .set instance void ILErrors.ClassProperties::set_Set2(int32 modopt([Unavailable]UnavailableClass) [])
  }
} // end of class ClassProperties

.class interface public abstract auto ansi ILErrors.InterfaceProperties
{
  .method public hidebysig newslot specialname abstract virtual 
          instance int32 modopt([Unavailable]UnavailableClass)  get_GetSet1() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_GetSet1(int32 modopt([Unavailable]UnavailableClass) 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance int32 modopt([Unavailable]UnavailableClass) []  get_GetSet2() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_GetSet2(int32 modopt([Unavailable]UnavailableClass) [] 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance int32 modopt([Unavailable]UnavailableClass)  get_Get1() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance int32 modopt([Unavailable]UnavailableClass) []  get_Get2() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Set1(int32 modopt([Unavailable]UnavailableClass) 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Set2(int32 modopt([Unavailable]UnavailableClass) [] 'value') cil managed
  {
  }

  .property instance int32 modopt([Unavailable]UnavailableClass)  GetSet1()
  {
    .get instance int32 modopt([Unavailable]UnavailableClass) ILErrors.InterfaceProperties::get_GetSet1()
    .set instance void ILErrors.InterfaceProperties::set_GetSet1(int32 modopt([Unavailable]UnavailableClass))
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) [] GetSet2()
  {
    .set instance void ILErrors.InterfaceProperties::set_GetSet2(int32 modopt([Unavailable]UnavailableClass) [])
    .get instance int32 modopt([Unavailable]UnavailableClass) [] ILErrors.InterfaceProperties::get_GetSet2()
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) Get1()
  {
    .get instance int32 modopt([Unavailable]UnavailableClass) ILErrors.InterfaceProperties::get_Get1()
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) [] Get2()
  {
    .get instance int32 modopt([Unavailable]UnavailableClass) [] ILErrors.InterfaceProperties::get_Get2()
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) Set1()
  {
    .set instance void ILErrors.InterfaceProperties::set_Set1(int32 modopt([Unavailable]UnavailableClass))
  }

  .property instance int32 modopt([Unavailable]UnavailableClass) [] Set2()
  {
    .set instance void ILErrors.InterfaceProperties::set_Set2(int32 modopt([Unavailable]UnavailableClass) [])
  }
} // end of class InterfaceProperties

.class public auto ansi beforefieldinit ILErrors.ClassEvents
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual instance void 
          add_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []> 'value') cil managed
  {
	ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          remove_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []> 'value') cil managed
  {
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .event class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []> Event1
  {
    .addon instance void ILErrors.ClassEvents::add_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []>)
    .removeon instance void ILErrors.ClassEvents::remove_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []>)
  } // end of event ILErrors.ClassEvents::Event1
} // end of class ILErrors.ClassEvents

.class public auto ansi beforefieldinit ILErrors.ClassEventsNonVirtual
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          add_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []> 'value') cil managed
  {
	ret
  }

  .method public hidebysig specialname instance void 
          remove_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []> 'value') cil managed
  {
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .event class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []> Event1
  {
    .addon instance void ILErrors.ClassEventsNonVirtual::add_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []>)
    .removeon instance void ILErrors.ClassEventsNonVirtual::remove_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []>)
  } // end of event ILErrors.ClassEventsNonVirtual::Event1
} // end of class ILErrors.ClassEventsNonVirtual

.class interface public abstract auto ansi ILErrors.InterfaceEvents
{
  .method public hidebysig newslot specialname abstract virtual instance void 
          add_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []> 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void 
          remove_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []> 'value') cil managed
  {
  }

  .event class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []> Event1
  {
    .addon instance void ILErrors.InterfaceEvents::add_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []>)
    .removeon instance void ILErrors.InterfaceEvents::remove_Event1(class [mscorlib]System.Action`1<int32 modopt([Unavailable]UnavailableClass) []>)
  } // end of event ILErrors.InterfaceEvents::Event1
} // end of class ILErrors.InterfaceEvents

.class public auto ansi beforefieldinit ILErrors.ModReqClassEventsNonVirtual
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          add_Event1(class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []> 'value') cil managed
  {
	ret
  }

  .method public hidebysig specialname instance void 
          remove_Event1(class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []> 'value') cil managed
  {
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .event class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []> Event1
  {
    .addon instance void ILErrors.ModReqClassEventsNonVirtual::add_Event1(class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []>)
    .removeon instance void ILErrors.ModReqClassEventsNonVirtual::remove_Event1(class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []>)
  }
}

.class interface public abstract auto ansi ILErrors.ModReqInterfaceEvents
{
  .method public hidebysig newslot specialname abstract virtual instance void 
          add_Event1(class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []> 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void 
          remove_Event1(class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []> 'value') cil managed
  {
  }

  .event class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []> Event1
  {
    .addon instance void ILErrors.ModReqInterfaceEvents::add_Event1(class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []>)
    .removeon instance void ILErrors.ModReqInterfaceEvents::remove_Event1(class [mscorlib]System.Action`1<int32 modreq([Unavailable]UnavailableClass) []>)
  }
}