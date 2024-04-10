// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public class GlobalOptionsTests
{
    [Export(typeof(IGlobalOptionService)), Shared, PartNotDiscoverable]
    internal class TestGlobalOptions : IGlobalOptionService
    {
        public readonly List<OptionKey2> AccessedOptionKeys = [];

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestGlobalOptions()
        {
        }

        private void OnOptionAccessed(OptionKey2 key)
        {
            AccessedOptionKeys.Add(key);
        }

        bool IOptionsReader.TryGetOption<T>(OptionKey2 optionKey, out T value)
        {
            value = GetOption<T>(optionKey);
            return true;
        }

        public T GetOption<T>(Option2<T> option)
            => GetOption<T>(new OptionKey2(option));

        public T GetOption<T>(PerLanguageOption2<T> option, string languageName)
            => GetOption<T>(new OptionKey2(option, languageName));

        public T GetOption<T>(OptionKey2 optionKey)
        {
            OnOptionAccessed(optionKey);
            return (T)OptionsTestHelpers.GetDifferentValue(typeof(T), optionKey.Option.DefaultValue)!;
        }

        #region Unused

        public ImmutableArray<object?> GetOptions(ImmutableArray<OptionKey2> optionKeys)
            => throw new NotImplementedException();

        public bool RefreshOption(OptionKey2 optionKey, object? newValue)
            => throw new NotImplementedException();

        public void SetGlobalOption<T>(Option2<T> option, T value)
            => throw new NotImplementedException();

        public void SetGlobalOption<T>(PerLanguageOption2<T> option, string language, T value)
            => throw new NotImplementedException();

        public void SetGlobalOption(OptionKey2 optionKey, object? value)
            => throw new NotImplementedException();

        public bool SetGlobalOptions(ImmutableArray<KeyValuePair<OptionKey2, object?>> options)
            => throw new NotImplementedException();

        public void AddOptionChangedHandler(object target, EventHandler<OptionChangedEventArgs> handler)
            => throw new NotImplementedException();

        public void RemoveOptionChangedHandler(object target, EventHandler<OptionChangedEventArgs> handler)
            => throw new NotImplementedException();

        #endregion
    }

    private static void VerifyDataMembersHaveNonDefaultValues(object options, object defaultOptions, string? language = null)
    {
        Assert.Equal(options.GetType(), defaultOptions.GetType());
        Recurse(options.GetType(), options, defaultOptions, language);

        static void Recurse(Type type, object options, object defaultOptions, string? language)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetCustomAttributes<DataMemberAttribute>().Any())
                {
                    // value initialized from global options:
                    var value = property.GetValue(options);

                    // default value for the option -- may be different then default(T):
                    var defaultValue = property.GetValue(defaultOptions);

                    if (OptionDefinition.IsSupportedOptionType(property.PropertyType))
                    {
                        if (IsStoredInGlobalOptions(property, language))
                        {
                            Assert.False(Equals(value, defaultValue), $"{type.FullName}.{property.Name} not initialized from global options");
                        }
                    }
                    else
                    {
                        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                        if (propertyType != property.PropertyType)
                        {
                            var getValueOrDefault = property.PropertyType.GetMethod("GetValueOrDefault", []);
                            value = getValueOrDefault.Invoke(value, []);
                            defaultValue = getValueOrDefault.Invoke(defaultValue, []);
                        }

                        Recurse(propertyType, value, defaultValue, language);
                    }
                }
            }
        }
    }

    private static TestWorkspace CreateWorkspace(out TestGlobalOptions globalOptions)
    {
        var composition = EditorTestCompositions.LanguageServerProtocolEditorFeatures.
            AddExcludedPartTypes(typeof(GlobalOptionService)).
            AddParts(typeof(TestGlobalOptions));

        var workspace = new TestWorkspace(composition: composition);
        globalOptions = Assert.IsType<TestGlobalOptions>(workspace.ExportProvider.GetExportedValue<IGlobalOptionService>());
        return workspace;
    }

    /// <summary>
    /// Properties for options not stored in global options.
    /// </summary>
    private static bool IsStoredInGlobalOptions(PropertyInfo property, string? language)
        => !(property.DeclaringType == typeof(AddImportPlacementOptions) && property.Name == nameof(AddImportPlacementOptions.AllowInHiddenRegions) ||
             property.DeclaringType == typeof(AddImportPlacementOptions) && property.Name == nameof(AddImportPlacementOptions.UsingDirectivePlacement) && language == LanguageNames.VisualBasic ||
             property.DeclaringType == typeof(DocumentFormattingOptions) && property.Name == nameof(DocumentFormattingOptions.FileHeaderTemplate) ||
             property.DeclaringType == typeof(DocumentFormattingOptions) && property.Name == nameof(DocumentFormattingOptions.InsertFinalNewLine) ||
             property.DeclaringType == typeof(ClassificationOptions) && property.Name == nameof(ClassificationOptions.FrozenPartialSemantics) ||
             property.DeclaringType == typeof(HighlightingOptions) && property.Name == nameof(HighlightingOptions.FrozenPartialSemantics) ||
             property.DeclaringType == typeof(BlockStructureOptions) && property.Name == nameof(BlockStructureOptions.IsMetadataAsSource));

    /// <summary>
    /// Our mock <see cref="IGlobalOptionService"/> implementation returns a non-default value for each option it reads.
    /// Option objects initialized from this service thus should have all their data properties initialized to
    /// non-default values. We then enumerate these properties via reflection and compare each property value with the
    /// default instance of the respective options type.
    /// </summary>
    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public void ReadingOptionsFromGlobalOptions(string language)
    {
        using var workspace = CreateWorkspace(out var globalOptions);
        var languageServices = workspace.Services.SolutionServices.GetLanguageServices(language);

        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetIdeAnalyzerOptions(languageServices), IdeAnalyzerOptions.GetDefault(languageServices), language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetCodeActionOptions(languageServices), CodeActionOptions.GetDefault(languageServices), language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetBraceMatchingOptions(language), BraceMatchingOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetFindUsagesOptions(language), FindUsagesOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetInlineHintsOptions(language), InlineHintsOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetAutoFormattingOptions(language), AutoFormattingOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetBlockStructureOptions(language, isMetadataAsSource: false), BlockStructureOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetDocumentationCommentOptions(globalOptions.GetLineFormattingOptions(language), language), DocumentationCommentOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetExtractMethodOptions(language), ExtractMethodOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetImplementTypeOptions(language), ImplementTypeOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetMetadataAsSourceOptions(languageServices), MetadataAsSourceOptions.GetDefault(languageServices), language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetSignatureHelpOptions(language), SignatureHelpOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetSymbolSearchOptions(language), SymbolSearchOptions.Default, language);
        VerifyDataMembersHaveNonDefaultValues(globalOptions.GetWorkspaceConfigurationOptions(), WorkspaceConfigurationOptions.Default);
    }
}
