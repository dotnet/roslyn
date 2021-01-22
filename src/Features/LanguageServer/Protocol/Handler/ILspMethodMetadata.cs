// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface ILspMethodMetadata
    {
        /// <summary>
        /// Name of the LSP method to handle.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Whether or not handling this method results in changes to the current solution state.
        /// Mutating requests will block all subsequent requests from starting until after they have
        /// completed and mutations have been applied. See <see cref="RequestExecutionQueue"/>.
        /// </summary>
        public bool MutatesSolutionState { get; }

        /// <summary>
        /// If the <see cref="MethodName"/> is <see cref="Methods.WorkspaceExecuteCommandName"/>
        /// then this is the name of the command to execute.
        /// </summary>
        public string? CommandName { get; }
    }
}
