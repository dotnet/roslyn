// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementDebugInfo
    {
        public readonly string DocumentName;
        public readonly LinePositionSpan LinePositionSpan;
        public readonly ActiveStatementFlags Flags;
        public readonly ActiveInstructionId InstructionId;

        public ActiveStatementDebugInfo(
            ActiveInstructionId instructionId,
            string documentName, 
            LinePositionSpan linePositionSpan,
            ActiveStatementFlags flags)
        {
            InstructionId = instructionId;
            Flags = flags;
            DocumentName = documentName;
            LinePositionSpan = linePositionSpan;
        }
    }
}
