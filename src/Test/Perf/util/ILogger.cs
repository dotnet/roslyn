namespace Roslyn.Test.Performance.Utilities
{
    internal interface ILogger
    {
        void Log(string v);
        void Flush();
    }
}
