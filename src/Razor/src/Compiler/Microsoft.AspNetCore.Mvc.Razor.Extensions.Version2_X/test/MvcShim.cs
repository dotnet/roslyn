// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

internal static class MvcShim
{
    public static readonly string AssemblyName = "Microsoft.AspNetCore.Razor.Test.MvcShim.Version2_X.Compiler";

    private static readonly Lazy<Assembly> s_assembly = new(CreateAssembly, isThreadSafe: true);
    private static readonly Lazy<CSharpCompilation> s_baseCompilation = new(() => TestCompilation.Create(Assembly), isThreadSafe: true);

    public static Assembly Assembly
    {
        get => s_assembly.Value;
    }

    public static CSharpCompilation BaseCompilation
    {
        get => s_baseCompilation.Value;
    }

    private static Assembly CreateAssembly()
    {
        var assemblyFileName = AssemblyName + ".dll";
        var testAssemblyLocation = typeof(MvcShim).Assembly.Location;
        if (string.IsNullOrEmpty(testAssemblyLocation))
        {
            throw new InvalidOperationException(
                $"Cannot locate '{assemblyFileName}' because '{typeof(MvcShim).Assembly.FullName}' has no location. CurrentDirectory='{Directory.GetCurrentDirectory()}', AppContext.BaseDirectory='{AppContext.BaseDirectory}'.");
        }

        var assemblyDirectory = Path.GetDirectoryName(testAssemblyLocation);
        var filePath = Path.Combine(assemblyDirectory!, assemblyFileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Could not locate '{assemblyFileName}'. Probed path '{filePath}'. CurrentDirectory='{Directory.GetCurrentDirectory()}', AppContext.BaseDirectory='{AppContext.BaseDirectory}', TestAssemblyLocation='{typeof(MvcShim).Assembly.Location}'.",
                filePath);
        }

        return Assembly.LoadFrom(filePath);
    }
}
