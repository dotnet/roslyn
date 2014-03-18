// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if MEF
using System.ComponentModel.Composition;
#endif
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !MEF
using Microsoft.CodeAnalysis.Composition;
#endif
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Options
{
#if MEF
    [ExportWorkspaceServiceFactory(typeof(IOptionService), WorkspaceKind.Any)]
#endif
    internal class OptionsServiceFactory : IWorkspaceServiceFactory
    {
#if MEF
        private IOptionService optionService;

        [ImportingConstructor]
        public OptionsServiceFactory(IOptionService optionService)
        {
            this.optionService = optionService;
        }

        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return optionService;
        }

#else
        private readonly ExportSource exports;

        public OptionsServiceFactory(ExportSource exports)
        {
            this.exports = exports;
        }

        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new OptionService(this.exports);
        }
#endif
    }
}
