// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    internal static class FeatureOnOffOptions
    {
        public static readonly PerLanguageOption2<bool> EndConstruct = new(nameof(FeatureOnOffOptions), nameof(EndConstruct), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AutoEndInsert"));

        // This value is only used by Visual Basic, and so is using the old serialization name that was used by VB.
        public static readonly PerLanguageOption2<bool> AutomaticInsertionOfAbstractOrInterfaceMembers = new(nameof(FeatureOnOffOptions), nameof(AutomaticInsertionOfAbstractOrInterfaceMembers), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AutoRequiredMemberInsert"));

        public static readonly PerLanguageOption2<bool> LineSeparator = new(nameof(FeatureOnOffOptions), nameof(LineSeparator), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.DisplayLineSeparators" : "TextEditor.%LANGUAGE%.Specific.Line Separator"));

        public static readonly PerLanguageOption2<bool> Outlining = new(nameof(FeatureOnOffOptions), nameof(Outlining), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Outlining"));

        public static readonly PerLanguageOption2<bool> KeywordHighlighting = new(nameof(FeatureOnOffOptions), nameof(KeywordHighlighting), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.EnableHighlightRelatedKeywords" : "TextEditor.%LANGUAGE%.Specific.Keyword Highlighting"));

        public static readonly PerLanguageOption2<bool> ReferenceHighlighting = new(nameof(FeatureOnOffOptions), nameof(ReferenceHighlighting), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.EnableHighlightReferences" : "TextEditor.%LANGUAGE%.Specific.Reference Highlighting"));

        public static readonly PerLanguageOption2<bool> FormatOnPaste = new(nameof(FeatureOnOffOptions), nameof(FormatOnPaste), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.FormatOnPaste"));

        public static readonly PerLanguageOption2<bool> AutoInsertBlockCommentStartString = new(nameof(FeatureOnOffOptions), nameof(AutoInsertBlockCommentStartString), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Insert Block Comment Start String"));

        public static readonly PerLanguageOption2<bool> PrettyListing = new(nameof(FeatureOnOffOptions), nameof(PrettyListing), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PrettyListing"));

        public static readonly PerLanguageOption2<bool> AutoFormattingOnTyping = new(
            nameof(FeatureOnOffOptions), nameof(AutoFormattingOnTyping), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Typing"));

        public static readonly PerLanguageOption2<bool> AutoFormattingOnCloseBrace = new(
            nameof(FeatureOnOffOptions), nameof(AutoFormattingOnCloseBrace), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Close Brace"));

        public static readonly PerLanguageOption2<bool> AutoFormattingOnSemicolon = new(
            nameof(FeatureOnOffOptions), nameof(AutoFormattingOnSemicolon), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Semicolon"));

        public static readonly PerLanguageOption2<bool> RenameTrackingPreview = new(nameof(FeatureOnOffOptions), nameof(RenameTrackingPreview), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.RenameTrackingPreview" : "TextEditor.%LANGUAGE%.Specific.Rename Tracking Preview"));

        /// <summary>
        /// This option is currently used by Roslyn, but we might want to implement it in the
        /// future. Keeping the option while it's unimplemented allows all upgrade paths to
        /// maintain any customized value for this setting, even through versions that have not
        /// implemented this feature yet.
        /// </summary>
        public static readonly PerLanguageOption2<bool> RenameTracking = new(nameof(FeatureOnOffOptions), nameof(RenameTracking), defaultValue: true);

        /// <summary>
        /// This option is currently used by Roslyn, but we might want to implement it in the
        /// future. Keeping the option while it's unimplemented allows all upgrade paths to
        /// maintain any customized value for this setting, even through versions that have not
        /// implemented this feature yet.
        /// </summary>
        public static readonly PerLanguageOption2<bool> RefactoringVerification = new(
            nameof(FeatureOnOffOptions), nameof(RefactoringVerification), defaultValue: false);

        public static readonly PerLanguageOption2<bool> StreamingGoToImplementation = new(
            nameof(FeatureOnOffOptions), nameof(StreamingGoToImplementation), defaultValue: true);

        public static readonly Option2<bool> NavigateToDecompiledSources = new(
            nameof(FeatureOnOffOptions), nameof(NavigateToDecompiledSources), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.{nameof(NavigateToDecompiledSources)}"));

        public static readonly Option2<int> UseEnhancedColors = new(
            nameof(FeatureOnOffOptions), nameof(UseEnhancedColors), defaultValue: 1,
            storageLocations: new RoamingProfileStorageLocation("WindowManagement.Options.UseEnhancedColorsForManagedLanguages"));

        public static readonly PerLanguageOption2<bool> AddImportsOnPaste = new(
            nameof(FeatureOnOffOptions), nameof(AddImportsOnPaste), defaultValue: false);
    }

    [ExportOptionProvider, Shared]
    internal class FeatureOnOffOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
            FeatureOnOffOptions.UseEnhancedColors,
            FeatureOnOffOptions.AddImportsOnPaste);
    }
}
