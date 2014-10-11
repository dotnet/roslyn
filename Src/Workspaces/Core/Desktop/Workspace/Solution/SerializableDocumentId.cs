using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    [Serializable]
    public sealed class SerializableDocumentId
    {
        private readonly SerializableProjectId projectId;
        private readonly Guid guid;
        private readonly string debugName;

        public SerializableDocumentId(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            this.projectId = new SerializableProjectId(documentId.ProjectId);
            this.guid = documentId.Id;
            this.debugName = documentId.DebugName;
        }

        public DocumentId DocumentId
        {
            get
            {
                return new DocumentId(projectId.ProjectId, guid, debugName);
            }
        }
    }
}
