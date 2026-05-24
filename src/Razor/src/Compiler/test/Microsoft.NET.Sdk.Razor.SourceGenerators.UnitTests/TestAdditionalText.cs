// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
 
namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    public sealed class TestAdditionalText : AdditionalText
    {
        private readonly SourceText _text;
 
        public TestAdditionalText(string path, SourceText text)
        {
            Path = path;
            _text = text;
        }
 
        public TestAdditionalText(string text = "", Encoding encoding = null, string path = "dummy")
            : this(path, SourceText.From(text, encoding))
        {
        }
 
        public override string Path { get; }
 
        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}
