// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    /// <summary>
    /// Data storage that works under %LOCALAPPDATA%
    /// </summary>
    internal sealed class LocalDataStorage : IDataStorage
    {
        private enum StorageKind
        {
            AssemblyPath,
            ExitCode,
            CommandLine,
            StandardOutput,
            ErrorOutput,
            ResultsFile,
        }

        internal const string DirectoryName = "RunTestsStorage";

        private readonly string _storagePath;

        internal LocalDataStorage(string storagePath = null)
        {
            _storagePath = storagePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DirectoryName);
        }

        public bool TryGetTestResult(string cacheKey, out TestResult testResult)
        {
            testResult = default(TestResult);

            var storageFolder = GetStorageFolder(cacheKey);
            if (!Directory.Exists(storageFolder))
            {
                return false;
            }

            try
            {
                var exitCode = Read(cacheKey, StorageKind.ExitCode);
                var commandLine = Read(cacheKey, StorageKind.CommandLine);
                var assemblyPath = Read(cacheKey, StorageKind.AssemblyPath);
                var standardOutput = Read(cacheKey, StorageKind.StandardOutput);
                var errorOutput = Read(cacheKey, StorageKind.ErrorOutput);
                var resultsFilePath = GetStoragePath(cacheKey, StorageKind.ResultsFile);
                if (!File.Exists(resultsFilePath))
                {
                    resultsFilePath = null;
                }

                testResult = new TestResult(
                    exitCode: int.Parse(exitCode),
                    assemblyPath: assemblyPath,
                    resultsFilePath: resultsFilePath,
                    commandLine: commandLine,
                    elapsed: TimeSpan.FromSeconds(0),
                    standardOutput: standardOutput,
                    errorOutput: errorOutput);
                return true;
            }
            catch (Exception e)
            {
                // Okay for exception to occur here on I/O
                Logger.Log($"Failed to read cache {cacheKey} {e.Message}");
            }

            return false;
        }

        public void AddTestResult(string cacheKey, TestResult testResult)
        {
            var storagePath = Path.Combine(_storagePath, cacheKey);
            try
            {
                if (!FileUtil.EnsureDirectory(storagePath))
                {
                    return;
                }

                Write(cacheKey, StorageKind.ExitCode, testResult.ExitCode.ToString());
                Write(cacheKey, StorageKind.AssemblyPath, testResult.AssemblyPath);
                Write(cacheKey, StorageKind.StandardOutput, testResult.StandardOutput);
                Write(cacheKey, StorageKind.ErrorOutput, testResult.ErrorOutput);
                Write(cacheKey, StorageKind.CommandLine, testResult.CommandLine);

                if (!string.IsNullOrEmpty(testResult.ResultsFilePath))
                {
                    File.Copy(testResult.ResultsFilePath, GetStoragePath(cacheKey, StorageKind.ResultsFile));
                }
            }
            catch (Exception e)
            {
                // I/O errors are expected and okay here.
                Logger.Log($"Failed to log {cacheKey} {e.Message}");
                FileUtil.DeleteDirectory(storagePath);
            }
        }

        private string GetStorageFolder(string cacheKey)
        {
            return Path.Combine(_storagePath, cacheKey);
        }

        private string GetStoragePath(string cacheKey, StorageKind kind)
        {
            return Path.Combine(GetStorageFolder(cacheKey), kind.ToString());
        }

        private void Write(string cacheKey, StorageKind kind, string contents)
        {
            var filePath = GetStoragePath(cacheKey, kind);
            File.WriteAllText(filePath, contents);
        }

        private string Read(string cacheKey, StorageKind kind)
        {
            var filePath = GetStoragePath(cacheKey, kind);
            return File.ReadAllText(filePath);
        }
    }
}
