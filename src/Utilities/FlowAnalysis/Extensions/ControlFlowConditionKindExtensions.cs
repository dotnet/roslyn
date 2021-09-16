// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
