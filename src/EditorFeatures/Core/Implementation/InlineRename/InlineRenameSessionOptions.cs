// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal static class InlineRenameSessionOptions
    {
        [ExportGlobalOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
                RenameOverloads,
                RenameInStrings,
                RenameInComments,
                RenameFile,
                PreviewChanges);

            private const string FeatureName = "InlineRenameDashboardOptions";

            public static Option<bool> RenameOverloads { get; } = new Option<bool>(FeatureName, "RenameOverloads", defaultValue: false, storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RenameOverloads"));
            public static Option<bool> RenameInStrings { get; } = new Option<bool>(FeatureName, "RenameInStrings", defaultValue: false, storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RenameInStrings"));
            public static Option<bool> RenameInComments { get; } = new Option<bool>(FeatureName, "RenameInComments", defaultValue: false, storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RenameInComments"));
            public static Option<bool> RenameFile { get; } = new Option<bool>(FeatureName, "RenameFile", defaultValue: true, storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RenameFile"));
            public static Option<bool> PreviewChanges { get; } = new Option<bool>(FeatureName, "PreviewChanges", defaultValue: false, storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreviewRename"));
        }
    }
}
