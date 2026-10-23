// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace BuildBoss
{
    internal sealed class BomCheckerUtil : ICheckerUtil
    {
        private static readonly byte[] s_utf8Bom = Encoding.UTF8.GetPreamble();
        private static readonly byte[] s_shebang = Encoding.ASCII.GetBytes("#!");

        private readonly string _repositoryDirectory;
        private readonly string _repositoryDirectoryPrefix;

        internal BomCheckerUtil(string repositoryDirectory)
        {
            _repositoryDirectory = Path.GetFullPath(repositoryDirectory);
            _repositoryDirectoryPrefix = _repositoryDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
        }

        public bool Check(TextWriter textWriter)
        {
            if (!TryGetTrackedFiles(textWriter, out var trackedFiles))
            {
                return false;
            }

            return CheckFiles(trackedFiles, textWriter);
        }

        internal bool CheckFiles(IEnumerable<string> trackedFiles, TextWriter textWriter)
        {
            var files = new List<(string RelativePath, string FullPath)>();
            var analyzerConfigs = new Dictionary<string, AnalyzerConfig>(StringComparer.OrdinalIgnoreCase);

            foreach (var relativePath in trackedFiles)
            {
                if (relativePath.StartsWith("eng/common/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(Path.Combine(_repositoryDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!IsWithinRepository(fullPath) || Directory.Exists(fullPath) || !File.Exists(fullPath))
                {
                    continue;
                }

                files.Add((relativePath, fullPath));
                AddAnalyzerConfigs(fullPath, analyzerConfigs);
            }

            var configSet = AnalyzerConfigSet.Create(analyzerConfigs.Values.ToImmutableArray());
            var allGood = true;
            foreach (var (relativePath, fullPath) in files)
            {
                var prefix = new byte[5];
                var prefixLength = 0;
                using (var stream = File.OpenRead(fullPath))
                {
                    while (prefixLength < prefix.Length)
                    {
                        var bytesRead = stream.Read(prefix, prefixLength, prefix.Length - prefixLength);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        prefixLength += bytesRead;
                    }
                }

                var hasBom = HasPrefix(prefix, prefixLength, s_utf8Bom);
                var hasShebang = HasPrefix(prefix, prefixLength, s_shebang, hasBom ? s_utf8Bom.Length : 0);
                if (hasBom && hasShebang)
                {
                    textWriter.WriteLine($"{relativePath}: expected shebang at byte zero (no BOM)");
                    allGood = false;
                    continue;
                }

                var options = configSet.GetOptionsForSourcePath(fullPath).AnalyzerOptions;
                var charset = options.TryGetValue("charset", out var configuredCharset) ? configuredCharset : null;
                if (charset is null && IsSdkSyncedFileBasedProgram(relativePath))
                {
                    charset = "utf-8-bom";
                }

                if (charset is null or "unset")
                {
                    continue;
                }

                if (charset is not ("utf-8" or "utf-8-bom"))
                {
                    textWriter.WriteLine($"{relativePath}: unsupported charset '{charset}'");
                    allGood = false;
                    continue;
                }

                if (hasBom != (charset == "utf-8-bom"))
                {
                    textWriter.WriteLine($"{relativePath}: expected {charset}");
                    allGood = false;
                }
            }

            return allGood;
        }

        private bool TryGetTrackedFiles(TextWriter textWriter, out string[] trackedFiles)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "ls-files -z",
                    WorkingDirectory = _repositoryDirectory,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    textWriter.WriteLine("Failed to start git ls-files.");
                    trackedFiles = Array.Empty<string>();
                    return false;
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                var output = outputTask.Result;
                var error = errorTask.Result;
                if (process.ExitCode != 0)
                {
                    textWriter.WriteLine($"git ls-files failed with exit code {process.ExitCode}.");
                    if (error.Length > 0)
                    {
                        textWriter.WriteLine(error);
                    }

                    trackedFiles = Array.Empty<string>();
                    return false;
                }

                trackedFiles = output.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                return true;
            }
            catch (Exception ex)
            {
                textWriter.WriteLine($"Error running git ls-files: {ex.Message}");
                trackedFiles = Array.Empty<string>();
                return false;
            }
        }

        private void AddAnalyzerConfigs(string fullPath, Dictionary<string, AnalyzerConfig> analyzerConfigs)
        {
            var directory = Path.GetDirectoryName(fullPath);
            while (directory is not null && IsWithinRepository(directory))
            {
                var configPath = Path.Combine(directory, ".editorconfig");
                if (File.Exists(configPath) && !analyzerConfigs.ContainsKey(configPath))
                {
                    analyzerConfigs.Add(configPath, AnalyzerConfig.Parse(File.ReadAllText(configPath), configPath));
                }

                if (string.Equals(directory, _repositoryDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                directory = Path.GetDirectoryName(directory);
            }
        }

        private bool IsWithinRepository(string path)
            => string.Equals(path, _repositoryDirectory, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(_repositoryDirectoryPrefix, StringComparison.OrdinalIgnoreCase);

        private static bool HasPrefix(byte[] bytes, int bytesLength, byte[] prefix, int offset = 0)
        {
            if (bytesLength < offset + prefix.Length)
            {
                return false;
            }

            for (var i = 0; i < prefix.Length; i++)
            {
                if (bytes[offset + i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSdkSyncedFileBasedProgram(string relativePath)
        {
            return relativePath.StartsWith("src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/", StringComparison.OrdinalIgnoreCase)
                && relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }
    }
}
