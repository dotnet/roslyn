namespace Roslyn.Services.Host
{
    /// <summary>
    /// Provides access to all available workspace services.
    /// </summary>
    public interface IHaveLanguageServiceProviderFactory
    {
        ILanguageServiceProviderFactory LanguageServicesFactory { get; }
    }
}