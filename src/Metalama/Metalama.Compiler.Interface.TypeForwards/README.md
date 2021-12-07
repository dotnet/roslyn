If tests fail on missing `Metalama.Compiler.Interface.dll` assembly, add a project reference to this project wherever `Microsoft.CodeAnalysis.CSharp.csproj` project is referenced.

For a bulk fix, use

`postsharp-eng msbuild apr Microsoft.CodeAnalysis.CSharp.csproj src\Metalama\Metalama.Compiler.Interface.TypeForwards\Metalama.Compiler.Interface.TypeForwards.csproj Test`

See https://dev.azure.com/postsharp/Engineering/_git/PostSharp.Engineering.BuildTools.