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


**Things to follow-up on:**
- Tests MethodImplementation_02, MethodImplementation_03 and MethodImplementation_04 in src/Compilers/CSharp/Test/Symbol/Symbols/DefaultInterfaceImplementationTests.cs reflect the current decision of LDM to continue reporting errors in scenarios like those. Need to get another confirmation regarding that, as well as address concerns expressed by @gafter around the wording used by the errors.
- None of the tests are running PEVerify or verify expected behavior by running the compiled code because, at the moment, we cannot target runtime that supports the feature. The tests should be adjusted once they are able to target a runtime with required support.
- The feature is currently targeting 7.1 language version, that might not be the final plan.
