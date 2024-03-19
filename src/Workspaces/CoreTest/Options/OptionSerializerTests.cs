// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Options;

public class OptionSerializerTests
{
    [Theory, CombinatorialData]
    public void SerializationAndDeserializationForNullableBool([CombinatorialValues(true, false, null)] bool? value)
    {
        var options = new IOption2[]
        {
            CompletionViewOptionsStorage.EnableArgumentCompletionSnippets,
            FeatureOnOffOptions.OfferRemoveUnusedReferences,
            InheritanceMarginOptionsStorage.ShowInheritanceMargin,
            WorkspaceConfigurationOptionsStorage.EnableOpeningSourceGeneratedFilesInWorkspace,
            SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFiles,
            CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
            CompletionOptionsStorage.ShowNewSnippetExperienceUserOption,
            CompletionOptionsStorage.TriggerOnDeletion,
        };

        foreach (var option in options)
        {
            var serializer = option.Definition.Serializer;
            var serializedValue = serializer.Serialize(value);
            switch (value)
            {
                case null:
                    Assert.Equal("null", serializedValue);
                    break;
                case true:
                    Assert.Equal("true", serializedValue);
                    break;
                case false:
                    Assert.Equal("false", serializedValue);
                    break;
                default:
                    throw ExceptionUtilities.Unreachable();
            }

            foreach (var possibleString in new[] { serializedValue, serializedValue.ToUpper() })
            {
                var success = serializer.TryParse(possibleString, out var parsedResult);
                Assert.True(success, $"Can't parse option: {option.Name}, value: {possibleString}");
                Assert.Equal(value, parsedResult);
            }
        }
    }

    [Fact]
    public void SerializationAndDeserializationForEnum()
    {
        var options = new IOption2[]
        {
            InlineDiagnosticsOptionsStorage.Location,
            WorkspaceConfigurationOptionsStorage.Database,
            SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption,
            SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption,
            ImplementTypeOptionsStorage.InsertionBehavior,
            ImplementTypeOptionsStorage.PropertyGenerationBehavior,
            CompletionOptionsStorage.EnterKeyBehavior,
            CompletionOptionsStorage.SnippetsBehavior,
        };

        foreach (var option in options)
        {
            var defaultValue = option.DefaultValue;
            // The default value for Enum option should not be null.
            Contract.ThrowIfNull(defaultValue, $"Option: {option.Name}");
            VerifyEnumValues(option, defaultValue.GetType());

            // Test invalid cases
            VerifyEnumInvalidParse(option, defaultValue.GetType());
        }
    }

    [Fact]
    public void SerializationAndDeserializationForNullableEnum()
    {
        var options = new IOption2[]
        {
            SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption,
        };

        foreach (var option in options)
        {
            var type = option.Definition.Type;
            var enumType = Nullable.GetUnderlyingType(type);
            // We are testing an nullable enum type, so the enum type can't be null.
            Contract.ThrowIfNull(enumType, $"Option: {option.Name}");

            // Test enum values
            VerifyEnumValues(option, enumType);

            // Test null
            var serializer = option.Definition.Serializer;
            var nullValue = serializer.Serialize(null);
            Assert.Equal("null", nullValue);
            var success = serializer.TryParse(nullValue, out var deserializedResult);
            Assert.True(success, $"Can't parse option for null. Option: {option.Name}");
            Assert.Null(deserializedResult);

            // Test invalid cases
            VerifyEnumInvalidParse(option, enumType);
        }
    }

    private static void VerifyEnumValues(IOption2 option, Type enumType)
    {
        var serializer = option.Definition.Serializer;
        var possibleEnumValues = enumType.GetEnumValues();
        foreach (var enumValue in possibleEnumValues)
        {
            var serializedValue = serializer.Serialize(enumValue);
            Assert.Equal(enumValue.ToString(), serializedValue);
            var success = serializer.TryParse(serializedValue, out var deserializedResult);
            Assert.True(success, $"Can't parse option: {option.Name}, value: {serializedValue}");
            Assert.Equal(enumValue, deserializedResult);
        }
    }

    private static void VerifyEnumInvalidParse(IOption2 option, Type enumType)
    {
        var serializer = option.Definition.Serializer;
        Assert.False(serializer.TryParse("1", out _));
        Assert.False(serializer.TryParse(enumType.GetEnumNames().Join(","), out _));
    }
}
