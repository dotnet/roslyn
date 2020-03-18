# Supported Edits in Edit & Continue (EnC)
Edit & Continue lets you modify/add to your source code in break-mode while debugging without ever having to restart your debugging session. 

**Definitions**
* [Variable Capturing](http://blogs.msdn.com/b/matt/archive/2008/03/01/understanding-variable-capturing-in-c.aspx) is the mechanism in which the lambda/delegate which is defined inline is able to hold on to any variables within its lexical scope
* **Scope** is the region of program text within which it is possible to refer to the entity declared by the name without qualification of the name
* **Debug statement** is a span of instructions delimited by subsequent sequence points. Usually a debug statement corresponds to a language statement, but it might correspond to just a part of a language statement (e.g. an opening brace of a block statement), an expression (e.g. lambda body) or other contiguous syntax (base constructor call).
* **Internal active statement** is a debug statement that contains a return address of a stack frame.
* **Leaf active debug statement** is a debug statement that contains an IP (instruction pointer) of any thread.


### Supported Edits
| Edit operation | Additional Info |
| ------------------- |--------------------|
| Add methods, fields, constructors, properties, events, indexers, field and property initializers, nested types and top-level types (including delegates, enums, interfaces, abstract and generic types, and anonymous types) to an existing type  | The existing type cannot be a generic or an interface. <br/> <br/> Adding or modifying [enum members](https://msdn.microsoft.com/en-us/library/sbbt4032.aspx) within an existing enum is not supported. |
| Add and modify iterators  | Changing a regular method to an iterator method *is* supported |
| Add async/await expressions  |  Adding an await expression into an existing async method is not supported. <br/><br/> Adding an await expression around an active statement is not supported. <br/><br/> Changing a regular method to async *is* supported. |
| Modify async/await expressions  |  Modifying await expressions wrapped inside other expressions (e.g. ```G(await F());```) is not supported |
| Add and modify operations with dynamic objects | - |
| Add and modify C# 6.0 language features like string interpolation and null-conditional operators | - |
| Add lambda expressions | Lambda expressions can only be added if they are static, access the “this” reference that has already been captured, or access captured variables from a single scope |
| Modify lambda expressions | The following rules guarantee that the structure of the emitted closure tree will not change--thus ensuring that lambdas in the new body are mapped to the corresponding generated CLR methods that implemented their previous versions: <ul><li>Lambda signatures cannot be modified (this includes names, types, ref-ness of parameters, and return types)</li><li>The set of variables captured by the lambda expression cannot be modified (a variable that has not been captured before cannot be captured after modification and vice versa)</li><li>The scope of captured variables cannot be modified</li><li>The set of captured variables accessed by the lambda expression cannot be modified</li></ul> |
| Add LINQ expressions | LINQ expressions contain implicitly-declared anonymous functions. This means the edit rules for lambdas and LINQ will be the same. |
| Modify LINQ expressions | LINQ expressions contain implicitly-declared anonymous functions. This means the edit rules for lambdas and LINQ will be the same. |
| Modifying async lambda and LINQ expressions in combination | You can edit various nested expressions provided that they otherwise satisfy the EnC rules | 

### Not Supported Edits
| Edit operation      | Additional Info |
| ------------------- |-----------------|
| Modify [method signatures](https://msdn.microsoft.com/en-us/library/ms173114.aspx) | - |
| Add or modify [generics](https://msdn.microsoft.com/en-us/library/512aeb7t.aspx) | - |
| Modify interfaces | - |
| Add a method body such that an abstract method becomes non-abstract | - |
| Add new abstract, virtual, or override member to a type | You CAN add a non-abstract member to an abstract type |
| Add [destructor](https://msdn.microsoft.com/en-us/library/66x5fx1b.aspx) to an existing type |  - |
| Modify a type parameter, base type, delegate type, or return type of an event/property/operator/indexer | - |
| Modify a catch-block if it contains an active statement (leaf or internal) | - |
| Modify a try-catch-finally block if the finally clause contains an active statement | - |
| Renaming of any kind | - |
| Delete members, types, namespaces | - |
| Delete entire method bodies | Not supported because deleting an entire method body would make the method “abstract”—which is not currently supported |
| Add using statements | - | 
| Add a namespace | - |
| Edit a member referencing an embedded interop type | - |
| Edit a member with On Error or Resume statements | Specific to Visual Basic |
| Edit a member containing an Aggregate, Group By, Simple Join, or Group Join LINQ query clause | Specific to Visual Basic |
| Edit an async method/lambda in a project that doesn't define or reference AsyncStateMachineAttribute type (e.g. projects targeting .NET Framework 4.0 and lower) | - |
| Edit an iterator method/lambda in a project that doesn't define or reference IteratorStateMachineAttribute type (e.g. projects targeting .NET Framework 4.0 and lower) | - |

### App Model EnC Support

| Support EnC | Do Not Support EnC | 
| ------------------ |------------------------------| 
| <ul><li>Apps that support the 4.6 desktop version of the CLR for both x86 and x64 (e.g., Console, WPF, Windows 8.1 apps)</li><li>UWP in Windows 10</li><ul> | <ul><li>ASP.NET 5 apps</li><li>Silverlight 5</li><li>Windows Phone 8.1</li><li>Windows Phone 10</li><li>Windows Phone emulator scenarios</li><li>Windows Store 8.1</li></ul>|