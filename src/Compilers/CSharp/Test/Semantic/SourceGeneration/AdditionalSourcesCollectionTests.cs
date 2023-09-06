// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class AdditionalSourcesCollectionTests
         : CSharpTestBase
    {
        [Theory]
        [InlineData("abc", "abc.cs")]
        [InlineData("abc.cs")]
        [InlineData("abc+nested.cs")]
        [InlineData("abc`1.cs")]
        [InlineData("abc.vb", "abc.vb.cs")]
        [InlineData("abc.generated.cs")]
        [InlineData("abc_-_", "abc_-_.cs")]
        [InlineData("abc - generated.cs")]
        [InlineData("abc\u0064.cs", "abcd.cs")]
        [InlineData("abc(1).cs")]
        [InlineData("abc[1].cs")]
        [InlineData("abc{1}.cs")]
        [InlineData("..", "...cs")]
        [InlineData(".", "..cs")]
        [InlineData("abc/", "abc/.cs")]
        [InlineData("abc/ ", "abc/ .cs")]
        [InlineData("a/b/c", "a/b/c.cs")]
        [InlineData("a\\b/c", "a/b/c.cs")]
        [InlineData(" abc ", " abc .cs")]
        [InlineData(" abc/generated.cs")]
        [InlineData(" a/ b/ generated.cs")]
        [WorkItem(58476, "https://github.com/dotnet/roslyn/issues/58476")]
        public void HintName_ValidValues(string hintName, string? expectedName = null)
        {
            expectedName ??= hintName;

            AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".cs");
            asc.Add(hintName, SourceText.From("public class D{}", Encoding.UTF8));
            Assert.True(asc.Contains(expectedName));

            var sources = asc.ToImmutableAndFree();
            var source = Assert.Single(sources);
            Assert.True(source.HintName.EndsWith(".cs"));
            Assert.Equal(expectedName, source.HintName, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("abc")] // abc.vb
        [InlineData("abc.cs")] //abc.cs.vb
        [InlineData("abc.vb")] // abc.vb
        public void HintName_WithExtension(string hintName)
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".vb");
            asc.Add(hintName, SourceText.From("public class D{}", Encoding.UTF8));
            Assert.True(asc.Contains(hintName));

            var sources = asc.ToImmutableAndFree();
            Assert.Single(sources);
            Assert.True(sources[0].HintName.EndsWith(".vb"));
        }

        [Theory]
        [InlineData("/abc/def.cs")]
        [InlineData("\\")]
        [InlineData(":")]
        [InlineData("*")]
        [InlineData("?")]
        [InlineData("\"")]
        [InlineData("<")]
        [InlineData(">")]
        [InlineData("|")]
        [InlineData("\0")]
        [InlineData("abc\u00A0.cs")] // unicode non-breaking space
        [InlineData("/..")]
        [InlineData("\\..")]
        [InlineData("//")]
        [InlineData("\\\\")]
        [InlineData("a//b")]
        [InlineData("a\\\\b")]
        [InlineData("../")]
        [InlineData("./")]
        [InlineData(" /")]
        [InlineData(" /generated.cs")]
        [InlineData(" /a/generated.cs")]
        [InlineData(" /abc")]
        [InlineData(" /a/ b/c/ ")]
        [InlineData(" a/ b /c ")]
        [InlineData(" /a/ b/c/ generated.cs")]
        [InlineData(" a/ b /c generated.cs")]
        [InlineData(" abc /generated.cs")]
        public void HintName_InvalidValues(string hintName)
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".cs");
            var exception = Assert.Throws<ArgumentException>(nameof(hintName), () => asc.Add(hintName, SourceText.From("public class D{}", Encoding.UTF8)));

            Assert.Contains(hintName.Replace('\\', '/'), exception.Message);
        }

        [Fact]
        public void AddedSources_Are_Deterministic()
        {
            // a few manual simple ones
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".cs");
            asc.Add("file3.cs", SourceText.From("", Encoding.UTF8));
            asc.Add("file1.cs", SourceText.From("", Encoding.UTF8));
            asc.Add("file2.cs", SourceText.From("", Encoding.UTF8));
            asc.Add("file5.cs", SourceText.From("", Encoding.UTF8));
            asc.Add("file4.cs", SourceText.From("", Encoding.UTF8));

            var sources = asc.ToImmutableAndFree();
            var hintNames = sources.Select(s => s.HintName).ToArray();
            Assert.Equal(new[]
            {
                "file3.cs",
                "file1.cs",
                "file2.cs",
                "file5.cs",
                "file4.cs"
            }, hintNames);

            // generate a long random list, remembering the order we added them
            Random r = new Random();
            string[] names = new string[1000];
            asc = new AdditionalSourcesCollection(".cs");
            for (int i = 0; i < 1000; i++)
            {
                names[i] = CSharpTestBase.GetUniqueName() + ".cs";
                asc.Add(names[i], SourceText.From("", Encoding.UTF8));
            }

            sources = asc.ToImmutableAndFree();
            hintNames = sources.Select(s => s.HintName).ToArray();
            Assert.Equal(names, hintNames);
        }

        [Theory]
        [InlineData("file.cs", "file.cs")]
        [InlineData("file", "file")]
        [InlineData("file", "file.cs")]
        [InlineData("file.cs", "file")]
        [InlineData("file.cs", "file.CS")]
        [InlineData("file.CS", "file.cs")]
        [InlineData("file", "file.CS")]
        [InlineData("file.CS", "file")]
        [InlineData("File", "file")]
        [InlineData("file", "File")]
        public void Hint_Name_Must_Be_Unique(string hintName1, string hintName2)
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".cs");
            asc.Add(hintName1, SourceText.From("", Encoding.UTF8));
            var exception = Assert.Throws<ArgumentException>("hintName", () => asc.Add(hintName2, SourceText.From("", Encoding.UTF8)));

            Assert.Contains(hintName2, exception.Message);
        }

        [Fact]
        public void Hint_Name_Must_Be_Unique_When_Combining_Sources()
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".cs");
            asc.Add("hintName1", SourceText.From("", Encoding.UTF8));
            asc.Add("hintName2", SourceText.From("", Encoding.UTF8));

            AdditionalSourcesCollection asc2 = new AdditionalSourcesCollection(".cs");
            asc2.Add("hintName3", SourceText.From("", Encoding.UTF8));
            asc2.Add("hintName1", SourceText.From("", Encoding.UTF8));

            var exception = Assert.Throws<ArgumentException>("hintName", () => asc.CopyTo(asc2));
            Assert.Contains("hintName1", exception.Message);
        }

        [Theory]
        [InlineData("file.cs", "file.cs")]
        [InlineData("file", "file")]
        [InlineData("file", "file.cs")]
        [InlineData("file.cs", "file")]
        [InlineData("file.CS", "file")]
        [InlineData("file", "file.CS")]
        [InlineData("File", "file.cs")]
        [InlineData("File.cs", "file")]
        [InlineData("File.cs", "file.CS")]
        public void Contains(string addHintName, string checkHintName)
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".cs");
            asc.Add(addHintName, SourceText.From("", Encoding.UTF8));
            Assert.True(asc.Contains(checkHintName));
        }

        [Theory]
        [InlineData("file.cs", "file.cs")]
        [InlineData("file", "file")]
        [InlineData("file", "file.cs")]
        [InlineData("file.cs", "file")]
        public void Remove(string addHintName, string removeHintName)
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".cs");
            asc.Add(addHintName, SourceText.From("", Encoding.UTF8));
            asc.RemoveSource(removeHintName);
            var sources = asc.ToImmutableAndFree();
            Assert.Empty(sources);
        }

        [Fact]
        public void SourceTextRequiresEncoding()
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".cs");

            // fine
            asc.Add("file1.cs", SourceText.From("", Encoding.UTF8));
            asc.Add("file2.cs", SourceText.From("", Encoding.UTF32));
            asc.Add("file3.cs", SourceText.From("", Encoding.Unicode));

            // no encoding
            Assert.Throws<ArgumentException>(() => asc.Add("file4.cs", SourceText.From("")));

            // explicit null encoding
            Assert.Throws<ArgumentException>(() => asc.Add("file5.cs", SourceText.From("", encoding: null)));

            var exception = Assert.Throws<ArgumentException>(() => asc.Add("file5.cs", SourceText.From("", encoding: null)));

            // check the exception contains the expected hintName
            Assert.Contains("file5.cs", exception.Message);
        }
    }
}
