// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial struct ChildSyntaxList
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
                this.node = node;
                this.childIndex = -1;
                this.listIndex = -1;
                this.list = null;
                this.currentChild = null;
            }

            public bool MoveNext()
            {
                if (node != null)
                {
                    if (this.list != null)
                    {
                        this.listIndex++;

                        if (this.listIndex < list.SlotCount)
                        {
                            this.currentChild = this.list.GetSlot(this.listIndex);
                            return true;
                        }

                        this.list = null;
                        this.listIndex = -1;
                    }

                    while (true)
                    {
                        this.childIndex++;

                        if (this.childIndex == node.SlotCount)
                        {
                            break;
                        }

                        var child = this.node.GetSlot(this.childIndex);
                        if (child == null)
                        {
                            continue;
                        }

                        if ((SyntaxKind)child.RawKind == SyntaxKind.List)
                        {
                            this.list = child;
                            this.listIndex++;

                            if (this.listIndex < this.list.SlotCount)
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