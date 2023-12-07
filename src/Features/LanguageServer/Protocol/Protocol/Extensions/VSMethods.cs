// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// <see cref="VSMethods"/> contains the string values for all Language Server Protocol Visual Studio specific methods.
    /// </summary>
    internal static class VSMethods
    {
        /// <summary>
        /// Method name for 'textDocument/_vs_getProjectContexts'.
        /// The 'textDocument/_vs_getProjectContexts' request is sent from the client to the server to query
        /// the list of project context associated with a document.
        /// This method has a parameter of type <see cref="VSGetProjectContextsParams" /> and a return value of type
        /// <see cref="VSProjectContextList" />.
        /// In order to enable the client to send the 'textDocument/_vs_getProjectContexts' requests, the server must
        /// set the <see cref="VSServerCapabilities.ProjectContextProvider"/> property.
        /// </summary>
        public const string GetProjectContextsName = "textDocument/_vs_getProjectContexts";

        /// <summary>
        /// Strongly typed request object for 'textDocument/_vs_getProjectContexts'.
        /// </summary>
        public static readonly LspRequest<VSGetProjectContextsParams, VSProjectContextList> GetProjectContexts = new LspRequest<VSGetProjectContextsParams, VSProjectContextList>(GetProjectContextsName);
    }
}
