// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The QuickTypeIdentifierAttributeChecker applies a simple fast heuristic for determining probable
    /// TypeIdentifier attributes without binding attribute types, just by looking at the final syntax of an 
    /// attribute usage. It is accessed via the QuickTypeIdentifierAttributeChecker property on Binder.
    /// </summary>
    /// <remarks>
    /// It works by maintaining a dictionary of all possible simple names that might map to a TypeIdentifier
    /// attribute.
    /// </remarks>
    internal class QuickTypeIdentifierAttributeChecker
    {
        private readonly HashSet<string> _candidates;
#if DEBUG
        private bool _sealed;
#endif

        public static readonly QuickTypeIdentifierAttributeChecker Predefined = new QuickTypeIdentifierAttributeChecker();

        private QuickTypeIdentifierAttributeChecker()
        {
            _candidates = new HashSet<string>();
            _candidates.Add(AttributeDescription.TypeIdentifierAttribute.Name);
#if DEBUG
            _sealed = true;
#endif
        }

        private QuickTypeIdentifierAttributeChecker(QuickTypeIdentifierAttributeChecker previous)
        {
            _candidates = new HashSet<string>(previous._candidates);
        }

        private void AddCandidate(string candidate)
        {
#if DEBUG
            Debug.Assert(!_sealed);
#endif 
            _candidates.Add(candidate);
        }

        public QuickTypeIdentifierAttributeChecker AddAliasesIfAny(ImmutableDictionary<string, AliasAndUsingDirective> usingAliases)
        {
            QuickTypeIdentifierAttributeChecker newChecker = null;

            foreach (KeyValuePair<string, AliasAndUsingDirective> pair in usingAliases)
            {
                if (_candidates.Contains(pair.Value.UsingDirective.Name.GetUnqualifiedName().Identifier.ValueText))
                {
                    (newChecker ?? (newChecker = new QuickTypeIdentifierAttributeChecker(this))).AddCandidate(pair.Key);
                }
            }

            if (newChecker != null)
            {
#if DEBUG
                newChecker._sealed = true;
#endif
                return newChecker;
            }

            return this;
        }

        public bool IsPossibleMatch(AttributeSyntax attr)
        {
#if DEBUG
            Debug.Assert(_sealed);
#endif
            string name = attr.Name.GetUnqualifiedName().Identifier.ValueText;
            return _candidates.Contains(name) || _candidates.Contains(name + "Attribute");
        }
    }
}
