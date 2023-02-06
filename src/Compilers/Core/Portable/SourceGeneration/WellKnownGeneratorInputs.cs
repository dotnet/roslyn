// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Well known incremental generator input step names.
    /// </summary>
    public static class WellKnownGeneratorInputs
    {
        public const string Compilation = nameof(Compilation);

        internal const string CompilationOptions = nameof(CompilationOptions);

        public const string ParseOptions = nameof(ParseOptions);

        public const string AdditionalTexts = nameof(AdditionalTexts);

        public const string AnalyzerConfigOptions = nameof(AnalyzerConfigOptions);

        public const string MetadataReferences = nameof(MetadataReferences);
    }
}
