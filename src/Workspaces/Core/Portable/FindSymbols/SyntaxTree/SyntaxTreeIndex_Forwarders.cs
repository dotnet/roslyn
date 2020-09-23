// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex
    {
        public ImmutableArray<DeclaredSymbolInfo> DeclaredSymbolInfos => _declarationInfo.DeclaredSymbolInfos;

        public ImmutableDictionary<string, ImmutableArray<int>> ReceiverTypeNameToExtensionMethodMap
            => _extensionMethodInfo.ReceiverTypeNameToExtensionMethodMap;

        public bool ContainsExtensionMethod => _extensionMethodInfo.ContainsExtensionMethod;

        public bool ProbablyContainsIdentifier(string identifier) => _identifierInfo.ProbablyContainsIdentifier(identifier);
        public bool ProbablyContainsEscapedIdentifier(string identifier) => _identifierInfo.ProbablyContainsEscapedIdentifier(identifier);

        public bool ContainsPredefinedType(PredefinedType type) => _contextInfo.ContainsPredefinedType(type);
        public bool ContainsPredefinedOperator(PredefinedOperator op) => _contextInfo.ContainsPredefinedOperator(op);

        public bool ProbablyContainsStringValue(string value) => _literalInfo.ProbablyContainsStringValue(value);
        public bool ProbablyContainsInt64Value(long value) => _literalInfo.ProbablyContainsInt64Value(value);

        public bool ContainsForEachStatement => _contextInfo.ContainsForEachStatement;
        public bool ContainsDeconstruction => _contextInfo.ContainsDeconstruction;
        public bool ContainsAwait => _contextInfo.ContainsAwait;
        public bool ContainsImplicitObjectCreation => _contextInfo.ContainsImplicitObjectCreation;
        public bool ContainsLockStatement => _contextInfo.ContainsLockStatement;
        public bool ContainsUsingStatement => _contextInfo.ContainsUsingStatement;
        public bool ContainsQueryExpression => _contextInfo.ContainsQueryExpression;
        public bool ContainsThisConstructorInitializer => _contextInfo.ContainsThisConstructorInitializer;
        public bool ContainsBaseConstructorInitializer => _contextInfo.ContainsBaseConstructorInitializer;
        public bool ContainsElementAccessExpression => _contextInfo.ContainsElementAccessExpression;
        public bool ContainsIndexerMemberCref => _contextInfo.ContainsIndexerMemberCref;
        public bool ContainsTupleExpressionOrTupleType => _contextInfo.ContainsTupleExpressionOrTupleType;
        public bool ContainsGlobalAttributes => _contextInfo.ContainsGlobalAttributes;
    }
}
