using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers;

namespace Roslyn.Compilers.CSharp
{
    internal class DiagnosticBufferBinderContext : BinderContext, IDisposable
    {
        private DiagnosticBag held;

        internal DiagnosticBufferBinderContext(BinderContext underlying)
            : base(underlying.Location(), underlying.Accessor, underlying)
        {
        }

        internal override DiagnosticInfo ReportDiagnostic(Diagnostic diagnostic)
        {
            if (this.held == null)
            {
                Interlocked.CompareExchange(ref this.held, new DiagnosticBag(), null);
            }

            this.held.Add(diagnostic);
            return diagnostic.Info;
        }

        internal bool AnyDiagnostics()
        {
            return held != null;
        }

        internal IEnumerable<Diagnostic> Commit(bool reportThroughContainingContext)
        {
            try
            {
                if (reportThroughContainingContext && held != null)
                {
                    foreach (var d in held.GetDiagnostics())
                    {
                        base.ReportDiagnostic(d);
                    }
                }

                return (held == null) ? SpecializedCollections.EmptyEnumerable<Diagnostic>() : held.GetDiagnostics();
            }
            finally
            {
                held = null;
            }
        }

        public void Dispose()
        {
            // you forgot to commit the diagnostics.
            if (held != null)
            {
                throw new NotSupportedException();
            }
        }
    }
}
