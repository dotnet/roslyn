// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Roslyn.Utilities;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    internal class ChainedFormattingRules
    {
        private static readonly ConcurrentDictionary<(Type type, string name), Type> s_typeImplementingMethod = new ConcurrentDictionary<(Type type, string name), Type>();

        private readonly ImmutableArray<AbstractFormattingRule> _formattingRules;
        private readonly OptionSet _optionSet;

        private readonly ImmutableArray<AbstractFormattingRule> _addSuppressOperationsRules;
        private readonly ImmutableArray<AbstractFormattingRule> _addAnchorIndentationOperationsRules;
        private readonly ImmutableArray<AbstractFormattingRule> _addIndentBlockOperationsRules;
        private readonly ImmutableArray<AbstractFormattingRule> _addAlignTokensOperationsRules;
        private readonly ImmutableArray<AbstractFormattingRule> _getAdjustNewLinesOperationRules;
        private readonly ImmutableArray<AbstractFormattingRule> _getAdjustSpacesOperationRules;

        public ChainedFormattingRules(IEnumerable<AbstractFormattingRule> formattingRules, OptionSet set)
        {
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(set);

            _formattingRules = formattingRules.ToImmutableArray();
            _optionSet = set;

            _addSuppressOperationsRules = FilterToRulesImplementingMethod(_formattingRules, nameof(AbstractFormattingRule.AddSuppressOperations));
            _addAnchorIndentationOperationsRules = FilterToRulesImplementingMethod(_formattingRules, nameof(AbstractFormattingRule.AddAnchorIndentationOperations));
            _addIndentBlockOperationsRules = FilterToRulesImplementingMethod(_formattingRules, nameof(AbstractFormattingRule.AddIndentBlockOperations));
            _addAlignTokensOperationsRules = FilterToRulesImplementingMethod(_formattingRules, nameof(AbstractFormattingRule.AddAlignTokensOperations));
            _getAdjustNewLinesOperationRules = FilterToRulesImplementingMethod(_formattingRules, nameof(AbstractFormattingRule.GetAdjustNewLinesOperation));
            _getAdjustSpacesOperationRules = FilterToRulesImplementingMethod(_formattingRules, nameof(AbstractFormattingRule.GetAdjustSpacesOperation));
        }

        public void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode currentNode)
        {
            var action = new NextSuppressOperationAction(_addSuppressOperationsRules, index: 0, currentNode, _optionSet, list);
            action.Invoke();
        }

        public void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode currentNode)
        {
            var action = new NextAnchorIndentationOperationAction(_addAnchorIndentationOperationsRules, index: 0, currentNode, _optionSet, list);
            action.Invoke();
        }

        public void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode currentNode)
        {
            var action = new NextIndentBlockOperationAction(_addIndentBlockOperationsRules, index: 0, currentNode, _optionSet, list);
            action.Invoke();
        }

        public void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode currentNode)
        {
            var action = new NextAlignTokensOperationAction(_addAlignTokensOperationsRules, index: 0, currentNode, _optionSet, list);
            action.Invoke();
        }

        public AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            var action = new NextGetAdjustNewLinesOperation(_getAdjustNewLinesOperationRules, index: 0, previousToken, currentToken, _optionSet);
            return action.Invoke();
        }

        public AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            var action = new NextGetAdjustSpacesOperation(_getAdjustSpacesOperationRules, index: 0, previousToken, currentToken, _optionSet);
            return action.Invoke();
        }

        private static ImmutableArray<AbstractFormattingRule> FilterToRulesImplementingMethod(ImmutableArray<AbstractFormattingRule> rules, string name)
        {
            return rules.Where(rule =>
            {
                var type = GetTypeImplementingMethod(rule, name);
                if (type == typeof(AbstractFormattingRule))
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
