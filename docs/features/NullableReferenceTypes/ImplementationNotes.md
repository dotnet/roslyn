Implicit type declarations:
-	Properties of anonymous types are considered nullable if their type is a reference type.
-	Element type of an array created by implicitly typed array creation expression is considered nullable if the type is a reference type.
-	Type of a var local is considered nullable if the type is a reference type.

For the purpose of flow analysis, result of expression can have three states:
-	Assumed to have not null value.
-	Possibly has null value.
-	Unknown nullability (no diagnostics is derived from this state). For example, this state can arise from usage of an API that hasn’t been annotated yet.  

Tracking state of 
-	Locals
-	Parameters
-	Fields of structures
-	Anonymous Type properties

The goal is to track state of the same entities, which are tracked by Definite Assignment, plus state of properties of Anonymous Types.  

Warn about possible null reference on 
-	An assignment to a local statically typed as not-nullable.
-	An assignment to a parameter statically typed as not-nullable.
-	An assignment to a field statically typed as not-nullable.
-	An assignment to a property statically typed as not-nullable.
-	An assignment to an indexer statically typed as not-nullable.
-	An assignment to an array element statically typed as not-nullable.
-	A member initializer for a member statically typed as not-nullable.
-	An argument passed to a parameter statically typed as not-nullable.
-	An argument (local, parameter, field, array element) statically typed as not-nullable passed as ref/out to a parameter statically typed as nullable.
-	A return expression if return type of the method is statically typed as not-nullable.
-	A receiver of a method/field/property/indexer access.
-	An array expression of an array access.

A local is considered to have a not null value if the local is statically typed as not-nullable, or the last value assigned to the field was not null.

A parameter is considered to have a not null value if the parameter is statically typed as not-nullable, or the last value assigned to the parameter was not null and the parameter is not ref/out.

A field is considered to have a not null value if the field is statically typed as not-nullable, or the field is tracked by the flow analysis and the last value assigned to the field was not null.

A property is considered to have a not null value if the property is statically typed as not-nullable, or the property is tracked by the flow analysis (readonly auto-property of a structure in its constructor, or a property of an Anonymous Type) and the last value assigned to the property was not null.

Result of an indexer access is considered to be a not null value if the indexer is statically typed as not-nullable.

Result of a method call is considered to be a not null value if its return type is statically typed as not-nullable.

Passing a tracked structure or a tracked Anonymous Type instance by reference invalidates accumulated tracking information for their members.

When a built-in or a user-defined operator ==/!= is used to compare an expression to a null value. 
For the purpose of flow analysis, a trackable expression is considered to be not null when == evaluates to false, or != evaluates to true. ~~If, according to flow analysis, the expression has a not null value before the operator is evaluated, a warning is reported that result of the comparison is always false (==), or always true (!=).~~ 

When expression is used as a left operand of a Null Coalescing Operator (??). 
~~For the purpose of flow analysis, a trackable expression is considered to be null before the right operand is evaluated.~~ ~~If, according to flow analysis, the expression has a not null value before the operator is evaluated, a warning is reported that the operand is never null.~~ Result of the operator is considered to be not null for the purpose of the flow analysis if either operand is considered to be not null.

When expression is used as a receiver of a conditional access (?./?[]). 
For the purpose of flow analysis, a trackable expression is considered to be not null before the access is evaluated. ~~If, according to flow analysis, the expression has a not null value before the receiver is evaluated, a warning is reported that the receiver is never null.~~ Result of the operator is considered to be not null for the purpose of the flow analysis only if both, the receiver and the access are considered to be not null.

Right now, some overriding cases can be ambiguous because constraints are not specified on an overriding method, but rather inherited from the overridden method. For example:
```
class A
{
    public virtual void M1<T>(T? x) where T : struct 
    { 
    }

    public virtual void M1<T>(T? x) where T : class 
    { 
    }
}

class B : A
{
    public override void M1<T>(T? x)
    {
    }
} 
```

Current implementation doesn't detect this ambiguity case and simply grabs the first applicable candidate for overriding, always in in declaration order, I assume.

Array type syntax is extended as follows to allow nullable modifiers:
-	string?[] x1; // not-nullable one-dimensional array of nullable strings
-	string?[]? x2; // nullable one-dimensional array of nullable strings
-	string[]? X3; // nullable one-dimensional array of not-nullable strings
-	string?[][,] x4; // not-nullable one-dimensional array of not-nullable two-dimensional arrays of nullable strings
-	string?[][,]? X5; // not-nullable one-dimensional array of nullable two-dimensional arrays of nullable strings
-	string?[]?[,] x6; // nullable one-dimensional array of not-nullable two-dimensional arrays of nullable strings

Warnings are reported when there is a signature mismatch with respect to nullability of reference types during overriding, 
interface implementing, or partial method implementing. 


**NullableAttribute**
NullableAttribute is applied to a module if it utilizes Nullable Reference Types feature.
NullableAttribute is applied to other targets in the module to point to specific nullable reference types in type references. 
The attribute is applied in the same fashion as DynamicAttribute, with the following exceptions:
- For types of events, it is applied to event declarations (not just to parameters of accessors).
- Types used as custom modifiers, do not have dedicated transform flags.

Here is the definition of the NullableAttribute required for the successful compilation:
```
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Event | // The type of the event is nullable, or has a nullable reference type as one of its constituents
                    AttributeTargets.Field | // The type of the field is a nullable reference type, or has a nullable reference type as one of its constituents
                    AttributeTargets.GenericParameter | // The generic parameter is a nullable reference type
                    AttributeTargets.Module | // Nullable reference types in this module are annotated by means of NullableAttribute applied to other targets in it
                    AttributeTargets.Parameter | // The type of the parameter is a nullable reference type, or has a nullable reference type as one of its constituents
                    AttributeTargets.ReturnValue, // The return type is a nullable reference type, or has a nullable reference type as one of its constituents
                   AllowMultiple = false)]
    public class NullableAttribute : Attribute
    {
        public NullableAttribute() { }
        public NullableAttribute(bool[] transformFlags)
        {
        }
    }
}
```


**Opting in and opting out of nullability warnings**

It is possible to suppress all nulability warnings originating from declarations in certain referenced 
assembly by applying the following attribute:
```
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Opt out of nullability warnings that could originate from definitions in the given assembly. 
    /// The attribute is not preserved in metadata and ignored if present in metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Module, AllowMultiple = true)]
    class NullableOptOutForAssemblyAttribute : Attribute
    {
        /// <param name=""assemblyName"">An assembly name - a simple name plus its PublicKey, if any.""/></param>
        public NullableOptOutForAssemblyAttribute(string assemblyName) { }
    }
}
``` 

It is possible to apply the following attribute to a declaration itself in order to opt in or opt out all consumers 
from nullability warnings originating from the declaration. 
The attribute can be applied to a module, type, method, event, field or property. The closest attribute application wins. 
If nullable reference types feature is enabled, the warnings are opted into on the module level by default, i.e.
explicit attribute application is not needed in this case.
When a method definition is opted out of the warnings, nullablility warnings in its method body are also suppressed.

```
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Opt-out or opt into nullability warnings that could originate from source code and definition(s) ...
    /// </summary>
    [AttributeUsage(AttributeTargets.Module | // in this module. If nullable reference types feature is enabled, the warnings are opted into on the module level by default
                    AttributeTargets.Class | // in this class
                    AttributeTargets.Constructor | // of this constructor
                    AttributeTargets.Delegate | // of this delegate
                    AttributeTargets.Event | // of this event
                    AttributeTargets.Field | // of this field
                    AttributeTargets.Interface | // in this interface
                    AttributeTargets.Method | // of this method
                    AttributeTargets.Property | // of this property
                    AttributeTargets.Struct, // in this structure
                    AllowMultiple = false)]
    class NullableOptOutAttribute : Attribute
    {
        public NullableOptOutAttribute(bool flag = true) { }
    }
}
```

