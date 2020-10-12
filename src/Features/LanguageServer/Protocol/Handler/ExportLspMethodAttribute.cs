// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines an attribute for LSP request handlers to map to LSP methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportLspMethodAttribute : ExportAttribute, IRequestHandlerMetadata
    {
        public string MethodName { get; }

        public string? LanguageName { get; }

        /// <summary>
        /// Whether or not handling this method results in changes to the current solution state.
        /// Mutating requests will block all subsequent requests from starting until after they have
        /// completed and mutations have been applied. See <see cref="RequestExecutionQueue"/>.
        /// </summary>
        public bool MutatesSolutionState { get; }

        public ExportLspMethodAttribute(string methodName, bool mutatesSolutionState, string? languageName = null) : base(typeof(IRequestHandler))
        {
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException(nameof(methodName));
            }

            MethodName = methodName;
            MutatesSolutionState = mutatesSolutionState;
            LanguageName = languageName;
        }
    }
}
