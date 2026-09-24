// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Basic.Reference.Assemblies;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Benchmarks;

/// <summary>
/// Benchmarks the decision DAG builder for switch expressions with many 'or' patterns.
/// Includes both synthetic cases and a snapshot of ErrorFacts.IsBuildOnlyDiagnostic from the compiler build.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public partial class DecisionDagBenchmarks
{
    private const int TrueArmCount = 56;
    private const int FalseArmCount = 1935;
    private const int TotalValues = TrueArmCount + FalseArmCount;

    private string? _switchExpressionSource;
    private string? _switchStatementSource;

    [GlobalSetup]
    public void Setup()
    {
        _switchExpressionSource = GenerateSwitchExpressionSource();
        _switchStatementSource = GenerateSwitchStatementSource();
    }

    public static object EmitCore(string sourceCode)
    {
        var sourceText = SourceText.From(sourceCode);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
        var compilation = CSharpCompilation.Create(
            "BenchmarkAssembly",
            [syntaxTree],
            Net90.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var assemblyStream = new MemoryStream();
        var emitResult = compilation.Emit(
            assemblyStream,
            pdbStream: null,
            xmlDocumentationStream: null,
            win32Resources: null,
            manifestResources: [],
            options: new EmitOptions().WithDebugInformationFormat(DebugInformationFormat.Embedded),
            cancellationToken: default);
        if (!emitResult.Success)
        {
            throw new Exception("Compilation failed: " + string.Join(Environment.NewLine, emitResult.Diagnostics));
        }

        assemblyStream.Position = 0;
        return assemblyStream;
    }

    [Benchmark]
    public object EmitWithSwitchExpression() => EmitCore(_switchExpressionSource!);

    [Benchmark]
    public object EmitWithSwitchStatement() => EmitCore(_switchStatementSource!);

    [Benchmark]
    public object EmitIsBuildOnlyDiagnostic() => EmitCore(IsBuildOnlyDiagnosticSource);

    [Benchmark]
    public object EmitWithoutSwitch() => EmitCore("""
            public static class ErrorFacts
            {
                public static bool IsBuildOnlyDiagnostic(int code)
                {
                    return code < 56;
                }
            }
            """);

    /// <summary>
    /// Generates a C# source file with an enum and a switch expression that mirrors
    /// the structure of ErrorFacts.IsBuildOnlyDiagnostic: two arms with many 'or' patterns.
    /// </summary>
    private static string GenerateSwitchExpressionSource()
    {
        var sb = new StringBuilder();
        sb.AppendLine("#pragma warning disable CS8524");
        sb.AppendLine();

        AppendEnum(sb);

        // Generate the method with the switch expression
        sb.AppendLine("public static class ErrorFacts");
        sb.AppendLine("{");
        sb.AppendLine("    public static bool IsBuildOnlyDiagnostic(ErrorCode code)");
        sb.AppendLine("    {");
        sb.AppendLine("        return code switch");
        sb.AppendLine("        {");

        // First arm: TrueArmCount values => true
        sb.Append("            ErrorCode.Value_0");
        for (var i = 1; i < TrueArmCount; i++)
        {
            sb.AppendLine();
            sb.Append("            or ErrorCode.Value_");
            sb.Append(i);
        }
        sb.AppendLine();
        sb.AppendLine("                => true,");

        // Second arm: FalseArmCount values => false
        sb.Append("            ErrorCode.Value_");
        sb.Append(TrueArmCount);
        for (var i = TrueArmCount + 1; i < TotalValues; i++)
        {
            sb.AppendLine();
            sb.Append("            or ErrorCode.Value_");
            sb.Append(i);
        }
        sb.AppendLine();
        sb.AppendLine("                => false,");

        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a C# source file with an enum and a switch statement that mirrors
    /// the structure of ErrorFacts.IsBuildOnlyDiagnostic using case labels instead of 'or' patterns.
    /// </summary>
    private static string GenerateSwitchStatementSource()
    {
        var sb = new StringBuilder();
        sb.AppendLine();

        AppendEnum(sb);

        // Generate the method with the switch statement
        sb.AppendLine("public static class ErrorFacts");
        sb.AppendLine("{");
        sb.AppendLine("    public static bool IsBuildOnlyDiagnostic(ErrorCode code)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (code)");
        sb.AppendLine("        {");

        // First group: TrueArmCount values => return true
        for (var i = 0; i < TrueArmCount; i++)
        {
            sb.Append("            case ErrorCode.Value_");
            sb.Append(i);
            sb.AppendLine(":");
        }
        sb.AppendLine("                return true;");

        // Second group: FalseArmCount values => return false
        for (var i = TrueArmCount; i < TotalValues; i++)
        {
            sb.Append("            case ErrorCode.Value_");
            sb.Append(i);
            sb.AppendLine(":");
        }
        sb.AppendLine("                return false;");

        sb.AppendLine("            default:");
        sb.AppendLine("                throw new System.ArgumentOutOfRangeException(nameof(code));");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AppendEnum(StringBuilder sb)
    {
        sb.AppendLine("public enum ErrorCode");
        sb.AppendLine("{");
        for (var i = 0; i < TotalValues; i++)
        {
            sb.Append("    Value_");
            sb.Append(i);
            sb.Append(" = ");
            sb.Append(i);
            sb.AppendLine(",");
        }
        sb.AppendLine("}");
        sb.AppendLine();
    }
}
