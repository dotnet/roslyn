// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Xaml
{
    [ExportOptionProvider, Shared]
    internal sealed class XamlOptions : IOptionProvider
    {
        private const string FeatureName = "XamlOptions";

        public static readonly Option2<bool> EnableLspIntelliSenseFeatureFlag = new(FeatureName, nameof(EnableLspIntelliSenseFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Xaml.EnableLspIntelliSense"));

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            EnableLspIntelliSenseFeatureFlag);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlOptions()
        {
        }
    }
}
