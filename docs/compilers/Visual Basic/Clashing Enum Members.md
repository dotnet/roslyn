Clashing Enum Members
=====================

As reported in [#2909](https://github.com/dotnet/roslyn/issues/2909), it is possible to reference an enumeration type from VB that contains more than one enumeration member with the same name. For example, a library author might release version 1 of his API in C# with this type

```cs
enum Something
{
    Something,
    Datetime,
    SomethingElse,
}
```

and then realize later that the name Datetime is misspelled (it should be PascalCase, with `Time` capitalized). So in version 2 the type is changed

```cs
enum SomeEnum
{
    Something,
    DateTime,
    [Obsolete] Datetime = DateTime,
    SomethingElse,
}
```

A C# client that used the old name of the enumeration member would receive an obsolete warning from the compiler encouraging the use of the new name instead of the old one, but would otherwise be able to compile and run the code unchanged.

A VB client that used `SomeEnum.Datetime` would have received no warning from previous versions of the VB compiler.

```vb
    Dim v = SomeEnum.Datetime
```

Although this program is technically in error (the name `Datetime` is ambiguous between two enum members), the Dev12 VB compiler would select the *lexically first* enumeration constant of a given (case-insensitive) name when looking up an enumeration constant within an enumeration type. Since that member does not have the Obsolete attribute in this example, the VB code would continue to compile and run without a problem.

The Roslyn compilers reproduce this bug - but only when the conflicting enumeration constants have the same underlying value, as in the example above - so that existing code that takes advantage of this will not be broken. See, for example,
- https://bugs.mysql.com/bug.php?id=37406
- http://lists.mysql.com/commits/48012
