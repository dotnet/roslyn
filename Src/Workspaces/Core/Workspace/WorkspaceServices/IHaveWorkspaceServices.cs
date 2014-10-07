namespace Roslyn.Services.Host
{
    /// <summary>
    /// Provides access to all available workspace services.
    /// </summary>
    public interface IHaveWorkspaceServices
    {
        IWorkspaceServiceProvider WorkspaceServices { get; }
    }
}
