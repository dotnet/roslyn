// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [Export(typeof(PrimaryWorkspace)), Shared]
    internal sealed class PrimaryWorkspace
    {
        private readonly ReaderWriterLockSlim _registryGate = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private Workspace _primaryWorkspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PrimaryWorkspace()
        {
        }

        /// <summary>
        /// The primary workspace, usually set by the host environment.
        /// </summary>
        public Workspace Workspace
        {
            get
            {
                using (_registryGate.DisposableRead())
                {
                    return _primaryWorkspace;
                }
            }
        }

        /// <summary>
        /// Register a workspace as the primary workspace. Only one workspace can be the primary.
        /// </summary>
        public void Register(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            using (_registryGate.DisposableWrite())
            {
                _primaryWorkspace = workspace;
            }
        }
    }
}
