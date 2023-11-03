// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The record type includes synthesized '==' and '!=' operators equivalent to operators declared as follows:
    ///
    /// For record class:
    /// public static bool operator==(R? left, R? right)
    ///      => (object) left == right || ((object)left != null &amp;&amp; left.Equals(right));
    /// public static bool operator !=(R? left, R? right)
    ///      => !(left == right);
    ///
    /// For record struct:
    /// public static bool operator==(R left, R right)
    ///      => left.Equals(right);
    /// public static bool operator !=(R left, R right)
    ///      => !(left == right);
    ///
    ///The 'Equals' method called by the '==' operator is the 'Equals(R? other)' (<see cref="SynthesizedRecordEquals"/>).
    ///The '!=' operator delegates to the '==' operator. It is an error if the operators are declared explicitly.
    /// </summary>
    internal sealed class SynthesizedRecordInequalityOperator : SynthesizedRecordEqualityOperatorBase
    {
        public SynthesizedRecordInequalityOperator(SourceMemberContainerTypeSymbol containingType, int memberOffset, BindingDiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.InequalityOperatorName, memberOffset, diagnostics)
        {
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            try
            {
                // => !(left == right);
                F.CloseMethod(F.Block(F.Return(F.Not(F.Call(receiver: null, ContainingType.GetMembers(WellKnownMemberNames.EqualityOperatorName).OfType<SynthesizedRecordEqualityOperator>().Single(),
                                                            F.Parameter(Parameters[0]), F.Parameter(Parameters[1]))))));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
