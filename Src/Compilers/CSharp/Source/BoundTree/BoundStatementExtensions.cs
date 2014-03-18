// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class BoundStatementExtensions
    {
        [Conditional("DEBUG")]
        internal static void AssertIsLabeledStatement(this BoundStatement node)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.Kind == BoundKind.LabelStatement || node.Kind == BoundKind.LabeledStatement || node.Kind == BoundKind.SwitchSection);
        }


        [Conditional("DEBUG")]
        internal static void AssertIsLabeledStatementWithLabel(this BoundStatement node, LabelSymbol label)
        {
            Debug.Assert(node != null);

            switch (node.Kind)
            {
                case BoundKind.LabelStatement:
                    Debug.Assert(((BoundLabelStatement)node).Label == label);
                    break;

                case BoundKind.LabeledStatement:
                    Debug.Assert(((BoundLabeledStatement)node).Label == label);
                    break;

                case BoundKind.SwitchSection:
                    foreach (var boundSwitchLabel in ((BoundSwitchSection)node).BoundSwitchLabels)
                    {
                        if (boundSwitchLabel.Label == label)
                        {
                            return;
                        }
                    }
                    Debug.Assert(false);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }
    }
}