// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal class TestFormattingLoggerFactory(ITestOutputHelper testOutputHelper) : IFormattingLoggerFactory
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    public IFormattingLogger? CreateLogger(string documentFilePath, string formattingType)
    {
        var logger = new TestFormattingLogger(_testOutputHelper);
        logger.LogMessage($"{formattingType} formatting for {documentFilePath}");
        return logger;
    }

    private class TestFormattingLogger(ITestOutputHelper testOutputHelper) : IFormattingLogger
    {
        private readonly HashSet<string> _loggedNames = [];

        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        public void LogMessage(string message)
        {
            _testOutputHelper.WriteLine(message);
        }

        public void LogObject<T>(string name, T value)
        {
            Assert.True(_loggedNames.Add(name), $"The name '{name}' has already been logged. Names must be unique per formatter run.");
            _testOutputHelper.WriteLine($"{name}:");
            _testOutputHelper.WriteLine(JsonSerializer.Serialize(value, JsonHelpers.JsonSerializerOptions));
        }

        public void LogSourceText(string name, SourceText sourceText)
        {
            Assert.True(_loggedNames.Add(name), $"The name '{name}' has already been logged. Names must be unique per formatter run.");
            _testOutputHelper.WriteLine("--------------------------------");
            _testOutputHelper.WriteLine($"{name}:");
            _testOutputHelper.WriteLine(sourceText.ToString());
            _testOutputHelper.WriteLine("--------------------------------");
        }
    }
}
