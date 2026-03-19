// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class AttributeSyntax
    {
        /// <summary>
        /// Return the name used in syntax for the attribute. This is typically the class
        /// name without the "Attribute" suffix. (For certain diagnostics, the native
        /// compiler uses the attribute name from syntax rather than the class name.)
        /// </summary>
        internal string GetErrorDisplayName()
        {
            // Dev10 uses the name from source, even if it's an alias.
            return Name.ErrorDisplayName();
        }

        internal AttributeArgumentSyntax? GetNamedArgumentSyntax(string namedArgName)
        {
            Debug.Assert(!String.IsNullOrEmpty(namedArgName));

            if (argumentList != null)
            {
                foreach (var argSyntax in argumentList.Arguments)
                {
                    if (argSyntax.NameEquals != null && argSyntax.NameEquals.Name.Identifier.ValueText == namedArgName)
                    {
                        return argSyntax;
                    }
                }
            }

            return null;
        }
    }
}
