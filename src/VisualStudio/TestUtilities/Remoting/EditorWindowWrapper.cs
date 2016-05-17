// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.VisualStudio.Test.Utilities.Remoting
{
    internal class EditorWindowWrapper : MarshalByRefObject
    {
        public static EditorWindowWrapper Create() => new EditorWindowWrapper();

        private EditorWindowWrapper()
        {
        }

        public string Contents
        {
            get
            {
                return RemotingHelper.ActiveTextViewContents;
            }

            set
            {
                RemotingHelper.ActiveTextViewContents = value;
            }
        }
    }
}
