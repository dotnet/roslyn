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

namespace Microsoft.CodeAnalysis.UnitTests;

public class VisualStudioOptionStorageTests
{
    [Fact]
    public void OptionValidation()
    {
        var storages = VisualStudioOptionStorage.Storages;
        var infos = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location));

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
            "CompletionOptions_ForceExpandedCompletionIndexCreation",                     // test-only option
            "dotnet_style_operator_placement_when_wrapping",                              // TODO: https://github.com/dotnet/roslyn/issues/66062
            "dotnet_style_prefer_foreach_explicit_cast_in_source",                        // For a small customer segment, doesn't warrant VS UI.
            "end_of_line",                                                                // persisted by the editor
            "ExtensionManagerOptions_DisableCrashingExtensions",                          // TODO: remove? https://github.com/dotnet/roslyn/issues/66063
            "FeatureOnOffOptions_RefactoringVerification",                                // TODO: remove? https://github.com/dotnet/roslyn/issues/66063 
            "FeatureOnOffOptions_RenameTracking",                                         // TODO: remove? https://github.com/dotnet/roslyn/issues/66063
            "file_header_template",                                                       // repository specific
            "FormattingOptions_WrappingColumn",                                           // TODO: https://github.com/dotnet/roslyn/issues/66062
            "InlineHintsOptions_DisplayAllOverride",                                      // TODO: https://github.com/dotnet/roslyn/issues/57283
            "insert_final_newline",                                                       // TODO: https://github.com/dotnet/roslyn/issues/66062
            "InternalDiagnosticsOptions_LiveShareDiagnosticMode",                         // TODO: remove once switched to LSP diagnostics
            "InternalDiagnosticsOptions_RazorDiagnosticMode",                             // TODO: remove once switched to LSP diagnostics
            "RazorDesignTimeDocumentFormattingOptions_TabSize",                           // TODO: remove once Razor removes design-time documents
            "RazorDesignTimeDocumentFormattingOptions_UseTabs",                           // TODO: remove once Razor removes design-time documents
            "RecommendationOptions_FilterOutOfScopeLocals",                               // public option
            "RecommendationOptions_HideAdvancedMembers",                                  // public option
            "RenameOptions_PreviewChanges",                                               // public option, deprecated
            "RenameOptions_RenameInComments",                                             // public option, deprecated
            "RenameOptions_RenameInStrings",                                              // public option, deprecated
            "RenameOptions_RenameOverloads",                                              // public option, deprecated
            "SimplificationOptions_AllowSimplificationToBaseType",                        // public option, deprecated
            "SimplificationOptions_AllowSimplificationToGenericType",                     // public option, deprecated
            "SimplificationOptions_PreferAliasToQualification",                           // public option, deprecated
            "SimplificationOptions_PreferImplicitTypeInference",                          // public option, deprecated
            "SimplificationOptions_PreferImplicitTypeInLocalDeclaration",                 // public option, deprecated
            "SimplificationOptions_PreferIntrinsicPredefinedTypeKeywordInDeclaration",    // public option, deprecated
            "SimplificationOptions_PreferIntrinsicPredefinedTypeKeywordInMemberAccess",   // public option, deprecated
            "SimplificationOptions_PreferOmittingModuleNamesInQualification",             // public option, deprecated
            "SimplificationOptions_QualifyEventAccess",                                   // public option, deprecated
            "SimplificationOptions_QualifyFieldAccess",                                   // public option, deprecated
            "SimplificationOptions_QualifyMemberAccessWithThisOrMe",                      // public option, deprecated
            "SimplificationOptions_QualifyMethodAccess",                                  // public option, deprecated
            "SimplificationOptions_QualifyPropertyAccess",                                // public option, deprecated
            "SolutionCrawlerOptionsStorage_SolutionBackgroundAnalysisScopeOption",        // handled by PackageSettingsPersister
        }, optionsWithoutStorage);
    }
}
