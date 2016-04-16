Indexed Property Access
=======================

[This is a placeholder. We need some more documentation here]

An indexed property declared in VB can be used in C#.  We should describe exactly what we do.

Actually it seems that it won't be usable with indexed props syntax unless you put [ComVisible] on it in VB.

If a type has `[System.Runtime.InteropServices.ComImportAttribute]` attribute and has one or more parameterized properties, those properties can be referenced from C# (provided they have appropriate visibility etc). The motivating scenario for this feature is support for COM libraries that often contain such properties. Before this feature, it was only possible to call accessors of such properties by their method names (P_set, P_get). But it’s also possible to declare such properties in VB, that we often use for testing this feature (note that when you specify `[ComImport]` attribute, you should also specify `[System.Runtime.InteropServices.GuidAttribute("…")]` attribute with a valid `GUID` as its argument, otherwise you get a compile-time error).

Note that it's possible to declare multiple parameterized properties with different names, and it's also possible to declare multiple overloaded signatures for each name. In the latter case, properties with the same name form a property group (it's similar to the method group concept explained in the C# spec). To determine a particular property to invoke C# employs overload resolution, that proceeds by exactly same rules as overload resolution in indexer invocations.

