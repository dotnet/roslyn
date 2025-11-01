// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Roslyn.Utils;

public sealed class Logger
{
    public Logger(IAnsiConsole console, string fileSuffix)
    {
        LogFilePath = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "roslyn", $"log-{fileSuffix}.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
        // This is disposed inside ProcessExit event, not here in Main, so it can be also used in UnhandledException handler.
        Writer = new StreamWriter(File.Open(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true,
        };
        Writer.WriteLine();
        Writer.WriteLine();

        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            Writer.Dispose();
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            this.Log($"Unhandled exception: {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            this.Log($"Unobserved task exception: {e.Exception}");
        };

        console.MarkupLineInterpolated($"Logging to [grey]{this.LogFilePath}[/]");

        console.Pipeline.Attach(new LoggingRenderHook(this));
    }

    public StreamWriter Writer { get; }
    public string LogFilePath { get; }

    public void Log(string message)
    {
        Writer.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] {message}");
    }
}

file sealed class LoggingRenderHook(Logger logger) : IRenderHook
{
    private long _lastOffset;

    public IEnumerable<IRenderable> Process(RenderOptions options, IEnumerable<IRenderable> renderables)
    {
        // Timestamp will be added on each intercepted newline,
        // but not if someone from the outside wrote to the log in the meantime.
        if (_lastOffset != logger.Writer.BaseStream.Position)
        {
            logger.Writer.Write($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] ");
        }

        foreach (var renderable in renderables)
        {
            var segments = renderable.Render(options, int.MaxValue).ToArray();
            var text = string.Concat(segments.Select(static s => s.Text));
            text = text.ReplaceLineEndings($"\n[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] ");
            logger.Writer.Write(text);
        }

        _lastOffset = logger.Writer.BaseStream.Position;

        return renderables;
    }
}
