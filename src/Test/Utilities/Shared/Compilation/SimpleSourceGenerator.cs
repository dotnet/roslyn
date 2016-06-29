// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class SimpleSourceGenerator : SourceGenerator
    {
        private readonly Action<SourceGeneratorContext> _execute;

        internal SimpleSourceGenerator(Action<SourceGeneratorContext> execute)
        {
            _execute = execute;
        }

        public override void Execute(SourceGeneratorContext context)
        {
            _execute(context);
        }
    }
}
