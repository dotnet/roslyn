using Roslyn.Compilers;

namespace Roslyn.Services.Host
{
    [ExportWorkspaceServiceFactory(typeof(IRetainerFactory<IText>), WorkspaceKind.Any)]
    internal partial class TextRetainerFactoryFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new TextRetainerFactory();
        }
    }
}
