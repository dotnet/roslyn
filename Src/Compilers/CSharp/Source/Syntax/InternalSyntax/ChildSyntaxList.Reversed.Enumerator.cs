// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial struct ChildSyntaxList
    {
        internal partial struct Reversed
        {
            internal struct Enumerator
            {
                private readonly GreenNode node;
                private int childIndex;
                private GreenNode list;
                private int listIndex;
                private GreenNode currentChild;

                internal Enumerator(GreenNode node)
                {
                    if (node != null)
                    {
                        this.node = node;
                        this.childIndex = node.SlotCount;
                        this.listIndex = -1;
                    }
                    else
                    {
                        this.node = null;
                        this.childIndex = 0;
                        this.listIndex = -1;
                    }

                    this.list = null;
                    this.currentChild = null;
                }

                public bool MoveNext()
                {
                    if (node != null)
                    {
                        if (this.list != null)
                        {
                            if (--this.listIndex >= 0)
                            {
                                this.currentChild = this.list.GetSlot(this.listIndex);
                                return true;
                            }

                            this.list = null;
                            this.listIndex = -1;
                        }

                        while (--this.childIndex >= 0)
                        {
                            var child = this.node.GetSlot(this.childIndex);
                            if (child == null)
                            {
                                continue;
                            }

                            if (child.IsList)
                            {
                                this.list = child;
                                this.listIndex = this.list.SlotCount;
                                if (--this.listIndex >= 0)
                                {
                                    this.currentChild = this.list.GetSlot(this.listIndex);
                                    return true;
                                }
                                else
                                {
                                    this.list = null;
                                    this.listIndex = -1;
                                    continue;
                                }
                            }
                            else
                            {
                                this.currentChild = child;
                            }

                            return true;
                        }
                    }

                    this.currentChild = null;
                    return false;
                }

                public GreenNode Current
                {
                    get { return this.currentChild; }
                }
            }
        }
    }
}