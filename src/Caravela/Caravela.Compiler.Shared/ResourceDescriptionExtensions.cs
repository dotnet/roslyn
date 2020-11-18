using System;
using System.IO;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Caravela.Compiler
{
    public static class ResourceDescriptionExtensions
    {
#if CARAVELA_COMPILER_INTERFACE
        private static InvalidOperationException NewInvalidOperationException() => new InvalidOperationException("This operation works only inside Caravela.");
#endif

        public static string GetResourceName(this ResourceDescription resource) =>
#if CARAVELA_COMPILER_INTERFACE
            throw NewInvalidOperationException();
#else
            resource.ResourceName;
#endif

        public static string? GetFileName(this ResourceDescription resource) =>
#if CARAVELA_COMPILER_INTERFACE
            throw NewInvalidOperationException();
#else
            resource.FileName;
#endif

        public static bool IsPublic(this ResourceDescription resource) =>
#if CARAVELA_COMPILER_INTERFACE
            throw NewInvalidOperationException();
#else
            resource.IsPublic;
#endif

        public static Stream GetData(this ResourceDescription resource) =>
#if CARAVELA_COMPILER_INTERFACE
            throw NewInvalidOperationException();
#else
            resource.DataProvider();
#endif
    }
}
