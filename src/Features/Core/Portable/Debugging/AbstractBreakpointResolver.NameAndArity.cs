﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                this.Name = name;
                this.Arity = arity;
            }
        }
    }
}
