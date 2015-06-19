// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Log
{
    [ExportWorkspaceServiceFactory(typeof(IErrorLogger), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioErrorLoggerFactory : IWorkspaceServiceFactory
    {
        private IErrorLogger _singleton;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton ?? (_singleton = new VisualStudioErrorLogger());
        }
    }
}
