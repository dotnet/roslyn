// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Debugger.Clr;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests.EditAndContinue
{
    public class DkmClrInstructionAddress
    {
        public DkmClrMethodId MethodId { get; set; }
        public uint ILOffset { get; set; }
        public DkmClrModuleInstance ModuleInstance { get; set; }
    }
}
