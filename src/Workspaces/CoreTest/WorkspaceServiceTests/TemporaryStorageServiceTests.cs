// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

using static TemporaryStorageService;

[UseExportProvider]
#if NET
[SupportedOSPlatform("windows")]
#endif
[Trait(Traits.Feature, Traits.Features.Workspace)]
public sealed class TemporaryStorageServiceTests
{
    [ConditionalFact(typeof(WindowsOnly))]
    public void TestTemporaryStorageText()
    {
        using var workspace = new AdhocWorkspace();
        var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
        var service = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

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

    [ConditionalFact(typeof(WindowsOnly)), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531188")]
    public void TestTemporaryStorageStream()
    {
        using var workspace = new AdhocWorkspace();
        var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
        var service = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

        using var data = SerializableBytes.CreateWritableStream();
        for (var i = 0; i < SharedPools.ByteBufferSize; i++)
        {
            data.WriteByte((byte)(i % 2));
        }

        var handle = service.WriteToTemporaryStorage(data);

        using var result = handle.ReadFromTemporaryStorage();
        Assert.Equal(data.Length, result.Length);

        for (var i = 0; i < SharedPools.ByteBufferSize; i++)
        {
            Assert.Equal(i % 2, result.ReadByte());
        }
    }

    private static void TestTemporaryStorage(ITemporaryStorageServiceInternal temporaryStorageService, SourceText text)
    {
        // write text into it
        var handle = temporaryStorageService.WriteToTemporaryStorage(text, CancellationToken.None);

        // read text back from it
        var text2 = handle.ReadFromTemporaryStorage(CancellationToken.None);

        Assert.NotSame(text, text2);
        Assert.Equal(text.ToString(), text2.ToString());
        Assert.Equal(text.Encoding, text2.Encoding);
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void TestZeroLengthStreams()
    {
        using var workspace = new AdhocWorkspace();
        var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
        var service = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

        // 0 length streams are allowed
        TemporaryStorageStreamHandle handle;
        using (var stream1 = new MemoryStream())
        {
            handle = service.WriteToTemporaryStorage(stream1);
        }

        using var stream2 = handle.ReadFromTemporaryStorage();
        Assert.Equal(0, stream2.Length);
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void TestTemporaryStorageMemoryMappedFileManagement()
    {
        using var workspace = new AdhocWorkspace();
        var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
        var service = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());
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
                var handle1 = service.WriteToTemporaryStorage(new MemoryStream(buffer.GetBuffer(), 0, 1024 * i - 1));
                var handle2 = service.WriteToTemporaryStorage(new MemoryStream(buffer.GetBuffer(), 0, 1024 * i));
                var handle3 = service.WriteToTemporaryStorage(new MemoryStream(buffer.GetBuffer(), 0, 1024 * i + 1));

                await Task.Yield();

                using var s1 = handle1.ReadFromTemporaryStorage();
                using var s2 = handle2.ReadFromTemporaryStorage();
                using var s3 = handle3.ReadFromTemporaryStorage();
                Assert.Equal(1024 * i - 1, s1.Length);
                Assert.Equal(1024 * i, s2.Length);
                Assert.Equal(1024 * i + 1, s3.Length);
            }
        });

        Task.WaitAll([.. tasks]);
        GC.Collect(2);
        GC.WaitForPendingFinalizers();
        GC.Collect(2);
    }

    [Fact(Skip = "This test exists so it can be locally executed for scale testing, when required. Do not remove this test or unskip it in CI.")]
    public void TestTemporaryStorageScaling()
    {
        // This will churn through 4GB of memory.  It validates that we don't
        // use up our address space in a 32 bit process.
        if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
        {
            using var workspace = new AdhocWorkspace();
            var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
            var service = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

            using var data = SerializableBytes.CreateWritableStream();
            for (var i = 0; i < 1024 * 128; i++)
            {
                data.WriteByte(1);
            }

            // Create 4GB of memory mapped files
            var fileCount = (int)((long)4 * 1024 * 1024 * 1024 / data.Length);
            var storageHandles = new List<TemporaryStorageStreamHandle>(fileCount);
            for (var i = 0; i < fileCount; i++)
            {
                var handle = service.WriteToTemporaryStorage(data);
                storageHandles.Add(handle);
            }

            for (var i = 0; i < 1024 * 5; i++)
            {
                using var s = storageHandles[i].ReadFromTemporaryStorage();
                Assert.Equal(1, s.ReadByte());
            }
        }
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void StreamTest1()
    {
        using var workspace = new AdhocWorkspace();
        var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
        var service = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

        using var expected = new MemoryStream();
        for (var i = 0; i < 10000; i++)
        {
            expected.WriteByte((byte)(i % byte.MaxValue));
        }

        var handle = service.WriteToTemporaryStorage(expected);

        expected.Position = 0;
        using var stream = handle.ReadFromTemporaryStorage();
        Assert.Equal(expected.Length, stream.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected.ReadByte(), stream.ReadByte());
        }
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void StreamTest2()
    {
        using var workspace = new AdhocWorkspace();
        var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
        var service = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

        using var expected = new MemoryStream();
        for (var i = 0; i < 10000; i++)
        {
            expected.WriteByte((byte)(i % byte.MaxValue));
        }

        var handle = service.WriteToTemporaryStorage(expected);

        expected.Position = 0;
        using var stream = handle.ReadFromTemporaryStorage();
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

    [ConditionalFact(typeof(WindowsOnly))]
    public void StreamTest3()
    {
        using var workspace = new AdhocWorkspace();
        var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
        var service = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

        using var expected = new MemoryStream();
        var random = new Random(Environment.TickCount);
        for (var i = 0; i < 100; i++)
        {
            var position = random.Next(10000);
            expected.Position = position;

            var value = (byte)(i % byte.MaxValue);
            expected.WriteByte(value);
        }

        var handle = service.WriteToTemporaryStorage(expected);

        expected.Position = 0;
        using var stream = handle.ReadFromTemporaryStorage();
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

    [ConditionalFact(typeof(WindowsOnly))]
    public void TestTemporaryStorageTextEncoding()
    {
        using var workspace = new AdhocWorkspace();
        var textFactory = Assert.IsType<TextFactoryService>(workspace.Services.GetService<ITextFactoryService>());
        var service = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

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
