using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text
{
    public class SourceTextTests
    {
        [Fact]
        public void Encoding1()
        {
            Assert.Same(Encoding.UTF8, SourceText.From("foo", Encoding.UTF8).Encoding);
            Assert.Same(Encoding.Unicode, SourceText.From("foo", Encoding.Unicode).Encoding);
            Assert.Same(Encoding.Unicode, SourceText.From(new MemoryStream(Encoding.Unicode.GetBytes("foo")), Encoding.Unicode).Encoding);
        }

        [Fact]
        public void ChecksumAlgorithm1()
        {
            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From("foo").ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From("foo", checksumAlgorithm: SourceHashAlgorithm.Sha1).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha256, SourceText.From("foo", checksumAlgorithm: SourceHashAlgorithm.Sha256).ChecksumAlgorithm);

            var stream = new MemoryStream(Encoding.Unicode.GetBytes("foo"));

            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From(stream).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha1).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha256, SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha256).ChecksumAlgorithm);
        }

        [Fact]
        public void ContentEquals()
        {
            var f = SourceText.From("foo", Encoding.UTF8);

            Assert.True(f.ContentEquals(SourceText.From("foo", Encoding.UTF8)));
            Assert.False(f.ContentEquals(SourceText.From("fooo", Encoding.UTF8)));
            Assert.True(SourceText.From("foo", Encoding.UTF8).ContentEquals(SourceText.From("foo", Encoding.UTF8)));

            var e1 = EncodedStringText.Create(new MemoryStream(Encoding.Unicode.GetBytes("foo")), Encoding.Unicode);
            var e2 = EncodedStringText.Create(new MemoryStream(Encoding.UTF8.GetBytes("foo")), Encoding.UTF8);

            Assert.True(e1.ContentEquals(e1));
            Assert.True(f.ContentEquals(e1));
            Assert.True(e1.ContentEquals(f));

            Assert.True(e2.ContentEquals(e2));
            Assert.True(e1.ContentEquals(e2));
            Assert.True(e2.ContentEquals(e1));
        }
    }
}
