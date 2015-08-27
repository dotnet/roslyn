// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    internal static class ServiceFeatureOnOffOptions
    {
        public const string OptionName = "ServiceFeaturesOnOff";

        public static readonly PerLanguageOption<bool> ClosedFileDiagnostic = new PerLanguageOption<bool>(OptionName, "Closed File Diagnostic", defaultValue: true);
    }
}
