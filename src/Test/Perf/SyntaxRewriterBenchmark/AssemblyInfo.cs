// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Roslyn.SyntaxRewriterBenchmark;

public record struct AssemblyInfo(Version? Version, string? CorePath, string? CSharpPath = null);

[AttributeUsage(AttributeTargets.Assembly)]
public abstract class AssemblyInfoAttribute : Attribute
{
    public Version? Version { get; set; }
    public string? CorePath { get; set; }
    public string? CSharpPath { get; set; }

    public AssemblyInfo Value => new(Version, CorePath, CSharpPath);

    protected AssemblyInfoAttribute(string? version, string? corePath, string? csharpPath = null)
    {
        if (!string.IsNullOrEmpty(version))
        {
            Version = new(version);
        }

        if (!string.IsNullOrEmpty(corePath))
        {
            CorePath = corePath;
        }

        if (!string.IsNullOrEmpty(csharpPath))
        {
            CSharpPath = csharpPath;
        }
    }
}

public class BaselineAssemblyInfoAttribute : AssemblyInfoAttribute
{
    public BaselineAssemblyInfoAttribute(string? version, string? corePath, string? csharpPath = null) : base(version, corePath, csharpPath)
    {
    }
}

public class LatestAssemblyInfoAttribute : AssemblyInfoAttribute
{
    public LatestAssemblyInfoAttribute(string? version, string? corePath, string? csharpPath = null) : base(version, corePath, csharpPath)
    {
    }
}
