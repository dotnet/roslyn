using System.Runtime.CompilerServices;
using Metalama.Compiler;
using Metalama.Compiler.Services;

[assembly: TypeForwardedTo(typeof(ISourceTransformer))]
[assembly: TypeForwardedTo(typeof(TransformerContext))]
[assembly: TypeForwardedTo(typeof(ISourceTransformerWithServices))]
[assembly: TypeForwardedTo(typeof(InitializeServicesContext))]
[assembly: TypeForwardedTo(typeof(InitializeServicesOptions))]
[assembly: TypeForwardedTo(typeof(IDisposableServiceProvider))]
[assembly: TypeForwardedTo(typeof(IExceptionReporter))]
[assembly: TypeForwardedTo(typeof(ILogger))]
[assembly: TypeForwardedTo(typeof(ILogWriter))]
[assembly: TypeForwardedTo(typeof(TransformerAttribute))]
[assembly: TypeForwardedTo(typeof(TransformerOrderAttribute))]
[assembly: TypeForwardedTo(typeof(MetalamaCompilerInfo))]
[assembly: TypeForwardedTo(typeof(SyntaxTreeTransformation))]
[assembly: TypeForwardedTo(typeof(SyntaxTreeTransformationKind))]
[assembly: TypeForwardedTo(typeof(DiagnosticFilteringRequest))]
[assembly: TypeForwardedTo(typeof(ManagedResource))]
[assembly: TypeForwardedTo(typeof(MetalamaCompilerAnnotations))]
[assembly: TypeForwardedTo(typeof(TransformerOptions))]

namespace Metalama.Compiler.Interface.TypeForwards
{
    public static class MetalamaCompilerInterfaces
    {
        public static void Initialize() { }
    }
}
