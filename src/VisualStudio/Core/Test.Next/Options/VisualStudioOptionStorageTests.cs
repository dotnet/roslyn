// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class VisualStudioOptionStorageTests
{
    public static IEnumerable<object[]> ConfigNames
    {
        get
        {
            return OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location))
                .Select(pair => new object[] { pair.Key });
        }
    }

    public static IEnumerable<object[]> PerLanguageConfigNames
    {
        get
        {
            return OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location))
                .Where(pair => pair.Value.Option.IsPerLanguage)
                .Select(pair => new object[] { pair.Key });
        }
    }

    public static IEnumerable<object[]> PublicOptionConfigNames
    {
        get
        {
            return OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location))
                .Where(pair => pair.Value.Accessors.Any(a => a.option.PublicOption is not null))
                .Select(pair => new object[] { pair.Key });
        }
    }

    public static IEnumerable<object[]> ConfigNamesWithRoamingProfileStorage
    {
        get
        {
            return OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location))
                .Where(pair => VisualStudioOptionStorage.Storages.TryGetValue(pair.Key, out var storage) && storage is VisualStudioOptionStorage.RoamingProfileStorage)
                .Select(pair => new object[] { pair.Key });
        }
    }

    public static IEnumerable<object[]> StorageNames
    {
        get
        {
            return VisualStudioOptionStorage.Storages
                .Select(pair => new object[] { pair.Key });
        }
    }

    /// <summary>
    /// Options with per-language values shouldn't be defined in language-specific assembly since then they wouldn't be
    /// applicable to the other language.
    /// </summary>
    [Theory]
    [MemberData(nameof(PerLanguageConfigNames), DisableDiscoveryEnumeration = true)]
    public void PerLanguageOptionDefinedInCorrectAssembly(string configName)
    {
        var infos = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location));
        var info = infos[configName];

        // This test should only be operating on per-language options
        Assert.True(info.Option.IsPerLanguage);

        var anyInCSharpNamespace = info.Accessors.Any(a => a.namespaceName.Contains("CSharp"));
        var anyInVisualBasicNamespace = info.Accessors.Any(a => a.namespaceName.Contains("VisualBasic"));
        var allInCSharpNamespace = info.Accessors.All(a => a.namespaceName.Contains("CSharp"));
        var allInVisualBasicNamespace = info.Accessors.All(a => a.namespaceName.Contains("VisualBasic"));
        if (anyInCSharpNamespace == allInCSharpNamespace)
            return;

        if (anyInVisualBasicNamespace == allInVisualBasicNamespace)
            return;

        // This is a per-language option, so verify it is defined in a correct assembly
        Assert.True(anyInCSharpNamespace || anyInVisualBasicNamespace);
    }

    /// <summary>
    /// Language-specific options have correct name prefix and are defined in language-specific assemblies.
    /// </summary>
    [Theory]
    [MemberData(nameof(ConfigNames), DisableDiscoveryEnumeration = true)]
    public void LanguageSpecificOptionsHaveCorrectPrefix(string configName)
    {
        var infos = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location));
        var info = infos[configName];

        if (info.Option is IPublicOption)
        {
            // public options do not need to follow the naming pattern
            return;
        }

        // TODO: https://github.com/dotnet/roslyn/issues/65787
        if (info.Option.Name is
            "csharp_format_on_return" or
            "csharp_format_on_typing" or
            "csharp_format_on_semicolon" or
            "csharp_format_on_close_brace" or
            "csharp_enable_inlay_hints_for_types" or
            "csharp_enable_inlay_hints_for_implicit_variable_types" or
            "csharp_enable_inlay_hints_for_lambda_parameter_types" or
            "csharp_enable_inlay_hints_for_implicit_object_creation" or
            "csharp_enable_inlay_hints_for_collection_expressions")
        {
            return;
        }

        if (info.Accessors.Any(a => a.namespaceName.Contains("CSharp")))
        {
            Assert.StartsWith(OptionDefinition.CSharpConfigNamePrefix, configName);
        }
        else if (info.Accessors.Any(a => a.namespaceName.Contains("VisualBasic")))
        {
            Assert.StartsWith(OptionDefinition.VisualBasicConfigNamePrefix, configName);
        }
        else
        {
            Assert.False(configName.StartsWith(OptionDefinition.CSharpConfigNamePrefix, StringComparison.OrdinalIgnoreCase));
            Assert.False(configName.StartsWith(OptionDefinition.VisualBasicConfigNamePrefix, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Each option that has associated public option is exposed via a public accessor.
    /// </summary>
    /// <param name="configName"></param>
    [Theory]
    [MemberData(nameof(PublicOptionConfigNames), DisableDiscoveryEnumeration = true)]
    public void PublicOptionHasPublicAccessor(string configName)
    {
        var storages = VisualStudioOptionStorage.Storages;
        var infos = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location));
        var info = infos[configName];

        // This method should only be validating public options
        Assert.Contains(info.Accessors, accessor => accessor.option.PublicOption is not null);

        // This public option should also have a public accessor
        Assert.Contains(info.Accessors, accessor => accessor.isPublic);
    }

    /// <summary>
    /// Options with per-language values specify %LANGUAGE% in the storage key, and vice versa.
    /// </summary>
    [Theory]
    [MemberData(nameof(ConfigNamesWithRoamingProfileStorage), DisableDiscoveryEnumeration = true)]
    public void OptionHasCorrectLanguageSubstitution(string configName)
    {
        var infos = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location));
        var info = infos[configName];
        var option = info.Option;
        var storage = (VisualStudioOptionStorage.RoamingProfileStorage)VisualStudioOptionStorage.Storages[configName];

        Assert.Equal(option.IsPerLanguage, storage.IsPerLanguage);
    }

    [Fact]
    public void StorageMappingsAreUnique()
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
    }

    /// <summary>
    /// Each storage is used by an option.
    /// </summary>
    [Theory]
    [MemberData(nameof(StorageNames), DisableDiscoveryEnumeration = true)]
    public void StorageMapsToOption(string storageName)
    {
        var infos = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location));

        Assert.True(infos.ContainsKey(storageName));
    }

    /// <summary>
    /// Options have no VS storage except for known storage-less cases.
    /// </summary>
    [Theory]
    [MemberData(nameof(ConfigNames), DisableDiscoveryEnumeration = true)]
    public void OptionHasStorageIfNecessary(string configName)
    {
        var storages = VisualStudioOptionStorage.Storages;
        if (storages.ContainsKey(configName))
        {
            // This option has storage
            return;
        }

        var optionsWithoutStorage = new[]
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
            "dotnet_remove_unnecessary_suppression_exclusions",                             // Doesn't have VS UI. TODO: https://github.com/dotnet/roslyn/issues/66062
            "dotnet_style_operator_placement_when_wrapping",                                // Doesn't have VS UI. TODO: https://github.com/dotnet/roslyn/issues/66062
            "dotnet_style_prefer_foreach_explicit_cast_in_source",                          // For a small customer segment, doesn't warrant VS UI.
            "dotnet_binary_log_path",                                                       // VSCode only option for the VS Code project system; does not apply to VS
            "dotnet_enable_automatic_restore",                                              // VSCode only option for the VS Code project system; does not apply to VS
            "dotnet_enable_file_based_programs",                                            // VSCode only option for the VS Code project system; does not apply to VS
            "dotnet_lsp_using_devkit",                                                      // VSCode internal only option.  Does not need any UI.
            "dotnet_enable_references_code_lens",                                           // VSCode only option.  Does not apply to VS.
            "dotnet_enable_tests_code_lens",                                                // VSCode only option.  Does not apply to VS.
            "dotnet_enable_auto_insert",                                                    // VSCode only option.  Does not apply to VS.
            "dotnet_organize_imports_on_format",                                            // VSCode only option.  Does not apply to VS.
            "end_of_line",                                                                  // persisted by the editor
            "ExtensionManagerOptions_DisableCrashingExtensions",                            // TODO: remove? https://github.com/dotnet/roslyn/issues/66063
            "FeatureOnOffOptions_RefactoringVerification",                                  // TODO: remove? https://github.com/dotnet/roslyn/issues/66063
            "FeatureOnOffOptions_RenameTracking",                                           // TODO: remove? https://github.com/dotnet/roslyn/issues/66063
            "file_header_template",                                                         // repository specific
            "csharp_binary_expression_wrapping",                                           // EditorConfig only option, no VS UI  
            "csharp_indent_wrapped_call_chains",                                           // EditorConfig only option, no VS UI
            "csharp_wrap_call_chains",                                                      // EditorConfig only option, no VS UI
            "csharp_parameter_wrapping",                                                    // EditorConfig only option, no VS UI
            "csharp_parameter_first_placement",                                            // EditorConfig only option, no VS UI
            "csharp_parameter_alignment",                                                   // EditorConfig only option, no VS UI
            "dotnet_unsupported_wrapping_column",                                           // TODO: https://github.com/dotnet/roslyn/issues/66062
            "insert_final_newline",                                                         // TODO: https://github.com/dotnet/roslyn/issues/66062
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
        };

        Assert.Contains(configName, optionsWithoutStorage);
    }

    [Fact]
    public void VerifyOptionGroupUnique()
    {
        var allOptionGroups = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(VisualStudioOptionStorage).Assembly.Location))
            .Values
            .Select(optionTestInfo => optionTestInfo.Option.Definition.Group)
            .Distinct();

        var allGroupNames = allOptionGroups.Select(GetFullOptionGroupName);

        // The full name of each Option group should be unique. Full name is obtained by joining the names of all groups that are chained via parent reference.
        // e.g. option group, code_style -> prefer_object_initializer.
        // Its full name code_style.prefer_object_initializer should be unique.
        var set = new HashSet<string>();
        foreach (var groupName in allGroupNames)
        {
            Assert.True(set.Add(groupName), $"Group {groupName} doesn't have a unique name.");
        }

        static string GetFullOptionGroupName(OptionGroup group)
        {
            var builder = new StringBuilder();
            var currentGroup = group;
            while (currentGroup != null)
            {
                var stringToInsert = builder.Length == 0 ? currentGroup.Name : currentGroup.Name + ".";
                builder.Insert(0, stringToInsert);
                currentGroup = currentGroup.Parent;
            }

            return builder.ToString();
        }
    }
}
