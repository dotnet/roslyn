using System;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    class QualifiedNameBinder : InContainerBinder
    {
        private readonly Binder ambient;
        private readonly NamespaceOrTypeSymbol qualifier;

        internal QualifiedNameBinder(NamespaceOrTypeSymbol qualifier, Binder outer)
            : base(qualifier, new BuckStopsHereBinder(outer.Compilation, outer.SourceTree))
        {
            if (outer == null || qualifier == null)
                throw new ArgumentNullException();

            this.ambient = outer;
            this.qualifier = qualifier;
        }

        override internal Binder SurroundingScope
        {
            get
            {
                return ambient;
            }
        }

        override internal NamespaceOrTypeSymbol Accessor
        {
            get
            {
                NamespaceOrTypeSymbol result = ambient.Accessor;
                Debug.Assert(result != null);
                return result;
            }
        }

        internal override CSDiagnosticInfo NotFound(Location location, string name, DiagnosticBag diagnostics)
        {
            return diagnostics.Add(
                ReferenceEquals(qualifier, Compilation.GlobalNamespace) ? ErrorCode.ERR_GlobalSingleTypeNameNotFound :
                    qualifier.IsNamespace ? ErrorCode.ERR_DottedTypeNameNotFoundInNS :
                    ErrorCode.ERR_DottedTypeNameNotFoundInAgg,
                location, name, qualifier.GetFullName());
        }
    }
}
