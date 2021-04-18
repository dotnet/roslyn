// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.Cci
{
    /// <summary>
    /// Names for compilation options that get embedded as debug information
    /// in the PDB as key-value pairs.
    /// </summary>
    /// <remarks>
    /// REMOVAL OR CHANGES TO EXISTING VALUES IS CONSIDERED A BREAKING CHANGE FOR PDB FORMAT
    /// </remarks>
    internal static class CompilationOptionNames
    {
        public const string CompilationOptionsVersion = "version";
        public const string CompilerVersion = "compiler-version";
        public const string FallbackEncoding = "fallback-encoding";
        public const string DefaultEncoding = "default-encoding";
        public const string PortabilityPolicy = "portability-policy";
        public const string RuntimeVersion = "runtime-version";
        public const string Platform = "platform";
        public const string Optimization = "optimization";
        public const string Checked = "checked";
        public const string Language = "language";
        public const string LanguageVersion = "language-version";
        public const string Unsafe = "unsafe";
        public const string Nullable = "nullable";
        public const string Define = "define";
        public const string SourceFileCount = "source-file-count";
        public const string EmbedRuntime = "embed-runtime";
        public const string GlobalNamespaces = "global-namespaces";
        public const string RootNamespace = "root-namespace";
        public const string OptionStrict = "option-strict";
        public const string OptionInfer = "option-infer";
        public const string OptionExplicit = "option-explicit";
        public const string OptionCompareText = "option-compare-text";
        public const string OutputKind = "output-kind";
    }
}
