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
        internal const string DirectoryName = "RunTestsStorage";

        private readonly string _storagePath;

        internal LocalDataStorage(string storagePath = null)
        {
            _storagePath = storagePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DirectoryName);
        }

        public bool TryGetTestResult(string cacheKey, out TestResult testResult)
        {
            testResult = default(TestResult);

            var filePath = Path.Combine(_storagePath, cacheKey);
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var text = File.ReadAllText(filePath);
                if (text.Length > 0)
                {
                    testResult = new TestResult(succeeded: true, assemblyName: text, elapsed: TimeSpan.FromSeconds(0), errorOutput: string.Empty);
                    return true;
                }
            }
            catch (Exception)
            {
                // Okay for exception to occur here on I/O
            }

            return false;
        }

        public void AddTestResult(string cacheKey, TestResult testResult)
        {
            // TODO: Cache more than just success 
            if (testResult.Succeeded)
            {
                try
                {
                    FileUtil.EnsureDirectory(_storagePath);
                    var filePath = Path.Combine(_storagePath, cacheKey);
                    File.WriteAllText(filePath, testResult.AssemblyName);
                }
                catch (Exception)
                {
                    // I/O errors are expected and okay here.
                }
            }
        }
    }
}
