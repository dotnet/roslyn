// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace BuildValidator
{
    /// <summary>
    /// Roslyn specific implementation for looking for files
    /// in the Roslyn repo
    /// </summary>
    internal class LocalSourceResolver
    {
        private readonly DirectoryInfo _baseDirectory;
        private readonly ILogger _logger;

        public LocalSourceResolver(ILoggerFactory loggerFactory)
        {
            _baseDirectory = GetSourceDirectory();
            _logger = loggerFactory.CreateLogger<LocalSourceResolver>();

            _logger.LogInformation($"Source Base Directory: {_baseDirectory}");
        }

        public SourceText ResolveSource(string name, Encoding encoding)
        {
            if (!File.Exists(name))
            {
                _logger.LogTrace($"{name} doesn't exist, adding base directory");
                name = Path.Combine(_baseDirectory.FullName, name);
            }
            if (File.Exists(name))
            {
                using var fileStream = File.OpenRead(name);
                var sourceText = SourceText.From(fileStream, encoding: encoding);
                return sourceText;
            }

            throw new FileNotFoundException(name);
        }

        private static DirectoryInfo GetSourceDirectory()
        {
            var assemblyLocation = typeof(LocalSourceResolver).Assembly.Location;
            var srcDir = Directory.GetParent(assemblyLocation);

            while (srcDir != null)
            {
                var potentialDir = srcDir.GetDirectories().FirstOrDefault(IsSourceDirectory);
                if (potentialDir is null)
                {
                    srcDir = srcDir.Parent;
                }
                else
                {
                    srcDir = potentialDir;
                    break;
                }
            }

            if (srcDir == null)
            {
                throw new Exception("Unable to find src directory");
            }

            return srcDir;

            static bool IsSourceDirectory(DirectoryInfo directoryInfo)
            {
                if (FileNameEqualityComparer.StringComparer.Equals(directoryInfo.Name, "src"))
                {
                    // Check that src/compilers exists to be more accurate about getting the correct src directory
                    return directoryInfo.GetDirectories().Any(d => FileNameEqualityComparer.StringComparer.Equals(d.Name, "compilers"));
                }

                return false;
            }
        }
    }
}
