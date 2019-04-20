namespace Microsoft.CodeAnalysis.Options
{
    /// <remarks>
    /// This interface exists so the Visual Studio workspace can create the .editorconfig provider,
    /// despite that the provider code currently lives in a Dev15-targeting assembly but
    /// the workspace itself is still in a Dev14-targeting assembly. Once those have merged,
    /// this can go away.
    /// </remarks>
    interface IDocumentOptionsProviderFactory
    {
        IDocumentOptionsProvider TryCreate(Workspace workspace);
    }
}
