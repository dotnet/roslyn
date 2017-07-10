// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class PrimaryWorkspace
    {
        private static readonly ReaderWriterLockSlim s_registryGate = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static Workspace s_primaryWorkspace;

        /// <summary>
        /// The primary workspace, usually set by the host environment.
        /// </summary>
        public static Workspace Workspace
        {
            get
            {
                using (s_registryGate.DisposableRead())
                {
                    return s_primaryWorkspace;
                }
            }
        }

        /// <summary>
        /// Register a workspace as the primary workspace. Only one workspace can be the primary.
        /// </summary>
        public static void Register(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            using (s_registryGate.DisposableWrite())
            {
                s_primaryWorkspace = workspace;
            }
        }
    }
}
