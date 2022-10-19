// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class StackTraceExplorerOptionsMetadata : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StackTraceExplorerOptionsMetadata()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            OpenOnFocus);

        private const string FeatureName = "StackTraceExplorerOptions";

        /// <summary>
        /// Used to determine if a user focusing VS should look at the clipboard for a callstack and automatically
        /// open the tool window with the callstack inserted
        /// </summary>
        public static readonly Option2<bool> OpenOnFocus = new(FeatureName, "OpenOnFocus", defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation("StackTraceExplorer.Options.OpenOnFocus"));
    }
}
