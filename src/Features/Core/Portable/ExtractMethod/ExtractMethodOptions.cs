// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal static class ExtractMethodOptions
    {
        public const string FeatureName = "ExtractMethod";

        public static readonly PerLanguageOption<bool> AllowBestEffort = new PerLanguageOption<bool>(FeatureName, "Allow Best Effort", defaultValue: false);
        public static readonly PerLanguageOption<bool> DontPutOutOrRefOnStruct = new PerLanguageOption<bool>(FeatureName, "Don't Put Out Or Ref On Strcut", defaultValue: true);
        public static readonly PerLanguageOption<bool> AllowMovingDeclaration = new PerLanguageOption<bool>(FeatureName, "Allow Moving Declaration", defaultValue: false);
    }
}
