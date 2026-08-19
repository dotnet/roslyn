// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using Microsoft.CodeAnalysis.CSharp;

namespace Benchmarks;

[Config(typeof(Config))]
public class WindowsAssemblyFileOperationBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddFilter(new SimpleFilter(_ => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)));
        }
    }

    private const int FileOperationCount = 16;

    private string _sourcePath = null!;
    private string _destinationDirectory = null!;
    private string[] _destinationPaths = null!;
    private int _destinationIndex;

    [GlobalSetup]
    public void GlobalSetup()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Hard-link creation is benchmarked through the Windows API.");
        }

        _sourcePath = typeof(CSharpCompilation).Assembly.Location;
        var temporaryPath = Path.GetTempPath();
        if (!string.Equals(Path.GetPathRoot(_sourcePath), Path.GetPathRoot(temporaryPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The assembly and temporary directory must be on the same volume.");
        }

        _destinationDirectory = Path.Combine(
            temporaryPath,
            $"{nameof(WindowsAssemblyFileOperationBenchmarks)}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_destinationDirectory);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _destinationPaths = new string[FileOperationCount];
        for (var i = 0; i < _destinationPaths.Length; i++)
        {
            _destinationPaths[i] = Path.Combine(_destinationDirectory, $"destination-{_destinationIndex++}.dll");
        }
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        foreach (var path in _destinationPaths)
        {
            try
            {
                File.Delete(path);
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        try
        {
            Directory.Delete(_destinationDirectory, recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [Benchmark(OperationsPerInvoke = FileOperationCount)]
    public void CopyAssembly()
    {
        foreach (var path in _destinationPaths)
        {
            File.Copy(_sourcePath, path);
        }
    }

    [Benchmark(OperationsPerInvoke = FileOperationCount)]
    public void HardLinkAssembly()
    {
        foreach (var path in _destinationPaths)
        {
            CreateHardLink(path);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = FileOperationCount)]
    public int CopyAndLoadAssembly()
    {
        var result = 0;
        foreach (var path in _destinationPaths)
        {
            File.Copy(_sourcePath, path);
            result += LoadAssembly(path);
        }

        return result;
    }

    [Benchmark(OperationsPerInvoke = FileOperationCount)]
    public int HardLinkAndLoadAssembly()
    {
        var result = 0;
        foreach (var path in _destinationPaths)
        {
            CreateHardLink(path);
            result += LoadAssembly(path);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoadAssembly(string path)
    {
        var loadContext = new AssemblyLoadContext(path, isCollectible: true);
        try
        {
            Assembly assembly = loadContext.LoadFromAssemblyPath(path);
            return assembly.ManifestModule.ModuleVersionId.GetHashCode();
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private void CreateHardLink(string path)
    {
        if (!CreateHardLink(path, _sourcePath, IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
}