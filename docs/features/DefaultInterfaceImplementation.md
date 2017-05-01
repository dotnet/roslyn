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

**Open issues and work items** are tracked in https://github.com/dotnet/roslyn/issues/17952.
