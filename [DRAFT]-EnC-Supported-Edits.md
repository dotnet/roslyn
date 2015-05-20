# Supported Edits in Edit & Continue (EnC)

**Definitions**
* [Variable Capturing](http://blogs.msdn.com/b/matt/archive/2008/03/01/understanding-variable-capturing-in-c.aspx): the mechanism in which the lambda/delegate which is defined inline is able to hold on to any variables within its lexical scope
* **Scope**: the region of program text within which it is possible to refer to the entity declared by the name without qualification of the name
* **Debug statement** is a span of instructions delimited by subsequent sequence points. Usually a debug statement corresponds to a language statement, but it might correspond to just a part of a language statement (e.g. an opening brace of a block statement), an expression (e.g. lambda body) or other contiguous syntax (base constructor call).
* **Internal active statement** is a debug statement that contains a return address of a stack frame.
* **Leaf active debug statement** is a debug statement that contains an IP (instruction pointer) of any thread.


### Supported Edits
| Edit operation | Status  | Additional Info |
| ------------------- |-----------| --------------------|
| Add methods, fields, constructors, properties, events, indexers, field and property initializers, nested types and top-level types (including delegates, enums, interfaces, abstract and generic types, and anonymous types) to an existing type  | Supported | The existing type cannot be a generic or an interface. <br/> <br/> Adding or modifying [enum members](https://msdn.microsoft.com/en-us/library/sbbt4032.aspx) within an existing enum is not supported. |
| Add any member to an existing type is supported if it is added within the same edit     | Supported |  - | 
| Add and modify iterators  | Supported   |  - |
| Add and modify async/await expressions  | Supported |  -  | Modifying await expressions wrapped inside other expressions (e.g. ```G(await F());```) |
| Add and modify operations with dynamic objects | Supported   | - |
| Add and modify C# 6.0 language features like string interpolation and null-conditional operators | Supported   | - |
| Add lambda expressions | Partially supported | Lambda expressions can only be added if they are static and access the “this” reference that has already been captured or access captured variables from a single scope |
| Modify lambda expressions | Partially supported | Lambda signatures cannot be modified (this includes names, types, ref-ness of parameters, and return types) <br/> <br/>The set of variables captured by the lambda expression cannot be modified (a variable that has not been captured before cannot be captured after modification and vice versa)  <br/> <br/> The scope of captured variables cannot be modified  <br/> <br/> The set of captured variables accessed by the lambda expression cannot be modified <br/> <br/> These rules guarantee that the structure of the emitted closure tree will not change--thus ensuring that lambdas in the new body are mapped to the corresponding generated CLR methods that implemented their previous versions. |
| Add LINQ expressions | Partially supported |  LINQ expressions contain implicitly-declared anonymous functions. This means the edit rules for lambdas and LINQ will be the same. |
| Modify lambda expressions | Partially supported |  LINQ expressions contain implicitly-declared anonymous functions. This means the edit rules for lambdas and LINQ will be the same. |

### Not Supported Edits
| Edit operation | Status | Planned? | Additional Info |
| ------------------- |-----------| --------------| --------------------|
| Modify [method signatures](https://msdn.microsoft.com/en-us/library/ms173114.aspx) | Not supported | - | - |
| Add or modify [generics](https://msdn.microsoft.com/en-us/library/512aeb7t.aspx) | Not supported | - | - |
| Add or modify interfaces | Not supported | - | - |
| Add a method body such that an abstract method becomes non-abstract | Not supported | - | - |
| Add new abstract or override member to abstract type | Not supported | - | You CAN add a non-abstract member to an abstract type |
| Add [destructors](https://msdn.microsoft.com/en-us/library/66x5fx1b.aspx) | Not supported | - | - |
| Modify a type parameter, base type, delegate type, or return type of an event/property/operator/indexer | Not supported | - | - |
| Modify a catch-block if it contains an active statement (leaf or internal) | Not supported | - | - |
| Modify a try-catch-finally block if the finally clause contains an active statement | Not supported | - | - |
| Renaming of any kind | Not supported | - | - |
| Delete members or types | Not supported | - | - |
| Delete entire method bodies | Not supported | - | Not supported because deleting an entire method body would make the method “abstract”—which is not currently supported |


### App Model EnC Support

| Support EnC | Do Not Support EnC | 
| ------------------ |------------------------------| 
| <ul><li>Apps that support the 4.6 desktop version of the CLR for both x86 and x64 (e.g., Console, WPF, Windows 8.1 apps)</li><li>UWP in Windows 10</li><ul> | <ul><li>ASP.NET 5 apps</li><li>Silverlight 5</li><li>Windows Phone 8.1</li><li>Windows Phone 10</li><li>Windows Phone emulator scenarios</li><li>Windows Store 8.1</li></ul>|