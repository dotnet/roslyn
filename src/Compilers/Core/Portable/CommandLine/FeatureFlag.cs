// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis;

// TODO2 rename file after review completes
internal static class Feature
{
    internal const string Strict = "strict";
    internal const string UseLegacyStrongNameProvider = "UseLegacyStrongNameProvider";
    internal const string EnableGeneratorCache = "enable-generator-cache";
    internal const string PdbPathDeterminism = "pdb-path-determinism";
    internal const string DebugDeterminism = "debug-determinism";
    internal const string DebugAnalyzers = "debug-analyzers";
    internal const string RuntimeAsync = "runtime-async";
    internal const string PEVerifyCompat = "peverify-compat";
    internal const string FileBasedProgram = "FileBasedProgram";
    internal const string NullablePublicOnly = "nullablePublicOnly";
    internal const string RunNullableAnalysis = "run-nullable-analysis";
    internal const string InterceptorsNamespaces = "InterceptorsNamespaces";
    internal const string NoRefSafetyRulesAttribute = "noRefSafetyRulesAttribute";
    internal const string DisableLengthBasedSwitch = "disable-length-based-switch";
    internal const string ExperimentalDataSectionStringLiterals = "experimental-data-section-string-literals";

    // For testing
    internal const string Experiment = "Experiment";
    internal const string Test = "Test";

    [Conditional("DEBUG")]
    internal static void AssertValidFeature(string s)
    {
        IEnumerable<string> flags = typeof(Feature)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!);

        Debug.Assert(flags.Contains(s), $"Unknown feature flag: {s}");
    }
}
