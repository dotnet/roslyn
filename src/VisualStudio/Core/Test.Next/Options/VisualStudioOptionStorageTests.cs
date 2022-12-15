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
    public void OptionsWithoutStorage()
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
           "InlineHintsOptions_DisplayAllOverride",
           "FeatureOnOffOptions_RenameTracking",
           "FeatureOnOffOptions_RefactoringVerification",
           "ExtensionManagerOptions_DisableCrashingExtensions",
           "SolutionCrawlerOptionsStorage_SolutionBackgroundAnalysisScopeOption",
           "CompletionOptions_ForceExpandedCompletionIndexCreation",
           "CompletionOptions_UpdateImportCompletionCacheInBackground",
           "FormattingOptions_WrappingColumn",
           "RazorDesignTimeDocumentFormattingOptions_UseTabs",
           "RazorDesignTimeDocumentFormattingOptions_TabSize",
           "InternalDiagnosticsOptions_RazorDiagnosticMode",
           "InternalDiagnosticsOptions_LiveShareDiagnosticMode",
           "SimplificationOptions_PreferAliasToQualification",
           "SimplificationOptions_PreferOmittingModuleNamesInQualification",
           "SimplificationOptions_PreferImplicitTypeInference",
           "SimplificationOptions_PreferImplicitTypeInLocalDeclaration",
           "SimplificationOptions_AllowSimplificationToGenericType",
           "SimplificationOptions_AllowSimplificationToBaseType",
           "SimplificationOptions_QualifyMemberAccessWithThisOrMe",
           "SimplificationOptions_QualifyFieldAccess",
           "SimplificationOptions_QualifyPropertyAccess",
           "SimplificationOptions_QualifyMethodAccess",
           "SimplificationOptions_QualifyEventAccess",
           "SimplificationOptions_PreferIntrinsicPredefinedTypeKeywordInDeclaration",
           "SimplificationOptions_PreferIntrinsicPredefinedTypeKeywordInMemberAccess",
           "RenameOptions_RenameOverloads",
           "RenameOptions_RenameInStrings",
           "RenameOptions_RenameInComments",
           "RenameOptions_PreviewChanges",
           "RecommendationOptions_HideAdvancedMembers",
           "RecommendationOptions_FilterOutOfScopeLocals",
           "end_of_line",
           "insert_final_newline",
           "dotnet_style_operator_placement_when_wrapping",
           "file_header_template",
           "dotnet_style_prefer_foreach_explicit_cast_in_source"
        }, optionsWithoutStorage);
    }
}
