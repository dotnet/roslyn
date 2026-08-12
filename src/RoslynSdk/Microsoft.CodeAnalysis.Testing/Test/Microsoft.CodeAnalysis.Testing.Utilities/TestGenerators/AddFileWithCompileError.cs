// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Testing.TestGenerators
{
    public class AddFileWithCompileError : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public virtual void Execute(GeneratorExecutionContext context)
        {
            switch (context.Compilation.Language)
            {
                case LanguageNames.CSharp:
                    context.AddSource("ErrorGeneratedFile", "class C {");
                    break;

                case LanguageNames.VisualBasic:
                    context.AddSource("ErrorGeneratedFile", "Class C");
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
