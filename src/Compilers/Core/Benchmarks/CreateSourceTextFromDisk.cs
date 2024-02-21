// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Text;

[MemoryDiagnoser]
public class CreateSourceTextFromDisk
{
    public static string SmallFilePath = "";
    public static string MediumFilePath = "";
    public static string LargeFilePath = "";

    public FileStream? SmallStream;
    public FileStream? MediumStream;
    public FileStream? LargeStream;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        SmallFilePath = writeCode("small", 1000);
        MediumFilePath = writeCode("medium", 4000);
        LargeFilePath = writeCode("medium", 8000);

        string writeCode(string fileName, int count)
        {
            var code = getCode(count);
            var filePath = Path.Combine(dir, fileName);
            File.WriteAllText(filePath, code, Encoding.UTF8);
            return filePath;
        }

        string getCode(int count)
        {
            var builder = new StringBuilder();
            builder.AppendLine("/*");
            var line = new string('=', 10);
            for (int i = 0; i < count / 10; i++)
            {
                builder.AppendLine(line);
            }
            builder.AppendLine("*/");
            return builder.ToString();
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        Directory.Delete(Path.GetDirectoryName(SmallFilePath)!, recursive: true);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        SmallStream = new FileStream(SmallFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
        MediumStream = new FileStream(MediumFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
        LargeStream = new FileStream(LargeFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        SmallStream?.Dispose();
        MediumStream?.Dispose();
        LargeStream?.Dispose();

        SmallStream = null;
        MediumStream = null;
        LargeStream = null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public SourceText LoadFromFile(FileStream fileStream)
    {
        return EncodedStringText.Create(fileStream, Encoding.UTF8);
    }

    [Benchmark]
    public SourceText SmallFile() => LoadFromFile(SmallStream!);

    [Benchmark]
    public SourceText MediumFile() => LoadFromFile(MediumStream!);

    [Benchmark]
    public SourceText LargeFile() => LoadFromFile(LargeStream!);
}
