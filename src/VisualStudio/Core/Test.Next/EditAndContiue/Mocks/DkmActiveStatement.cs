// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Symbols;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests.EditAndContinue
{
    internal sealed class DkmActiveStatement
    {
        public DkmActiveStatementFlags Flags { get; set; }
        public DkmInstructionSymbol InstructionSymbol { get; set; }
        public DkmClrInstructionAddress InstructionAddress { get; set; }
        public uint ExecutingMethodVersion { get; set; }
        public DkmThread Thread { get; set; }

        public DkmActiveStatement(
            Guid threadId,
            Guid moduleId,
            int methodToken,
            uint methodVersion,
            uint ilOffset,
            DkmActiveStatementFlags flags)
        {
            Flags = flags;
            InstructionSymbol = new DkmInstructionSymbol() { Module = new DkmModule() { Id = new DkmModuleId(moduleId, default) } };
            InstructionAddress = new DkmClrInstructionAddress()
            {
                MethodId = new DkmClrMethodId(methodToken, methodVersion),
                ILOffset = ilOffset,
            };
            ExecutingMethodVersion = methodVersion;
            Thread = new DkmThread() { UniqueId = threadId };
        }
    }
}
