# Supported Edits in Edit & Continue (EnC) and Hot Reload on .NET 8
Hot Reload lets you modify/add to your source code while the application is running, either with debugger attached to the process (F5) or not attached (Ctrl+F5).
Edit & Continue lets you modify/add to your source code in break-mode while debugging without having to restart your debugging session.

This document captures the current state. Potential future improvements in this area are tracked by https://github.com/dotnet/roslyn/issues/49001.

**Definitions**
* [Variable Capturing](http://blogs.msdn.com/b/matt/archive/2008/03/01/understanding-variable-capturing-in-c.aspx) is the mechanism in which the lambda/delegate which is defined inline is able to hold on to any variables within its lexical scope
* **Scope** is the region of program text within which it is possible to refer to the entity declared by the name without qualification of the name
* **Debug statement** is a span of instructions delimited by subsequent sequence points. Usually a debug statement corresponds to a language statement, but it might correspond to just a part of a language statement (e.g. an opening brace of a block statement), an expression (e.g. lambda body) or other contiguous syntax (base constructor call).
* **Internal active statement** is a debug statement that contains a return address of a stack frame.
* **Leaf active debug statement** is a debug statement that contains an IP (instruction pointer) of any thread.


### Supported Edits
| Edit operation | Additional Info |
| ------------------- |--------------------|
| Add methods, fields, constructors, properties, events, indexers, field and property initializers, nested types and top-level types (including delegates, enums, interfaces, abstract and generic types, and anonymous types) to an existing type  | The existing type cannot be an interface. <br/> <br/> Adding or modifying [enum members](https://msdn.microsoft.com/en-us/library/sbbt4032.aspx) within an existing enum is not supported. |
| Add and modify iterators, `yield` statements | Changing a regular method to an iterator method *is* supported |
| Add and modify async methods, `await` expressions | Modifying await expressions wrapped inside other expressions (e.g. ```G(await F());```) is not supported. <br/><br/> Changing a regular method to async *is* supported. |
| Add and modify operations with dynamic objects | - |
| Add lambda expressions | Lambda expressions can only be added if they are static, access the “this” reference that has already been captured, or access captured variables from a single scope |
| Modify generic code | Enabled in .NET 8 and Visual Studio 17.7 |
| Modify lambda expressions | The following rules guarantee that the structure of the emitted closure tree will not change--thus ensuring that lambdas in the new body are mapped to the corresponding generated CLR methods that implemented their previous versions: <ul><li>Lambda signatures cannot be modified (this includes names, types, ref-ness of parameters, and return types)</li><li>The set of variables captured by the lambda expression cannot be modified (a variable that has not been captured before cannot be captured after modification and vice versa)</li><li>The scope of captured variables cannot be modified</li><li>The set of captured variables accessed by the lambda expression cannot be modified</li></ul> |
| Add LINQ expressions | LINQ expressions contain implicitly-declared anonymous functions. This means the edit rules for lambdas and LINQ will be the same. |
| Modify LINQ expressions | LINQ expressions contain implicitly-declared anonymous functions. This means the edit rules for lambdas and LINQ will be the same. |
| Modifying async lambda and LINQ expressions in combination | You can edit various nested expressions provided that they otherwise satisfy the EnC rules | 
| Edit partial class | Enabled in [VS 16.10](https://learn.microsoft.com/en-us/visualstudio/releases/2019/release-notes-v16.10) |
| Edit Source Generated File | Enabled in [VS 16.10](https://learn.microsoft.com/en-us/visualstudio/releases/2019/release-notes-v16.10). |
| Add using directive | Enabled in [VS 16.10](https://learn.microsoft.com/en-us/visualstudio/releases/2019/release-notes-v16.10). |
| Deleting members other than fields | Enabled in [VS 17.3](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes-v17.3) |
| Renaming members other than fields | Enabled in [VS 17.4](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes-v17.4) |
| Rename method parameters | Enabled in [VS 17.0](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes-v17.0) | 
| Modify method parameter types | Enabled in [VS 17.4](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes-v17.4) |
| Change return type of a method/event/property/operator/indexer | Enabled in [VS 17.4](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes-v17.4) |
| Add and modify custom attributes | Enabled in [VS 17.0](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes-v17.0) | 
| Adding and modifying namespace declarations | Enabled in [VS 17.3](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes-v17.3) |

### Not Supported Edits
| Edit operation      | Additional Info |
| ------------------- |-----------------|
| Modify interfaces | - |
| Add a method body such that an abstract method becomes non-abstract | - |
| Add new abstract, virtual, or override member to a type | You CAN add a non-abstract member to an abstract type |
| Add [destructor](https://msdn.microsoft.com/en-us/library/66x5fx1b.aspx) to an existing type |  - |
| Modify a type parameter, base type, delegate type | - |
| Modify a catch-block if it contains an active statement (leaf or internal) | - |
| Modify a try-catch-finally block if the finally clause contains an active statement | - |
| Delete types | - |
| Edit a member referencing an embedded interop type | - |
| Edit a member with On Error or Resume statements | Specific to Visual Basic |
| Edit a member containing an Aggregate, Group By, Simple Join, or Group Join LINQ query clause | Specific to Visual Basic |
