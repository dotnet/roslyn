// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Internal.VisualStudio.Shell.Embeddable.Feedback;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Xunit;
using Xunit.Harness;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

internal static class VisualStudioLogging
{
    private static bool s_customLoggersAdded = false;

    public const string RazorOutputLogId = "RazorOutputLog";
    public const string LogHubLogId = "RazorLogHub";
    public const string ServiceHubLogId = "ServiceHubLog";
    public const string ComponentModelCacheId = "ComponentModelCache";
    public const string ExtensionDirectoryId = "ExtensionDirectory";
    public const string MEFErrorId = "MEFErrorsFromHive";

    private static readonly object s_lockObj = new();

    public static void AddCustomLoggers()
    {
        lock (s_lockObj)
        {
            // Add custom logs on failure if they haven't already been.
            if (!s_customLoggersAdded)
            {
                DataCollectionService.RegisterCustomLogger(RazorOutputPaneLogger, RazorOutputLogId, "log");
                DataCollectionService.RegisterCustomLogger(RazorLogHubLogger, LogHubLogId, "zip");
                DataCollectionService.RegisterCustomLogger(RazorServiceHubLogger, ServiceHubLogId, "zip");
                DataCollectionService.RegisterCustomLogger(RazorComponentModelCacheLogger, ComponentModelCacheId, "zip");
                DataCollectionService.RegisterCustomLogger(RazorExtensionExplorerLogger, ExtensionDirectoryId, "txt");
                DataCollectionService.RegisterCustomLogger(RazorMEFErrorLogger, MEFErrorId, "txt");

                s_customLoggersAdded = true;
            }
        }
    }

    private static void RazorMEFErrorLogger(string filePath)
    {
        var hiveDirectory = GetHiveDirectory();
        var errorFile = Path.Combine(hiveDirectory, "ComponentModelCache", "Microsoft.VisualStudio.Default.err");
        if (File.Exists(errorFile))
        {
            File.Copy(errorFile, filePath);
        }
    }

    private static void RazorLogHubLogger(string filePath)
    {
        FeedbackLoggerInternal(filePath, "LogHub", "Razor");
    }

    private static void RazorServiceHubLogger(string filePath)
    {
        FeedbackLoggerInternal(filePath, "ServiceHubLogs");
    }

    private static void RazorComponentModelCacheLogger(string filePath)
    {
        FeedbackLoggerInternal(filePath, "ComponentModelCache");
    }

    private static void FeedbackLoggerInternal(string filePath, params string[] expectedFileParts)
    {
        var componentModel = GlobalServiceProvider.ServiceProvider.GetService<SComponentModel, IComponentModel>();
        if (componentModel is null)
        {
            // Unable to get componentModel
            return;
        }

        var feedbackFileProviders = componentModel.GetExtensions<IFeedbackDiagnosticFileProvider>();

        // Collect all the file names first since they can kick of file creation events that might need extra time to resolve.
        var files = new List<string>();
        foreach (var feedbackFileProvider in feedbackFileProviders)
        {
            try
            {
                files.AddRange(feedbackFileProvider.GetFiles());
            }
            catch
            {
                // If one of the providers has issues, we don't want it causing us to not be able to report our stuff properly
            }
        }

        _ = CollectFeedbackItemsAsync(files, filePath, expectedFileParts);
    }

    private static void RazorExtensionExplorerLogger(string filePath)
    {
        var hiveDirectory = GetHiveDirectory();

        using var _ = StringBuilderPool.GetPooledObject(out var fileBuilder);

        var extensionsDir = Path.Combine(hiveDirectory, "Extensions");
        var compatListFile = Path.Combine(extensionsDir, "CompatibilityList.xml");
        if (File.Exists(compatListFile))
        {
            var compatListContent = File.ReadAllText(compatListFile);
            fileBuilder.AppendLine("CompatListContents:");
            fileBuilder.AppendLine(compatListContent);
        }
        else
        {
            fileBuilder.AppendLine("Missing CompatList file");
        }

        var microsoftDir = Path.Combine(extensionsDir, "Microsoft");
        var msExtensionFiles = Directory.EnumerateFiles(microsoftDir, "*", SearchOption.AllDirectories);
        foreach (var msExtensionFile in msExtensionFiles)
        {
            fileBuilder.Append("  ");
            fileBuilder.AppendLine(msExtensionFile);
        }

        File.WriteAllText(filePath, fileBuilder.ToString());
    }

    internal static string GetHiveDirectory()
    {
        // There could be multiple copies of visual studio installed, each with their own RoslynDev hive
        // so to make sure we find the one for the instance of VS we are actually running, we need to find
        // the installation ID for this install. This is stored in an ini file, next to devenv.exe, and the
        // ID itself is pre-pended to the hive name in the file system.
        var devenvPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        var isolationIni = Path.Combine(devenvPath, "devenv.isolation.ini");

        var installationId = "";
        // Lazy ini file parsing starts now!
        foreach (var line in File.ReadAllLines(isolationIni))
        {
            if (line.StartsWith("InstallationID=", StringComparison.OrdinalIgnoreCase))
            {
                installationId = line.Split('=')[1];
                break;
            }
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var vsLocalDir = Path.Combine(localAppData, "Microsoft", "VisualStudio");

        // Just in case the enterprise grade ini file parsing above didn't work, or VS changes how they
        // store things, the following is written to work even if installationId is an empty string. In
        // that case it will fall back to the previous behavior of expecting a single RoslynDev hive to
        // exist, or fail.
        var directories = Directory.GetDirectories(vsLocalDir, $"18*{installationId}RoslynDev", SearchOption.TopDirectoryOnly);
        var hiveDirectories = directories.Where(d => !d.Contains("$")).ToList();
        if (hiveDirectories.Count == 0)
        {
            directories = Directory.GetDirectories(vsLocalDir, $"17*{installationId}RoslynDev", SearchOption.TopDirectoryOnly);
            hiveDirectories = directories.Where(d => !d.Contains("$")).ToList();
        }

        Assert.True(hiveDirectories.Count == 1, $"Could not find the hive path for InstallationID '{installationId}'. Found instead:{Environment.NewLine}{string.Join(Environment.NewLine, hiveDirectories)}");

        return hiveDirectories[0];
    }

    private static void RazorOutputPaneLogger(string filePath)
    {
        // JoinableTaskFactory.Run isn't an option because we might be disposing already.
        // Don't use ThreadHelper.JoinableTaskFactory in test methods, but it's correct here.
#pragma warning disable VSTHRD103 // Call async methods when in an async method
        ThreadHelper.JoinableTaskFactory.Run(async () =>
#pragma warning restore VSTHRD103 // Call async methods when in an async method
        {
            try
            {
                var testServices = await Extensibility.Testing.TestServices.CreateAsync(ThreadHelper.JoinableTaskFactory);
                var paneContent = await testServices.Output.GetRazorOutputPaneContentAsync(CancellationToken.None);
                File.WriteAllText(filePath, paneContent);
            }
            catch (Exception)
            {
                // Eat any errors so we don't block further collection
            }
        });
    }

    private static async Task CollectFeedbackItemsAsync(IEnumerable<string> files, string destination, string[] expectedFileParts)
    {
        // What's important in this weird threading stuff is ensuring we vacate the thread RazorLogHubLogger was called on
        // because if we don't it ends up blocking the thread that creates the zip file we need.
        await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);

                if (!Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (expectedFileParts.All(part => name.IndexOf(part, StringComparison.OrdinalIgnoreCase) == -1))
                {
                    continue;
                }

                await Task.Run(() =>
                {
                    WaitForFileExists(file);
                    WaitForFileFinishedWriting(file);
                    if (File.Exists(file))
                    {
                        File.Copy(file, destination);
                    }
                });
            }
        });
    }

    private static void WaitForFileExists(string file)
    {
        const int MaxRetries = 50;
        var retries = 0;
        while (!File.Exists(file) && retries < MaxRetries)
        {
            retries++;
            // Free your thread
            Thread.Yield();
            // Wait a bit
            Thread.Sleep(100);
        }
    }

    private static void WaitForFileFinishedWriting(string file)
    {
        const int MaxRetries = 50;
        var retries = 0;
        while (IsFileLocked(file) && retries < MaxRetries)
        {
            retries++;
            // Free your thread
            Thread.Yield();
            // Wait a bit
            Thread.Sleep(100);
        }
    }

    private static bool IsFileLocked(string file)
    {
        FileStream? stream = null;

        try
        {
            stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }
        finally
        {
            if (stream != null)
                stream.Close();
        }

        //file is not locked
        return false;
    }
}
