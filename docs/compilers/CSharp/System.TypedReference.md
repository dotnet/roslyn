System.TypedReference
=====================

[This is a placeholder. We need some more documentation here]

This is an old email conversation that gives some context about the interop support in the C# compiler. Ironically, the conversation suggests that we'll never document it!

-------------------
Subject: RE: error CS0610: Field or property cannot be of type 'System.TypedReference'
From: Eric Lippert
To: Aleksey Tsingauz; Neal Gafter; Roslyn Compiler Dev Team
Sent: Monday, January 24, 2011 9:42 AM

Basically what’s going on here is we have some undocumented features which enable you to pass around a reference to a **variable** without knowing the type of the variable at compile time. The reason why we have this feature is to enable C-style “varargs” in the CLR; you might have a method that takes an unspecified number of arguments and some of those arguments might be references to variables. 

Because a `TypedReference` can contain the address of a stack-allocated variable, you’re not allowed to store them in fields, same as you’re not allowed to make a field of ref type. That way we know we’re never storing a reference to a “dead” stack variable.

We have a bunch of goo in the native compiler to make sure that typed references (and a few other similarly magical types) are not used incorrectly; we’ll have to do the same thing in Roslyn at some point.

None of this stuff is well documented. We have four undocumented language keywords that allow you to manipulate typed references; we have no intention as far as I know of ever documenting them. They are only there for rare situations where C# code needs to interoperate with a C-style method.

Some various articles on these features that might give you more background if you’re interested:

http://www.eggheadcafe.com/articles/20030114.asp

http://stackoverflow.com/questions/4764573/why-is-typedreference-behind-the-scenes-its-so-fast-and-safe-almost-magical

http://stackoverflow.com/questions/1711393/practical-uses-of-typedreference

http://stackoverflow.com/questions/2064509/c-type-parameters-specification

http://stackoverflow.com/questions/4046397/generic-variadic-parameters

http://bartdesmet.net/blogs/bart/archive/2006/09/28/4473.aspx

Cheers,
Eric

---------------------
From: Aleksey Tsingauz 
Sent: Sunday, January 23, 2011 11:00 PM
To: Neal Gafter; Roslyn Compiler Dev Team
Subject: RE: error CS0610: Field or property cannot be of type 'System.TypedReference'
 
I believe it is about ECMA-335 §8.2.1.1 Managed pointers and related types:
 
A **managed pointer** (§12.1.1.2), or **byref** (§8.6.1.3, §12.4.1.5.2), can point to a local variable, parameter, field of a compound type, or element of an array. However, when a call crosses a remoting boundary (see §12.5) a conforming implementation can use a copy-in/copy-out mechanism instead of a managed pointer. Thus programs shall not rely on the aliasing behavior of true pointers. Managed pointer types are only allowed for local variable (§8.6.1.3) and parameter signatures (§8.6.1.4); they cannot be used for field signatures (§8.6.1.2), as the element type of an array (§8.9.1), and boxing a value of managed pointer type is disallowed (§8.2.4). Using a managed pointer type for the return type of methods (§8.6.1.5) is not verifiable (§8.8). 

[Rationale: For performance reasons items on the GC heap may not contain references to the interior of other GC objects, this motivates the restrictions on fields and boxing. Further returning a managed pointer which references a local or parameter variable may cause the reference to outlive the variable, hence it is not verifiable . end rationale]

There are three value types in the Base Class Library (see Partition IV Library): `System.TypedReference`, `System.RuntimeArgumentHandle`, and `System.ArgIterator`; which are treated specially by the CLI. 
The value type `System.TypedReference`, or *typed reference* or *typedref* , (§8.2.2, §8.6.1.3, §12.4.1.5.3) contains both a managed pointer to a location and a runtime representation of the type that can be stored at that location. Typed references have the same restrictions as byrefs. Typed references are created by the CIL instruction `mkrefany` (see Partition III).

The value types `System.RuntimeArgumentHandle` and `System.ArgIterator` (see Partition IV and CIL instruction `arglist` in Partition III), contain pointers into the VES stack. They can be used for local variable and parameter signatures. The use of these types for fields, method return types, the element type of an array, or in boxing is not verifiable (§8.8). These two types are referred to as *byref-like* types.

----------------
From: Neal Gafter 
Sent: Sunday, January 23, 2011 8:37 PM
To: Roslyn Compiler Dev Team
Cc: Neal Gafter
Subject: error CS0610: Field or property cannot be of type 'System.TypedReference'
 
What is this error all about?  Where is it documented?
