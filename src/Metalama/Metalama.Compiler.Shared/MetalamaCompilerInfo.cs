namespace Metalama.Compiler
{
    public static class MetalamaCompilerInfo
    {
        /// <summary>
        /// Ensures that the <c>Metalama.Compiler.Interface</c> assembly is loaded.
        /// </summary>
        public static void EnsureInitialized() { }

        /// <summary>
        /// Returns a value indicating whether the <c>Metalama.Compiler</c> process is active, or <c>false</c>
        /// if the current <c>Metalama.Compiler.Interface</c> assembly is the reference assembly.
        /// </summary>
        public static bool IsActive =>
#if METALAMA_COMPILER_INTERFACE
            false;
#else
            true;
#endif

    }
}
