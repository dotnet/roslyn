// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.DotNet.FileBasedPrograms;
using Microsoft.Extensions.FileSystemGlobbing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

[ExportWorkspaceService(typeof(IFileBasedProgramService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FileBasedProgramService() : IFileBasedProgramService
{
    public string GetArtifactsPath(string entryPointFileFullPath, string? dotNetSubdirectory = null)
        => VirtualProjectBuilder.GetArtifactsPath(entryPointFileFullPath, dotNetSubdirectory);

    public string GetTempSubdirectory(string? dotNetSubdirectory = null)
        => VirtualProjectBuilder.GetTempSubdirectory(dotNetSubdirectory);

    public IDictionary<string, string> GetGlobalBuildProperties()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in VirtualProjectBuilder.GetGlobalBuildProperties())
        {
            result.Add(kvp.Key, kvp.Value);
        }
        return result;
    }

    public bool IsValidEntryPointPath(string entryPointFilePath)
        => VirtualProjectBuilder.IsValidEntryPointPath(entryPointFilePath);

    public async ValueTask<IProjectRootElement> LoadFileBasedAppProjectAsync(
        IBuildService buildService,
        IProjectCollection projectCollection,
        string entryPointFilePath,
        Action<string> reportError)
    {
        var entryPointFileFullPath = Path.GetFullPath(entryPointFilePath);
        var virtualProjectBuilder = new VirtualProjectBuilder(buildService, entryPointFileFullPath, targetFramework: null);
        var result = await virtualProjectBuilder.CreateProjectInstanceAsync(
            projectCollection,
            (text, path, textSpan, message, innerException) => reportError($"{new SourceFile(path, text).GetLocationString(textSpan)}: {message}"))
            .ConfigureAwait(false);
        return result.ProjectRootElement;
    }

    public async Task<ImmutableArray<string>> GetFileLevelDirectiveTargetPathsAsync(Document document, int position, CancellationToken cancellationToken)
    {
        if (document.Project.Language != LanguageNames.CSharp
            || document.Project.ParseOptions?.Features.ContainsKey("FileBasedProgram") != true)
        {
            return [];
        }

        var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = syntaxRoot.FindToken(position, findInsideTrivia: true);
        if (token.Parent is not IgnoredDirectiveTriviaSyntax { Content: (kind: SyntaxKind.StringLiteralToken) })
            return [];

        // Caret is on the content token of an FBA directive.
        // Get all directives (parsed form of the content) and find the one that matches current position.
        var sourceText = await syntaxRoot.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var directives = FileLevelDirectiveHelpers.FindDirectives(new SourceFile(syntaxRoot.SyntaxTree.FilePath, sourceText), reportAllErrors: false, errorReporter: ErrorReporters.IgnoringReporter, checkDuplicates: false);
        var directive = directives.FirstOrDefault(static (d, position) => d.Info.Span.Contains(position), position) as CSharpDirective.Named;
        if (directive is null)
            return [];

        // This is meant to do a similar transform as 'FileLevelDirectiveHelpers.EnsureProjectFilePath'
        // except it doesn't look at the file system and always uses an absolute path.
        var directivePath = FileUtilities.NormalizeRelativePath(directive.Name.Replace('\\', '/'), document.FilePath, baseDirectory: null);
        if (directivePath is null)
            return [];

        // Match '*'/'**' globs in the directivePath.
        // 'Matcher' is designed for patterns to be relative to a base directory.
        // For simplicity we will just use the path root as a base directory.
        var pathRoot = Path.GetPathRoot(directivePath);
        Contract.ThrowIfNull(pathRoot); // 'directivePath' is expected to be absolute here, so we should always have a 'pathRoot'
        var matcher = new Matcher();
        var includePath = PathUtilities.GetRelativePath(pathRoot, PathUtilities.TrimTrailingSeparators(directivePath));
        matcher.AddInclude(includePath);

        using var builder = TemporaryArray<string>.Empty;
        var isProjectDirective = directive is CSharpDirective.Project;
        if (isProjectDirective || directive is CSharpDirective.Ref)
        {
            var solution = document.Project.Solution;
            foreach (var reference in document.Project.ProjectReferences)
            {
                var projectPath = solution.GetRequiredProject(reference.ProjectId).FilePath;
                if (!PathUtilities.IsAbsolute(projectPath))
                    continue;

                if (matcher.Match(rootDir: pathRoot, projectPath).HasMatches)
                    builder.Add(projectPath);

                // For '#:project', the 'directivePath' could look like '/folder/MyProject.csproj' or could look like '/folder' or '/folder/'.
                if (isProjectDirective
                    && Path.GetDirectoryName(projectPath) is { } projectDirectory
                    && matcher.Match(rootDir: pathRoot, PathUtilities.TrimTrailingSeparators(projectDirectory)).HasMatches)
                {
                    builder.Add(projectPath);
                }
            }
        }
        else if (directive is CSharpDirective.IncludeOrExclude)
        {
            foreach (var doc in document.Project.Documents)
            {
                if (!PathUtilities.IsAbsolute(doc.FilePath))
                    continue;

                if (matcher.Match(rootDir: pathRoot, doc.FilePath).HasMatches)
                    builder.Add(doc.FilePath);
            }

            foreach (var doc in document.Project.AdditionalDocuments)
            {
                if (!PathUtilities.IsAbsolute(doc.FilePath))
                    continue;

                if (matcher.Match(rootDir: pathRoot, doc.FilePath).HasMatches)
                    builder.Add(doc.FilePath);
            }
        }

        return builder.ToImmutableAndClear();
    }
}
