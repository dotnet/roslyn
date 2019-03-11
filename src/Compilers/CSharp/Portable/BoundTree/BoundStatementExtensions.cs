// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class BoundStatementExtensions
    {
        [Conditional("DEBUG")]
        internal static void AssertIsLabeledStatement(this BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundKind.LabelStatement:
                case BoundKind.LabeledStatement:
                case BoundKind.SwitchSection:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind);
            }
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
                    foreach (var boundSwitchLabel in ((BoundSwitchSection)node).SwitchLabels)
                    {
                        if (boundSwitchLabel.Label == label)
                        {
                            return;
                        }
                    }
                    throw ExceptionUtilities.Unreachable;

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind);
            }
        }
    }
}
