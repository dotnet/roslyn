Default Interface Implementation
=========================

The *Default Interface Implementation* feature enables a default implementation of an interface member to be provided as part of the interface declaration. 

Here is a link to the proposal https://github.com/dotnet/csharplang/blob/master/proposals/default-interface-methods.md. 

**What is supported:**
- Supplying an implementation along with declaration of a regular interface method and recognizing that implementation as default implementation for the method when a type implements the interface. 
Here is an example:
```
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine("Default implementation of M1 is called!!!");
    }
}

class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M1();
    }
}
```

- Supplying an implementation along with declaration of a property or an indexer and recognizing that implementation as default implementation for them when a type implements the interface. 

- Supplying an implementation along with declaration of an event and recognizing that implementation as default implementation for the event when a type implements the interface. 

- Using **public**, **internal**, **private**, **static**, **virtual**, **sealed**, **abstract**, **extern** and **async** modifiers with interface methods.

- Using **public**, **internal**, **private**, **static**, **virtual**, **sealed**, **abstract** and **extern** modifiers with interface properties.

- Using **public**, **internal**, **private**, **virtual**, **sealed**, **abstract** and **extern** modifiers with interface indexers.

- Using **internal** and **private** modifiers with interface property/indexer accessors.

- Using **public**, **internal**, **private**, **static**, **virtual**, **sealed**, **abstract** and **extern** modifiers with interface events.

- Declaring types within interfaces. **Protected** and **protected internal** accessibility is not supported.

- Implementing interface methods in derived interfaces by using explicit implementation syntax, accessibility is private, allowed modifiers: **extern** and **async**.

- Implementing interface properties and indexers in derived interfaces by using explicit implementation syntax, accessibility is private, allowed modifiers: **extern**.

- Implementing interface events in derived interfaces by using explicit implementation syntax, accessibility is private, no allowed modifiers.


**Open issues and work items** are tracked in https://github.com/dotnet/roslyn/issues/17952.

**Parts of ECMA-335 that become obsolete/inaccurate/incomplete**
>I.8.5.3.2 Accessibility of members and nested types
Members (other than nested types) defined by an interface shall be public.
I.8.9.4 Interface type definition
Similarly, an interface type definition shall not provide implementations for any methods on the
values of its type.
Interfaces can have static or virtual methods, but shall not have instance methods.
However, since accessibility attributes are relative to the implementing type rather
than the interface itself, all members of an interface shall have public accessibility, ...
I.8.11.1 Method definitions
All non-static methods of an interface definition are abstract.
All non-static method definitions in interface definitions shall be virtual methods.
II.10.4 Method implementat ion requirements
II.12 Semantics of interfaces
Interfaces can have static fields and methods, but they shall not have instance fields or 
methods. Interfaces can define virtual methods, but only if those methods are abstract 
(see Partition I and §II.15.4.2.4).
II.12.2 Implement ing virtual methods on interfaces
If the class defines any public virtual methods whose name and signature
match a virtual method on the interface, then add these to the list for that
method, in type declaration order (see above).
If there are any public virtual methods available on this class (directly or inherited)
having the same name and signature as the interface method, and whose generic type
parameters do not exactly match any methods in the existing list for that interface
method for this class or any class in its inheritance chain, then add them (in type
declaration order) to the list for the corresponding methods on the interface.
II.15.2 Static, instance, and virtual methods
It follows that instance methods shall only be defined in classes or value types, 
but not in interfaces or outside of a type (i.e., globally).
II.22.27 MethodImpl : 0x19
The method indexed by MethodBody shall be a member of Class or some base class
of Class (MethodImpls do not allow compilers to ‘hook’ arbitrary method bodies)
II.22.37 TypeDef : 0x02
All of the methods owned by an Interface (Flags.Interface = 1) shall be abstract
(Flags.Abstract = 1)
IV.6 Implementation-specific modifications to the system libraries
Interfaces and virtual methods shall not be added to an existing interface.
