using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LspVGrepTool.Algorithms;
using LspVGrepTool.Execution;
using LspVGrepTool.Infrastructure;
using LspVGrepTool.Models;
using LspVGrepTool.Reporting;

namespace LspVGrepTool;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: LspVGrepTool <input.json>");
            return 1;
        }

        var inputPath = Path.GetFullPath(args[0]);
        var input = await InputLoader.LoadAsync(inputPath, CancellationToken.None);
        var queries = QueryRequestFactory.Create(input);
        var resolvedDirectory = ResolveTargetDirectory(inputPath, input.Directory!);

        using var context = new QueryExecutionContext(
            resolvedDirectory,
            new RoslynWorkspaceProvider(),
            new ExternalSearchRunner());

        // Eagerly load workspace so timing is separate from individual algorithm runs.
        var workspaceStopwatch = Stopwatch.StartNew();
        await context.GetWorkspaceAsync(CancellationToken.None);
        workspaceStopwatch.Stop();

        var algorithms = new IQueryAlgorithm[]
        {
            new FindTypeDefinitionPwshAlgorithm(),
            new FindTypeDefinitionPwshSimpleAlgorithm(),
            new FindTypeDefinitionRoslynAlgorithm(),
            new FindTypeDefinitionRoslynLspAlgorithm(),
            new FindTypeDefinitionRoslynWorkspaceSymbolAlgorithm(),
            new FindInterfaceImplementationPwshAlgorithm(),
            new FindInterfaceImplementationRoslynAlgorithm(),
            new FindDerivedTypesPwshAlgorithm(),
            new FindDerivedTypesRoslynAlgorithm(),
            new FindMemberDefinitionPwshAlgorithm(),
            new FindMemberDefinitionRoslynAlgorithm()
        };

        var executor = new QueryExecutor(algorithms);
        var report = await executor.ExecuteAsync(queries, context, workspaceStopwatch.Elapsed, CancellationToken.None);

        var outputPath = ResolveOutputPath(inputPath, input.Output);
        var html = HtmlReportRenderer.Render(report);
        await File.WriteAllTextAsync(outputPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), CancellationToken.None);

        Console.WriteLine(outputPath);
        return 0;
    }

    private static string ResolveTargetDirectory(string inputPath, string configuredDirectory)
    {
        var inputDirectory = Path.GetDirectoryName(inputPath)
            ?? throw new InvalidOperationException($"Could not resolve a parent directory for '{inputPath}'.");

        var candidatePath = Path.IsPathRooted(configuredDirectory)
            ? configuredDirectory
            : Path.Combine(inputDirectory, configuredDirectory);

        return Path.GetFullPath(candidatePath);
    }

    private static string ResolveOutputPath(string inputPath, string? configuredOutput)
    {
        var inputDirectory = Path.GetDirectoryName(inputPath)!;
        var fileName = configuredOutput ?? "result.html";
        return Path.IsPathRooted(fileName)
            ? fileName
            : Path.Combine(inputDirectory, fileName);
    }
}
