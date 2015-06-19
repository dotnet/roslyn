Internal Accessibility
======================

The C# language specification is very vague when it describes internal visibility.  We map "the program" to the assembly being compiled; `internal` accessibility is not extended to assemblies that import the compilation unless specifically granted that access by the use of `InternalsVisibleToAttribute`.

There are many places where this affects the semantics of the language without supporting language from the C# language specification.  For example, if there is a type name in the current assembly that conflicts with the same name from another assembly, we allow the reference to the name and treat it as a reference to the name from the current assembly. Furthermore, the rule that a using alias may not conflict with a type of the same name is not enforced if the conflicting type comes from another assembly.
