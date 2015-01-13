// ilasm /dll Events.il /out=Events.dll

.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly Events {}

.class public auto ansi beforefieldinit NonGeneric
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          add_InstanceEvent(class [mscorlib]System.Action 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          remove_InstanceEvent(class [mscorlib]System.Action 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  add_StaticEvent(class [mscorlib]System.Action 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  remove_StaticEvent(class [mscorlib]System.Action 'value') cil managed
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

  .event class [mscorlib]System.Action InstanceEvent
  {
    .addon instance void NonGeneric::add_InstanceEvent(class [mscorlib]System.Action)
    .removeon instance void NonGeneric::remove_InstanceEvent(class [mscorlib]System.Action)
  }

  .event class [mscorlib]System.Action StaticEvent
  {
    .addon void NonGeneric::add_StaticEvent(class [mscorlib]System.Action)
    .removeon void NonGeneric::remove_StaticEvent(class [mscorlib]System.Action)
  }
} // end of class NonGeneric

.class public auto ansi beforefieldinit Generic`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          add_InstanceEvent(class [mscorlib]System.Action`1<!T> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          remove_InstanceEvent(class [mscorlib]System.Action`1<!T> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  add_StaticEvent(class [mscorlib]System.Action`1<!T> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  remove_StaticEvent(class [mscorlib]System.Action`1<!T> 'value') cil managed
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

  .event class [mscorlib]System.Action`1<!T> InstanceEvent
  {
    .addon instance void Generic`1::add_InstanceEvent(class [mscorlib]System.Action`1<!T>)
    .removeon instance void Generic`1::remove_InstanceEvent(class [mscorlib]System.Action`1<!T>)
  }

  .event class [mscorlib]System.Action`1<!T> StaticEvent
  {
    .addon void Generic`1::add_StaticEvent(class [mscorlib]System.Action`1<!T>)
    .removeon void Generic`1::remove_StaticEvent(class [mscorlib]System.Action`1<!T>)
  }
} // end of class Generic`1

.class public auto ansi beforefieldinit ClosedGeneric
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          add_InstanceEvent(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          remove_InstanceEvent(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  add_StaticEvent(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  remove_StaticEvent(class [mscorlib]System.Action`1<int32> 'value') cil managed
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

  .event class [mscorlib]System.Action`1<int32> InstanceEvent
  {
    .addon instance void ClosedGeneric::add_InstanceEvent(class [mscorlib]System.Action`1<int32>)
    .removeon instance void ClosedGeneric::remove_InstanceEvent(class [mscorlib]System.Action`1<int32>)
  }

  .event class [mscorlib]System.Action`1<int32> StaticEvent
  {
    .addon void ClosedGeneric::add_StaticEvent(class [mscorlib]System.Action`1<int32>)
    .removeon void ClosedGeneric::remove_StaticEvent(class [mscorlib]System.Action`1<int32>)
  }
} // end of class ClosedGeneric

.class public auto ansi beforefieldinit SignatureMismatch
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          add_AddMismatch(class [mscorlib]System.Action`1<char> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          remove_AddMismatch(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  add_RemoveMismatch(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  remove_RemoveMismatch(class [mscorlib]System.Action`1<char> 'value') cil managed
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

  .event class [mscorlib]System.Action`1<int32> AddMismatch
  {
    .addon instance void SignatureMismatch::add_AddMismatch(class [mscorlib]System.Action`1<char>)
    .removeon instance void SignatureMismatch::remove_AddMismatch(class [mscorlib]System.Action`1<int32>)
  }

  .event class [mscorlib]System.Action`1<int32> RemoveMismatch
  {
    .addon void SignatureMismatch::add_RemoveMismatch(class [mscorlib]System.Action`1<int32>)
    .removeon void SignatureMismatch::remove_RemoveMismatch(class [mscorlib]System.Action`1<char>)
  }
} // end of class SignatureMismatch

.class public auto ansi beforefieldinit AccessorMissingParameter
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          add_AddNoParam() cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          remove_AddNoParam(class [mscorlib]System.Action 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  add_RemoveNoParam(class [mscorlib]System.Action 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          void  remove_RemoveNoParam() cil managed
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

  .event class [mscorlib]System.Action AddNoParam
  {
    .addon instance void AccessorMissingParameter::add_AddNoParam()
    .removeon instance void AccessorMissingParameter::remove_AddNoParam(class [mscorlib]System.Action 'value')
  }

  .event class [mscorlib]System.Action RemoveNoParam
  {
    .addon void AccessorMissingParameter::add_RemoveNoParam(class [mscorlib]System.Action 'value')
    .removeon void AccessorMissingParameter::remove_RemoveNoParam()
  }
} // end of class AccessorMissingParameter

.class public auto ansi beforefieldinit NonDelegateEvent
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          add_NonDelegate(int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          remove_NonDelegate(int32 'value') cil managed
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

  .event int32 NonDelegate
  {
    .addon instance void NonDelegateEvent::add_NonDelegate(int32 'value')
    .removeon instance void NonDelegateEvent::remove_NonDelegate(int32 'value')
  }
} // end of class NonDelegateEvent

.class public auto ansi beforefieldinit VirtualEventWithRaise
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method VirtualEventWithRaise::.ctor

    .method public hidebysig newslot specialname virtual instance void 
          add_e(class [mscorlib]System.Action 'value') cil managed
  {
    ldstr      "VirtualEventWithRaise Add"
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          remove_e(class [mscorlib]System.Action 'value') cil managed
  {
    ldstr      "VirtualEventWithRaise Remove"
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          raise_e(object sender, object e) cil managed
  {
    ldstr      "VirtualEventWithRaise Raise"
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  // public virtual event with fire accessor
  .event [mscorlib]System.Action e
  {
    .addon instance void VirtualEventWithRaise::add_e(class [mscorlib]System.Action)
    .removeon instance void VirtualEventWithRaise::remove_e(class [mscorlib]System.Action)
    .fire instance void VirtualEventWithRaise::raise_e(object, object)
  } // end of event VirtualEventWithRaise::e

  .method public hidebysig instance void  Fire() cil managed
  {
    ldarg.0
    ldarg.0
	ldnull
    callvirt   instance void VirtualEventWithRaise::raise_e(object, object)
    ret
  }

} // end of class VirtualEventWithRaise

.class public auto ansi beforefieldinit AccessorModifierMismatchBase
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  AddOverride(class [mscorlib]System.Action a) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot virtual 
          instance void  RemoveOverride(class [mscorlib]System.Action a) cil managed
  {
    ret
  }
  
  .method public hidebysig newslot virtual 
          instance void  AddSealed(class [mscorlib]System.Action a) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot virtual 
          instance void  RemoveSealed(class [mscorlib]System.Action a) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor(class [mscorlib]System.Action)
    ret
  }

} // end of class AccessorModifierMismatchBase

.class public abstract auto ansi beforefieldinit AccessorModifierMismatch
       extends AccessorModifierMismatchBase
{
  .method public hidebysig instance void  AddNone(class [mscorlib]System.Action a) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig instance void  RemoveNone(class [mscorlib]System.Action a) cil managed
  {
    ret
  }

  .method public hidebysig newslot abstract virtual 
          instance void  AddAbstract(class [mscorlib]System.Action a) cil managed
  {
  }

  .method public hidebysig newslot abstract virtual 
          instance void  RemoveAbstract(class [mscorlib]System.Action a) cil managed
  {
  }

  .method public hidebysig newslot virtual 
          instance void  AddVirtual(class [mscorlib]System.Action a) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot virtual 
          instance void  RemoveVirtual(class [mscorlib]System.Action a) cil managed
  {
    ret
  }

  .method public hidebysig virtual instance void 
          AddOverride(class [mscorlib]System.Action a) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig virtual instance void 
          RemoveOverride(class [mscorlib]System.Action a) cil managed
  {
    ret
  }

  .method public hidebysig virtual final instance void 
          AddSealed(class [mscorlib]System.Action a) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig virtual final instance void 
          RemoveSealed(class [mscorlib]System.Action a) cil managed
  {
    ret
  }

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void AccessorModifierMismatchBase::.ctor()
    ret
  }

  ////

  .event [mscorlib]System.Action NoneNone
  {
    .addon instance void AccessorModifierMismatch::AddNone(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveNone(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action NoneAbstract
  {
    .addon instance void AccessorModifierMismatch::AddNone(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveAbstract(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action NoneVirtual
  {
    .addon instance void AccessorModifierMismatch::AddNone(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveVirtual(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action NoneOverride
  {
    .addon instance void AccessorModifierMismatch::AddNone(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveOverride(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action NoneSealed
  {
    .addon instance void AccessorModifierMismatch::AddNone(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveSealed(class [mscorlib]System.Action)
  }

  ////

  .event [mscorlib]System.Action AbstractNone
  {
    .addon instance void AccessorModifierMismatch::AddAbstract(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveNone(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action AbstractAbstract
  {
    .addon instance void AccessorModifierMismatch::AddAbstract(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveAbstract(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action AbstractVirtual
  {
    .addon instance void AccessorModifierMismatch::AddAbstract(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveVirtual(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action AbstractOverride
  {
    .addon instance void AccessorModifierMismatch::AddAbstract(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveOverride(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action AbstractSealed
  {
    .addon instance void AccessorModifierMismatch::AddAbstract(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveSealed(class [mscorlib]System.Action)
  }

  ////

  .event [mscorlib]System.Action VirtualNone
  {
    .addon instance void AccessorModifierMismatch::AddVirtual(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveNone(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action VirtualAbstract
  {
    .addon instance void AccessorModifierMismatch::AddVirtual(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveAbstract(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action VirtualVirtual
  {
    .addon instance void AccessorModifierMismatch::AddVirtual(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveVirtual(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action VirtualOverride
  {
    .addon instance void AccessorModifierMismatch::AddVirtual(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveOverride(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action VirtualSealed
  {
    .addon instance void AccessorModifierMismatch::AddVirtual(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveSealed(class [mscorlib]System.Action)
  }

  ////

  .event [mscorlib]System.Action OverrideNone
  {
    .addon instance void AccessorModifierMismatch::AddOverride(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveNone(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action OverrideAbstract
  {
    .addon instance void AccessorModifierMismatch::AddOverride(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveAbstract(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action OverrideVirtual
  {
    .addon instance void AccessorModifierMismatch::AddOverride(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveVirtual(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action OverrideOverride
  {
    .addon instance void AccessorModifierMismatch::AddOverride(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveOverride(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action OverrideSealed
  {
    .addon instance void AccessorModifierMismatch::AddOverride(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveSealed(class [mscorlib]System.Action)
  }

  ////

  .event [mscorlib]System.Action SealedNone
  {
    .addon instance void AccessorModifierMismatch::AddSealed(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveNone(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action SealedAbstract
  {
    .addon instance void AccessorModifierMismatch::AddSealed(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveAbstract(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action SealedVirtual
  {
    .addon instance void AccessorModifierMismatch::AddSealed(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveVirtual(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action SealedOverride
  {
    .addon instance void AccessorModifierMismatch::AddSealed(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveOverride(class [mscorlib]System.Action)
  }

  .event [mscorlib]System.Action SealedSealed
  {
    .addon instance void AccessorModifierMismatch::AddSealed(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveSealed(class [mscorlib]System.Action)
  }

} // end of class AccessorModifierMismatch

