// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The synthesized <c>Invoke</c> method of a <see cref="SynthesizedRefStructClosureTypeSymbol"/>.
    /// It explicitly implements the function interface's invoke method. Its body is the lowered lambda
    /// body, produced during the enclosing method's closure conversion and stored via
    /// <see cref="SetLoweredBody"/> before the synthesized type is emitted.
    /// </summary>
    internal sealed class SynthesizedRefStructClosureInvokeMethod : SynthesizedImplementationMethod
    {
        private BoundStatement? _loweredBody;

        internal SynthesizedRefStructClosureInvokeMethod(NamedTypeSymbol containingType, MethodSymbol interfaceMethod)
            : base(interfaceMethod, containingType)
        {
        }

        /// <summary>
        /// The function interface invoke method this method implements.
        /// </summary>
        internal MethodSymbol InterfaceMethod => _interfaceMethod;

        /// <summary>
        /// Stores the already-lowered body produced during closure conversion of the enclosing method.
        /// Must be called before the synthesized closure type is emitted.
        /// </summary>
        internal void SetLoweredBody(BoundStatement loweredBody)
        {
            Debug.Assert(_loweredBody is null);
            _loweredBody = loweredBody;
        }

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var f = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            f.CurrentFunction = this;

            // The lowered body is produced during closure conversion of the enclosing method.
            // If it is missing (e.g. due to earlier errors), emit a throw so the type remains valid.
            Debug.Assert(_loweredBody is not null);
            f.CloseMethod(_loweredBody ?? f.ThrowNull());
        }
    }
}
