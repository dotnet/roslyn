// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace System.Runtime.CompilerServices
{
    // PROTOTYPE(NullableDogfood): Remove attribute definition if synthesized by the compiler.
    internal sealed class NonNullTypesAttribute : Attribute
    {
        public NonNullTypesAttribute(bool b = true)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal class NotNullWhenTrueAttribute : Attribute
    {
        public NotNullWhenTrueAttribute() { }
    }
}
