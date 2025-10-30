// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Analyzers.Utilities.UnitTests
{
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
    /// <summary>
    /// Tests for <see cref="Analyzer.Utilities.AdditionalFileProvider"/>.
    /// </summary>
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
    public sealed class AdditionalFileProviderTests
    {
        [Theory]
        [InlineData([new string[] { }])]
        [InlineData([new string[] { "b.txt" }])]
        [InlineData([new string[] { "a.bat" }])]
        public void DesiredFileMissing_GetFile_ReturnsNull(string[] fileNames)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(fileNames));

            var file = fileProvider.GetFile("a.txt");

            Assert.Null(file);
        }

        [Theory]
        [InlineData("a.txt")]
        [InlineData("a.txt", "b.txt")]
        [InlineData("b.txt", "a.txt")]
        [InlineData("a.bat", "a.txt")]
        public void DesiredFilePresent_GetFile_ReturnsFile(params string[] fileNames)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(fileNames));

            var file = fileProvider.GetFile("a.txt");

            Assert.NotNull(file);
            Assert.Equal("a.txt", file.Path);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("a.+")]
        [InlineData("a.tx")]
        public void DesiredFilePresent_GetFileWithoutExactName_ReturnsNull(string fileName)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles("a.txt"));

            var file = fileProvider.GetFile(fileName);

            Assert.Null(file);
        }

        [Fact]
        public void DesiredFilePresentMoreThanOnce_GetFile_ReturnsFirstFile()
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(("a.txt", "1"), ("b.txt", "2"), ("a.txt", "3")));

            var file = fileProvider.GetFile("a.txt");
            Assert.NotNull(file);
            Assert.Equal("a.txt", file.Path);

            var text = file.GetText();
            Assert.NotNull(text);
            Assert.Equal("1", text.ToString());
        }

        [Theory]
        [InlineData(new string[] { }, ".")]
        [InlineData(new[] { "a.txt" }, "c")]
        public void MatchingFilesMissing_GetMatchingFiles_ReturnsEmptyEnumerable(IEnumerable<string> fileNames, string pattern)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(fileNames.ToArray()));

            var files = fileProvider.GetMatchingFiles(pattern);

            Assert.Empty(files);
        }

        [Theory]
        [InlineData(new[] { "a.txt" }, "a", new[] { "a.txt" })]
        [InlineData(new[] { "a.txt", "b.txt" }, @"\w\.", new[] { "a.txt", "b.txt" })]
        [InlineData(new[] { "a.txt", "b.txt", "c.bat" }, @"\.txt", new[] { "a.txt", "b.txt" })]
        public void MatchingFilesPresent_GetMatchingFiles_ReturnsMatchingFiles(IEnumerable<string> fileNames, string pattern, IEnumerable<string> expectedFileNames)
        {
            var fileProvider = new AdditionalFileProvider(CreateAdditionalFiles(fileNames.ToArray()));

            var files = fileProvider.GetMatchingFiles(pattern);

            Assert.Equal(expectedFileNames, files.Select(x => x.Path));
        }

        private static ImmutableArray<AdditionalText> CreateAdditionalFiles(params (string FileName, string Content)[] fileNameAndContentGroups)
            => ImmutableArray.Create(fileNameAndContentGroups.Select(x => CreateAdditionalFile(x.FileName, x.Content)).ToArray());

        private static ImmutableArray<AdditionalText> CreateAdditionalFiles(params string[] fileNames)
            => ImmutableArray.Create(fileNames.Select(x => CreateAdditionalFile(x)).ToArray());

        private static AdditionalText CreateAdditionalFile(string fileName, string content = "")
            => new FakeAdditionalText(fileName, content);

        private sealed class FakeAdditionalText : AdditionalText
        {
            private readonly SourceText _text;

            public FakeAdditionalText(string path, string text = "")
            {
                Path = path;
                _text = SourceText.From(text);
            }

            public override string Path { get; }

            public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
        }
    }
}
