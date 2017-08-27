﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIndex
    {
        private struct ContextInfo
        {
            private readonly int _predefinedTypes;
            private readonly int _predefinedOperators;
            private readonly ContainingNodes _containingNodes;

            public ContextInfo(
                int predefinedTypes,
                int predefinedOperators,
                bool containsForEachStatement,
                bool containsLockStatement,
                bool containsUsingStatement,
                bool containsQueryExpression,
                bool containsThisConstructorInitializer,
                bool containsBaseConstructorInitializer,
                bool containsElementAccessExpression,
                bool containsIndexerMemberCref) :
                this(predefinedTypes, predefinedOperators,
                     ConvertToContainingNodeFlag(
                         containsForEachStatement,
                         containsLockStatement,
                         containsUsingStatement,
                         containsQueryExpression,
                         containsThisConstructorInitializer,
                         containsBaseConstructorInitializer,
                         containsElementAccessExpression,
                         containsIndexerMemberCref))
            {
            }

            private ContextInfo(int predefinedTypes, int predefinedOperators, ContainingNodes containingNodes)
            {
                _predefinedTypes = predefinedTypes;
                _predefinedOperators = predefinedOperators;
                _containingNodes = containingNodes;
            }

            private static ContainingNodes ConvertToContainingNodeFlag(
                bool containsForEachStatement,
                bool containsLockStatement,
                bool containsUsingStatement,
                bool containsQueryExpression,
                bool containsThisConstructorInitializer,
                bool containsBaseConstructorInitializer,
                bool containsElementAccessExpression,
                bool containsIndexerMemberCref)
            {
                var containingNodes = ContainingNodes.None;

                containingNodes = containsForEachStatement ? (containingNodes | ContainingNodes.ContainsForEachStatement) : containingNodes;
                containingNodes = containsLockStatement ? (containingNodes | ContainingNodes.ContainsLockStatement) : containingNodes;
                containingNodes = containsUsingStatement ? (containingNodes | ContainingNodes.ContainsUsingStatement) : containingNodes;
                containingNodes = containsQueryExpression ? (containingNodes | ContainingNodes.ContainsQueryExpression) : containingNodes;
                containingNodes = containsThisConstructorInitializer ? (containingNodes | ContainingNodes.ContainsThisConstructorInitializer) : containingNodes;
                containingNodes = containsBaseConstructorInitializer ? (containingNodes | ContainingNodes.ContainsBaseConstructorInitializer) : containingNodes;
                containingNodes = containsElementAccessExpression ? (containingNodes | ContainingNodes.ContainsElementAccessExpression) : containingNodes;
                containingNodes = containsIndexerMemberCref ? (containingNodes | ContainingNodes.ContainsIndexerMemberCref) : containingNodes;

                return containingNodes;
            }

            public bool ContainsPredefinedType(PredefinedType type)
                => (_predefinedTypes & (int)type) == (int)type;

            public bool ContainsPredefinedOperator(PredefinedOperator op)
                => (_predefinedOperators & (int)op) == (int)op;

            public bool ContainsForEachStatement
                => (_containingNodes & ContainingNodes.ContainsForEachStatement) == ContainingNodes.ContainsForEachStatement;

            public bool ContainsLockStatement
                => (_containingNodes & ContainingNodes.ContainsLockStatement) == ContainingNodes.ContainsLockStatement;

            public bool ContainsUsingStatement
                => (_containingNodes & ContainingNodes.ContainsUsingStatement) == ContainingNodes.ContainsUsingStatement;

            public bool ContainsQueryExpression
                => (_containingNodes & ContainingNodes.ContainsQueryExpression) == ContainingNodes.ContainsQueryExpression;

            public bool ContainsThisConstructorInitializer
                => (_containingNodes & ContainingNodes.ContainsThisConstructorInitializer) == ContainingNodes.ContainsThisConstructorInitializer;

            public bool ContainsBaseConstructorInitializer
                => (_containingNodes & ContainingNodes.ContainsBaseConstructorInitializer) == ContainingNodes.ContainsBaseConstructorInitializer;

            public bool ContainsElementAccessExpression
                => (_containingNodes & ContainingNodes.ContainsElementAccessExpression) == ContainingNodes.ContainsElementAccessExpression;

            public bool ContainsIndexerMemberCref
                => (_containingNodes & ContainingNodes.ContainsIndexerMemberCref) == ContainingNodes.ContainsIndexerMemberCref;

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(_predefinedTypes);
                writer.WriteInt32(_predefinedOperators);
                writer.WriteInt32((int)_containingNodes);
            }

            public static ContextInfo? TryReadFrom(ObjectReader reader)
            {
                try
                {
                    var predefinedTypes = reader.ReadInt32();
                    var predefinedOperators = reader.ReadInt32();
                    var containingNodes = (ContainingNodes)reader.ReadInt32();

                    return new ContextInfo(predefinedTypes, predefinedOperators, containingNodes);
                }
                catch (Exception)
                {
                }

                return null;
            }

            [Flags]
            private enum ContainingNodes
            {
                None = 0,
                ContainsForEachStatement = 1,
                ContainsLockStatement = 1 << 1,
                ContainsUsingStatement = 1 << 2,
                ContainsQueryExpression = 1 << 3,
                ContainsThisConstructorInitializer = 1 << 4,
                ContainsBaseConstructorInitializer = 1 << 5,
                ContainsElementAccessExpression = 1 << 6,
                ContainsIndexerMemberCref = 1 << 7,
            }
        }
    }
}
