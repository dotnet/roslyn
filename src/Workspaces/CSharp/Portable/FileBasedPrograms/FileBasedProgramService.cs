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

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position, findInsideTrivia: true);
        if (token.Parent is not IgnoredDirectiveTriviaSyntax { Content: (kind: SyntaxKind.StringLiteralToken) })
            return [];

        // Caret is on the content token of an FBA directive.
        // Get all directives (parsed form of the content) and find the one that matches current position.
        var sourceText = await root.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var directives = FileLevelDirectiveHelpers.FindDirectives(new SourceFile(root.SyntaxTree.FilePath, sourceText), reportAllErrors: false, errorReporter: ErrorReporters.IgnoringReporter, checkDuplicates: false);
        var directive = directives.FirstOrDefault(static (d, position) => d.Info.Span.Contains(position), position) as CSharpDirective.Named;
        if (directive is null)
            return [];

        // This is meant to do a similar transform as 'FileLevelDirectiveHelpers.EnsureProjectFilePath'
        // except it doesn't look at the file system and always uses an absolute path.
        var directivePath = FileUtilities.NormalizeRelativePath(directive.Name.Replace('\\', '/'), document.FilePath, baseDirectory: null);
        if (directivePath is null)
            return [];

        Contract.ThrowIfFalse(PathUtilities.IsAbsolute(directivePath));
        using var builder = TemporaryArray<string>.Empty;
        var isProjectDirective = directive is CSharpDirective.Project;
        if (isProjectDirective || directive is CSharpDirective.Ref)
        {
            var solution = document.Project.Solution;
            foreach (var reference in document.Project.ProjectReferences)
            {
                var projectPath = solution.GetRequiredProject(reference.ProjectId).FilePath;
                if (projectPath is null)
                    continue;

                if (PathUtilities.Comparer.Equals(directivePath, projectPath))
                    builder.Add(projectPath);

                // For '#:project', the 'directivePath' could look like '/folder/MyProject.csproj' or may just look like '/folder'.
                if (isProjectDirective
                    && Path.GetDirectoryName(projectPath) is { } projectDirectory
                    && PathUtilities.Comparer.Equals(
                        PathUtilities.TrimTrailingSeparators(directivePath),
                        PathUtilities.TrimTrailingSeparators(projectDirectory)))
                {
                    builder.Add(projectPath);
                }
            }
        }
        else if (directive is CSharpDirective.IncludeOrExclude)
        {
            foreach (var doc in document.Project.Documents)
            {
                if (string.Equals(directivePath, doc.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    builder.Add(directivePath);
                }
            }

            foreach (var doc in document.Project.AdditionalDocuments)
            {
                if (string.Equals(directivePath, doc.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    builder.Add(directivePath);
                }
            }
        }

        return builder.ToImmutableAndClear();
    }
}
