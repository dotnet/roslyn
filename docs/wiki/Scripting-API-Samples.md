The scripting APIs enable .NET applications to instatiate a C# engine and execute code snippets against host-supplied objects. Below are examples of how to get started with the scripting APIs and some common samples. You can also view the Scripting API [source code](https://github.com/dotnet/roslyn/tree/a7319e2bc8cac34c34527031e6204d383d29d4ab/src/Scripting).

## Supported Platforms

Scripting APIs require desktop .NET Framework 4.6+, or .NET Core 1.1 (supported since [Roslyn v2.0.0-rc3](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Scripting/2.0.0-rc3), Visual Studio 2017 RC3).

Scripting APIs can't be used within Universal Windows Applications and .NET Native since the application model doesn't support loading code generated at runtime. 

## Getting Started
Install the [Scripting API NuGet Package](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting/):
```
Install-Package Microsoft.CodeAnalysis.CSharp.Scripting
```

## Code Samples
> **Note:** the samples require the following using: <br/>
> ```using Microsoft.CodeAnalysis.CSharp.Scripting;``` <br/>

### Scenarios
* [Evaluate a C# expression](#expr)
* [Evaluate a C# expression (strongly-typed)](#exprstrong)
* [Evaluated a C# expression with error handling](#error)
* [Add references](#addref)
* [Add namespace and type imports](#addimports)
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

#### <a name="error"></a>Evaluate a C# expression with error handling
```csharp
try
{
    Console.WriteLine(await CSharpScript.EvaluateAsync("2+2"));
}
catch (CompilationErrorException e)
{
    Console.WriteLine(string.Join(Environment.NewLine, e.Diagnostics));
}
```

### <a name="addref"></a>Add references
```csharp
var result = await CSharpScript.EvaluateAsync("System.Net.Dns.GetHostName()", 
     ScriptOptions.Default.WithReferences(typeof(System.Net.Dns).Assembly));
```

#### <a name="addimports"></a>Add namespace and type imports

In the following code `WithImports("System.IO")` adds `using System.IO;` to the script options, making it possible to reference the types of `System.IO` namespace from the script code without qualification.

```csharp
var result = await CSharpScript.EvaluateAsync("Directory.GetCurrentDirectory()"), 
     ScriptOptions.Default.WithImports("System.IO"));
```

Likewise, `WithImports("System.Math")` adds `using static System.Math;` to the script options, making it possible to reference the members of `System.Math` type without qualification.

```csharp
var result = await CSharpScript.EvaluateAsync("Sqrt(2)", 
     ScriptOptions.Default.WithImports("System.Math"));
```

#### <a name="parameter"></a>Parameterize a script
```csharp
public class Globals
{
    public int X;
    public int Y;
}
var globals = new Globals { X = 1, Y = 2 };
Console.WriteLine(await CSharpScript.EvaluateAsync<int>("X+Y", globals: globals));
```

> **Note**: Currently the Globals type has to be defined in an assembly loaded from a file. If the assembly is in-memory (including e.g. when the sample is executed in Interactive Window) the script won't be able to access the type. See [issue](https://github.com/dotnet/roslyn/issues/6101) here.

#### <a name="multi"></a> Create & build a C# script and execute it multiple times
```csharp
var script = CSharpScript.Create<int>("X*Y", globalsType: typeof(Globals));
script.Compile();
for (int i = 0; i < 10; i++)
{
    Console.WriteLine((await script.RunAsync(new Globals { X = i, Y = i })).ReturnValue);
}
```

#### <a name="delegate"></a> Create a delegate to a script
The delegate doesn’t hold compilation resources (syntax trees, etc.) alive.

```csharp
var script = CSharpScript.Create<int>("X*Y", globalsType: typeof(Globals));
ScriptRunner<int> runner = script.CreateDelegate();
for (int i = 0; i < 10; i++)
{
    Console.WriteLine(await runner(new Globals { X = i, Y = i }));
}
```

#### <a name="inspect"></a> Run a C# snippet and inspect defined script variables
```csharp
var state = await CSharpScript.RunAsync<int>("int answer = 42;");
foreach (var variable in state.Variables)
     Console.WriteLine($"{variable.Name} = {variable.Value} of type {variable.Type}");
```

#### <a name="chain"></a> Chain code snippets to form a script
```csharp
var script = CSharpScript.
    Create<int>("int x = 1;").
    ContinueWith("int y = 2;").
    ContinueWith("x + y");

Console.WriteLine((await script.RunAsync()).ReturnValue);
```

#### <a name="previoustate"></a> Continue script execution from a previous state
```csharp
var state = await CSharpScript.RunAsync("int x = 1;");
state = await state.ContinueWithAsync("int y = 2;");
state = await state.ContinueWithAsync("x+y");
Console.WriteLine(state.ReturnValue);
```

#### <a name="createscript"></a> Create and analyze a C# script
```csharp
using Microsoft.CodeAnalysis;

var script = CSharpScript.Create<int>("3");
Compilation compilation = script.GetCompilation();
//do stuff
```
Compilation gives access to the full set of Roslyn APIs.

#### <a name="assembly"></a> Customize assembly loading
```csharp
using Microsoft.CodeAnalysis.Scripting.Hosting;

using (var loader = new InteractiveAssemblyLoader())
{
    var script = CSharpScript.Create<int>("1", assemblyLoader: loader);
    //do stuff 
}
```