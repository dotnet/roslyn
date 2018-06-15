# Toolset packages

Our repo needs a number of tools that are delivered by NuGet packages.  These packages shouldn't 
be added to the project files because the assets aren't actually referenced.  Additionally in 
some cases the package references can complicate the package graph and lead to developer headaches.  

For such core toolset packages we put all of the `<PackageReference>` elements here and do a 
single restore before we build. 

In general we try and keep the number of files here low.  New ones are added only when the 
contents can potentially conflict with packages listed in the existing files.

File purposes:

- RoslynToolset.csproj: all tools necessary for doing a normal Roslyn build
- InternalToolset.csproj: tools specific to official builds of Roslyn
