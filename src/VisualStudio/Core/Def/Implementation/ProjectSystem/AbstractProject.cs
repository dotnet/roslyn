// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
    internal abstract partial class AbstractProject : ForegroundThreadAffinitizedObject, IVisualStudioHostProject
    {
        private string _displayName;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;

        public AbstractProject(
            VisualStudioProjectTracker projectTracker,
            string projectSystemName,
            string projectFilePath,
            IVsHierarchy hierarchy,
            string language,
            Guid projectGuid,
            VisualStudioWorkspaceImpl workspace)
            : base(projectTracker.ThreadingContext)
        {
            Hierarchy = hierarchy;
            Guid = projectGuid;
            Language = language;
            ProjectTracker = projectTracker;
            _visualStudioWorkspace = workspace;

            if (File.Exists(projectFilePath))
            {
                ProjectFilePath = projectFilePath;
            }

            ProjectSystemName = projectSystemName;
            DisplayName = hierarchy != null && hierarchy.TryGetName(out var name) ? name : projectSystemName;
        }

        // back-compat stub
        public AbstractProject(
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            string projectFilePath,
            IVsHierarchy hierarchy,
            string language,
            Guid projectGuid,
            IServiceProvider serviceProviderNotUsed,
            VisualStudioWorkspaceImpl workspace,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
            ICommandLineParserService commandLineParserServiceOpt = null)
            : this(projectTracker,
                   projectSystemName,
                   projectFilePath,
                   hierarchy,
                   language,
                   projectGuid,
                   workspace)
        {
        }

        public virtual ProjectId Id => VisualStudioProject?.Id ?? ExplicitId;

        internal ProjectId ExplicitId { get; set; }

        public string Language { get; }
        public VisualStudioProjectTracker ProjectTracker { get; }

        /// <summary>
        /// The <see cref="IVsHierarchy"/> for this project.  NOTE: May be null in Deferred Project Load cases.
        /// </summary>
        public IVsHierarchy Hierarchy { get; }

        /// <summary>
        /// Guid of the project
        /// 
        /// it is not readonly since it can be changed while loading project
        /// </summary>
        public Guid Guid { get; protected set; }

        /// <summary>
        /// The full path of the project file. Null if none exists (consider Venus.)
        /// Note that the project file path might change with project file rename.
        /// </summary>
        public string ProjectFilePath { get; private set; }

        /// <summary>
        /// The public display name of the project. This name is not unique and may be shared
        /// between multiple projects, especially in cases like Venus where the intellisense
        /// projects will match the name of their logical parent project.
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;

                UpdateVisualStudioProjectProperties();
            }
        }

        internal string AssemblyName { get; private set; }

        /// <summary>
        /// The name of the project according to the project system. In "regular" projects this is
        /// equivalent to <see cref="DisplayName"/>, but in Venus cases these will differ. The
        /// ProjectSystemName is the 2_Default.aspx project name, whereas the regular display name
        /// matches the display name of the project the user actually sees in the solution explorer.
        /// These can be assumed to be unique within the Visual Studio workspace.
        /// </summary>
        public string ProjectSystemName { get; }

#nullable enable

        public VisualStudioProject? VisualStudioProject { get; internal set; }

#nullable restore

        internal void UpdateVisualStudioProjectProperties()
        {
            if (VisualStudioProject != null)
            {
                VisualStudioProject.DisplayName = this.DisplayName;
            }
        }

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        protected void UpdateProjectDisplayName(string displayName)
            => this.DisplayName = displayName;

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal void AddDocument(IVisualStudioHostDocument document, bool isCurrentContext, bool hookupHandlers)
        {
            var shimDocument = (DocumentProvider.ShimDocument)document;

            VisualStudioProject.AddSourceFile(shimDocument.FilePath, shimDocument.SourceCodeKind);
        }

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal void RemoveDocument(IVisualStudioHostDocument document)
        {
            var containedDocument = ContainedDocument.TryGetContainedDocument(document.Id);
            if (containedDocument != null)
            {
                VisualStudioProject.RemoveSourceTextContainer(containedDocument.SubjectBuffer.AsTextContainer());
                containedDocument.Dispose();
            }
            else
            {
                var shimDocument = (DocumentProvider.ShimDocument)document;
                VisualStudioProject.RemoveSourceFile(shimDocument.FilePath);
            }
        }

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal IVisualStudioHostDocument GetCurrentDocumentFromPath(string filePath)
        {
            var id = _visualStudioWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault(d => d.ProjectId == Id);

            if (id != null)
            {
                return new DocumentProvider.ShimDocument(this, id, filePath);
            }
            else
            {
                return null;
            }
        }

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal ImmutableArray<IVisualStudioHostDocument> GetCurrentDocuments()
        {
            return _visualStudioWorkspace.CurrentSolution.GetProject(Id).Documents.SelectAsArray(
                d => (IVisualStudioHostDocument)new DocumentProvider.ShimDocument(this, d.Id, d.FilePath, d.SourceCodeKind));
        }
    }
}
