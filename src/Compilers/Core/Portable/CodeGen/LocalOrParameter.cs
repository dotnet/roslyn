// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeGen
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct LocalOrParameter
    {
        public readonly LocalDefinition? Local;
        public readonly int ParameterIndex;

        private LocalOrParameter(LocalDefinition? local, int parameterIndex)
        {
            this.Local = local;
            this.ParameterIndex = parameterIndex;
        }

        public static implicit operator LocalOrParameter(LocalDefinition? local)
        {
            return new LocalOrParameter(local, -1);
        }

        public static implicit operator LocalOrParameter(int parameterIndex)
        {
            return new LocalOrParameter(null, parameterIndex);
        }

        private string GetDebuggerDisplay()
        {
            return (Local != null) ? Local.GetDebuggerDisplay() : ParameterIndex.ToString();
        }
    }
}
