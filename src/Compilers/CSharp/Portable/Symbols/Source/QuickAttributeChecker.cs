// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The QuickAttributeChecker applies a simple fast heuristic for determining probable
    /// attributes of certain kinds without binding attribute types, just by looking at the final syntax of an
    /// attribute usage.
    /// </summary>
    /// <remarks>
    /// It works by maintaining a dictionary of all possible simple names that might map to the given
    /// attribute.
    /// </remarks>
    internal class QuickAttributeChecker
    {
        private readonly Dictionary<string, QuickAttributes> _nameToAttributeMap;
        private static QuickAttributeChecker _lazyQuickAttributeChecker;

#if DEBUG
        private bool _sealed;
#endif

        internal static QuickAttributeChecker Predefined
        {
            get
            {
                if (_lazyQuickAttributeChecker is null)
                {
                    Interlocked.CompareExchange(ref _lazyQuickAttributeChecker, CreateQuickAttributeChecker(), null);
                }

                return _lazyQuickAttributeChecker;
            }
        }

        private static QuickAttributeChecker CreateQuickAttributeChecker()
        {
            var result = new QuickAttributeChecker();
            result.AddName(AttributeDescription.TypeIdentifierAttribute.Name, QuickAttributes.TypeIdentifier);
            result.AddName(AttributeDescription.TypeForwardedToAttribute.Name, QuickAttributes.TypeForwardedTo);

#if DEBUG
            result._sealed = true;
#endif
            return result;
        }

        private QuickAttributeChecker()
        {
            _nameToAttributeMap = new Dictionary<string, QuickAttributes>();
            // NOTE: caller must seal
        }

        protected QuickAttributeChecker(QuickAttributeChecker previous)
        {
            _nameToAttributeMap = new Dictionary<string, QuickAttributes>(previous._nameToAttributeMap);
            // NOTE: caller must seal
        }

        private void AddName(string name, QuickAttributes newAttributes)
        {
#if DEBUG
            Debug.Assert(!_sealed);
#endif
            var currentValue = QuickAttributes.None;
            _nameToAttributeMap.TryGetValue(name, out currentValue);

            QuickAttributes newValue = newAttributes | currentValue;
            _nameToAttributeMap[name] = newValue;

            // We allow "Name" to bind to "NameAttribute"
            if (name.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
            {
                _nameToAttributeMap[name.Substring(0, name.Length - "Attribute".Length)] = newValue;
            }
        }

        internal QuickAttributeChecker AddAliasesIfAny(ImmutableDictionary<string, AliasAndUsingDirective> usingAliases)
        {
            QuickAttributeChecker newChecker = null;

            foreach (KeyValuePair<string, AliasAndUsingDirective> pair in usingAliases)
            {
                string finalName = pair.Value.UsingDirective.Name.GetUnqualifiedName().Identifier.ValueText;
                if (_nameToAttributeMap.TryGetValue(finalName, out var foundAttributes))
                {
                    string aliasName = pair.Key;
                    (newChecker ?? (newChecker = new QuickAttributeChecker(this))).AddName(aliasName, foundAttributes);
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

        public bool IsPossibleMatch(AttributeSyntax attr, QuickAttributes pattern)
        {
#if DEBUG
            Debug.Assert(_sealed);
#endif
            string name = attr.Name.GetUnqualifiedName().Identifier.ValueText;
            if (_nameToAttributeMap.TryGetValue(name, out var foundAttributes))
            {
                return (foundAttributes & pattern) != 0;
            }

            return false;
        }
    }

    [Flags]
    internal enum QuickAttributes : byte
    {
        None = 0,
        TypeIdentifier = 1 << 0,
        TypeForwardedTo = 2 << 0
    }
}
