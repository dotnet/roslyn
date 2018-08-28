// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal enum ParenthesesPreference
    {
        AlwaysForClarity,
        NeverIfUnnecessary,
    }

    internal class EditorconfigOptionToParenthesesPreference
    {
        internal static readonly Dictionary<string, ParenthesesPreference> options = new Dictionary<string, ParenthesesPreference>
        {
            { "always_for_clarity", ParenthesesPreference.AlwaysForClarity },
            { "never_if_unnecessary", ParenthesesPreference.NeverIfUnnecessary }
        };
    }
}
