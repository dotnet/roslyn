## API

The API of RoslynEx consists of the following 4 types in the `RoslynEx` namespace:

### `ISourceTransformer`

```c#
/// <summary>
/// The interface required to implement a source transformer.
/// </summary>
public interface ISourceTransformer
{
    /// <summary>
    /// Called to perform source transformation.
    /// </summary>
    Compilation Execute(TransformerContext context);
}
```

The interfece to be implemented by any source transformer. Its only method, `Execute`, is invoked by the compiler to execute the transformer, passing it all the information it needs (especially the input `Compilation`) in the `context` parameter. The returned `Compilation` should be the input compilation with the required changes applied to it.

Source transformers also have to marked with the `[Transformer]` attribute.

### `TransformerAttribute`

```c#
/// <summary>
/// Place this attribute onto a type to cause it to be considered a source transformer.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TransformerAttribute : Attribute { }
```

Attribute that has to be applied to any `ISourceTransformer`.

### `TransformerContext`

```c#
public class TransformerContext
{
    /// <summary>
    /// Get the current <see cref="Compilation"/> at the time of execution.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// Allows access to global options provided by an analyzer config,
    /// which can in turn come from the csproj file.
    /// </summary>
    public AnalyzerConfigOptions GlobalOptions { get; }

    /// <summary>
    /// Adds a managed resource to the assembly.
    /// </summary>
    public void AddManifestResource(ResourceDescription resource);

    /// <summary>
    /// Adds a <see cref="Diagnostic"/> to the user's compilation.
    /// </summary>
    /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
    /// <remarks>
    /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
    /// </remarks>
    public void ReportDiagnostic(Diagnostic diagnostic);
}
```

Contains data provided as an input to an `ISourceTransformer`, along with methods that can be used by a transformer to perform extra actions.

### `TransformerOrderAttribute`

```c#
/// <summary>
/// Applying this attribute on an assembly specifies the execution order of transformers it knows about, including transformers inside the assembly itself.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class TransformerOrderAttribute : Attribute
{
    /// <summary>
    /// Array of namespace-qualified names of transformer types. Their order specifies the execution order of the corresponding transformers.
    /// </summary>
    public string[] TransformerNames { get; }

    /// <param name="transformerNames">Namespace-qualified names of transformer types. Their order specifies the execution order of the corresponding transformers.</param>
    public TransformerOrderAttribute(params string[] transformerNames);
}
```

The `[TransformerOrder]` attribute can be applied to an assembly containing transformers to specify the execution order of transformers within it, and possibly also other transformers it knows about.

The order of all transformers has to be fully specified. If the authors of used transformers did not do that, a user will have to specify the `RoslynExTransformerOrder` property in their csproj.