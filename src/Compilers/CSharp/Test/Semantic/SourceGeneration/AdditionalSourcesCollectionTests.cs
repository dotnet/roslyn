﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Xunit;
namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class AdditionalSourcesCollectionTests
         : CSharpTestBase
    {
        [Theory]
        [InlineData("abc")] // abc.cs
        [InlineData("abc.cs")] //abc.cs
        [InlineData("abc.vb")] // abc.vb.cs
        [InlineData("abc.generated.cs")]
        [InlineData("abc_-_")]
        [InlineData("abc - generated.cs")]
        [InlineData("abc\u0064.cs")] //acbd.cs
        [InlineData("abc(1).cs")]
        [InlineData("abc[1].cs")]
        [InlineData("abc{1}.cs")]
        public void HintName_ValidValues(string hintName)
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection();
            asc.Add(hintName, SourceText.From("public class D{}", Encoding.UTF8));
            Assert.True(asc.Contains(hintName));

            var sources = asc.ToImmutableAndFree();
            Assert.True(sources[0].HintName.EndsWith(".cs"));

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
        public void HintName_InvalidValues(string hintName)
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection();
            Assert.Throws<ArgumentException>(nameof(hintName), () => asc.Add(hintName, SourceText.From("public class D{}", Encoding.UTF8)));
        }

        [Fact]
        public void AddedSources_Are_Deterministic()
        {
            // a few manual simple ones
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection();
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
            asc = new AdditionalSourcesCollection();
            for (int i = 0; i < 1000; i++)
            {
                names[i] = r.NextDouble().ToString() + ".cs";
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
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection();
            asc.Add(hintName1, SourceText.From("", Encoding.UTF8));
            Assert.Throws<ArgumentException>("hintName", () => asc.Add(hintName2, SourceText.From("", Encoding.UTF8)));
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
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection();
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
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection();
            asc.Add(addHintName, SourceText.From("", Encoding.UTF8));
            asc.RemoveSource(removeHintName);
            var sources = asc.ToImmutableAndFree();
            Assert.Empty(sources);
        }

        [Fact]
        public void SourceTextRequiresEncoding()
        {
            AdditionalSourcesCollection asc = new AdditionalSourcesCollection();

            // fine
            asc.Add("file1.cs", SourceText.From("", Encoding.UTF8));
            asc.Add("file2.cs", SourceText.From("", Encoding.UTF32));
            asc.Add("file3.cs", SourceText.From("", Encoding.Unicode));

            // no encoding
            Assert.Throws<ArgumentException>(() => asc.Add("file4.cs", SourceText.From("")));

            // explicit null encoding
            Assert.Throws<ArgumentException>(() => asc.Add("file5.cs", SourceText.From("", encoding: null)));
        }
    }
}
