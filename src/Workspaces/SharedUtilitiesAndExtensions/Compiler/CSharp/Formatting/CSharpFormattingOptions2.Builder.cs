// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal static partial class CSharpFormattingOptions2
    {
        private readonly ref struct Builder
        {
            private readonly EditorConfigHelpers _editorConfig;

            // Maps to store mapping between special option kinds and the corresponding options.
            public readonly ImmutableArray<IOption2>.Builder AllOptionsBuilder;
            public readonly ImmutableDictionary<Option2<bool>, SpacingWithinParenthesesOption>.Builder SpacingWithinParenthesisOptionsMapBuilder;
            public readonly ImmutableDictionary<Option2<bool>, NewLineOption>.Builder NewLineOptionsMapBuilder;

            private readonly OptionGroup _indentationGroup;
            private readonly OptionGroup _newLineGroup;
            private readonly OptionGroup _spacingGroup;
            private readonly OptionGroup _wrappingGroup;

            public Builder(EditorConfigHelpers editorConfig)
            {
                _editorConfig = editorConfig;
                AllOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();
                SpacingWithinParenthesisOptionsMapBuilder = ImmutableDictionary.CreateBuilder<Option2<bool>, SpacingWithinParenthesesOption>();
                NewLineOptionsMapBuilder = ImmutableDictionary.CreateBuilder<Option2<bool>, NewLineOption>();

                _indentationGroup = CSharpFormattingOptionGroups.Indentation;
                _newLineGroup = CSharpFormattingOptionGroups.NewLine;
                _spacingGroup = CSharpFormattingOptionGroups.Spacing;
                _wrappingGroup = CSharpFormattingOptionGroups.Wrapping;
            }

            public Option2<T> CreateOption<T>(
                OptionGroup group, string name, T defaultValue,
                OptionStorageLocation2 editorConfigStorage,
                OptionStorageLocation2 roamingProfileStorage)
            {
                var option = new Option2<T>(
                    "CSharpFormattingOptions",
                    group, name, defaultValue,
                    ImmutableArray.Create(editorConfigStorage, roamingProfileStorage));

                AllOptionsBuilder.Add(option);
                return option;
            }

            public Option2<T> CreateOption<T>(
                OptionGroup group, string name, T defaultValue,
                OptionStorageLocation2 editorConfigStorage,
                string roamingProfileKeyName)
                => CreateOption(group, name, defaultValue,
                    editorConfigStorage,
                    new RoamingProfileStorageLocation(roamingProfileKeyName));

            public Option2<T> CreateOption<T>(
                OptionGroup group, string name, T defaultValue,
                string editorConfigKeyName,
                OptionStorageLocation2 roamingProfileStorage)
                => CreateOption(group, name, defaultValue,
                    EditorConfigStorageLocation.ForBoolOption(editorConfigKeyName),
                    roamingProfileStorage);

            public Option2<T> CreateOption<T>(
                OptionGroup group, string name, T defaultValue,
                string editorConfigKeyName,
                string roamingProfileKeyName)
                => CreateOption(group, name, defaultValue,
                    EditorConfigStorageLocation.ForBoolOption(editorConfigKeyName),
                    new RoamingProfileStorageLocation(roamingProfileKeyName));

            public Option2<T> CreateIndentationOption<T>(
                string name, T defaultValue,
                OptionStorageLocation2 editorConfigStorage,
                string roamingProfileKeyName)
                => CreateOption(_indentationGroup, name, defaultValue,
                    editorConfigStorage,
                    new RoamingProfileStorageLocation(roamingProfileKeyName));

            public Option2<T> CreateIndentationOption<T>(
                string name, T defaultValue,
                string editorConfigKeyName,
                string roamingProfileKeyName)
                => CreateOption(_indentationGroup, name, defaultValue,
                    EditorConfigStorageLocation.ForBoolOption(editorConfigKeyName),
                    new RoamingProfileStorageLocation(roamingProfileKeyName));

            public Option2<T> CreateNewLineOption<T>(
                string name, T defaultValue,
                string editorConfigKeyName,
                string roamingProfileKeyName)
                => CreateOption(_newLineGroup, name, defaultValue,
                    EditorConfigStorageLocation.ForBoolOption(editorConfigKeyName),
                    new RoamingProfileStorageLocation(roamingProfileKeyName));

            public Option2<T> CreateSpacingOption<T>(
                string name, T defaultValue,
                OptionStorageLocation2 editorConfigStorage,
                string roamingProfileKeyName)
                => CreateOption(_spacingGroup, name, defaultValue,
                    editorConfigStorage,
                    new RoamingProfileStorageLocation(roamingProfileKeyName));

            public Option2<T> CreateSpacingOption<T>(
                string name, T defaultValue,
                string editorConfigKeyName,
                string roamingProfileKeyName)
                => CreateOption(_spacingGroup, name, defaultValue,
                    EditorConfigStorageLocation.ForBoolOption(editorConfigKeyName),
                    new RoamingProfileStorageLocation(roamingProfileKeyName));

            public Option2<T> CreateWrappingOption<T>(
                string name, T defaultValue,
                string editorConfigKeyName,
                string roamingProfileKeyName)
                => CreateOption(_wrappingGroup, name, defaultValue,
                    EditorConfigStorageLocation.ForBoolOption(editorConfigKeyName),
                    new RoamingProfileStorageLocation(roamingProfileKeyName));

            public Option2<bool> CreateSpaceWithinParenthesesOption(SpacingWithinParenthesesOption parenthesesOption, string name)
            {
                var option = CreateOption(
                    _spacingGroup, name,
                    defaultValue: false,
                    new EditorConfigStorageLocation<bool>(
                        "csharp_space_between_parentheses",
                        _editorConfig.DetermineIfSpaceOptionIsSet(parenthesesOption),
                        _editorConfig.GetSpacingWithParenthesesString),
                    roamingProfileKeyName: $"TextEditor.CSharp.Specific.{name}");

                Debug.Assert(_editorConfig.SpacingWithinParenthesisOptionsEditorConfigMap.ContainsValue(parenthesesOption));
                SpacingWithinParenthesisOptionsMapBuilder.Add(option, parenthesesOption);

                return option;
            }

            public Option2<bool> CreateNewLineForBracesOption(NewLineOption newLineOption, string name)
            {
                var option = CreateOption(
                    _newLineGroup, name,
                    defaultValue: true,
                    new EditorConfigStorageLocation<bool>(
                        "csharp_new_line_before_open_brace",
                        _editorConfig.DetermineIfNewLineOptionIsSet(newLineOption),
                        _editorConfig.GetNewLineOptionString),
                    roamingProfileKeyName: $"TextEditor.CSharp.Specific.{name}");

                Debug.Assert(_editorConfig.NewLineOptionsEditorConfigMap.ContainsValue(newLineOption));
                NewLineOptionsMapBuilder.Add(option, newLineOption);

                return option;
            }
        }
    }
}
