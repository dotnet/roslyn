// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class SignatureHelpItemEventArgs : EventArgs
    {
        public SignatureHelpItem SignatureHelpItem { get; }

        public SignatureHelpItemEventArgs(SignatureHelpItem signatureHelpItem)
        {
            this.SignatureHelpItem = signatureHelpItem;
        }
    }
}
