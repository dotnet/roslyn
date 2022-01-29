# Incremental Generators

## Summary

Incremental generators are a new API that exists alongside
[source generators](generators.md) to allow users to specify generation
strategies that can be applied in a high performance way by the hosting layer.

### High Level Design Goals

- Allow for a finer grained approach to defining a generator
- Scale source generators to support 'Roslyn/CoreCLR' scale projects in Visual Studio
- Exploit caching between fine grained steps to reduce duplicate work
- Support generating more items that just source texts
- Exist alongside `ISourceGenerator` based implementations

## Simple Example

We begin by defining a simple incremental generator that extracts the contents
of additional text files and makes their contents available as compile time
`const`s. In the following section we'll go into more depth around the concepts
shown.

```csharp
[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // define the execution pipeline here via a series of transformations:

        // find all additional files that end with .txt
        IncrementalValuesProvider<AdditionalText> textFiles = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".txt"));

        // read their contents and save their name
        IncrementalValuesProvider<(string name, string content)> namesAndContents = textFiles.Select((text, cancellationToken) => (name: Path.GetFileNameWithoutExtension(text.Path), content: text.GetText(cancellationToken)!.ToString()));

        // generate a class that contains their values as const strings
        context.RegisterSourceOutput(namesAndContents, (spc, nameAndContent) =>
        {
            spc.AddSource($"ConstStrings.{nameAndContent.name}", $@"
    public static partial class ConstStrings
    {{
        public const string {nameAndContent.name} = ""{nameAndContent.content}"";
    }}");
        });
    }
}
```

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

### Pipeline based execution

`IIncrementalGenerator` has an `Initialize` method that is called by the
host[^1] exactly once, regardless of the number of further compilations that may
occur. For instance a host with multiple loaded projects may share the same
generator instance across multiple projects, and will only call `Initialize` a
single time for the lifetime of the host.

[^1]: Such as the IDE or the command-line compiler

Rather than a dedicated `Execute` method, an Incremental Generator instead
defines an immutable execution pipeline as part of initialization. The
`Initialize` method receives an instance of
`IncrementalGeneratorInitializationContext`which is used by the generator to
define a set of transformations.


```csharp
public void Initialize(IncrementalGeneratorInitializationContext initContext)
{
    // define the execution pipeline here via a series of transformations:
}
```

The defined transformations are not executed directly at initialization, and
instead are deferred until the data they are using changes. Conceptually this is
similar to LINQ, where a lambda expression might not be executed until the
enumerable is actually iterated over:

**IEnumerable**:

```csharp
    var squares = Enumerable.Range(1, 10).Select(i => i * 2); 

    // the code inside select is not executed until we iterate the collection
    foreach (var square in squares) { ... }
```

These transformations are used to form a directed graph of actions that can be
executed on demand later, as the input data changes.

**Incremental Generators**:

```csharp
    IncrementalValuesProvider<AdditionalText> textFiles = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".txt"));
    // the code in the Where(...) above will not be executed until the value of the additional texts actually changes
```
 
Between each transformation, the data produced is cached, allowing previously calculated
values to be re-used where applicable. This caching reduces the computation
required for subsequent compilations. See [caching](#caching) for more details.

### IncrementalValue\[s\]Provider&lt;T&gt;

Input data is available to the pipeline in the form of opaque data sources,
either an `IncrementalValueProvider<T>` or `IncrementalValuesProvider<T>` (note
the plural _values_) where _T_ is the type of input data that is provided.

An initial set of providers are created by the host, and can be accessed from the
`IncrementalGeneratorInitializationContext` provided during initialization.

The currently available providers are:

- CompilationProvider
- AdditionalTextsProvider
- AnalyzerConfigOptionsProvider
- MetadataReferencesProvider
- ParseOptionsProvider

*Note*: there is no provider for accessing syntax nodes. This is handled
in a slightly different way. See [SyntaxValueProvider](#syntaxvalueprovider) for details.

A value provider can be thought of as a 'box' that holds the value itself. An
execution pipeline does not access the values in a value provider directly.

```ascii
IValueProvider<TSource>
   ┌─────────────┐
   |             |
   │   TSource   │
   |             |
   └─────────────┘
```

Instead, the generator supplies a set of transformations that ar to be applied to the
data contained within the provider, which in turn creates a new value provider.

### Select

The simplest transformation is `Select`. This maps the value in one provider
into a new provider by applying a transform to it.

```ascii
 IValueProvider<TSource>                     IValueProvider<TResult>
    ┌─────────────┐                            ┌─────────────┐
    │             │  Select<TSource,TResult1>  │             │
    │   TSource   ├───────────────────────────►│   TResult   │
    │             │                            │             │
    └─────────────┘                            └─────────────┘
```

Generator transformations can be thought of as being conceptually somewhat similar to
LINQ, with the value provider taking the place of `IEnumerable<T>`.
Transforms are created through a set of extension methods:

```csharp
public static partial class IncrementalValueSourceExtensions
{
    // 1 => 1 transform 
    public static IncrementalValueProvider<TResult> Select<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector);
    public static IncrementalValuesProvider<TResult> Select<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector);
}
```

Note how the return type of these methods are also an instance of
`IncrementalValue[s]Provider`. This allows the generator to chain multiple
transformations together:

```ascii
 IValueProvider<TSource>                     IValueProvider<TResult1>                 IValueProvider<TResult2>
    ┌─────────────┐                            ┌─────────────┐                           ┌─────────────┐
    │             │  Select<TSource,TResult1>  │             │ Select<TResult1,TResult2> │             │
    │   TSource   ├───────────────────────────►│   TResult1  │──────────────────────────►│   TResult2  │
    │             │                            │             │                           │             │
    └─────────────┘                            └─────────────┘                           └─────────────┘
```

Consider the following simple example:

```csharp
initContext.RegisterExecutionPipeline(context =>
{
    // get the additional text provider
    IncrementalValuesProvider<AdditionalText> additionalTexts = context.AdditionalTextsProvider;

    // apply a 1-to-1 transform on each text, which represents extracting the path
    IncrementalValuesProvider<string> transformed = additionalTexts.Select(static (text, _) => text.Path);

    // transform each extracted path into something else
    IncrementalValuesProvider<string> prefixTransform = transformed.Select(static (path, _) => "prefix_" + path);

});
```

Note how `transformed` and `prefixTransform` are themselves an
`IncrementalValuesProvider`. They represent the outcome of the transformation
that will be applied, rather than the resulting data.

### Multi Valued providers

An `IncrementalValueProvider<T>` will always provide a single value, whereas an
`IncrementalValuesProvider<T>` may provide zero or more values. For example the
`CompilationProvider` will always produce a single compilation instance, whereas
the `AdditionalTextsProvider` will produce a variable number of values,
depending on how many additional texts where passed to the compiler.

Conceptually it is simple to think about the transformation of a single item
from an `IncrementalValueProvider<T>`: the single item has the selector function
applied to it which produces a single value of `TResult`.

For an `IncrementalValuesProvider<T>` however, this transformation is more
subtle. The selector function is applied multiple times, one to each item in the
values provider. The results of each transformation are then used to create the
values for the resulting values provider:

```ascii
                                          Select<TSource, TResult>
                                   .......................................
                                   .                   ┌───────────┐     .
                                   .   selector(Item1) │           │     .
                                   . ┌────────────────►│  Result1  ├───┐ .
                                   . │                 │           │   │ .
IncrementalValuesProvider<TSource> . │                 └───────────┘   │ . IncrementalValuesProvider<TResult>
          ┌───────────┐            . │                 ┌───────────┐   │ .        ┌────────────┐
          │           │            . │ selector(Item2) │           │   │ .        │            │
          │  TSource  ├──────────────┼────────────────►│  Result2  ├───┼─────────►│   TResult  │
          │           │            . │                 │           │   │ .        │            │
          └───────────┘            . │                 └───────────┘   │ .        └────────────┘
            3 items                . │                 ┌───────────┐   │ .            3 items
     [Item1, Item2, Item3]         . │ selector(Item3) │           │   │ .  [Result1, Result2, Result3]
                                   . └────────────────►│  Result3  ├───┘ .
                                   .                   │           │     .
                                   .                   └───────────┘     .
                                   .......................................
```

It is this item-wise transformation that allows the caching to be particularly
powerful in this model. Consider when the values inside
`IncrementalValueProvider<TSource>` change. Its likely that any given change
will only change one item at a time rather than the whole collection (for example
a user typing in an additional text only changes the given text, leaving the
other additional texts unmodified).

When this occurs the generator driver can compare the input items with the ones
that were used previously. If they are considered to be equal then the
transformations for those items can be skipped and the previously computed
versions used instead. See [Comparing Items](#comparing-items) for more details.

In the above diagram if `Item2` were to change we would execute the selector on
the modified value producing a new value for `Result2`. As `Item1`and `Item3`
are unchanged the driver is free to skip executing the selector and just use the
cached values of `Result1` and `Result3` from the previous execution.

### Select Many

In addition to the 1-to-1 transform shown above, there are also transformations
that produce batches of data. For instance a given transformation may want to
produce multiple values for each input. There are a set of `SelectMany` methods that allow a transformation of 1 to
many, or many to many items:

**1 to many:**

``` csharp
public static partial class IncrementalValueSourceExtensions
{
    public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, IEnumerable<TResult>> selector);
}
```

```ascii
                                         SelectMany<TSource, TResult>
                                   .......................................
                                   .                   ┌───────────┐     .
                                   .                   │           │     .
                                   .               ┌──►│  Result1  ├───┐ .
                                   .               │   │           │   │ .
 IncrementalValueProvider<TSource> .               │   └───────────┘   │ . IncrementalValuesProvider<TResult>
          ┌───────────┐            .               │   ┌───────────┐   │ .        ┌────────────┐
          │           │            . selector(Item)│   │           │   │ .        │            │
          │  TSource  ├────────────────────────────┼──►│  Result2  ├───┼─────────►│   TResult  │
          │           │            .               │   │           │   │ .        │            │
          └───────────┘            .               │   └───────────┘   │ .        └────────────┘
              Item                 .               │   ┌───────────┐   │ .            3 items
                                   .               │   │           │   │ .  [Result1, Result2, Result3]
                                   .               └──►│  Result3  ├───┘ .
                                   .                   │           │     .
                                   .                   └───────────┘     .
                                   .......................................
```

**Many to many:**

``` csharp
public static partial class IncrementalValueSourceExtensions
{
    public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, IEnumerable<TResult>> selector);
}
```

```ascii
                                             SelectMany<TSource, TResult>
                                   ...............................................
                                   .                        ┌─────────┐          .
                                   .                        │         │          .
                                   .                  ┌────►│ Result1 ├───────┐  .
                                   .                  │     │         │       │  .
                                   .                  │     └─────────┘       │  .
                                   .  selector(Item1) │                       │  .
                                   .┌─────────────────┘     ┌─────────┐       │  .
                                   .│                       │         │       │  .
 IncrementalValuesProvider<TSource>.│                 ┌────►│ Result2 ├───────┤  .    IncrementalValuesProvider<TResult>
          ┌───────────┐            .│                 │     │         │       │  .            ┌────────────┐
          │           │            .│ selector(Item2) │     └─────────┘       │  .            │            │
          │  TSource  ├─────────────┼─────────────────┤     ┌─────────┐       ├──────────────►│  TResult   │
          │           │            .│                 │     │         │       │  .            │            │
          └───────────┘            .│                 └────►│ Result3 ├───────┤  .            └────────────┘
             3 items               .│                       │         │       │  .               7 items
       [Item1, Item2, Item3]       .│ selector(Item3)       └─────────┘       │  .  [Result1, Result2, Result3, Result4, 
                                   .└─────────────────┐                       │  .      Result5, Result6, Result7 ]
                                   .                  │     ┌─────────┐       │  .
                                   .                  │     │         │       │  .
                                   .                  ├────►│ Result4 ├───────┤  .
                                   .                  │     │         │       │  .
                                   .                  │     └─────────┘       │  .
                                   .                  │     ┌─────────┐       │  .
                                   .                  │     │         │       │  .
                                   .                  ├────►│ Result5 ├───────┤  .
                                   .                  │     │         │       │  .
                                   .                  │     └─────────┘       │  .
                                   .                  │     ┌─────────┐       │  .
                                   .                  │     │         │       │  .
                                   .                  └────►│ Result6 ├───────┘  .
                                   .                        │         │          .
                                   .                        └─────────┘          .
                                   ...............................................
```

For example, consider a set of additional XML files that contain multiple
elements of the same type. The generator may want to treat each element as a
distinct item for generation, effectively splitting a single additional file
into multiple sub-items.

``` csharp
initContext.RegisterExecutionPipeline(context =>
{
    // get the additional text provider
    IncrementalValuesProvider<AdditionalText> additionalTexts = context.AdditionalTextsProvider;

    // extract each element from each additional file
    IncrementalValuesProvider<MyElementType> elements = additionalTexts.SelectMany(static (text, _) => /*transform text into an array of MyElementType*/);

    // now the generator can consider the union of elements in all additional texts, without needing to consider multiple files
    IncrementalValuesProvider<string> transformed = elements.Select(static (element, _) => /*transform the individual element*/);
}
```

### Where

Where allows the author to filter the values in a value provider by a given
predicate. Where is actually a specific form of select many, where each input
transforms to exactly 1 or 0 outputs. However, as it is such a common operation
it is provided as a primitive transformation directly.

``` csharp
public static partial class IncrementalValueSourceExtensions
{
    public static IncrementalValuesProvider<TSource> Where<TSource>(this IncrementalValuesProvider<TSource> source, Func<TSource, bool> predicate);
}
```

```ascii
                                          Select<TSource, TResult>
                                   .......................................
                                   .                   ┌───────────┐     .
                                   .   predicate(Item1)│           │     .
                                   . ┌────────────────►│   Item1   ├───┐ .
                                   . │                 │           │   │ .
IncrementalValuesProvider<TSource> . │                 └───────────┘   │ . IncrementalValuesProvider<TResult>
          ┌───────────┐            . │                                 │ .        ┌────────────┐
          │           │            . │ predicate(Item2)                │ .        │            │
          │  TSource  ├──────────────┼─────────────────X               ├─────────►│   TResult  │
          │           │            . │                                 │ .        │            │
          └───────────┘            . │                                 │ .        └────────────┘
             3 Items               . │                 ┌───────────┐   │ .           2 Items
                                   . │ predicate(Item3)│           │   │ .
                                   . └────────────────►│   Item3   ├───┘ .
                                   .                   │           │     .
                                   .                   └───────────┘     .
                                   .......................................
```

An obvious use case is to filter out inputs the generator knows it isn't
interested in. For example, the generator will likely want to filter additional
texts on file extensions:

```csharp
initContext.RegisterExecutionPipeline(context =>
{
    // get the additional text provider
    IncrementalValuesProvider<AdditionalText> additionalTexts = context.AdditionalTextsProvider;

    // filter additional texts by extension
    IncrementalValuesProvider<string> xmlFiles = additionalTexts.Where(static (text, _) => text.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
}
```

### Collect

When performing transformations on a value provider with multiple items, it can
often be useful to view the items as a single collection rather than one item at
a time. For this there is the `Collect` transformation.

`Collect` transforms an `IncrementalValuesProvider<T>` to an
`IncrementalValueProvider<ImmutableArray<T>>`. Essentially it transforms a multi-valued source
into a single value source with an array of all the items.

```csharp
public static partial class IncrementalValueSourceExtensions
{
    IncrementalValueProvider<ImmutableArray<TSource>> Collect<TSource>(this IncrementalValuesProvider<TSource> source);
}
```

```ascii
IncrementalValuesProvider<TSource>                IncrementalValueProvider<ImmutableArray<TSource>>
          ┌───────────┐                                  ┌─────────────────────────┐
          │           │          Collect<TSource>        │                         │
          │  TSource  ├─────────────────────────────────►│ ImmutableArray<TSource> │
          │           │                                  │                         │
          └───────────┘                                  └─────────────────────────┘
             3 Items                                             Single Item

              Item1                                         [Item1, Item2, Item3]
              Item2
              Item3
```

```csharp
initContext.RegisterExecutionPipeline(context =>
{
    // get the additional text provider
    IncrementalValuesProvider<AdditionalText> additionalTexts = context.AdditionalTextsProvider;

    // collect the additional texts into a single item
    IncrementalValueProvider<AdditionalText[]> collected = context.AdditionalTexts.Collect();

    // perform a transformation where you can access all texts at once
    var transform = collected.Select((texts, _) => /* ... */);
}

```

### Multi-path pipelines

The transformations described so far are all effectively single-path operations:
while there may be multiple items in a given provider, each transformation
operates on a single input value provider and produce a single derived output
provider.

While sufficient for simple operations, it is often necessary to combine the
values from multiple input providers or use the results of a transformation
multiple times. For this there are a set of transformations that split and
combine a single path of transformations into a multi-path pipeline.

### Split

It is possible to split the output of a transformations into multiple
parallel inputs. Rather than having a dedicated transformation this can be
achieved by simply using the same value provider as the input to multiple
transforms.

```ascii

                                                     IncrementalValueProvider<TResult>
                                                              ┌───────────┐
                                      Select<TSource,TResult> │           │
 IncrementalValueProvider<TSource>   ┌───────────────────────►│  TResult  │
           ┌───────────┐             │                        │           │
           │           │             │                        └───────────┘
           │  TSource  ├─────────────┤
           │           │             │
           └───────────┘             │                    IncrementalValuesProvider<TResult2>
                                     │                              ┌───────────┐
                                     │ SelectMany<TSource,TResult2> │           │
                                     └─────────────────────────────►│  TResult2 │
                                                                    │           │
                                                                    └───────────┘
```

Those transforms can then be used as the inputs to new single path transforms, independent of one another.

For example:

```csharp
initContext.RegisterExecutionPipeline(context =>
{
    // get the additional text provider
    IncrementalValuesProvider<AdditionalText> additionalTexts = context.AdditionalTextsProvider;

    // apply a 1-to-1 transform on each text, extracting the path
    IncrementalValuesProvider<string> transformed = additionalTexts.Select(static (text, _) => text.Path);

    // split the processing into two paths of derived data
    IncrementalValuesProvider<string> nameTransform = transformed.Select(static (path, _) => "prefix_" + path);
    IncrementalValuesProvider<string> extensionTransform = transformed.Select(static (path, _) => Path.ChangeExtension(path, ".new"));
}
```

`nameTransform` and `extensionTransform` produce different values for the same
set of additional text inputs. For example if there was an additional file
called `file.txt` then `nameTransform` would produce the string
`prefix_file.txt` where `extensionTransform` would produce the string
`file.new`.

When the value of the additional file changes, the subsequent values produced
may or may not differ. For example if the name of the additional file was
changed to `file.xml` then `nameTransform` would now produce `prefix_file.xml`
whereas `extensionTransform` would still produce `file.new`. Any child transform
with input from `nameTransform` would be re-run with the new value, but any
child of `extensionTransform` would use the previously cached version as it's
input hasn't changed.

### Combine

Combine is the most powerful, but also most complicated transformation. It
allows a generator to take two input providers and create a single unified
output provider.

**Single-value to single-value**:

```csharp
public static partial class IncrementalValueSourceExtensions
{
    IncrementalValueProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValueProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2);
}
```

When combining two single value providers, the resulting node is conceptually
easy to understand: a new value provider that contains a `Tuple` of the two
input items.

```ascii

IncrementalValueProvider<TSource1>
         ┌───────────┐
         │           │
         │  TSource1 ├────────────────┐
         │           │                │                                 IncrementalValueProvider<(TSource1, TSource2)>
         └───────────┘                │
          Single Item                 │                                          ┌────────────────────────┐
                                      │       Combine<TSource1, TSource2>        │                        │
            Item1                     ├─────────────────────────────────────────►│  (TSource1, TSource2)  │
                                      │                                          │                        │
IncrementalValueProvider<TSource2>    │                                          └────────────────────────┘
         ┌───────────┐                │                                                   Single Item
         │           │                │
         │  TSource2 ├────────────────┘                                                  (Item1, Item2)
         │           │
         └───────────┘
          Single Item

            Item2

```

**Multi-value to single-value:**

```csharp
public static partial class IncrementalValueSourceExtensions
{
    IncrementalValuesProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValuesProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2);
}
```

When combining a multi value provider to a single value provider, however, the
semantics are a little more complicated. The resulting multi-valued provider
produces a series of tuples: the left hand side of each tuple is the value
produced from the multi-value input, while the right hand side is always the
same single value from the single value provider input.

```ascii
 IncrementalValuesProvider<TSource1>
          ┌───────────┐
          │           │
          │  TSource1 ├────────────────┐
          │           │                │
          └───────────┘                │
             3 Items                   │                                IncrementalValuesProvider<(TSource1, TSource2)>
                                       │
            LeftItem1                  │                                          ┌────────────────────────┐
            LeftItem2                  │       Combine<TSource1, TSource2>        │                        │
            LeftItem3                  ├─────────────────────────────────────────►│  (TSource1, TSource2)  │
                                       │                                          │                        │
                                       │                                          └────────────────────────┘
 IncrementalValueProvider<TSource2>    │                                                  3 Items
          ┌───────────┐                │
          │           │                │                                            (LeftItem1, RightItem)
          │  TSource2 ├────────────────┘                                            (LeftItem2, RightItem)
          │           │                                                             (LeftItem3, RightItem)
          └───────────┘
           Single Item

            RightItem
```

**Multi-value to multi-value:**

As shown by the definitions above it is not possible to combine a multi-value
source to another multi-value source. The resulting cross join would potentially
contain a large number of values, so the operation is not provided by default.

Instead, an author can call `Collect()` on one of the input multi-value providers
to produce a single-value provider that can be combined as above.

```ascii
                                           IncrementalValuesProvider<TSource1>
                                                  ┌───────────┐
                                                  │           │
                                                  │ TSource1  ├──────────────┐
                                                  │           │              │
                                                  └───────────┘              │
                                                     3 Items                 │                                IncrementalValuesProvider<(TSource1, TSource2[])>
                                                                             │
                                                    LeftItem1                │                                          ┌────────────────────────┐
                                                    LeftItem2                │       Combine<TSource1, TSource2[]>      │                        │
                                                    LeftItem3                ├─────────────────────────────────────────►│  (TSource1, TSource2)  │
                                                                             │                                          │                        │
                                                                             │                                          └────────────────────────┘
IncrementalValuesProvider<TSource2>     IncrementalValueProvider<TSource2[]> │                                                  3 Items
         ┌───────────┐                           ┌────────────┐              │
         │           │      Collect<TSource2>    │            │              │                               (LeftItem1, [RightItem1, RightItem2, RightItem3])
         │  TSource2 ├───────────────────────────┤ TSource2[] ├──────────────┘                               (LeftItem2, [RightItem1, RightItem2, RightItem3])
         │           │                           │            │                                              (LeftItem3, [RightItem1, RightItem2, RightItem3])
         └───────────┘                           └────────────┘
            3 Items                                Single Item

          RightItem1                 [RightItem1, RightItem2, RightItem3]
          RightItem2
          RightItem3
```

With the above transformations the generator author can now take one or more
inputs and combine them into a single source of data. For example:

```csharp
initContext.RegisterExecutionPipeline(context =>
{
    // get the additional text provider
    IncrementalValuesProvider<AdditionalText> additionalTexts = context.AdditionalTextsProvider;

    // combine each additional text with the parse options
    IncrementalValuesProvider<(AdditionalText, ParseOptions)> combined = context.AdditionalTextsProvider.Combine(context.ParseOptionsProvider);

    // perform a transform on each text, with access to the options
    var transformed = combined.Select((pair, _) => 
    {
        AdditionalText text = pair.Left;
        ParseOptions parseOptions = pair.Right;

        // do the actual transform ...
    });
}
```

If either of the inputs to a combine change, then subsequent transformation will
re-run. However, the caching is considered on a pairwise basis for each output
tuple. For instance, in the above example, if only additional text changes the
subsequent transform will only be run for the text that changed. The other text
and parse options pairs are skipped and their previously computed value are
used. If the single value changes, such as the parse options in the example,
then the transformation is executed for every tuple.

### SyntaxValueProvider

Syntax Nodes are not available directly through a value provider. Instead, a
generator author uses the special `SyntaxValueProvider` (provided via the
`IncrementalGeneratorInitializationContext.SyntaxProvider`) to create a
dedicated input node that instead exposes a sub-set of the syntax they are
interested in. The syntax provider is specialized in this way to achieve a
desired level of performance.

**CreateSyntaxProvider**:

Currently the provider exposes a single method `CreateSyntaxProvider` that
allows the author to construct an input node.

```csharp
    public readonly struct SyntaxValueProvider
    {
        public IncrementalValuesProvider<T> CreateSyntaxProvider<T>(Func<SyntaxNode, CancellationToken, bool> predicate, Func<GeneratorSyntaxContext, CancellationToken, T> transform);
    }
```

Note how this takes _two_ lambda parameters: one that examines a `SyntaxNode` in
isolation, and a second one that can then use the `GeneratorSyntaxContext` to
access a semantic model and transform the node for downstream usage.

It is because of this split that performance can be achieved: as the driver is
aware of which nodes are chosen for examination, it can safely skip the first
`predicate` lambda when a syntax tree remains unchanged. The driver will still
re-run the second `transform` lambda even for nodes in unchanged files, as a
change in one file can impact the semantic meaning of a node in another file.

Consider the following syntax trees:

```csharp
// file1.cs
public class Class1
{
    public int Method1() => 0;
}

// file2.cs
public class Class2
{
    public Class1 Method2() => null;
}

// file3.cs
public class Class3 {}
```

As an author I can make an input node that extracts the return type information 

```csharp
initContext.RegisterExecutionPipeline(context =>
{
    // create a syntax provider that extracts the return type kind of method symbols
    var returnKinds = context.SyntaxProvider.CreateSyntaxProvider((n, _) => n is MethodDeclarationSyntax,
                                                                  (n, _) => ((IMethodSymbol)n.SemanticModel.GetDeclaredSymbol(n.Node)).ReturnType.Kind);
}
```

Initially the `predicate` will run for all syntax nodes, and select the two
`MethodDeclarationSyntax` nodes `Method1()` and `Method2()`. These are then
passed to the `transform` where the semantic model is used to obtain the method
symbol and extract the kind of the return type for the method. `returnKinds`
will contain two values, both `NamedType`.

Now imagine that `file3.cs` is edited:

```csharp
// file3.cs
public class Class3 {
    public int field;
}
```

The `predicate` will only be run for syntax nodes inside `file3.cs`, and will
not return any as it still doesn't contain any method symbols. The `transform`
however will still be run again for the two methods from `Class1` and `Class2`.

To see why it was necessary to re-run the `transform` consider the following
edit to `file1.cs` where we change the classes name:

```csharp
// file1.cs
public class Class4
{
    public int Method1() => 0;
}
```

The `predicate` will be re-run for `file1.cs` as it has changed, and will pick
out the method symbol `Method1()` again.  Next, because the `transform` is
re-run for _all_ the methods, the return type kind for `Method2()` is correctly
changed to `Error` as `Class1` no longer exists.

Note that we didn't need to run the `predicate` over for nodes in `file2.cs`
even though they referenced something in `file1.cs`. Because the first check is
purely _syntactic_ we can be sure the results for `file2.cs` would be the same.

While it may seem unfortunate that the driver must run the `transform` for all
selected syntax nodes, if it did not it could up producing incorrect data
due to cross file dependencies. Because the initial syntactic check
allows the driver to substantially filter the number of nodes on which the
semantic checks have to be re-run, significantly improved performance
characteristics are still observed when editing a syntax tree.

## Outputting values

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

## Simple example

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

## Caching

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

### Comparing Items

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