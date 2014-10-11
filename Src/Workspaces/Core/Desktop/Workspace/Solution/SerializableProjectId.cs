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
        private readonly Guid guid;
        private readonly string debugName;

        public SerializableProjectId(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            guid = projectId.Id;
            debugName = projectId.DebugName;
        }

        public ProjectId ProjectId
        {
            get
            {
                return new ProjectId(guid, debugName);
            }
        }
    }
}
