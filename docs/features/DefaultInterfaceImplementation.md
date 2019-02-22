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

- Using **partial**, **public**, **internal**, **private**, **static**, **virtual**, **sealed**, **abstract**, **extern** and **async** modifiers with interface methods.

- Using **public**, **internal**, **private**, **static**, **virtual**, **sealed**, **abstract** and **extern** modifiers with interface properties.

- Using **public**, **internal**, **private**, **virtual**, **sealed**, **abstract** and **extern** modifiers with interface indexers.

- Using **internal** and **private** modifiers with interface property/indexer accessors.

- Using **public**, **internal**, **private**, **static**, **virtual**, **sealed**, **abstract** and **extern** modifiers with interface events.

- Declaring types within interfaces. **Protected** and **protected internal** accessibility is not supported.

- Implementing interface methods in derived interfaces by using explicit implementation syntax, accessibility matches accessibility of the implemented member, allowed modifiers: **extern** and **async**.

- Implementing interface properties and indexers in derived interfaces by using explicit implementation syntax, accessibility matches accessibility of the implemented member, allowed modifiers: **extern**.

- Implementing interface events in derived interfaces by using explicit implementation syntax, accessibility matches accessibility of the implemented member, no allowed modifiers.

- Declaring static fields, auto-properties and field-like events (**protected** modifier is not allowed).

- Declaring operators ```+ - ! ~ ++ -- true false * / % & | ^ << >> > < >= <=``` in interfaces.

- Base access
The following forms of base-access are added (https://github.com/dotnet/csharplang/blob/master/meetings/2018/LDM-2018-11-14.md)
```
    base ( <type-syntax> )  .   identifier
    base ( <type-syntax> )   [   argument-list   ]
```

The type-syntax can refer to one of the base classes of the containing type, or one of the interfaces implemented or inherited by the containing type.

When the type-syntax refers to a class, the member lookup rules, overload resolution rules and IL emit match the rules for the 7.3 supported
forms of base-access. The only difference is the specified base class is used instead of the immediate base class, the most derived implementation is
found in that class, etc.

When the type-syntax refers to an interface: 
1. The member lookup is performed in that interface, using the regular member lookup rules within interfaces, with an exception that members of
   System.Object do not participate in the lookup.
2. Regular overload resolution is performed for members returned by the lookup process, virtual or abstract members are not replaced with most
   derived implementations at this step (unlike the case when the type-syntax refers to a class). If result of overload resolution is a virtual
   or abstract method, it must have a most specific implementation within the interface type, an error is reported otherwise. The most specific
   implementation must be accessible at the call site.
3. During IL emit a **call** (non-virtual call) instruction is used to invoke methods. If result of overload resolution on the previous step is
   virtual or abstract method, the most specific implementation of the method is used as the target for the instruction.
   
Given the accessibility requirements for the most specific interface implementation, accessibility of implementations provided in derived interfaces
is changed to match accessibility of implemented members, prior to this change the accessibility was private.

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
