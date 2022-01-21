A question that arises when emitting assemblies via Roslyn Compilation APIs in .NET Core app is how to create MetadataReferences for .NET Core Framework libraries (such as ```System.Runtime.dll```, ```System.IO.dll```, etc.). 

There are two approaches to runtime code generation:

### Compile against runtime (implementation) assemblies

This is what [C# Scripting API](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md) currently does. There are a few gotchas with this approach:
- in .NET Core 1.x the implementation assemblies currently contain some duplicate public types (fixed in 2.0, see https://github.com/dotnet/corefx/issues/5540).
- the implementation assemblies change with releases, so code that compiles against one version of CoreCLR might not compile against newer version.
  The APIs are backward compatible, however compiler overload resolution might prefer an API added in the new version instead of the one that it used to pick before.

The Scripting APIs use Trusted Platform Assembly list to locate the implementation assemblies on CoreCLR.

You can get a list of semicolon-separated paths to these .dlls like so (see [RuntimeMetadataReferenceResolver](http://sourceroslyn.io/#Microsoft.CodeAnalysis.Scripting/Hosting/Resolvers/RuntimeMetadataReferenceResolver.cs) implementation):

```C#
AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
```

Enumerate this and find paths to assemblies you're interested in (you'll find "mscorlib.dll" on that list too).
You can use RuntimeMetadataReferenceResolver to do this for you.

### Compile against reference (contract) assemblies

This is what the compiler does when invoked from msbuild. You need to decide what reference assemblies to use (e.g. ```netstandard1.5```). Once you decide, you need to get them from nuget packages and distribute them with your application, e.g. in a form of embedded resources. Then in your application extract the binaries from resources and create MetadataReferences for them. 

To find out what reference assemblies you need and where to get them you can create an empty .NET Core library, set the target framework to the one you need to target, build using ```msbuild /v:detailed``` and look for ```csc.exe``` invocation in msbuild output. The command line will list all references the C# compiler uses to build the library (look for ```/reference``` command line arguments).

Alternatively, projects that use .NET SDK can set `PreserveCompilationContext` build property to `true`. Publishing such project will copy reference assemblies for the framework the project targets to a `refs` sub-directory of the `publish` directory.

This approach has the benefit of stable APIs - the reference assemblies will never change, so your code will work on future versions of CoreCLR runtimes.

---

Related issues:
- https://github.com/dotnet/roslyn/issues/16846
