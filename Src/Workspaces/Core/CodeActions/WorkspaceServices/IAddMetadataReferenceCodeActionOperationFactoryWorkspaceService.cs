using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Services.WorkspaceServices;

namespace Roslyn.Services.CodeActions.WorkspaceServices
{
    public interface IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService : IWorkspaceService
    {
        ICodeActionOperation CreateAddMetadataReferenceOperation(ProjectId projectId, AssemblyIdentity assemblyIdentity);
    }
}
