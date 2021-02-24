// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
{
    internal class SplitCommentOptions
    {
        public static PerLanguageOption2<bool> Enabled =
           new PerLanguageOption2<bool>(nameof(SplitCommentOptions), nameof(Enabled), defaultValue: true,
               storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SplitComments"));
    }

    [ExportOptionProvider, Shared]
    internal class SplitCommentOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SplitCommentOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            SplitCommentOptions.Enabled);
    }
}
