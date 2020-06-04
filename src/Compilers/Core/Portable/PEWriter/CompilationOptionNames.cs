using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.PEWriter
{
    /// <summary>
    /// Names for compilation options that get embedded as debug information
    /// in the PDB as key-value pairs.
    /// </summary>
    internal static class CompilationOptionNames
    {
        public const string CompilerVersion = "compiler-version";
        public const string FallbackEncoding = "fallback-encoding";
        public const string DefaultEncoding = "default-encoding";
        public const string PortabilityPolicy = "portability-policy";
        public const string RuntimeVersion = "runtime-version";
        public const string Optimization = "optimization";
        public const string Checked = "checked";
        public const string LanguageVersion = "language-version";
        public const string Unsafe = "unsafe";
        public const string Nullable = "nullable";
        public const string Define = "define";
        public const string Strict = "strict";
    }
}
