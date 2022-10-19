// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int Fading_FadeOutUnreachableCode
        {
            get { return GetBooleanOption(IdeAnalyzerOptionsStorage.FadeOutUnreachableCode); }
            set { SetBooleanOption(IdeAnalyzerOptionsStorage.FadeOutUnreachableCode, value); }
        }

        public int Fading_FadeOutUnusedImports
        {
            get { return GetBooleanOption(IdeAnalyzerOptionsStorage.FadeOutUnusedImports); }
            set { SetBooleanOption(IdeAnalyzerOptionsStorage.FadeOutUnusedImports, value); }
        }
    }
}
