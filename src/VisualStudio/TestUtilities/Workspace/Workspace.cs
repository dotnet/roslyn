// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class Workspace
    {
        private readonly VisualStudioInstance _visualStudioInstance;
        private readonly WorkspaceWrapper _workspaceWrapper;

        internal Workspace(VisualStudioInstance visualStudioInstance)
        {
            _visualStudioInstance = visualStudioInstance;

            var integrationService = _visualStudioInstance.IntegrationService;
            _workspaceWrapper = integrationService.Execute<WorkspaceWrapper>(typeof(WorkspaceWrapper), nameof(WorkspaceWrapper.Create));
        }

        public bool UseSuggestionMode
        {
            get
            {
                return _workspaceWrapper.UseSuggestionMode;
            }

            set
            {
                _workspaceWrapper.UseSuggestionMode = value;
            }
        }
    }
}
