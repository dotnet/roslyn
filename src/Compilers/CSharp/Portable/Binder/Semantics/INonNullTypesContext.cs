// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Instead of storing a bool to tell us if we're in a NonNullTypes context,
    /// we use this interface to pull on that information lazily.
    /// </summary>
    internal interface INonNullTypesContext
    {
        bool? NonNullTypes { get; }
    }

    internal sealed class NonNullTypesTrueContext : INonNullTypesContext
    {
        public static readonly INonNullTypesContext Instance = new NonNullTypesTrueContext();
        public bool? NonNullTypes => true;
    }

    internal sealed class NonNullTypesFalseContext : INonNullTypesContext
    {
        public static readonly INonNullTypesContext Instance = new NonNullTypesFalseContext();
        public bool? NonNullTypes => false;
    }
}
