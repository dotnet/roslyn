namespace Roslyn.Test.Performance.Utilities
{
    public interface ILogger
    {
        void Log(string v);
        void Flush();
    }
}
