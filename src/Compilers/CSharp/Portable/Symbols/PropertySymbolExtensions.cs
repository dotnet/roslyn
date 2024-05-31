// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class PropertySymbolExtensions
    {
        public static bool IsParams(this PropertySymbol property)
        {
            return property.ParameterCount != 0 && property.Parameters[property.ParameterCount - 1].IsParams;
        }

        /// <summary>
        /// If the property has a GetMethod, return that.  Otherwise check the overridden
        /// property, if any.  Repeat for each overridden property.
        /// </summary>
        public static MethodSymbol? GetOwnOrInheritedGetMethod(this PropertySymbol? property)
        {
            while ((object?)property != null)
            {
                MethodSymbol getMethod = property.GetMethod;
                if ((object?)getMethod != null)
                {
                    return getMethod;
                }

                property = property.OverriddenProperty;
            }

            return null;
        }

        /// <summary>
        /// If the property has a SetMethod, return that.  Otherwise check the overridden
        /// property, if any.  Repeat for each overridden property.
        /// </summary>
        public static MethodSymbol? GetOwnOrInheritedSetMethod(this PropertySymbol? property)
        {
            while ((object?)property != null)
            {
                MethodSymbol setMethod = property.SetMethod;
                if ((object?)setMethod != null)
                {
                    return setMethod;
                }

                property = property.OverriddenProperty;
            }

            return null;
        }

        public static bool CanCallMethodsDirectly(this PropertySymbol property)
        {
            if (property.MustCallMethodsDirectly)
            {
                return true;
            }

            // Indexed property accessors can always be called directly, to support legacy code.
            return property.IsIndexedProperty && (!property.IsIndexer || property.HasRefOrOutParameter());
        }

        public static bool HasRefOrOutParameter(this PropertySymbol property)
        {
            foreach (ParameterSymbol param in property.Parameters)
            {
                if (param.RefKind == RefKind.Ref || param.RefKind == RefKind.Out)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
