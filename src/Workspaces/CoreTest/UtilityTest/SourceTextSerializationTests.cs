// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class SourceTextSerializationTests
    {
        [Fact]
        public void TestSourceTextSerialization()
        {
            using var workspace = new AdhocWorkspace();
            var textService = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());

            var maxSize = SourceTextExtensions.SourceTextLengthThreshold * 3;
            var sb = new StringBuilder(0, maxSize);

            for (var i = 0; i < maxSize; i++)
            {
                var originalText = CreateSourceText(sb, i);

                using var stream = SerializableBytes.CreateWritableStream();

                using (var writer = new ObjectWriter(stream, leaveOpen: true))
                {
                    originalText.WriteTo(writer, CancellationToken.None);
                }

                stream.Position = 0;

                using var reader = ObjectReader.TryGetReader(stream);
                var recovered = SourceTextExtensions.ReadFrom(textService, reader, originalText.Encoding, originalText.ChecksumAlgorithm, CancellationToken.None);

                Assert.Equal(originalText.ToString(), recovered.ToString());
            }
        }

        private static SourceText CreateSourceText(StringBuilder sb, int size)
        {
            for (var i = sb.Length; i < size; i++)
            {
                sb.Append((char)('0' + (i % 10)));
            }

            return SourceText.From(sb.ToString());
        }
    }
}
