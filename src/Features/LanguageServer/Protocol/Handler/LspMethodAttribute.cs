// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines a metadata attribute for <see cref="IRequestHandler{RequestType, ResponseType}"/>
    /// to use to specify the kind of LSP request the handler accepts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class LspMethodAttribute : Attribute, ILspMethodMetadata
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

        public LspMethodAttribute(string methodName, bool mutatesSolutionState)
        {
            MethodName = methodName;
            MutatesSolutionState = mutatesSolutionState;
        }

        public static ILspMethodMetadata GetLspMethodMetadata(Type instance)
        {
            var attribute = (ILspMethodMetadata?)Attribute.GetCustomAttribute(instance, typeof(LspMethodAttribute));
            Contract.ThrowIfNull(attribute, $"Handler {instance} does not declare an [LspMethod] attribute");
            return attribute;
        }
    }
}
