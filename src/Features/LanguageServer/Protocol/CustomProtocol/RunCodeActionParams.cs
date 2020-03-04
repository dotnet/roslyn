// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.CustomProtocol
{
    /// <summary>
    /// Params of a RunCodeAction command that is returned by the GetCodeActionsHandler.
    /// Unfortunately, while the client does not use these params, it gets parsed on the client side.
    /// Therefore this type must match the client's type.
    /// </summary>
    internal class RunCodeActionParams
    {
        /// <summary>
        /// Params that were passed to originally get a list of codeactions.
        /// </summary>
        public CodeActionParams CodeActionParams { get; set; }

        /// <summary>
        /// Title of the action to execute.
        /// </summary>
        public string Title { get; set; }
    }
}
