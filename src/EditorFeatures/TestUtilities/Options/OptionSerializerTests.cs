// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Options;

public sealed class OptionSerializerTests
{
    [Theory, CombinatorialData]
    public void SerializationAndDeserializationForNullableBool([CombinatorialValues(true, false, null)] bool? value)
    {
        var options = new IOption2[]
        {
            CompletionViewOptionsStorage.EnableArgumentCompletionSnippets,
            FeatureOnOffOptions.OfferRemoveUnusedReferences,
            InheritanceMarginOptionsStorage.ShowInheritanceMargin,
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
            SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption,
            SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption,
            CompletionOptionsStorage.EnterKeyBehavior,
            CompletionOptionsStorage.SnippetsBehavior,
        };

        foreach (var option in options)
        {
            var defaultValue = option.DefaultValue;
            // The default value for Enum option should not be null.
            Contract.ThrowIfNull(defaultValue, $"Option: {option.Name}");
            VerifyEnumValues(option, defaultValue.GetType(), allowsPacalCase: true, allowsSnakeCase: false);

            // Test invalid cases
            VerifyEnumInvalidParse(option, defaultValue.GetType());
        }
    }

    [Fact]
    public void SerializationAndDeserializationForNullableEnum()
    {
        var options = new IOption2[]
        {
            new Option2<ConsoleColor?>("Name1", null),
            new Option2<ConsoleColor?>("Name2", ConsoleColor.Black),
        };

        foreach (var option in options)
        {
            var type = option.Definition.Type;
            var enumType = Nullable.GetUnderlyingType(type);
            // We are testing an nullable enum type, so the enum type can't be null.
            Contract.ThrowIfNull(enumType, $"Option: {option.Name}");

            // Test enum values
            VerifyEnumValues(option, enumType, allowsPacalCase: true, allowsSnakeCase: false);

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

    [Fact]
    public void SerializationAndDeserializationForEnum_SnakeCase()
    {
        var options = new IOption2[]
        {
            ImplementTypeOptionsStorage.InsertionBehavior,
            ImplementTypeOptionsStorage.PropertyGenerationBehavior,
        };

        foreach (var option in options)
        {
            var defaultValue = option.DefaultValue;
            // The default value for Enum option should not be null.
            Contract.ThrowIfNull(defaultValue, $"Option: {option.Name}");
            VerifyEnumValues(option, defaultValue.GetType(), allowsPacalCase: true, allowsSnakeCase: true);

            // Test invalid cases
            VerifyEnumInvalidParse(option, defaultValue.GetType());
        }
    }

    private static string PascalToSnakeCase(string str)
    {
        var builder = new StringBuilder();
        var prevIsLower = false;
        foreach (var c in str)
        {
            var lower = char.ToLowerInvariant(c);
            var isLower = lower == c;

            if (prevIsLower && !isLower && builder.Length > 0)
            {
                builder.Append('_');
            }

            builder.Append(lower);
            prevIsLower = isLower;
        }

        return builder.ToString();
    }

    private static void VerifyEnumValues(IOption2 option, Type enumType, bool allowsSnakeCase, bool allowsPacalCase)
    {
        var serializer = option.Definition.Serializer;
        var possibleEnumValues = enumType.GetEnumValues();
        foreach (var enumValue in possibleEnumValues)
        {
            var serializedValue = serializer.Serialize(enumValue);
            var expectedPascalCase = enumValue.ToString();
            var expectedSnakeCase = PascalToSnakeCase(expectedPascalCase);

            // if option allows snake case it should use it for serialization:
            Assert.Equal(allowsSnakeCase ? expectedSnakeCase : expectedPascalCase, serializedValue);

            if (allowsPacalCase)
            {
                VerifyParsing(expectedPascalCase);

                // parsing should be case-insensitive:
                VerifyParsing(expectedPascalCase.ToLowerInvariant());
            }

            if (allowsSnakeCase)
            {
                VerifyParsing(expectedSnakeCase);

                // parsing should be case-insensitive:
                VerifyParsing(expectedSnakeCase.ToUpperInvariant());
            }

            void VerifyParsing(string value)
            {
                Assert.True(
                    serializer.TryParse(value, out var deserializedResult),
                    $"Can't parse option: {option.Name}, value: {value}");

                Assert.Equal(enumValue, deserializedResult);
            }
        }
    }

    private static void VerifyEnumInvalidParse(IOption2 option, Type enumType)
    {
        var serializer = option.Definition.Serializer;
        Assert.False(serializer.TryParse("1", out _));
        Assert.False(serializer.TryParse(enumType.GetEnumNames().Join(","), out _));
    }
}
