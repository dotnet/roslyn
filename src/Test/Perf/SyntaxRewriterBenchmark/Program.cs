// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Baseline;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using static Roslyn.SyntaxRewriterBenchmark.Program.BuiltInFileSets;
using System.Runtime.Loader;

namespace Roslyn.SyntaxRewriterBenchmark;

public class Program
{
    public static readonly IReadOnlyList<string> DefaultFileSet = new[] { RoslynSrc, DefaultTestFile };
    public static readonly Tests DefaultTests = Tests.All ^ Tests.Validation;
    private const string DefaultSearchPattern = "*.cs";
    private const SearchOption DefaultSearchOption = SearchOption.AllDirectories;

    private const string Help = @"
        Roslyn.SyntaxRewriterBenchmark [FileSet [TestList]]
        FileSet: {directory} | {file} | 'roslyn' | 'testFileA' | {semicolon-separated combination of these}
        TestList: nextToken | prevToken | indexOfNode | {comma-separated combination of these}
    @";

    public static int Main(string[] args)
    {
        RegisterAssemblies();
        return MainCore(args);
    }

    private static int MainCore(string[] args)
    {
        if (!ParseArgs(args, out var paths, out var tests))
        {
            return Result.Error;
        }

        SetupEnvironment();

        CheckSideBySideAssemblies();

        foreach (string path in paths)
        {
            Run(tests, path);
        }

        return Result.Success;
    }

    public static void Run(Tests tests, string path)
    {
        WriteStartMessage(path);

        var totalTestTimer = Stopwatch.StartNew();
        AnalyzeFileOrDirectory(path, tests);
        totalTestTimer.Stop();

        // Write the results to the console
        var horizontalDivider = new string('-', 6 * (2 + 16 + 1) + 1);
        var padding = new string(' ', 16);
        var totalElapsedSeconds = totalTestTimer.Elapsed.TotalSeconds - s_validationTimer.Elapsed.TotalSeconds;
        int maxElapsedSecondsDigitCount = Math.Max(1, (int)Math.Log10(totalElapsedSeconds) + 1);
        int elapsedSecondsDecimalCount = 15 - maxElapsedSecondsDigitCount;
        var elapsedSecondsDecimalFormat = new string('0', elapsedSecondsDecimalCount);
        var percentagePadding = padding[0..2];
        var ratioPadding = padding[0..3];
        var rowFormat = $"| {{0,-16}} | {{1,16:0.{elapsedSecondsDecimalFormat}}} | {{2,16:0.{elapsedSecondsDecimalFormat}}} | {percentagePadding} {{3,10:##0.0000 %}} {percentagePadding} | {percentagePadding} {{4,10:##0.0000 %}} {percentagePadding} | {ratioPadding} {{5,7:0.00}}x {ratioPadding} |";
        const string? TestTableHeader = "|       Test       |  Baseline (sec)  |   Latest (sec)   | Baseline Time %  |  Latest Time %   |    Perf Ratio    |";
        const string NextTokenTestTitle = "  GetNextToken  ";
        const string PrevTokenTestTitle = "GetPreviousToken";
        const string NodeIndexTestTitle = "  IndexOfNode   ";
        const string RewriterTotalTitle = "[Rewriter Total]";
        string TestTableFooterFormat = $"|  Total Test Time : {{0,16:0.{elapsedSecondsDecimalFormat}}} seconds {' ',(6 - 2) * (2 + 16 + 1) - 9} |";
        Console.WriteLine();
        Console.WriteLine(horizontalDivider);
        Console.WriteLine(TestTableHeader);
        Console.WriteLine(horizontalDivider);

        if (tests.HasFlag(Tests.IndexOfNodeInParent))
        {
            var nodeIndexElapsedSeconds = (Baseline: s_indexOfNodeTimers.Baseline.Elapsed.TotalSeconds, Latest: s_indexOfNodeTimers.Latest.Elapsed.TotalSeconds);
            double baselineSeconds = nodeIndexElapsedSeconds.Baseline;
            double latestSeconds = nodeIndexElapsedSeconds.Latest;
            Console.WriteLine(rowFormat, NodeIndexTestTitle, baselineSeconds, latestSeconds, baselineSeconds / totalElapsedSeconds, latestSeconds / totalElapsedSeconds, baselineSeconds / latestSeconds);
        }

        if (tests.HasFlag(Tests.GetPreviousToken))
        {
            var prevTokenElapsedSeconds = (Baseline: s_prevTokenTimers.Baseline.Elapsed.TotalSeconds, Latest: s_prevTokenTimers.Latest.Elapsed.TotalSeconds);
            double baselineSeconds = prevTokenElapsedSeconds.Baseline;
            double latestSeconds = prevTokenElapsedSeconds.Latest;
            Console.WriteLine(rowFormat, PrevTokenTestTitle, baselineSeconds, latestSeconds, baselineSeconds / totalElapsedSeconds, latestSeconds / totalElapsedSeconds, baselineSeconds / latestSeconds);
        }

        if (tests.HasFlag(Tests.GetNextToken))
        {
            var nextTokenElapsedSeconds = (Baseline: s_nextTokenTimers.Baseline.Elapsed.TotalSeconds, Latest: s_nextTokenTimers.Latest.Elapsed.TotalSeconds);
            double baselineSeconds = nextTokenElapsedSeconds.Baseline;
            double latestSeconds = nextTokenElapsedSeconds.Latest;
            Console.WriteLine(rowFormat, NextTokenTestTitle, baselineSeconds, latestSeconds, baselineSeconds / totalElapsedSeconds, latestSeconds / totalElapsedSeconds, baselineSeconds / latestSeconds);
        }

        {
            var overallTraversalSeconds = (Baseline: s_overallTraversalTimers.Baseline.Elapsed.TotalSeconds, Latest: s_overallTraversalTimers.Latest.Elapsed.TotalSeconds);
            if (overallTraversalSeconds is not (0, 0))
            {
                double baselineSeconds = overallTraversalSeconds.Baseline;
                double latestSeconds = overallTraversalSeconds.Latest;
                Console.WriteLine(rowFormat, RewriterTotalTitle, baselineSeconds, latestSeconds, baselineSeconds / totalElapsedSeconds, latestSeconds / totalElapsedSeconds, baselineSeconds / latestSeconds);
            }
        }

        Console.WriteLine(horizontalDivider);
        Console.WriteLine(TestTableFooterFormat, totalElapsedSeconds);
        Console.WriteLine(horizontalDivider);
        Console.WriteLine();

        ResetTimers();
    }

    public static class BuiltInFileSets
    {
        public const string Src = "src";
        public const string Roslyn = "rosyln";
        public const string DefaultTestFile = "testfile";
        public const string TestFileA = "testfilea";
        public const string RoslynSrc = Roslyn;
    }

    [Flags]
    public enum Tests
    {
        GetNextToken = 1,
        GetPreviousToken = 2,
        IndexOfNodeInParent = 4,
        Validation = 128, // when not parsing with both CSharp assemblies, the validation is built in
        All = -1
    }

    public static Dictionary<string, Tests> TestsByName { get; } =
        new(Enum.GetValues<Tests>().Select(t => KeyValuePair.Create(Enum.GetName(t)!, t)), StringComparer.OrdinalIgnoreCase)
        {
            ["nextToken"] = Tests.GetNextToken,
            ["prevToken"] = Tests.GetPreviousToken,
            ["indexOfNode"] = Tests.IndexOfNodeInParent
        };

    private static class Result
    {
        public const int Success = 0;
        public const int Error = -1;
    }

    private static bool ParseArgs(string[] args, out IReadOnlyList<string> paths, out Tests tests)
    {
        bool onlyPaths = false;

        string[] pathsArray;
        if (args.Length > 0)
        {
            pathsArray = args[0].Split(';', StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            // Use the current directory if it's different from the app's base directory and if no file set was specified.
            var currentPath = Environment.CurrentDirectory;
            var currentPathWithoutTrailingSlash = Path.TrimEndingDirectorySeparator(currentPath.AsSpan());
            var appBasePathWithoutTrailingSlash = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory.AsSpan());
            if (!currentPathWithoutTrailingSlash.SequenceEqual(appBasePathWithoutTrailingSlash))
            {
                pathsArray = new[] { currentPath };
                onlyPaths = true;
            }
            else
            {
                pathsArray = DefaultFileSet.ToArray();
            }
        }

        paths = pathsArray;
        if (!onlyPaths)
        {
            // Convert the names of built-in file sets into paths. 
            foreach (ref var path in pathsArray.AsSpan())
            {
                var srcPath = FindRoslynSrc();
                if (path is TestFileA or DefaultTestFile)
                {
                    path = $"{srcPath}/Test/Perf/AnalysisTestFileA.cs";
                }
                else if (path is RoslynSrc or Src)
                {
                    path = srcPath;
                }
            }
        }

        tests = DefaultTests;
        if (args.Length > 1)
        {
            if (args.Length > 2)
            {
                Console.Error.WriteLine("Error: SyntaxRewriterBenchmark doesn't currently support more than two arguments.");
                Console.Error.WriteLine(Help);
                return false;
            }

            tests = 0;
            var testList = args[1].Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string testName in testList)
            {
                if (TestsByName.TryGetValue(testName, out var testValue))
                {
                    tests |= testValue;
                }
                else
                {
                    Console.Error.WriteLine($"Error: SyntaxRewriterBenchmark couldn't find '{testName}' in its list of tests.");
                    Console.Error.WriteLine(Help);
                    return false;
                }
            }
        }

        return true;
    }

    private static string FindRoslynSrc()
    {
        var parentDir = new DirectoryInfo(AppContext.BaseDirectory);
        string srcPath;
        while (true)
        {
            srcPath = Path.Join(parentDir.FullName, "src");
            if (Directory.Exists(srcPath))
            {
                break;
            }

            parentDir = parentDir.Parent;
            if (parentDir == null)
            {
                throw new DirectoryNotFoundException("Couldn't find the root of roslyn's source code.");
            }
        }
        return srcPath;
    }

    private static void SetupEnvironment()
    {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        if (Debugger.IsAttached)
        {
            Console.Error.WriteLine("Warning: A debugger is attached to the current process.");
        }
    }

    private static void RegisterAssemblies()
    {
        const string RoslynNamespace = "Microsoft.CodeAnalysis";
        var baselineAssemblyInfo = Assembly.GetExecutingAssembly().GetCustomAttribute<BaselineAssemblyInfoAttribute>()?.Value ?? default;
        var latestAssemblyInfo = Assembly.GetExecutingAssembly().GetCustomAttribute<LatestAssemblyInfoAttribute>()?.Value ?? default;
        var baselineAssemblyVersion = baselineAssemblyInfo.Version ?? throw new InvalidOperationException($"Baseline assembly version for {RoslynNamespace} could not be extracted!");
        var latestAssemblyVersion = latestAssemblyInfo.Version ?? Assembly.GetExecutingAssembly().GetName().Version ?? new(42, 42, 42, 42);
        Assembly? ResolveAssembliesNotFound(object? sender, ResolveEventArgs args)
        {
            var assemblyName = args.Name.TrimStart();
            if (assemblyName.StartsWith(RoslynNamespace, StringComparison.OrdinalIgnoreCase) && assemblyName.Length > RoslynNamespace.Length)
            {
                if (assemblyName[RoslynNamespace.Length] == ',')
                {
                    // Load the requested version of roslyn core
                    var assemblyVersion = new AssemblyName(assemblyName).Version;
                    if (assemblyVersion == baselineAssemblyVersion)
                    {
                        var baselineCoreAssemblyPath = baselineAssemblyInfo.CorePath ?? throw new EmptyPathException("Baseline", RoslynNamespace);
                        return BaselineAssemblyContext.LoadFile(Path.Combine(AppContext.BaseDirectory, baselineCoreAssemblyPath));
                    }
                    if (assemblyVersion == null || assemblyVersion == latestAssemblyVersion)
                    {
                        var latestCoreAssemblyPath = latestAssemblyInfo.CorePath ?? throw new EmptyPathException("Latest", RoslynNamespace);
                        return LatestAssemblyContext.LoadFile(Path.Combine(AppContext.BaseDirectory, latestCoreAssemblyPath));
                    }
                }
                else if (assemblyName.AsSpan(RoslynNamespace.Length).StartsWith(".CSharp,", StringComparison.OrdinalIgnoreCase))
                {
                    // Load the requested version of roslyn C#
                    var assemblyVersion = new AssemblyName(assemblyName).Version;
                    if (assemblyVersion == baselineAssemblyVersion)
                    {
                        var baselineCSharpAssemblyPath = baselineAssemblyInfo.CSharpPath ?? throw new EmptyPathException("Baseline", RoslynNamespace + ".CSharp");
                        return BaselineAssemblyContext.LoadFile(Path.Combine(AppContext.BaseDirectory, baselineCSharpAssemblyPath));
                    }
                    if (assemblyVersion == null || assemblyVersion == latestAssemblyVersion)
                    {
                        var latestCSharpAssemblyPath = latestAssemblyInfo.CSharpPath ?? throw new EmptyPathException("Latest", RoslynNamespace + ".CSharp");
                        return LatestAssemblyContext.LoadFile(Path.Combine(AppContext.BaseDirectory, latestCSharpAssemblyPath));
                    }
                }
            }
            return null;
        }
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembliesNotFound;
    }

    private class BaselineAssemblyContext : AssemblyLoadContext
    {
        public static Assembly LoadFile(string assemblyPath) => Default.LoadFromAssemblyPath(assemblyPath);
        public static new BaselineAssemblyContext Default { get; } = new();
        private BaselineAssemblyContext() : base($"{nameof(SyntaxRewriterBenchmark)}.Baseline")
        {
        }
    }

    private class LatestAssemblyContext : AssemblyLoadContext
    {
        public static Assembly LoadFile(string assemblyPath) => Default.LoadFromAssemblyPath(assemblyPath);
        public static new LatestAssemblyContext Default { get; } = new();
        private LatestAssemblyContext() : base($"{nameof(SyntaxRewriterBenchmark)}.Latest")
        {
        }
    }

    private class EmptyPathException : InvalidOperationException
    {
        private const string MessageFormat = "{0} assembly path for {1} could not be extracted from project metadata!";
        internal EmptyPathException(string assemblyVersionType, string assemblyName) : base(string.Format(MessageFormat, assemblyVersionType, assemblyName))
        {
        }
    }

    private static bool CheckSideBySideAssemblies()
    {
        bool CheckSxS()
        {
            bool assembliesAreSideBySide = true;

            if (typeof(SyntaxNode) == typeof(Baseline::Microsoft.CodeAnalysis.SyntaxNode))
            {
                assembliesAreSideBySide = false;
                Console.Error.WriteLine("Warning: Baseline assembly is the same as the latest assembly for `Microsoft.CodeAnalysis`!");
            }

#if ParseWithBothCSharpAssemblies

            if (typeof(CSharpSyntaxTree) == typeof(Baseline::Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree))
            {
                assembliesAreSideBySide = false;
                Console.Error.WriteLine("Warning: Baseline assembly is the same as the latest assembly for `Microsoft.CodeAnalysis.CSharp`!");
            }

#endif

            return assembliesAreSideBySide;
        }

        // Wrap the check inside a task for further assembly-loading order assurance.
        return Task.Factory.StartNew(CheckSxS, default(CancellationToken), TaskCreationOptions.None, TaskScheduler.Default).GetAwaiter().GetResult();
    }

    private static void WriteStartMessage(string path)
    {
        var shortPath = Path.TrimEndingDirectorySeparator(path.AsSpan());
        int lastSlashIndex = shortPath.LastIndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (lastSlashIndex >= 0)
        {
            lastSlashIndex = shortPath.Slice(0, lastSlashIndex).LastIndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        shortPath = shortPath.Slice(lastSlashIndex + 1);
        Console.WriteLine();
        Console.WriteLine($"Analyzing '{shortPath}'...");
    }

    public static void AnalyzeFileOrDirectory(string path, Tests tests, string directorySearchPattern = DefaultSearchPattern, SearchOption directorySearchOption = DefaultSearchOption)
    {
        path = Path.GetFullPath(path);
        var file = new FileInfo(path);
        if (file.Exists)
        {
            Analyze(file, tests);
        }
        else
        {
            Analyze(new DirectoryInfo(path), tests, directorySearchPattern, directorySearchOption);
        }
    }

    public static void Analyze(DirectoryInfo root, Tests tests, string searchPattern = DefaultSearchPattern, SearchOption searchOption = DefaultSearchOption)
    {
        foreach (FileInfo file in root.EnumerateFiles(searchPattern, searchOption))
        {
            Analyze(file, tests);
        }
    }

    public static void Analyze(FileInfo file, Tests tests)
    {
        using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Analyze(stream, tests);
    }

    public static void Analyze(Stream stream, Tests tests)
    {
#if ParseWithBothCSharpAssemblies
        var baselineSrc = Baseline::Microsoft.CodeAnalysis.Text.SourceText.From(stream);
        var baselineRoot = Baseline::Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(baselineSrc).GetRoot();
#endif
        var src = SourceText.From(stream);
        var root = CSharpSyntaxTree.ParseText(src).GetRoot();
        var rewriter = new HybridRewriter()
        {
            Tests = tests,
            NextTokenTimers = s_nextTokenTimers,
            PreviousTokenTimers = s_prevTokenTimers,
            IndexOfNodeTimers = s_indexOfNodeTimers,
            OverallTraversalTimers = s_overallTraversalTimers,
            ValidationTimer = s_validationTimer
        };
#if ParseWithBothCSharpAssemblies
        rewriter.Visit(baselineRoot, root);
#else
        rewriter.Visit(root);
#endif
    }

    private static (Stopwatch Baseline, Stopwatch Latest) s_nextTokenTimers = (new(), new());
    private static (Stopwatch Baseline, Stopwatch Latest) s_prevTokenTimers = (new(), new());
    private static (Stopwatch Baseline, Stopwatch Latest) s_indexOfNodeTimers = (new(), new());
    private static (Stopwatch Baseline, Stopwatch Latest) s_overallTraversalTimers = (new(), new());

    /// <inheritdoc cref="HybridRewriter.ValidationTime"/>
    private static readonly Stopwatch s_validationTimer = new();

    private static void ResetTimers()
    {
        s_nextTokenTimers.Baseline.Reset();
        s_nextTokenTimers.Latest.Reset();
        s_prevTokenTimers.Baseline.Reset();
        s_prevTokenTimers.Latest.Reset();
        s_indexOfNodeTimers.Baseline.Reset();
        s_indexOfNodeTimers.Latest.Reset();
        s_overallTraversalTimers.Baseline.Reset();
        s_overallTraversalTimers.Latest.Reset();
        s_validationTimer.Reset();
    }
}
