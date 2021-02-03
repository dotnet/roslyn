# Modifications and additions

This is a list of significant modifications done to Roslyn to support Caravela, along with some details.

To see all changes made in Caravela Compiler, you can do a diff with the last merged Roslyn commit. E.g., at the moment of writing, `git diff Visual-Studio-2019-Version-16.8..master` or `gitk Visual-Studio-2019-Version-16.8..master`.

## Caravela.Compiler.CodeAnalysis

This shared source project contains Caravela additions to the Microsoft.CodeAnalysis project. It exists for better separation of Caravela Compiler code from Roslyn code

### TransformerDependencyResolver

Ensures that the order of transformers is consistent and fully specified. This is a holdover from an earlier design where it was assumed there would be more than one transformer and can probably be removed now.

It uses a simplified version of `Graph` from PostSharp.

### TimeBomb

Prevents a version of Caravela Compiler to be used 90 days after it has been built, with a warning after 60 days.

This code should be removed once Caravela is out of preview.

### TreeTracker

To support debugging and reporting diagnostics in user code, the Caravela Compiler tracks changes done to syntax trees during transformer execution and maintains a map from syntax nodes in modified trees to nodes in the original tree. The central code for doing this is in `TreeTracker`.

The way tracking works is that each tracked tree has its root node annotated and there is also a `ConditionalWeakTable` mapping each annotation to the original node. When a change is made inside a tracked subtree, new annotations are added, to make sure nodes can still be mapped to their originals. The annotation of the root of the tree is then changed, to indicate that it has been modified.

For example, consider `{ int i; }`. If we started tracking this and then added a new statement `i++;` to the block, then both the modified block and the variable declaration will have their own annotations and can be mapped to their original nodes.

But if we instead created a new block that contained the declaration from the tracked block, then only the declaration could be tracked back to its original.

Tree tracker is called from several places in the code base, most interestingly from Syntax.xml.Main.Generated.cs and Syntax.xml.Syntax.Generated.cs. 
Note that if you need to modify the .Generated.cs files, you should make your changes in `SourceWriter` in the CSharpSyntaxGenerator project.

Tree tracker is then used when emitting PDBs (in `CodeGenerator`) and when handling diagnostics (in `CSDiagnostic` and `CSharpDiagnosticFilter`).

## Caravela.Compiler.Shared

This code is included into the Caravela.Compiler.Sdk version of Caravela.Compiler.Interface.dll and the Caravela.Compiler version of Microsoft.CodeAnalysis.dll (using `#if`s where distinction between the two is necessary).

### CaravelaCompilerInfo

Can be used from a source generator (or an analyzer) to see if it is running inside the Caravela Compiler.

### Intrinsics

The API for `ldtoken`/GetRuntimeHandle using documentation comment ID intrinsics. The actual implementation is in LocalRewriter_Call.cs, method `MakeCall` and in EmitExpression.cs, method `EmitLdtoken`.

### ISourceTransformer, TransformerContext

The interface to be implemented by a source transformer (i.e. Caravela proper).

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

Changes required to make try.postsharp.net work, since it uses the Roslyn Workspace API and not the command-line compiler.

## Caravela.CodeAnalysis.Workspaces.Lightweight

Trimmed down versions of Workspaces projects, used in Caravela.Framework.Impl. Trimming is performed by including only the necessary files from the Workspaces projects and their shared source parts and also sometimes (mostly for Extensions files) by trimming parts of files using `#if !LIGHTWEIGHT`.