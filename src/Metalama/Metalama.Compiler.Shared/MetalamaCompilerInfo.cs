namespace Metalama.Compiler
{
    public static class MetalamaCompilerInfo
    {
        public static bool IsActive =>
#if METALAMA_COMPILER_INTERFACE
            false;
#else
            true;
#endif

    }
}
