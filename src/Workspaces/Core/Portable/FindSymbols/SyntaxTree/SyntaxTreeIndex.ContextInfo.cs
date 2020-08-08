// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIndex
    {
        private readonly struct ContextInfo
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
                bool containsIndexerMemberCref,
                bool containsDeconstruction,
                bool containsAwait,
                bool containsTupleExpressionOrTupleType,
                bool containsImplicitObjectCreation,
                bool containsGlobalAttributes)
                : this(predefinedTypes, predefinedOperators,
                       ConvertToContainingNodeFlag(
                         containsForEachStatement,
                         containsLockStatement,
                         containsUsingStatement,
                         containsQueryExpression,
                         containsThisConstructorInitializer,
                         containsBaseConstructorInitializer,
                         containsElementAccessExpression,
                         containsIndexerMemberCref,
                         containsDeconstruction,
                         containsAwait,
                         containsTupleExpressionOrTupleType,
                         containsImplicitObjectCreation,
                         containsGlobalAttributes))
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
                bool containsIndexerMemberCref,
                bool containsDeconstruction,
                bool containsAwait,
                bool containsTupleExpressionOrTupleType,
                bool containsImplicitObjectCreation,
                bool containsGlobalAttributes)
            {
                var containingNodes = ContainingNodes.None;

                containingNodes |= containsForEachStatement ? ContainingNodes.ContainsForEachStatement : 0;
                containingNodes |= containsLockStatement ? ContainingNodes.ContainsLockStatement : 0;
                containingNodes |= containsUsingStatement ? ContainingNodes.ContainsUsingStatement : 0;
                containingNodes |= containsQueryExpression ? ContainingNodes.ContainsQueryExpression : 0;
                containingNodes |= containsThisConstructorInitializer ? ContainingNodes.ContainsThisConstructorInitializer : 0;
                containingNodes |= containsBaseConstructorInitializer ? ContainingNodes.ContainsBaseConstructorInitializer : 0;
                containingNodes |= containsElementAccessExpression ? ContainingNodes.ContainsElementAccessExpression : 0;
                containingNodes |= containsIndexerMemberCref ? ContainingNodes.ContainsIndexerMemberCref : 0;
                containingNodes |= containsDeconstruction ? ContainingNodes.ContainsDeconstruction : 0;
                containingNodes |= containsAwait ? ContainingNodes.ContainsAwait : 0;
                containingNodes |= containsTupleExpressionOrTupleType ? ContainingNodes.ContainsTupleExpressionOrTupleType : 0;
                containingNodes |= containsImplicitObjectCreation ? ContainingNodes.ContainsImplicitObjectCreation : 0;
                containingNodes |= containsGlobalAttributes ? ContainingNodes.ContainsGlobalAttributes : 0;

                return containingNodes;
            }

            public bool ContainsPredefinedType(PredefinedType type)
                => (_predefinedTypes & (int)type) == (int)type;

            public bool ContainsPredefinedOperator(PredefinedOperator op)
                => (_predefinedOperators & (int)op) == (int)op;

            public bool ContainsForEachStatement
                => (_containingNodes & ContainingNodes.ContainsForEachStatement) == ContainingNodes.ContainsForEachStatement;

            public bool ContainsDeconstruction
                => (_containingNodes & ContainingNodes.ContainsDeconstruction) == ContainingNodes.ContainsDeconstruction;

            public bool ContainsAwait
                => (_containingNodes & ContainingNodes.ContainsAwait) == ContainingNodes.ContainsAwait;

            public bool ContainsImplicitObjectCreation
                => (_containingNodes & ContainingNodes.ContainsImplicitObjectCreation) == ContainingNodes.ContainsImplicitObjectCreation;

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

            public bool ContainsTupleExpressionOrTupleType
                => (_containingNodes & ContainingNodes.ContainsTupleExpressionOrTupleType) == ContainingNodes.ContainsTupleExpressionOrTupleType;

            public bool ContainsGlobalAttributes
                => (_containingNodes & ContainingNodes.ContainsGlobalAttributes) == ContainingNodes.ContainsGlobalAttributes;

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
                ContainsDeconstruction = 1 << 8,
                ContainsAwait = 1 << 9,
                ContainsTupleExpressionOrTupleType = 1 << 10,
                ContainsImplicitObjectCreation = 1 << 11,
                ContainsGlobalAttributes = 1 << 12,
            }
        }
    }
}
