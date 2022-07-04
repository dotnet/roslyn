// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int EnableInlineDiagnostics
        {
            get { return GetBooleanOption(InlineDiagnosticsOptions.EnableInlineDiagnostics); }
            set { SetBooleanOption(InlineDiagnosticsOptions.EnableInlineDiagnostics, value); }
        }

        public int InlineDiagnosticsLocation
        {
            get { return (int)GetOption(InlineDiagnosticsOptions.Location); }
            set { SetOption(InlineDiagnosticsOptions.Location, (InlineDiagnosticsLocations)value); }
        }
    }
}
