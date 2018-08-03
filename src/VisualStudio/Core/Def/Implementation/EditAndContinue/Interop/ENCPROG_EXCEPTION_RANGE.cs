// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ENCPROG_EXCEPTION_RANGE
    {
        public int MethodToken;
        public int MethodVersion;
        public int StartLine;
        public int StartCol;
        public int EndLine;
        public int EndCol;
        public int Delta;
    }
}
