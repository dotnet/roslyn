namespace Roslyn.Compilers.CSharp
{
    internal abstract class ExternalBinderFactory
    {
        internal abstract Binder CreateBinder(Binder next);
    }
}