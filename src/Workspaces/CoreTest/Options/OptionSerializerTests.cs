// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.UnitTests.Options;

public class OptionSerializerTests
{
    [Theory]
    [CombinatorialData]
    public void SerializationAndDeserializationForNullableBoolean(bool? value)
    {
        var options = new List<IOption2>()
        {
            CompletionViewOptions.EnableArgumentCompletionSnippets,
            FeatureOnOffOptions.OfferRemoveUnusedReferences,
            FeatureOnOffOptions.ShowInheritanceMargin,
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

            var success = serializer.TryParse(serializedValue, out var parsedResult);
            Assert.True(success, $"Can't parse option: {option.Name}, value: {serializedValue}");
            Assert.Equal(value, parsedResult);
        }
    }
}
