# Supported Edits in Edit & Continue (EnC)

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
| Add any member to an existing type is supported if it is added within the same edit | - | 
| Add and modify iterators  | - |
| Add and modify async/await expressions  |  Modifying await expressions wrapped inside other expressions (e.g. ```G(await F());```) is not supported |
| Add and modify operations with dynamic objects | - |
| Add and modify C# 6.0 language features like string interpolation and null-conditional operators | - |
| Add lambda expressions | Lambda expressions can only be added if they are static and access the “this” reference that has already been captured or access captured variables from a single scope |
| Modify lambda expressions | The following rules guarantee that the structure of the emitted closure tree will not change--thus ensuring that lambdas in the new body are mapped to the corresponding generated CLR methods that implemented their previous versions: <ul><li>Lambda signatures cannot be modified (this includes names, types, ref-ness of parameters, and return types)</li><li>The set of variables captured by the lambda expression cannot be modified (a variable that has not been captured before cannot be captured after modification and vice versa)</li><li>The scope of captured variables cannot be modified</li><li>The set of captured variables accessed by the lambda expression cannot be modified</li></ul> |
| Add LINQ expressions | LINQ expressions contain implicitly-declared anonymous functions. This means the edit rules for lambdas and LINQ will be the same. |
| Modify lambda expressions | LINQ expressions contain implicitly-declared anonymous functions. This means the edit rules for lambdas and LINQ will be the same. |

### Not Supported Edits
| Edit operation | Planned? | Additional Info |
| ------------------- |--------------| --------------------|
| Modify [method signatures](https://msdn.microsoft.com/en-us/library/ms173114.aspx) | - | - |
| Add or modify [generics](https://msdn.microsoft.com/en-us/library/512aeb7t.aspx) | - | - |
| Add or modify interfaces | - | - |
| Add a method body such that an abstract method becomes non-abstract | - | - |
| Add new abstract or override member to abstract type | - | You CAN add a non-abstract member to an abstract type |
| Add [destructors](https://msdn.microsoft.com/en-us/library/66x5fx1b.aspx) | - | - |
| Modify a type parameter, base type, delegate type, or return type of an event/property/operator/indexer | - | - |
| Modify a catch-block if it contains an active statement (leaf or internal) | - | - |
| Modify a try-catch-finally block if the finally clause contains an active statement | - | - |
| Renaming of any kind | - | - |
| Delete members or types | - | - |
| Delete entire method bodies | - | Not supported because deleting an entire method body would make the method “abstract”—which is not currently supported |


### App Model EnC Support

| Support EnC | Do Not Support EnC | 
| ------------------ |------------------------------| 
| <ul><li>Apps that support the 4.6 desktop version of the CLR for both x86 and x64 (e.g., Console, WPF, Windows 8.1 apps)</li><li>UWP in Windows 10</li><ul> | <ul><li>ASP.NET 5 apps</li><li>Silverlight 5</li><li>Windows Phone 8.1</li><li>Windows Phone 10</li><li>Windows Phone emulator scenarios</li><li>Windows Store 8.1</li></ul>|