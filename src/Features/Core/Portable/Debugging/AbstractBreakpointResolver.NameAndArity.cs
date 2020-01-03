// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
