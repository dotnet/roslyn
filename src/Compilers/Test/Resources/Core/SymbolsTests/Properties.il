// ilasm /dll Properties.il /out=Properties.dll

.assembly extern mscorlib 
{
    .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                      
    .ver 4:0:0:0
}

.assembly Properties {}

.class public Valid
{
	.method public static int32 StaticGet()
	{
	    ldc.i4.0
		ret
	}
	
    .method public static void StaticSet(int32 val)
	{
		ret
	}
	
	.property int32 Static()
	{
	    .get int32 Valid::StaticGet()
		.set void Valid::StaticSet(int32)
	}

	.method public instance int32 InstanceGet()
	{
	    ldc.i4.0
		ret
	}

    .method public instance void InstanceSet(int32 val)
	{
		ret
	}

	.property int32 Instance()
	{
	    .get instance int32 Valid::InstanceGet()
		.set instance void Valid::InstanceSet(int32)
	}
}

.class public Mismatched
{
	.method public static int32 StaticInt32Get()
	{
	    ldc.i4.0
		ret
	}
	
    .method public static void StaticBoolSet(bool val)
	{
		ret
	}

	.property int32 StaticMismatched()
	{
	    .get int32 Mismatched::StaticInt32Get()
		.set void Mismatched::StaticBoolSet(bool)
	}

	.method public instance int32 InstanceInt32Get()
	{
	    ldc.i4.0
		ret
	}

    .method public instance void InstanceBoolSet(bool val)
	{
		ret
	}

	.property int32 InstanceMismatched()
	{
	    .get instance int32 Mismatched::InstanceInt32Get()
		.set instance void Mismatched::InstanceBoolSet(bool)
	}
}

.class public Signatures
{
	.method public static int32 StaticGet()
	{
	    ldc.i4.0
		ret
	}
	
    .method public static void StaticSet(int32 val)
	{
		ret
	}
	
    .method public static void StaticSet(bool val)
	{
		ret
	}
	
	.property int32 GoodStatic()
	{
	    .get int32 Signatures::StaticGet()
		.set void Signatures::StaticSet(int32)
	}

	.property bool BadStatic()
	{
	    .get int32 Signatures::StaticGet()
		.set void Signatures::StaticSet(int32)
	}
	
	.property int32 MismatchedStatic()
	{
	    .get int32 Signatures::StaticGet()
		.set void Signatures::StaticSet(bool)
	}

	.method public instance int32 InstanceGet()
	{
	    ldc.i4.0
		ret
	}

    .method public instance void InstanceSet(int32 val)
	{
		ret
	}

    .method public instance void InstanceSet(bool val)
	{
		ret
	}

	.property int32 GoodInstance()
	{
	    .get instance int32 Signatures::InstanceGet()
		.set instance void Signatures::InstanceSet(int32)
	}

	.property bool BadInstance()
	{
	    .get instance int32 Signatures::InstanceGet()
		.set instance void Signatures::InstanceSet(int32)
	}

	.property int32 MismatchedInstance()
	{
	    .get instance int32 Signatures::InstanceGet()
		.set instance void Signatures::InstanceSet(bool)
	}

	.property int32 StaticAndInstance()
	{
	    .get int32 Signatures::StaticGet()
		.set instance void Signatures::InstanceSet(int32)
	}

	.property int32 GetUsedAsSet()
	{
	    .get instance int32 Signatures::InstanceGet()
	    .set instance int32 Signatures::InstanceGet()
	}
}

.class public NoAccessors
{
	.method public static void StaticMethod()
	{
		ret
	}
	
	.property int32 Static()
	{
	    .other void NoAccessors::StaticMethod()
	}

	.method public instance void InstanceMethod()
	{
		ret
	}

	.property int32 Instance()
	{
	    .other instance void NoAccessors::InstanceMethod()
	}
}

.class public FamilyAssembly
{
	.method family static int32 StaticFamilyGet()
	{
	    ldc.i4.0
		ret
	}
	
	.method assembly static int32 StaticAssemblyGet()
	{
	    ldc.i4.0
		ret
	}
	
	.method famorassem static int32 StaticFamilyOrAssemblyGet()
	{
	    ldc.i4.0
		ret
	}
		
	.method famandassem static int32 StaticFamilyAndAssemblyGet()
	{
	    ldc.i4.0
		ret
	}

    .method assembly static void StaticAssemblySet(int32 val)
	{
		ret
	}
	
    .method famorassem static void StaticFamilyOrAssemblySet(int32 val)
	{
		ret
	}
	
    .method famandassem static void StaticFamilyAndAssemblySet(int32 val)
	{
		ret
	}
	
	.property int32 FamilyGetAssemblySetStatic()
	{
	    .get int32 FamilyAssembly::StaticFamilyGet()
		.set void FamilyAssembly::StaticAssemblySet(int32)
	}
		
	.property int32 FamilyGetFamilyOrAssemblySetStatic()
	{
	    .get int32 FamilyAssembly::StaticFamilyGet()
		.set void FamilyAssembly::StaticFamilyOrAssemblySet(int32)
	}
		
	.property int32 FamilyGetFamilyAndAssemblySetStatic()
	{
	    .get int32 FamilyAssembly::StaticFamilyGet()
		.set void FamilyAssembly::StaticFamilyAndAssemblySet(int32)
	}
		
	.property int32 AssemblyGetFamilyOrAssemblySetStatic()
	{
	    .get int32 FamilyAssembly::StaticAssemblyGet()
		.set void FamilyAssembly::StaticFamilyOrAssemblySet(int32)
	}
		
	.property int32 AssemblyGetFamilyAndAssemblySetStatic()
	{
	    .get int32 FamilyAssembly::StaticAssemblyGet()
		.set void FamilyAssembly::StaticFamilyAndAssemblySet(int32)
	}
			
	.property int32 FamilyOrAssemblyGetFamilyOrAssemblySetStatic()
	{
	    .get int32 FamilyAssembly::StaticFamilyOrAssemblyGet()
		.set void FamilyAssembly::StaticFamilyOrAssemblySet(int32)
	}
			
	.property int32 FamilyOrAssemblyGetFamilyAndAssemblySetStatic()
	{
	    .get int32 FamilyAssembly::StaticFamilyOrAssemblyGet()
		.set void FamilyAssembly::StaticFamilyAndAssemblySet(int32)
	}
			
	.property int32 FamilyAndAssemblyGetFamilyAndAssemblySetStatic()
	{
	    .get int32 FamilyAssembly::StaticFamilyAndAssemblyGet()
		.set void FamilyAssembly::StaticFamilyAndAssemblySet(int32)
	}

	.method famandassem instance int32 InstanceFamilyAndAssemblyGet()
	{
	    ldc.i4.0
		ret
	}

	.method famorassem instance void InstanceFamilyOrAssemblySet(int32 val)
	{
		ret
	}

	.property int32 FamilyAndAssemblyGetOnlyInstance()
	{
	    .get instance int32 FamilyAssembly::InstanceFamilyAndAssemblyGet()
	}

	.property int32 FamilyOrAssemblySetOnlyInstance()
	{
		.set instance void FamilyAssembly::InstanceFamilyOrAssemblySet(int32)
	}
	
	.property int32 FamilyAndAssemblyGetFamilyOrAssemblySetInstance()
	{
	    .get instance int32 FamilyAssembly::InstanceFamilyAndAssemblyGet()
		.set instance void FamilyAssembly::InstanceFamilyOrAssemblySet(int32)
	}
}

.class public Generic<T>
{
    .method public void .ctor()
	{
	}

	.method public instance !T InstanceGet()
	{
	    ldc.i4.0
		ret
	}

    .method public instance void InstanceSet(!T val)
	{
		ret
	}

	.property !T Instance()
	{
	    .get instance !T Generic::InstanceGet()
		.set instance void Generic::InstanceSet(!T)
	}
}

.class public GenericMismatch<T>
{
	.method public static !T StaticGet()
	{
	    ldc.i4.0
		ret
	}
	
    .method public static void StaticSet(int32 val)
	{
		ret
	}
	
	.property !T Static()
	{
	    .get !T GenericMismatch::StaticGet()
		.set void GenericMismatch::StaticSet(int32)
	}
}

.class public auto ansi WithParameterizedProps
       extends [mscorlib]System.Object
{
  .field private object _obj
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerNonUserCodeAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  } // end of method WithParameterizedProps::.ctor

  .method public specialname instance object 
          get_P(int32 i) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init ([0] object P)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      object WithParameterizedProps::_obj
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method WithParameterizedProps::get_P

  .method public specialname instance void 
          set_P(int32 i,
                object 'value') cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  box        [mscorlib]System.Int32
    IL_0008:  stfld      object WithParameterizedProps::_obj
    IL_000d:  nop
    IL_000e:  ret
  } // end of method WithParameterizedProps::set_P

  .method public specialname instance object 
          get_P(string s) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init ([0] object P)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      object WithParameterizedProps::_obj
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method WithParameterizedProps::get_P

  .method public specialname instance void 
          set_P(string s,
                object 'value') cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  stfld      object WithParameterizedProps::_obj
    IL_0008:  nop
    IL_0009:  ret
  } // end of method WithParameterizedProps::set_P

  .method public specialname instance object 
          get_P(int32 i,
                string s) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init ([0] object P)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      object WithParameterizedProps::_obj
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method WithParameterizedProps::get_P

  .method public specialname instance void 
          set_P(int32 i,
                string s,
                object 'value') cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.2
    IL_0003:  ldarga.s   i
    IL_0005:  call       instance string [mscorlib]System.Int32::ToString()
    IL_000a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000f:  stfld      object WithParameterizedProps::_obj
    IL_0014:  nop
    IL_0015:  ret
  } // end of method WithParameterizedProps::set_P

  .property instance object P(int32)
  {
    .get instance object WithParameterizedProps::get_P(int32)
    .set instance void WithParameterizedProps::set_P(int32,
                                                     object)
  } // end of property WithParameterizedProps::P
  .property instance object P(string)
  {
    .get instance object WithParameterizedProps::get_P(string)
    .set instance void WithParameterizedProps::set_P(string,
                                                     object)
  } // end of property WithParameterizedProps::P
  .property instance object P(int32,
                              string)
  {
    .get instance object WithParameterizedProps::get_P(int32,
                                                       string)
    .set instance void WithParameterizedProps::set_P(int32,
                                                     string,
                                                     object)
  } // end of property WithParameterizedProps::P
} // end of class WithParameterizedProps

.class public auto ansi beforefieldinit AccessorModifierMismatchBase
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance int32  GetOverride() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot virtual 
          instance void  SetOverride(int32 x) cil managed
  {
    ret
  }
  
  .method public hidebysig newslot virtual 
          instance int32  GetSealed() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot virtual 
          instance void  SetSealed(int32 x) cil managed
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

} // end of class AccessorModifierMismatchBase

.class public abstract auto ansi beforefieldinit AccessorModifierMismatch
       extends AccessorModifierMismatchBase
{
  .method public hidebysig instance int32  GetNone() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig instance void  SetNone(int32 x) cil managed
  {
    ret
  }

  .method public hidebysig newslot abstract virtual 
          instance int32  GetAbstract() cil managed
  {
  }

  .method public hidebysig newslot abstract virtual 
          instance void  SetAbstract(int32 x) cil managed
  {
  }

  .method public hidebysig newslot virtual 
          instance int32  GetVirtual() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot virtual 
          instance void  SetVirtual(int32 x) cil managed
  {
    ret
  }

  .method public hidebysig virtual instance int32 
          GetOverride() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig virtual instance void 
          SetOverride(int32 x) cil managed
  {
    ret
  }

  .method public hidebysig virtual final instance int32 
          GetSealed() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig virtual final instance void 
          SetSealed(int32 x) cil managed
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

  .property instance int32 NoneNone()
  {
    .get instance int32 AccessorModifierMismatch::GetNone()
    .set instance void AccessorModifierMismatch::SetNone(int32)
  }

  .property instance int32 NoneAbstract()
  {
    .get instance int32 AccessorModifierMismatch::GetNone()
    .set instance void AccessorModifierMismatch::SetAbstract(int32)
  }

  .property instance int32 NoneVirtual()
  {
    .get instance int32 AccessorModifierMismatch::GetNone()
    .set instance void AccessorModifierMismatch::SetVirtual(int32)
  }

  .property instance int32 NoneOverride()
  {
    .get instance int32 AccessorModifierMismatch::GetNone()
    .set instance void AccessorModifierMismatch::SetOverride(int32)
  }

  .property instance int32 NoneSealed()
  {
    .get instance int32 AccessorModifierMismatch::GetNone()
    .set instance void AccessorModifierMismatch::SetSealed(int32)
  }

  ////

  .property instance int32 AbstractNone()
  {
    .get instance int32 AccessorModifierMismatch::GetAbstract()
    .set instance void AccessorModifierMismatch::SetNone(int32)
  }

  .property instance int32 AbstractAbstract()
  {
    .get instance int32 AccessorModifierMismatch::GetAbstract()
    .set instance void AccessorModifierMismatch::SetAbstract(int32)
  }

  .property instance int32 AbstractVirtual()
  {
    .get instance int32 AccessorModifierMismatch::GetAbstract()
    .set instance void AccessorModifierMismatch::SetVirtual(int32)
  }

  .property instance int32 AbstractOverride()
  {
    .get instance int32 AccessorModifierMismatch::GetAbstract()
    .set instance void AccessorModifierMismatch::SetOverride(int32)
  }

  .property instance int32 AbstractSealed()
  {
    .get instance int32 AccessorModifierMismatch::GetAbstract()
    .set instance void AccessorModifierMismatch::SetSealed(int32)
  }

  ////

  .property instance int32 VirtualNone()
  {
    .get instance int32 AccessorModifierMismatch::GetVirtual()
    .set instance void AccessorModifierMismatch::SetNone(int32)
  }

  .property instance int32 VirtualAbstract()
  {
    .get instance int32 AccessorModifierMismatch::GetVirtual()
    .set instance void AccessorModifierMismatch::SetAbstract(int32)
  }

  .property instance int32 VirtualVirtual()
  {
    .get instance int32 AccessorModifierMismatch::GetVirtual()
    .set instance void AccessorModifierMismatch::SetVirtual(int32)
  }

  .property instance int32 VirtualOverride()
  {
    .get instance int32 AccessorModifierMismatch::GetVirtual()
    .set instance void AccessorModifierMismatch::SetOverride(int32)
  }

  .property instance int32 VirtualSealed()
  {
    .get instance int32 AccessorModifierMismatch::GetVirtual()
    .set instance void AccessorModifierMismatch::SetSealed(int32)
  }

  ////

  .property instance int32 OverrideNone()
  {
    .get instance int32 AccessorModifierMismatch::GetOverride()
    .set instance void AccessorModifierMismatch::SetNone(int32)
  }

  .property instance int32 OverrideAbstract()
  {
    .get instance int32 AccessorModifierMismatch::GetOverride()
    .set instance void AccessorModifierMismatch::SetAbstract(int32)
  }

  .property instance int32 OverrideVirtual()
  {
    .get instance int32 AccessorModifierMismatch::GetOverride()
    .set instance void AccessorModifierMismatch::SetVirtual(int32)
  }

  .property instance int32 OverrideOverride()
  {
    .get instance int32 AccessorModifierMismatch::GetOverride()
    .set instance void AccessorModifierMismatch::SetOverride(int32)
  }

  .property instance int32 OverrideSealed()
  {
    .get instance int32 AccessorModifierMismatch::GetOverride()
    .set instance void AccessorModifierMismatch::SetSealed(int32)
  }

  ////

  .property instance int32 SealedNone()
  {
    .get instance int32 AccessorModifierMismatch::GetSealed()
    .set instance void AccessorModifierMismatch::SetNone(int32)
  }

  .property instance int32 SealedAbstract()
  {
    .get instance int32 AccessorModifierMismatch::GetSealed()
    .set instance void AccessorModifierMismatch::SetAbstract(int32)
  }

  .property instance int32 SealedVirtual()
  {
    .get instance int32 AccessorModifierMismatch::GetSealed()
    .set instance void AccessorModifierMismatch::SetVirtual(int32)
  }

  .property instance int32 SealedOverride()
  {
    .get instance int32 AccessorModifierMismatch::GetSealed()
    .set instance void AccessorModifierMismatch::SetOverride(int32)
  }

  .property instance int32 SealedSealed()
  {
    .get instance int32 AccessorModifierMismatch::GetSealed()
    .set instance void AccessorModifierMismatch::SetSealed(int32)
  }

} // end of class AccessorModifierMismatch