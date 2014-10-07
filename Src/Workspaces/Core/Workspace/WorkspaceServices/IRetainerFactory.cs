namespace Roslyn.Services.Host
{
    public interface IRetainerFactory<T> : IWorkspaceService
    {
        IRetainer<T> CreateRetainer(T value);

        void ClearPool();
    }
}