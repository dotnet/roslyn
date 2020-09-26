// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Globalization;
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
    internal sealed class SynthesizedRecordEqualityOperator : SynthesizedRecordEqualityOperatorBase
    {
        public SynthesizedRecordEqualityOperator(SourceMemberContainerTypeSymbol containingType, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.EqualityOperatorName, memberOffset, diagnostics)
        {
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            try
            {
                // => (object)r1 == r2 || ((object)r1 != null && r1.Equals(r2));
                MethodSymbol? equals = null;
                foreach (var member in ContainingType.GetMembers(WellKnownMemberNames.ObjectEquals))
                {
                    if (member is MethodSymbol candidate && candidate.ParameterCount == 1 && candidate.Parameters[0].RefKind == RefKind.None &&
                        candidate.ReturnType.SpecialType == SpecialType.System_Boolean && !candidate.IsStatic &&
                        candidate.Parameters[0].Type.Equals(ContainingType, TypeCompareKind.AllIgnoreOptions))
                    {
                        equals = candidate;
                        break;
                    }
                }

                if (equals is null)
                {
                    // Unable to locate expected method, an error was reported elsewhere
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                var r1 = F.Parameter(Parameters[0]);
                var r2 = F.Parameter(Parameters[1]);

                BoundExpression objectEqual = F.ObjectEqual(r1, r2);
                BoundExpression recordEquals = F.LogicalAnd(F.ObjectNotEqual(r1, F.Null(F.SpecialType(SpecialType.System_Object))),
                                                            F.Call(r1, equals, r2));

                F.CloseMethod(F.Block(F.Return(F.LogicalOr(objectEqual, recordEquals))));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
