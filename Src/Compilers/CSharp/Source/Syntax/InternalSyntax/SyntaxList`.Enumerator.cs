// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial struct SyntaxList<TNode> where TNode : CSharpSyntaxNode
    {
        internal struct Enumerator
        {
            private SyntaxList<TNode> list;
            private int index;

            internal Enumerator(SyntaxList<TNode> list)
            {
                this.list = list;
                this.index = -1;
            }

            public bool MoveNext()
            {
                var newIndex = this.index + 1;
                if (newIndex < this.list.Count)
                {
                    this.index = newIndex;
                    return true;
                }

                return false;
            }

            public TNode Current
            {
                get
                {
                    return this.list[this.index];
                }
            }
        }
    }
}