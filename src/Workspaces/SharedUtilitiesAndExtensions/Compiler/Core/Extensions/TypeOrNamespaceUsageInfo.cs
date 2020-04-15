// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum TypeOrNamespaceUsageInfo
    {
        /// <summary>
        /// Represents default value indicating no usage.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Represents a reference to a namespace or type on the left side of a dotted name (qualified name or member access).
        /// For example, 'NS' in <code>NS.Type x = new NS.Type();</code> or <code>NS.Type.StaticMethod();</code> or 
        /// 'Type' in <code>Type.NestedType x = new Type.NestedType();</code> or <code>Type.StaticMethod();</code>
        /// </summary>
        Qualified = 0x01,

        /// <summary>
        /// Represents a generic type argument reference.
        /// For example, 'Type' in <code>Generic{Type} x = ...;</code> or <code>class Derived : Base{Type} { }</code>
        /// </summary>
        TypeArgument = 0x02,

        /// <summary>
        /// Represents a type parameter constraint that is a type.
        /// For example, 'Type' in <code>class Derived{T} where T : Type { }</code>
        /// </summary>
        TypeConstraint = 0x04,

        /// <summary>
        /// Represents a base type or interface reference in the base list of a named type.
        /// For example, 'Base' in <code>class Derived : Base { }</code>.
        /// </summary>
        Base = 0x08,

        /// <summary>
        /// Represents a reference to a type whose instance is being created.
        /// For example, 'C' in <code>var x = new C();</code>, where 'C' is a named type.
        /// </summary>
        ObjectCreation = 0x10,

        /// <summary>
        /// Represents a reference to a namespace or type within a using or imports directive.
        /// For example, <code>using NS;</code> or <code>using static NS.Extensions</code> or <code>using Alias = MyType</code>.
        /// </summary>
        Import = 0x20,

        /// <summary>
        /// Represents a reference to a namespace name in a namespace declaration context.
        /// For example, 'N1' or <code>namespaces N1.N2 { }</code>.
        /// </summary>
        NamespaceDeclaration = 0x40,
    }
}
