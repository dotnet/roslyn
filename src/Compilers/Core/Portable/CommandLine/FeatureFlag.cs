// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal static class FeatureFlag
{
    internal const string Strict = "strict";
    internal const string UseLegacyStrongNameProvider = "UseLegacyStrongNameProvider";
    internal const string EnableGeneratorCache = "enable-generator-cache";
    internal const string PdbPathDeterminism = "pdb-path-determinism";
    internal const string DebugDeterminism = "debug-determinism";
    internal const string DebugAnalyzers = "debug-analyzers";
    internal const string PEVerifyCompat = "peverify-compat";
    internal const string FileBasedProgram = "FileBasedProgram";
    internal const string InterceptorsNamespaces = "InterceptorsNamespaces";
    internal const string NoRefSafetyRulesAttribute = "noRefSafetyRulesAttribute";
    internal const string NullablePublicOnly = "nullablePublicOnly";
    internal const string RunNullableAnalysis = "run-nullable-analysis";
    internal const string DisableLengthBasedSwitch = "disable-length-based-switch";
    internal const string ExperimentalDataSectionStringLiterals = "experimental-data-section-string-literals";
    internal const string RuntimeAsync = "runtime-async";
}
