// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices.ProjectInfoService;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectInfoService
{
    [ExportWorkspaceServiceFactory(typeof(IProjectInfoService), ServiceLayer.Editor), Shared]
    internal sealed class DefaultProjectInfoServiceFactory : IWorkspaceServiceFactory
    {
        private readonly Lazy<IProjectInfoService> _singleton =
            new(() => new DefaultProjectInfoService());

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultProjectInfoServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _singleton.Value;
    }
}
