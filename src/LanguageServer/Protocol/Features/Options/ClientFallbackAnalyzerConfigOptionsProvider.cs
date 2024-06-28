// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Flows editorconfig options stored by <see cref="IGlobalOptionService"/> to <see cref="Solution.FallbackAnalyzerOptions"/> whenever a new language is added to one of the target workspaces.
/// Works alongside <see cref="SolutionAnalyzerConfigOptionsUpdater"/>, which keeps values of these options up-to-date afterwards.
/// </summary>
[Shared]
[ExportWorkspaceService(typeof(IFallbackAnalyzerConfigOptionsProvider), workspaceKinds:
    [WorkspaceKind.Host, WorkspaceKind.Interactive, WorkspaceKind.SemanticSearch, WorkspaceKind.MetadataAsSource, WorkspaceKind.MiscellaneousFiles, WorkspaceKind.Debugger, WorkspaceKind.Preview])]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ClientFallbackAnalyzerConfigOptionsProvider(EditorConfigOptionsEnumerator optionsEnumerator, IGlobalOptionService globalOptions) : IFallbackAnalyzerConfigOptionsProvider
{
    public StructuredAnalyzerConfigOptions GetOptions(string language)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(AnalyzerConfigOptions.KeyComparer);

        var optionDefinitions = optionsEnumerator.GetOptions(language);

        foreach (var (_, options) in optionDefinitions)
        {
            foreach (var option in options)
            {
                var value = globalOptions.GetOption<object>(new OptionKey2(option, option.IsPerLanguage ? language : null));

                var configName = option.Definition.ConfigName;
                var configValue = option.Definition.Serializer.Serialize(value);

                builder.Add(configName, configValue);
            }
        }

        return StructuredAnalyzerConfigOptions.Create(new DictionaryAnalyzerConfigOptions(builder.ToImmutable()));
    }
}
