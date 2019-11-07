// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Test.Utilities
{
    public class TestAdditionalDocument : AdditionalText
    {
        private readonly string _filePath;
        private readonly string _fileName;
        private readonly SourceText? _sourceText;

        public TestAdditionalDocument(string fileName, string text)
            : this(fileName, fileName, text)
        {
        }

        public TestAdditionalDocument(TextDocument textDocument)
            : this(textDocument.FilePath, textDocument.Name, textDocument.GetTextAsync(CancellationToken.None).Result.ToString())
        {
        }

        public TestAdditionalDocument(string filePath, string fileName, string text)
        {
            _filePath = filePath;
            _fileName = fileName;

            if (text != null)
            {
                _sourceText = SourceText.From(text);
            }
        }

        public override string Path => _filePath;
        public string Name => _fileName;

        public override SourceText? GetText(CancellationToken cancellationToken = default)
            => _sourceText;
    }
}
