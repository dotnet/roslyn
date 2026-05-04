// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

public sealed class RudeEditDiagnosticTests
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
            RudeEditKind.UpdateExceptionHandlerOfActiveTry,
            RudeEditKind.UpdateTryOrCatchWithActiveFinally,
            RudeEditKind.UpdateCatchHandlerAroundActiveStatement,
            RudeEditKind.FieldKindUpdate,
            RudeEditKind.TypeKindUpdate,
            RudeEditKind.AccessorKindUpdate,
            RudeEditKind.DeclareLibraryUpdate,
            RudeEditKind.DeclareAliasUpdate,
            RudeEditKind.InsertDllImport,
            RudeEditKind.GenericMethodUpdate,
            RudeEditKind.GenericTypeUpdate,
            RudeEditKind.ExperimentalFeaturesEnabled,
            RudeEditKind.AwaitStatementUpdate,
            RudeEditKind.InsertFile,
            RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas,
            RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement,
            RudeEditKind.SwitchBetweenLambdaAndLocalFunction,
            RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier,
            RudeEditKind.NotSupportedByRuntime,
            RudeEditKind.MakeMethodAsyncNotSupportedByRuntime,
            RudeEditKind.MakeMethodIteratorNotSupportedByRuntime,
            RudeEditKind.ChangeImplicitMainReturnType,
            RudeEditKind.UpdatingStateMachineMethodNotSupportedByRuntime
        };

        var arg2 = new HashSet<RudeEditKind>()
        {
            RudeEditKind.InsertOrMoveStructMember,
            RudeEditKind.InsertOrMoveTypeWithLayoutMember,
            RudeEditKind.ChangingCapturedVariableType,
            RudeEditKind.RenamingCapturedVariable,
            RudeEditKind.ChangingStateMachineShape,
            RudeEditKind.InternalError,
            RudeEditKind.MemberBodyInternalError,
            RudeEditKind.ChangingNonCustomAttribute,
            RudeEditKind.NotCapturingPrimaryConstructorParameter
        };

        var arg3 = new HashSet<RudeEditKind>()
        {
            RudeEditKind.ChangingNamespace,
        };

        var allKinds = Enum.GetValues<RudeEditKind>();

        foreach (var kind in allKinds)
        {
            if (kind == RudeEditKind.None)
            {
                continue;
            }

            if (arg0.Contains(kind))
            {
                var re = new RudeEditDiagnostic(kind, TextSpan.FromBounds(1, 2));
                var d = re.ToDiagnostic(tree);
                Assert.False(d.GetMessage().Contains("{"), kind.ToString());
            }
            else if (arg2.Contains(kind))
            {
                var re = new RudeEditDiagnostic(kind, TextSpan.FromBounds(1, 2), syntaxNode, ["<1>", "<2>"]);
                var d = re.ToDiagnostic(tree);
                Assert.True(d.GetMessage().Contains("<1>"), kind.ToString());
                Assert.True(d.GetMessage().Contains("<2>"), kind.ToString());
                Assert.False(d.GetMessage().Contains("{"), kind.ToString());
            }
            else if (arg3.Contains(kind))
            {
                var re = new RudeEditDiagnostic(kind, TextSpan.FromBounds(1, 2), syntaxNode, ["<1>", "<2>", "<3>"]);
                var d = re.ToDiagnostic(tree);
                Assert.True(d.GetMessage().Contains("<1>"), kind.ToString());
                Assert.True(d.GetMessage().Contains("<2>"), kind.ToString());
                Assert.True(d.GetMessage().Contains("<3>"), kind.ToString());
                Assert.False(d.GetMessage().Contains("{"), kind.ToString());
            }
            else
            {
                var re = new RudeEditDiagnostic(kind, TextSpan.FromBounds(1, 2), syntaxNode, ["<1>"]);
                var d = re.ToDiagnostic(tree);
                Assert.True(d.GetMessage().Contains("<1>"), kind.ToString());
                Assert.False(d.GetMessage().Contains("{"), kind.ToString());
            }
        }

        // check that all values are unique:
        AssertEx.Equal(allKinds, allKinds.Distinct());
    }
}
