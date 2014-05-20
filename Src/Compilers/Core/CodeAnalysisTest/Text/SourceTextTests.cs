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
