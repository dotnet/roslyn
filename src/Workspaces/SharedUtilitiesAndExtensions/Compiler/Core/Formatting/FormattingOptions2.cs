﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Indentation;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#else
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// Formatting options stored in editorconfig.
    /// </summary>
    internal sealed partial class FormattingOptions2
    {
#if !CODE_STYLE
        [ExportSolutionOptionProvider, Shared]
        internal sealed class Provider : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Provider()
            {
            }

            public ImmutableArray<IOption> Options { get; } = FormattingOptions2.Options;
        }
#endif
        private const string FeatureName = "FormattingOptions";

        public static PerLanguageOption2<bool> UseTabs =
            new(FeatureName, FormattingOptionGroups.IndentationAndSpacing, nameof(UseTabs), LineFormattingOptions.Default.UseTabs,
            storageLocations: ImmutableArray.Create<OptionStorageLocation2>(
                new EditorConfigStorageLocation<bool>("indent_style", s => s == "tab", isSet => isSet ? "tab" : "space"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Insert Tabs", useEditorLanguageName: true)));

        public static PerLanguageOption2<int> TabSize =
            new(FeatureName, FormattingOptionGroups.IndentationAndSpacing, nameof(TabSize), LineFormattingOptions.Default.TabSize,
            storageLocations: ImmutableArray.Create<OptionStorageLocation2>(
                EditorConfigStorageLocation.ForInt32Option("tab_width"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Tab Size", useEditorLanguageName: true)));

        public static PerLanguageOption2<int> IndentationSize =
            new(FeatureName, FormattingOptionGroups.IndentationAndSpacing, nameof(IndentationSize), LineFormattingOptions.Default.IndentationSize,
            storageLocations: ImmutableArray.Create<OptionStorageLocation2>(
                EditorConfigStorageLocation.ForInt32Option("indent_size"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Indent Size", useEditorLanguageName: true)));

        public static PerLanguageOption2<string> NewLine =
            new(FeatureName, FormattingOptionGroups.NewLine, nameof(NewLine), LineFormattingOptions.Default.NewLine,
            storageLocation: new EditorConfigStorageLocation<string>(
                "end_of_line",
                parseValue: value => value.Trim() switch
                {
                    "lf" => "\n",
                    "cr" => "\r",
                    "crlf" => "\r\n",
                    _ => Environment.NewLine
                },
                serializeValue: value => value switch
                {
                    "\n" => "lf",
                    "\r" => "cr",
                    "\r\n" => "crlf",
                    _ => "unset"
                }));

        internal static Option2<bool> InsertFinalNewLine =
            new(FeatureName, FormattingOptionGroups.NewLine, nameof(InsertFinalNewLine), DocumentFormattingOptions.Default.InsertFinalNewLine,
            storageLocation: EditorConfigStorageLocation.ForBoolOption("insert_final_newline"));

        public static PerLanguageOption2<IndentStyle> SmartIndent { get; } =
            new(FeatureName, FormattingOptionGroups.IndentationAndSpacing, nameof(SmartIndent), defaultValue: IndentationOptions.DefaultIndentStyle,
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Indent Style", useEditorLanguageName: true));

#if !CODE_STYLE
        internal static readonly ImmutableArray<IOption> Options = ImmutableArray.Create<IOption>(
            UseTabs,
            TabSize,
            IndentationSize,
            NewLine,
            InsertFinalNewLine);
#endif
    }

    internal static class FormattingOptionGroups
    {
        public static readonly OptionGroup IndentationAndSpacing = new(WorkspacesResources.Indentation_and_spacing, priority: 1);
        public static readonly OptionGroup NewLine = new(WorkspacesResources.New_line_preferences, priority: 2);
    }
}
