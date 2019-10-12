// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitComment
{
    internal class SplitCommentOptions
    {
        public static PerLanguageOption<bool> Enabled =
           new PerLanguageOption<bool>(nameof(SplitCommentOptions), nameof(Enabled), defaultValue: true,
               storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SplitComments"));
    }

    [ExportOptionProvider, Shared]
    internal class SplitStringLiteralOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public SplitStringLiteralOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            SplitCommentOptions.Enabled);
    }
}
