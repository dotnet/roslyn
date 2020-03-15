// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.DateAndTimeFormatString
{
    internal class DateAndTimeFormatStringOptions
    {
        public static PerLanguageOption<bool> ProvideDateAndTimeFormatStringCompletions =
            new PerLanguageOption<bool>(
                nameof(DateAndTimeFormatStringOptions),
                nameof(ProvideDateAndTimeFormatStringCompletions),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ProvideDateAndTimeFormatStringCompletions"));
    }

    [ExportOptionProvider, Shared]
    internal class DateAndTimeFormatStringOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public DateAndTimeFormatStringOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            DateAndTimeFormatStringOptions.ProvideDateAndTimeFormatStringCompletions);
    }
}
