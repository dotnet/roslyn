// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ENCPROG_ACTIVE_STATEMENT_REMAP
    {
        public Guid ThreadId;
        public int MethodToken;
        public int OldMethodVersion;
        public int OldILOffset;
        public int NewStartLine;
        public int NewStartCol;
        public int NewEndLine;
        public int NewEndCol;
    }
}
