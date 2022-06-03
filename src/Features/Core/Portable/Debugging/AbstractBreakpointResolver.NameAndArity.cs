﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Debugging
{
    internal partial class AbstractBreakpointResolver
    {
        protected struct NameAndArity
        {
            public string Name;
            public int Arity;

            public NameAndArity(string name, int arity)
            {
                Name = name;
                Arity = arity;
            }
        }
    }
}
