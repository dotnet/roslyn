﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.EditAndContinue.Contracts
{
    /// <summary>
    /// Active statement affected by a managed update.
    /// This is used when remapping the instruction pointer to the appropriate location.
    /// </summary>
    [DataContract]
    internal readonly struct ManagedActiveStatementUpdate
    {
        /// <summary>
        /// Creates a ManagedActiveStatementUpdate.
        /// </summary>
        /// <param name="method">Method information before the change was made.</param>
        /// <param name="ilOffset">Old IL offset of the active statement.</param>
        /// <param name="newSpan">Updated text span for the active statement.</param>
        public ManagedActiveStatementUpdate(
            ManagedModuleMethodId method,
            int ilOffset,
            SourceSpan newSpan)
        {
            Method = method;
            ILOffset = ilOffset;
            NewSpan = newSpan;
        }

        /// <summary>
        /// Method ID. It has the token for the method that contains the active statement
        /// and the version when the change was made.
        /// </summary>
        [DataMember(Name = "method")]
        public ManagedModuleMethodId Method { get; }

        /// <summary>
        /// Old IL offset for the active statement.
        /// </summary>
        [DataMember(Name = "ilOffset")]
        public int ILOffset { get; }

        /// <summary>
        /// Updated text span for the active statement after the edit was made.
        /// </summary>
        [DataMember(Name = "newSpan")]
        public SourceSpan NewSpan { get; }
    }
}
