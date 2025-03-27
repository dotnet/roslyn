// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class ShadowCopyAnalyzerPathResolverTests : IDisposable
{
    public TempRoot TempRoot { get; }
    internal ShadowCopyAnalyzerPathResolver PathResolver { get; }

    public ShadowCopyAnalyzerPathResolverTests()
    {
        TempRoot = new TempRoot();
        PathResolver = new ShadowCopyAnalyzerPathResolver(TempRoot.CreateDirectory().Path);
    }

    public void Dispose()
    {
        TempRoot.Dispose();
    }

    [Fact]
    public void IsAnalyzerPathHandled()
    {
        var analyzerPath = TempRoot.CreateDirectory().CreateFile("analyzer.dll").Path;
        Assert.True(PathResolver.IsAnalyzerPathHandled(analyzerPath));
    }

    /// <summary>
    /// Don't create the shadow directory until a copy actually happens
    /// </summary>
    [Fact]
    public void ShadowDirectoryIsDelayCreated()
    {
        Assert.False(Directory.Exists(PathResolver.ShadowDirectory));
    }

    /// <summary>
    /// A shadow copy of a file that doesn't exist should produce a file that doesn't exist, not throw
    /// </summary>
    [Fact]
    public void GetRealPath_FileDoesNotExist()
    {
        var analyzerPath = Path.Combine(TempRoot.CreateDirectory().Path, "analyzer.dll");
        var shadowPath = PathResolver.GetResolvedAnalyzerPath(analyzerPath);
        Assert.False(File.Exists(shadowPath));
    }

    [Fact]
    public void GetRealPath_Copies()
    {
        var analyzerPath = Path.Combine(TempRoot.CreateDirectory().Path, "analyzer.dll");
        File.WriteAllText(analyzerPath, "test");
        var shadowPath = PathResolver.GetResolvedAnalyzerPath(analyzerPath);
        Assert.True(File.Exists(shadowPath));
        Assert.Equal("test", File.ReadAllText(shadowPath));
    }

    /// <summary>
    /// When shadow copying two files in the same directory they should end up in the same shadow 
    /// directory
    /// </summary>
    [Fact]
    public void GetRealPath_FilesInSameDirectory()
    {
        var dir = TempRoot.CreateDirectory().Path;
        var analyzer1Path = Path.Combine(dir, "analyzer1.dll");
        File.WriteAllText(analyzer1Path, "test");
        var analyzer2Path = Path.Combine(dir, "analyzer2.dll");
        File.WriteAllText(analyzer2Path, "test");
        var shadow1Path = PathResolver.GetResolvedAnalyzerPath(analyzer1Path);
        var shadow2Path = PathResolver.GetResolvedAnalyzerPath(analyzer2Path);
        Assert.Equal(Path.GetDirectoryName(shadow1Path), Path.GetDirectoryName(shadow2Path));
    }

    [Fact]
    public void GetRealPath_GroupOnDirectory()
    {
        var dir = TempRoot.CreateDirectory().Path;
        var group1AnalyzerPath = createAnalyzer("group1", "analyzer.dll");
        var group2AnalyzerPath = createAnalyzer("group2", "analyzer.dll");
        var group1ShadowPath = PathResolver.GetResolvedAnalyzerPath(group1AnalyzerPath);
        var group2ShadowPath = PathResolver.GetResolvedAnalyzerPath(group2AnalyzerPath);
        Assert.NotEqual(group1ShadowPath, group2ShadowPath);
        Assert.Equal("group1-analyzer.dll", File.ReadAllText(group1ShadowPath));
        Assert.Equal("group2-analyzer.dll", File.ReadAllText(group2ShadowPath));

        string createAnalyzer(string groupName, string name)
        {
            var groupDir = Path.Combine(dir, groupName, "analyzers");
            _ = Directory.CreateDirectory(groupDir);
            var filePath = Path.Combine(groupDir, name);
            File.WriteAllText(filePath, $"{Path.GetFileName(groupName)}-{name}");
            return filePath;
        }
    }
}
