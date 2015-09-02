// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class CommandLineScriptTests : CSharpTestBase
    {
        // Simulates a sensible override of object.Equals.
        private class TestDocumentationProviderEquals : DocumentationProvider
        {
            protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken) => "";
            public override bool Equals(object obj) => obj != null && this.GetType() == obj.GetType();
            public override int GetHashCode() => GetType().GetHashCode();
        }

        // Simulates no override of object.Equals.
        private class TestDocumentationProviderNoEquals : DocumentationProvider
        {
            protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken) => "";
            public override bool Equals(object obj) => ReferenceEquals(this, obj);
            public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
        }

        private class TestMetadataReferenceProvider : Microsoft.CodeAnalysis.MetadataFileReferenceProvider
        {
            public Func<DocumentationProvider> MakeDocumentationProvider;
            private readonly Dictionary<string, AssemblyMetadata> _cache = new Dictionary<string, AssemblyMetadata>();

            public override PortableExecutableReference GetReference(string fullPath, MetadataReferenceProperties properties = default(MetadataReferenceProperties))
            {
                AssemblyMetadata metadata;
                if (_cache.TryGetValue(fullPath, out metadata))
                {
                    return metadata.GetReference(MakeDocumentationProvider());
                }

                _cache.Add(fullPath, metadata = AssemblyMetadata.CreateFromFile(fullPath));
                return metadata.GetReference(MakeDocumentationProvider());
            }
        }

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
            var provider = new TestMetadataReferenceProvider() { MakeDocumentationProvider = () => new TestDocumentationProviderNoEquals() };

            var s1 = CSharpCompilation.CreateSubmission("s1.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("struct S { }", options),
                references: MakeReferencesViaCommandLine(provider),
                returnType: typeof(object));

            s1.VerifyDiagnostics();

            var s2 = CSharpCompilation.CreateSubmission("s2.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("System.Collections.IEnumerable Iterator() { yield return new S(); }", options),
                previousSubmission: s1,
                references: MakeReferencesViaCommandLine(provider),
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
            var provider = new TestMetadataReferenceProvider() { MakeDocumentationProvider = () => new TestDocumentationProviderEquals() };

            var s1 = CSharpCompilation.CreateSubmission("s1.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("struct S { }", options),
                references: MakeReferencesViaCommandLine(provider),
                returnType: typeof(object));

            s1.VerifyDiagnostics();

            var s2 = CSharpCompilation.CreateSubmission("s2.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("System.Collections.IEnumerable Iterator() { yield return new S(); }", options),
                previousSubmission: s1,
                references: MakeReferencesViaCommandLine(provider),
                returnType: typeof(object));

            s2.VerifyDiagnostics();

            Assert.Equal(s1.GetSpecialType(SpecialType.System_Object), s2.GetSpecialType(SpecialType.System_Object));
        }

        /// <summary>
        /// NOTE: We're going through the command line parser to mimic the approach of visual studio and the ETA.
        /// Crucially, this CommandLineArguments will use the provided TestMetadataReferenceProvider to attach a fresh
        /// DocumentationProvider to each reference.
        /// </summary>
        private static IEnumerable<MetadataReference> MakeReferencesViaCommandLine(TestMetadataReferenceProvider metadataReferenceProvider)
        {
            var commandLineArguments = CSharpCommandLineParser.Interactive.Parse(
                new string[0],
                Directory.GetDirectoryRoot("."), //NOTE: any absolute path will do - we're not going to use this.
                RuntimeEnvironment.GetRuntimeDirectory());

            return commandLineArguments.ResolveMetadataReferences(new AssemblyReferenceResolver(MetadataFileReferenceResolver.Default, metadataReferenceProvider));
        }
    }
}
