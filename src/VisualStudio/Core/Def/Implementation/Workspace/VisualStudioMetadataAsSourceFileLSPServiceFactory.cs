// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IMetadataAsSourceFileLSPService), ServiceLayer.Host), Shared]
    internal class VisualStudioMetadataAsSourceFileLSPServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IMetadataAsSourceFileLSPService _singleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioMetadataAsSourceFileLSPServiceFactory(
            SVsServiceProvider serviceProvider)
        {
            _singleton = new VisualStudioMetadataAsSourceFileLSPService(serviceProvider);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _singleton;
    }
}
