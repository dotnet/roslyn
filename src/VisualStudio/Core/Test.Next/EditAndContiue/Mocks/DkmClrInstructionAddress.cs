// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
