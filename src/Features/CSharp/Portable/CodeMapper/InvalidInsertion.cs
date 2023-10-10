// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

internal sealed partial class CSharpCodeMapper
{
    /// <summary>
    /// Represents an invalid insertion operation.
    /// </summary>
    public record InvalidInsertion
    {
        /// <summary>
        /// The invalid source node.
        /// </summary>
        public readonly CSharpSourceNode InsertNode;

        /// <summary>
        /// The reason why the insertion operation is invalid.
        /// </summary>
        public readonly InvalidInsertionReason Reason;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidInsertion"/> class.
        /// </summary>
        /// <param name="insertNode">The source node to insert.</param>
        /// <param name="reason">The reason why the insertion operation is invalid.</param>
        public InvalidInsertion(CSharpSourceNode insertNode, InvalidInsertionReason reason)
        {
            InsertNode = insertNode;
            Reason = reason;
        }
    }
}
