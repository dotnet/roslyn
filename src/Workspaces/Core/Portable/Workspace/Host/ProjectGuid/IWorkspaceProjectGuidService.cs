using System;

namespace Microsoft.CodeAnalysis.Host
{
    interface IWorkspaceProjectGuidService : IWorkspaceService
    {
        Guid GetProjectGuid(Project project);
    }
}
