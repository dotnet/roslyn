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
    internal class InternalOptionSerializer : AbstractLocalUserRegistryOptionSerializer
    {
        [ImportingConstructor]
        public InternalOptionSerializer(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        protected override string GetCollectionPathForOption(OptionKey key)
        {
            if (key.Option.Feature == EditorComponentOnOffOptions.OptionName)
            {
                return @"Roslyn\Internal\OnOff\Components";
            }
            else if (key.Option.Feature == InternalFeatureOnOffOptions.OptionName)
            {
                return @"Roslyn\Internal\OnOff\Features";
            }
            else if (key.Option.Feature == PerformanceFunctionIdOptionsProvider.Name)
            {
                return @"Roslyn\Internal\Performance\FunctionId";
            }
            else if (key.Option.Feature == LoggerOptions.FeatureName)
            {
                return @"Roslyn\Internal\Performance\Logger";
            }
            else if (key.Option.Feature == InternalDiagnosticsOptions.OptionName)
            {
                return @"Roslyn\Internal\Diagnostics";
            }
            else if (key.Option.Feature == InternalSolutionCrawlerOptions.OptionName)
            {
                return @"Roslyn\Internal\SolutionCrawler";
            }
            else if (key.Option.Feature == CacheOptions.FeatureName)
            {
                return @"Roslyn\Internal\Performance\Cache";
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
