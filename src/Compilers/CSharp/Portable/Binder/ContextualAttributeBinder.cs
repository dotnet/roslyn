// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        /// <param name="enclosing">Next binder in the chain (enclosing).</param>
        /// <param name="symbol">Symbol to which the attribute was applied (e.g. a parameter).</param>
        public ContextualAttributeBinder(Binder enclosing, Symbol symbol)
            : base(enclosing, enclosing.Flags | BinderFlags.InContextualAttributeBinder)
        {
            _attributeTarget = symbol;
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
