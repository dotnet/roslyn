# Source Generators Cookbook

## Summary

> **Note**: The design for the source generator proposal is still under review. This document uses only one possible syntax, and
> it is expected to change without notice as the feature evolves.

This document aims to be a guide to help the creation of source generators by providing a series of guidelines for common patterns.
It also aims to set out what types of generators are possible under the current design, and what is expected to be explicitly out 
of scope in the final design of the shipping feature. 

**This document expands on the details in the [full design document](source-generators.md), please ensure you have read that first.**

## Proposal

As a reminder, the high level design goals of source generators are:

- Generators produce one or more strings that represent C# source code to be added to the compilation.
- Explicitly _additive_ only. Generators can add new source code to a compilation but may **not** modify existing user code.
- Can produce diagnostics. When unable to generate source, the generator can inform the user of the problem.
- May access _additional files_, that is, non-C# source texts.
- Run _un-ordered_, each generator will see the same input compilation, with no access to files created by other source generators.
- A user specifies the generators to run via list of assemblies, much like analyzers.

## Out of scope designs

We will briefly look at the non-solvable problems as examples of the kind of problems source generators are *not* designed to solve:

### Language features

Source generators are not designed to replace new language features: for instance one could imagine [records](records.md) being implemented as a source generator
that converts the specified syntax to a compilable C# representation.

We explicitly consider this to be an anti-pattern; the language will continue to evolve and add new features, and we don't expect source generators to be
a way to enable this. Doing so would create new 'dialects' of C# that are incompatible with the compiler without generators. Further, because generators,
by design, cannot interact with each other, language features implemented in this way would quickly become incompatible with other additions to the language.

### Code rewriting

There are many post-processing tasks that users perform on their assemblies today, which here we define broadly as 'code rewriting'. These include, but
are not limited to:

- Optimization
- Logging injection
- IL Weaving
- Call site re-writing

While these techniques have many valuable use cases, they do not fit into the idea of *source generation*. They are, by definition, code altering operations
which are explicitly ruled out by the source generators proposal.

There are already well supported tools and techniques for achieving these kinds of operations, and the source generators proposal is not aimed at replacing them.

## Conventions

TODO: List a set of general conventions that apply to all designs below. E.g. Re-using namespaces, generated file names etc.

## Designs

This section is broken down by user scenarios, with general solutions listed first, and more specific examples later on.

### Generated class

**User scenario:** As a generator author I want to be able to add a type to the compilation, that can be referenced by the users code.

**Solution:** Have the user write the code as if the type was already present. Generate the missing type based on information available in the compilation.


**Example:**

Given the following user code:

```csharp
public partial class UserClass
{
    public void UserMethod()
    {
        // call into a generated method
        GeneratedNamespace.GeneratedClass.GeneratedMethod();
    }
}
```

Create a generator that will create the missing type when run:

```csharp
public class CustomGenerator : ISourceGenerator
{
    public void Execute(SourceGeneratorContext context)
    {
        context.AddSource("myGeneratedFile.cs", SourceText.From($@"
namespace GeneratedNamespace
{
    public class GeneratedClass
    {
        public static void GeneratedMethod()
        {
            // generated code
        }
    }
}"));
    }
}
```


### Additional file transformation

**User scenario:** As a generator author I want to be able to transform an external non-C# file into an equivalent C# representation.

**Solution:** Use the additional files property of the `SourceGeneratorContext` to retrieve the contents of the file, convert it to the C# representation and return it.

**Example:**

```csharp
public class FileTransformGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // find anything that matches our files
            var myFiles = context.AnalyzerOptions.AdditionalFiles.Where(at => at.Path.EndsWith(".xml"));
            foreach (var file in myFiles)
            {
                var content = file.GetText(context.CancellationToken);

                // do some transforms based on the file context
                string output = MyXmlToCSharpCompiler.Compile(content);

                var sourceText = SourceText.From(output);

                context.AddSource($"{file.Name}generated.cs", sourceText);
            }
        }
    }
```

### Augment user code

**User scenario:** As a generator author I want to be able to augment a users code with new functionality.

**Solution:** Require the user to make the class you want to augment be a `partial class`, and mark it with e.g. a unique attribute, or name.
Look for any classes marked for generation and generate a matching `partial class` that contains the additional functionality.

**Example:**

```csharp
public partial class UserClass
{
    public void UserMethod()
    {
        // call into a method inside the class
        this.GeneratedMethod();
    }
}
```

```csharp
public class AugmentingGenerator : ISourceGenerator
{
    public void Execute(SourceGeneratorContext context)
    {
        var syntaxTrees = context.Compilation.SyntaxTrees;
        foreach (var syntaxTree in syntaxTrees)
        {
                // find the class to augment
            var classToAugment = syntaxTree.GetRoot().DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Name.ToString() == "UserClass")
                .Single();

            var sourceText = SourceText.From($@"
public partial class {classToAugment.Identifier.ToString()}
{
private void GeneratedMethod()
{
    // generated code
}
}");
            context.AddSource("myGeneratedFile.cs", sourceText);
        }
    }
}

```

### Participate in the IDE experience

**User scenario:** As a generator author I want to be able to interactively regenerate code as the user is editing files.

**Solution:** We expect there to be an an opt-in set of interactive interfaces that can be implemented to allow for progressively more complex generation strategies.
It is anticipated there will be a mechanism for providing symbol mapping for lighting up features such a 'Find all references'.

```csharp
public class InteractiveGenerator : ISourceGenerator, IAdditionalFilesChangedGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // generators must always support a total generation pass
        }

        public void OnAdditionalFilesChanged(AdditionalFilesChangedContext context)
        {
            // determine which file changed, and if it affects this generator
            // regenerate only the parts that are affected by this change.
        }
    }
```

### INotifyPropertyChanged

**User scenario:** As a generator author I want to be able to implement the `INotifyPropertyChanged` pattern automatically for a user.

**Solution:** The design tenant 'Explicitly additive only' seems to be at direct odds with the ability to implement this, and appears to call for user code modification.
However we can instead take advantage of explicit fields and instead of *editing* the users properties, directly provide them for listed fields.

**Example:**

Given a user class such as:

```csharp
using AutoNotify;

public partial class UserClass
{
    [AutoNotify]
    private bool _boolProp;

    [AutoNotify(PropertyName = "Count")]
    private int _intProp;
}
```

A generator could produce the following:

```csharp
using System;
using System.ComponentModel;

namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}


public partial class UserClass : INotifyPropertyChanged
{
    public bool BoolProp
    {
        get => _boolProp;
        set
        {
            _boolProp = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserBool"));
        }
    }

    public int Count
    {
        get => _intProp;
        set
        {
            _intProp = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}

```

### Serialization

TODO:

### Auto interface implementation

TODO:

## Open Issues

This section track other miscellaneous TODO items:

**NuGet Packaging**: How does a user package a source generator via nuget?

**Framework targets**: May want to mention if we have framework requirements for the generators, e.g. they must target netstandard2.0 or similar.

**Conventions**: (See TODO in [conventions](#conventions) section above). What standard conventions are we suggesting to users?

**Partial methods**: Should we provide a scenario that includes partial methods? Reasons:
 - Control of name. The developer can control the name of the member
 - Generation is optional/depending on other state. Based on other information, generator might decide that the method isn't needed.


