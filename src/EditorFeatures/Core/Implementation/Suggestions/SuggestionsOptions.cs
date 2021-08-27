// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal class SuggestionsOptions
    {
        public static Option2<bool?> Asynchronous =
           new(nameof(SuggestionsOptions), nameof(Asynchronous), defaultValue: null,
               storageLocation: new RoamingProfileStorageLocation("TextEditor.Specific.Suggestions.Asynchronous2"));
    }
}
