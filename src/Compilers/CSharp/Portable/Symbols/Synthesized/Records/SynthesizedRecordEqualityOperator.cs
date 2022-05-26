// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal sealed class SynthesizedRecordEqualityOperator : SynthesizedRecordEqualityOperatorBase
    {
        public SynthesizedRecordEqualityOperator(SourceMemberContainerTypeSymbol containingType, int memberOffset, BindingDiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.EqualityOperatorName, memberOffset, diagnostics)
        {
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            try
            {
                // For record class:
                // => (object)left == right || ((object)left != null && left.Equals(right));
                // For record struct:
                // => left.Equals(right));
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

                var left = F.Parameter(Parameters[0]);
                var right = F.Parameter(Parameters[1]);

                BoundExpression expression;
                if (ContainingType.IsRecordStruct)
                {
                    expression = F.Call(left, equals, right);
                }
                else
                {
                    BoundExpression objectEqual = F.ObjectEqual(left, right);
                    BoundExpression recordEquals = F.LogicalAnd(F.ObjectNotEqual(left, F.Null(F.SpecialType(SpecialType.System_Object))),
                                                            F.Call(left, equals, right));
                    expression = F.LogicalOr(objectEqual, recordEquals);
                }

                F.CloseMethod(F.Block(F.Return(expression)));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
