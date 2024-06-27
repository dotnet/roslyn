﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.VSMac;

internal static class CompletionOptionsAccessor
{
    public static PerLanguageOption2<bool?> ShowItemsFromUnimportedNamespaces
        => CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces;

    public static PerLanguageOption2<bool> TriggerOnTypingLetters
        => CompletionOptionsStorage.TriggerOnTypingLetters;

    public static PerLanguageOption2<bool?> TriggerOnDeletion
        => CompletionOptionsStorage.TriggerOnDeletion;
}

