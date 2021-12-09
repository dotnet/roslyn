using System;

namespace Metalama.Compiler
{
    public static class Intrinsics
    {
        private static InvalidOperationException NewInvalidOperationException() =>
            new InvalidOperationException("Code calling this method has to be compiled by the Metalama compiler.");

        public static RuntimeMethodHandle GetRuntimeMethodHandle(string documentationId) => throw NewInvalidOperationException();
        public static RuntimeFieldHandle GetRuntimeFieldHandle(string documentationId) => throw NewInvalidOperationException();
        public static RuntimeTypeHandle GetRuntimeTypeHandle(string documentationId) => throw NewInvalidOperationException();
    }
}
