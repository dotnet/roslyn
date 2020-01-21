// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal partial class ExtensionManagerOptions
    {
        public static readonly Option<bool> DisableCrashingExtensions = new Option<bool>(nameof(ExtensionManagerOptions), nameof(DisableCrashingExtensions), defaultValue: true);
    }

    [ExportOptionProvider, Shared]
    internal class ExtensionManagerOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public ExtensionManagerOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            ExtensionManagerOptions.DisableCrashingExtensions);
    }
}
