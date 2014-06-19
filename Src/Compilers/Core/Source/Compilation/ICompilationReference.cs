namespace Roslyn.Compilers.Common
{
    public interface ICompilationReference
    {
        CommonCompilation Compilation { get; }
        bool Equals(ICompilationReference obj);
    }
}