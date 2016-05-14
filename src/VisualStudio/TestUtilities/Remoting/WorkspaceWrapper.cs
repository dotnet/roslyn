// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices;

namespace Roslyn.VisualStudio.Test.Utilities.Remoting
{
    internal class WorkspaceWrapper : MarshalByRefObject
    {
        private readonly VisualStudioWorkspace _workspace;

        public static WorkspaceWrapper Create()
        {
            var visualStudioWorkspace = RemotingHelper.VisualStudioWorkspace;
            return new WorkspaceWrapper(visualStudioWorkspace);
        }

        private WorkspaceWrapper(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
        }

        public bool UseSuggestionMode
        {
            get
            {
                return Options.GetOption(EditorCompletionOptions.UseSuggestionMode);
            }

            set
            {
                if (value != UseSuggestionMode)
                {
                    RemotingHelper.DTE.ExecuteCommandAsync("Edit.ToggleCompletionMode").GetAwaiter().GetResult();
                }
            }
        }

        private OptionSet Options
        {
            get
            {
                return _workspace.Options;
            }

            set
            {
                _workspace.Options = value;
            }
        }
    }
}
