﻿using System.Runtime.CompilerServices;
using Metalama.Compiler;

[assembly: TypeForwardedTo(typeof(ISourceTransformer))]
[assembly: TypeForwardedTo(typeof(TransformerContext))]
[assembly: TypeForwardedTo(typeof(TransformerAttribute))]
[assembly: TypeForwardedTo(typeof(TransformerOrderAttribute))]
[assembly: TypeForwardedTo(typeof(MetalamaCompilerInfo))]
[assembly: TypeForwardedTo(typeof(SyntaxTreeTransformation))]
[assembly: TypeForwardedTo(typeof(DiagnosticFilteringRequest))]
[assembly: TypeForwardedTo(typeof(ManagedResource))]

namespace Metalama.Compiler.Interface.TypeForwards
{
    public static class MetalamaCompilerInterfaces
    {
        public static void Initialize() { }
    }
}
