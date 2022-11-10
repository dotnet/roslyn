// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Formatting;
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
        public readonly List<OptionKey> AccessedOptionKeys = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestGlobalOptions()
        {
        }

        private void OnOptionAccessed(OptionKey key)
        {
            AccessedOptionKeys.Add(key);
        }

        public T GetOption<T>(Option2<T> option)
        {
            OnOptionAccessed(new OptionKey(option));
            return (T)GetNonEqualValue(typeof(T), option.DefaultValue);
        }

        public T GetOption<T>(PerLanguageOption2<T> option, string? languageName)
        {
            OnOptionAccessed(new OptionKey(option, languageName));
            return (T)GetNonEqualValue(typeof(T), option.DefaultValue);
        }

        public object? GetOption(OptionKey optionKey)
            => throw new NotImplementedException();

        #region Unused

        public void RegisterWorkspace(Workspace workspace)
        {
        }

        public void UnregisterWorkspace(Workspace workspace)
        {
        }

#pragma warning disable CS0067
        public event EventHandler<OptionChangedEventArgs>? OptionChanged;
#pragma warning restore

        public ImmutableArray<object?> GetOptions(ImmutableArray<OptionKey> optionKeys)
            => throw new NotImplementedException();

        public void RefreshOption(OptionKey optionKey, object? newValue)
            => throw new NotImplementedException();

        public void SetGlobalOption(OptionKey optionKey, object? value)
            => throw new NotImplementedException();

        public void SetGlobalOptions(ImmutableArray<OptionKey> optionKeys, ImmutableArray<object?> values)
            => throw new NotImplementedException();

        public void SetOptions(OptionSet optionSet, IEnumerable<OptionKey> optionKeys)
            => throw new NotImplementedException();

        #endregion
    }

    /// <summary>
    /// True if the type is a type of an option value.
    /// </summary>
    private static bool IsOptionValueType(Type type)
    {
        type = GetNonNullableType(type);

        return
            type == typeof(bool) ||
            type == typeof(int) ||
            type == typeof(string) ||
            type.IsEnum ||
            type == typeof(NamingStylePreferences) ||
            typeof(ICodeStyleOption).IsAssignableFrom(type);
    }

    private static Type GetNonNullableType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? type.GetGenericArguments()[0] : type;

    /// <summary>
    /// Returns another value of the same type that's not equal to the specified <paramref name="value"/>.
    /// </summary>
    private static object GetNonEqualValue(Type type, object? value)
    {
        Assert.True(IsOptionValueType(type));

        switch (value)
        {
            case bool b:
                return !b;

            case int i:
                return i == 0 ? 1 : 0;

            case string s:
                return "!" + s;

            case ICodeStyleOption codeStyle:
                return codeStyle
                    .WithValue(GetNonEqualValue(codeStyle.GetType().GetGenericArguments()[0], codeStyle.Value))
                    .WithNotification((codeStyle.Notification == NotificationOption2.Error) ? NotificationOption2.Warning : NotificationOption2.Error);

            case NamingStylePreferences naming:
                return naming.IsEmpty ? NamingStylePreferences.Default : NamingStylePreferences.Empty;

            default:
                if (value != null && type.IsEnum)
                {
                    var zero = Enum.ToObject(type, 0);
                    return value.Equals(zero) ? Enum.ToObject(type, 1) : zero;
                }

                throw TestExceptionUtilities.UnexpectedValue(value);
        }
    }

    private static void VerifyDataMembersHaveNonDefaultValues(object options, object defaultOptions, string language)
    {
        Assert.Equal(options.GetType(), defaultOptions.GetType());
        Recurse(options.GetType(), options, defaultOptions, language);

        static void Recurse(Type type, object options, object defaultOptions, string language)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetCustomAttributes<DataMemberAttribute>().Any())
                {
                    // value initialized from global options:
                    var value = property.GetValue(options);

                    // default value for the option -- may be different then default(T):
                    var defaultValue = property.GetValue(defaultOptions);

                    if (IsOptionValueType(property.PropertyType))
                    {
                        if (IsStoredInGlobalOptions(property, language))
                        {
                            Assert.False(Equals(value, defaultValue), $"{type.FullName}.{property.Name} not initialized from global options");
                        }
                    }
                    else
                    {
                        var propertyType = GetNonNullableType(property.PropertyType);

                        if (propertyType != property.PropertyType)
                        {
                            var getValueOrDefault = property.PropertyType.GetMethod("GetValueOrDefault", Array.Empty<Type>());
                            value = getValueOrDefault.Invoke(value, Array.Empty<object>());
                            defaultValue = getValueOrDefault.Invoke(defaultValue, Array.Empty<object>());
                        }

                        Recurse(propertyType, value, defaultValue, language);
                    }
                }
            }
        }
    }

    private static TestWorkspace CreateWorkspace(out TestGlobalOptions globalOptions)
    {
        var composition = EditorTestCompositions.LanguageServerProtocol.
            AddExcludedPartTypes(typeof(GlobalOptionService)).
            AddParts(typeof(TestGlobalOptions));

        var workspace = new TestWorkspace(composition: composition);
        globalOptions = Assert.IsType<TestGlobalOptions>(workspace.ExportProvider.GetExportedValue<IGlobalOptionService>());
        return workspace;
    }

    /// <summary>
    /// Properties for options not stored in global options.
    /// </summary>
    private static bool IsStoredInGlobalOptions(PropertyInfo property, string language)
        => !(property.DeclaringType == typeof(AddImportPlacementOptions) && property.Name == nameof(AddImportPlacementOptions.AllowInHiddenRegions) ||
             property.DeclaringType == typeof(AddImportPlacementOptions) && property.Name == nameof(AddImportPlacementOptions.UsingDirectivePlacement) && language == LanguageNames.VisualBasic ||
             property.DeclaringType == typeof(DocumentFormattingOptions) && property.Name == nameof(DocumentFormattingOptions.FileHeaderTemplate) ||
             property.DeclaringType == typeof(DocumentFormattingOptions) && property.Name == nameof(DocumentFormattingOptions.InsertFinalNewLine) ||
             property.DeclaringType == typeof(ClassificationOptions) && property.Name == nameof(ClassificationOptions.ForceFrozenPartialSemanticsForCrossProcessOperations) ||
             property.DeclaringType == typeof(BlockStructureOptions) && property.Name == nameof(BlockStructureOptions.IsMetadataAsSource));

    /// <summary>
    /// Our mock <see cref="IGlobalOptionService"/> implementation returns a non-default value for each option it reads.
    /// Option objects initialized from this service thus should have all their data properties initialized to non-default values.
    /// We then enumerate these properties via reflection and compare each property value with the default instance of the respective options type.
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
    }
}
