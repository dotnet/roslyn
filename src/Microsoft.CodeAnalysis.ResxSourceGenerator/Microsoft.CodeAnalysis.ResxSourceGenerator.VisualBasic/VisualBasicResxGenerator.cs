// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ResxSourceGenerator.VisualBasic
{
    [Generator(LanguageNames.VisualBasic)]
    internal sealed class VisualBasicResxGenerator : AbstractResxGenerator
    {
        protected override bool SupportsNullable(GeneratorExecutionContext context)
        {
            return false;
        }
    }
}
