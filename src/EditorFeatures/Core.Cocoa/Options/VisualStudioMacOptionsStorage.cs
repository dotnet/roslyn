// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.EditorFeatures.Cocoa;

internal static class VisualStudioMacOptionStorage
{
    internal readonly struct PropertyName
    {
        private readonly string _name;
        private readonly string? _vbName;
        private readonly string? _altName;

        public PropertyName(string name, string? vbName = null, string? altName = null)
        {
            _name = name;
            _vbName = vbName;
            _altName = altName;
        }

        private static string SubstituteLanguage(string keyName, string? language)
            => keyName.Replace("%LANGUAGE%", language switch
            {
                LanguageNames.CSharp => "CSharp",
                LanguageNames.VisualBasic => "VisualBasic",
                _ => language // handles F#, TypeScript and Xaml
            });

        public string GetName(string? language)
            => (_vbName != null && language == LanguageNames.VisualBasic) ? _vbName : SubstituteLanguage(_name, language);

        public string? TryGetAlternativeName(string? language)
            => _altName != null ? SubstituteLanguage(_altName, language) : null;
    }

    /// <summary>
    /// <see cref="OptionDefinition.ConfigName"/> of options to persist, other then those listed in <see cref="PersistedOptionsWithLegacyPropertyNames"/>.
    /// </summary>
    public static readonly IReadOnlySet<string> PersistedOptions = new HashSet<string>()
    {
    };

    /// <summary>
    /// Maps option <see cref="OptionDefinition.ConfigName"/> to a legacy property name used in VS Mac XML settings file.
    /// This list is frozen and only used to enable migration from existing settings files.
    /// Do not add mappings for new options.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, PropertyName> PersistedOptionsWithLegacyPropertyNames = new Dictionary<string, PropertyName>()
    {
        {"BraceCompletionOptions_AutoFormattingOnCloseBrace", new PropertyName("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Close Brace")},
        {"FormattingOptions_AutoFormattingOnReturn", new PropertyName("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Return")},
        {"FormattingOptions_AutoFormattingOnSemicolon", new PropertyName("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Semicolon")},
        {"FormattingOptions_AutoFormattingOnTyping", new PropertyName("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Typing")},
        {"FormattingOptions_FormatOnPaste", new PropertyName("TextEditor.%LANGUAGE%.SpeciAddImportsOnPaste2fic.FormatOnPaste")},
        {"dotnet_sort_system_directives_first", new PropertyName("TextEditor.%LANGUAGE%.Specific.PlaceSystemNamespaceFirst")},
        {"dotnet_separate_import_directive_groups", new PropertyName("TextEditor.%LANGUAGE%.Specific.SeparateImportDirectiveGroups")},
        {"QuickInfoOptions_ShowRemarksInQuickInfo", new PropertyName("TextEditor.%LANGUAGE%.Specific.ShowRemarks")},
        {"dotnet_add_imports_on_paste", new PropertyName("TextEditor.%LANGUAGE%.Specific.AddImportsOnPaste2")},
        {"CompletionOptions_ShowItemsFromUnimportedNamespaces", new PropertyName("TextEditor.%LANGUAGE%.Specific.ShowItemsFromUnimportedNamespaces")},
        {"CompletionOptions_TriggerOnDeletion", new PropertyName("TextEditor.%LANGUAGE%.Specific.TriggerOnDeletion")},
        {"CompletionOptions_TriggerOnTypingLetters", new PropertyName("TextEditor.%LANGUAGE%.Specific.TriggerOnTypingLetters")},
        {"CompletionOptions_ShowCompletionItemFilters", new PropertyName("TextEditor.%LANGUAGE%.Specific.ShowCompletionItemFilters")},
        {"SolutionCrawlerOptionsStorage_BackgroundAnalysisScopeOption", new PropertyName("TextEditor.%LANGUAGE%.Specific.BackgroundAnalysisScopeOption")},
        {"SolutionCrawlerOptionsStorage_CompilerDiagnosticsScopeOption", new PropertyName("TextEditor.%LANGUAGE%.Specific.CompilerDiagnosticsScopeOption")},
        {"SymbolSearchOptions_SuggestForTypesInNuGetPackages", new PropertyName("TextEditor.%LANGUAGE%.Specific.SuggestForTypesInNuGetPackages")},
        {"csharp_automatically_complete_statement_on_semicolon", new PropertyName("TextEditor.AutomaticallyCompleteStatementOnSemicolon")},
    };
}
