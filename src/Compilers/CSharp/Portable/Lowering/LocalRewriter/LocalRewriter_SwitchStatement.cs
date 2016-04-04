// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        // Rewriting for integral and string switch statements.
        // 
        // For switch statements, we have an option of completely rewriting the switch header
        // and switch sections into simpler constructs, i.e. we can rewrite the switch header
        // using bound conditional goto statements and the rewrite the switch sections into
        // bound labeled statements.

        // However, all the logic for emitting the switch jump tables is language agnostic
        // and includes IL optimizations. Hence we delay the switch jump table generation
        // till the emit phase. This way we also get additional benefit of sharing this code
        // between both VB and C# compilers.

        // For integral switch statements, we delay almost all the work
        // to the emit phase.

        // For string switch statements, we need to determine if we are generating a hash
        // table based jump table or a non hash jump table, i.e. linear string comparisons
        // with each case label. We use the Dev10 Heuristic to determine this
        // (see SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch() for details).
        // If we are generating a hash table based jump table, we use a simple customizable
        // hash function to hash the string constants corresponding to the case labels.
        // See SwitchStringJumpTableEmitter.ComputeStringHash().
        // We need to emit this function to compute the hash value into the compiler generate
        // <PrivateImplementationDetails> class. 
        // If we have at least one string switch statement in a module that needs a
        // hash table based jump table, we generate a single public string hash synthesized method
        // that is shared across the module.

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            var syntax = node.Syntax;

            var rewrittenExpression = (BoundExpression)Visit(node.Expression);
            var rewrittenSections = VisitSwitchSections(node.SwitchSections);

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the expression are being executed.
            var rewrittenStatement = MakeSwitchStatement(syntax, AddConditionSequencePoint(rewrittenExpression, node), rewrittenSections, node.ConstantTargetOpt, node.InnerLocals, node.InnerLocalFunctions, node.BreakLabel, node);

            // Create the sequence point if generating debug info and
            // node is not compiler generated
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                rewrittenStatement = _instrumenter.InstrumentSwitchStatement(node, rewrittenStatement);
            }

            return rewrittenStatement;
        }

        private BoundStatement MakeSwitchStatement(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenExpression,
            ImmutableArray<BoundSwitchSection> rewrittenSections,
            LabelSymbol constantTargetOpt,
            ImmutableArray<LocalSymbol> locals,
            ImmutableArray<LocalFunctionSymbol> localFunctions,
            GeneratedLabelSymbol breakLabel,
            BoundSwitchStatement oldNode)
        {
            Debug.Assert(oldNode != null);
            Debug.Assert((object)rewrittenExpression.Type != null);

            return rewrittenExpression.Type.IsNullableType() ?
                MakeSwitchStatementWithNullableExpression(syntax, rewrittenExpression, rewrittenSections, constantTargetOpt, locals, localFunctions, breakLabel, oldNode) :
                MakeSwitchStatementWithNonNullableExpression(syntax, null, rewrittenExpression, rewrittenSections, constantTargetOpt, locals, localFunctions, breakLabel, oldNode);
        }

        private BoundStatement MakeSwitchStatementWithNonNullableExpression(
            CSharpSyntaxNode syntax,
            BoundStatement preambleOpt,
            BoundExpression rewrittenExpression,
            ImmutableArray<BoundSwitchSection> rewrittenSections,
            LabelSymbol constantTargetOpt,
            ImmutableArray<LocalSymbol> locals,
            ImmutableArray<LocalFunctionSymbol> localFunctions,
            GeneratedLabelSymbol breakLabel,
            BoundSwitchStatement oldNode)
        {
            Debug.Assert(!rewrittenExpression.Type.IsNullableType());
            Debug.Assert((object)oldNode.StringEquality == null);

            // If we are emitting a hash table based string switch,
            // we need to generate a helper method for computing
            // string hash value in <PrivateImplementationDetails> class.

            MethodSymbol stringEquality = null;
            if (rewrittenExpression.Type.SpecialType == SpecialType.System_String)
            {
                EnsureStringHashFunction(rewrittenSections, syntax);
                stringEquality = GetSpecialTypeMethod(syntax, SpecialMember.System_String__op_Equality);
            }

            return oldNode.Update(
                loweredPreambleOpt: preambleOpt,
                expression: rewrittenExpression,
                constantTargetOpt: constantTargetOpt,
                innerLocals: locals,
                innerLocalFunctions: localFunctions,
                switchSections: rewrittenSections,
                breakLabel: breakLabel,
                stringEquality: stringEquality);
        }

        private BoundStatement MakeSwitchStatementWithNullableExpression(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenExpression,
            ImmutableArray<BoundSwitchSection> rewrittenSections,
            LabelSymbol constantTargetOpt,
            ImmutableArray<LocalSymbol> locals,
            ImmutableArray<LocalFunctionSymbol> localFunctions,
            GeneratedLabelSymbol breakLabel,
            BoundSwitchStatement oldNode)
        {
            Debug.Assert(rewrittenExpression.Type.IsNullableType());

            var exprSyntax = rewrittenExpression.Syntax;
            var exprNullableType = rewrittenExpression.Type;

            var statementBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            // Rewrite the nullable expression to a temp as we might have a user defined conversion from source expression to switch governing type.
            // We can avoid generating the temp if the expression is a bound local.
            LocalSymbol tempLocal;
            if (rewrittenExpression.Kind != BoundKind.Local)
            {
                BoundAssignmentOperator assignmentToTemp;
                BoundLocal boundTemp = _factory.StoreToTemp(rewrittenExpression, out assignmentToTemp);
                var tempAssignment = new BoundExpressionStatement(exprSyntax, assignmentToTemp);
                statementBuilder.Add(tempAssignment);
                tempLocal = boundTemp.LocalSymbol;
                rewrittenExpression = boundTemp;
            }
            else
            {
                tempLocal = null;
            }

            // Generate a BoundConditionalGoto with null check as the conditional expression and appropriate switch label as the target: null, default or exit label.
            BoundStatement condGotoNullValueTargetLabel = new BoundConditionalGoto(
                exprSyntax,
                condition: MakeNullCheck(exprSyntax, rewrittenExpression, BinaryOperatorKind.NullableNullEqual),
                jumpIfTrue: true,
                label: GetNullValueTargetSwitchLabel(rewrittenSections, breakLabel));

            // Rewrite the switch statement using nullable expression's underlying value as the switch expression.

            // rewrittenExpression.GetValueOrDefault()
            MethodSymbol getValueOrDefault = GetNullableMethod(syntax, exprNullableType, SpecialMember.System_Nullable_T_GetValueOrDefault);
            BoundCall callGetValueOrDefault = BoundCall.Synthesized(exprSyntax, rewrittenExpression, getValueOrDefault);
            rewrittenExpression = callGetValueOrDefault;

            // rewrite switch statement
            BoundStatement rewrittenSwitchStatement = MakeSwitchStatementWithNonNullableExpression(
                syntax,
                condGotoNullValueTargetLabel,
                rewrittenExpression, 
                rewrittenSections, 
                constantTargetOpt, 
                locals, 
                localFunctions, 
                breakLabel, 
                oldNode);

            statementBuilder.Add(rewrittenSwitchStatement);

            return new BoundBlock(
                syntax,
                locals: (object)tempLocal == null ? ImmutableArray<LocalSymbol>.Empty : ImmutableArray.Create<LocalSymbol>(tempLocal),
                localFunctions: ImmutableArray<LocalFunctionSymbol>.Empty,
                statements: statementBuilder.ToImmutableAndFree());
        }

        private static LabelSymbol GetNullValueTargetSwitchLabel(ImmutableArray<BoundSwitchSection> sections, GeneratedLabelSymbol breakLabel)
        {
            LabelSymbol fallThroughLabel = breakLabel;

            foreach (var section in sections)
            {
                foreach (BoundSwitchLabel boundLabel in section.SwitchLabels)
                {
                    var label = (SourceLabelSymbol)boundLabel.Label;
                    var labelConstant = label.SwitchCaseLabelConstant;

                    if (labelConstant == ConstantValue.Null)
                    {
                        return label;
                    }
                    else if (labelConstant == null)
                    {
                        // Default label
                        Debug.Assert(label.IdentifierNodeOrToken.Kind() == SyntaxKind.DefaultSwitchLabel);
                        Debug.Assert(fallThroughLabel == breakLabel);

                        fallThroughLabel = label;
                    }
                }
            }

            return fallThroughLabel;
        }

        private ImmutableArray<BoundSwitchSection> VisitSwitchSections(ImmutableArray<BoundSwitchSection> sections)
        {
            if (sections.Length > 0)
            {
                // Visit the switch sections
                var sectionsBuilder = ArrayBuilder<BoundSwitchSection>.GetInstance();
                foreach (BoundSwitchSection section in sections)
                {
                    sectionsBuilder.Add((BoundSwitchSection)VisitSwitchSection(section));
                }

                return sectionsBuilder.ToImmutableAndFree();
            }

            return ImmutableArray<BoundSwitchSection>.Empty;
        }

        public override BoundNode VisitSwitchSection(BoundSwitchSection node)
        {
            return node.Update(VisitList(node.SwitchLabels), VisitList(node.Statements));
        }

        private static int CountLabels(ImmutableArray<BoundSwitchSection> rewrittenSections)
        {
            int count = 0;
            foreach (var section in rewrittenSections)
            {
                foreach (var boundLabel in section.SwitchLabels)
                {
                    if (boundLabel.Label.IdentifierNodeOrToken.Kind() == SyntaxKind.CaseSwitchLabel)
                    {
                        Debug.Assert(((SourceLabelSymbol)boundLabel.Label).SwitchCaseLabelConstant.IsString ||
                            ((SourceLabelSymbol)boundLabel.Label).SwitchCaseLabelConstant.IsNull);

                        count++;
                    }
                }
            }

            return count;
        }

        // Checks whether we are generating a hash table based string switch and
        // we need to generate a new helper method for computing string hash value.
        // Creates the method if needed.
        private void EnsureStringHashFunction(ImmutableArray<BoundSwitchSection> rewrittenSections, CSharpSyntaxNode syntaxNode)
        {
            var module = this.EmitModule;
            if (module == null)
            {
                return;
            }

            int labelsCount = CountLabels(rewrittenSections);
            if (!SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(module, labelsCount))
            {
                return;
            }

            // If we have already generated the helper, possibly for another switch
            // or on another thread, we don't need to regenerate it.
            var privateImplClass = module.GetPrivateImplClass(syntaxNode, _diagnostics);
            if (privateImplClass.GetMethod(PrivateImplementationDetails.SynthesizedStringHashFunctionName) != null)
            {
                return;
            }

            // cannot emit hash method if have no access to Chars.
            var charsMember = _compilation.GetSpecialTypeMember(SpecialMember.System_String__Chars);
            if ((object)charsMember == null || charsMember.GetUseSiteDiagnostic() != null)
            {
                return;
            }

            TypeSymbol returnType = _factory.SpecialType(SpecialType.System_UInt32);
            TypeSymbol paramType = _factory.SpecialType(SpecialType.System_String);

            var method = new SynthesizedStringSwitchHashMethod(module.SourceModule, privateImplClass, returnType, paramType);
            privateImplClass.TryAddSynthesizedMethod(method);
        }
    }
}
