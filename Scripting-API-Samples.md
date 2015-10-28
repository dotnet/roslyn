The scripting APIs enable .NET applications to instatiate a C# engine and execute code snippets against host-supplied objects. Below are examples of how to get started with the scripting APIs and some common samples. 

> **Note:** most samples require the following usings: <br/>
> ```using Microsoft.CodeAnalysis.CSharp.Scripting;``` <br/>

You can also view the [source code](http://source.roslyn.io/#Microsoft.CodeAnalysis.CSharp.Scripting/CSharpScript.cs). There may be some changes to the API after it was finalized early October 2015.

## Scenarios
* [Evaluate a C# expression](#expr)
* [Evaluate a C# expression (strongly-typed)](#exprstrong)
* [Evaluated a C# expression with error handling](#error)
* [Add references](#addref)
* [Add imports](#addimports)
* [Parameterize a script](#parameter)
* [Create & build a C# script and execute it multiple times](#multi)
* [Create a delegate to a script](#delegate)
* [Run a C# snippet and inspect defined script variables](#inspect)
* [Chain code snippets to form a script](#chain)
* [Continue script execution from a previous state](#prevstate)
* [Create and analyze a C# script](#createscript)
* [Customize assembly loading](#assembly)

<hr/>

### <a name="expr"></a>Evaluate a C# expression
```csharp
object result = await CSharpScript.EvaluateAsync("1 + 2");
```

### <a name="exprstrong"></a>Evaluate a C# expression (strongly-typed)
```csharp
int result = await CSharpScript.EvaluateAsync<int>("1 + 2");
```

### <a name="error"></a>Evaluate a C# expression with error handling
```csharp
try
{
    Console.WriteLine(await CSharpScript.EvaluateAsync(Console.ReadLine()));
}
catch (CompilationErrorException e)
{
    Console.WriteLine(string.Join(Environment.NewLine, e.Diagnostics));
}
```

### <a name="addref"></a>Add references
```csharp
var result = await CSharpScript.EvaluateAsync("MyLib.DoStuff()", 
     ScriptOptions.Default.WithReferences(typeof(Lib).Assembly));
```

### <a name="addimports"></a>Add imports
```csharp
var result = await CSharpScript.EvaluateAsync("Sqrt(2)", 
     ScriptOptions.Default.WithImports("System.Math"));
```

### <a name="parameter"></a>Parameterize a script
```csharp
public class Globals
{
    public int X;
    public int Y;
}
var globals = new Globals { X = 1, Y = 2 };
Console.WriteLine(await CSharpScript.EvaluateAsync<int>("X+Y", globals: globals));
```

### <a name="multi"></a> Create & build a C# script and execute it multiple times
```csharp
var script = CSharpScript.Create<int>("X*Y", globalsType: typeof(Globals));
script.Compile();
for (int i = 0; i < 10; i++)
{
    Console.WriteLine(await script.EvaluateAsync(new Globals { X = i, Y = i }));
}
```

### <a name="delegate"></a> Create a delegate to a script
The delegate doesn’t hold compilation resources (syntax trees, etc.) alive.

```csharp
var script = CSharpScript.Create<int>("X*Y", globalsType: typeof(Globals));
ScriptRunner<int> runner = script.CreateDelegate();
for (int i = 0; i < 10; i++)
{
    await runner(new Globals { X = i, Y = i });
}
```

### <a name="inspect"></a> Run a C# snippet and inspect defined script variables
```csharp
var state = await CSharpScript.RunAsync<int>(Console.ReadLine());
foreach (var variable in state.Variables)
     Console.WriteLine($"{variable.Name} = {variable.Value} of type {variable.Type}");
```

### <a name="chain"></a> Chain code snippets to form a script
```csharp
var script = CSharpScript.
    Create<int>("int x = 1;").
    ContinueWith("int y = 2;").
    ContinueWith("x + y");

Console.WriteLine(await script.EvaluateAsync());
```

### <a name="previoustate"></a> Continue script execution from a previous state
```csharp
var state = await CSharpScript.RunAsync("int x = 1;");
state = await state.ContinueWithAsync("int y = 2;");
state = await state.ContinueWithAsync("x+y");
Console.WriteLine(state.ReturnValue);
```

### <a name="createscript"></a> Create and analyze a C# script
```csharp
using Microsoft.CodeAnalysis;

var script = CSharpScript.Create<int>(Console.ReadLine());
Compilation compilation = script.GetCompilation();
//do stuff
```
Compilation gives access to the full set of Roslyn APIs.

### <a name="assembly"></a> Customize assembly loading
```csharp
using Microsoft.CodeAnalysis.Scripting.Hosting;

using (var loader = new InteractiveAssemblyLoader())
{
    var script = CSharpScript.Create<int>("1", assemblyLoader: loader);
    //do stuff 
}
```