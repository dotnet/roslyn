// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SourceTextSerializationTests
    {
        [Fact]
        public void TestSourceTextSerialization()
        {
            var textService = new TextFactoryService();

            var maxSize = SourceTextExtensions.SourceTextLengthThreshold * 3;
            var sb = new StringBuilder(0, maxSize);

            for (var i = 0; i < maxSize; i++)
            {
                var originalText = CreateSourceText(sb, i);

                using var stream = SerializableBytes.CreateWritableStream();
                using var writer = new ObjectWriter(stream);
                originalText.WriteTo(writer, CancellationToken.None);

                stream.Position = 0;

                using var reader = ObjectReader.TryGetReader(stream);
                var recovered = SourceTextExtensions.ReadFrom(textService, reader, originalText.Encoding, CancellationToken.None);

                Assert.Equal(originalText.ToString(), recovered.ToString());
            }
        }

        private SourceText CreateSourceText(StringBuilder sb, int size)
        {
            for (var i = sb.Length; i < size; i++)
            {
                sb.Append((char)('0' + (i % 10)));
            }

            return SourceText.From(sb.ToString());
        }
    }
}
