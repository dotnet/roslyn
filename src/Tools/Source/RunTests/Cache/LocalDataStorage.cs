// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    /// <summary>
    /// Data storage that works under %LOCALAPPDATA%
    /// TODO: need to do garbage collection on the files
    /// </summary>
    internal sealed class LocalDataStorage : IDataStorage
    {
        private enum StorageKind
        {
            ExitCode,
            StandardOutput,
            ErrorOutput,
            ResultsFileContent,
            ResultsFileName,
            Content
        }

        internal const int MaxStorageCount = 200;
        internal const string DirectoryName = "RunTestsStorage";

        private readonly string _storagePath;

        internal LocalDataStorage(string storagePath = null)
        {
            _storagePath = storagePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DirectoryName);
        }

        public bool TryGetCachedTestResult(string checksum, out CachedTestResult testResult)
        {
            testResult = default(CachedTestResult);

            var storageFolder = GetStorageFolder(checksum);
            if (!Directory.Exists(storageFolder))
            {
                return false;
            }

            try
            {
                var exitCode = Read(checksum, StorageKind.ExitCode);
                var standardOutput = Read(checksum, StorageKind.StandardOutput);
                var errorOutput = Read(checksum, StorageKind.ErrorOutput);
                var resultsFileName = Read(checksum, StorageKind.ResultsFileName);
                var resultsFileContent = Read(checksum, StorageKind.ResultsFileContent);

                testResult = new CachedTestResult(
                    exitCode: int.Parse(exitCode),
                    standardOutput: standardOutput,
                    errorOutput: errorOutput,
                    resultsFileName: resultsFileName,
                    resultsFileContent: resultsFileContent);
                return true;
            }
            catch (Exception e)
            {
                // Okay for exception to occur here on I/O
                Logger.Log($"Failed to read cache {checksum} {e.Message}");
            }

            return false;
        }

        public void AddCachedTestResult(ContentFile contentFile, CachedTestResult testResult)
        {
            var checksum = contentFile.Checksum;
            var storagePath = Path.Combine(_storagePath, checksum);
            try
            {
                if (!FileUtil.EnsureDirectory(storagePath))
                {
                    return;
                }

                Write(checksum, StorageKind.ExitCode, testResult.ExitCode.ToString());
                Write(checksum, StorageKind.StandardOutput, testResult.StandardOutput);
                Write(checksum, StorageKind.ErrorOutput, testResult.ErrorOutput);
                Write(checksum, StorageKind.ResultsFileName, testResult.ResultsFileName);
                Write(checksum, StorageKind.ResultsFileContent, testResult.ResultsFileContent);
                Write(checksum, StorageKind.Content, contentFile.Content);
            }
            catch (Exception e)
            {
                // I/O errors are expected and okay here.
                Logger.Log($"Failed to log {checksum} {e.Message}");
                FileUtil.DeleteDirectory(storagePath);
            }
        }

        private string GetStorageFolder(string checksum)
        {
            return Path.Combine(_storagePath, checksum);
        }

        private string GetStoragePath(string checksum, StorageKind kind)
        {
            return Path.Combine(GetStorageFolder(checksum), kind.ToString());
        }

        private void Write(string checksum, StorageKind kind, string contents)
        {
            var filePath = GetStoragePath(checksum, kind);
            File.WriteAllText(filePath, contents);
        }

        private string Read(string checksum, StorageKind kind)
        {
            var filePath = GetStoragePath(checksum, kind);
            return File.ReadAllText(filePath);
        }

        private void CleanupStorage()
        {
            try
            {
                var files = Directory.GetFiles(_storagePath);
                if (files.Length < MaxStorageCount)
                {
                    return;
                }

                var clean = files.Length - (MaxStorageCount / 2);
                var items = files
                    .Select(x => new DirectoryInfo(x))
                    .OrderBy(x => x.CreationTimeUtc)
                    .Take(clean);

                foreach (var item in items)
                {
                    FileUtil.DeleteDirectory(item.Name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to cleanup storage {ex.Message}");
            }
        }
    }
}
