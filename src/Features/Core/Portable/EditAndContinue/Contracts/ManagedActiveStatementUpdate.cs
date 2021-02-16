// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    [DataContract]
    internal readonly struct ManagedActiveStatementUpdate
    {
        [DataMember(Order = 0)]
        public readonly ManagedModuleMethodId Method;

        [DataMember(Order = 1)]
        public readonly int ILOffset;

        [DataMember(Order = 2)]
        public readonly SourceSpan NewSpan;

        public ManagedActiveStatementUpdate(ManagedModuleMethodId method, int ilOffset, SourceSpan newSpan)
        {
            Method = method;
            ILOffset = ilOffset;
            NewSpan = newSpan;
        }
    }
}
