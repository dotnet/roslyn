// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Logging;

/// <summary>
/// Builds an XML representation of the workspace structure for diagnostic purposes.
/// This common logic is shared by the Visual Studio command and the LSP handler.
/// </summary>
internal class WorkspaceStructureLogger
{
    private int _nextCompilationId = -1;
    private readonly ConditionalWeakTable<Compilation, StrongBox<int>> _compilationIds = new();

    /// <summary>
    /// Builds an XML document describing the full workspace structure for the given solution.
    /// </summary>
    /// <param name="solution">The solution to log.</param>
    /// <param name="workspaceKind">The <see cref="Workspace.Kind"/> string (e.g. "MSBuildWorkspace").</param>
    /// <param name="progress">Optional progress callback receiving (current, total) project counts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<XDocument> BuildWorkspaceStructureAsync(
        Solution solution,
        string? workspaceKind,
        IProgress<(int current, int total)>? progress,
        CancellationToken cancellationToken = default)
    {
        var document = new XDocument();
        var workspaceElement = new XElement("workspace");
        workspaceElement.SetAttributeValue("kind", workspaceKind);
        document.Add(workspaceElement);

        var projectsProcessed = 0;
        var totalProjects = solution.ProjectIds.Count;

        foreach (var project in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(cancellationToken).Select(solution.GetProject))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (project is null)
                continue;

            var projectElement = await BuildProjectElementAsync(project, cancellationToken).ConfigureAwait(false);

            // Dump MSBuild <Reference> nodes
            var msbuildReferencesElement = CreateMsBuildReferencesElement(project);
            if (msbuildReferencesElement != null)
                projectElement.Add(msbuildReferencesElement);

            // Allow callers to inject host-specific elements (e.g. DTE references)
            var additionalElements = await CreateAdditionalProjectElementsAsync(project, cancellationToken).ConfigureAwait(false);
            if (additionalElements != null)
            {
                projectElement.Add(additionalElements);
            }

            // Add workspace references (metadata + project)
            projectElement.Add(BuildWorkspaceReferencesElement(project));

            var workspaceAnalyzerReferencesElement = new XElement("workspaceAnalyzerReferences");
            projectElement.Add(workspaceAnalyzerReferencesElement);

            foreach (var analyzerReference in project.AnalyzerReferences)
            {
                workspaceAnalyzerReferencesElement.Add(CreateElementForAnalyzerReference(analyzerReference));
            }

            // Add documents
            projectElement.Add(new XElement("workspaceDocuments", await CreateElementsForDocumentCollectionAsync(project.Documents, "document", cancellationToken).ConfigureAwait(false)));
            projectElement.Add(new XElement("workspaceAdditionalDocuments", await CreateElementsForDocumentCollectionAsync(project.AdditionalDocuments, "additionalDocuments", cancellationToken).ConfigureAwait(false)));
            projectElement.Add(new XElement("workspaceAnalyzerConfigDocuments", await CreateElementsForDocumentCollectionAsync(project.AnalyzerConfigDocuments, "analyzerConfigDocument", cancellationToken).ConfigureAwait(false)));

            // Add source generated documents
            var sourceGeneratedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            projectElement.Add(new XElement("workspaceSourceGeneratedDocuments", CreateElementsForSourceGeneratedDocuments(sourceGeneratedDocuments)));

            // Add generator diagnostics
            var generatorDiagnosticsElement = new XElement("generatorDiagnostics");
            projectElement.Add(generatorDiagnosticsElement);

            foreach (var diagnostic in await project.GetSourceGeneratorDiagnosticsAsync(cancellationToken).ConfigureAwait(false))
            {
                generatorDiagnosticsElement.Add(CreateElementForDiagnostic(diagnostic));
            }

            // Add compilation info and diagnostics
            await AddCompilationElementsAsync(projectElement, project, cancellationToken).ConfigureAwait(false);

            workspaceElement.Add(projectElement);

            projectsProcessed++;
            progress?.Report((projectsProcessed, totalProjects));
        }

        return document;
    }

    protected virtual Task<IEnumerable<XElement>> CreateAdditionalProjectElementsAsync(Project project, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<XElement>>([]);

    private static async Task<XElement> BuildProjectElementAsync(Project project, CancellationToken cancellationToken)
    {
        var projectElement = new XElement("project");

        projectElement.SetAttributeValue("id", SanitizePath(project.Id.ToString()));
        projectElement.SetAttributeValue("name", project.Name);
        projectElement.SetAttributeValue("assemblyName", project.AssemblyName);
        projectElement.SetAttributeValue("language", project.Language);
        projectElement.SetAttributeValue("path", SanitizePath(project.FilePath));
        projectElement.SetAttributeValue("outputPath", SanitizePath(project.OutputFilePath));

        var hasSuccessfullyLoaded = await project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);
        projectElement.SetAttributeValue("hasSuccessfullyLoaded", hasSuccessfullyLoaded);

        return projectElement;
    }

    private static XElement? CreateMsBuildReferencesElement(Project project)
    {
        if (project.FilePath == null)
            return null;

        var msbuildProject = XDocument.Load(project.FilePath, LoadOptions.None);
        var msbuildNamespace = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

        var msbuildReferencesElement = new XElement("msbuildReferences");

        msbuildReferencesElement.Add(msbuildProject.Descendants(msbuildNamespace + "ProjectReference"));
        msbuildReferencesElement.Add(msbuildProject.Descendants(msbuildNamespace + "Reference"));
        msbuildReferencesElement.Add(msbuildProject.Descendants(msbuildNamespace + "ReferencePath"));

        return msbuildReferencesElement;
    }

    private XElement BuildWorkspaceReferencesElement(Project project)
    {
        var workspaceReferencesElement = new XElement("workspaceReferences");

        foreach (var metadataReference in project.MetadataReferences)
            workspaceReferencesElement.Add(CreateElementForPortableExecutableReference(metadataReference));

        foreach (var projectReference in project.AllProjectReferences)
        {
            var referenceElement = new XElement("projectReference", new XAttribute("id", SanitizePath(projectReference.ProjectId.ToString())));

            if (!project.ProjectReferences.Contains(projectReference))
                referenceElement.SetAttributeValue("missingInSolution", "true");

            workspaceReferencesElement.Add(referenceElement);
        }

        return workspaceReferencesElement;
    }

    private async Task AddCompilationElementsAsync(XElement projectElement, Project project, CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation == null)
            return;

        var compilationReferencesElement = new XElement("compilationReferences");
        projectElement.Add(compilationReferencesElement);

        foreach (var reference in compilation.References)
            compilationReferencesElement.Add(CreateElementForPortableExecutableReference(reference));

        projectElement.Add(CreateElementForCompilation(compilation));

        var diagnosticsElement = new XElement("diagnostics");
        projectElement.Add(diagnosticsElement);

        foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
        {
            diagnosticsElement.Add(CreateElementForDiagnostic(diagnostic));
        }
    }

    private static XElement CreateElementForDiagnostic(Diagnostic diagnostic)
        => new("diagnostic",
            new XAttribute("id", diagnostic.Id),
            new XAttribute("severity", diagnostic.Severity.ToString()),
            new XAttribute("path", SanitizePath(diagnostic.Location.GetLineSpan().Path)),
            diagnostic.GetMessage());

    private static IEnumerable<XElement> CreateElementsForSourceGeneratedDocuments(IEnumerable<SourceGeneratedDocument> documents)
    {
        var elements = new List<XElement>();

        foreach (var document in documents)
        {
            var identity = document.Identity;
            var element = new XElement("sourceGeneratedDocument",
                new XAttribute("hintName", document.HintName),
                new XAttribute("path", SanitizePath(document.FilePath)),
                new XAttribute("generatorType", identity.Generator.TypeName),
                new XAttribute("generatorAssembly", identity.Generator.AssemblyName),
                new XAttribute("generatorAssemblyVersion", identity.Generator.AssemblyVersion.ToString()),
                new XAttribute("generatorAssemblyPath", SanitizePath(identity.Generator.AssemblyPath)));

            elements.Add(element);
        }

        return elements;
    }

    protected static string SanitizePath(string? s)
    {
        if (s is null)
            return "(none)";

        return ReplacePathComponent(s, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "%USERPROFILE%");
    }

    /// <summary>
    /// Equivalent to string.Replace, but uses OrdinalIgnoreCase for matching.
    /// </summary>
    protected static string ReplacePathComponent(string s, string oldValue, string newValue)
    {
        while (true)
        {
            var index = s.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
                return s;

            s = s.Substring(0, index) + newValue + s.Substring(index + oldValue.Length);
        }
    }

    internal XElement CreateElementForPortableExecutableReference(MetadataReference reference)
    {
        var aliasesAttribute = new XAttribute("aliases", string.Join(",", reference.Properties.Aliases));

        if (reference is CompilationReference compilationReference)
        {
            return new XElement("compilationReference",
                aliasesAttribute,
                CreateElementForCompilation(compilationReference.Compilation));
        }
        else if (reference is PortableExecutableReference portableExecutableReference)
        {
            return new XElement("peReference",
                new XAttribute("file", SanitizePath(portableExecutableReference.FilePath)),
                new XAttribute("display", SanitizePath(portableExecutableReference.Display)),
                aliasesAttribute);
        }
        else
        {
            return new XElement("metadataReference", new XAttribute("display", SanitizePath(reference.Display)));
        }
    }

    private static XElement CreateElementForAnalyzerReference(Microsoft.CodeAnalysis.Diagnostics.AnalyzerReference reference)
        => new("analyzerReference",
            new XAttribute("path", SanitizePath(reference.FullPath)),
            new XAttribute("display", SanitizePath(reference.Display)));

    private XElement CreateElementForCompilation(Compilation compilation)
    {
        var compilationId = _compilationIds.GetValue(
             compilation, _ => new StrongBox<int>(Interlocked.Increment(ref _nextCompilationId)));

        var namespaces = new Queue<INamespaceSymbol>();
        var typesElement = new XElement("types");

        namespaces.Enqueue(compilation.Assembly.GlobalNamespace);

        while (namespaces.Count > 0)
        {
            var @ns = namespaces.Dequeue();

            foreach (var type in @ns.GetTypeMembers())
                typesElement.Add(new XElement("type", new XAttribute("name", type.ToDisplayString())));

            foreach (var childNamespace in @ns.GetNamespaceMembers())
                namespaces.Enqueue(childNamespace);
        }

        return new XElement("compilation",
            new XAttribute("objectId", compilationId.Value),
            new XAttribute("assemblyIdentity", compilation.Assembly.Identity.ToString()),
            typesElement);
    }

    internal static async Task<IEnumerable<XElement>> CreateElementsForDocumentCollectionAsync(IEnumerable<TextDocument> documents, string elementName, CancellationToken cancellationToken)
    {
        var elements = new List<XElement>();

        foreach (var document in documents)
        {
            var documentElement = new XElement(elementName, new XAttribute("path", SanitizePath(document.FilePath)));

            var clientName = document.DocumentServiceProvider.GetService<DocumentPropertiesService>()?.DiagnosticsLspClientName;
            if (clientName != null)
                documentElement.SetAttributeValue("clientName", clientName);

            var loadDiagnostic = await document.State.GetFailedToLoadExceptionMessageAsync(cancellationToken).ConfigureAwait(false);

            if (loadDiagnostic != null)
                documentElement.Add(new XElement("loadDiagnostic", loadDiagnostic));

            elements.Add(documentElement);
        }

        return elements;
    }
}
