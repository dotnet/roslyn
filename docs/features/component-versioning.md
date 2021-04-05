# Roslyn Component Versioning

This document proposes a scheme that allows the authors of Roslyn Components (Analyzers, Generators etc.) to specify the
set of features the compiler is required to provide in order to load the specified component.

## Rationale

Today, as an author of a Roslyn component, there is no way to declare a particular version of roslyn that you depend on. For instance
a Generator that requires the _PostInitialization_ feature will fail to load in compilers that did not support it, but only by
virtue of the roslyn version it references containing types not available in the older compiler.

With analyzers this was generally less of a problem, as the API shape tended to be more stable, and Syntax is generally additive. Newer
analyzers could continue to work in older compilers as the code paths referencing new syntax types would not be hit. However, newer APIs, such as those introduced for nullable analysis, still have the load time failure issue.

Although blocked from loading, the current user experience of using a component targeting a newer compiler version is sub-par and opaque as to the action the
user needs to take to resolve it.

Instead, it would be ideal if the author had a way to specify a particular feature of the compiler that is required; older compilers (at least the
ones going forward from now on) would be able to detect this feature requirement at load time, and specify a specific warning that this version of the compiler cannot load the component.

## Proposal

We introduce a new Attribute `[RoslynFeature(...)]` that a component author can apply to their component definition. The attribute would take a required string parameter, which is a comma separated list of features the component requires in order to load.

`[RoslynFeature("Generator.PostInitialization, LanguageVersion9")]`

_(__Alternative__: this could directly be an array, rather than separated list?)_

These features would be defined in the compiler, and passed to the component loader. The component loader would check that the requirements are met, and issue a warning when unable to meet them. The requirements would be accessible via API to enable the author to strongly reference the requirement (either an Enum or set of readonly properties).

The exact requirement list is TBD, but would likely include language version and specific component features we introduce going forward. These features could be dynamically calculated by the compiler: for instance, whilst still in preview a feature might only be enabled if the selected language version is also preview.

While we'd allow authors to key on language version, we wouldn't be changing any of the existing functionality in the compiler to use these requirements: it would purely be for making decisions about whether to load the component or not.

Features would also be able to represent super-sets of other features, for instance `LanguageVersion9` would imply `LanguageVersion1..8`. Components without the attribute would always be unconditionally loaded, with the same behavior as today.

### Selective disabling

A second use case for this feature would be to selective _disable_ the loading of certain components.

Consider the migration of an `ISourceGenerator` to `IIncrementalGenerator`: the author may wish to create a new `IIncrementalGenerator`, but still fall back to the original `ISourceGenerator` in older compilers. The above proposal allows the user to *enable* the incremental generator when supported, but conversely they also need a mechanism to *disable* the `ISourceGenerator`.

The `[RoslynFeature]` attribute would support a second, optional parameter that would cause the component to **not** load when the given feature is present.

`[RoslynFeature("", disableWhen: "IncrementalGenerator")]`

This would allow the author to provide a pair of components that enable/disable depending on features, allowing a smoother upgrade path.

### Disable Warning

In the above case, the author would not want the compiler to warn about either generator not loading, because a fallback is in place in both cases. The attribute would have a third optional parameter that would suppress the production of a warning:

`[RoslynFeature("", warnOnNotLoaded: false)]`

## Alternative Approaches

There are a couple of obvious alternatives to the above proposal:

### Version numbers

Instead of being feature based, we could instead allow the author to specify the Minimum (and/or maximum) roslyn version that is required for the compiler.

This has two downsides: it requires the author to explicitly know the version in which features are introduced; more importantly it doesn't allow us to move features into preview. 

Imagine a feature that is introduced into roslyn version 3.8, but under a preview flag. How would a component specify that it should be loaded only when the specified feature is actually enabled?

### MSBuild only support

We could also define the features in the MSBuild targets rather than in the compiler. Component authors could then specify custom targets in their NuGet packages to selectively pass the component to the compiler or not.

This works, but does require the author to split up components by assembly, and also provide the custom targets. This seems like an unnecessary burden on authors and seems to add a non-trivial amount of complexity to the authoring story.

## Extensions

Given that we allow authors to trigger on compiler provided features, we could imagine a world where we provide more fine grained features, such as:

- IDE vs. Batch compiler
- MSBuild properties / items?
- References etc.

It feels like aside from the IDE / Batch scenario these can already be achieved by programmatic lookup during execution of the component to selectively enable/disable, but there may be some performance benefit to allow skipping loading the component altogether.

## Open Questions

- Attribute Name
- List of features
- Comma separated vs. Arrays  
  - Array is nicer for multiples, single string is easier for one required features  