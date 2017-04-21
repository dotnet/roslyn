// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal struct EmitContext
    {
        public readonly CommonPEModuleBuilder Module;
        public readonly SyntaxNode SyntaxNodeOpt;
        public readonly DiagnosticBag Diagnostics;

        public EmitContext(CommonPEModuleBuilder module, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(module != null);
            Debug.Assert(diagnostics != null);

            Module = module;
            SyntaxNodeOpt = syntaxNodeOpt;
            Diagnostics = diagnostics;
        }
    }
}
