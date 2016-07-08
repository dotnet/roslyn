// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SourceReferenceResolverTests
    {
        private class Resolver : SourceReferenceResolver
        {
            private readonly string _s;
            private readonly int _index;
            private readonly int _count;

            public Resolver(string s, int index, int count)
            {
                _s = s;
                _index = index;
                _count = count;
            }

            public override bool Equals(object other) => ReferenceEquals(this, other);
            public override int GetHashCode() => 42;
            public override string ResolveReference(string path, string baseFilePath) => path;
            public override string NormalizePath(string path, string baseFilePath) => path;

            public override Stream OpenRead(string resolvedPath) => new MemoryStream(
                Encoding.ASCII.GetBytes(_s), _index, _count, writable: false, publiclyVisible: true);
        }

        [Fact]
        public void MemoryStreamInResolver()
        {
            SourceText text = new Resolver("AB", 0, 2).ReadText("ignored");

            Assert.Equal("AB", text.ToString());
        }

        [Fact]
        public void CountMemoryStreamInResolver()
        {
            SourceText text = new Resolver("AB", 0, 1).ReadText("ignored");

            Assert.Equal("A", text.ToString());
        }

        [WorkItem(12348, "https://github.com/dotnet/roslyn/issues/12348")]
        [Fact]
        public void IndexCountMemoryStreamInResolver()
        {
            SourceText text = new Resolver("AB", 1, 1).ReadText("ignored");

            Assert.Equal("B", text.ToString());
        }
    }
}
