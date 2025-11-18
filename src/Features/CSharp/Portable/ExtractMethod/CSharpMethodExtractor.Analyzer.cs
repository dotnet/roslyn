// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal sealed partial class CSharpExtractMethodService
{
    internal sealed partial class CSharpMethodExtractor
    {
        private sealed class CSharpAnalyzer(SelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken)
            : Analyzer(selectionResult, localFunction, cancellationToken)
        {
            protected override bool TreatOutAsRef
                => false;

            protected override bool IsInPrimaryConstructorBaseType()
                => this.SelectionResult.GetContainingScopeOf<PrimaryConstructorBaseTypeSyntax>() != null;

            protected override ITypeSymbol? GetRangeVariableType(IRangeVariableSymbol symbol)
            {
                var info = this.SemanticModel.GetSpeculativeTypeInfo(SelectionResult.FinalSpan.Start, SyntaxFactory.ParseName(symbol.Name), SpeculativeBindingOption.BindAsExpression);
                if (info.Type is IErrorTypeSymbol)
                    return null;

                return info.Type == null || info.Type.SpecialType == SpecialType.System_Object
                    ? info.Type
                    : info.ConvertedType;
            }

            protected override ExtractMethodFlowControlInformation GetStatementFlowControlInformation(
                ControlFlowAnalysis controlFlowAnalysis)
            {
                return ExtractMethodFlowControlInformation.Create(
                    this.SemanticModel.Compilation,
                    supportsComplexFlowControl: true,
                    breakStatementCount: controlFlowAnalysis.ExitPoints.Count(n => n is BreakStatementSyntax),
                    continueStatementCount: controlFlowAnalysis.ExitPoints.Count(n => n is ContinueStatementSyntax),
                    returnStatementCount: controlFlowAnalysis.ExitPoints.Count(n => n is ReturnStatementSyntax),
                    endPointIsReachable: controlFlowAnalysis.EndPointIsReachable);
            }

            protected override bool ReadOnlyFieldAllowed()
            {
                var scope = SelectionResult.GetContainingScopeOf<ConstructorDeclarationSyntax>();
                return scope == null;
            }

            protected override bool IsReadOutside(ISymbol symbol, HashSet<ISymbol> readOutsideMap)
            {
                if (!base.IsReadOutside(symbol, readOutsideMap))
                    return false;

                // Special case `using var v = ...` where the selection grabs the last statement that follows the local
                // declaration.  The compiler here considers the local variable 'read outside' since it makes it to the
                // implicit 'dispose' call that comes after the last statement.  However, as that implicit dispose would
                // move if we move the `using var v` entirely into the new method, then it's still safe to move as there's
                // no actual "explicit user read" that happens in the outer caller at all.
                if (!this.SelectionResult.IsExtractMethodOnExpression &&
                    symbol is ILocalSymbol { IsUsing: true, DeclaringSyntaxReferences: [var reference] } &&
                    reference.GetSyntax(this.CancellationToken) is VariableDeclaratorSyntax
                    {
                        Parent: VariableDeclarationSyntax
                        {
                            Parent: LocalDeclarationStatementSyntax
                            {
                                Parent: BlockSyntax { Statements: [.., var lastBlockStatement] },
                            },
                        }
                    })
                {
                    var lastStatement = this.SelectionResult.GetLastStatement();
                    if (lastStatement == lastBlockStatement)
                        return false;
                }

                return true;
            }
        }
    }
}
