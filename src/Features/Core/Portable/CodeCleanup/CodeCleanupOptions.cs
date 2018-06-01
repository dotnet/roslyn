// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal static class CodeCleanupOptions
    {
        public static readonly PerLanguageOption<bool> IsCodeCleanupRulesConfigured = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(IsCodeCleanupRulesConfigured), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Is Code Cleanup Rules Configured"));

        public static readonly PerLanguageOption<bool> RemoveUnusedUsings = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(RemoveUnusedUsings), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Remove Unused Usings"));

        public static readonly PerLanguageOption<bool> SortUsings = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(SortUsings), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Sort Usings"));

        public static readonly PerLanguageOption<bool> FixImplicitExplicitType = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(FixImplicitExplicitType), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Fix Implicit Explicit Type"));

        public static readonly PerLanguageOption<bool> FixThisQualification = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(FixThisQualification), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Fix This Qualification"));

        public static readonly PerLanguageOption<bool> FixFrameworkTypes = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(FixFrameworkTypes), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Fix Framework Types"));

        public static readonly PerLanguageOption<bool> FixAddRemoveBraces = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(FixAddRemoveBraces), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Fix Add Remove Braces"));

        public static readonly PerLanguageOption<bool> FixAccessibilityModifiers = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(FixAccessibilityModifiers), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Fix Accessibility Modifiers"));

        public static readonly PerLanguageOption<bool> SortAccessibilityModifiers = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(SortAccessibilityModifiers), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Sort Accessibility Modifiers"));

        public static readonly PerLanguageOption<bool> MakeReadonly = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(MakeReadonly), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Make Readonly"));

        public static readonly PerLanguageOption<bool> RemoveUnnecessaryCasts = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(RemoveUnnecessaryCasts), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Remove Unnecessary Casts"));

        public static readonly PerLanguageOption<bool> FixExpressionBodiedMembers = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(FixExpressionBodiedMembers), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Fix Expression Bodied Members"));

        public static readonly PerLanguageOption<bool> FixInlineVariableDeclarations = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(FixInlineVariableDeclarations), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Fix Inline Variable Declarations"));

        public static readonly PerLanguageOption<bool> RemoveUnusedVariables = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(RemoveUnusedVariables), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Remove Unused Variables"));

        public static readonly PerLanguageOption<bool> FixObjectCollectionInitialization = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(FixObjectCollectionInitialization), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Fix Object Collection Initialization"));

        public static readonly PerLanguageOption<bool> FixLanguageFeatures = new PerLanguageOption<bool>(
            nameof(CodeCleanupOptions), nameof(FixLanguageFeatures), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Fix Language Features"));
    }

    [ExportOptionProvider, Shared]
    internal class CodeCleanupOptionsProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            CodeCleanupOptions.FixAccessibilityModifiers,
            CodeCleanupOptions.FixAddRemoveBraces,
            CodeCleanupOptions.FixExpressionBodiedMembers,
            CodeCleanupOptions.FixFrameworkTypes,
            CodeCleanupOptions.FixImplicitExplicitType,
            CodeCleanupOptions.FixInlineVariableDeclarations,
            CodeCleanupOptions.FixLanguageFeatures,
            CodeCleanupOptions.FixObjectCollectionInitialization,
            CodeCleanupOptions.FixThisQualification,
            CodeCleanupOptions.IsCodeCleanupRulesConfigured,
            CodeCleanupOptions.MakeReadonly,
            CodeCleanupOptions.RemoveUnnecessaryCasts,
            CodeCleanupOptions.RemoveUnusedUsings,
            CodeCleanupOptions.RemoveUnusedVariables,
            CodeCleanupOptions.SortAccessibilityModifiers,
            CodeCleanupOptions.SortUsings
            );
    }
}
