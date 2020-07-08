// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using FsCheck;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Xunit;
using static System.Environment;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    /// <summary>
    /// This class contains fuzzing code to generate random combinations of capturing of local
    /// functions and lambdas.
    /// </summary>
    public class CodeGenCapturing : CSharpTestBase
    {
        // FsCheck doesn't currently have a signed binary, so this only works on CoreClr
        [ConditionalFact(typeof(CoreClrOnly))]
        public void CompileSamples()
        {
            const string template = @"
using System;
using static System.Console;
class C
{{
    static void Main()
    {{
        {0}
    }}
}}";
            // We can customize the size of the input program, and
            // the number of programs we generate below
            foreach (StmtList p in Gen.Sample(4, 100, GenerateStmtList()))
            {
                var sb = new StringBuilder();
                p.Print(sb, indent: 0);

                var src = string.Format(template, sb.ToString());
                CompileAndVerify(src, options: TestOptions.ReleaseExe);
            }
        }

        /// Data model: the following type hierarchy represents a simple
        /// statement list, where a statement is either a variable
        /// declaration, variable use, or a function, which is either a
        /// lambda or local function which is immediately declared and
        /// executed.
        #region types

        abstract record Stmt
        {
            public abstract void Print(StringBuilder b, int indent);
        }
        partial record VariableDecl(string Name) : Stmt;
        partial record Inc(VariableDecl Var) : Stmt;
        partial record LocalFunc(string Name, StmtList Stmts) : Stmt;
        partial record Lambda(StmtList Stmts) : Stmt;

        partial record StmtList(Stmt[] Stmts)
        {
            public readonly static StmtList Empty = new StmtList(Array.Empty<Stmt>());
        }

        #endregion

        private static Gen<StmtList> GenerateStmtList()
        {
            return Gen.Sized(size => helper(
                size,
                ImmutableList<VariableDecl>.Empty,
                funcCounter: 0));

            static Gen<StmtList> helper(
                int size,
                ImmutableList<VariableDecl> varsInScope,
                int funcCounter)
            {
                // Each nested statement list contains:
                // - newly declared vars
                // - uses of existing vars
                // - new nested functions
                var genNewVars = Gen.Choose(0, 2).Select(count =>
                {
                    var builder = ImmutableList.CreateBuilder<VariableDecl>();
                    for (int i = 0; i < count; i++)
                    {
                        builder.Add(new VariableDecl($"i{i + varsInScope.Count}"));
                    }
                    return builder.ToImmutableList();
                });
                var genIncs = Gen.Resize(2, Gen.SubListOf(varsInScope.AsEnumerable())
                    .Select(vars => vars.Select(v => (Stmt)new Inc(v))));
                return from newVars in genNewVars
                       from incs in genIncs
                       from func in genFunc(size, varsInScope.AddRange(newVars), funcCounter)
                       let stmtsWithoutFunc = newVars.Concat(incs)
                       let stmts = func is object ? stmtsWithoutFunc.Append(func) : stmtsWithoutFunc
                       select new StmtList(stmts.ToArray());
            }

            static Gen<Stmt?> genFunc(int size, ImmutableList<VariableDecl> varsInScope, int funcCounter)
            {
                if (size == 0)
                    return Gen.Constant<Stmt?>(null);

                return Arb.Generate<bool>().SelectMany(isLambda =>
                {
                    var newCounter = isLambda ? funcCounter : funcCounter + 1;
                    return helper(size / 2, varsInScope, newCounter).Select(nestedStmts =>
                        isLambda
                            ? (Stmt?)new Lambda(nestedStmts)
                            : (Stmt?)new LocalFunc($"f{funcCounter}", nestedStmts)
                    );
                });
            }
        }

        #region printing

        internal static void Append(StringBuilder b, int indent, string val)
        {
            b.Append(' ', indent * 4);
            b.Append(val);
        }

        internal static void AppendLine(StringBuilder b, int indent, string val)
        {
            Append(b, indent, val);
            b.AppendLine();
        }

        partial record StmtList
        {
            public void Print(StringBuilder b, int indent)
            {
                foreach (var stmt in Stmts)
                {
                    stmt.Print(b, indent);
                }
                // Now print values of variables declared in this scope
                foreach (var stmt in Stmts)
                {
                    if (stmt is VariableDecl v)
                    {
                        AppendLine(b, indent, $"WriteLine({v.Name});");
                    }
                }
            }
        }
        partial record VariableDecl
        {
            public override void Print(StringBuilder b, int indent)
            {
                AppendLine(b, indent, $"int {Name} = 0;");
            }
        }
        partial record Inc
        {
            public override void Print(StringBuilder b, int indent)
            {
                AppendLine(b, indent, $"{Var.Name}++;");
            }
        }
        partial record LocalFunc
        {
            public override void Print(StringBuilder b, int indent)
            {
                AppendLine(b, indent, $"void {Name}()");
                AppendLine(b, indent, "{");
                Stmts.Print(b, indent + 1);
                AppendLine(b, indent, "}");
                AppendLine(b, indent, $"{Name}();");
            }
        }

        partial record Lambda
        {
            public override void Print(StringBuilder b, int indent)
            {
                AppendLine(b, indent, $"((Action)(() =>");
                AppendLine(b, indent, "{");
                Stmts.Print(b, indent + 1);
                AppendLine(b, indent, "}))();");
            }
        }

        #endregion
    }
}
