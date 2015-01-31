// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class BraceCompletionOptions
    {
        public const string FeatureName = "BraceCompletion";

        [ExportOption]
        public static readonly PerLanguageOption<bool> EnableBraceCompletion = new PerLanguageOption<bool>(FeatureName, "EnableBraceCompletion", defaultValue: true);
    }
}
