// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    [ComVisible(true)]
    public partial class AutomationObject : AbstractAutomationObject
    {
        internal AutomationObject(ILegacyGlobalOptionService legacyGlobalOptions)
            : base(legacyGlobalOptions, LanguageNames.CSharp)
        {
        }

        private int GetBooleanOption(Option2<bool> option)
            => GetOption(option) ? 1 : 0;

        private int GetBooleanOption(PerLanguageOption2<bool> option)
            => GetOption(option) ? 1 : 0;

        private int GetBooleanOption(Option2<NewLineBeforeOpenBracePlacement> option, NewLineBeforeOpenBracePlacement flag)
            => GetOption(option).HasFlag(flag) ? 1 : 0;

        private int GetBooleanOption(Option2<SpacePlacementWithinParentheses> option, SpacePlacementWithinParentheses flag)
            => GetOption(option).HasFlag(flag) ? 1 : 0;

        private void SetBooleanOption(Option2<bool> option, int value)
            => SetOption(option, value != 0);

        private void SetBooleanOption(PerLanguageOption2<bool> option, int value)
            => SetOption(option, value != 0);

        private void SetBooleanOption(Option2<NewLineBeforeOpenBracePlacement> option, NewLineBeforeOpenBracePlacement flag, int value)
            => SetOption(option, GetOption(option).WithFlagValue(flag, value != 0));

        private void SetBooleanOption(Option2<SpacePlacementWithinParentheses> option, SpacePlacementWithinParentheses flag, int value)
            => SetOption(option, GetOption(option).WithFlagValue(flag, value != 0));
    }
}
