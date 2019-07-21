// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal static class FeatureOnOffOptions
    {
        public static readonly PerLanguageOption<bool> EndConstruct = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(EndConstruct), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AutoEndInsert"));

        // This value is only used by Visual Basic, and so is using the old serialization name that was used by VB.
        public static readonly PerLanguageOption<bool> AutomaticInsertionOfAbstractOrInterfaceMembers = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(AutomaticInsertionOfAbstractOrInterfaceMembers), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AutoRequiredMemberInsert"));

        public static readonly PerLanguageOption<bool> LineSeparator = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(LineSeparator), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.DisplayLineSeparators" : "TextEditor.%LANGUAGE%.Specific.Line Separator"));

        public static readonly PerLanguageOption<bool> Outlining = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(Outlining), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Outlining"));

        public static readonly PerLanguageOption<bool> KeywordHighlighting = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(KeywordHighlighting), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.EnableHighlightRelatedKeywords" : "TextEditor.%LANGUAGE%.Specific.Keyword Highlighting"));

        public static readonly PerLanguageOption<bool> ReferenceHighlighting = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(ReferenceHighlighting), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.EnableHighlightReferences" : "TextEditor.%LANGUAGE%.Specific.Reference Highlighting"));

        public static readonly PerLanguageOption<bool> FormatOnPaste = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(FormatOnPaste), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.FormatOnPaste"));

        public static readonly PerLanguageOption<bool> AutoXmlDocCommentGeneration = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(AutoXmlDocCommentGeneration), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.AutoComment" : "TextEditor.%LANGUAGE%.Specific.Automatic XML Doc Comment Generation"));

        public static readonly PerLanguageOption<bool> AutoInsertBlockCommentStartString = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(AutoInsertBlockCommentStartString), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Insert Block Comment Start String"));

        public static readonly PerLanguageOption<bool> PrettyListing = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(PrettyListing), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PrettyListing"));

        public static readonly PerLanguageOption<bool> AutoFormattingOnTyping = new PerLanguageOption<bool>(
            nameof(FeatureOnOffOptions), nameof(AutoFormattingOnTyping), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Typing"));

        public static readonly PerLanguageOption<bool> AutoFormattingOnCloseBrace = new PerLanguageOption<bool>(
            nameof(FeatureOnOffOptions), nameof(AutoFormattingOnCloseBrace), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Close Brace"));

        public static readonly PerLanguageOption<bool> AutoFormattingOnSemicolon = new PerLanguageOption<bool>(
            nameof(FeatureOnOffOptions), nameof(AutoFormattingOnSemicolon), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Semicolon"));

        public static readonly PerLanguageOption<bool> RenameTrackingPreview = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(RenameTrackingPreview), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.RenameTrackingPreview" : "TextEditor.%LANGUAGE%.Specific.Rename Tracking Preview"));

        /// <summary>
        /// This option is currently used by Roslyn, but we might want to implement it in the
        /// future. Keeping the option while it's unimplemented allows all upgrade paths to
        /// maintain any customized value for this setting, even through versions that have not
        /// implemented this feature yet.
        /// </summary>
        public static readonly PerLanguageOption<bool> RenameTracking = new PerLanguageOption<bool>(nameof(FeatureOnOffOptions), nameof(RenameTracking), defaultValue: true);

        /// <summary>
        /// This option is currently used by Roslyn, but we might want to implement it in the
        /// future. Keeping the option while it's unimplemented allows all upgrade paths to
        /// maintain any customized value for this setting, even through versions that have not
        /// implemented this feature yet.
        /// </summary>
        public static readonly PerLanguageOption<bool> RefactoringVerification = new PerLanguageOption<bool>(
            nameof(FeatureOnOffOptions), nameof(RefactoringVerification), defaultValue: false);

        public static readonly PerLanguageOption<bool> StreamingGoToImplementation = new PerLanguageOption<bool>(
            nameof(FeatureOnOffOptions), nameof(StreamingGoToImplementation), defaultValue: true);

        public static readonly Option<bool> NavigateToDecompiledSources = new Option<bool>(
            nameof(FeatureOnOffOptions), nameof(NavigateToDecompiledSources), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.{nameof(NavigateToDecompiledSources)}"));

        public static readonly Option<int> UseEnhancedColors = new Option<int>(
            nameof(FeatureOnOffOptions), nameof(UseEnhancedColors), defaultValue: 1,
            storageLocations: new RoamingProfileStorageLocation("WindowManagement.Options.UseEnhancedColorsForManagedLanguages"));

        /// <summary>
        /// Feature to enable <see cref="CompilerFeatureFlags.RunNullableAnalysis"/> in the compiler flags for all csharp projects.
        /// 0 = default, which leaves the flag unset.
        /// 1 = set to true
        /// -1 = set to false
        /// </summary>
        public static readonly Option<int> UseNullableReferenceTypeAnalysis = new Option<int>(
            nameof(FeatureOnOffOptions), nameof(UseNullableReferenceTypeAnalysis), defaultValue: 0,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.CSharp.{nameof(UseNullableReferenceTypeAnalysis)}"));

        // Note: no storage location since this is intentionally a session variable
        public static readonly Option<bool> AcceptedDecompilerDisclaimer = new Option<bool>(
            nameof(FeatureOnOffOptions), nameof(AcceptedDecompilerDisclaimer), defaultValue: false);
    }

    [ExportOptionProvider, Shared]
    internal class FeatureOnOffOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public FeatureOnOffOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            FeatureOnOffOptions.EndConstruct,
            FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers,
            FeatureOnOffOptions.LineSeparator,
            FeatureOnOffOptions.Outlining,
            FeatureOnOffOptions.KeywordHighlighting,
            FeatureOnOffOptions.ReferenceHighlighting,
            FeatureOnOffOptions.FormatOnPaste,
            FeatureOnOffOptions.AutoXmlDocCommentGeneration,
            FeatureOnOffOptions.AutoInsertBlockCommentStartString,
            FeatureOnOffOptions.PrettyListing,
            FeatureOnOffOptions.AutoFormattingOnTyping,
            FeatureOnOffOptions.AutoFormattingOnCloseBrace,
            FeatureOnOffOptions.AutoFormattingOnSemicolon,
            FeatureOnOffOptions.RenameTrackingPreview,
            FeatureOnOffOptions.RenameTracking,
            FeatureOnOffOptions.RefactoringVerification,
            FeatureOnOffOptions.StreamingGoToImplementation,
            FeatureOnOffOptions.NavigateToDecompiledSources,
            FeatureOnOffOptions.AcceptedDecompilerDisclaimer,
            FeatureOnOffOptions.UseEnhancedColors);
    }
}
