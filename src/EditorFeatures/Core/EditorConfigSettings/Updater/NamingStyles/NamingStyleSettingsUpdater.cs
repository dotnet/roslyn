// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles.EditorConfigNamingStylesParser;
using RoslynEnumerableExtensions = Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions.EnumerableExtensions;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

internal sealed partial class NamingStyleSettingsUpdater(Workspace workspace, IGlobalOptionService globalOptions, string editorconfigPath) : SettingsUpdaterBase<(Action<(object, object?)> onSettingChange, NamingStyleSetting option), object>(workspace, editorconfigPath)
{
    public readonly IGlobalOptionService GlobalOptions = globalOptions;

    protected override SourceText? GetNewText(
        SourceText analyzerConfigDocument,
        IReadOnlyList<((Action<(object, object?)> onSettingChange, NamingStyleSetting option) option, object value)> settingsToUpdate,
        CancellationToken token)
    {
        var result = Parse(analyzerConfigDocument, EditorconfigPath);
        if (!result.Rules.Any() && settingsToUpdate.Any())
        {
            // handle no naming style rules in the editorconfig file.
            // The implementation does not allow naming style rules to layer meaning all rules are either 
            // defined in Visual Studios settings or in an editorconfig file. 
            analyzerConfigDocument = analyzerConfigDocument.WithNamingStyles(GlobalOptions);
            result = Parse(analyzerConfigDocument, EditorconfigPath);
        }

        foreach (var ((onSettingChange, option), value) in settingsToUpdate)
        {
            if (result.TryGetParseResultForRule(option, out var parseResult))
            {
                var endOfSection = new TextSpan(parseResult.Section.Span.End, 0);
                if (value is ReportDiagnostic enforcement)
                {
                    var newLine = $"dotnet_naming_rule.{parseResult.RuleName.Value}.severity = {enforcement.ToEditorConfigString()}";
                    analyzerConfigDocument = UpdateDocument(analyzerConfigDocument, newLine, parseResult.Severity.Span, endOfSection);
                    result = Parse(analyzerConfigDocument, EditorconfigPath);
                    onSettingChange((enforcement, null));
                }

                if (value is NamingStyle prevStyle)
                {
                    var allCurrentStyles = result.Rules.Select(x => x.NamingScheme).Distinct().Select(x => (x, style: x.AsNamingStyle()));
                    var styleParseResult = TryGetStyleParseResult(prevStyle, allCurrentStyles);
                    var allDistinctStyles = RoslynEnumerableExtensions.DistinctBy(allCurrentStyles.Select(x => x.style), x => x.Name).ToArray();
                    if (styleParseResult is (NamingScheme namingScheme, NamingStyle style))
                    {
                        var newLine = $"dotnet_naming_rule.{parseResult.RuleName.Value}.style = {namingScheme.OptionName.Value}";
                        analyzerConfigDocument = UpdateDocument(analyzerConfigDocument, newLine, parseResult.NamingScheme.OptionName.Span, endOfSection);
                        result = Parse(analyzerConfigDocument, EditorconfigPath);
                        onSettingChange((style, allDistinctStyles));
                    }

                    continue;
                }
            }
        }

        return analyzerConfigDocument;

        static (NamingScheme? scheme, NamingStyle style) TryGetStyleParseResult(
            NamingStyle prevStyle,
            IEnumerable<(NamingScheme scheme, NamingStyle style)> allCurrentStyles)
        {
            foreach (var (scheme, currentStyle) in allCurrentStyles)
            {
                if (prevStyle.Prefix == currentStyle.Prefix &&
                    prevStyle.Suffix == currentStyle.Suffix &&
                    prevStyle.WordSeparator == currentStyle.WordSeparator &&
                    prevStyle.CapitalizationScheme == currentStyle.CapitalizationScheme)
                {
                    return (scheme, currentStyle);
                }
            }

            return (null, default);
        }

        static SourceText UpdateDocument(SourceText sourceText, string newLine, TextSpan? potentialSpan, TextSpan backupSpan)
        {
            if (potentialSpan is null)
            {
                // there is no place to update in the current document instead
                // we are appending to the end of a section so we need to add a newline
                newLine = "\r\n" + newLine;
            }

            var span = potentialSpan ?? backupSpan;
            var textChange = new TextChange(span, newLine);
            return sourceText.WithChanges(textChange);
        }
    }
}
