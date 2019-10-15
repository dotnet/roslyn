using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingExceptionUtilitesWrapper
    {
        public static Exception Unreachable => ExceptionUtilities.Unreachable;
    }
}
