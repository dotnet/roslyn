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
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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

            Log(serviceProvider, saveDialog.FileName);
        }

        public static void Log(IServiceProvider serviceProvider, string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            Assumes.Present(componentModel);
            var dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(SDTE));

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            var solution = workspace.CurrentSolution;

            var threadedWaitDialog = (IVsThreadedWaitDialog3)serviceProvider.GetService(typeof(SVsThreadedWaitDialog));
            Assumes.Present(threadedWaitDialog);
            var threadedWaitCallback = new ThreadedWaitCallback();

            var projectsProcessed = 0;
            threadedWaitDialog.StartWaitDialogWithCallback(ServicesVSResources.Visual_Studio, ServicesVSResources.Logging_Roslyn_Workspace_structure, null, null, null, true, 0, true, solution.ProjectIds.Count, 0, threadedWaitCallback);
            var cancellationToken = threadedWaitCallback.CancellationToken;

            try
            {
                var document = new XDocument();
                var workspaceElement = new XElement("workspace");
                workspaceElement.SetAttributeValue("kind", workspace.Kind);
                document.Add(workspaceElement);

                foreach (var project in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(threadedWaitCallback.CancellationToken).Select(solution.GetProject))
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

                    var hasSuccessfullyLoaded = TryGetHasSuccessfullyLoaded(project, cancellationToken);

                    if (hasSuccessfullyLoaded.HasValue)
                    {
                        projectElement.SetAttributeValue("hasSuccessfullyLoaded", hasSuccessfullyLoaded.Value);
                    }

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
                    var langProjProject = TryFindLangProjProject(dte, project);

                    if (langProjProject != null)
                    {
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

                    projectElement.Add(new XElement("workspaceDocuments", CreateElementsForDocumentCollection(project.Documents, "document", cancellationToken)));
                    projectElement.Add(new XElement("workspaceAdditionalDocuments", CreateElementsForDocumentCollection(project.AdditionalDocuments, "additionalDocuments", cancellationToken)));

                    // Read AnalyzerConfigDocuments via reflection, as our target version may not be on a Roslyn
                    // new enough to support it.
                    var analyzerConfigDocumentsProperty = project.GetType().GetProperty("AnalyzerConfigDocuments");

                    if (analyzerConfigDocumentsProperty != null)
                    {
                        var analyzerConfigDocuments = (IEnumerable<TextDocument>)analyzerConfigDocumentsProperty.GetValue(project);
                        projectElement.Add(new XElement("workspaceAnalyzerConfigDocuments", CreateElementsForDocumentCollection(analyzerConfigDocuments, "analyzerConfigDocument", cancellationToken)));
                    }

                    // Dump references from the compilation; this should match the workspace but can help rule out
                    // cross-language reference bugs or other issues like that
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits -- this is fine since it's a Roslyn API
                    var compilation = project.GetCompilationAsync(cancellationToken).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

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

                    bool cancelled;
                    threadedWaitDialog.UpdateProgress(null, null, null, projectsProcessed, solution.ProjectIds.Count, false, out cancelled);
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
            finally
            {
                int cancelled;
                threadedWaitDialog.EndWaitDialog(out cancelled);
            }
        }

        private static System.Reflection.MethodInfo TryGetMethodInfo(this object o, string methodName)
        {
            return o.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        private static T? TryCallNonPublicMethod<T>(this object o, string methodName, params object[] parameters) where T : class
        {
            var method = o.TryGetMethodInfo(methodName);

            if (method == null)
            {
                return null;
            }

            return method.Invoke(o, parameters) as T;
        }

        private static T? TryCallNonPublicGenericMethod<T>(this object o, string methodName, Type typeParameter, params object[] parameters) where T : class
        {
            var method = o.TryGetMethodInfo(methodName);

            if (method == null)
            {
                return null;
            }

            method = method.MakeGenericMethod(typeParameter);
            if (method == null)
            {
                return null;
            }

            return method.Invoke(o, parameters) as T;
        }

        private static T? TryGetNonPublicPropertyFromService<T>(this object o, string serviceTypeName, string propertyName) where T : class
        {
            var services = o.TryGetNonPublicProperty<object>("Services");
            if (services == null)
            {
                return null;
            }

            // With apologies to future developers about the enormity of this assumption
            var serviceType = o.GetType().Assembly.GetType(serviceTypeName);
            if (serviceType == null)
            {
                return null;
            }

            var service = services.TryCallNonPublicGenericMethod<object>("GetService", serviceType);
            if (service == null)
            {
                return null;
            }

            return service.TryGetNonPublicProperty<T>(propertyName);
        }

        private static T? TryGetNonPublicProperty<T>(this object o, string propertyName) where T : class
        {
            // Yes this method says NonPublic, but it could be a public property on a non-public type
            var method = o.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                return null;
            }

            return method.GetValue(o) as T;
        }

        private static bool? TryGetHasSuccessfullyLoaded(Project project, CancellationToken cancellationToken)
        {
            // This method has not been made a public API, but is useful for analyzing some issues
            var task = project.TryCallNonPublicMethod<Task<bool>>("HasSuccessfullyLoadedAsync", cancellationToken);

            if (task == null)
            {
                return null;
            }

            task.Wait(cancellationToken);

            return task.Result;
        }

        private static VSLangProj.VSProject? TryFindLangProjProject(EnvDTE.DTE dte, Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dteProject = dte.Solution.Projects.Cast<EnvDTE.Project>().FirstOrDefault(
                p =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    try
                    {
                        return string.Equals(p.FullName, project.FilePath, StringComparison.OrdinalIgnoreCase);
                    }
                    catch (NotImplementedException)
                    {
                        // Some EnvDTE.Projects will throw on p.FullName, so just bail in that case.
                        return false;
                    }
                });

            return dteProject?.Object as VSLangProj.VSProject;
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

        public static IEnumerable<XElement> CreateElementsForDocumentCollection(IEnumerable<TextDocument> documents, string elementName, CancellationToken cancellationToken)
        {
            foreach (var document in documents)
            {
                var documentElement = new XElement(elementName, new XAttribute("path", SanitizePath(document.FilePath ?? "(none)")));

                var clientName = document.TryGetNonPublicPropertyFromService<string>("Microsoft.CodeAnalysis.Host.DocumentPropertiesService", "DiagnosticsLspClientName");
                if (clientName != null)
                {
                    documentElement.SetAttributeValue("clientName", clientName);
                }

                var documentState = document.TryGetNonPublicProperty<object>("State");
                if (documentState != null)
                {
                    var loadDiagnosticTask = documentState.TryCallNonPublicMethod<Task<Diagnostic>>("GetLoadDiagnosticAsync", cancellationToken);

                    if (loadDiagnosticTask != null)
                    {
                        loadDiagnosticTask.Wait(cancellationToken);

                        if (loadDiagnosticTask.Result != null)
                        {
                            documentElement.Add(new XElement("loadDiagnostic", loadDiagnosticTask.Result.GetMessage()));
                        }
                    }
                }

                yield return documentElement;
            }
        }

        private sealed class ThreadedWaitCallback : IVsThreadedWaitDialogCallback
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new();

            public CancellationToken CancellationToken
            {
                get { return _cancellationTokenSource.Token; }
            }

            public void OnCanceled()
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}