// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    [Serializable]
    public sealed class SerializableProjectId
    {
        private readonly Guid _guid;
        private readonly string _debugName;

        public SerializableProjectId(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            _guid = projectId.Id;
            _debugName = projectId.DebugName;
        }

        public ProjectId ProjectId
        {
            get
            {
                return new ProjectId(_guid, _debugName);
            }
        }
    }
}
