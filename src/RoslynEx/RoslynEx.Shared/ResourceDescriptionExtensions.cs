using System;
using System.IO;
using Microsoft.CodeAnalysis;

#nullable enable

namespace RoslynEx
{
    public static class ResourceDescriptionExtensions
    {
#if ROSLYNEX_INTERFACE
        private static InvalidOperationException NewInvalidOperationException() => new InvalidOperationException("This operation works only inside RoslynEx.");
#endif

        public static string GetResourceName(this ResourceDescription resource) =>
#if ROSLYNEX_INTERFACE
            throw NewInvalidOperationException();
#else
            resource.ResourceName;
#endif

        public static string? GetFileName(this ResourceDescription resource) =>
#if ROSLYNEX_INTERFACE
            throw NewInvalidOperationException();
#else
            resource.FileName;
#endif

        public static bool IsPublic(this ResourceDescription resource) =>
#if ROSLYNEX_INTERFACE
            throw NewInvalidOperationException();
#else
            resource.IsPublic;
#endif

        public static Stream GetData(this ResourceDescription resource) =>
#if ROSLYNEX_INTERFACE
            throw NewInvalidOperationException();
#else
            resource.DataProvider();
#endif
    }
}
