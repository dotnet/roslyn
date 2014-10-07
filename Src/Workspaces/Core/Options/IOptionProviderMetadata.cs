using System;

namespace Roslyn.Services
{
    /// <summary>
    /// required metadata for IOptionsProvider
    /// </summary>
    public interface IOptionProviderMetadata
    {
        string Version { get; }
        string Name { get; }
        Type FeatureOptionsSerializer { get; }
    }
}
