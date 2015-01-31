// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    public class CodeFix
    {
        public CodeFixProvider Provider { get; private set; }
        public TextSpan TextSpan { get; private set; }
        public IEnumerable<ICodeAction> Actions { get; private set; }

        public CodeFix(CodeFixProvider provider, TextSpan span, IEnumerable<ICodeAction> codeActions)
        {
            this.Provider = provider;
            this.TextSpan = span;
            this.Actions = codeActions;
        }
    }
}
