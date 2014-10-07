namespace Roslyn.Services.Host
{
    public interface IBackgroundCompilerListener
    {
        void OnProjectCompilationStarted(ISolution solution, ProjectId projectId);
        void OnProjectCompilationFinished(ISolution solution, ProjectId projectId);
    }
}