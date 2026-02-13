// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

#pragma warning disable CA2007 // We are OK awaiting tasks since we're following Visual Studio threading rules in this file

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem.Logging
{
    internal static class RoslynWorkspaceStructureLogger
    {
        private static int s_NextCompilationId;
        private static readonly ConditionalWeakTable<Compilation, StrongBox<int>> s_CompilationIds = new();

        public static void ShowSaveDialogAndLog(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var saveDialog = new SaveFileDialog()
            {
                Filter = "Zip Files (*.zip)|*.zip"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var threadingContext = serviceProvider.GetMefService<IThreadingContext>();

            threadingContext.JoinableTaskFactory.RunAsync(() => LogAsync(serviceProvider, threadingContext, saveDialog.FileName));
        }

        public static async Task LogAsync(IServiceProvider serviceProvider, IThreadingContext threadingContext, string path)
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            Assumes.Present(componentModel);
            var dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(SDTE));

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            var solution = workspace.CurrentSolution;

            var dialogFactory = (IVsThreadedWaitDialogFactory)serviceProvider.GetService(typeof(SVsThreadedWaitDialogFactory));
            Assumes.Present(dialogFactory);
            using var session = dialogFactory.StartWaitDialog(
                ServicesVSResources.Visual_Studio,
                new ThreadedWaitDialogProgressData(
                    ServicesVSResources.Logging_Roslyn_Workspace_structure,
                    progressText: null,
                    statusBarText: null,
                    isCancelable: true,
                    currentStep: 0,
                    totalSteps: solution.ProjectIds.Count),
                delayToShowDialog: TimeSpan.Zero);
            var cancellationToken = session.UserCancellationToken;

            // Now switch to the background thread while we're working
            await TaskScheduler.Default;

            try
            {
                var document = new XDocument();
                var workspaceElement = new XElement("workspace");
                workspaceElement.SetAttributeValue("kind", workspace.Kind);
                document.Add(workspaceElement);

                var projectsProcessed = 0;

                foreach (var project in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(cancellationToken).Select(solution.GetProject))
                {
                    if (project is null)
                        continue;

                    // Dump basic project attributes
                    var projectElement = new XElement("project");
                    workspaceElement.Add(projectElement);

                    projectElement.SetAttributeValue("id", SanitizePath(project.Id.ToString()));
                    projectElement.SetAttributeValue("name", project.Name);
                    projectElement.SetAttributeValue("assemblyName", project.AssemblyName);
                    projectElement.SetAttributeValue("language", project.Language);
                    projectElement.SetAttributeValue("path", SanitizePath(project.FilePath ?? "(none)"));
                    projectElement.SetAttributeValue("outputPath", SanitizePath(project.OutputFilePath ?? "(none)"));

                    var hasSuccessfullyLoaded = await project.HasSuccessfullyLoadedAsync(cancellationToken);
                    projectElement.SetAttributeValue("hasSuccessfullyLoaded", hasSuccessfullyLoaded);

                    // Dump MSBuild <Reference> nodes
                    if (project.FilePath != null)
                    {
                        var msbuildProject = XDocument.Load(project.FilePath);
                        var msbuildNamespace = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

                        var msbuildReferencesElement = new XElement("msbuildReferences");
                        projectElement.Add(msbuildReferencesElement);

                        msbuildReferencesElement.Add(msbuildProject.Descendants(msbuildNamespace + "ProjectReference"));
                        msbuildReferencesElement.Add(msbuildProject.Descendants(msbuildNamespace + "Reference"));
                        msbuildReferencesElement.Add(msbuildProject.Descendants(msbuildNamespace + "ReferencePath"));
                    }

                    // Dump DTE references
                    var langProjProject = await TryFindLangProjProjectAsync(threadingContext, dte, project);

                    if (langProjProject != null)
                    {
                        // Use of DTE is going to require the UI thread
                        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                        var dteReferences = new XElement("dteReferences");
                        projectElement.Add(dteReferences);

                        foreach (var reference in langProjProject.References.Cast<VSLangProj.Reference>())
                        {
                            if (reference.SourceProject != null)
                            {
                                dteReferences.Add(new XElement("projectReference", new XAttribute("projectName", reference.SourceProject.Name)));
                            }
                            else
                            {
                                dteReferences.Add(new XElement("metadataReference",
                                    reference.Path != null ? new XAttribute("path", SanitizePath(reference.Path)) : null,
                                    new XAttribute("name", reference.Name)));
                            }
                        }
                    }

                    // Dump the actual metadata references in the workspace
                    var workspaceReferencesElement = new XElement("workspaceReferences");
                    projectElement.Add(workspaceReferencesElement);

                    foreach (var metadataReference in project.MetadataReferences)
                    {
                        workspaceReferencesElement.Add(CreateElementForPortableExecutableReference(metadataReference));
                    }

                    // Dump project references in the workspace
                    foreach (var projectReference in project.AllProjectReferences)
                    {
                        var referenceElement = new XElement("projectReference", new XAttribute("id", SanitizePath(projectReference.ProjectId.ToString())));

                        if (!project.ProjectReferences.Contains(projectReference))
                        {
                            referenceElement.SetAttributeValue("missingInSolution", "true");
                        }

                        workspaceReferencesElement.Add(referenceElement);
                    }

                    projectElement.Add(new XElement("workspaceDocuments", await CreateElementsForDocumentCollectionAsync(project.Documents, "document", cancellationToken)));
                    projectElement.Add(new XElement("workspaceAdditionalDocuments", await CreateElementsForDocumentCollectionAsync(project.AdditionalDocuments, "additionalDocuments", cancellationToken)));

                    projectElement.Add(new XElement("workspaceAnalyzerConfigDocuments", await CreateElementsForDocumentCollectionAsync(project.AnalyzerConfigDocuments, "analyzerConfigDocument", cancellationToken)));

                    // Dump references from the compilation; this should match the workspace but can help rule out
                    // cross-language reference bugs or other issues like that
                    var compilation = await project.GetCompilationAsync(cancellationToken);

                    if (compilation != null)
                    {
                        var compilationReferencesElement = new XElement("compilationReferences");
                        projectElement.Add(compilationReferencesElement);

                        foreach (var reference in compilation.References)
                        {
                            compilationReferencesElement.Add(CreateElementForPortableExecutableReference(reference));
                        }

                        projectElement.Add(CreateElementForCompilation(compilation));

                        // Dump all diagnostics
                        var diagnosticsElement = new XElement("diagnostics");
                        projectElement.Add(diagnosticsElement);

                        foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
                        {
                            diagnosticsElement.Add(
                                new XElement("diagnostic",
                                    new XAttribute("id", diagnostic.Id),
                                    new XAttribute("severity", diagnostic.Severity.ToString()),
                                    new XAttribute("path", SanitizePath(diagnostic.Location.GetLineSpan().Path ?? "(none)")),
                                    diagnostic.GetMessage()));
                        }
                    }

                    projectsProcessed++;
                    session.Progress.Report(new ThreadedWaitDialogProgressData(
                        ServicesVSResources.Logging_Roslyn_Workspace_structure,
                        progressText: null,
                        statusBarText: null,
                        isCancelable: true,
                        currentStep: projectsProcessed,
                        totalSteps: solution.ProjectIds.Count));
                }

                File.Delete(path);

                using (var zipFile = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    var zipFileEntry = zipFile.CreateEntry("Workspace.xml", CompressionLevel.Fastest);
                    using (var stream = zipFileEntry.Open())
                    {
                        document.Save(stream);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // They cancelled
            }
        }

        private static async Task<VSLangProj.VSProject?> TryFindLangProjProjectAsync(IThreadingContext threadingContext, EnvDTE.DTE dte, Project project)
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

            foreach (EnvDTE.Project p in dte.Solution.Projects)
            {
                try
                {
                    if (string.Equals(p.FullName, project.FilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return p.Object as VSLangProj.VSProject;
                    }
                }
                catch (NotImplementedException)
                {
                    // Some EnvDTE.Projects will throw on p.FullName, so just bail in that case.
                }
            }

            return null;
        }

        private static string SanitizePath(string s)
        {
            return ReplacePathComponent(s, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "%USERPROFILE%");
        }

        /// <summary>
        /// Equivalent to string.Replace, but uses OrdinalIgnoreCase for matching.
        /// </summary>
        private static string ReplacePathComponent(string s, string oldValue, string newValue)
        {
            while (true)
            {
                var index = s.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                {
                    return s;
                }

                s = s.Substring(0, index) + newValue + s.Substring(index + oldValue.Length);
            }
        }

        private static XElement CreateElementForPortableExecutableReference(MetadataReference reference)
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
                    new XAttribute("file", SanitizePath(portableExecutableReference.FilePath ?? "(none)")),
                    new XAttribute("display", SanitizePath(portableExecutableReference.Display ?? "(none)")),
                    aliasesAttribute);
            }
            else
            {
                return new XElement("metadataReference", new XAttribute("display", SanitizePath(reference.Display ?? "(none)")));
            }
        }

        private static XElement CreateElementForCompilation(Compilation compilation)
        {
            StrongBox<int> compilationId;
            if (!s_CompilationIds.TryGetValue(compilation, out compilationId))
            {
                compilationId = new StrongBox<int>(s_NextCompilationId++);
                s_CompilationIds.Add(compilation, compilationId);
            }

            var namespaces = new Queue<INamespaceSymbol>();
            var typesElement = new XElement("types");

            namespaces.Enqueue(compilation.Assembly.GlobalNamespace);

            while (namespaces.Count > 0)
            {
                var @ns = namespaces.Dequeue();

                foreach (var type in @ns.GetTypeMembers())
                {
                    typesElement.Add(new XElement("type", new XAttribute("name", type.ToDisplayString())));
                }

                foreach (var childNamespace in @ns.GetNamespaceMembers())
                {
                    namespaces.Enqueue(childNamespace);
                }
            }

            return new XElement("compilation",
                new XAttribute("objectId", compilationId.Value),
                new XAttribute("assemblyIdentity", compilation.Assembly.Identity.ToString()),
                typesElement);
        }

        public static async Task<IEnumerable<XElement>> CreateElementsForDocumentCollectionAsync(IEnumerable<TextDocument> documents, string elementName, CancellationToken cancellationToken)
        {
            var elements = new List<XElement>();

            foreach (var document in documents)
            {
                var documentElement = new XElement(elementName, new XAttribute("path", SanitizePath(document.FilePath ?? "(none)")));

                var clientName = document.DocumentServiceProvider.GetService<DocumentPropertiesService>()?.DiagnosticsLspClientName;
                if (clientName != null)
                {
                    documentElement.SetAttributeValue("clientName", clientName);
                }

                var loadDiagnostic = await document.State.GetFailedToLoadExceptionMessageAsync(cancellationToken);

                if (loadDiagnostic != null)
                {
                    documentElement.Add(new XElement("loadDiagnostic", loadDiagnostic));
                }

                elements.Add(documentElement);
            }

            return elements;
        }
    }
}