#if false
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Host;
using Roslyn.Services.Shared.Collections;
using Roslyn.Services.Shared.Utilities;

namespace Roslyn.Services.CodeActions
{
    [ExportWorkspaceServiceFactory(typeof(ICodeActionsExtensionManager), WorkspaceKind.Any)]
    internal partial class ServicesLayerCodeActionsExtensionManager : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new CodeActionsExtensionManager(workspaceServices.GetService<IExtensionManager>());
        }
    }
}
#endif