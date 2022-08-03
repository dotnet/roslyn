// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Roslyn.Utilities;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;
using System.Net.WebSockets;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Collections.Internal;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features.SettingsHelper;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    internal class SettingsHelper
    {
        public struct SettingsSnapshots
        {
            public ImmutableArray<CodeStyleSetting>? codeStyleSnapshot;
            public ImmutableArray<WhitespaceSetting>? whitespaceSnapshot;
            public ImmutableArray<AnalyzerSetting>? analyzerSnapshot;
        }

        public static SettingsSnapshots GetSettingsSnapshots(Workspace workspace, string filePath)
        {
            var settingsAggregator = workspace.Services.GetRequiredService<ISettingsAggregator>();

            var codeStyleProvider = settingsAggregator.GetSettingsProvider<CodeStyleSetting>(filePath);
            var whitespaceProvider = settingsAggregator.GetSettingsProvider<WhitespaceSetting>(filePath);
            var analyzerProvider = settingsAggregator.GetSettingsProvider<AnalyzerSetting>(filePath);

            var codeStyleSnapshot = codeStyleProvider?.GetCurrentDataSnapshot();
            var whitespaceSnapshot = whitespaceProvider?.GetCurrentDataSnapshot();
            var analyzerSnapshot = analyzerProvider?.GetCurrentDataSnapshot();

            return new SettingsSnapshots
            {
                codeStyleSnapshot = codeStyleSnapshot,
                whitespaceSnapshot = whitespaceSnapshot,
                analyzerSnapshot = analyzerSnapshot,
            };
        }
    }
}
