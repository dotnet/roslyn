// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal class ChainedFormattingRules
    {
        private static readonly ConcurrentDictionary<(Type type, string name), Type> s_typeImplementingMethod = new ConcurrentDictionary<(Type type, string name), Type>();

        private readonly ImmutableArray<FormattingRule> _formattingRules;
        private readonly AnalyzerConfigOptions _options;

        private readonly ImmutableArray<FormattingRule> _addSuppressOperationsRules;
        private readonly ImmutableArray<FormattingRule> _addAnchorIndentationOperationsRules;
        private readonly ImmutableArray<FormattingRule> _addIndentBlockOperationsRules;
        private readonly ImmutableArray<FormattingRule> _addAlignTokensOperationsRules;
        private readonly ImmutableArray<FormattingRule> _getAdjustNewLinesOperationRules;
        private readonly ImmutableArray<FormattingRule> _getAdjustSpacesOperationRules;

        public ChainedFormattingRules(IEnumerable<FormattingRule> formattingRules, AnalyzerConfigOptions options)
        {
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(options);

            _formattingRules = formattingRules.ToImmutableArray();
            _options = options;

            _addSuppressOperationsRules = FilterToRulesImplementingMethod(_formattingRules, nameof(FormattingRule.AddSuppressOperations));
            _addAnchorIndentationOperationsRules = FilterToRulesImplementingMethod(_formattingRules, nameof(FormattingRule.AddAnchorIndentationOperations));
            _addIndentBlockOperationsRules = FilterToRulesImplementingMethod(_formattingRules, nameof(FormattingRule.AddIndentBlockOperations));
            _addAlignTokensOperationsRules = FilterToRulesImplementingMethod(_formattingRules, nameof(FormattingRule.AddAlignTokensOperations));
            _getAdjustNewLinesOperationRules = FilterToRulesImplementingMethod(_formattingRules, nameof(FormattingRule.GetAdjustNewLinesOperation));
            _getAdjustSpacesOperationRules = FilterToRulesImplementingMethod(_formattingRules, nameof(FormattingRule.GetAdjustSpacesOperation));
        }

        public void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode currentNode)
        {
            var action = new NextSuppressOperationAction(_addSuppressOperationsRules, index: 0, currentNode, _options, list);
            action.Invoke();
        }

        public void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode currentNode)
        {
            var action = new NextAnchorIndentationOperationAction(_addAnchorIndentationOperationsRules, index: 0, currentNode, _options, list);
            action.Invoke();
        }

        public void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode currentNode)
        {
            var action = new NextIndentBlockOperationAction(_addIndentBlockOperationsRules, index: 0, currentNode, _options, list);
            action.Invoke();
        }

        public void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode currentNode)
        {
            var action = new NextAlignTokensOperationAction(_addAlignTokensOperationsRules, index: 0, currentNode, _options, list);
            action.Invoke();
        }

        public AdjustNewLinesOperation? GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            var action = new NextGetAdjustNewLinesOperation(_getAdjustNewLinesOperationRules, index: 0, previousToken, currentToken, _options);
            return action.Invoke();
        }

        public AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            var action = new NextGetAdjustSpacesOperation(_getAdjustSpacesOperationRules, index: 0, previousToken, currentToken, _options);
            return action.Invoke();
        }

        private static ImmutableArray<FormattingRule> FilterToRulesImplementingMethod(ImmutableArray<FormattingRule> rules, string name)
        {
            return rules.Where(rule =>
            {
                var type = GetTypeImplementingMethod(rule, name);
                if (type == typeof(FormattingRule))
                {
                    return false;
                }

                if (type == typeof(CompatAbstractFormattingRule))
                {
                    type = GetTypeImplementingMethod(rule, name + "Slow");
                    if (type == typeof(CompatAbstractFormattingRule))
                    {
                        return false;
                    }
                }

                return true;
            }).ToImmutableArray();
        }

        private static Type GetTypeImplementingMethod(object obj, string name)
        {
            return s_typeImplementingMethod.GetOrAdd(
                (obj.GetType(), name),
                key => key.type.GetRuntimeMethods().FirstOrDefault(method => method.Name == key.name)?.DeclaringType);
        }
    }
}
