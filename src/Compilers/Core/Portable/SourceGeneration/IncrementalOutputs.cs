// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An opaque type that represents an output from an incremental generator
    /// </summary>
    /// <remarks>
    /// This isn't created directly by the user, but from the result of calls to extension methods,
    /// which is then passed back to the <see cref="IncrementalGeneratorPipelineContext"/>
    /// </remarks>
    public struct IncrementalGeneratorOutput
    {
        internal readonly IOutputNode node;

        internal IncrementalGeneratorOutput(IOutputNode node)
        {
            this.node = node;
        }
    }

    /// <summary>
    /// Internal representation of an incremental output
    /// </summary>
    internal interface IOutputNode
    {
    }
}
