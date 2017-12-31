// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ValidateRegexString
{
    internal class ValidateRegexStringOption
    {
        public static PerLanguageOption<bool> ReportInvalidRegexPatterns =
            new PerLanguageOption<bool>(
                nameof(ValidateRegexStringOption), 
                nameof(ReportInvalidRegexPatterns), 
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns"));
    }

    [ExportOptionProvider, Shared]
    internal class ValidateRegexStringOptionProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            ValidateRegexStringOption.ReportInvalidRegexPatterns);
    }
}
