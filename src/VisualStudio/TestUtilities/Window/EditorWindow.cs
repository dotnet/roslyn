// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class EditorWindow
    {
        private IntegrationHost _host;

        internal EditorWindow(IntegrationHost host)
        {
            _host = host;
        }

        public string Text
        {
            get
            {
                return RemotingHelper.GetActiveTextViewContents();
            }

            set
            {
                RemotingHelper.SetActiveTextViewContents(value);
            }
        }
    }
}
