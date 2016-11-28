// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Shell;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages
{
    internal sealed partial class ForceLowMemoryMode
    {
        public const string OptionName = nameof(ForceLowMemoryMode);
        public static readonly Option<bool> Enabled = new Option<bool>(OptionName, nameof(Enabled), defaultValue: false);
        public static readonly Option<int> SizeInMegabytes = new Option<int>(OptionName, nameof(SizeInMegabytes), defaultValue: 500);
    }

    [ExportOptionSerializer(ForceLowMemoryMode.OptionName), Shared]
    internal sealed class ForceLowMemoryModeSerializer : AbstractLocalUserRegistryOptionSerializer
    {
        [ImportingConstructor]
        public ForceLowMemoryModeSerializer(SVsServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override string GetCollectionPathForOption(OptionKey key)
        {
            return @"Roslyn\ForceLowMemoryMode";
        }
    }
}
