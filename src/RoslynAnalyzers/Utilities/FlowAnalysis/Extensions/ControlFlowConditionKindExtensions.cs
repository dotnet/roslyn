// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal static class ControlFlowConditionKindExtensions
    {
        public static ControlFlowConditionKind Negate(this ControlFlowConditionKind controlFlowConditionKind)
        {
            switch (controlFlowConditionKind)
            {
                case ControlFlowConditionKind.WhenFalse:
                    return ControlFlowConditionKind.WhenTrue;

                case ControlFlowConditionKind.WhenTrue:
                    return ControlFlowConditionKind.WhenFalse;

                default:
                    Debug.Fail($"Unsupported conditional kind: '{controlFlowConditionKind}'");
                    return controlFlowConditionKind;
            }
        }
    }
}
