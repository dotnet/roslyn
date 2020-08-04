// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The record type includes synthesized '==' and '!=' operators equivalent to operators declared as follows:
    /// 
    /// public static bool operator==(R? r1, R? r2)
    ///      => (object) r1 == r2 || ((object)r1 != null &amp;&amp; r1.Equals(r2));
    /// public static bool operator !=(R? r1, R? r2)
    ///      => !(r1 == r2);
    ///        
    ///The 'Equals' method called by the '==' operator is the 'Equals(R? other)' (<see cref="SynthesizedRecordEquals"/>).
    ///The '!=' operator delegates to the '==' operator. It is an error if the operators are declared explicitly.
    /// </summary>
    internal sealed class SynthesizedRecordInequalityOperator : SynthesizedRecordEqualityOperatorBase
    {
        public SynthesizedRecordInequalityOperator(SourceMemberContainerTypeSymbol containingType, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.InequalityOperatorName, memberOffset, diagnostics)
        {
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            try
            {
                // => !(r1 == r2);
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
