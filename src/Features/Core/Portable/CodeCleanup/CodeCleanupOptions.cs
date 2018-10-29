// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal static class CodeCleanupOptions
    {
        // runtime only option. it is not saved anywhere
        public static readonly PerLanguageOption<bool> CodeCleanupInfoBarShown = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(CodeCleanupInfoBarShown), defaultValue: false);

        public static readonly PerLanguageOption<bool> NeverShowCodeCleanupInfoBarAgain = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(NeverShowCodeCleanupInfoBarAgain), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Never Show Code Cleanup Info Bar Again"));

        public static readonly PerLanguageOption<bool> PerformAdditionalCodeCleanupDuringFormatting = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(PerformAdditionalCodeCleanupDuringFormatting), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Perform Additional Code Cleanup During Formatting"));

        public static readonly PerLanguageOption<bool> RemoveUnusedImports = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(RemoveUnusedImports), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Remove Unused Imports"));

        public static readonly PerLanguageOption<bool> SortImports = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(SortImports), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Sort Imports"));

        public static readonly PerLanguageOption<bool> AddRemoveBracesForSingleLineControlStatements = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(AddRemoveBracesForSingleLineControlStatements), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Add Remove Braces For Single Line Control Statements"));

        public static readonly PerLanguageOption<bool> AddAccessibilityModifiers = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(AddAccessibilityModifiers), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Add Accessibility Modifiers"));

        public static readonly PerLanguageOption<bool> SortAccessibilityModifiers = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(SortAccessibilityModifiers), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Sort Accessibility Modifiers"));

        public static readonly PerLanguageOption<bool> ApplyExpressionBlockBodyPreferences = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(ApplyExpressionBlockBodyPreferences), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Apply Expression Block Body Preferences"));

        public static readonly PerLanguageOption<bool> ApplyImplicitExplicitTypePreferences = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(ApplyImplicitExplicitTypePreferences), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Apply Implicit Explicit Type Preferences"));

        public static readonly PerLanguageOption<bool> ApplyInlineOutVariablePreferences = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(ApplyInlineOutVariablePreferences), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Apply Inline Out Variable Preferences"));

        public static readonly PerLanguageOption<bool> ApplyLanguageFrameworkTypePreferences = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(ApplyLanguageFrameworkTypePreferences), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Apply Language Framework Type Preferences"));

        public static readonly PerLanguageOption<bool> ApplyObjectCollectionInitializationPreferences = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(ApplyObjectCollectionInitializationPreferences), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Apply Object Collection Initialization Preferences"));

        public static readonly PerLanguageOption<bool> ApplyThisQualificationPreferences = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(ApplyThisQualificationPreferences), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Apply This Qualification Preferences"));

        public static readonly PerLanguageOption<bool> MakePrivateFieldReadonlyWhenPossible = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(MakePrivateFieldReadonlyWhenPossible), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Make Private Field Readonly When Possible"));

        public static readonly PerLanguageOption<bool> RemoveUnnecessaryCasts = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(RemoveUnnecessaryCasts), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Remove Unnecessary Casts"));

        public static readonly PerLanguageOption<bool> RemoveUnusedVariables = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(RemoveUnusedVariables), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Remove Unused Variables"));
    }

    [ExportOptionProvider, Shared]
    internal class CodeCleanupOptionsProvider : IOptionProvider
    {
        public static ImmutableArray<IOption> SingletonOptions { get; } = ImmutableArray.Create<IOption>(
            CodeCleanupOptions.CodeCleanupInfoBarShown,
            CodeCleanupOptions.AddAccessibilityModifiers,
            CodeCleanupOptions.AddRemoveBracesForSingleLineControlStatements,
            CodeCleanupOptions.ApplyExpressionBlockBodyPreferences,
            CodeCleanupOptions.ApplyLanguageFrameworkTypePreferences,
            CodeCleanupOptions.ApplyImplicitExplicitTypePreferences,
            CodeCleanupOptions.ApplyInlineOutVariablePreferences,
            CodeCleanupOptions.ApplyObjectCollectionInitializationPreferences,
            CodeCleanupOptions.ApplyThisQualificationPreferences,
            CodeCleanupOptions.MakePrivateFieldReadonlyWhenPossible,
            CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain,
            CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting,
            CodeCleanupOptions.RemoveUnnecessaryCasts,
            CodeCleanupOptions.RemoveUnusedImports,
            CodeCleanupOptions.RemoveUnusedVariables,
            CodeCleanupOptions.SortAccessibilityModifiers,
            CodeCleanupOptions.SortImports
        );

        public ImmutableArray<IOption> Options { get; } = SingletonOptions;
    }
}
