﻿using System.Runtime.CompilerServices;
using Caravela.Compiler;

[assembly: TypeForwardedTo(typeof(ISourceTransformer))]
[assembly: TypeForwardedTo(typeof(TransformerContext))]
[assembly: TypeForwardedTo(typeof(TransformerAttribute))]
[assembly: TypeForwardedTo(typeof(TransformerOrderAttribute))]
[assembly: TypeForwardedTo(typeof(CaravelaCompilerInfo))]
[assembly: TypeForwardedTo(typeof(SyntaxTreeTransformation))]
[assembly: TypeForwardedTo(typeof(DiagnosticFilteringRequest))]
[assembly: TypeForwardedTo(typeof(ManagedResource))]

namespace Caravela.Compiler.Interface.TypeForwards
{
    public static class CaravelaCompilerInterfaces
    {
        public static void Initialize() { }
    }
}
