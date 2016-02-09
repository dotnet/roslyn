// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
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
}
