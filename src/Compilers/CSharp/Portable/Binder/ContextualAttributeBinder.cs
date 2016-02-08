// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Each application of an attribute is effectively a constructor call.  Since the attribute constructor
    /// might have a CallerMemberName parameter, we need to keep track of which method/property/event
    /// the attribute is on/in (e.g. on a parameter) so that we can use the name of that member as the 
    /// CallerMemberName argument.
    /// </summary>
    internal sealed class ContextualAttributeBinder : Binder
    {
        private readonly Symbol _attributeTarget;
        private readonly Symbol _attributedMember;

        /// <param name="enclosing">Next binder in the chain (enclosing).</param>
        /// <param name="symbol">Symbol to which the attribute was applied (e.g. a parameter).</param>
        public ContextualAttributeBinder(Binder enclosing, Symbol symbol)
            : base(enclosing, enclosing.Flags | BinderFlags.InContectualAttributeBinder)
        {
            _attributeTarget = symbol;
            _attributedMember = GetAttributedMember(symbol);
        }

        /// <summary>
        /// We're binding an attribute and this is the member to/in which the attribute was applied.
        /// </summary>
        /// <remarks>
        /// Method, property, event, or null.
        /// A virtual property on Binder (i.e. our usual pattern) would be more robust, but the applicability
        /// of this property is so narrow that it doesn't seem worthwhile.
        /// </remarks>
        internal Symbol AttributedMember
        {
            get
            {
                return _attributedMember;
            }
        }

        /// <summary>
        /// Walk up to the nearest method/property/event.
        /// </summary>
        private static Symbol GetAttributedMember(Symbol symbol)
        {
            for (; (object)symbol != null; symbol = symbol.ContainingSymbol)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Method:
                    case SymbolKind.Property:
                    case SymbolKind.Event:
                        return symbol;
                }
            }

            return symbol;
        }

        internal Symbol AttributeTarget
        {
            get
            {
                return _attributeTarget;
            }
        }
    }
}
