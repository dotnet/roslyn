namespace Roslyn.Compilers.Scripting
{
    /// <summary>
    /// A sequence of chained compilations. Free variables in a compilation bind to symbols of the previous compilations. 
    /// </summary>
    internal sealed class CompilationChain
    {
        // TODO: Top-level symbol cache to speed up top-level symbol lookup

        internal CompilationChain()
        {
        }
    }
}