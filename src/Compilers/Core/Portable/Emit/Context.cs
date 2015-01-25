// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal struct EmitContext
    {
        public readonly Cci.IModule Module;
        public readonly SyntaxNode SyntaxNodeOpt;
        public readonly DiagnosticBag Diagnostics;

        public EmitContext(Cci.IModule module, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(module != null);
            Debug.Assert(diagnostics != null);

            this.Module = module;
            this.SyntaxNodeOpt = syntaxNodeOpt;
            this.Diagnostics = diagnostics;
        }

        public CommonPEModuleBuilder ModuleBuilder
        {
            get { return (CommonPEModuleBuilder)Module; }
        }
    }
}
