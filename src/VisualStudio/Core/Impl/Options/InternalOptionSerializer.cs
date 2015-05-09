// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [ExportOptionSerializer(
        EditorComponentOnOffOptions.OptionName,
        InternalFeatureOnOffOptions.OptionName,
        PerformanceFunctionIdOptionsProvider.Name,
        LoggerOptions.FeatureName,
        CacheOptions.FeatureName,
        InternalDiagnosticsOptions.OptionName,
        InternalSolutionCrawlerOptions.OptionName), Shared]
    internal class InternalOptionSerializer : AbstractSettingStoreOptionSerializer
    {
        [ImportingConstructor]
        public InternalOptionSerializer(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        protected override Tuple<string, string> GetCollectionPathAndPropertyNameForOption(IOption key, string languageName)
        {
            if (key.Feature == EditorComponentOnOffOptions.OptionName)
            {
                return Tuple.Create(@"Roslyn\Internal\OnOff\Components", key.Name);
            }
            else if (key.Feature == InternalFeatureOnOffOptions.OptionName)
            {
                return Tuple.Create(@"Roslyn\Internal\OnOff\Features", key.Name);
            }
            else if (key.Feature == PerformanceFunctionIdOptionsProvider.Name)
            {
                return Tuple.Create(@"Roslyn\Internal\Performance\FunctionId", key.Name);
            }
            else if (key.Feature == LoggerOptions.FeatureName)
            {
                return Tuple.Create(@"Roslyn\Internal\Performance\Logger", key.Name);
            }
            else if (key.Feature == InternalDiagnosticsOptions.OptionName)
            {
                return Tuple.Create(@"Roslyn\Internal\Diagnostics", key.Name);
            }
            else if (key.Feature == InternalSolutionCrawlerOptions.OptionName)
            {
                return Tuple.Create(@"Roslyn\Internal\SolutionCrawler", key.Name);
            }
            else if (key.Feature == CacheOptions.FeatureName)
            {
                return Tuple.Create(@"Roslyn\Internal\Performance\Cache", key.Name);
            }

            throw ExceptionUtilities.Unreachable;
        }

        public override bool TryFetch(OptionKey optionKey, out object value)
        {
            switch (optionKey.Option.Feature)
            {
                case CacheOptions.FeatureName:
                case InternalSolutionCrawlerOptions.OptionName:
                    return TryFetch(optionKey, (r, k, o) => r.GetValue(k, defaultValue: o.DefaultValue), out value);
            }

            return base.TryFetch(optionKey, out value);
        }

        public override bool TryPersist(OptionKey optionKey, object value)
        {
            switch (optionKey.Option.Feature)
            {
                case CacheOptions.FeatureName:
                case InternalSolutionCrawlerOptions.OptionName:
                    return TryPersist(optionKey, value, (r, k, o, v) => r.SetValue(k, v, o.Type == typeof(int) ? RegistryValueKind.DWord : RegistryValueKind.QWord));
            }

            return base.TryPersist(optionKey, value);
        }
    }
}
