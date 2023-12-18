// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int SplitStringLiteralOnEnter
        {
            get { return GetBooleanOption(SplitStringLiteralOptions.Enabled); }
            set { SetBooleanOption(SplitStringLiteralOptions.Enabled, value); }
        }
    }
}
