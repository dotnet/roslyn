// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.Design;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [ExportWorkspaceServiceFactory(typeof(IFrameworkAssemblyPathResolver), ServiceLayer.Host), Shared]
    internal sealed class VisualStudiorFrameworkAssemblyPathResolverFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices.Workspace as VisualStudioWorkspaceImpl);
        }

        private sealed class Service : ForegroundThreadAffinitizedObject, IFrameworkAssemblyPathResolver
        {
            private readonly VisualStudioWorkspaceImpl _workspace;

            public Service(VisualStudioWorkspaceImpl workspace)
                : base(assertIsForeground: false)
            {
                _workspace = workspace;
            }

            public string ResolveAssemblyPath(ProjectId projectId, string assemblyName)
            {
                this.AssertIsForeground();

                if (_workspace != null)
                {
                    IVsHierarchy hierarchy;
                    string targetMoniker;
                    if (_workspace.TryGetHierarchy(projectId, out hierarchy) &&
                        hierarchy.TryGetProperty((__VSHPROPID)__VSHPROPID4.VSHPROPID_TargetFrameworkMoniker, out targetMoniker) &&
                        targetMoniker != null)
                    {
                        try
                        {
                            var frameworkProvider = new VsTargetFrameworkProvider(
                                _workspace.GetVsService<SVsFrameworkMultiTargeting, IVsFrameworkMultiTargeting>(),
                                targetMoniker,
                                _workspace.GetVsService<SVsSmartOpenScope, IVsSmartOpenScope>());
                            var assembly = frameworkProvider.GetReflectionAssembly(new AssemblyName(assemblyName));

                            // Codebase specifies where the assembly is on disk.  However, it's in 
                            // full URI format (i.e. file://c:/...). This will allow us to get the 
                            // actual local in the normal path format.
                            return new Uri(assembly.CodeBase).LocalPath;
                        }
                        catch (InvalidOperationException)
                        {
                            // VsTargetFrameworkProvider throws InvalidOperationException in the 
                            // some cases (like when targetting packs are missing).  In that case
                            // we can't resolve this path.
                        }
                    }
                }

                return null;
            }
        }
    }
}
