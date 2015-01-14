// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class CompilationTestData
    {
        internal struct MethodData
        {
            public readonly ILBuilder ILBuilder;
            public readonly IMethodSymbol Method;

            public MethodData(ILBuilder ilBuilder, IMethodSymbol method)
            {
                this.ILBuilder = ilBuilder;
                this.Method = method;
            }
        }

        // The map is used for storing a list of methods and their associated IL.
        // The key is a formatted qualified method name, e.g. Namespace.Class<T>.m<S>
        public readonly ConcurrentDictionary<string, MethodData> Methods = new ConcurrentDictionary<string, MethodData>();

        // The emitted module.
        public Cci.IModule Module;

        public Func<object> SymWriterFactory;
    }
}
