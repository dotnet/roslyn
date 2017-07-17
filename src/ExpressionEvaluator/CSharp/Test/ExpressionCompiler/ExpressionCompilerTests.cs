﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;
using CommonResources = Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests.Resources;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ExpressionCompilerTests : ExpressionCompilerTestBase
    {
        /// <summary>
        /// Each assembly should have a unique MVID and assembly name.
        /// </summary>
        [WorkItem(1029280, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029280")]
        [Fact]
        public void UniqueModuleVersionId()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                ImmutableArray<MetadataBlock> blocks;
                Guid moduleVersionId;
                ISymUnmanagedReader symReader;
                int methodToken;
                int localSignatureToken;
                GetContextState(runtime, "C.M", out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);

                uint ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader);
                var context = EvaluationContext.CreateMethodContext(
                    default(CSharpMetadataContext),
                    blocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ilOffset,
                    localSignatureToken: localSignatureToken);

                string error;
                var result = context.CompileExpression("1", out error);
                var mvid1 = result.Assembly.GetModuleVersionId();
                var name1 = result.Assembly.GetAssemblyName();
                Assert.NotEqual(mvid1, Guid.Empty);

                context = EvaluationContext.CreateMethodContext(
                    new CSharpMetadataContext(blocks, context),
                    blocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ilOffset,
                    localSignatureToken: localSignatureToken);

                result = context.CompileExpression("2", out error);
                var mvid2 = result.Assembly.GetModuleVersionId();
                var name2 = result.Assembly.GetAssemblyName();
                Assert.NotEqual(mvid2, Guid.Empty);
                Assert.NotEqual(mvid2, mvid1);
                Assert.NotEqual(name2.FullName, name1.FullName);
            });
        }

        [Fact]
        public void ParseError()
        {
            var source =
@"class C
{
    static void M() { }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var result = context.CompileExpression("M(", out error);
                Assert.Null(result);
                Assert.Equal(error, "error CS1026: ) expected");
            });
        }

        /// <summary>
        /// Diagnostics should be formatted with the CurrentUICulture.
        /// </summary>
        [WorkItem(941599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/941599")]
        [Fact]
        public void FormatterCultureInfo()
        {
            var previousCulture = Thread.CurrentThread.CurrentCulture;
            var previousUICulture = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            try
            {
                var source =
@"class C
{
    static void M() { }
}";
                var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
                WithRuntimeInstance(compilation0, runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.M");
                    ResultProperties resultProperties;
                    string error;
                    ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                    var result = context.CompileExpression(
                        "M(",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        CustomDiagnosticFormatter.Instance,
                        out resultProperties,
                        out error,
                        out missingAssemblyIdentities,
                        preferredUICulture: null,
                        testData: null);
                    Assert.Null(result);
                    Assert.Equal(error, "LCID=1031, Code=1026");
                    Assert.Empty(missingAssemblyIdentities);
                });
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = previousUICulture;
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }

        /// <summary>
        /// Compile should succeed if there are
        /// parse warnings but no errors.
        /// </summary>
        [Fact]
        public void ParseWarning()
        {
            var source =
@"class C
{
    static void M() { }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                // (1,2): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                const string expr = "0l";
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression(expr, out error, testData);
                Assert.NotNull(result.Assembly);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i8
  IL_0002:  ret
}");
            });
        }

        /// <summary>
        /// Reference to local in another scope.
        /// </summary>
        [Fact]
        public void BindingError()
        {
            var source =
@"class C
{
    static void M(object o)
    {
        var a = new object[0];
        foreach (var x in a)
        {
            M(x);
        }
        foreach (var y in a)
        {
#line 999
            M(y);
        }
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                atLineNumber: 999,
                expr: "y ?? x",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0103: The name 'x' does not exist in the current context");
        }

        [Fact]
        public void EmitError()
        {
            var longName = new string('P', 1100);
            var source =
@"class C
{
    static void M(object o)
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: string.Format("new {{ {0} = o }}", longName),
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, string.Format("error CS7013: Name '<{0}>i__Field' exceeds the maximum length allowed in metadata.", longName));
        }

        [Fact]
        public void NoSymbols()
        {
            var source =
@"class C
{
    static object F(object o)
    {
        return o;
    }
    static void M(int x)
    {
        int y = x + 1;
    }
}";
            var compilation0 = CSharpTestBase.CreateStandardCompilation(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0, debugFormat: 0);
            foreach (var module in runtime.Modules)
            {
                Assert.Null(module.SymReader);
            }
            var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
            // Local reference.
            string error;
            var testData = new CompilationTestData();
            var result = context.CompileExpression("F(y)", out error, testData);
            Assert.Equal(error, "error CS0103: The name 'y' does not exist in the current context");
            // No local reference.
            testData = new CompilationTestData();
            result = context.CompileExpression("F(x)", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        ""int""
  IL_0006:  call       ""object C.F(object)""
  IL_000b:  ret
}");
        }

        /// <summary>
        /// Reuse Compilation if references match, and reuse entire
        /// EvaluationContext if references and local scopes match.
        /// </summary>
        [Fact]
        public void ReuseEvaluationContext()
        {
            var sourceA =
@"public interface I
{
}";
            var sourceB =
@"class C
{
    static void F(I o)
    {
        object x = 1;
        if (o == null)
        {
            object y = 2;
            y = x;
        }
        else
        {
            object z;
        }
        x = 3;
    }
    static void G()
    {
    }
}";
            var compilationA = CreateStandardCompilation(sourceA, options: TestOptions.DebugDll);
            var referenceA = compilationA.EmitToImageReference();

            var compilationB = CreateStandardCompilation(
                sourceB,
                options: TestOptions.DebugDll,
                references: new MetadataReference[] { referenceA });

            const int methodVersion = 1;

            var referencesB = new[] { MscorlibRef, referenceA };
            var moduleB = compilationB.ToModuleInstance();

            CSharpMetadataContext previous = default(CSharpMetadataContext);
            int startOffset;
            int endOffset;
            var runtime = CreateRuntimeInstance(moduleB, referencesB);
            ImmutableArray<MetadataBlock> typeBlocks;
            ImmutableArray<MetadataBlock> methodBlocks;
            Guid moduleVersionId;
            ISymUnmanagedReader symReader;
            int typeToken;
            int methodToken;
            int localSignatureToken;
            GetContextState(runtime, "C", out typeBlocks, out moduleVersionId, out symReader, out typeToken, out localSignatureToken);
            GetContextState(runtime, "C.F", out methodBlocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);

            // Get non-empty scopes.
            var scopes = symReader.GetScopes(methodToken, methodVersion, EvaluationContext.IsLocalScopeEndInclusive).WhereAsArray(s => s.Locals.Length > 0);
            Assert.True(scopes.Length >= 3);
            var outerScope = scopes.First(s => s.Locals.Contains("x"));

            startOffset = outerScope.StartOffset;
            endOffset = outerScope.EndOffset - 1;

            // At start of outer scope.
            var context = EvaluationContext.CreateMethodContext(previous, methodBlocks, symReader, moduleVersionId, methodToken, methodVersion, (uint)startOffset, localSignatureToken);
            Assert.Equal(default(CSharpMetadataContext), previous);
            previous = new CSharpMetadataContext(methodBlocks, context);

            // At end of outer scope - not reused because of the nested scope.
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, symReader, moduleVersionId, methodToken, methodVersion, (uint)endOffset, localSignatureToken);
            Assert.NotEqual(context, previous.EvaluationContext); // Not required, just documentary.

            // At type context.
            context = EvaluationContext.CreateTypeContext(previous, typeBlocks, moduleVersionId, typeToken);
            Assert.NotEqual(context, previous.EvaluationContext);
            Assert.Null(context.MethodContextReuseConstraints);
            Assert.Equal(context.Compilation, previous.Compilation);

            // Step through entire method.
            var previousScope = (Scope)null;
            previous = new CSharpMetadataContext(typeBlocks, context);
            for (int offset = startOffset; offset <= endOffset; offset++)
            {
                var scope = scopes.GetInnermostScope(offset);
                var constraints = previous.EvaluationContext.MethodContextReuseConstraints;
                if (constraints.HasValue)
                {
                    Assert.Equal(scope == previousScope, constraints.GetValueOrDefault().AreSatisfied(moduleVersionId, methodToken, methodVersion, offset));
                }

                context = EvaluationContext.CreateMethodContext(previous, methodBlocks, symReader, moduleVersionId, methodToken, methodVersion, (uint)offset, localSignatureToken);
                if (scope == previousScope)
                {
                    Assert.Equal(context, previous.EvaluationContext);
                }
                else
                {
                    // Different scope. Should reuse compilation.
                    Assert.NotEqual(context, previous.EvaluationContext);
                    if (previous.EvaluationContext != null)
                    {
                        Assert.NotEqual(context.MethodContextReuseConstraints, previous.EvaluationContext.MethodContextReuseConstraints);
                        Assert.Equal(context.Compilation, previous.Compilation);
                    }
                }
                previousScope = scope;
                previous = new CSharpMetadataContext(methodBlocks, context);
            }

            // With different references.
            var fewerReferences = new[] { MscorlibRef };
            runtime = CreateRuntimeInstance(moduleB, fewerReferences);
            GetContextState(runtime, "C.F", out methodBlocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);

            // Different references. No reuse.
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, symReader, moduleVersionId, methodToken, methodVersion, (uint)endOffset, localSignatureToken);
            Assert.NotEqual(context, previous.EvaluationContext);
            Assert.True(previous.EvaluationContext.MethodContextReuseConstraints.Value.AreSatisfied(moduleVersionId, methodToken, methodVersion, endOffset));
            Assert.NotEqual(context.Compilation, previous.Compilation);
            previous = new CSharpMetadataContext(methodBlocks, context);

            // Different method. Should reuse Compilation.
            GetContextState(runtime, "C.G", out methodBlocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, symReader, moduleVersionId, methodToken, methodVersion, ilOffset: 0, localSignatureToken: localSignatureToken);
            Assert.NotEqual(context, previous.EvaluationContext);
            Assert.False(previous.EvaluationContext.MethodContextReuseConstraints.Value.AreSatisfied(moduleVersionId, methodToken, methodVersion, 0));
            Assert.Equal(context.Compilation, previous.Compilation);

            // No EvaluationContext. Should reuse Compilation
            previous = new CSharpMetadataContext(previous.MetadataBlocks, previous.Compilation);
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, symReader, moduleVersionId, methodToken, methodVersion, ilOffset: 0, localSignatureToken: localSignatureToken);
            Assert.Null(previous.EvaluationContext);
            Assert.NotNull(context);
            Assert.Equal(context.Compilation, previous.Compilation);
        }

        /// <summary>
        /// Allow trailing semicolon after expression. This is to support
        /// copy/paste of (simple cases of) RHS of assignment in Watch window,
        /// not to allow arbitrary syntax after the semicolon, not even comments.
        /// </summary>
        [WorkItem(950242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/950242")]
        [Fact]
        public void TrailingSemicolon()
        {
            var source =
@"class C
{
    static object F(string x, string y)
    {
        return x;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.F");
                string error;
                var result = context.CompileExpression("x;", out error);
                Assert.Null(error);
                result = context.CompileExpression("x \t;\t ", out error);
                Assert.Null(error);
                // Multiple semicolons: not supported.
                result = context.CompileExpression("x;;", out error);
                Assert.Equal(error, "error CS1073: Unexpected token ';'");
                // // comments.
                result = context.CompileExpression("x;//", out error);
                Assert.Equal(error, "error CS0726: ';//' is not a valid format specifier");
                result = context.CompileExpression("x//;", out error);
                Assert.Null(error);
                // /*...*/ comments.
                result = context.CompileExpression("x/*...*/", out error);
                Assert.Null(error);
                result = context.CompileExpression("x/*;*/", out error);
                Assert.Null(error);
                result = context.CompileExpression("x;/*...*/", out error);
                Assert.Equal(error, "error CS0726: ';/*...*/' is not a valid format specifier");
                result = context.CompileExpression("x/*...*/;", out error);
                Assert.Null(error);
                // Trailing semicolon, no expression.
                result = context.CompileExpression(" ; ", out error);
                Assert.Equal(error, "error CS1733: Expected expression");
            });
        }

        [Fact]
        public void FormatSpecifiers()
        {
            var source =
@"class C
{
    static object F(string x, string y)
    {
        return x;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.F");
                string error;
                // No format specifiers.
                var result = context.CompileExpression("x", out error);
                CheckFormatSpecifiers(result);
                // Format specifiers on expression.
                result = context.CompileExpression("x,", out error);
                Assert.Equal(error, "error CS0726: ',' is not a valid format specifier");
                result = context.CompileExpression("x,,", out error);
                Assert.Equal(error, "error CS0726: ',' is not a valid format specifier");
                result = context.CompileExpression("x y", out error);
                Assert.Equal(error, "error CS0726: 'y' is not a valid format specifier");
                result = context.CompileExpression("x yy zz", out error);
                Assert.Equal(error, "error CS0726: 'yy' is not a valid format specifier");
                result = context.CompileExpression("x,,y", out error);
                Assert.Equal(error, "error CS0726: ',' is not a valid format specifier");
                result = context.CompileExpression("x,yy,zz,ww", out error);
                CheckFormatSpecifiers(result, "yy", "zz", "ww");
                result = context.CompileExpression("x, y z", out error);
                Assert.Equal(error, "error CS0726: 'z' is not a valid format specifier");
                result = context.CompileExpression("x, y  ,  z  ", out error);
                CheckFormatSpecifiers(result, "y", "z");
                result = context.CompileExpression("x, y, z,", out error);
                Assert.Equal(error, "error CS0726: ',' is not a valid format specifier");
                result = context.CompileExpression("x,y,z;w", out error);
                Assert.Equal(error, "error CS0726: 'z;w' is not a valid format specifier");
                result = context.CompileExpression("x, y;, z", out error);
                Assert.Equal(error, "error CS0726: 'y;' is not a valid format specifier");
                // Format specifiers after // comment: ignored.
                result = context.CompileExpression("x // ,f", out error);
                CheckFormatSpecifiers(result);
                // Format specifiers after /*...*/ comment.
                result = context.CompileExpression("x /*,f*/, g, h", out error);
                CheckFormatSpecifiers(result, "g", "h");
                // Format specifiers on assignment value.
                result = context.CompileAssignment("x", "null, y", out error);
                Assert.Null(result);
                Assert.Equal(error, "error CS1073: Unexpected token ','");
                // Trailing semicolon, no format specifiers.
                result = context.CompileExpression("x; ", out error);
                CheckFormatSpecifiers(result);
                // Format specifiers, no expression.
                result = context.CompileExpression(",f", out error);
                Assert.Equal(error, "error CS1525: Invalid expression term ','");
                // Format specifiers before semicolon: not supported.
                result = context.CompileExpression("x,f;\t", out error);
                Assert.Equal(error, "error CS1073: Unexpected token ','");
                // Format specifiers after semicolon: not supported.
                result = context.CompileExpression("x;,f", out error);
                Assert.Equal(error, "error CS0726: ';' is not a valid format specifier");
                result = context.CompileExpression("x; f, g", out error);
                Assert.Equal(error, "error CS0726: ';' is not a valid format specifier");
            });
        }

        private static void CheckFormatSpecifiers(CompileResult result, params string[] formatSpecifiers)
        {
            Assert.NotNull(result.Assembly);
            if (formatSpecifiers.Length == 0)
            {
                Assert.Null(result.FormatSpecifiers);
            }
            else
            {
                Assert.Equal(formatSpecifiers, result.FormatSpecifiers);
            }
        }

        /// <summary>
        /// Locals in generated method should account for
        /// temporary slots in the original method. Also, some
        /// temporaries may not be included in any scope.
        /// </summary>
        [Fact]
        public void IncludeTemporarySlots()
        {
            var source =
@"class C
{
    static string F(int[] a)
    {
        lock (new C())
        {
#line 999
            string s = a[0].ToString();
            return s;
        }
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.F", atLineNumber: 999);

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("a[0]", out error, testData);

                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (C V_0,
                bool V_1,
                string V_2, //s
                string V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.i4
  IL_0003:  ret
}");
            });
        }

        [Fact]
        public void EvaluateThis()
        {
            var source =
@"class A
{
    internal virtual object F() { return null; }
    internal object G;
    internal virtual object P { get { return null; } }
}
class B : A
{
    internal override object F() { return null; }
    internal new object G;
    internal override object P { get { return null; } }
    static object F(System.Func<object> f) { return null; }
    void M()
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "B.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("this.F() ?? this.G ?? this.P", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""object B.F()""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001a
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""object B.G""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  pop
  IL_0014:  ldarg.0
  IL_0015:  callvirt   ""object B.P.get""
  IL_001a:  ret
}");
                testData = new CompilationTestData();
                result = context.CompileExpression("F(this.F)", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""object B.F()""
  IL_0008:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_000d:  call       ""object B.F(System.Func<object>)""
  IL_0012:  ret
}");
                testData = new CompilationTestData();
                result = context.CompileExpression("F(new System.Func<object>(this.F))", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""object B.F()""
  IL_0008:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_000d:  call       ""object B.F(System.Func<object>)""
  IL_0012:  ret
}");
            });
        }

        [Fact]
        public void EvaluateBase()
        {
            var source =
@"class A
{
    internal virtual object F() { return null; }
    internal object G;
    internal virtual object P { get { return null; } }
}
class B : A
{
    internal override object F() { return null; }
    internal new object G;
    internal override object P { get { return null; } }
    static object F(System.Func<object> f) { return null; }
    void M()
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "B.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("base.F() ?? base.G ?? base.P", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object A.F()""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001a
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""object A.G""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  pop
  IL_0014:  ldarg.0
  IL_0015:  call       ""object A.P.get""
  IL_001a:  ret
}");
                testData = new CompilationTestData();
                result = context.CompileExpression("F(base.F)", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""object A.F()""
  IL_0007:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""object B.F(System.Func<object>)""
  IL_0011:  ret
}");
                testData = new CompilationTestData();
                result = context.CompileExpression("F(new System.Func<object>(base.F))", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""object A.F()""
  IL_0007:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""object B.F(System.Func<object>)""
  IL_0011:  ret
}");
            });
        }

        /// <summary>
        /// If "this" is a struct, the generated parameter
        /// should be passed by reference.
        /// </summary>
        [Fact]
        public void EvaluateStructThis()
        {
            var source =
@"struct S
{
    static object F(object x, object y)
    {
        return null;
    }
    object x;
    void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "S.M",
                expr: "F(this, this.x)");
            var methodData = testData.GetMethodData("<>x.<>m0(ref S)");
            var parameter = ((MethodSymbol)methodData.Method).Parameters[0];
            Assert.Equal(parameter.RefKind, RefKind.Ref);
            methodData.VerifyIL(
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""S""
  IL_0006:  box        ""S""
  IL_000b:  ldarg.0
  IL_000c:  ldfld      ""object S.x""
  IL_0011:  call       ""object S.F(object, object)""
  IL_0016:  ret
}");
        }

        [Fact]
        public void EvaluateStaticMethodParameters()
        {
            var source =
@"class C
{
    static object F(int x, int y)
    {
        return x + y;
    }
    static void M(int x, int y)
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "F(y, x)");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  call       ""object C.F(int, int)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void EvaluateInstanceMethodParametersAndLocals()
        {
            var source =
@"class C
{
    object F(int x)
    {
        return x;
    }
    void M(int x)
    {
#line 999
        int y = 1;
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                atLineNumber: 999,
                expr: "F(x + y)");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       10 (0xa)
  .maxstack  3
  .locals init (int V_0) //y
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldloc.0
  IL_0003:  add
  IL_0004:  callvirt   ""object C.F(int)""
  IL_0009:  ret
}");
        }

        [Fact]
        public void EvaluateLocals()
        {
            var source =
@"class C
{
    static void M()
    {
        int x = 1;
        if (x < 0)
        {
            int y = 2;
        }
        else
        {
#line 999
            int z = 3;
        }
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                atLineNumber: 999,
                expr: "x + z");

            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (int V_0, //x
                bool V_1,
                int V_2,
                int V_3) //z
  IL_0000:  ldloc.0
  IL_0001:  ldloc.3
  IL_0002:  add
  IL_0003:  ret
}");
        }

        [Fact]
        public void EvaluateForEachLocal()
        {
            var source =
@"class C
{
    static bool F(object[] args)
    {
        if (args == null)
        {
            return true;
        }
        foreach (var o in args)
        {
#line 999
        }
        return false;
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.F",
                atLineNumber: 999,
                expr: "o");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (bool V_0,
  bool V_1,
  object[] V_2,
  int V_3,
  object V_4) //o
  IL_0000:  ldloc.s    V_4
  IL_0002:  ret
}");
        }

        /// <summary>
        /// Generated "this" parameter should not
        /// conflict with existing "@this" parameter.
        /// </summary>
        [Fact]
        public void ParameterNamedThis()
        {
            var source =
@"class C
{
    object M(C @this)
    {
        return null;
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "@this.M(this)");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""object C.M(C)""
  IL_0007:  ret
}");
        }

        /// <summary>
        /// Generated "this" parameter should not
        /// conflict with existing "@this" local.
        /// </summary>
        [Fact]
        public void LocalNamedThis()
        {
            var source =
@"class C
{
    object M(object o)
    {
        var @this = this;
        return null;
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "@this.M(this)");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (C V_0, //this
                object V_1)
  IL_0000:  ldloc.0
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""object C.M(object)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void ByRefParameter()
        {
            var source =
@"class C
{
    static object M(out object x)
    {
        object y;
        x = null;
        return null;
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "M(out y)");
            var methodData = testData.GetMethodData("<>x.<>m0(out object)");
            var parameter = ((MethodSymbol)methodData.Method).Parameters[0];
            Assert.Equal(parameter.RefKind, RefKind.Out);
            methodData.VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (object V_0, //y
                object V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""object C.M(out object)""
  IL_0007:  ret
}");
        }

        /// <summary>
        /// Method defined in IL where PDB does not
        /// contain C# custom metadata.
        /// </summary>
        [Fact]
        public void LocalType_FromIL()
        {
            var source =
@".class public C
{
  .method public specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .field public object F;
  .method public static void M()
  {
    .locals init ([0] class C c)
    ret
  }
}";
            var module = ExpressionCompilerTestHelpers.GetModuleInstanceForIL(source);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var context = CreateMethodContext(runtime, methodName: "C.M");

            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("c.F", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C V_0) //c
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""object C.F""
  IL_0006:  ret
}");
        }

        /// <summary>
        /// Allow locals with optional custom modifiers. 
        /// </summary>
        /// <remarks>
        /// The custom modifiers are not copied to the corresponding
        /// local in the generated method since there is no need.
        /// </remarks>
        [WorkItem(884627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884627")]
        [Fact]
        public void LocalType_CustomModifiers()
        {
            var source =
@".class public C
{
  .method public specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .field public object F;
  .method public static void M()
  {
    .locals init ([0] class C modopt(int32) modopt(object) c)
    ret
  }
}";
            var module = ExpressionCompilerTestHelpers.GetModuleInstanceForIL(source);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var context = CreateMethodContext(runtime, "C.M");

            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("c.F", out error, testData);
            var methodData = testData.GetMethodData("<>x.<>m0");
            var locals = methodData.ILBuilder.LocalSlotManager.LocalsInOrder();
            var local = locals[0];
            Assert.Equal(local.Type.ToString(), "C");
            Assert.Equal(local.CustomModifiers.Length, 0); // Custom modifiers are not copied.
            methodData.VerifyIL(
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C V_0) //c
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""object C.F""
  IL_0006:  ret
}");
        }

        [WorkItem(1012956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1012956")]
        [Fact]
        public void LocalType_ByRefOrPinned()
        {
            var source = @"
.class private auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method private hidebysig static void  M(string s, int32[] a) cil managed
  {
    // Code size       73 (0x49)
    .maxstack  2
    .locals init ([0] string pinned s,
                  [1] int32& pinned f,
                  [2] int32& i)
    ret
  }
}
";
            var module = ExpressionCompilerTestHelpers.GetModuleInstanceForIL(source);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var context = CreateMethodContext(runtime, "C.M");

            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("s", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (pinned string V_0, //s
                pinned int& V_1, //f
                int& V_2) //i
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
            testData = new CompilationTestData();
            context.CompileAssignment("s", "\"hello\"", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (pinned string V_0, //s
                pinned int& V_1, //f
                int& V_2) //i
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.0
  IL_0006:  ret
}");
            testData = new CompilationTestData();
            context.CompileExpression("f", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (pinned string V_0, //s
                pinned int& V_1, //f
                int& V_2) //i
  IL_0000:  ldloc.1
  IL_0001:  ret
}");
            testData = new CompilationTestData();
            context.CompileAssignment("f", "1", out error, testData);
            Assert.Equal("error CS1656: Cannot assign to 'f' because it is a 'fixed variable'", error);

            testData = new CompilationTestData();
            context.CompileExpression("i", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (pinned string V_0, //s
                pinned int& V_1, //f
                int& V_2) //i
  IL_0000:  ldloc.2
  IL_0001:  ldind.i4
  IL_0002:  ret
}");
            testData = new CompilationTestData();
            context.CompileAssignment("i", "1", out error, testData);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (pinned string V_0, //s
                pinned int& V_1, //f
                int& V_2) //i
  IL_0000:  ldloc.2
  IL_0001:  ldc.i4.1
  IL_0002:  stind.i4
  IL_0003:  ret
}");
        }

        [Fact]
        public void LocalType_FixedVariable()
        {
            var source =
@"class C
{
    static int x;
    static unsafe void M(string s, int[] a)
    {
        fixed (char* p1 = s)
        {
            fixed (int* p2 = &x)
            {
                fixed (void* p3 = a)
                {
#line 999
                    int y = x + 1;
                }
            }
        }
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.UnsafeDebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M", atLineNumber: 999);

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("(int)p1[0] + p2[0] + ((int*)p3)[0]", out error, testData);

                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (char* V_0, //p1
                pinned string V_1,
                int* V_2, //p2
                pinned int& V_3,
                void* V_4, //p3
                pinned int[] V_5,
                int V_6) //y
  IL_0000:  ldloc.0
  IL_0001:  ldind.u2
  IL_0002:  ldloc.2
  IL_0003:  ldind.i4
  IL_0004:  add
  IL_0005:  ldloc.s    V_4
  IL_0007:  ldind.i4
  IL_0008:  add
  IL_0009:  ret
}");
            });
        }

        [WorkItem(1034549, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1034549")]
        [Fact]
        public void AssignLocal()
        {
            var source =
@"class C
{
    static void M()
    {
        int x = 0;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileAssignment(
                    target: "x",
                    expr: "1",
                    error: out error,
                    testData: testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ret
}");
                // Assign to a local, as above, but in an expression.
                testData = new CompilationTestData();
                context.CompileExpression(
                    expr: "x = 1",
                    error: out error,
                    testData: testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ret
}");
            });
        }

        [Fact]
        public void AssignInstanceMethodParametersAndLocals()
        {
            var source =
@"class C
{
    object[] a;
    static int F(int x)
    {
        return x;
    }
    void M(int x)
    {
        int y;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileAssignment(
                    target: "this.a[F(x)]",
                    expr: "this.a[y]",
                    error: out error,
                    testData: testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       22 (0x16)
  .maxstack  4
  .locals init (int V_0) //y
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object[] C.a""
  IL_0006:  ldarg.1
  IL_0007:  call       ""int C.F(int)""
  IL_000c:  ldarg.0
  IL_000d:  ldfld      ""object[] C.a""
  IL_0012:  ldloc.0
  IL_0013:  ldelem.ref
  IL_0014:  stelem.ref
  IL_0015:  ret
}");
            });
        }

        [Fact]
        public void EvaluateNull()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "null",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.ReadOnlyResult);
            var methodData = testData.GetMethodData("<>x.<>m0");
            var method = (MethodSymbol)methodData.Method;
            Assert.Equal(method.ReturnType.SpecialType, SpecialType.System_Object);
            Assert.False(method.ReturnsVoid);
            methodData.VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
        }

        [Fact]
        public void MayHaveSideEffects()
        {
            var source =
@"using System;
using System.Diagnostics.Contracts;
class C
{
    object F()
    {
        return 1;
    }
    [Pure]
    object G()
    {
        return 2;
    }
    object P { get; set; }
    static object H()
    {
        return 3;
    }
    static void M(C o, int i)
    {
        ((dynamic)o).G();
    }
}";
            var compilation0 = CreateStandardCompilation(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemCoreRef, CSharpRef });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
                CheckResultFlags(context, "o.F()", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                // Calls to methods are reported as having side effects, even if
                // the method is marked [Pure]. This matches the native EE.
                CheckResultFlags(context, "o.G()", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "o.P", DkmClrCompilationResultFlags.None);
                CheckResultFlags(context, "o.P = 2", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "((dynamic)o).G()", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "(Action)(() => { })", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "++i", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "--i", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "i++", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "i--", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "i += 2", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "i *= 3", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "new C() { P = 1 }", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "new C() { P = H() }", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            });
        }

        [Fact]
        public void IsAssignable()
        {
            var source = @"
using System;
class C
{
    int F;
    readonly int RF;
    const int CF = 1;
    
    event System.Action E;
    event System.Action CE { add { } remove { } }

    int RP { get { return 0; } }
    int WP { set { } }
    int RWP { get; set; }

    int this[int x] { get { return 0; } }
    int this[int x, int y] { set { } }
    int this[int x, int y, int z] { get { return 0; } set { } }

    int M() { return 0; }
        }
";
            var compilation0 = CreateStandardCompilation(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemCoreRef, CSharpRef });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                CheckResultFlags(context, "F", DkmClrCompilationResultFlags.None);
                CheckResultFlags(context, "RF", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "CF", DkmClrCompilationResultFlags.ReadOnlyResult);

                // Note: flags are always None in error cases.
                // CheckResultFlags(context, "E", DkmClrCompilationResultFlags.None); // TODO: DevDiv #1055825
                CheckResultFlags(context, "CE", DkmClrCompilationResultFlags.None, "error CS0079: The event 'C.CE' can only appear on the left hand side of += or -=");

                CheckResultFlags(context, "RP", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "WP", DkmClrCompilationResultFlags.None, "error CS0154: The property or indexer 'C.WP' cannot be used in this context because it lacks the get accessor");
                CheckResultFlags(context, "RWP", DkmClrCompilationResultFlags.None);

                CheckResultFlags(context, "this[1]", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "this[1, 2]", DkmClrCompilationResultFlags.None, "error CS0154: The property or indexer 'C.this[int, int]' cannot be used in this context because it lacks the get accessor");
                CheckResultFlags(context, "this[1, 2, 3]", DkmClrCompilationResultFlags.None);

                CheckResultFlags(context, "M()", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);

                CheckResultFlags(context, "null", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "1", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "M", DkmClrCompilationResultFlags.None, "error CS0428: Cannot convert method group 'M' to non-delegate type 'object'. Did you intend to invoke the method?");
                CheckResultFlags(context, "typeof(C)", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "new C()", DkmClrCompilationResultFlags.ReadOnlyResult);
            });
        }

        [Fact]
        public void IsAssignable_Array()
        {
            var source = @"
using System;
class C
{
    readonly int[] RF = new int[1];

    int[] rp = new int[2];
    int[] RP { get { return rp; } }

    int[] m = new int[3];
    int[] M() { return m; }
}
";
            var compilation0 = CreateStandardCompilation(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemCoreRef, CSharpRef });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                CheckResultFlags(context, "RF", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "RF[0]", DkmClrCompilationResultFlags.None);

                CheckResultFlags(context, "RP", DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "RP[0]", DkmClrCompilationResultFlags.None);

                CheckResultFlags(context, "M()", DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
                CheckResultFlags(context, "M()[0]", DkmClrCompilationResultFlags.PotentialSideEffect);
            });
        }

        private static void CheckResultFlags(EvaluationContext context, string expr, DkmClrCompilationResultFlags expectedFlags, string expectedError = null)
        {
            ResultProperties resultProperties;
            string error;
            var testData = new CompilationTestData();
            var result = context.CompileExpression(expr, out resultProperties, out error, testData);
            Assert.Equal(expectedError, error);
            Assert.NotEqual(expectedError == null, result == null);
            Assert.Equal(expectedFlags, resultProperties.Flags);
        }

        /// <summary>
        /// Set BooleanResult for bool expressions.
        /// </summary>
        [Fact]
        public void EvaluateBooleanExpression()
        {
            var source =
@"class C
{
    static bool F()
    {
        return false;
    }
    static void M(bool x, bool? y)
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                ResultProperties resultProperties;
                string error;
                context.CompileExpression("x", out resultProperties, out error);
                Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.BoolResult);
                context.CompileExpression("y", out resultProperties, out error);
                Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.None);
                context.CompileExpression("(bool)y", out resultProperties, out error);
                Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.BoolResult | DkmClrCompilationResultFlags.ReadOnlyResult);
                context.CompileExpression("!y", out resultProperties, out error);
                Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.ReadOnlyResult);
                context.CompileExpression("false", out resultProperties, out error);
                Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.BoolResult | DkmClrCompilationResultFlags.ReadOnlyResult);
                context.CompileExpression("F()", out resultProperties, out error);
                Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.BoolResult | DkmClrCompilationResultFlags.ReadOnlyResult | DkmClrCompilationResultFlags.PotentialSideEffect);
            });
        }

        /// <summary>
        /// Expression that is not an rvalue.
        /// </summary>
        [Fact]
        public void EvaluateNonRValueExpression()
        {
            var source =
@"class C
{
    object P { set { } }
    void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "P",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor");
        }

        /// <summary>
        /// Expression that does not return a value.
        /// </summary>
        [Fact]
        public void EvaluateVoidExpression()
        {
            var source =
@"class C
{
    void M()
    {
    }
}";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "this.M()",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            var methodData = testData.GetMethodData("<>x.<>m0");
            var method = (MethodSymbol)methodData.Method;
            Assert.Equal(method.ReturnType.SpecialType, SpecialType.System_Void);
            Assert.True(method.ReturnsVoid);

            methodData.VerifyIL(
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""void C.M()""
  IL_0006:  ret
}");
        }

        [Fact]
        public void EvaluateMethodGroup()
        {
            var source =
@"class C
{
    void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "this.M",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0428: Cannot convert method group 'M' to non-delegate type 'object'. Did you intend to invoke the method?");
        }

        [Fact]
        public void AssignMethodGroup()
        {
            var source =
@"class C
{
    static void M()
    {
        object o;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileAssignment(
                    target: "o",
                    expr: "M",
                    error: out error,
                    testData: testData);
                Assert.Equal(error, "error CS0428: Cannot convert method group 'M' to non-delegate type 'object'. Did you intend to invoke the method?");
            });
        }

        [Fact]
        public void EvaluateConstant()
        {
            var source =
@"class C
{
    static void M()
    {
        const string x = ""str"";
        const int y = 2;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("x[y]", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldstr      ""str""
  IL_0005:  ldc.i4.2
  IL_0006:  call       ""char string.this[int].get""
  IL_000b:  ret
}");
            });
        }

        [Fact]
        public void AssignToConstant()
        {
            var source =
@"class C
{
    static void M()
    {
        const int x = 1;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                runtime,
                methodName: "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileAssignment(
                    target: "x",
                    expr: "2",
                    error: out error,
                    testData: testData);
                Assert.Equal(error, "error CS0131: The left-hand side of an assignment must be a variable, property or indexer");
            });
        }

        [Fact]
        public void AssignOutParameter()
        {
            var source =
@"class C
{
    static void F<T>(System.Func<T> f)
    {
    }
    static void M1(out int x)
    {
        x = 1;
    }
    static void M2<T>(ref T y)
    {
        y = default(T);
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                runtime,
                methodName: "C.M1");
                string error;
                var testData = new CompilationTestData();
                context.CompileAssignment(
                    target: "x",
                    expr: "2",
                    error: out error,
                    testData: testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  stind.i4
  IL_0003:  ret
}");
                context = CreateMethodContext(
                    runtime,
                    methodName: "C.M2");
                testData = new CompilationTestData();
                context.CompileAssignment(
                    target: "y",
                    expr: "default(T)",
                    error: out error,
                    testData: testData);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""T""
  IL_0007:  ret
}");
                testData = new CompilationTestData();
                context.CompileExpression(
                    expr: "F(() => y)",
                    error: out error,
                    testData: testData);
                Assert.Equal(error, "error CS1628: Cannot use ref or out parameter 'y' inside an anonymous method, lambda expression, or query expression");
            });
        }

        [Fact]
        public void EvaluateNamespace()
        {
            var source =
@"namespace N
{
    class C
    {
        static void M()
        {
        }
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "N.C.M",
                expr: "N",
                resultProperties: out resultProperties,
                error: out error);
            // Note: The native EE reports "CS0119: 'N' is a namespace, which is not valid in the given context"
            Assert.Equal(error, "error CS0118: 'N' is a namespace but is used like a variable");
        }

        [Fact]
        public void EvaluateType()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "C",
                resultProperties: out resultProperties,
                error: out error);
            // The native EE returns a representation of the type (but not System.Type)
            // that the user can expand to see the base type. To enable similar
            // behavior, the expression compiler would probably return something
            // other than IL. Instead, we disallow this scenario.
            Assert.Equal(error, "error CS0119: 'C' is a type, which is not valid in the given context");
        }

        [Fact]
        public void EvaluateObjectAddress()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "@0x123 ?? @0xa1b2c3 ?? (object)$exception ?? @0XA1B2C3.GetHashCode()",
                    DkmEvaluationFlags.TreatAsExpression,
                    ImmutableArray.Create(ExceptionAlias()),
                    out error,
                    testData);
                Assert.Null(error);
                Assert.Equal(testData.Methods.Count, 1);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       61 (0x3d)
  .maxstack  2
  IL_0000:  ldc.i4     0x123
  IL_0005:  conv.i8
  IL_0006:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectAtAddress(ulong)""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_003c
  IL_000e:  pop
  IL_000f:  ldc.i4     0xa1b2c3
  IL_0014:  conv.i8
  IL_0015:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectAtAddress(ulong)""
  IL_001a:  dup
  IL_001b:  brtrue.s   IL_003c
  IL_001d:  pop
  IL_001e:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
  IL_0023:  dup
  IL_0024:  brtrue.s   IL_003c
  IL_0026:  pop
  IL_0027:  ldc.i4     0xa1b2c3
  IL_002c:  conv.i8
  IL_002d:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectAtAddress(ulong)""
  IL_0032:  callvirt   ""int object.GetHashCode()""
  IL_0037:  box        ""int""
  IL_003c:  ret
}");
                testData = new CompilationTestData();
                // Report overflow, even though native EE does not.
                context.CompileExpression(
                    "@0xffff0000ffff0000ffff0000",
                    out error, testData);
                Assert.Equal(error, "error CS1021: Integral constant is too large");
            });
        }

        [WorkItem(986227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/986227")]
        [Fact]
        public void RewriteCatchLocal()
        {
            var source =
@"using System;
class E<T> : Exception { }
class C<T>
{
    static void M()
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    expr:
@"((Func<E<T>>)(() =>
{
    E<T> e1 = null;
    try
    {
        string.Empty.ToString();
    }
    catch (E<T> e2)
    {
        e1 = e2;
    }
    catch
    {
    }
    return e1;
}))()",
                    error: out error,
                    testData: testData);

                var methodData = testData.GetMethodData("<>x<T>.<>c.<<>m0>b__0_0");
                var method = (MethodSymbol)methodData.Method;
                var containingType = method.ContainingType;
                var returnType = (NamedTypeSymbol)method.ReturnType;
                // Return type E<T> with type argument T from <>c<T>.
                Assert.Equal(returnType.TypeArguments[0].ContainingSymbol, containingType.ContainingType);
                var locals = methodData.ILBuilder.LocalSlotManager.LocalsInOrder();
                Assert.Equal(1, locals.Length);
                // All locals of type E<T> with type argument T from <>c<T>.
                foreach (var local in locals)
                {
                    var localType = (NamedTypeSymbol)local.Type;
                    var typeArg = localType.TypeArguments[0];
                    Assert.Equal(typeArg.ContainingSymbol, containingType.ContainingType);
                }

                methodData.VerifyIL(
@"{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (E<T> V_0) //e1
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldsfld     ""string string.Empty""
    IL_0007:  callvirt   ""string object.ToString()""
    IL_000c:  pop
    IL_000d:  leave.s    IL_0015
  }
  catch E<T>
  {
    IL_000f:  stloc.0
    IL_0010:  leave.s    IL_0015
  }
  catch object
  {
    IL_0012:  pop
    IL_0013:  leave.s    IL_0015
  }
  IL_0015:  ldloc.0
  IL_0016:  ret
}");
            });
        }

        [WorkItem(986227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/986227")]
        [Fact]
        public void RewriteSequenceTemps()
        {
            var source =
@"class C
{
    object F;
    static void M<T>() where T : C, new()
    {
        T t;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    expr: "new T() { F = 1 }",
                    error: out error,
                    testData: testData);

                var methodData = testData.GetMethodData("<>x.<>m0<T>()");
                var method = (MethodSymbol)methodData.Method;
                var returnType = method.ReturnType;
                Assert.Equal(returnType.TypeKind, TypeKind.TypeParameter);
                Assert.Equal(returnType.ContainingSymbol, method);

                var locals = methodData.ILBuilder.LocalSlotManager.LocalsInOrder();
                // The original local of type T from <>m0<T>.
                Assert.Equal(locals.Length, 1);
                foreach (var local in locals)
                {
                    var localType = (TypeSymbol)local.Type;
                    Assert.Equal(localType.ContainingSymbol, method);
                }

                methodData.VerifyIL(
@"{
  // Code size       23 (0x17)
  .maxstack  3
  .locals init (T V_0) //t
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  dup
  IL_0006:  box        ""T""
  IL_000b:  ldc.i4.1
  IL_000c:  box        ""int""
  IL_0011:  stfld      ""object C.F""
  IL_0016:  ret
}");
            });
        }

        [Fact]
        public void AssignEmitError()
        {
            var longName = new string('P', 1100);
            var source =
@"class C
{
    static void M(object o)
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileAssignment(
                    target: "o",
                    expr: string.Format("new {{ {0} = 1 }}", longName),
                    error: out error,
                    testData: testData);
                Assert.Equal(error, string.Format("error CS7013: Name '<{0}>i__Field' exceeds the maximum length allowed in metadata.", longName));
            });
        }

        /// <summary>
        /// Attempt to assign where the rvalue is not an rvalue.
        /// </summary>
        [Fact]
        public void AssignVoidExpression()
        {
            var source =
@"class C
{
    static void M()
    {
        object o;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileAssignment(
                    target: "o",
                    expr: "M()",
                    error: out error,
                    testData: testData);
                Assert.Equal(error, "error CS0029: Cannot implicitly convert type 'void' to 'object'");
            });
        }

        [Fact]
        public void AssignUnsafeExpression()
        {
            var source =
@"class C
{
    static unsafe void M(int *p)
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.UnsafeDebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileAssignment(
                    target: "p[1]",
                    expr: "p[0] + 1",
                    error: out error,
                    testData: testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.4
  IL_0002:  add
  IL_0003:  ldarg.0
  IL_0004:  ldind.i4
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  stind.i4
  IL_0008:  ret
}");
            });
        }

        /// <remarks>
        /// This is interesting because we're always in an unsafe context in
        /// the expression compiler and so an await expression would not
        /// normally be allowed.
        /// </remarks>
        [WorkItem(1075258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075258")]
        [Fact]
        public void Await()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task<object> F()
    {
        return null;
    }
    static void G(Func<Task<object>> f)
    {
    }
    static void Main()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.UnsafeDebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.Main");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("G(async() => await F())", out error, testData);
                Assert.Null(error);
            });
        }

        /// <remarks>
        /// This would be illegal in any non-debugger context.
        /// </remarks>
        [WorkItem(1075258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075258")]
        [Fact]
        public void AwaitInUnsafeContext()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task<object> F()
    {
        return null;
    }
    static void G(Func<Task<object>> f)
    {
    }
    static void Main()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.UnsafeDebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.Main");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(@"G(async() => 
{
    unsafe 
    {
        return await F();
    }
})", out error, testData);
                Assert.Null(error);
            });
        }

        /// <summary>
        /// Flow analysis should catch definite assignment errors
        /// for variables declared within the expression.
        /// </summary>
        [WorkItem(549, "https://github.com/dotnet/roslyn/issues/549")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/549")]
        public void FlowAnalysis()
        {
            var source =
@"class C
{
    static void M(bool b)
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr:
@"((System.Func<object>)(() =>
{
    object o;
    if (b) o = 1;
    return o;
}))()",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0165: Use of unassigned local variable 'o'");
        }

        /// <summary>
        /// Should be possible to evaluate an expression
        /// of a type that the compiler does not normally
        /// support as a return value.
        /// </summary>
        [Fact]
        public void EvaluateRestrictedTypeExpression()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "new System.RuntimeArgumentHandle()");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (System.RuntimeArgumentHandle V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.RuntimeArgumentHandle""
  IL_0008:  ldloc.0
  IL_0009:  ret
}");
        }

        [Fact]
        public void NestedNamespacesAndTypes()
        {
            var source =
@"namespace N
{
    namespace M
    {
        class A
        {
            class B
            {
                static object F() { return null; }
            }
        }
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "N.M.A.B.F",
                expr: "F()");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (object V_0)
  IL_0000:  call       ""object N.M.A.B.F()""
  IL_0005:  ret
}");
        }

        [Fact]
        public void GenericMethod()
        {
            var source =
@"class A<T>
{
    class B<U, V> where V : U
    {
        static void M1<W, X>() where X : A<W>.B<object, U[]>
        {
            var t = default(T);
            var w = default(W);
        }
        static void M2()
        {
            var t = default(T);
        }
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "A.B.M1");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("(object)t ?? (object)w ?? typeof(V) ?? typeof(X)", out error, testData);
                var methodData = testData.GetMethodData("<>x<T, U, V>.<>m0<W, X>");
                methodData.VerifyIL(
@"{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (T V_0, //t
                W V_1) //w
  IL_0000:  ldloc.0
  IL_0001:  box        ""T""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_002c
  IL_0009:  pop
  IL_000a:  ldloc.1
  IL_000b:  box        ""W""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_002c
  IL_0013:  pop
  IL_0014:  ldtoken    ""V""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  dup
  IL_001f:  brtrue.s   IL_002c
  IL_0021:  pop
  IL_0022:  ldtoken    ""X""
  IL_0027:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_002c:  ret
}");
                // Verify generated type and method are generic.
                Assert.Equal(((Cci.IMethodDefinition)methodData.Method).CallingConvention, Cci.CallingConvention.Generic);
                var metadata = ModuleMetadata.CreateFromImage(ImmutableArray.CreateRange(result.Assembly));
                var reader = metadata.MetadataReader;
                var typeDef = reader.GetTypeDef(result.TypeName);
                reader.CheckTypeParameters(typeDef.GetGenericParameters(), "T", "U", "V");
                var methodDef = reader.GetMethodDef(typeDef, result.MethodName);
                reader.CheckTypeParameters(methodDef.GetGenericParameters(), "W", "X");

                context = CreateMethodContext(
                    runtime,
                    methodName: "A.B.M2");
                testData = new CompilationTestData();
                context.CompileExpression("(object)t ?? typeof(T) ?? typeof(U)", out error, testData);
                methodData = testData.GetMethodData("<>x<T, U, V>.<>m0");
                Assert.Equal(((Cci.IMethodDefinition)methodData.Method).CallingConvention, Cci.CallingConvention.Default);
            });
        }

        [Fact]
        public void GenericClosureClass()
        {
            var source =
@"using System;
class C<T>
{
    static U F<U>(Func<U> f)
    {
        return f();
    }
    U M<U>(U u)
    {
        return u;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("F(() => this.M(u))", out error, testData);
                var methodData = testData.GetMethodData("<>x<T>.<>m0<U>");
                methodData.VerifyIL(@"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (U V_0)
  IL_0000:  newobj     ""<>x<T>.<>c__DisplayClass0_0<U>..ctor()""
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      ""C<T> <>x<T>.<>c__DisplayClass0_0<U>.<>4__this""
  IL_000c:  dup
  IL_000d:  ldarg.1
  IL_000e:  stfld      ""U <>x<T>.<>c__DisplayClass0_0<U>.u""
  IL_0013:  ldftn      ""U <>x<T>.<>c__DisplayClass0_0<U>.<<>m0>b__0()""
  IL_0019:  newobj     ""System.Func<U>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""U C<T>.F<U>(System.Func<U>)""
  IL_0023:  ret
}");
                Assert.Equal(((Cci.IMethodDefinition)methodData.Method).CallingConvention, Cci.CallingConvention.Generic);
            });
        }

        [WorkItem(976847, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/976847")]
        [Fact]
        public void VarArgMethod()
        {
            var source =
@"class C
{
    static void M(object o, __arglist)
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("new System.ArgIterator(__arglist)", out error, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  arglist
  IL_0002:  newobj     ""System.ArgIterator..ctor(System.RuntimeArgumentHandle)""
  IL_0007:  ret
}");
                Assert.Equal(((Cci.IMethodDefinition)methodData.Method).CallingConvention, Cci.CallingConvention.ExtraArguments);
            });
        }

        [Fact]
        public void EvaluateLambdaWithParameters()
        {
            var source =
@"class C
{
    static void M(object x, object y)
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "((System.Func<object, object, object>)((a, b) => a ?? b))(x, y)");
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldsfld     ""System.Func<object, object, object> <>x.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_000e:  ldftn      ""object <>x.<>c.<<>m0>b__0_0(object, object)""
  IL_0014:  newobj     ""System.Func<object, object, object>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""System.Func<object, object, object> <>x.<>c.<>9__0_0""
  IL_001f:  ldarg.0
  IL_0020:  ldarg.1
  IL_0021:  callvirt   ""object System.Func<object, object, object>.Invoke(object, object)""
  IL_0026:  ret
}");
        }

        [Fact]
        public void EvaluateLambdaWithLocals()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr:
@"((System.Func<object>)(() =>
{
    int x = 1;
    if (x < 0)
    {
        int y = 2;
        return y;
    }
    else
    {
        int z = 3;
        return z;
    }
}))()");
        }

        /// <summary>
        /// Lambda expression containing names
        /// that shadow names outside expression.
        /// </summary>
        [Fact]
        public void EvaluateLambdaWithNameShadowing()
        {
            var source =
@"class C
{
    static void M(object x)
    {
        object y;
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr:
@"((System.Func<object, object>)(y =>
{
    object x = y;
    return y;
}))(x, y)",
                resultProperties: out resultProperties,
                error: out error);
            // Currently generating errors but this seems unnecessary and
            // an extra burden for the user. Consider allowing names
            // inside the expression that shadow names outside.
            Assert.Equal("error CS0136: A local or parameter named 'y' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter", error);
        }

        [Fact]
        public void EvaluateNestedLambdaClosedOverLocal()
        {
            var source =
@"delegate object D(C c);
class C
{
    object F(D d)
    {
        return d(this);
    }
    static void Main(string[] args)
    {
        int x = 1;
        C y = new C();
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.ConsoleApplication,
                methodName: "C.Main",
                expr: "y.F(a => y.F(b => x))");
            // Verify display class was included.
            testData.GetMethodData("<>x.<>c__DisplayClass0_0..ctor").VerifyIL(
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}");
            // Verify evaluation method.
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (int V_0, //x
                C V_1, //y
                <>x.<>c__DisplayClass0_0 V_2) //CS$<>8__locals0
  IL_0000:  newobj     ""<>x.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.2
  IL_0006:  ldloc.2
  IL_0007:  ldloc.1
  IL_0008:  stfld      ""C <>x.<>c__DisplayClass0_0.y""
  IL_000d:  ldloc.2
  IL_000e:  ldloc.0
  IL_000f:  stfld      ""int <>x.<>c__DisplayClass0_0.x""
  IL_0014:  ldloc.2
  IL_0015:  ldfld      ""C <>x.<>c__DisplayClass0_0.y""
  IL_001a:  ldloc.2
  IL_001b:  ldftn      ""object <>x.<>c__DisplayClass0_0.<<>m0>b__0(C)""
  IL_0021:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0026:  callvirt   ""object C.F(D)""
  IL_002b:  ret
}");
        }

        [Fact]
        public void EvaluateLambdaClosedOverThis()
        {
            var source =
@"class A
{
    internal virtual object F() { return null; }
    internal object G;
    internal virtual object P { get { return null; } }
}
class B : A
{
    internal override object F() { return null; }
    internal new object G;
    internal override object P { get { return null; } }
    static object F(System.Func<object> f1, System.Func<object> f2, object g) { return null; }
    void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "B.M",
                expr: "((System.Func<object>)(() => this.G))()",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0()").VerifyIL(
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0006:  ldfld      ""object B.G""
  IL_000b:  ret
}");
            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "B.M",
                expr: "((System.Func<object>)(() => this.F() ?? this.P))()",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0()").VerifyIL(
@"{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0006:  callvirt   ""object B.F()""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_001a
  IL_000e:  pop
  IL_000f:  ldarg.0
  IL_0010:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0015:  callvirt   ""object B.P.get""
  IL_001a:  ret
}");
            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "B.M",
                expr: "((System.Func<object>)(() => F(new System.Func<object>(this.F), this.F, this.G)))()");
            testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0()").VerifyIL(
@"{
  // Code size       53 (0x35)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0006:  dup
  IL_0007:  ldvirtftn  ""object B.F()""
  IL_000d:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_0012:  ldarg.0
  IL_0013:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0018:  dup
  IL_0019:  ldvirtftn  ""object B.F()""
  IL_001f:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_0024:  ldarg.0
  IL_0025:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_002a:  ldfld      ""object B.G""
  IL_002f:  call       ""object B.F(System.Func<object>, System.Func<object>, object)""
  IL_0034:  ret
}");
        }

        [WorkItem(905986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/905986")]
        [Fact]
        public void EvaluateLambdaClosedOverBase()
        {
            var source =
@"class A
{
    internal virtual object F() { return null; }
    internal object G;
    internal virtual object P { get { return null; } }
}
class B : A
{
    internal override object F() { return null; }
    internal new object G;
    internal override object P { get { return null; } }
    static object F(System.Func<object> f1, System.Func<object> f2, object g) { return null; }
    void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "B.M",
                expr: "((System.Func<object>)(() => base.F() ?? base.P))()");
            testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0()").VerifyIL(
@"{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0006:  call       ""object A.F()""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_001a
  IL_000e:  pop
  IL_000f:  ldarg.0
  IL_0010:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0015:  call       ""object A.P.get""
  IL_001a:  ret
}");
            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "B.M",
                expr: "((System.Func<object>)(() => F(new System.Func<object>(base.F), base.F, base.G)))()");
            testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0()").VerifyIL(
@"{
  // Code size       51 (0x33)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0006:  ldftn      ""object A.F()""
  IL_000c:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0017:  ldftn      ""object A.F()""
  IL_001d:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_0022:  ldarg.0
  IL_0023:  ldfld      ""B <>x.<>c__DisplayClass0_0.<>4__this""
  IL_0028:  ldfld      ""object A.G""
  IL_002d:  call       ""object B.F(System.Func<object>, System.Func<object>, object)""
  IL_0032:  ret
}");
        }

        [Fact]
        public void EvaluateCapturedLocalsAlreadyCaptured()
        {
            var source =
@"class A
{
    internal virtual object F(object o)
    {
        return 1;
    }
}
class B : A
{
    internal override object F(object o)
    {
        return 2;
    }
    static void F(System.Func<object> f)
    {
        f();
    }
    void M(object x)
    {
        object y = 1;
        F(() => this.F(x));
        F(() => base.F(y));
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(
                runtime,
                methodName: "B.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("F(() => this.F(x))", out error, testData);

                // Note there are duplicate local names (one from the original
                // display class, the other from the new display class in each case).
                // That is expected since we do not rename old locals nor do we
                // offset numbering of new locals. Having duplicate local names
                // in the PDB should be harmless though.
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (B.<>c__DisplayClass2_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""<>x.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldloc.0
  IL_0007:  stfld      ""B.<>c__DisplayClass2_0 <>x.<>c__DisplayClass0_0.CS$<>8__locals0""
  IL_000c:  ldftn      ""object <>x.<>c__DisplayClass0_0.<<>m0>b__0()""
  IL_0012:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_0017:  call       ""void B.F(System.Func<object>)""
  IL_001c:  ret
}");
                testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0").VerifyIL(
@"{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B.<>c__DisplayClass2_0 <>x.<>c__DisplayClass0_0.CS$<>8__locals0""
  IL_0006:  ldfld      ""B B.<>c__DisplayClass2_0.<>4__this""
  IL_000b:  ldarg.0
  IL_000c:  ldfld      ""B.<>c__DisplayClass2_0 <>x.<>c__DisplayClass0_0.CS$<>8__locals0""
  IL_0011:  ldfld      ""object B.<>c__DisplayClass2_0.x""
  IL_0016:  callvirt   ""object B.F(object)""
  IL_001b:  ret
}");
                testData = new CompilationTestData();
                context.CompileExpression("F(() => base.F(y))", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (B.<>c__DisplayClass2_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""<>x.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldloc.0
  IL_0007:  stfld      ""B.<>c__DisplayClass2_0 <>x.<>c__DisplayClass0_0.CS$<>8__locals0""
  IL_000c:  ldftn      ""object <>x.<>c__DisplayClass0_0.<<>m0>b__0()""
  IL_0012:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_0017:  call       ""void B.F(System.Func<object>)""
  IL_001c:  ret
}");
                testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0").VerifyIL(
@"{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""B.<>c__DisplayClass2_0 <>x.<>c__DisplayClass0_0.CS$<>8__locals0""
  IL_0006:  ldfld      ""B B.<>c__DisplayClass2_0.<>4__this""
  IL_000b:  ldarg.0
  IL_000c:  ldfld      ""B.<>c__DisplayClass2_0 <>x.<>c__DisplayClass0_0.CS$<>8__locals0""
  IL_0011:  ldfld      ""object B.<>c__DisplayClass2_0.y""
  IL_0016:  call       ""object A.F(object)""
  IL_001b:  ret
}");
            });
        }

        [WorkItem(994485, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994485")]
        [Fact]
        public void Repro994485()
        {
            var source = @"
using System;

enum E
{
    A
}

class C
{
    Action M(E? e)
    {
        Action a = () => e.ToString();
        E ee = e.Value;
        return a;
    }
}
";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("e.HasValue", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Action V_1, //a
                E V_2, //ee
                System.Action V_3)
  IL_0000:  ldloc.0
  IL_0001:  ldflda     ""E? C.<>c__DisplayClass0_0.e""
  IL_0006:  call       ""bool E?.HasValue.get""
  IL_000b:  ret
}");
            });
        }

        [Fact]
        public void EvaluateCapturedLocalsOutsideLambda()
        {
            var source =
@"class A
{
    internal virtual object F(object o)
    {
        return 1;
    }
}
class B : A
{
    internal override object F(object o)
    {
        return 2;
    }
    static void F(System.Func<object> f)
    {
        f();
    }
    void M<T>(object x) where T : A, new()
    {
        F(() => this.F(x));
        if (x != null)
        {
#line 999
            var y = new T();
            var z = 1;
            F(() => base.F(y));
        }
        else
        {
            var w = 2;
            F(() => w);
        }
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "B.M", atLineNumber: 999);

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("this.F(y)", out error, testData);

                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (B.<>c__DisplayClass2_0<T> V_0, //CS$<>8__locals0
                bool V_1,
                B.<>c__DisplayClass2_1<T> V_2, //CS$<>8__locals1
                int V_3, //z
                B.<>c__DisplayClass2_2<T> V_4)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""B B.<>c__DisplayClass2_0<T>.<>4__this""
  IL_0006:  ldloc.2
  IL_0007:  ldfld      ""T B.<>c__DisplayClass2_1<T>.y""
  IL_000c:  box        ""T""
  IL_0011:  callvirt   ""object B.F(object)""
  IL_0016:  ret
}");
                testData = new CompilationTestData();
                context.CompileExpression("base.F(x)", out error, testData);

                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(
@"{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (B.<>c__DisplayClass2_0<T> V_0, //CS$<>8__locals0
                bool V_1,
                B.<>c__DisplayClass2_1<T> V_2, //CS$<>8__locals1
                int V_3, //z
                B.<>c__DisplayClass2_2<T> V_4)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""B B.<>c__DisplayClass2_0<T>.<>4__this""
  IL_0006:  ldloc.0
  IL_0007:  ldfld      ""object B.<>c__DisplayClass2_0<T>.x""
  IL_000c:  call       ""object A.F(object)""
  IL_0011:  ret
}");
            });
        }

        [Fact]
        public void EvaluateCapturedLocalsInsideLambda()
        {
            var source =
@"class C
{
    static void F(System.Func<object> f)
    {
        f();
    }
    void M(C x)
    {
        F(() => x ?? this);
        if (x != null)
        {
            var y = new C();
            F(() =>
            {
                var z = 1;
                return y ?? this;
            });
        }
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.<>c__DisplayClass1_1.<M>b__1",
                expr: "y ?? this ?? (object)z");

            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (int V_0, //z
                object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_1.y""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001f
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""C.<>c__DisplayClass1_0 C.<>c__DisplayClass1_1.CS$<>8__locals1""
  IL_0010:  ldfld      ""C C.<>c__DisplayClass1_0.<>4__this""
  IL_0015:  dup
  IL_0016:  brtrue.s   IL_001f
  IL_0018:  pop
  IL_0019:  ldloc.0
  IL_001a:  box        ""int""
  IL_001f:  ret
}");
        }

        /// <summary>
        /// Values of existing locals must be copied to new display
        /// classes generated in the compiled expression.
        /// </summary>
        [Fact]
        public void CopyLocalsToDisplayClass()
        {
            var source =
@"class C
{
    static void F(System.Func<object> f)
    {
        f();
    }
    void M(int p, int q)
    {
        int x = 1;
        if (p > 0)
        {
#line 999
            int y = 2;
            F(() => x + q);
        }
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                atLineNumber: 999,
                expr: "F(() => x + y + p + q)");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                bool V_1,
                int V_2) //y
  IL_0000:  newobj     ""<>x.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldloc.0
  IL_0007:  stfld      ""C.<>c__DisplayClass1_0 <>x.<>c__DisplayClass0_0.CS$<>8__locals0""
  IL_000c:  dup
  IL_000d:  ldloc.2
  IL_000e:  stfld      ""int <>x.<>c__DisplayClass0_0.y""
  IL_0013:  dup
  IL_0014:  ldarg.1
  IL_0015:  stfld      ""int <>x.<>c__DisplayClass0_0.p""
  IL_001a:  ldftn      ""object <>x.<>c__DisplayClass0_0.<<>m0>b__0()""
  IL_0020:  newobj     ""System.Func<object>..ctor(object, System.IntPtr)""
  IL_0025:  call       ""void C.F(System.Func<object>)""
  IL_002a:  ret
}");
        }

        [Fact]
        public void EvaluateNewAnonymousType()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "new { A = 1, B = 2 }");

            // Verify anonymous type was generated (find an
            // accessor of one of the generated properties).
            testData.GetMethodData("<>f__AnonymousType0<<A>j__TPar, <B>j__TPar>.A.get").VerifyIL(
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<A>j__TPar <>f__AnonymousType0<<A>j__TPar, <B>j__TPar>.<A>i__Field""
  IL_0006:  ret
}");

            // Verify evaluation method.
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void EvaluateExistingAnonymousType()
        {
            var source =
@"class C
{
    static object F()
    {
        return new { A = new { }, B = 2 };
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.F",
                expr: "new { A = 1, B = new { } }");
            // Verify anonymous types were generated. (There
            // shouldn't be any reuse of existing anonymous types
            // since the existing types were from metadata.)
            var methods = testData.GetMethodsByName();
            Assert.True(methods.ContainsKey("<>f__AnonymousType0<<A>j__TPar, <B>j__TPar>..ctor(<A>j__TPar, <B>j__TPar)"));
            Assert.True(methods.ContainsKey("<>f__AnonymousType1..ctor()"));

            // Verify evaluation method.
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     ""<>f__AnonymousType1..ctor()""
  IL_0006:  newobj     ""<>f__AnonymousType0<int, <empty anonymous type>>..ctor(int, <empty anonymous type>)""
  IL_000b:  ret
}");
        }

        /// <summary>
        /// Should re-use anonymous types from the module
        /// containing the current frame, so new instances can
        /// be used interchangeably with existing instances.
        /// </summary>
        [WorkItem(3188, "https://github.com/dotnet/roslyn/issues/3188")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/3188")]
        public void EvaluateExistingAnonymousType_2()
        {
            var source =
@"class C
{
    static void M()
    {
        var o = new { P = 1 };
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "o == new { P = 2 }");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
...
}");
        }

        /// <summary>
        /// Generate PrivateImplementationDetails class
        /// for initializer expressions.
        /// </summary>
        [Fact]
        public void EvaluateInitializerExpression()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll.WithModuleName("MODULE"));
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("new [] { 1, 2, 3, 4, 5 }", out error, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.Equal(methodData.Method.ReturnType.ToDisplayString(), "int[]");
                methodData.VerifyIL(
@"{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.1036C5F8EF306104BD582D73E555F4DAE8EECB24""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  ret
}");
            });
        }

        // Scenario from the lambda / anonymous type milestone.
        [Fact]
        public void EvaluateLINQExpression()
        {
            var source =
@"using System.Collections.Generic;
using System.Linq;
class Employee
{
    internal string Name;
    internal int Salary;
    internal List<Employee> Reports;
}
class Program
{
    static void F(Employee mgr)
    {
        var o = mgr.Reports.Where(e => e.Salary < 100).Select(e => new { e.Name, e.Salary }).First();
    }
}";
            var compilation0 = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "Program.F");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("mgr.Reports.Where(e => e.Salary < 100).Select(e => new { e.Name, e.Salary }).First()", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       84 (0x54)
  .maxstack  3
  .locals init (<>f__AnonymousType0<string, int> V_0) //o
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""System.Collections.Generic.List<Employee> Employee.Reports""
  IL_0006:  ldsfld     ""System.Func<Employee, bool> <>x.<>c.<>9__0_0""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0025
  IL_000e:  pop
  IL_000f:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_0014:  ldftn      ""bool <>x.<>c.<<>m0>b__0_0(Employee)""
  IL_001a:  newobj     ""System.Func<Employee, bool>..ctor(object, System.IntPtr)""
  IL_001f:  dup
  IL_0020:  stsfld     ""System.Func<Employee, bool> <>x.<>c.<>9__0_0""
  IL_0025:  call       ""System.Collections.Generic.IEnumerable<Employee> System.Linq.Enumerable.Where<Employee>(System.Collections.Generic.IEnumerable<Employee>, System.Func<Employee, bool>)""
  IL_002a:  ldsfld     ""System.Func<Employee, <anonymous type: string Name, int Salary>> <>x.<>c.<>9__0_1""
  IL_002f:  dup
  IL_0030:  brtrue.s   IL_0049
  IL_0032:  pop
  IL_0033:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_0038:  ldftn      ""<anonymous type: string Name, int Salary> <>x.<>c.<<>m0>b__0_1(Employee)""
  IL_003e:  newobj     ""System.Func<Employee, <anonymous type: string Name, int Salary>>..ctor(object, System.IntPtr)""
  IL_0043:  dup
  IL_0044:  stsfld     ""System.Func<Employee, <anonymous type: string Name, int Salary>> <>x.<>c.<>9__0_1""
  IL_0049:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string Name, int Salary>> System.Linq.Enumerable.Select<Employee, <anonymous type: string Name, int Salary>>(System.Collections.Generic.IEnumerable<Employee>, System.Func<Employee, <anonymous type: string Name, int Salary>>)""
  IL_004e:  call       ""<anonymous type: string Name, int Salary> System.Linq.Enumerable.First<<anonymous type: string Name, int Salary>>(System.Collections.Generic.IEnumerable<<anonymous type: string Name, int Salary>>)""
  IL_0053:  ret
}");
            });
        }

        [Fact]
        public void ExpressionTree()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    static object F(Expression<Func<object>> e)
    {
        var f = e.Compile();
        return f();
    }
    static void M(int o)
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("F(() => o + 1)", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size      100 (0x64)
  .maxstack  3
  IL_0000:  newobj     ""<>x.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      ""int <>x.<>c__DisplayClass0_0.o""
  IL_000c:  ldtoken    ""<>x.<>c__DisplayClass0_0""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_001b:  ldtoken    ""int <>x.<>c__DisplayClass0_0.o""
  IL_0020:  call       ""System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)""
  IL_0025:  call       ""System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)""
  IL_002a:  ldc.i4.1
  IL_002b:  box        ""int""
  IL_0030:  ldtoken    ""int""
  IL_0035:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_003a:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_003f:  call       ""System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression.Add(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression)""
  IL_0044:  ldtoken    ""object""
  IL_0049:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_004e:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_0053:  ldc.i4.0
  IL_0054:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0059:  call       ""System.Linq.Expressions.Expression<System.Func<object>> System.Linq.Expressions.Expression.Lambda<System.Func<object>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_005e:  call       ""object C.F(System.Linq.Expressions.Expression<System.Func<object>>)""
  IL_0063:  ret
}");
            });
        }

        /// <summary>
        /// DiagnosticsPass must be run on evaluation method.
        /// </summary>
        [WorkItem(530404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530404")]
        [Fact]
        public void DiagnosticsPass()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class C
{
    static object F(Expression<Func<object>> e)
    {
        var f = e.Compile();
        return f();
    }
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("F(() => null ?? new object())", out error, testData);
                Assert.Equal(error, "error CS0845: An expression tree lambda may not contain a coalescing operator with a null or default literal left-hand side");
            });
        }

        [WorkItem(935651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/935651")]
        [Fact]
        public void EvaluatePropertySet()
        {
            var source =
@"class C
{
    object P { get; set; }
    void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "this.P = null");

            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       11 (0xb)
  .maxstack  3
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  callvirt   ""void C.P.set""
  IL_0009:  ldloc.0
  IL_000a:  ret
}");
        }

        /// <summary>
        /// Evaluating an expression where the imported namespace
        /// is valid but the required reference is missing.
        /// </summary>
        [Fact]
        public void EvaluateExpression_MissingReferenceImportedNamespace()
        {
            // System.Linq namespace is available but System.Core is
            // missing since the reference was not needed in compilation.
            var source =
@"using System.Linq;
class C
{
    static void M(object []o)
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                var result = context.CompileExpression(
                    "o.First()",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(error, "error CS1061: 'object[]' does not contain a definition for 'First' and no extension method 'First' accepting a first argument of type 'object[]' could be found (are you missing a using directive or an assembly reference?)");
                AssertEx.SetEqual(missingAssemblyIdentities, EvaluationContextBase.SystemCoreIdentity);
            });
        }

        [Fact]
        public void EvaluateExpression_UnusedImportedType()
        {
            var source =
@"using E=System.Linq.Enumerable;
class C
{
    static void M(object []o)
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;

                var testData = new CompilationTestData();
                var result = context.CompileExpression("E.First(o)", out error, testData);
                Assert.Null(error);

                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object System.Linq.Enumerable.First<object>(System.Collections.Generic.IEnumerable<object>)""
  IL_0006:  ret
}");
            });
        }

        [Fact]
        public void NetModuleReference()
        {
            var sourceNetModule =
@"class A
{
}";
            var source1 =
@"class B : A
{
    void M()
    {
    }
}";
            var netModuleRef = CreateStandardCompilation(sourceNetModule, options: TestOptions.DebugModule).EmitToImageReference();
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll, references: new[] { netModuleRef });

            WithRuntimeInstance(compilation1, runtime =>
            {
                var context = CreateMethodContext(runtime, "B.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("this", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            });
        }

        /// <summary>
        /// Netmodules with same name.
        /// </summary>
        [Fact]
        public void NetModuleDuplicateReferences()
        {
            // Netmodule 0
            var sourceN0 =
@"public class A0
{
    public int F0;
}";
            // Netmodule 1
            var sourceN1 =
@"public class A1
{
    public int F1;
}";
            // Netmodule 2
            var sourceN2 =
@"public class A2
{
    public int F2;
}";
            // DLL 0 + netmodule 0
            var sourceD0 =
@"public class B0 : A0
{
}";
            // DLL 1 + netmodule 0
            var sourceD1 =
@"public class B1 : A0
{
}";
            // DLL 2 + netmodule 1 + netmodule 2
            var source =
@"class C
{
    static B0 x;
    static B1 y;
    static A1 z;
    static A2 w;
    static void M()
    {
    }
}";
            var assemblyName = ExpressionCompilerUtilities.GenerateUniqueName();
            var compilationN0 = CreateStandardCompilation(
                sourceN0,
                options: TestOptions.DebugModule,
                assemblyName: assemblyName + "_N0");
            var referenceN0 = ModuleMetadata.CreateFromImage(compilationN0.EmitToArray()).GetReference(display: assemblyName + "_N0");
            var compilationN1 = CreateStandardCompilation(
                sourceN1,
                options: TestOptions.DebugModule,
                assemblyName: assemblyName + "_N0"); // Note: "_N0" not "_N1"
            var referenceN1 = ModuleMetadata.CreateFromImage(compilationN1.EmitToArray()).GetReference(display: assemblyName + "_N0");
            var compilationN2 = CreateStandardCompilation(
                sourceN2,
                options: TestOptions.DebugModule,
                assemblyName: assemblyName + "_N2");
            var referenceN2 = ModuleMetadata.CreateFromImage(compilationN2.EmitToArray()).GetReference(display: assemblyName + "_N2");
            var compilationD0 = CreateStandardCompilation(
                sourceD0,
                options: TestOptions.DebugDll,
                assemblyName: assemblyName + "_D0",
                references: new MetadataReference[] { referenceN0 });
            var referenceD0 = AssemblyMetadata.CreateFromImage(compilationD0.EmitToArray()).GetReference(display: assemblyName + "_D0");
            var compilationD1 = CreateStandardCompilation(
                sourceD1,
                options: TestOptions.DebugDll,
                assemblyName: assemblyName + "_D1",
                references: new MetadataReference[] { referenceN0 });
            var referenceD1 = AssemblyMetadata.CreateFromImage(compilationD1.EmitToArray()).GetReference(display: assemblyName + "_D1");
            var compilation = CreateStandardCompilation(
                source,
                options: TestOptions.DebugDll,
                assemblyName: assemblyName,
                references: new MetadataReference[] { referenceN1, referenceN2, referenceD0, referenceD1 });

            Assert.Equal(((ModuleMetadata)referenceN0.GetMetadataNoCopy()).Name, ((ModuleMetadata)referenceN1.GetMetadataNoCopy()).Name); // different netmodule, same name

            var references = new[]
            {
                MscorlibRef,
                referenceD0,
                referenceN0, // From D0
                referenceD1,
                referenceN0, // From D1
                referenceN1, // From D2
                referenceN2, // From D2
            };

            WithRuntimeInstance(compilation, references, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                // Expression references ambiguous modules.
                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                context.CompileExpression(
                    "x.F0 + y.F0",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                AssertEx.SetEqual(missingAssemblyIdentities, EvaluationContextBase.SystemCoreIdentity);
                Assert.Equal("error CS7079: The type 'A0' is defined in a module that has not been added. You must add the module '" + assemblyName + "_N0.netmodule'.", error);

                context.CompileExpression(
                    "y.F0",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                AssertEx.SetEqual(missingAssemblyIdentities, EvaluationContextBase.SystemCoreIdentity);
                Assert.Equal("error CS7079: The type 'A0' is defined in a module that has not been added. You must add the module '" + assemblyName + "_N0.netmodule'.", error);

                context.CompileExpression(
                    "z.F1",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Empty(missingAssemblyIdentities);
                Assert.Equal("error CS7079: The type 'A1' is defined in a module that has not been added. You must add the module '" + assemblyName + "_N0.netmodule'.", error);

                // Expression does not reference ambiguous modules.
                var testData = new CompilationTestData();
                context.CompileExpression("w.F2", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsfld     ""A2 C.w""
  IL_0005:  ldfld      ""int A2.F2""
  IL_000a:  ret
}");
            });
        }

        [Fact]
        public void SizeOfReferenceType()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "sizeof(C)",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal(error, "error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('C')");
        }

        [Fact]
        public void SizeOfValueType()
        {
            var source =
@"struct S
{
}
class C
{
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "sizeof(S)");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  sizeof     ""S""
  IL_0006:  ret
}");
        }

        /// <summary>
        /// Unnamed temporaries at the end of the local
        /// signature should be preserved.
        /// </summary>
        [Fact]
        public void TrailingUnnamedTemporaries()
        {
            var source =
@"class C
{
    object F;
    static bool M(object[] c)
    {
        foreach (var o in c)
        {
            if (o != null) return true;
        }
        return false;
    }
}";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "new C() { F = 1 }",
                resultProperties: out resultProperties,
                error: out error);

            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       18 (0x12)
  .maxstack  3
  .locals init (object[] V_0,
  int V_1,
  object V_2,
  bool V_3,
  bool V_4)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  box        ""int""
  IL_000c:  stfld      ""object C.F""
  IL_0011:  ret
}");
        }

        [WorkItem(958448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958448")]
        [Fact]
        public void ConditionalAttribute()
        {
            var source =
@"using System.Diagnostics;
class C
{
    static void M(int x)
    {
    }
    [Conditional(""D"")]
    static void F(object o)
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "F(x + 1)");

            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  box        ""int""
  IL_0008:  call       ""void C.F(object)""
  IL_000d:  ret
}");
        }

        [WorkItem(958448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958448")]
        [Fact]
        public void ConditionalAttribute_CollectionInitializer()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
class C : IEnumerable
{
    private List<object> c = new List<object>();
    [Conditional(""D"")]
    void Add(object o)
    {
        this.c.Add(o);
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.c.GetEnumerator();
    }
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "new C() { 1, 2 }");

            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  box        ""int""
  IL_000c:  callvirt   ""void C.Add(object)""
  IL_0011:  dup
  IL_0012:  ldc.i4.2
  IL_0013:  box        ""int""
  IL_0018:  callvirt   ""void C.Add(object)""
  IL_001d:  ret
}");
        }

        [Fact]
        public void ConditionalAttribute_Delegate()
        {
            var source =
@"using System.Diagnostics;
delegate void D();
class C
{
    [Conditional(""D"")]
    static void F()
    {
    }
    static void G(D d)
    {
    }
    static void M()
    {
    }
}";
            ResultProperties resultProperties;
            string error;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "G(F)",
                resultProperties: out resultProperties,
                error: out error);
            // Should delegates to [Conditional] methods be supported?
            Assert.Equal(error, "error CS1618: Cannot create delegate with 'C.F()' because it or a method it overrides has a Conditional attribute");
        }

        [Fact]
        public void StaticDelegate()
        {
            var source =
@"delegate void D();
class C
{
    static void F()
    {
    }
    static void G(D d)
    {
    }
    static void M()
    {
    }
}";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "G(F)");
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.F()""
  IL_0007:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void C.G(D)""
  IL_0011:  ret
}");
        }

        [Fact]
        public void StaticLambda()
        {
            var source = @"
delegate int D(int x);

class C
{
    void M()
    {
    }
}
";
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "((D)(x => x + x))(1)");

            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"
{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldsfld     ""D <>x.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_000e:  ldftn      ""int <>x.<>c.<<>m0>b__0_0(int)""
  IL_0014:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""D <>x.<>c.<>9__0_0""
  IL_001f:  ldc.i4.1
  IL_0020:  callvirt   ""int D.Invoke(int)""
  IL_0025:  ret
}");
        }

        [WorkItem(984509, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984509")]
        [Fact]
        public void LambdaContainingIncrementOperator()
        {
            var source =
@"class C
{
    static void M(int i)
    {
    }
}";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "(System.Action)(() => i++)",
                resultProperties: out resultProperties,
                error: out error);

            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            testData.GetMethodData("<>x.<>c__DisplayClass0_0.<<>m0>b__0").VerifyIL(
@"{
  // Code size       17 (0x11)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <>x.<>c__DisplayClass0_0.i""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  add
  IL_000b:  stfld      ""int <>x.<>c__DisplayClass0_0.i""
  IL_0010:  ret
}");
        }

        [Fact]
        public void NestedGenericTypes()
        {
            var source = @"
class C<T>
{
    class D<U>
    {
        void M(U u, T t, System.Type type1, System.Type type2)
        {
        }
    }
}
";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.D.M",
                expr: "M(u, t, typeof(U), typeof(T))",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Null(error);
            Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
            testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(
@"{
  // Code size       29 (0x1d)
  .maxstack  5
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldtoken    ""U""
  IL_0008:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000d:  ldtoken    ""T""
  IL_0012:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0017:  callvirt   ""void C<T>.D<U>.M(U, T, System.Type, System.Type)""
  IL_001c:  ret
}");
        }

        [Fact]
        public void NestedGenericTypes_GenericMethod()
        {
            var source = @"
class C<T>
{
    class D<U>
    {
        void M<V>(V v, U u, T t, System.Type type1, System.Type type2, System.Type type3)
        {
        }
    }
}
";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.D.M",
                expr: "M(v, u, t, typeof(V), typeof(U), typeof(T))",
                resultProperties: out resultProperties,
                error: out error);

            Assert.Null(error);
            Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);

            testData.GetMethodData("<>x<T, U>.<>m0<V>").VerifyIL(
@"{
  // Code size       40 (0x28)
  .maxstack  7
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldarg.3
  IL_0004:  ldtoken    ""V""
  IL_0009:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000e:  ldtoken    ""U""
  IL_0013:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0018:  ldtoken    ""T""
  IL_001d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0022:  callvirt   ""void C<T>.D<U>.M<V>(V, U, T, System.Type, System.Type, System.Type)""
  IL_0027:  ret
}");
        }

        [WorkItem(1000946, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1000946")]
        [Fact]
        public void BaseExpression()
        {
            var source = @"
class Base
{
}

class Derived : Base
{
    void M() { }
}
";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "Derived.M",
                expr: "base",
                resultProperties: out resultProperties,
                error: out error);

            Assert.Equal("error CS0175: Use of keyword 'base' is not valid in this context", error);
        }

        [Fact]
        public void StructBaseCall()
        {
            var source = @"
struct S
{
    public void M()
    {
    }
}
";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "S.M",
                expr: "base.ToString()",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""S""
  IL_0006:  box        ""S""
  IL_000b:  call       ""string System.ValueType.ToString()""
  IL_0010:  ret
}");
        }

        [WorkItem(1010922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010922")]
        [Fact]
        public void IntOverflow()
        {
            var source = @"
class C
{
    public void M()
    {
    }
}
";
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("checked(2147483647 + 1)", out error, testData);
                Assert.Equal("error CS0220: The operation overflows at compile time in checked mode", error);

                testData = new CompilationTestData();
                context.CompileExpression("unchecked(2147483647 + 1)", out error, testData);
                Assert.Null(error);

                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldc.i4     0x80000000
  IL_0005:  ret
}");

                testData = new CompilationTestData();
                context.CompileExpression("2147483647 + 1", out error, testData);
                Assert.Null(error);

                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldc.i4     0x80000000
  IL_0005:  ret
}");
            });
        }

        [WorkItem(1012956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1012956")]
        [Fact]
        public void AssignmentConversion()
        {
            var source = @"
class C
{
    public void M(uint u)
    {
    }
}
";
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("u = 2147483647 + 1", out error, testData);
                Assert.Equal("error CS0031: Constant value '-2147483648' cannot be converted to a 'uint'", error);

                testData = new CompilationTestData();
                context.CompileAssignment("u", "2147483647 + 1", out error, testData);
                Assert.Equal("error CS0031: Constant value '-2147483648' cannot be converted to a 'uint'", error);

                testData = new CompilationTestData();
                context.CompileExpression("u = 2147483647 + 1u", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldc.i4     0x80000000
  IL_0005:  dup
  IL_0006:  starg.s    V_1
  IL_0008:  ret
}");

                testData = new CompilationTestData();
                context.CompileAssignment("u", "2147483647 + 1u", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4     0x80000000
  IL_0005:  starg.s    V_1
  IL_0007:  ret
}");
            });
        }

        [WorkItem(1016530, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016530")]
        [Fact]
        public void EvaluateStatement()
        {
            var source = @"
class C
{
    void M() { }
}
";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "C.M",
                expr: "throw new System.Exception()",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Equal("error CS8115: A throw expression is not allowed in this context.", error);
        }

        [WorkItem(1016555, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016555")]
        [Fact]
        public void UnmatchedCloseAndOpenParens()
        {
            var source =
@"class C
{
    static void M()
    {
        object o = 1;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileAssignment(
                    target: "o",
                    expr: "(System.Func<object>)(() => 2))(",
                    error: out error,
                    testData: testData);
                Assert.Equal("error CS1073: Unexpected token ')'", error);
            });
        }

        [WorkItem(1015887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015887")]
        [Fact]
        public void DateTimeFieldConstant()
        {
            var source =
@".class public C
{
  .field public static initonly valuetype [mscorlib]System.DateTime D
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64)
           = {int64(633979872000000000)}

  .method public specialname rtspecialname instance void .ctor()
  {
    ret
  }

  .method public static void M()
  {
    ret
  }
}";
            var module = ExpressionCompilerTestHelpers.GetModuleInstanceForIL(source);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var context = CreateMethodContext(runtime, methodName: "C.M");

            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("D", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldsfld     ""System.DateTime C.D""
  IL_0005:  ret
}");
        }

        [WorkItem(1015887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015887")]
        [Fact]
        public void DecimalFieldConstant()
        {
            var source = @"
struct S
{
    public const decimal D = 3.14M;

    public void M()
    {
        System.Console.WriteLine();
    }
}
";
            string error;
            ResultProperties resultProperties;
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "S.M",
                expr: "D",
                resultProperties: out resultProperties,
                error: out error);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       15 (0xf)
  .maxstack  5
  IL_0000:  ldc.i4     0x13a
  IL_0005:  ldc.i4.0
  IL_0006:  ldc.i4.0
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.2
  IL_0009:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_000e:  ret
}");
        }

        [WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")]
        [Fact]
        public void IteratorParameter()
        {
            var source =
@"class C
{
    System.Collections.IEnumerable F(int x)
    {
        yield return x;
        yield return this; // Until iterators always capture 'this', do it explicitly.
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F>d__0.MoveNext");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("x", out error, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.Equal(SpecialType.System_Int32, methodData.Method.ReturnType.SpecialType);
                methodData.VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.x""
  IL_0006:  ret
}
");
            });
        }

        [WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")]
        [Fact]
        public void IteratorGenericLocal()
        {
            var source =
@"class C<T>
{
    System.Collections.IEnumerable F(int x)
    {
        T t = default(T);
        yield return t;
        t.ToString();
        yield return this; // Until iterators always capture 'this', do it explicitly.
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<F>d__0.MoveNext");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("t", out error, testData);
                var methodData = testData.GetMethodData("<>x<T>.<>m0");
                Assert.Equal("T", methodData.Method.ReturnType.Name);
                methodData.VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T C<T>.<F>d__0.<t>5__1""
  IL_0006:  ret
}
");
            });
        }

        [WorkItem(1028808, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1028808")]
        [Fact]
        public void StaticLambdaInDisplayClass()
        {
            var source =
@".class private auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .class auto ansi sealed nested private beforefieldinit '<>c__DisplayClass2'
         extends [mscorlib]System.Object
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public class C c
    .field private static class [mscorlib]System.Action`1<int32> 'CS$<>9__CachedAnonymousMethodDelegate4'
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      ret
    }

    .method private hidebysig static void 
            '<Test>b__1'(int32 x) cil managed
    {
      ret
    }
  }

  // Need some static method 'Test' with 'x' in scope.
  .method private hidebysig static void 
          Test(int32 x) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ret
  }
}";
            var module = ExpressionCompilerTestHelpers.GetModuleInstanceForIL(source);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var context = CreateMethodContext(runtime, methodName: "C.<>c__DisplayClass2.<Test>b__1");

            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("x", out error, testData);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void ConditionalAccessExpressionType()
        {
            var source =
@"class C
{
    int F()
    {
        return 0;
    }
    C G()
    {
        return null;
    }
    void M()
    {
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("this?.F()", out error, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                Assert.Equal(((MethodSymbol)methodData.Method).ReturnType.ToDisplayString(), "int?");
                methodData.VerifyIL(
@"{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (int? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""int?""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  call       ""int C.F()""
  IL_0013:  newobj     ""int?..ctor(int)""
  IL_0018:  ret
}");

                testData = new CompilationTestData();
                result = context.CompileExpression("(new C())?.G()?.F()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                Assert.Equal(((MethodSymbol)methodData.Method).ReturnType.ToDisplayString(), "int?");

                testData = new CompilationTestData();
                result = context.CompileExpression("G()?.M()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                Assert.True(((MethodSymbol)methodData.Method).ReturnsVoid);
                methodData.VerifyIL(
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""C C.G()""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  pop
  IL_000a:  ret
  IL_000b:  call       ""void C.M()""
  IL_0010:  ret
}");
            });
        }

        [Fact]
        public void CallerInfoAttributes()
        {
            var source =
@"using System.Runtime.CompilerServices;
class C
{
    static object F(
        [CallerFilePath]string path = null,
        [CallerMemberName]string member = null,
        [CallerLineNumber]int line = 0)
    {
        return string.Format(""[{0}] [{1}] [{2}]"", path, member, line);
    }
    static void Main()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.Main");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("F()", out error, testData);
                // Currently, the name of the evaluation method is used for
                // [CallerMemberName] so "F()" will generate "[] [<>m0] [1]".
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldstr      """"
  IL_0005:  ldstr      ""<>m0""
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""object C.F(string, string, int)""
  IL_0010:  ret
}");
            });
        }

        [Fact]
        public void ExternAlias()
        {
            var source = @"
extern alias X;
using SXL = X::System.Xml.Linq;
using LO = X::System.Xml.Linq.LoadOptions;
using X::System.Xml.Linq;

class C
{
    int M()
    {
        X::System.Xml.Linq.LoadOptions.None.ToString();
        return 1;
    }
}
";
            var expectedIL = @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (System.Xml.Linq.LoadOptions V_0,
                System.Xml.Linq.LoadOptions V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_1
  IL_0004:  constrained. ""System.Xml.Linq.LoadOptions""
  IL_000a:  callvirt   ""string object.ToString()""
  IL_000f:  ret
}
";

            var comp = CreateStandardCompilation(source, new[] { SystemXmlLinqRef.WithAliases(ImmutableArray.Create("X")) });
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("SXL.LoadOptions.None.ToString()", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);

                testData = new CompilationTestData();
                result = context.CompileExpression("LO.None.ToString()", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);

                testData = new CompilationTestData();
                result = context.CompileExpression("LoadOptions.None.ToString()", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);

                testData = new CompilationTestData();
                result = context.CompileExpression("X.System.Xml.Linq.LoadOptions.None.ToString()", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);

                testData = new CompilationTestData();
                result = context.CompileExpression("X::System.Xml.Linq.LoadOptions.None.ToString()", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);
            });
        }

        [Fact]
        public void ExternAliasAndGlobal()
        {
            var source = @"
extern alias X;
using A = X::System.Xml.Linq;
using B = global::System.Xml.Linq;

class C
{
    int M()
    {
        A.LoadOptions.None.ToString();
        B.LoadOptions.None.ToString();
        return 1;
    }
}
";
            var expectedIL = @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (System.Xml.Linq.LoadOptions V_0,
                System.Xml.Linq.LoadOptions V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_1
  IL_0004:  constrained. ""System.Xml.Linq.LoadOptions""
  IL_000a:  callvirt   ""string object.ToString()""
  IL_000f:  ret
}
";

            var comp = CreateStandardCompilation(source, new[] { SystemXmlLinqRef.WithAliases(ImmutableArray.Create("global", "X")) });
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("A.LoadOptions.None.ToString()", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);

                testData = new CompilationTestData();
                result = context.CompileExpression("B.LoadOptions.None.ToString()", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);
            });
        }

        [Fact]
        public void ExternAliasForMultipleAssemblies()
        {
            var source = @"
extern alias X;

class C
{
    int M()
    {
        X::System.Xml.Linq.LoadOptions.None.ToString();
        var d = new X::System.Xml.XmlDocument();
        return 1;
    }
}
";

            var comp = CreateStandardCompilation(
                source,
                new[]
                {
                    SystemXmlLinqRef.WithAliases(ImmutableArray.Create("X")),
                    SystemXmlRef.WithAliases(ImmutableArray.Create("X"))
                });

            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("new X::System.Xml.XmlDocument()", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (System.Xml.Linq.LoadOptions V_0)
  IL_0000:  newobj     ""System.Xml.XmlDocument..ctor()""
  IL_0005:  ret
}
");

                testData = new CompilationTestData();
                result = context.CompileExpression("X::System.Xml.Linq.LoadOptions.None", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (System.Xml.Linq.LoadOptions V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}
");
            });
        }

        [Fact]
        [WorkItem(1055825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1055825")]
        public void FieldLikeEvent()
        {
            var source = @"
class C
{
    event System.Action E;

    void M()
    {
    }
}
";
            var comp = CreateStandardCompilation(source);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var actionType = context.Compilation.GetWellKnownType(WellKnownType.System_Action);

                ResultProperties resultProperties;
                string error;
                CompilationTestData testData;
                CompileResult result;
                CompilationTestData.MethodData methodData;

                // Inspect the value.
                testData = new CompilationTestData();
                result = context.CompileExpression("E", out resultProperties, out error, testData);
                Assert.Null(error);
                Assert.Equal(DkmClrCompilationResultFlags.None, resultProperties.Flags);
                methodData = testData.GetMethodData("<>x.<>m0");
                Assert.Equal(actionType, methodData.Method.ReturnType);
                methodData.VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""System.Action C.E""
  IL_0006:  ret
}
");

                // Invoke the delegate.
                testData = new CompilationTestData();
                result = context.CompileExpression("E()", out resultProperties, out error, testData);
                Assert.Null(error);
                Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
                methodData = testData.GetMethodData("<>x.<>m0");
                Assert.True(methodData.Method.ReturnsVoid);
                methodData.VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""System.Action C.E""
  IL_0006:  callvirt   ""void System.Action.Invoke()""
  IL_000b:  ret
}
");

                // Assign to the event.
                testData = new CompilationTestData();
                result = context.CompileExpression("E = null", out resultProperties, out error, testData);
                Assert.Null(error);
                Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
                methodData = testData.GetMethodData("<>x.<>m0");
                Assert.Equal(actionType, methodData.Method.ReturnType);
                methodData.VerifyIL(@"
{
  // Code size       11 (0xb)
  .maxstack  3
  .locals init (System.Action V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  stfld      ""System.Action C.E""
  IL_0009:  ldloc.0
  IL_000a:  ret
}
");

                // Event (compound) assignment.
                testData = new CompilationTestData();
                result = context.CompileExpression("E += null", out resultProperties, out error, testData);
                Assert.Null(error);
                Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
                methodData = testData.GetMethodData("<>x.<>m0");
                Assert.True(methodData.Method.ReturnsVoid);
                methodData.VerifyIL(@"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  callvirt   ""void C.E.add""
  IL_0007:  ret
}
");
            });
        }

        [Fact]
        [WorkItem(1055825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1055825")]
        public void FieldLikeEvent_WinRT()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1<class [mscorlib]System.Action> E

  .method public hidebysig specialname instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
          add_E(class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname instance void 
          remove_E(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .event [mscorlib]System.Action E
  {
    .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C::add_E(class [mscorlib]System.Action)
    .removeon instance void C::remove_E(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
  } // end of event C::E

  .method public hidebysig instance void 
          M() cil managed
  {
    ret
  }
} // end of class C
";
            var module = ExpressionCompilerTestHelpers.GetModuleInstanceForIL(ilSource);
            var runtime = CreateRuntimeInstance(module, WinRtRefs);
            var context = CreateMethodContext(runtime, "C.M");

            var actionType = context.Compilation.GetWellKnownType(WellKnownType.System_Action);

            ResultProperties resultProperties;
            string error;
            CompilationTestData testData;
            CompileResult result;
            CompilationTestData.MethodData methodData;

            // Inspect the value.
            testData = new CompilationTestData();
            result = context.CompileExpression("E", out resultProperties, out error, testData);
            Assert.Null(error);
            Assert.Equal(DkmClrCompilationResultFlags.None, resultProperties.Flags);
            methodData = testData.GetMethodData("<>x.<>m0");
            Assert.Equal(actionType, methodData.Method.ReturnType);
            methodData.VerifyIL(@"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.E""
  IL_0006:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000b:  callvirt   ""System.Action System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.InvocationList.get""
  IL_0010:  ret
}
");

            // Invoke the delegate.
            testData = new CompilationTestData();
            result = context.CompileExpression("E()", out resultProperties, out error, testData);
            Assert.Null(error);
            Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
            methodData = testData.GetMethodData("<>x.<>m0");
            Assert.True(methodData.Method.ReturnsVoid);
            methodData.VerifyIL(@"
{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.E""
  IL_0006:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000b:  callvirt   ""System.Action System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.InvocationList.get""
  IL_0010:  callvirt   ""void System.Action.Invoke()""
  IL_0015:  ret
}
");

            // Assign to the event.
            testData = new CompilationTestData();
            result = context.CompileExpression("E = null", out resultProperties, out error, testData);
            Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
            methodData = testData.GetMethodData("<>x.<>m0");
            Assert.True(methodData.Method.ReturnsVoid);
            methodData.VerifyIL(@"
{
  // Code size       48 (0x30)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""void C.E.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0011:  ldarg.0
  IL_0012:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.E.add""
  IL_0018:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001d:  ldarg.0
  IL_001e:  ldftn      ""void C.E.remove""
  IL_0024:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0029:  ldnull
  IL_002a:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_002f:  ret
}
");

            // Event (compound) assignment.
            testData = new CompilationTestData();
            result = context.CompileExpression("E += null", out resultProperties, out error, testData);
            Assert.Null(error);
            Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
            methodData = testData.GetMethodData("<>x.<>m0");
            Assert.True(methodData.Method.ReturnsVoid);
            methodData.VerifyIL(@"
{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.E.add""
  IL_0007:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  ldarg.0
  IL_000d:  ldftn      ""void C.E.remove""
  IL_0013:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0018:  ldnull
  IL_0019:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_001e:  ret
}
");
        }

        [WorkItem(1079749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079749")]
        [Fact]
        public void RangeVariableError()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}";

            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                context.CompileExpression(
                    "from c in \"ABC\" select c",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                Assert.Equal(new AssemblyIdentity("System.Core"), missingAssemblyIdentities.Single());
                Assert.Equal(error, "error CS1935: Could not find an implementation of the query pattern for source type 'string'.  'Select' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?");
            });
        }

        [WorkItem(1079762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079762")]
        [Fact]
        public void Bug1079762()
        {
            var source =
@"class C
{
    static void F(System.Func<object, bool> f, object o)
    {
        f(o);
    }
    static void M(object x, object y)
    {
        F(z => z != null && x != null, 3);
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__DisplayClass1_0.<M>b__0");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("z", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
                testData = new CompilationTestData();
                context.CompileExpression("y", out error, testData);
                Assert.Equal(error, "error CS0103: The name 'y' does not exist in the current context");
            });
        }

        [WorkItem(1079762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079762")]
        [Fact]
        public void LambdaParameter()
        {
            var source =
@"class C
{
    static void M()
    {
        System.Func<object, bool> f = z => z != null;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c.<M>b__0_0");
                ResultProperties resultProperties;
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("z", out resultProperties, out error, testData);
                Assert.Null(error);
                Assert.Equal(DkmClrCompilationResultFlags.None, resultProperties.Flags);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}");
            });
        }

        [WorkItem(1084059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084059")]
        [Fact]
        public void StaticTypeImport()
        {
            var source = @"
using static System.Math;

class C
{
    static void M()
    {
        Max(1, 2);
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                ResultProperties resultProperties;
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("Min(1, 2)", out resultProperties, out error, testData);
                Assert.Null(error);
                Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  call       ""int System.Math.Min(int, int)""
  IL_0007:  ret
}");
            });
        }

        [WorkItem(1014763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1014763")]
        [Fact]
        public void NonStateMachineTypeParameter()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<T> I<T>(T[] tt)
    {
        return tt;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.I");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("typeof(T)", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0<T>").VerifyIL(@"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerable<T> V_0)
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ret
}");
            });
        }

        [WorkItem(1014763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1014763")]
        [Fact]
        public void StateMachineTypeParameter()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<T> I<T>(T[] tt)
    {
        foreach (T t in tt)
        {
            yield return t;
        }
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<I>d__0.MoveNext");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("typeof(T)", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T>.<>m0").VerifyIL(@"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ret
}");
            });
        }

        [WorkItem(1085642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085642")]
        [Fact]
        public void ModuleWithBadImageFormat()
        {
            var source = @"
class C
{
    int F = 1;
    static void M()
    {
    }
}";
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);

            var modulesBuilder = ArrayBuilder<ModuleInstance>.GetInstance();

            using (var pinnedMetadata = new PinnedBlob(CommonResources.NoValidTables))
            {
                var corruptMetadata = ModuleInstance.Create(pinnedMetadata.Pointer, pinnedMetadata.Size, default(Guid));

                var runtime = RuntimeInstance.Create(new[] { corruptMetadata, comp.ToModuleInstance(), MscorlibRef.ToModuleInstance() });
                var context = CreateMethodContext(runtime, "C.M");
                ResultProperties resultProperties;
                string error;
                var testData = new CompilationTestData();
                // Verify that we can still evaluate expressions for modules that are not corrupt.
                context.CompileExpression("(new C()).F", out resultProperties, out error, testData);
                Assert.Null(error);
                Assert.Equal(DkmClrCompilationResultFlags.None, resultProperties.Flags);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldfld      ""int C.F""
  IL_000a:  ret
}");
            }
        }

        [WorkItem(1089688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089688")]
        [Fact]
        public void MissingType()
        {
            var libSource = @"
public class Missing { }
";

            var source = @"
public class C
{
    Missing field;    

    public void M(Missing parameter)
    {
        Missing local;
    }
}
";
            var libRef = CreateStandardCompilation(libSource, assemblyName: "Lib").EmitToImageReference();
            var comp = CreateStandardCompilation(source, new[] { libRef }, TestOptions.DebugDll);

            WithRuntimeInstance(comp, new[] { MscorlibRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var expectedError = "error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.";
                var expectedMissingAssemblyIdentity = new AssemblyIdentity("Lib");

                ResultProperties resultProperties;
                string actualError;
                ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;

                Action<string> verify = expr =>
                {
                    context.CompileExpression(
                        expr,
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        out resultProperties,
                        out actualError,
                        out actualMissingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData: null);
                    Assert.Equal(expectedError, actualError);
                    Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
                };

                verify("M(null)");
                verify("field");
                verify("field.Method");
                verify("parameter");
                verify("parameter.Method");
                verify("local");
                verify("local.Method");

                // Note that even expressions that don't require the missing type will fail because
                // the method we synthesize refers to the original locals and parameters.
                verify("0");
            });
        }

        [WorkItem(1089688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089688")]
        [Fact]
        public void UseSiteWarning()
        {
            var signedDllOptions = TestOptions.ReleaseDll.
                WithCryptoKeyFile(SigningTestHelpers.KeyPairFile).
                WithStrongNameProvider(new SigningTestHelpers.VirtualizedStrongNameProvider(ImmutableArray.Create<string>()));

            var libBTemplate = @"
[assembly: System.Reflection.AssemblyVersion(""{0}.0.0.0"")]
public class B {{ }}
";

            var libBv1Ref = CreateStandardCompilation(string.Format(libBTemplate, "1"), assemblyName: "B", options: signedDllOptions).EmitToImageReference();
            var libBv2Ref = CreateStandardCompilation(string.Format(libBTemplate, "2"), assemblyName: "B", options: signedDllOptions).EmitToImageReference();

            var libASource = @"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]

public class A : B
{
}
";

            var libAv1Ref = CreateStandardCompilation(libASource, new[] { libBv1Ref }, assemblyName: "A", options: signedDllOptions).EmitToImageReference();

            var source = @"
public class Source
{
    public void Test()
    {
        object o = new A();
    }
}
";

            var comp = CreateStandardCompilation(source, new[] { libAv1Ref, libBv2Ref }, TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // warning CS1701: Assuming assembly reference 'B, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' used by 'A' matches identity 'B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' of 'B', you may need to supply runtime policy
                Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin).WithArguments("B, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "A", "B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "B").WithLocation(1, 1));

            WithRuntimeInstance(comp, new[] { MscorlibRef, libAv1Ref, libBv2Ref }, runtime =>
            {
                var context = CreateMethodContext(runtime, "Source.Test");

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("new A()", out error, testData);
                Assert.Null(error);
                var methodData = testData.GetMethodData("<>x.<>m0");

                // Even though the method's return type has a use-site warning, we are able to evaluate the expression.
                Assert.Equal(ErrorCode.WRN_UnifyReferenceMajMin, (ErrorCode)((MethodSymbol)methodData.Method).ReturnType.GetUseSiteDiagnostic().Code);
                methodData.VerifyIL(@"
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (object V_0) //o
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  ret
}");
            });
        }

        [WorkItem(1090458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090458")]
        [Fact]
        public void ObsoleteAttribute()
        {
            var source = @"
using System;
using System.Diagnostics;
　
class C
{
    static void Main()
    {
        C c = new C();
    }

    [Obsolete(""Hello"", true)]
    int P { get; set; }
}
";
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.Main");
                ResultProperties resultProperties;
                string error;
                context.CompileExpression("c.P", out resultProperties, out error);
                Assert.Null(error);
            });
        }

        [WorkItem(1090458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090458")]
        [Fact]
        public void DeprecatedAttribute()
        {
            var source = @"
using System;
using Windows.Foundation.Metadata;
　
class C
{
    static void Main()
    {
        C c = new C();
    }

    [Deprecated(""Hello"", DeprecationType.Remove, 1)]
    int P { get; set; }
}

namespace Windows.Foundation.Metadata
{
    [AttributeUsage(
        AttributeTargets.Class | 
        AttributeTargets.Struct | 
        AttributeTargets.Enum | 
        AttributeTargets.Constructor | 
        AttributeTargets.Method | 
        AttributeTargets.Property | 
        AttributeTargets.Field | 
        AttributeTargets.Event | 
        AttributeTargets.Interface | 
        AttributeTargets.Delegate, AllowMultiple = true)]
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(string message, DeprecationType type, uint version)
        {
        }

        public DeprecatedAttribute(string message, DeprecationType type, uint version, Type contract)
        {
        }
    }

    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}
";
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.Main");
                ResultProperties resultProperties;
                string error;
                context.CompileExpression("c.P", out resultProperties, out error);
                Assert.Null(error);
            });
        }

        [WorkItem(1089591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089591")]
        [Fact]
        public void BadPdb_MissingMethod()
        {
            var source = @"
public class C
{
    public static void Main()
    {
    }
}
";
            var comp = CreateStandardCompilation(source);
            var peImage = comp.EmitToArray();
            var symReader = new MockSymUnmanagedReader(ImmutableDictionary<int, MethodDebugInfoBytes>.Empty);
            var module = ModuleInstance.Create(peImage, symReader);

            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var evalContext = CreateMethodContext(runtime, "C.Main");
            string error;
            var testData = new CompilationTestData();
            evalContext.CompileExpression("1", out error, testData);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
");
        }

        [WorkItem(1108133, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108133")]
        [Fact]
        public void SymUnmanagedReaderNotImplemented()
        {
            var source = @"
public class C
{
    public static void Main()
    {
    }
}
";
            var comp = CreateStandardCompilation(source);
            var peImage = comp.EmitToArray();
            var module = ModuleInstance.Create(peImage, NotImplementedSymUnmanagedReader.Instance);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var evalContext = CreateMethodContext(runtime, "C.Main");

            string error;
            var testData = new CompilationTestData();
            evalContext.CompileExpression("1", out error, testData);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
");
        }

        [WorkItem(1115543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115543")]
        [Fact]
        public void MethodTypeParameterInLambda()
        {
            var source = @"
using System;

public class C<T>
{
    public void M<U>()
    {
        Func<U, int> getInt = u =>
        {
            return u.GetHashCode();
        };

        var result = getInt(default(U));
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.<>c__0.<M>b__0_0");

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("typeof(U)", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldtoken    ""U""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ret
}
");
            });
        }

        [WorkItem(1136085, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1136085")]
        [Fact]
        public void TypeofOpenGenericType()
        {
            var source = @"
using System;

public class C
{
    public void M()
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source);
            WithRuntimeInstance(compilation, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;
                var expectedIL = @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldtoken    ""System.Action<T>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ret
}";

                var testData = new CompilationTestData();
                context.CompileExpression("typeof(Action<>)", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);

                testData = new CompilationTestData();
                context.CompileExpression("typeof(Action<>  )", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);

                context.CompileExpression("typeof(Action<Action<>>)", out error, testData);
                Assert.Equal("error CS7003: Unexpected use of an unbound generic name", error);

                context.CompileExpression("typeof(Action<Action< > > )", out error);
                Assert.Equal("error CS7003: Unexpected use of an unbound generic name", error);

                context.CompileExpression("typeof(Action<>a)", out error);
                Assert.Equal("error CS1026: ) expected", error);
            });
        }

        [WorkItem(1068138, "DevDiv")]
        [Fact]
        public void GetSymAttributeByVersion()
        {
            var source1 = @"
public class C
{
    public static void M()
    {
        int x = 1;
    }
}";

            var source2 = @"
public class C
{
    public static void M()
    {
        int x = 1;
        string y = ""a"";
    }
}";
            var comp1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll);
            var comp2 = CreateStandardCompilation(source2, options: TestOptions.DebugDll);

            using (MemoryStream
                peStream1Unused = new MemoryStream(),
                peStream2 = new MemoryStream(),
                pdbStream1 = new MemoryStream(),
                pdbStream2 = new MemoryStream())
            {
                Assert.True(comp1.Emit(peStream1Unused, pdbStream1).Success);
                Assert.True(comp2.Emit(peStream2, pdbStream2).Success);

                pdbStream1.Position = 0;
                pdbStream2.Position = 0;
                peStream2.Position = 0;

                var symReader = SymReaderFactory.CreateReader(pdbStream1);
                symReader.UpdateSymbolStore(pdbStream2);

                var module = ModuleInstance.Create(peStream2.ToImmutable(), symReader);
                var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef, ExpressionCompilerTestHelpers.IntrinsicAssemblyReference });

                ImmutableArray<MetadataBlock> blocks;
                Guid moduleVersionId;
                ISymUnmanagedReader symReader2;
                int methodToken;
                int localSignatureToken;
                GetContextState(runtime, "C.M", out blocks, out moduleVersionId, out symReader2, out methodToken, out localSignatureToken);

                Assert.Same(symReader, symReader2);

                AssertEx.SetEqual(symReader.GetLocalNames(methodToken, methodVersion: 1), "x");
                AssertEx.SetEqual(symReader.GetLocalNames(methodToken, methodVersion: 2), "x", "y");

                var context1 = EvaluationContext.CreateMethodContext(
                    default(CSharpMetadataContext),
                    blocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: 0,
                    localSignatureToken: localSignatureToken);

                var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                string typeName;
                context1.CompileGetLocals(
                    locals,
                    argumentsOnly: false,
                    typeName: out typeName,
                    testData: null);
                AssertEx.SetEqual(locals.Select(l => l.LocalName), "x");

                var context2 = EvaluationContext.CreateMethodContext(
                    default(CSharpMetadataContext),
                    blocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 2,
                    ilOffset: 0,
                    localSignatureToken: localSignatureToken);

                locals.Clear();
                context2.CompileGetLocals(
                    locals,
                    argumentsOnly: false,
                    typeName: out typeName,
                    testData: null);
                AssertEx.SetEqual(locals.Select(l => l.LocalName), "x", "y");
            }
        }

        /// <summary>
        /// Ignore accessibility in lambda rewriter.
        /// </summary>
        [WorkItem(1618, "https://github.com/dotnet/roslyn/issues/1618")]
        [Fact]
        public void LambdaRewriterIgnoreAccessibility()
        {
            var source =
@"using System.Linq;
class C
{
    static void M()
    {
        var q = new[] { new C() }.AsQueryable();
    }
}";
            var compilation0 = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.M");
                var testData = new CompilationTestData();
                string error;
                context.CompileExpression("q.Where(c => true)", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       64 (0x40)
  .maxstack  6
  .locals init (System.Linq.IQueryable<C> V_0, //q
                System.Linq.Expressions.ParameterExpression V_1)
  IL_0000:  ldloc.0
  IL_0001:  ldtoken    ""C""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""c""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.1
  IL_0016:  ldc.i4.1
  IL_0017:  box        ""bool""
  IL_001c:  ldtoken    ""bool""
  IL_0021:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0026:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_002b:  ldc.i4.1
  IL_002c:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0031:  dup
  IL_0032:  ldc.i4.0
  IL_0033:  ldloc.1
  IL_0034:  stelem.ref
  IL_0035:  call       ""System.Linq.Expressions.Expression<System.Func<C, bool>> System.Linq.Expressions.Expression.Lambda<System.Func<C, bool>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003a:  call       ""System.Linq.IQueryable<C> System.Linq.Queryable.Where<C>(System.Linq.IQueryable<C>, System.Linq.Expressions.Expression<System.Func<C, bool>>)""
  IL_003f:  ret
}");
            });
        }

        /// <summary>
        /// Ignore accessibility in async rewriter.
        /// </summary>
        [Fact]
        public void AsyncRewriterIgnoreAccessibility()
        {
            var source =
@"using System;
using System.Threading.Tasks;
class C
{
    static void F<T>(Func<Task<T>> f)
    {
    }
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.M");
                var testData = new CompilationTestData();
                string error;
                context.CompileExpression("F(async () => new C())", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<System.Threading.Tasks.Task<C>> <>x.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_000e:  ldftn      ""System.Threading.Tasks.Task<C> <>x.<>c.<<>m0>b__0_0()""
  IL_0014:  newobj     ""System.Func<System.Threading.Tasks.Task<C>>..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""System.Func<System.Threading.Tasks.Task<C>> <>x.<>c.<>9__0_0""
  IL_001f:  call       ""void C.F<C>(System.Func<System.Threading.Tasks.Task<C>>)""
  IL_0024:  ret
}");
            });
        }

        [Fact]
        public void CapturedLocalInLambda()
        {
            var source = @"
using System;
class C
{
    void M(Func<int> f)
    {
        int x = 42;
        M(() => x);
    }
}";
            var comp = CreateCompilationWithMscorlib45(source);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("M(() => x)", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                <>x.<>c__DisplayClass0_0 V_1) //CS$<>8__locals0
  IL_0000:  newobj     ""<>x.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  ldloc.0
  IL_0008:  stfld      ""C.<>c__DisplayClass0_0 <>x.<>c__DisplayClass0_0.CS$<>8__locals0""
  IL_000d:  ldarg.0
  IL_000e:  ldloc.1
  IL_000f:  ldftn      ""int <>x.<>c__DisplayClass0_0.<<>m0>b__0()""
  IL_0015:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001a:  callvirt   ""void C.M(System.Func<int>)""
  IL_001f:  ret
}");
            });
        }

        [WorkItem(3309, "https://github.com/dotnet/roslyn/issues/3309")]
        [Fact]
        public void NullAnonymousTypeInstance()
        {
            var source =
@"class C
{
    static void Main()
    {
    }
}";
            var testData = Evaluate(source, OutputKind.ConsoleApplication, "C.Main", "false ? new { P = 1 } : null");
            var methodData = testData.GetMethodData("<>x.<>m0");
            var returnType = (NamedTypeSymbol)methodData.Method.ReturnType;
            Assert.True(returnType.IsAnonymousType);
            methodData.VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
        }

        /// <summary>
        /// DkmClrInstructionAddress.ILOffset is set to uint.MaxValue
        /// if the instruction does not map to an IL offset.
        /// </summary>
        [WorkItem(1185315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185315")]
        [Fact]
        public void NoILOffset()
        {
            var source =
@"class C
{
    static void M(int x)
    {
        int y;
    }
}";
            var compilation0 = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                ImmutableArray<MetadataBlock> blocks;
                Guid moduleVersionId;
                ISymUnmanagedReader symReader;
                int methodToken;
                int localSignatureToken;
                GetContextState(runtime, "C.M", out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);

                var context = EvaluationContext.CreateMethodContext(
                    blocks.ToCompilation(),
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ExpressionCompilerTestHelpers.NoILOffset,
                    localSignatureToken: localSignatureToken);

                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("x + y", out error, testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (int V_0) //y
  IL_0000:  ldarg.0
  IL_0001:  ldloc.0
  IL_0002:  add
  IL_0003:  ret
}");

                // Verify the context is re-used for ILOffset == 0.
                var previous = context;
                context = EvaluationContext.CreateMethodContext(
                    new CSharpMetadataContext(blocks, previous),
                    blocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: 0,
                    localSignatureToken: localSignatureToken);
                Assert.Same(previous, context);

                // Verify the context is re-used for NoILOffset.
                previous = context;
                context = EvaluationContext.CreateMethodContext(
                    new CSharpMetadataContext(blocks, previous),
                    blocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ExpressionCompilerTestHelpers.NoILOffset,
                    localSignatureToken: localSignatureToken);
                Assert.Same(previous, context);
            });
        }

        [WorkItem(4098, "https://github.com/dotnet/roslyn/issues/4098")]
        [Fact]
        public void SelectAnonymousType()
        {
            var source =
@"using System.Collections.Generic;
using System.Linq;

class C
{
    static void M(List<int> list)
    {
        var useLinq = list.Last();
    }
}";
            var compilation0 = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("from x in list from y in list where x > 0 select new { x, y };", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size      140 (0x8c)
  .maxstack  4
  .locals init (int V_0, //useLinq
                <>x.<>c__DisplayClass0_0 V_1) //CS$<>8__locals0
  IL_0000:  newobj     ""<>x.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""System.Collections.Generic.List<int> <>x.<>c__DisplayClass0_0.list""
  IL_000d:  ldloc.1
  IL_000e:  ldfld      ""System.Collections.Generic.List<int> <>x.<>c__DisplayClass0_0.list""
  IL_0013:  ldloc.1
  IL_0014:  ldftn      ""System.Collections.Generic.IEnumerable<int> <>x.<>c__DisplayClass0_0.<<>m0>b__0(int)""
  IL_001a:  newobj     ""System.Func<int, System.Collections.Generic.IEnumerable<int>>..ctor(object, System.IntPtr)""
  IL_001f:  ldsfld     ""System.Func<int, int, <anonymous type: int x, int y>> <>x.<>c.<>9__0_1""
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_003e
  IL_0027:  pop
  IL_0028:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_002d:  ldftn      ""<anonymous type: int x, int y> <>x.<>c.<<>m0>b__0_1(int, int)""
  IL_0033:  newobj     ""System.Func<int, int, <anonymous type: int x, int y>>..ctor(object, System.IntPtr)""
  IL_0038:  dup
  IL_0039:  stsfld     ""System.Func<int, int, <anonymous type: int x, int y>> <>x.<>c.<>9__0_1""
  IL_003e:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: int x, int y>> System.Linq.Enumerable.SelectMany<int, int, <anonymous type: int x, int y>>(System.Collections.Generic.IEnumerable<int>, System.Func<int, System.Collections.Generic.IEnumerable<int>>, System.Func<int, int, <anonymous type: int x, int y>>)""
  IL_0043:  ldsfld     ""System.Func<<anonymous type: int x, int y>, bool> <>x.<>c.<>9__0_2""
  IL_0048:  dup
  IL_0049:  brtrue.s   IL_0062
  IL_004b:  pop
  IL_004c:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_0051:  ldftn      ""bool <>x.<>c.<<>m0>b__0_2(<anonymous type: int x, int y>)""
  IL_0057:  newobj     ""System.Func<<anonymous type: int x, int y>, bool>..ctor(object, System.IntPtr)""
  IL_005c:  dup
  IL_005d:  stsfld     ""System.Func<<anonymous type: int x, int y>, bool> <>x.<>c.<>9__0_2""
  IL_0062:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: int x, int y>> System.Linq.Enumerable.Where<<anonymous type: int x, int y>>(System.Collections.Generic.IEnumerable<<anonymous type: int x, int y>>, System.Func<<anonymous type: int x, int y>, bool>)""
  IL_0067:  ldsfld     ""System.Func<<anonymous type: int x, int y>, <anonymous type: int x, int y>> <>x.<>c.<>9__0_3""
  IL_006c:  dup
  IL_006d:  brtrue.s   IL_0086
  IL_006f:  pop
  IL_0070:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_0075:  ldftn      ""<anonymous type: int x, int y> <>x.<>c.<<>m0>b__0_3(<anonymous type: int x, int y>)""
  IL_007b:  newobj     ""System.Func<<anonymous type: int x, int y>, <anonymous type: int x, int y>>..ctor(object, System.IntPtr)""
  IL_0080:  dup
  IL_0081:  stsfld     ""System.Func<<anonymous type: int x, int y>, <anonymous type: int x, int y>> <>x.<>c.<>9__0_3""
  IL_0086:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: int x, int y>> System.Linq.Enumerable.Select<<anonymous type: int x, int y>, <anonymous type: int x, int y>>(System.Collections.Generic.IEnumerable<<anonymous type: int x, int y>>, System.Func<<anonymous type: int x, int y>, <anonymous type: int x, int y>>)""
  IL_008b:  ret
}");
            });
        }

        [WorkItem(2501, "https://github.com/dotnet/roslyn/issues/2501")]
        [Fact]
        public void ImportsInAsyncLambda()
        {
            var source =
@"namespace N
{
    using System.Linq;
    class C
    {
        static void M()
        {
            System.Action f = async () =>
            {
                var c = new[] { 1, 2, 3 };
                c.Select(i => i);
            };
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(
                source,
                options: TestOptions.DebugDll,
                references: new[] { SystemCoreRef });

            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "N.C.<>c.<<M>b__0_0>d.MoveNext");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("c.Where(n => n > 0)", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (int V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int[] N.C.<>c.<<M>b__0_0>d.<c>5__1""
  IL_0006:  ldsfld     ""System.Func<int, bool> <>x.<>c.<>9__0_0""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0025
  IL_000e:  pop
  IL_000f:  ldsfld     ""<>x.<>c <>x.<>c.<>9""
  IL_0014:  ldftn      ""bool <>x.<>c.<<>m0>b__0_0(int)""
  IL_001a:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_001f:  dup
  IL_0020:  stsfld     ""System.Func<int, bool> <>x.<>c.<>9__0_0""
  IL_0025:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_002a:  ret
}");
            });
        }

        [Fact]
        public void AssignDefaultToLocal()
        {
            var source = @"
class C
{
    void Test()
    {
        int a = 1;
    }
}
";
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                ResultProperties resultProperties;
                string error;
                var testData = new CompilationTestData();
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                context.CompileAssignment("a", "default", NoAliases, DebuggerDiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
                Assert.Null(error);
                Assert.Empty(missingAssemblyIdentities);

                Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect, resultProperties.Flags);
                Assert.Equal(default(DkmEvaluationResultCategory), resultProperties.Category); // Not Data
                Assert.Equal(default(DkmEvaluationResultAccessType), resultProperties.AccessType); // Not Public
                Assert.Equal(default(DkmEvaluationResultStorageType), resultProperties.StorageType);
                Assert.Equal(default(DkmEvaluationResultTypeModifierFlags), resultProperties.ModifierFlags); // Not Virtual
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (int V_0) //a
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ret
}");

                testData = new CompilationTestData();
                context.CompileExpression("a = default;", DkmEvaluationFlags.None, ImmutableArray<Alias>.Empty, out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (int V_0) //a
  IL_0000:  ldc.i4.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ret
}");
                testData = new CompilationTestData();
                context.CompileExpression("int b = default;", DkmEvaluationFlags.None, ImmutableArray<Alias>.Empty, out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       43 (0x2b)
  .maxstack  4
  .locals init (int V_0, //a
                System.Guid V_1)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""b""
  IL_000f:  ldloca.s   V_1
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.1
  IL_0018:  ldnull
  IL_0019:  call       ""void Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, string, System.Guid, byte[])""
  IL_001e:  ldstr      ""b""
  IL_0023:  call       ""int Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress<int>(string)""
  IL_0028:  ldc.i4.0
  IL_0029:  stind.i4
  IL_002a:  ret
}");

                testData = new CompilationTestData();
                context.CompileExpression("default", DkmEvaluationFlags.None, ImmutableArray<Alias>.Empty, out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //a
  IL_0000:  ldnull
  IL_0001:  ret
}");
                Assert.Equal(SpecialType.System_Object, testData.GetMethodData("<>x.<>m0").Method.ReturnType.SpecialType);

                testData = new CompilationTestData();
                context.CompileExpression("null", DkmEvaluationFlags.None, ImmutableArray<Alias>.Empty, out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0) //a
  IL_0000:  ldnull
  IL_0001:  ret
}");
        });
        }
    }
}
