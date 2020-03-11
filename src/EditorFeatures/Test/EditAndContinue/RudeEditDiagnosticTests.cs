﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public class RudeEditDiagnosticTests
    {
        [Fact]
        public void ToDiagnostic()
        {
            var tree = SyntaxFactory.ParseCompilationUnit("class C { }").SyntaxTree;
            var syntaxNode = tree.GetRoot();

            // most rude edits have a single argument, list those that have different count:

            var arg0 = new HashSet<RudeEditKind>()
            {
                RudeEditKind.ActiveStatementUpdate,
                RudeEditKind.PartiallyExecutedActiveStatementUpdate,
                RudeEditKind.PartiallyExecutedActiveStatementDelete,
                RudeEditKind.DeleteActiveStatement,
                RudeEditKind.UpdateExceptionHandlerOfActiveTry,
                RudeEditKind.UpdateTryOrCatchWithActiveFinally,
                RudeEditKind.UpdateCatchHandlerAroundActiveStatement,
                RudeEditKind.FieldKindUpdate,
                RudeEditKind.TypeKindUpdate,
                RudeEditKind.AccessorKindUpdate,
                RudeEditKind.MethodKindUpdate,
                RudeEditKind.DeclareLibraryUpdate,
                RudeEditKind.DeclareAliasUpdate,
                RudeEditKind.ChangingConstructorVisibility,
                RudeEditKind.InsertDllImport,
                RudeEditKind.MethodBodyAdd,
                RudeEditKind.MethodBodyDelete,
                RudeEditKind.GenericMethodUpdate,
                RudeEditKind.GenericTypeUpdate,
                RudeEditKind.ExperimentalFeaturesEnabled,
                RudeEditKind.AwaitStatementUpdate,
                RudeEditKind.InsertFile,
                RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas,
                RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement,
                RudeEditKind.SwitchBetweenLambdaAndLocalFunction,
                RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier,
            };

            var arg2 = new HashSet<RudeEditKind>()
            {
                RudeEditKind.ConstraintKindUpdate,
                RudeEditKind.InsertIntoStruct,
                RudeEditKind.ConstraintKindUpdate,
                RudeEditKind.InsertIntoStruct,
                RudeEditKind.ChangingCapturedVariableType,
                RudeEditKind.AccessingCapturedVariableInLambda,
                RudeEditKind.NotAccessingCapturedVariableInLambda,
                RudeEditKind.RenamingCapturedVariable,
                RudeEditKind.ChangingStateMachineShape,
                RudeEditKind.InternalError,
            };

            var arg3 = new HashSet<RudeEditKind>()
            {
                RudeEditKind.InsertLambdaWithMultiScopeCapture,
                RudeEditKind.DeleteLambdaWithMultiScopeCapture,
            };

            foreach (RudeEditKind value in Enum.GetValues(typeof(RudeEditKind)))
            {
                if (value == RudeEditKind.None)
                {
                    continue;
                }

                if (arg0.Contains(value))
                {
                    var re = new RudeEditDiagnostic(value, TextSpan.FromBounds(1, 2));
                    var d = re.ToDiagnostic(tree);
                    Assert.False(d.GetMessage().Contains("{"), value.ToString());
                }
                else if (arg2.Contains(value))
                {
                    var re = new RudeEditDiagnostic(value, TextSpan.FromBounds(1, 2), syntaxNode, new[] { "<1>", "<2>" });
                    var d = re.ToDiagnostic(tree);
                    Assert.True(d.GetMessage().Contains("<1>"), value.ToString());
                    Assert.True(d.GetMessage().Contains("<2>"), value.ToString());
                    Assert.False(d.GetMessage().Contains("{"), value.ToString());
                }
                else if (arg3.Contains(value))
                {
                    var re = new RudeEditDiagnostic(value, TextSpan.FromBounds(1, 2), syntaxNode, new[] { "<1>", "<2>", "<3>" });
                    var d = re.ToDiagnostic(tree);
                    Assert.True(d.GetMessage().Contains("<1>"), value.ToString());
                    Assert.True(d.GetMessage().Contains("<2>"), value.ToString());
                    Assert.True(d.GetMessage().Contains("<3>"), value.ToString());
                    Assert.False(d.GetMessage().Contains("{"), value.ToString());
                }
                else
                {
                    var re = new RudeEditDiagnostic(value, TextSpan.FromBounds(1, 2), syntaxNode, new[] { "<1>" });
                    var d = re.ToDiagnostic(tree);
                    Assert.True(d.GetMessage().Contains("<1>"), value.ToString());
                    Assert.False(d.GetMessage().Contains("{"), value.ToString());
                }
            }
        }
    }
}
