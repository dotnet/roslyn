// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.DateTime
{
    internal class DateTimeOptions
    {
        public static PerLanguageOption<bool> ProvideDateTimeOptionsCompletions =
            new PerLanguageOption<bool>(
                nameof(DateTimeOptions),
                nameof(ProvideDateTimeOptionsCompletions),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ProvideDateTimeOptionsCompletions"));
    }

    [ExportOptionProvider, Shared]
    internal class DateTimeOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public DateTimeOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            DateTimeOptions.ProvideDateTimeOptionsCompletions);
    }
}
