# Source Generators Cookbook

## Summary

This document aims to be a guide to help the creation of source generators by providing a series of reusable patterns.
It also aims to set out what types of generators are possible under the current design and what is explictly out of scope.

> **Note**: The design for the source generator proposal is still under review. This document uses only one possible syntax, and
> it is expected to change without notice as the feature evolves.

## Proposal

It is worth breifly setting out the design goals of generators here. For a more complete design see the [full design document](source-generators.md)

- Generators produce one or more strings that represent C# source code to be added to the compilation.
- Explicitly _additive_ only. Generators can add new code to a compilation but may **not** modify existing user code.
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

### Code-not-source generators

There are many post-processing tasks that users perform on their assemblies today, which here we define as 'code-not-source' generation. These include, but
are not limited to:

- Optimization
- Logging injection
- IL Weaving
- Call site re-writing

While these techniques have many valuable use cases, they do not fit into the idea of *source* generation. They are, by defintion, code altering operations
which is explicitly ruled out by the source generation proposal.

There are already well supported tools and techniques for acheiving these kinds of operations, and the source generators proposal is not aimed at replacing them.

## Designs

This section is broken down by user scenarios, with general solutions listed first, and more specific examples later on.

### Generated class

**User scenario:** As a generator author I want to be able to create a class that can be referenced from user code.

**Solution:** Generate the class based on the data available in the compilation, and add the new source to compilation.

**Example:**

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

### Single file transformation

**User scenario:** As a generator author I want to be able to transform a single file into a C# representation.

**Solution:** Use the additional files property of the `SourceGeneratorContext` to retreive the contents of the file, convert it to the C# representation and return it.

**Example:**

```csharp
public class FileTransformGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // find anything that matches our files
            var myFiles = context.AnalyzerOptions.AdditionalFiles.Where(at => at.Path.EndsWith(".myextension"));
            foreach (var file in myFiles)
            {
                var content = file.GetText(context.CancellationToken);

                // do some transforms based on the file contenxt

                string output = "namespace MyNS { public class MyClass { } }";
                var sourceText = SourceText.From(output);

                context.AddSource($"{file.Name}generated.cs", sourceText);
            }
        }
    }
```

### Augment user code

**User scenario:** As a generator author I want to be able to augment a users code with new functionality.

**Solution:** Require the user to make the class you want to augment be a `partial class`, and mark it with e.g. a unique attribute, or name.
Look for any classes marked for generation and generate the a matching `partial class` that contains the additional functionality.

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
However we can instead take advantage of explict fields and instead of *editing* the users properties, directly provide them for listed fields.

**Example:**

Given a user class such as:

```csharp
public partial class UserClass
{
    [AutoNotify]
    private bool _boolProp;

    [AutoNotify(propertyName: "Count")]
    private int _intProp;
}
```

A generator could produce the following:

```csharp
using System.ComponentModel;

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

### Auto interface implementation
