// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal partial class ILBuilder
    {
        /// <summary>
        /// Abstract Execution state. 
        /// If we know something interesting about IL stream we put it here.
        /// </summary>
        private struct EmitState
        {
            private int maxStack;
            private int curStack;
            private int instructionsEmitted;

            internal int InstructionsEmitted
            {
                get
                {
                    return instructionsEmitted;
                }
            }

            internal void InstructionAdded()
            {
                instructionsEmitted += 1;
            }

            /// <summary>
            /// Eval stack's high watermark.
            /// </summary>
            internal int MaxStack
            {
                get
                {
                    return maxStack;
                }
                private set
                {
                    Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                    maxStack = value;
                }
            }

            /// <summary>
            /// Current evaluation stack depth.
            /// </summary>
            internal int CurStack
            {
                get
                {
                    return curStack;
                }
                private set
                {
                    Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                    curStack = value;
                }
            }

            //TODO: for debugging we could also record what we have in the stack (I, F, O, &, ...)

            /// <summary>
            /// Record effects of that currently emitted instruction on the eval stack.
            /// </summary>
            internal void AdjustStack(int count)
            {
                CurStack += count;
                MaxStack = Math.Max(MaxStack, CurStack);
            }
        }
    }
}
