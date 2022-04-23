﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.IntelliCode.Api
{
    internal static class IntelliCodeCompletionOptions
    {
        public static PerLanguageOption<bool> TriggerOnTyping { get; } = (PerLanguageOption<bool>)CompletionOptions.TriggerOnTyping;

        public static PerLanguageOption<bool> TriggerOnTypingLetters { get; } = (PerLanguageOption<bool>)CompletionOptions.TriggerOnTypingLetters2;
    }
}
