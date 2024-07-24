#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace Metalama.Compiler;

internal class TransformedPathGenerator
{
    private static readonly string _backwardDirectory = $"..{Path.DirectorySeparatorChar}";
    private readonly HashSet<string> _generatedPaths = new(StringComparer.OrdinalIgnoreCase);

    private readonly string? _projectDirectory;
    private readonly string _outputDirectory;
    private readonly string _workingDirectory;
    private readonly string? _generatorsDirectory;

    public TransformedPathGenerator(string? projectDirectory, string? outputDirectory, string workingDirectory, string? generatorsDirectory)
    {
        _projectDirectory = projectDirectory;

        // The outputDirectory variable may be null if the code is not to be written to disk.
        _outputDirectory = outputDirectory ?? "(Transformed)";
        _workingDirectory = workingDirectory;
        _generatorsDirectory = generatorsDirectory;
    }

    public string GetOutputPath(string? syntaxTreeFilePath)
    {
        if (string.IsNullOrEmpty(syntaxTreeFilePath))
        {
            syntaxTreeFilePath = "Unnamed.cs";
        }

        string stem;

        if (_generatorsDirectory != null && syntaxTreeFilePath.StartsWith(_generatorsDirectory))
        {
            stem = Path.Combine("generated", syntaxTreeFilePath.Substring(_generatorsDirectory.Length + 1));
        }
        else if (_projectDirectory != null)
        {
            // We should have a project directory in production.
            var fullPath = Path.GetFullPath(Path.Combine(_workingDirectory, syntaxTreeFilePath));
            var relativePath = new Uri(_projectDirectory + "/").MakeRelativeUri(new Uri(fullPath, UriKind.Absolute))
                .ToString()
                .Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(relativePath))
            {
                stem = Path.Combine("links", relativePath.Substring(Path.GetPathRoot(relativePath)?.Length ?? 0));
            }
            else if (relativePath.StartsWith(_backwardDirectory))
            {
                stem = Path.Combine("links", relativePath.Replace(_backwardDirectory, ""));
            }
            else
            {
                stem = relativePath;
            }
        }
        else
        {
            // This for testing.
            stem = Path.GetFileName(syntaxTreeFilePath);
        }

        if (!_generatedPaths.Add(stem))
        {
            var extension = Path.GetExtension(stem);
            var stemWithoutExtension = stem.Substring(0, stem.Length - extension.Length);

            for (var i = 2; ; i++)
            {
                var candidate = $"{stemWithoutExtension}_{i}{extension}";
                if (_generatedPaths.Add(candidate))
                {
                    stem = candidate;
                    break;
                }
            }
        }

        return Path.Combine(_outputDirectory, stem);
    }
}
