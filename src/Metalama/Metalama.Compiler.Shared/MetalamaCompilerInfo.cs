namespace Caravela.Compiler
{
    public static class CaravelaCompilerInfo
    {
        public static bool IsActive =>
#if CARAVELA_COMPILER_INTERFACE
            false;
#else
            true;
#endif

    }
}
