// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Samples.CodeAction.CopyPasteWithUsing
{
    [Export, Shared]
    internal class CopyDataService 
    {
        private readonly object syncLock = new object();
        private CopyData data;

        [ImportingConstructor]
        public CopyDataService()
        {
            this.data = null;

#if false
            Workspace.PrimaryWorkspace.WorkspaceChanged += (sender, args) =>
            {
                SaveData(null);
            };
#endif
        }

        public void SaveData(CopyData data)
        {
            lock (this.syncLock)
            {
                this.data = data;
            }
        }

        public CopyData Data
        {
            get
            {
                lock (this.syncLock)
                {
                    return this.data;
                }
            }
        }
    }
}
