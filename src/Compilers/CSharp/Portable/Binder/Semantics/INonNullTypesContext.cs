// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Instead of storing a bool to tell us if we're in a NonNullTypes context,
    /// we use this interface to pull on that information lazily.
    /// </summary>
    internal interface INonNullTypesContext
    {
        bool NonNullTypes { get; }
    }

    internal sealed class NonNullTypesContextTrue : INonNullTypesContext
    {
        public static INonNullTypesContext Instance = new NonNullTypesContextTrue();
        public bool NonNullTypes => true;
    }

    internal sealed class NonNullTypesContextFalse : INonNullTypesContext
    {
        public static INonNullTypesContext Instance = new NonNullTypesContextFalse();
        public bool NonNullTypes => false;
    }
}
