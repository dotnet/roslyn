// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers;

internal static class PredefinedCodeCleanupProviderNames
{
    public const string Simplification = "SimplificationCodeCleanupProvider";
    public const string CaseCorrection = "CaseCorrectionCodeCleanupProvider";
    public const string AddMissingTokens = "AddMissingTokensCodeCleanupProvider";
    public const string NormalizeModifiersOrOperators = "NormalizeModifiersOrOperatorsCodeCleanupProvider";
    public const string RemoveUnnecessaryLineContinuation = nameof(RemoveUnnecessaryLineContinuation);
    public const string Format = "FormatCodeCleanupProvider";
    public const string FixIncorrectTokens = "FixIncorrectTokensCodeCleanupProvider";
    public const string ReduceTokens = "ReduceTokensCodeCleanupProvider";
}
