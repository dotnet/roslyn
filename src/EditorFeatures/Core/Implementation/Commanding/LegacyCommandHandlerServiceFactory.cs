using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Commanding
{
    [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
    [Export(typeof(ICommandHandlerServiceFactory))]
    internal sealed class LegacyCommandHandlerServiceFactory : ICommandHandlerServiceFactory
    {
        public LegacyCommandHandlerServiceFactory()
        {
        }
    }
}
