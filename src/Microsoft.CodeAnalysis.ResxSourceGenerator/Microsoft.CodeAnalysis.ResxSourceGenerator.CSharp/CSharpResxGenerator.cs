// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.ResxSourceGenerator.CSharp
{
    [Generator(LanguageNames.CSharp)]
    internal sealed class CSharpResxGenerator : AbstractResxGenerator
    {
        protected override bool SupportsNullable(GeneratorExecutionContext context)
        {
            return ((CSharpCompilation)context.Compilation).LanguageVersion >= LanguageVersion.CSharp8;
        }
    }
}
