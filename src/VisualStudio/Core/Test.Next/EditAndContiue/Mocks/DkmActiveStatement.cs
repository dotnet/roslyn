// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            InstructionAddress = new DkmClrInstructionAddress()
            {
                MethodId = new DkmClrMethodId(methodToken, methodVersion),
                ILOffset = ilOffset,
                ModuleInstance = new DkmClrModuleInstance() { Mvid = moduleId }
            };
            Thread = new DkmThread() { UniqueId = threadId };
        }
    }
}
