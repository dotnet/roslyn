// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal class FormattingLoggerFactory : IFormattingLoggerFactory
{
    private const string LogDirEnvVar = "RazorFormattingLogPath";
    private static string? BaseLogDir { get; } = Environment.GetEnvironmentVariable(LogDirEnvVar);

    public IFormattingLogger? CreateLogger(string documentFilePath, string formattingType)
    {
        // If the env var isn't set, we do nothing
        if (BaseLogDir == null)
        {
            return null;
        }

        // Folder format is <BaseLogDir>/<timestamp>_<type>_<filename>/
        var fileName = Path.GetFileName(documentFilePath).Replace(".", "_");
        var folder = $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{formattingType}_{fileName}";
        var logFolder = Path.Combine(BaseLogDir, folder);
        if (Directory.Exists(logFolder))
        {
            // Ensure uniqueness in case of a clash, however unlikely
            logFolder += $"_{Guid.NewGuid():N}";
        }

        try
        {
            Directory.CreateDirectory(logFolder);
        }
        catch
        {
            // If we can't create the directory, we can't log
            return null;
        }

        var logger = new FormattingLogger(logFolder);
        logger.LogMessage($"{formattingType} formatting for {documentFilePath}");
        return logger;
    }
}
