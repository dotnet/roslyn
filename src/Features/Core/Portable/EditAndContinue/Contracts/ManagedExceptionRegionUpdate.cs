// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    [DataContract]
    internal readonly struct ManagedExceptionRegionUpdate
    {
        [DataMember(Order = 0)]
        public readonly ManagedModuleMethodId Method;

        [DataMember(Order = 1)]
        public readonly int LineDelta;

        [DataMember(Order = 2)]
        public readonly SourceSpan NewSpan;

        public ManagedExceptionRegionUpdate(ManagedModuleMethodId method, int delta, SourceSpan newSpan)
        {
            Method = method;
            LineDelta = delta;
            NewSpan = newSpan;
        }
    }
}
