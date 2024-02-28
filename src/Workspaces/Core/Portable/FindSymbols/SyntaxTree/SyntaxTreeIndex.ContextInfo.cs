// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

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
            bool containsExplicitOrImplicitElementAccessExpression,
            bool containsIndexerMemberCref,
            bool containsDeconstruction,
            bool containsAwait,
            bool containsTupleExpressionOrTupleType,
            bool containsImplicitObjectCreation,
            bool containsGlobalSuppressMessageAttribute,
            bool containsConversion,
            bool containsGlobalKeyword,
            bool containsCollectionInitializer)
            : this(predefinedTypes, predefinedOperators,
                   ConvertToContainingNodeFlag(
                     containsForEachStatement,
                     containsLockStatement,
                     containsUsingStatement,
                     containsQueryExpression,
                     containsThisConstructorInitializer,
                     containsBaseConstructorInitializer,
                     containsExplicitOrImplicitElementAccessExpression,
                     containsIndexerMemberCref,
                     containsDeconstruction,
                     containsAwait,
                     containsTupleExpressionOrTupleType,
                     containsImplicitObjectCreation,
                     containsGlobalSuppressMessageAttribute,
                     containsConversion,
                     containsGlobalKeyword,
                     containsCollectionInitializer))
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
            bool containsExplicitOrImplicitElementAccessExpression,
            bool containsIndexerMemberCref,
            bool containsDeconstruction,
            bool containsAwait,
            bool containsTupleExpressionOrTupleType,
            bool containsImplicitObjectCreation,
            bool containsGlobalSuppressMessageAttribute,
            bool containsConversion,
            bool containsGlobalKeyword,
            bool containsCollectionInitializer)
        {
            var containingNodes = ContainingNodes.None;

            containingNodes |= containsForEachStatement ? ContainingNodes.ContainsForEachStatement : 0;
            containingNodes |= containsLockStatement ? ContainingNodes.ContainsLockStatement : 0;
            containingNodes |= containsUsingStatement ? ContainingNodes.ContainsUsingStatement : 0;
            containingNodes |= containsQueryExpression ? ContainingNodes.ContainsQueryExpression : 0;
            containingNodes |= containsThisConstructorInitializer ? ContainingNodes.ContainsThisConstructorInitializer : 0;
            containingNodes |= containsBaseConstructorInitializer ? ContainingNodes.ContainsBaseConstructorInitializer : 0;
            containingNodes |= containsExplicitOrImplicitElementAccessExpression ? ContainingNodes.ContainsExplicitOrImplicitElementAccessExpression : 0;
            containingNodes |= containsIndexerMemberCref ? ContainingNodes.ContainsIndexerMemberCref : 0;
            containingNodes |= containsDeconstruction ? ContainingNodes.ContainsDeconstruction : 0;
            containingNodes |= containsAwait ? ContainingNodes.ContainsAwait : 0;
            containingNodes |= containsTupleExpressionOrTupleType ? ContainingNodes.ContainsTupleExpressionOrTupleType : 0;
            containingNodes |= containsImplicitObjectCreation ? ContainingNodes.ContainsImplicitObjectCreation : 0;
            containingNodes |= containsGlobalSuppressMessageAttribute ? ContainingNodes.ContainsGlobalSuppressMessageAttribute : 0;
            containingNodes |= containsConversion ? ContainingNodes.ContainsConversion : 0;
            containingNodes |= containsGlobalKeyword ? ContainingNodes.ContainsGlobalKeyword : 0;
            containingNodes |= containsCollectionInitializer ? ContainingNodes.ContainsCollectionInitializer : 0;

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

        public bool ContainsExplicitOrImplicitElementAccessExpression
            => (_containingNodes & ContainingNodes.ContainsExplicitOrImplicitElementAccessExpression) == ContainingNodes.ContainsExplicitOrImplicitElementAccessExpression;

        public bool ContainsIndexerMemberCref
            => (_containingNodes & ContainingNodes.ContainsIndexerMemberCref) == ContainingNodes.ContainsIndexerMemberCref;

        public bool ContainsTupleExpressionOrTupleType
            => (_containingNodes & ContainingNodes.ContainsTupleExpressionOrTupleType) == ContainingNodes.ContainsTupleExpressionOrTupleType;

        public bool ContainsGlobalKeyword
            => (_containingNodes & ContainingNodes.ContainsGlobalKeyword) == ContainingNodes.ContainsGlobalKeyword;

        public bool ContainsGlobalSuppressMessageAttribute
            => (_containingNodes & ContainingNodes.ContainsGlobalSuppressMessageAttribute) == ContainingNodes.ContainsGlobalSuppressMessageAttribute;

        public bool ContainsConversion
            => (_containingNodes & ContainingNodes.ContainsConversion) == ContainingNodes.ContainsConversion;

        public bool ContainsCollectionInitializer
            => (_containingNodes & ContainingNodes.ContainsCollectionInitializer) == ContainingNodes.ContainsCollectionInitializer;

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
            ContainsExplicitOrImplicitElementAccessExpression = 1 << 6,
            ContainsIndexerMemberCref = 1 << 7,
            ContainsDeconstruction = 1 << 8,
            ContainsAwait = 1 << 9,
            ContainsTupleExpressionOrTupleType = 1 << 10,
            ContainsImplicitObjectCreation = 1 << 11,
            ContainsGlobalSuppressMessageAttribute = 1 << 12,
            ContainsConversion = 1 << 13,
            ContainsGlobalKeyword = 1 << 14,
            ContainsCollectionInitializer = 1 << 15,
        }
    }
}
