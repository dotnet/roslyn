using Metalama.Compiler.Services;

namespace Metalama.Compiler;

/// <summary>
/// The interface required to implement a source transformer.
/// </summary>
public interface ISourceTransformer
{
    /// <summary>
    /// Called to perform source transformation.
    /// </summary>
    void Execute(TransformerContext context);
}

public interface ISourceTransformerWithServices : ISourceTransformer
{
    ServicesHolder? InitializeServices(InitializeServicesContext context);
}
