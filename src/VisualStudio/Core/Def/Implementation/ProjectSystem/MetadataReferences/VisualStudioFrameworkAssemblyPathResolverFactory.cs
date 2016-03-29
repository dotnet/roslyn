// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
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

            public string ResolveAssemblyPath(
                ProjectId projectId,
                string assemblyName,
                string fullyQualifiedTypeName = null)
            {
                this.AssertIsForeground();

                var assembly = ResolveAssembly(projectId, assemblyName);
                if (assembly != null)
                {
                    // Codebase specifies where the assembly is on disk.  However, it's in 
                    // full URI format (i.e. file://c:/...). This will allow us to get the 
                    // actual local in the normal path format.
                    Uri uri;
                    if (Uri.TryCreate(assembly.CodeBase, UriKind.RelativeOrAbsolute, out uri) &&
                        this.CanResolveType(assembly, fullyQualifiedTypeName))
                    {
                        return uri.LocalPath;
                    }
                }

                return null;
            }

            private bool CanResolveType(Assembly assembly, string fullyQualifiedTypeName)
            {
                if (fullyQualifiedTypeName == null)
                {
                    // nothing to resolve.
                    return true;
                }

                // We only get a type name without generic indicators.  So try to few different
                // generic versions of the type name in case any of those hit.  it's highly 
                // unlikely we'd find something with more than 4 generic parameters, so only try
                // up that point.
                for (var i = 0; i < 5; i++)
                {
                    var name = i == 0
                        ? fullyQualifiedTypeName
                        : fullyQualifiedTypeName + "`" + i;

                    try
                    {
                        var type = assembly.GetType(name, throwOnError: false);
                        if (type != null)
                        {
                            return true;
                        }
                    }
                    catch (FileNotFoundException)
                    {
                    }
                    catch (FileLoadException)
                    {
                    }
                    catch (BadImageFormatException)
                    {
                    }
                }

                return false;
            }

            private Assembly ResolveAssembly(ProjectId projectId, string assemblyName)
            {
                this.AssertIsForeground();

                if (_workspace == null)
                {
                    return null;
                }

                IVsHierarchy hierarchy;
                string targetMoniker;
                if (!_workspace.TryGetHierarchy(projectId, out hierarchy) ||
                    !hierarchy.TryGetProperty((__VSHPROPID)__VSHPROPID4.VSHPROPID_TargetFrameworkMoniker, out targetMoniker) ||
                    targetMoniker == null)
                {
                    return null;
                }

                try
                {
                    // Below we use the DesignTimeAssemblyResolver functionality of VS to 
                    // determine if we can resolve the specified assembly name in the context
                    // of this project.  However, this service does not do the right thing
                    // in UWP apps.  Specifically, it *will* resolve the assembly to a 
                    // reference assembly, even though that's never what we want.  In order
                    // to deal with that, we put in this little check where we do not allow
                    // reference assembly resolution if the projects TargetFrameworkMoniker
                    // is ".NETCore, Version=5.0" or greater.

                    var frameworkName = new FrameworkName(targetMoniker);
                    if (StringComparer.OrdinalIgnoreCase.Equals(frameworkName.Identifier, ".NETCore") &&
                        frameworkName.Version >= new Version(major: 5, minor: 0))
                    {
                        return null;
                    }
                }
                catch (ArgumentException)
                {
                    // Something wrong with our TFM.  We don't have enough information to 
                    // properly resolve this assembly name.
                    return null;
                }

                try
                {
                    var frameworkProvider = new VsTargetFrameworkProvider(
                        _workspace.GetVsService<SVsFrameworkMultiTargeting, IVsFrameworkMultiTargeting>(),
                        targetMoniker,
                        _workspace.GetVsService<SVsSmartOpenScope, IVsSmartOpenScope>());
                    return frameworkProvider.GetReflectionAssembly(new AssemblyName(assemblyName));
                }
                catch (InvalidOperationException)
                {
                    // VsTargetFrameworkProvider throws InvalidOperationException in the 
                    // some cases (like when targetting packs are missing).  In that case
                    // we can't resolve this path.
                    return null;
                }
            }
        }
    }
}
