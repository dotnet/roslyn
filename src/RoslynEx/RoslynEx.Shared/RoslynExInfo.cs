namespace RoslynEx
{
    public static class RoslynExInfo
    {
        public static bool IsActive =>
#if ROSLYNEX_INTERFACE
            false;
#else
            true;
#endif

    }
}
