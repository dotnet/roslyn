// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Test.Utilities
{
    /// <summary>
    /// An implementation of AdditionalText meant specifically for unit-testing.
    /// </summary>
    public class TestAdditionalDocument : AdditionalText
    {
        private readonly SourceText _sourceText;

        public TestAdditionalDocument(string fileName, string text)
            : this(fileName, fileName, text)
        {
        }

        public TestAdditionalDocument(TextDocument textDocument)
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            : this(textDocument.FilePath, textDocument.Name, textDocument.GetTextAsync(CancellationToken.None).Result.ToString())
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        {
        }

        public TestAdditionalDocument(string filePath, string fileName, string text)
        {
            Path = filePath;
            Name = fileName;
            _sourceText = SourceText.From(text);
        }

        public override string Path { get; }

        public string Name { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _sourceText;
    }
}
