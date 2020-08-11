// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class TemporaryStorageServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTemporaryStorageText()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);

            // test normal string
            var text = SourceText.From(new string(' ', 4096) + "public class A {}");
            TestTemporaryStorage(service, text);

            // test empty string
            text = SourceText.From(string.Empty);
            TestTemporaryStorage(service, text);

            // test large string
            text = SourceText.From(new string(' ', 1024 * 1024) + "public class A {}");
            TestTemporaryStorage(service, text);
        }

        [WorkItem(531188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531188")]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTemporaryStorageStream()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var temporaryStorage = service.CreateTemporaryStreamStorage(System.Threading.CancellationToken.None);

            using var data = SerializableBytes.CreateWritableStream();
            for (var i = 0; i < SharedPools.ByteBufferSize; i++)
            {
                data.WriteByte((byte)(i % 2));
            }

            data.Position = 0;
            temporaryStorage.WriteStreamAsync(data).Wait();
            using var result = temporaryStorage.ReadStreamAsync().Result;
            Assert.Equal(data.Length, result.Length);

            for (var i = 0; i < SharedPools.ByteBufferSize; i++)
            {
                Assert.Equal(i % 2, result.ReadByte());
            }
        }

        private static void TestTemporaryStorage(ITemporaryStorageService temporaryStorageService, SourceText text)
        {
            // create a temporary storage location
            var temporaryStorage = temporaryStorageService.CreateTemporaryTextStorage(System.Threading.CancellationToken.None);

            // write text into it
            temporaryStorage.WriteTextAsync(text).Wait();

            // read text back from it
            var text2 = temporaryStorage.ReadTextAsync().Result;

            Assert.NotSame(text, text2);
            Assert.Equal(text.ToString(), text2.ToString());
            Assert.Equal(text.Encoding, text2.Encoding);

            temporaryStorage.Dispose();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTemporaryTextStorageExceptions()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var storage = service.CreateTemporaryTextStorage(CancellationToken.None);

            // Nothing has been written yet
            Assert.Throws<InvalidOperationException>(() => storage.ReadText());
            Assert.Throws<AggregateException>(() => storage.ReadTextAsync().Result);

            // write a normal string
            var text = SourceText.From(new string(' ', 4096) + "public class A {}");
            storage.WriteTextAsync(text).Wait();

            // Writing multiple times is not allowed
            Assert.Throws<InvalidOperationException>(() => storage.WriteText(text));
            Assert.Throws<AggregateException>(() => storage.WriteTextAsync(text).Wait());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTemporaryStreamStorageExceptions()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var storage = service.CreateTemporaryStreamStorage(CancellationToken.None);

            // Nothing has been written yet
            Assert.Throws<InvalidOperationException>(() => storage.ReadStream());
            Assert.Throws<AggregateException>(() => storage.ReadStreamAsync().Result);

            // write a normal stream
            var stream = new MemoryStream();
            stream.Write(new byte[] { 42 }, 0, 1);
            stream.Position = 0;
            storage.WriteStreamAsync(stream).Wait();

            // Writing multiple times is not allowed
            Assert.Throws<InvalidOperationException>(() => storage.WriteStream(null));
            Assert.Throws<AggregateException>(() => storage.WriteStreamAsync(null).Wait());
        }

        [Fact]
        public void TestZeroLengthStreams()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var storage = service.CreateTemporaryStreamStorage(CancellationToken.None);

            // 0 length streams are allowed
            using (var stream1 = new MemoryStream())
            {
                storage.WriteStream(stream1);
            }

            using (var stream2 = storage.ReadStream())
            {
                Assert.Equal(0, stream2.Length);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTemporaryStorageMemoryMappedFileManagement()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var buffer = new MemoryStream(257 * 1024 + 1);
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer.WriteByte((byte)i);
            }

            // Do a relatively cheap concurrent stress test of the backing MemoryMappedFile management
            var tasks = Enumerable.Range(1, 257).Select(async i =>
            {
                for (var j = 1; j < 5; j++)
                {
                    using ITemporaryStreamStorage storage1 = service.CreateTemporaryStreamStorage(CancellationToken.None),
                        storage2 = service.CreateTemporaryStreamStorage(CancellationToken.None);
                    var storage3 = service.CreateTemporaryStreamStorage(CancellationToken.None); // let the finalizer run for this instance

                    storage1.WriteStream(new MemoryStream(buffer.GetBuffer(), 0, 1024 * i - 1));
                    storage2.WriteStream(new MemoryStream(buffer.GetBuffer(), 0, 1024 * i));
                    storage3.WriteStream(new MemoryStream(buffer.GetBuffer(), 0, 1024 * i + 1));

                    await Task.Yield();

                    using Stream s1 = storage1.ReadStream(),
                        s2 = storage2.ReadStream(),
                        s3 = storage3.ReadStream();
                    Assert.Equal(1024 * i - 1, s1.Length);
                    Assert.Equal(1024 * i, s2.Length);
                    Assert.Equal(1024 * i + 1, s3.Length);
                }
            });

            Task.WaitAll(tasks.ToArray());
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            GC.Collect(2);
        }

        [Fact(Skip = "This test exists so it can be locally executed for scale testing, when required. Do not remove this test or unskip it in CI.")]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTemporaryStorageScaling()
        {
            // This will churn through 4GB of memory.  It validates that we don't
            // use up our address space in a 32 bit process.
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                using var workspace = new AdhocWorkspace();
                var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
                var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);

                using var data = SerializableBytes.CreateWritableStream();
                for (var i = 0; i < 1024 * 128; i++)
                {
                    data.WriteByte(1);
                }

                // Create 4GB of memory mapped files
                var fileCount = (int)((long)4 * 1024 * 1024 * 1024 / data.Length);
                var storageHandles = new List<ITemporaryStreamStorage>(fileCount);
                for (var i = 0; i < fileCount; i++)
                {
                    var s = service.CreateTemporaryStreamStorage(CancellationToken.None);
                    storageHandles.Add(s);
                    data.Position = 0;
                    s.WriteStreamAsync(data).Wait();
                }

                for (var i = 0; i < 1024 * 5; i++)
                {
                    using var s = storageHandles[i].ReadStreamAsync().Result;
                    Assert.Equal(1, s.ReadByte());
                    storageHandles[i].Dispose();
                }
            }
        }

        [Fact]
        public void StreamTest1()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var storage = service.CreateTemporaryStreamStorage(CancellationToken.None);

            using var expected = new MemoryStream();
            for (var i = 0; i < 10000; i++)
            {
                expected.WriteByte((byte)(i % byte.MaxValue));
            }

            expected.Position = 0;
            storage.WriteStream(expected);

            expected.Position = 0;
            using var stream = storage.ReadStream();
            Assert.Equal(expected.Length, stream.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected.ReadByte(), stream.ReadByte());
            }
        }

        [Fact]
        public void StreamTest2()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var storage = service.CreateTemporaryStreamStorage(CancellationToken.None);

            using var expected = new MemoryStream();
            for (var i = 0; i < 10000; i++)
            {
                expected.WriteByte((byte)(i % byte.MaxValue));
            }

            expected.Position = 0;
            storage.WriteStream(expected);

            expected.Position = 0;
            using var stream = storage.ReadStream();
            Assert.Equal(expected.Length, stream.Length);

            var index = 0;
            int count;
            var bytes = new byte[1000];

            while ((count = stream.Read(bytes, 0, bytes.Length)) > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    Assert.Equal((byte)(index % byte.MaxValue), bytes[i]);
                    index++;
                }
            }

            Assert.Equal(index, stream.Length);
        }

        [Fact]
        public void StreamTest3()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var storage = service.CreateTemporaryStreamStorage(CancellationToken.None);

            using var expected = new MemoryStream();
            var random = new Random(Environment.TickCount);
            for (var i = 0; i < 100; i++)
            {
                var position = random.Next(10000);
                expected.Position = position;

                var value = (byte)(i % byte.MaxValue);
                expected.WriteByte(value);
            }

            expected.Position = 0;
            storage.WriteStream(expected);

            expected.Position = 0;
            using var stream = storage.ReadStream();
            Assert.Equal(expected.Length, stream.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                var value = expected.ReadByte();
                if (value != 0)
                {
                    stream.Position = i;
                    Assert.Equal(value, stream.ReadByte());
                }
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTemporaryStorageTextEncoding()
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);

            // test normal string
            var text = SourceText.From(new string(' ', 4096) + "public class A {}", Encoding.ASCII);
            TestTemporaryStorage(service, text);

            // test empty string
            text = SourceText.From(string.Empty);
            TestTemporaryStorage(service, text);

            // test large string
            text = SourceText.From(new string(' ', 1024 * 1024) + "public class A {}");
            TestTemporaryStorage(service, text);
        }
    }
}
