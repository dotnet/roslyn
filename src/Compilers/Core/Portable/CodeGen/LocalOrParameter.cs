// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeGen
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal struct LocalOrParameter
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
