// ilasm /dll InvalidDynamicAttributeArgs.il

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                      
  .ver 4:0:0:0
}
.assembly extern System.Core
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )              
  .ver 4:0:0:0
}
.assembly C
{
  .ver 0:0:0:0
}

.class public C extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } 

  // F1 - no value, 2 required

  .field public class [mscorlib]System.Collections.Generic.List`1<object> F1
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 
  
  // F2 - no value, 1 required
  
  .field public object F2
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 
  
  // F3 - 2 values { false, true }, 3 required 
  
  .field public class [mscorlib]System.Collections.Generic.Dictionary`2<object, object> F3
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 01 00 00 )  
  
  // F4 - no true value { false, false }

  .field public class [mscorlib]System.Collections.Generic.List`1<object> F4
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 00 00 00 ) 

  // F5 - true value for non-object { false, false, true }

  .field public class [mscorlib]System.Collections.Generic.Dictionary`2<object, bool> F5
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 00 00 01 00 00 ) 

  // F6 - true for modified type { false, true }

  .field public class [mscorlib]System.Collections.Generic.List`1<object> modopt([mscorlib]System.Runtime.CompilerServices.IsConst) F6
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 

  // M1

  .method public hidebysig instance class [mscorlib]System.Collections.Generic.List`1<object> M1()
  {
    // No bool values.
    .param [0]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 
    
	ldnull
    ret
  }

  // M2

  .method public hidebysig instance class [mscorlib]System.Collections.Generic.List`1<object> M2()
  {
    // No bool values.
    .param [0]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 
    
	ldnull
    ret
  }

  // P1

  .method public hidebysig specialname instance object get_P1()
  {
    // No bool values.
    .param [0]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 

	ldnull
    ret
  }

  .method public hidebysig specialname instance void set_P1(object 'value')
  {
    // No bool values.
    .param [1]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 

    ret
  } 

  .property instance object P1()
  {
    // No bool values.
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 

    .get instance object C::get_P1()
    .set instance void C::set_P1(object)
  } 

  // P2

  .method public hidebysig specialname instance class [mscorlib]System.Collections.Generic.List`1<object> get_P2()
  {
    // No bool values.
  
    .param [0]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 

	ldnull
    ret
  }

  .method public hidebysig specialname instance void set_P2(class [mscorlib]System.Collections.Generic.List`1<object> 'value')
  {
    // No bool values.

	.param [1]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 
    ret
  } 

  .property instance class [mscorlib]System.Collections.Generic.List`1<object> P2()
  {
    // No bool values.

    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance class [mscorlib]System.Collections.Generic.List`1<object> C::get_P2()
    .set instance void C::set_P2(class [mscorlib]System.Collections.Generic.List`1<object>)
  } 
} 