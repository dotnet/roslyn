// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.DateAndTime
{
    internal class DateAndTimeOptions
    {
        public static readonly PerLanguageOption2<bool> ProvideDateAndTimeCompletions =
            new PerLanguageOption2<bool>(
                nameof(DateAndTime),
                nameof(ProvideDateAndTimeCompletions),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ProvideDateAndTimeCompletions"));
    }

    [ExportOptionProvider, Shared]
    internal class DateAndTimeOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DateAndTimeOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            DateAndTimeOptions.ProvideDateAndTimeCompletions);
    }
}
