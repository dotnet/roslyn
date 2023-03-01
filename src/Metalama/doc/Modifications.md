# Modifications and additions

This is a list of significant modifications done to Roslyn to support Metalama, along with some details.

To see all changes made in Metalama Compiler, you can do a diff with the last merged Roslyn commit.

First, add the remote to your local clone:

```
git remote add roslyn https://github.com/dotnet/roslyn.git
```

Then at moment of writing:

* `git diff Visual-Studio-2019-Version-16.8..master` or 
* `gitk Visual-Studio-2019-Version-16.8..master`.

## Metalama.Compiler.CodeAnalysis

This shared source project contains Metalama additions to the Microsoft.CodeAnalysis project. It exists for better separation of Metalama Compiler code from Roslyn code

### TransformerDependencyResolver

Ensures that the order of transformers is consistent and fully specified. This is a holdover from an earlier design where it was assumed there would be more than one transformer and can probably be removed now.

It uses a simplified version of `Graph` from PostSharp.

### TreeTracker

#### Tracking of nodes

To support debugging and reporting diagnostics in user code, the Metalama Compiler tracks changes done to syntax trees during transformer execution and maintains a map from syntax nodes in modified trees to nodes in the original tree. The central code for doing this is in `TreeTracker`.

The way tracking works is that each tracked tree has its root node annotated and there is also a `ConditionalWeakTable` mapping each annotation to the original node. When a change is made inside a tracked subtree, new annotations are added, to make sure nodes can still be mapped to their originals. The annotation of the root of the tree is then changed, to indicate that it has been modified.

For example, consider `{ int i; }`. If we started tracking this and then added a new statement `i++;` to the block, then both the modified block and the variable declaration will have their own annotations and can be mapped to their original nodes.

But if we instead created a new block that contained the declaration from the tracked block, then only the declaration could be tracked back to its original.

Tree tracker is called from several places in the code base, most interestingly from Syntax.xml.Main.Generated.cs and Syntax.xml.Syntax.Generated.cs. 
Note that if you need to modify the .Generated.cs files, you should make your changes in `SourceWriter` in the CSharpSyntaxGenerator project.

We have also added a node in `BoundNodes.xml`. To make it obvious in the generated files that this node is added in Metalama, we have modified the `Model.cs` and `BoundNodeClassWriter.cs` in BoundTreeGeneratorProject.

There are two ways the code is generated:

- The code generated to CSharpSyntaxGenerator.SourceGenerator folder is generated using a source generator in project Microsoft.CodeAnalysis.CSharp.csproj.
- The rest is generated using `eng\generate-compiler-code.cmd` command.

Tree tracker is then used when emitting PDBs (in `CodeGenerator`) and when handling diagnostics (in `CSDiagnostic` and `CSharpDiagnosticFilter`).

#### Mapping of locations and diagnostics

The method `TreeTracker.MapDiagnostic` maps a diagnostic from the transformed syntax tree to the source syntax tree. Additionally, it stores the `SyntaxNode` and `Compilation` related to this diagnostic. This info can be retrieved using `TreeTracker.TryGetDiagnosticInfo`. This is used to make it easier for diagnostic suppression to retrieve symbol information about a diagnostic.  

## Metalama.Compiler.Shared

This code is included into the Metalama.Compiler.Sdk version of Metalama.Compiler.Interface.dll and the Metalama.Compiler version of Microsoft.CodeAnalysis.dll (using `#if`s where distinction between the two is necessary).

### MetalamaCompilerInfo

Can be used from a source generator (or an analyzer) to see if it is running inside the Metalama Compiler.

### Intrinsics

The API for `ldtoken`/GetRuntimeHandle using documentation comment ID intrinsics. The actual implementation is in LocalRewriter_Call.cs, method `MakeCall` and in EmitExpression.cs, method `EmitLdtoken`.

### ISourceTransformer, TransformerContext

The interface to be implemented by a source transformer (i.e. Metalama proper).

### ResourceDescriptionExtensions

Used to provide a source transformer access to read information about a managed resource, which is not exposed in the Roslyn API.

## Microsoft.CodeAnalysis.CSharp

The method `CSharpCompiler.RunTransformers` is where transformers are actually executed.

## Microsoft.Build.Tasks.CodeAnalysis

Added parameters to the Csc MSBuild task and added MSBuild properties.

## Microsoft.CodeAnalysis

Added and processed parameters to the command line compiler (which is used by the Csc task).

Also, loading transformers from assemblies, using the same approach as loading source generators and analyzers.

## Microsoft.CodeAnalysis.CSharp.Workspaces

Changes required to make try.metalama.net work, since it uses the Roslyn Workspace API and not the command-line compiler.
