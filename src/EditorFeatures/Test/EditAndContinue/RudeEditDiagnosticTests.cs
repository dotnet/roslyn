// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditAndContinue
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
                RudeEditKind.STMT_MID_DELETE,
                RudeEditKind.STMT_NON_LEAF_DELETE,
                RudeEditKind.STMT_CTOR_CALL,
                RudeEditKind.STMT_FIELD_INIT,
                RudeEditKind.STMT_DELETE,
                RudeEditKind.STMT_DELETE_REMAP,
                RudeEditKind.STMT_READONLY,
                RudeEditKind.RUDE_NO_ACTIVE_STMT,
                RudeEditKind.RUDE_ACTIVE_STMT_DELETED,
                RudeEditKind.EXC_HANDLER_ERROR,
                RudeEditKind.EXC_FINALLY_ERROR,
                RudeEditKind.EXC_CATCH_ERROR,
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
                RudeEditKind.RUDE_EDIT_MODIFY_ANON_METHOD,
                RudeEditKind.RUDE_EDIT_ADD_ANON_METHOD,
                RudeEditKind.RUDE_EDIT_DELETE_ANON_METHOD,
                RudeEditKind.RUDE_EDIT_MOVE_ANON_METHOD,
                RudeEditKind.RUDE_EDIT_MODIFY_LAMBDA_EXPRESSION,
                RudeEditKind.RUDE_EDIT_ADD_LAMBDA_EXPRESSION,
                RudeEditKind.RUDE_EDIT_DELETE_LAMBDA_EXPRESSION,
                RudeEditKind.RUDE_EDIT_MOVE_LAMBDA_EXPRESSION,
                RudeEditKind.RUDE_EDIT_MODIFY_QUERY_EXPRESSION,
                RudeEditKind.RUDE_EDIT_ADD_QUERY_EXPRESSION,
                RudeEditKind.RUDE_EDIT_DELETE_QUERY_EXPRESSION,
                RudeEditKind.RUDE_EDIT_MOVE_QUERY_EXPRESSION,
                RudeEditKind.RUDE_EDIT_MODIFY_ANONYMOUS_TYPE,
                RudeEditKind.RUDE_EDIT_ADD_ANONYMOUS_TYPE,
                RudeEditKind.RUDE_EDIT_DELETE_ANONYMOUS_TYPE,
                RudeEditKind.RUDE_EDIT_MOVE_ANONYMOUS_TYPE,
                RudeEditKind.RUDE_EDIT_ADD_NEW_FILE,
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
            };

            var arg3 = new HashSet<RudeEditKind>()
            {
                RudeEditKind.InsertLambdaWithMultiScopeCapture,
                RudeEditKind.DeleteLambdaWithMultiScopeCapture,
            };

            List<RudeEditKind> errors = new List<RudeEditKind>();
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
