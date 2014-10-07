using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Roslyn.Services;
using Roslyn.Utilities;

namespace Roslyn.Services.Host
{
    public abstract partial class TrackingWorkspace : IWorkspaceVersion
    {
        public override IWorkspaceVersion Version
        {
            get { return this; }
        }

        private class SolutionVersion
        {
            internal DateTime LastModified;
            internal DateTime AnyProjectLastModified;
            internal DateTime AnyDocumentLastModified;
        }

        private class ProjectVersion
        {
            internal DateTime LastModified;
            internal DateTime AnyDocumentLastModified;
        }

        private class DocumentVersion
        {
            internal DateTime LastModified;
        }

        private SolutionVersion solutionModified;

        private readonly ConcurrentDictionary<ProjectId, ProjectVersion> projectVersions
            = new ConcurrentDictionary<ProjectId, ProjectVersion>();

        private readonly ConcurrentDictionary<DocumentId, DocumentVersion> documentVersions
            = new ConcurrentDictionary<DocumentId, DocumentVersion>();

        protected void SetSolutionLastModified(DateTime dateTime)
        {
            this.GetSolutionVersion().LastModified = dateTime;
        }

        protected void SetAnyProjectLastModified(DateTime dateTime)
        {
            this.GetSolutionVersion().AnyProjectLastModified = dateTime;
        }

        protected void SetAnyDocumentLastModified(DateTime dateTime)
        {
            this.GetSolutionVersion().AnyDocumentLastModified = dateTime;
        }

        protected void SetProjectLastModified(ProjectId projectId, DateTime dateTime)
        {
            this.GetProjectVersion(projectId).LastModified = dateTime;
            this.SetAnyProjectLastModified(dateTime);
        }

        protected void SetAnyDocumentLastModified(ProjectId projectId, DateTime dateTime)
        {
            this.GetProjectVersion(projectId).AnyDocumentLastModified = dateTime;
            this.SetAnyDocumentLastModified(dateTime);
        }

        protected void SetDocumentLastModified(DocumentId documentId, DateTime dateTime)
        {
            this.GetDocumentVersion(documentId).LastModified = dateTime;
            this.SetAnyDocumentLastModified(documentId.ProjectId, dateTime);
        }

        protected void ClearVersions()
        {
            using (this.stateLock.DisposableWrite())
            {
                this.ClearVersions_NoLock();
            }
        }

        private void ClearVersions_NoLock()
        {
            this.solutionModified = null;
            this.projectVersions.Clear();
            this.documentVersions.Clear();
        }

        private SolutionVersion GetSolutionVersion()
        {
            if (this.solutionModified == null)
            {
                this.solutionModified = LoadOrCreateSolutionVersion(this.CurrentSolution.Id);
            }

            return this.solutionModified;
        }

        private const string PersistenceName = "version";

        private SolutionVersion LoadOrCreateSolutionVersion(SolutionId solutionId)
        {
            SolutionVersion state;
            if (!this.persistenceService.TryLoad(solutionId, PersistenceName, ReadSolutionVersion, out state))
            {
                var now = DateTime.UtcNow;
                state = new SolutionVersion
                {
                    LastModified = now,
                    AnyProjectLastModified = now,
                    AnyDocumentLastModified = now
                };

                // write it out so next time it is the same
                SaveSolutionVersion(solutionId, state);
            }

            return state;
        }

        private void SaveSolutionVersion(SolutionId solutionId, SolutionVersion state)
        {
            this.persistenceService.TrySave(solutionId, PersistenceName, WriteSolutionVersion, state);
        }

        private const string SolutionElementName = "Solution";
        private const string LastModifiedElementName = "LastModified";
        private const string AnyProjectLastModifiedElementName = "ProjectLastModified";
        private const string AnyDocumentLastModifiedElementName = "DocumentLastModified";

        private static SolutionVersion ReadSolutionVersion(TextReader reader)
        {
            var doc = XDocument.Load(reader);
            var solutionElement = doc.Element(SolutionElementName);

            return new SolutionVersion
            {
                LastModified = XmlConvert.ToDateTime(solutionElement.Element(LastModifiedElementName).Value, XmlDateTimeSerializationMode.Local),
                AnyProjectLastModified = XmlConvert.ToDateTime(solutionElement.Element(AnyProjectLastModifiedElementName).Value, XmlDateTimeSerializationMode.Local),
                AnyDocumentLastModified = XmlConvert.ToDateTime(solutionElement.Element(AnyDocumentLastModifiedElementName).Value, XmlDateTimeSerializationMode.Local)
            };
        }

        private static void WriteSolutionVersion(TextWriter writer, SolutionVersion state)
        {
            using (var xmlWriter = new XmlTextWriter(writer))
            {
                xmlWriter.Formatting = System.Xml.Formatting.Indented;
                xmlWriter.Indentation = 2;

                xmlWriter.WriteStartElement(SolutionElementName);
                xmlWriter.WriteElementString(LastModifiedElementName, XmlConvert.ToString(state.LastModified, XmlDateTimeSerializationMode.Utc));
                xmlWriter.WriteElementString(AnyProjectLastModifiedElementName, XmlConvert.ToString(state.AnyProjectLastModified, XmlDateTimeSerializationMode.Utc));
                xmlWriter.WriteElementString(AnyDocumentLastModifiedElementName, XmlConvert.ToString(state.AnyDocumentLastModified, XmlDateTimeSerializationMode.Utc));
                xmlWriter.WriteEndElement();
            }
        }

        private ProjectVersion GetProjectVersion(ProjectId projectId)
        {
            return this.projectVersions.GetOrAdd(projectId, LoadOrCreateProjectVersion);
        }

        private ProjectVersion LoadOrCreateProjectVersion(ProjectId projectId)
        {
            ProjectVersion state;
            if (!this.persistenceService.TryLoad(projectId, PersistenceName, ReadProjectVersion, out state))
            {
                var now = DateTime.UtcNow;
                state = new ProjectVersion
                {
                    LastModified = now,
                    AnyDocumentLastModified = now
                };

                // write it out so next time it is the same
                SaveProjectVersion(projectId, state);
            }

            return state;
        }

        private void SaveProjectVersion(ProjectId projectId, ProjectVersion state)
        {
            this.persistenceService.TrySave(projectId, PersistenceName, WriteProjectVersion, state);
        }

        private const string ProjectElementName = "Project";

        private static ProjectVersion ReadProjectVersion(TextReader reader)
        {
            var doc = XDocument.Load(reader);
            var projectElement = doc.Element(ProjectElementName);
            return new ProjectVersion
            {
                LastModified = XmlConvert.ToDateTime(projectElement.Element(LastModifiedElementName).Value, XmlDateTimeSerializationMode.Local),
                AnyDocumentLastModified = XmlConvert.ToDateTime(projectElement.Element(AnyDocumentLastModifiedElementName).Value, XmlDateTimeSerializationMode.Local)
            };
        }

        private static void WriteProjectVersion(TextWriter writer, ProjectVersion state)
        {
            using (var xmlWriter = new XmlTextWriter(writer))
            {
                xmlWriter.Formatting = System.Xml.Formatting.Indented;
                xmlWriter.Indentation = 2;

                xmlWriter.WriteStartElement(ProjectElementName);
                xmlWriter.WriteElementString(LastModifiedElementName, XmlConvert.ToString(state.LastModified, XmlDateTimeSerializationMode.Utc));
                xmlWriter.WriteElementString(AnyDocumentLastModifiedElementName, XmlConvert.ToString(state.AnyDocumentLastModified, XmlDateTimeSerializationMode.Utc));
                xmlWriter.WriteEndElement();
            }
        }

        private DocumentVersion GetDocumentVersion(DocumentId documentId)
        {
            return this.documentVersions.GetOrAdd(documentId, CreateDocumentVersion);
        }

        private DocumentVersion CreateDocumentVersion(DocumentId documentId)
        {
            // try to get document last modified from file system
            var fileName = documentId.FilePath;
            try
            {
                if (File.Exists(fileName))
                {
                    var lastWrite = File.GetLastWriteTime(fileName);
                    return new DocumentVersion { LastModified = lastWrite };
                }
            }
            catch (Exception)
            {
            }

            return new DocumentVersion { LastModified = DateTime.UtcNow };
        }

        protected void SaveSolutionVersion()
        {
            SaveSolutionVersion(this.CurrentSolution.Id, this.GetSolutionVersion());
        }

        protected void SaveProjectVersion(ProjectId projectId)
        {
            SaveProjectVersion(projectId, this.GetProjectVersion(projectId));
            this.SaveSolutionVersion();
        }

        protected void SaveDocumentVersion(DocumentId documentId)
        {
            // no need to save document state, since it is obtained from the file system
            this.SaveProjectVersion(documentId.ProjectId);
        }

        public DateTime GetSolutionLastModified()
        {
            using (this.stateLock.DisposableWrite())
            {
                return this.GetSolutionVersion().LastModified;
            }
        }

        public DateTime GetProjectLastModified(ProjectId projectId)
        {
            using (this.stateLock.DisposableWrite())
            {
                return this.GetProjectVersion(projectId).LastModified;
            }
        }

        public DateTime GetDocumentLastModified(DocumentId documentId)
        {
            using (this.stateLock.DisposableWrite())
            {
                return this.GetDocumentVersion(documentId).LastModified;
            }
        }

        public DateTime GetAnyProjectLastModified()
        {
            using (this.stateLock.DisposableWrite())
            {
                return this.GetSolutionVersion().AnyProjectLastModified;
            }
        }

        public DateTime GetAnyDocumentLastModified()
        {
            using (this.stateLock.DisposableWrite())
            {
                return this.GetSolutionVersion().AnyDocumentLastModified;
            }
        }

        public DateTime GetAnyDocumentLastModified(ProjectId projectId)
        {
            using (this.stateLock.DisposableWrite())
            {
                return this.GetProjectVersion(projectId).AnyDocumentLastModified;
            }
        }
    }
}