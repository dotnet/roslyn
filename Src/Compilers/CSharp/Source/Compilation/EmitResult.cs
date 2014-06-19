using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// The result of the Compilation Emit operation.
    /// </summary>
    public sealed class EmitResult : CommonEmitResult
    {
        private readonly ImmutableArray<Diagnostic> diagnostics;

        public override ImmutableArray<Diagnostic> Diagnostics { get { return this.diagnostics; } }

        internal EmitResult(bool success, ImmutableArray<Diagnostic> diagnostics, EmitBaseline baseline)
            : base(success, baseline)
        {
            this.diagnostics = diagnostics;
        }
    }
}
