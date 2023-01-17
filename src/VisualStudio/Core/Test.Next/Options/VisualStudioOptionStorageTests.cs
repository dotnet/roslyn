// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Microsoft.VisualStudio.LanguageServices.Options;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UnitTests;

public class VisualStudioOptionStorageTests
{
    [Fact]
    public void OptionValidation()
    {
        var storages = VisualStudioOptionStorage.Storages;
        var infos = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location));

        // Options with per-language values shouldn't be defined in language-specific assembly since then they wouldn't be applicable to the other language.

        var perLanguageOptionsDefinedInIncorrectAssembly =
            from info in infos
            where info.Value.Option.IsPerLanguage && info.Value.ContainingAssemblyLanguage is "CSharp" or "VisualBasic"
            select info.Key;

        Assert.Empty(perLanguageOptionsDefinedInIncorrectAssembly);

        // language specific options have correct name prefix and are defined in language specific assemblies:

        var languageSpecificOptionsHaveIncorrectPrefix =
            from info in infos
            where info.Value.Option is not IPublicOption // public options do not need to follow the naming pattern
            where info.Value.Option.Definition.IsEditorConfigOption // TODO: remove condition once all options have config name https://github.com/dotnet/roslyn/issues/65787
            where info.Key.StartsWith(OptionDefinition.CSharpConfigNamePrefix, StringComparison.Ordinal) != info.Value.ContainingAssemblyLanguage is "CSharp" ||
                  info.Key.StartsWith(OptionDefinition.VisualBasicConfigNamePrefix, StringComparison.Ordinal) != info.Value.ContainingAssemblyLanguage is "VisualBasic"
            select info.Key;

        Assert.Empty(languageSpecificOptionsHaveIncorrectPrefix);

        // each option that has associated public option is exposed via a public accessor

        var publicOptionsWithoutPublicAccessor =
            from info in infos
            where info.Value.Accessors.Any(a => a.option.PublicOption != null)
            where !info.Value.Accessors.Any(a => a.isPublic)
            select info.Key;

        Assert.Empty(publicOptionsWithoutPublicAccessor);

        // Options with per-langauge values specify %LANGUAGE% in the storage key, and vice versa.

        var optionsWithIncorrectLanguageSubstitution =
            from info in infos
            let option = info.Value.Option
            where option.IsPerLanguage !=
                  VisualStudioOptionStorage.Storages.TryGetValue(info.Key, out var storage) &&
                  storage is VisualStudioOptionStorage.RoamingProfileStorage { IsPerLanguage: true }
            select info.Key;

        Assert.Empty(optionsWithIncorrectLanguageSubstitution);

        // no two option names map to the same storage (however, there may be multiple option definitions that share the same option name and storage):

        var duplicateRoamingProfileStorages =
            from storage in storages
            let roamingStorageKey = storage.Value is VisualStudioOptionStorage.RoamingProfileStorage { Key: var key } ? key : null
            where roamingStorageKey is not null
            group storage.Key by roamingStorageKey into g
            where g.Count() > 1
            select string.Join(",", g);

        Assert.Empty(duplicateRoamingProfileStorages);

        // each storage is used by an option:

        var unusedStorageMappings =
            from configName in storages.Keys
            where !infos.ContainsKey(configName)
            select configName;

        Assert.Empty(unusedStorageMappings);

        // following options have no VS storage (update upon adding new storage-less option):

        var optionsWithoutStorage =
            from info in infos
            where !storages.ContainsKey(info.Key)
            orderby info.Key
            select info.Key;

        AssertEx.Equal(new[]
        {
            "CompletionOptions_ForceExpandedCompletionIndexCreation",                       // test-only option
            "CSharpFormattingOptions_NewLinesForBracesInAccessors",                         // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_NewLinesForBracesInAnonymousMethods",                  // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_NewLinesForBracesInAnonymousTypes",                    // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_NewLinesForBracesInControlBlocks",                     // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_NewLinesForBracesInLambdaExpressionBody",              // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_NewLinesForBracesInMethods",                           // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_NewLinesForBracesInObjectCollectionArrayInitializers", // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_NewLinesForBracesInProperties",                        // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_NewLinesForBracesInTypes",                             // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_SpaceWithinCastParentheses",                           // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_SpaceWithinExpressionParentheses",                     // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "CSharpFormattingOptions_SpaceWithinOtherParentheses",                          // public option deserialized via CSharpVisualStudioOptionStorageReadFallbacks
            "dotnet_style_operator_placement_when_wrapping",                                // Doesn't have VS UI. TODO: https://github.com/dotnet/roslyn/issues/66062
            "dotnet_style_prefer_foreach_explicit_cast_in_source",                          // For a small customer segment, doesn't warrant VS UI.
            "dotnet_remove_unnecessary_suppression_exclusions",                             // Doesn't have VS UI. TODO: https://github.com/dotnet/roslyn/issues/66062
            "end_of_line",                                                                  // persisted by the editor
            "ExtensionManagerOptions_DisableCrashingExtensions",                            // TODO: remove? https://github.com/dotnet/roslyn/issues/66063
            "FeatureOnOffOptions_RefactoringVerification",                                  // TODO: remove? https://github.com/dotnet/roslyn/issues/66063 
            "FeatureOnOffOptions_RenameTracking",                                           // TODO: remove? https://github.com/dotnet/roslyn/issues/66063
            "file_header_template",                                                         // repository specific
            "FormattingOptions_WrappingColumn",                                             // TODO: https://github.com/dotnet/roslyn/issues/66062
            "InlineHintsOptions_DisplayAllOverride",                                        // TODO: https://github.com/dotnet/roslyn/issues/57283
            "insert_final_newline",                                                         // TODO: https://github.com/dotnet/roslyn/issues/66062
            "InternalDiagnosticsOptions_LiveShareDiagnosticMode",                           // TODO: remove once switched to LSP diagnostics
            "InternalDiagnosticsOptions_RazorDiagnosticMode",                               // TODO: remove once switched to LSP diagnostics
            "RazorDesignTimeDocumentFormattingOptions_TabSize",                             // TODO: remove once Razor removes design-time documents
            "RazorDesignTimeDocumentFormattingOptions_UseTabs",                             // TODO: remove once Razor removes design-time documents
            "RecommendationOptions_FilterOutOfScopeLocals",                                 // public option not stored in VS storage
            "RecommendationOptions_HideAdvancedMembers",                                    // public option not stored in VS storage
            "RenameOptions_PreviewChanges",                                                 // public option, deprecated
            "RenameOptions_RenameInComments",                                               // public option, deprecated
            "RenameOptions_RenameInStrings",                                                // public option, deprecated
            "RenameOptions_RenameOverloads",                                                // public option, deprecated
            "SimplificationOptions_AllowSimplificationToBaseType",                          // public option, deprecated
            "SimplificationOptions_AllowSimplificationToGenericType",                       // public option, deprecated
            "SimplificationOptions_PreferAliasToQualification",                             // public option, deprecated
            "SimplificationOptions_PreferImplicitTypeInference",                            // public option, deprecated
            "SimplificationOptions_PreferImplicitTypeInLocalDeclaration",                   // public option, deprecated
            "SimplificationOptions_PreferIntrinsicPredefinedTypeKeywordInDeclaration",      // public option, deprecated
            "SimplificationOptions_PreferIntrinsicPredefinedTypeKeywordInMemberAccess",     // public option, deprecated
            "SimplificationOptions_PreferOmittingModuleNamesInQualification",               // public option, deprecated
            "SimplificationOptions_QualifyEventAccess",                                     // public option, deprecated
            "SimplificationOptions_QualifyFieldAccess",                                     // public option, deprecated
            "SimplificationOptions_QualifyMemberAccessWithThisOrMe",                        // public option, deprecated
            "SimplificationOptions_QualifyMethodAccess",                                    // public option, deprecated
            "SimplificationOptions_QualifyPropertyAccess",                                  // public option, deprecated
            "SolutionCrawlerOptionsStorage_SolutionBackgroundAnalysisScopeOption",          // handled by PackageSettingsPersister
        }, optionsWithoutStorage);
    }
}
