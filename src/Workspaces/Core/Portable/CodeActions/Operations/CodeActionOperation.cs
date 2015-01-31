// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Represents a single operation of a multi-operation code action.
    /// </summary>
    public abstract class CodeActionOperation
    {
        /// <summary>
        /// A short title describing of the effect of the operation.
        /// </summary>
        public virtual string Title
        {
            get { return null; }
        }

        /// <summary>
        /// Called by the host environment to apply the effect of the operation.
        /// This method is guaranteed to be called on the UI thread.
        /// </summary>
        public virtual void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
        }
    }
}
