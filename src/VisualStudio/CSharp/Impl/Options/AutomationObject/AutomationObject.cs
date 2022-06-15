// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    [ComVisible(true)]
    public partial class AutomationObject : AbstractAutomationObject
    {
        internal AutomationObject(Workspace workspace) : base(workspace, LanguageNames.CSharp)
        {
        }

        private int GetBooleanOption(Option2<bool> key)
            => GetOption(key) ? 1 : 0;

        private int GetBooleanOption(PerLanguageOption2<bool> key)
            => GetOption(key) ? 1 : 0;

        private void SetBooleanOption(Option2<bool> key, int value)
            => SetOption(key, value != 0);

        private void SetBooleanOption(PerLanguageOption2<bool> key, int value)
            => SetOption(key, value != 0);
    }
}
