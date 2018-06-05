using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Assets
{
    internal struct ProjectOrDocumentId : IObjectWritable
    {
        private object _id;

        public ProjectOrDocumentId(ProjectId projectId)
        {
            _id = projectId;
        }

        public ProjectOrDocumentId(DocumentId documentId)
        {
            _id = documentId;
        }

        public bool IsProject => _id is ProjectId;
        public bool IsDocument => _id is DocumentId;

        public static implicit operator ProjectId(ProjectOrDocumentId id)
        {
            return (ProjectId)id._id;
        }

        public static implicit operator DocumentId(ProjectOrDocumentId id)
        {
            return (DocumentId)id._id;
        }

        public static implicit operator ProjectOrDocumentId(ProjectId id)
        {
            return new ProjectOrDocumentId(id);
        }

        public static implicit operator ProjectOrDocumentId(DocumentId id)
        {
            return new ProjectOrDocumentId(id);
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteBoolean(IsProject);
            ((IObjectWritable)_id).WriteTo(writer);
        }

        public static ProjectOrDocumentId ReadFrom(ObjectReader reader)
        {
            var project = reader.ReadBoolean();
            if (project)
            {
                return new ProjectOrDocumentId(ProjectId.ReadFrom(reader));
            }

            return new ProjectOrDocumentId(DocumentId.ReadFrom(reader));
        }
    }
}
