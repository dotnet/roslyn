# Incremental Generators

## Summary

Incremental generators are intended to be a new API that exists alongside
[source generators](generators.md) to allow users to specify generation
strategies that can be applied in a high performance way by the hosting layer.

### High Level Design Goals

- Allow for a finer grained approach to defining a generator
- Scale source generators to support 'Roslyn/CoreCLR' scale projects in Visual Studio
- Exploit caching between fine grained steps to reduce duplicate work
- Support generating more items that just source texts
- Exist alongside `ISourceGenerator` based implementations

## Implementation

An incremental generator is an implementation of `Microsoft.CodeAnalysis.IIncrementalGenerator`.

```csharp
namespace Microsoft.CodeAnalysis
{
    public interface IIncrementalGenerator
    {
        void Initialize(IncrementalGeneratorInitializationContext initContext);
    }
}
```

As with source generators, incremental generators are defined in external
assemblies and passed to the compiler via the `-analyzer:` option.
Implementations are required to be annotated with the
`Microsoft.CodeAnalysis.GeneratorAttribute` with an optional parameter
indicating the languages the generator supports:

```csharp
[Generator(LanguageNames.CSharp)]
public class MyGenerator : IIncrementalGenerator { ... }
```

An assembly can contain a mix of diagnostic analyzers, source generators and
incremental generators.

### Initialization

`IIncrementalGenerator` has an `Initialize` method that is called by the host
(either the IDE or the command-line compiler) exactly once, regardless of the
number of further compilations that may occur. `Initialize` passes an instance
of `IncrementalGeneratorInitializationContext` which can be used by the
generator to register a set of callbacks that affect how future generation
passes will occur.

Currently `IncrementalGeneratorInitializationContext` supports two callbacks:

- `RegisterForPostInitialization(GeneratorPostInitializationContext)`: the same
  callback in shape and function as supported by Source Generators today
- `RegisterExecutionPipeline(IncrementalGeneratorPipelineContext)`: replaces Execute, described below

### Pipeline based execution

Rather than a dedicated `Execute` method, an Incremental Generator instead
creates an execution 'pipeline' as part of the initialization process via the
`RegisterExecutionPipeline` method:

```csharp
public void Initialize(IncrementalGeneratorInitializationContext initContext)
{
    initContext.RegisterExecutionPipeline(context =>
    {
        // build the pipeline...
    });
}
```

This pipeline is not directly executed, but instead consists of a set of steps
that are executed on demand as the input data to the pipeline changes. Between
each step the data produced is cached, allowing previously calculated values to
be reused in later computations when applicable, reducing the overall
computation required between compilations.

### IncrementalValueSource&lt;T&gt;

Input data is available to the pipeline in the form of an opaque data source,
modelled as an `IncrementalValueSource<T>` where _T_ is the type of data that
can be accessed in the pipeline.

These sources are defined up front by the compiler, and can be accessed from the
`Values` property of the `context` passed as part of the
`RegisterExecutionPipeline` callback. Example values sources include

- Compilation
- AdditionalTexts
- AnalyzerConfigOptions
- MetadataReferences
- ParseOptions

Value sources have 'zero-or-more' potential values that can be produced. For
example, the `Compilation` will always produce a single value, whereas the
`AdditionalTexts` will produce a variable number of values, depending on how
many additional texts where passed to the compiler.

An execution pipeline cannot access these values directly. Instead it supplies a
set of transforms that will be applied to the data as it changes. Transforms are
applied through a set of extension methods:

```csharp
public static partial class IncrementalValueSourceExtensions
{
    // 1 => 1 transform 
    public static IncrementalValueSource<U> Transform<T, U>(this IncrementalValueSource<T> source, Func<T, U> func) => ...
}
```

These extension methods allow the user to perform a series of transforms,
conceptually somewhat similar to LINQ, over the values coming from the data
source:

```csharp
initContext.RegisterExecutionPipeline(context =>
{
    // get the additional text source
    IncrementalValueSource<AdditionalText> additionalTexts = context.Sources.AdditionalTexts;

    // apply a 1-to-1 transform on each text, which represents extracting the path
    IncrementalValueSource<string> transformed = additionalTexts.Transform(static text => text.Path);

});
```

Note that `transformed` is similarly opaque. It represents the outcome of the
transformation being applied to the data, but cannot be accessed directly.

The transformed steps can be further transformed, and it is also valid to
perform multiple transformations on the same input node, essentially 'splitting'
the pipeline into multiple streams of processing.

```csharp
    // apply a 1-to-1 transform on each text, extracting the path
    IncrementalValueSource<string> transformed = additionalTexts.Transform(static text => text.Path);

    // split the processing into two streams of derived data
    IncrementalValueSource<string> prefixTransform = transformed.Transform(static path => "prefix_" + path);
    IncrementalValueSource<string> postfixTransform = transformed.Transform(static path => path + "_postfixed");
```

### Batching

In addition to the 1-to-1 transform shown above, there are also extension
methods for producing and consuming batches of data. For instance a given
transform may want to produce more than one value for each input, or want to
view all the data in a single collected view in order to make cross data
decisions.

``` csharp
public static partial class IncrementalValueSourceExtensions
{
    // 1 => many (or none)
    public static IncrementalValueSource<U> TransformMany<T, U>(this IncrementalValueSource<T> source, Func<T, IEnumerable<U>> func) => ...

    // many => 1
    public static IncrementalValueSource<U> BatchTransform<T, U>(this IncrementalValueSource<T> source, Func<IEnumerable<T>, U> func) => ...

    // many => many (or none)
    public static IncrementalValueSource<U> BatchTransformMany<T, U>(this IncrementalValueSource<T> source, Func<IEnumerable<T>, IEnumerable<U>> func) => ...
}
```

In our above example we could use `BatchTransform` to collect the individual
file paths collected, and convert them into a single collection:

``` csharp
    // apply a 1-to-1 transform on each text, which represents extracting the path
    IncrementalValueSource<string> transformed = additionalTexts.Transform(static text => text.Path);

    // batch the collected file paths into a single collection
    IncrementalValueSource<IEnumerable<string>> batched = transformed.BatchTransform(static paths => paths);
```

The author could have equally combined these two steps into a single operation
that utilizes LINQ:

``` csharp
    // using System.Linq;
    IncrementalValueSource<IEnumerable<string>> singleOp = additionalTexts.BatchTransform(static texts => texts.Select(text => text.Path));
```

**OPEN QUESTION** Should there be versions of
`BatchTransform`/`BatchTransformMany` that take no transformation, and just
perform the identity function as specified above?

### Outputting values

At some point in the pipeline the author will want to actually use the
transformed data to produce an output, such as a `SourceText`. For this purpose
there are 'terminating' extension methods that allow the author to provide the
resulting data the generator produces. The set of terminating extensions
include:

- GenerateSource
- GenerateEmbeddedFile
- GenerateArtifact

GenerateSource for example looks like:

``` csharp
static partial class IncrementalValueSourceExtensions
{
    public internal static IncrementalGeneratorOutput GenerateSource<T>(this IncrementalValueSource<T> source, Action<SourceProductionContext, T> action) => ...
}
```

That can be used by the author to supply `SourceText` to be appended to the
compilation via the passed in `SourceProductionContext`:

``` csharp
    // take the file paths from the above batch and make some user visible syntax
    batched.GenerateSource(static (sourceProductionContext, filePaths) =>
    {
        sourceProductionContext.AddSource("additionalFiles.cs", @"
namespace Generated
{
    public class AdditionalTextList
    {
        public static void PrintTexts()
        {
            System.Console.WriteLine(""Additional Texts were: " + string.Join(", ", filePaths) + @" "");
        }
    }
}");
    });
```

A generator may create and register multiple output nodes as part of the
pipeline, but an output cannot be further transformed once it is created.

Splitting the outputs by type produced allows the host (such as the IDE) to not
run outputs that are not required. For instance artifacts are only produced as
part of a command line build, so the IDE has no need to run the artifact based
outputs or steps that feed into it.

**OPEN QUESTION** Should we have `GenerateBatch...` versions of the output
methods? This can already be achieved by the author calling `BatchTransform`
before calling `Generate...` so would just be a helper, but seems common enough
that it could be useful.

### Simple example

Putting together the various steps outlined above, an example incremental
generator might look like the following:

``` csharp
[Generator(LanguageNames.CSharp)]
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        initContext.RegisterExecutionPipeline(context =>
        {
            // get the additional text source
            IncrementalValueSource<AdditionalText> additionalTexts = context.Sources.AdditionalTexts;

            // apply a 1-to-1 transform on each text, extracting the path
            IncrementalValueSource<string> transformed = additionalTexts.Transform(static text => text.Path);

            // batch the collected file paths into a single collection
            IncrementalValueSource<IEnumerable<string>> batched = transformed.BatchTransform(static paths => paths);

            // take the file paths from the above batch and make some user visible syntax
            batched.GenerateSource(static (sourceContext, filePaths) =>
            {
                sourceContext.AddSource("additionalFiles.cs", @"
namespace Generated
{
    public class AdditionalTextList
    {
        public static void PrintTexts()
        {
            System.Console.WriteLine(""Additional Texts were: " + string.Join(", ", filePaths) + @" "");
        }
    }
}");
            });
        });
    }
}
```

## Advanced Implementation

### Combining and filtering

While the transformation steps outlined above allow a user to create simple
generators from a single source of data, in reality it is expected that an
author will need a way to take multiple sources of data and combine them.

There exists an extension method `Combine` that allows the author to take one
data source and merge it with another, for example, extracting a set of types
from the compilation and using them to generate something on a per additional
file basis.

```csharp
public static partial class IncrementalValueSourceExtensions
{
    // join 1 => many ((source1[0], source2), (source1[1], source2), (source1[2], source2), ...)
    public static IncrementalValueSource<(T source1Item, IEnumerable<U> source2Batch)> Combine<T, U>(this IncrementalValueSource<T> source1, IncrementalValueSource<U> source2) => ...
}
```

The second data source is batched, and the resulting step has a value type of
`(T, IEnumerable<U>)`. That is, a tuple where each element consists of a single
item of data from `source1` combined with the entire batch of data from
`source2`. While this is somewhat low-level, when combined with subsequent
transforms, it gives the author the ability to combine an arbitrary number of
data sources into any shape they require.

In the following example the author combines the additional text source with the
compilation:

```csharp
IncrementalValueSource<(AdditionalText source1Item, IEnumerable<Compilation> source2Batch)> combined = context.Sources.AdditionalTexts.Combine(context.Sources.Compilation);
```

The type of combined is an
`IncrementalValueSource<(AdditionalText, IEnumerable<Compilation>)>`. For each
additional text, there is a tuple with the text, and the batched data of the
compilation source. As the author knows that there is only ever a single
compilation, they are free to transform the data to select the single
compilation object:

```csharp
IncrementalValueSource<(AdditionalText, Compilation)> transformed = combined.Transform(static pair => (pair.source1Item, pair.source2Batch.Single()));
```

Similarly a cross join can be achieved by first combining two value sources,
then batch transforming the resulting value source.

```csharp
IncrementalValueSource<(AdditionalText, MetadataReference)> combined = context.Sources.AdditionalTexts
                                                                                      .Combine(context.Sources.MetadataReference)
                                                                                      .TransformMany(static pair => pair.source2Batch.Select(static metadataRef => (pair.source1Item, metadataRef));
```

**OPEN QUESTION**: Combine is pretty low level, but does allow you to do
everything needed when used in conjunction with transform. Should we provide
some higher level extension methods that chain together combine and transform
out of the box for common operations?

While filtering can be easily enough implement by the user as a transform step,
it seems common and useful enough that we provide an implementation directly for
the user to consume

```csharp
static partial class IncrementalValueSourceExtensions
{
    // helper for filtering values
    public static IncrementalValueSource<T> Filter<T>(this IncrementalValueSource<T> source, Func<T, bool> filter) => ...
}
```

### Caching

While the finer grained steps allow for some coarse control of output types via
the generator host, the performance benefits are only really seen when the
driver can cache the outputs from one pipeline step to the next. While we have
generally said that the execute method in an `ISourceGenerator` should be
deterministic, incremental generators actively _require_ this property to be
true.

When calculating the required transformations to be applied as part of a step,
the generator driver is free to look at inputs it has seen before and used
previous computed and cached values of the transformation for these inputs. When
using non batch transforms it can do this on an element by element basis.

Consider the following transform:

```csharp
IValueSource<string> transform = context.Sources.AdditionalTexts
                                                .Transform(static t => t.Path)
                                                .Transform(static p => "prefix_" + p);
```

During the first execution of the pipeline each of the two lambdas will be
executed for each additional file:

AdditionalText          | Transform1 | Transform2
------------------------|------------|-----------------
Text{ Path: "abc.txt" } | "abc.txt"  | "prefix_abc.txt"
Text{ Path: "def.txt" } | "def.txt"  | "prefix_def.txt"
Text{ Path: "ghi.txt" } | "ghi.txt"  | "prefix_ghi.txt"

Now consider the case where in some future iteration, the first additional file
has changed and has a different path, and the second file has changed, but kept
its path the same.

AdditionalText               | Transform1 | Transform2
-----------------------------|------------|-----------
**Text{ Path: "diff.txt" }** |            |
**Text{ Path: "def.txt" }**  |            |
Text{ Path: "ghi.txt" }      |            |

The generator would run transform1 on the first and second files, producing
"diff.txt" and "def.txt" respectively. However, it would not need to re-run the
transform for the third file, as the input has not changed. It can just use the
previously cached value.

AdditionalText               | Transform1     | Transform2
-----------------------------|----------------|-----------
**Text{ Path: "diff.txt" }** | **"diff.txt"** |
**Text{ Path: "def.txt" }**  | **"def.txt"**  |
Text{ Path: "ghi.txt" }      | "ghi.txt"      |

Next the driver would look to run Transform2. It would operate on `"diff.txt"`
producing `"prefix_diff.txt"`, but when it comes to `"def.txt"` it can observe
that the item produced was the same as the last iteration. Even though the
original input (`Text{ Path: "def.txt" }`) was changed, the result of Transform1
on it was the same. Thus there is no need to re-run Transform2 on `"def.txt"` as
it can just use the cached value from before. Similarly the cached state of
"ghi.txt" can be used.

AdditionalText               | Transform1     | Transform2
-----------------------------|----------------|----------------------
**Text{ Path: "diff.txt" }** | **"diff.txt"** | **"prefix_diff.txt"**
**Text{ Path: "def.txt" }**  | **"def.txt"**  | "prefix_diff.txt"
Text{ Path: "ghi.txt" }      | "ghi.txt"      | "prefix_ghi.txt"

In this way, only changes that are consequential flow through the pipeline, and
duplicate work is avoided. If a generator only relies on `AdditionalTexts` then
the driver knows there can be no work to be done when a `SyntaxTree` changes.

### WithComparer

For a user provided result to be comparable across iterations, there needs to be some concept of equivalence. Rather than requiring types returned from transformations implement `IEquatable<T>`, there exists an extension method that allows the author to supply a comparer that should be used when comparing values for the given transformation:

```csharp
public static partial class IncrementalValueSourceExtensions
{
    public static IncrementalValueSource<T> WithComparer(IEqualityComparer<T> comparer) => ...
}
```

Allowing the user to specify a given comparer.

```csharp
var withComparer = context.Sources.AdditionalTexts
                                  .Transform(t => t.Path)
                                  .WithComparer(myComparer);
```

Note that the comparer is on a per-transformation basis, meaning an author can specify different comparers for different parts of the pipeline. 

```csharp
var transform = context.Sources.AdditionalTexts.Transform(t => t.Path);

var noCompareTransform = transform.Transform(...);
var compareTransform = transform.WithComparer(myComparer).Transform(...);
```

The same transform node can have no comparer when acting as input to one step,
and still provide one when acting as input to a different step.

**OPEN QUESTION**: This again gives maximal flexibility, but might be annoying
if you have lots of custom data structures that you use in multiple places. Should
we consider allowing the author to specify a set of 'default' comparers that
apply to all transforms unless overridden?

**OPEN QUESTION**: `IValueComparer<T>` seems like the correct type to use, but
can be less ergonomic as it requires the author to define a type not inline and
reference it here. Should we provided a 'functional' overload that creates the
equality comparer for the author under the hood given a lambda?

### Syntax Trees

## Internal Implementation

TK: compiler specific implementation. StateTables etc.

### Hosting ISourceGenerator on the new APIs