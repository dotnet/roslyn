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
    [ExportGlobalOptionProvider, Shared]
    internal sealed class FeatureOnOffOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FeatureOnOffOptions()
        {
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            EndConstruct,
            AutomaticInsertionOfAbstractOrInterfaceMembers,
            LineSeparator,
            Outlining,
            KeywordHighlighting,
            ReferenceHighlighting,
            AutoInsertBlockCommentStartString,
            PrettyListing,
            RenameTrackingPreview,
            RenameTracking,
            RefactoringVerification,
            AddImportsOnPaste,
            OfferRemoveUnusedReferences,
            OfferRemoveUnusedReferencesFeatureFlag,
            ShowInheritanceMargin,
            InheritanceMarginCombinedWithIndicatorMargin,
            AutomaticallyCompleteStatementOnSemicolon,
            SkipAnalyzersForImplicitlyTriggeredBuilds);

        private const string FeatureName = "FeatureOnOffOptions";

        public static readonly PerLanguageOption2<bool> EndConstruct = new(FeatureName, "EndConstruct", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AutoEndInsert"));

        // This value is only used by Visual Basic, and so is using the old serialization name that was used by VB.
        public static readonly PerLanguageOption2<bool> AutomaticInsertionOfAbstractOrInterfaceMembers = new(FeatureName, "AutomaticInsertionOfAbstractOrInterfaceMembers", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AutoRequiredMemberInsert"));

        public static readonly PerLanguageOption2<bool> LineSeparator = new(FeatureName, "LineSeparator", defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.DisplayLineSeparators" : "TextEditor.%LANGUAGE%.Specific.Line Separator"));

        public static readonly PerLanguageOption2<bool> Outlining = new(FeatureName, "Outlining", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Outlining"));

        public static readonly PerLanguageOption2<bool> KeywordHighlighting = new(FeatureName, "KeywordHighlighting", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.EnableHighlightRelatedKeywords" : "TextEditor.%LANGUAGE%.Specific.Keyword Highlighting"));

        public static readonly PerLanguageOption2<bool> ReferenceHighlighting = new(FeatureName, "ReferenceHighlighting", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.EnableHighlightReferences" : "TextEditor.%LANGUAGE%.Specific.Reference Highlighting"));

        public static readonly PerLanguageOption2<bool> AutoInsertBlockCommentStartString = new(FeatureName, "AutoInsertBlockCommentStartString", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Insert Block Comment Start String"));

        public static readonly PerLanguageOption2<bool> PrettyListing = new(FeatureName, "PrettyListing", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PrettyListing"));

        public static readonly PerLanguageOption2<bool> StringIdentation = new(FeatureName, "StringIdentation", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.StringIdentation"));

        public static readonly PerLanguageOption2<bool> RenameTrackingPreview = new(FeatureName, "RenameTrackingPreview", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.RenameTrackingPreview" : "TextEditor.%LANGUAGE%.Specific.Rename Tracking Preview"));

        /// <summary>
        /// This option is not currently used by Roslyn, but we might want to implement it in the
        /// future. Keeping the option while it's unimplemented allows all upgrade paths to
        /// maintain any customized value for this setting, even through versions that have not
        /// implemented this feature yet.
        /// </summary>
        public static readonly PerLanguageOption2<bool> RenameTracking = new(FeatureName, "RenameTracking", defaultValue: true);

        /// <summary>
        /// This option is not currently used by Roslyn, but we might want to implement it in the
        /// future. Keeping the option while it's unimplemented allows all upgrade paths to
        /// maintain any customized value for this setting, even through versions that have not
        /// implemented this feature yet.
        /// </summary>
        public static readonly PerLanguageOption2<bool> RefactoringVerification = new(
            FeatureName, "RefactoringVerification", defaultValue: false);

        public static readonly Option2<bool> NavigateAsynchronously = new(
            FeatureName, "NavigateAsynchronously", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.NavigateAsynchronously"));

        public static readonly PerLanguageOption2<bool?> AddImportsOnPaste = new(
            FeatureName, "AddImportsOnPaste", defaultValue: null,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AddImportsOnPaste"));

        public static readonly Option2<bool?> OfferRemoveUnusedReferences = new(
            FeatureName, "OfferRemoveUnusedReferences", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.OfferRemoveUnusedReferences"));

        public static readonly Option2<bool> OfferRemoveUnusedReferencesFeatureFlag = new(
            FeatureName, "OfferRemoveUnusedReferencesFeatureFlag", defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.RemoveUnusedReferences"));

        public static readonly PerLanguageOption2<bool?> ShowInheritanceMargin = new(
            FeatureName, "ShowInheritanceMargin", defaultValue: true,
            new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowInheritanceMargin"));

        public static readonly Option2<bool> InheritanceMarginCombinedWithIndicatorMargin = new(
            FeatureName, "InheritanceMarginCombinedWithIndicatorMargin", defaultValue: false,
            new RoamingProfileStorageLocation("TextEditor.InheritanceMarginCombinedWithIndicatorMargin"));

        public static readonly PerLanguageOption2<bool> InheritanceMarginIncludeGlobalImports = new(
            FeatureName, "InheritanceMarginIncludeGlobalImports", defaultValue: true,
            new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.InheritanceMarginIncludeGlobalImports"));

        public static readonly Option2<bool> AutomaticallyCompleteStatementOnSemicolon = new(
            FeatureName, "AutomaticallyCompleteStatementOnSemicolon", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.AutomaticallyCompleteStatementOnSemicolon"));

        public static readonly PerLanguageOption2<bool> AutomaticallyFixStringContentsOnPaste = new(
            FeatureName, "AutomaticallyFixStringContentsOnPaste", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.AutomaticallyFixStringContentsOnPaste"));

        /// <summary>
        /// Not used by Roslyn but exposed in C# and VB option UI. Used by TestWindow and Project System.
        /// TODO: remove https://github.com/dotnet/roslyn/issues/57253
        /// </summary>
        public static readonly Option2<bool> SkipAnalyzersForImplicitlyTriggeredBuilds = new(
            FeatureName, "SkipAnalyzersForImplicitlyTriggeredBuilds", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.SkipAnalyzersForImplicitlyTriggeredBuilds"));
    }
}
