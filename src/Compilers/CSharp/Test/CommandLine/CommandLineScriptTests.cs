// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class CommandLineScriptTests : CSharpTestBase
    {
        [WorkItem(546173)]
        [Fact]
        public void CompilationChain_SystemObject_OrderDependency1()
        {
            CompilationChain_SystemObject_NotEquals();
            CompilationChain_SystemObject_Equals();
        }

        [WorkItem(546173)]
        [Fact]
        public void CompilationChain_SystemObject_OrderDependency2()
        {
            CompilationChain_SystemObject_Equals();
            CompilationChain_SystemObject_NotEquals();
        }

        [WorkItem(545665)]
        [Fact]
        public void CompilationChain_SystemObject_NotEquals()
        {
            // As in VS/ETA, make a new list of references for each submission.

            var options = new CSharpParseOptions(kind: SourceCodeKind.Interactive, documentationMode: DocumentationMode.None);
            var corLib = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib);

            var s1 = CSharpCompilation.CreateSubmission("s1.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("struct S { }", options),
                references: new[] { corLib.GetReference(documentation: new TestDocumentationProviderNoEquals()) },
                returnType: typeof(object));

            s1.VerifyDiagnostics();

            var s2 = CSharpCompilation.CreateSubmission("s2.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("System.Collections.IEnumerable Iterator() { yield return new S(); }", options),
                previousSubmission: s1,
                references: new[] { corLib.GetReference(documentation: new TestDocumentationProviderNoEquals()) },
                returnType: typeof(object));

            Assert.NotEqual(s1.GetSpecialType(SpecialType.System_Object), s2.GetSpecialType(SpecialType.System_Object));

            s2.VerifyDiagnostics(
                // (1,58): error CS0029: Cannot implicitly convert type 'S' to 'object'
                // System.Collections.IEnumerable Iterator() { yield return new S(); }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new S()").WithArguments("S", "object"));
        }

        [WorkItem(545665)]
        [Fact]
        public void CompilationChain_SystemObject_Equals()
        {
            // As in VS/ETA, make a new list of references for each submission.

            var options = new CSharpParseOptions(kind: SourceCodeKind.Interactive, documentationMode: DocumentationMode.None);
            var corLib = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib);

            var s1 = CSharpCompilation.CreateSubmission("s1.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("struct S { }", options),
                references: new[] { corLib.GetReference(documentation: new TestDocumentationProviderEquals()) },
                returnType: typeof(object));

            s1.VerifyDiagnostics();

            var s2 = CSharpCompilation.CreateSubmission("s2.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("System.Collections.IEnumerable Iterator() { yield return new S(); }", options),
                previousSubmission: s1,
                references: new[] { corLib.GetReference(documentation: new TestDocumentationProviderEquals()) },
                returnType: typeof(object));

            s2.VerifyDiagnostics();

            Assert.Equal(s1.GetSpecialType(SpecialType.System_Object), s2.GetSpecialType(SpecialType.System_Object));
        }
    }
}
