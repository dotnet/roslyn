// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly SerializableProjectId _projectId;
        private readonly Guid _guid;
        private readonly string _debugName;

        public SerializableDocumentId(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            _projectId = new SerializableProjectId(documentId.ProjectId);
            _guid = documentId.Id;
            _debugName = documentId.DebugName;
        }

        public DocumentId DocumentId
        {
            get
            {
                return new DocumentId(_projectId.ProjectId, _guid, _debugName);
            }
        }
    }
}
