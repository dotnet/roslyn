// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public class CompilationOptionsReaderTests : CSharpTestBase
    {
        private CompilationOptionsReader GetOptionsReader(Compilation compilation)
        {
            compilation.VerifyDiagnostics();
            var peBytes = compilation.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
            var originalReader = new PEReader(peBytes);
            var originalPdbReader = originalReader.GetEmbeddedPdbMetadataReader();
            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger("RoundTripVerification");
            return new CompilationOptionsReader(logger, originalPdbReader, originalReader);
        }

        [Fact]
        public void PublicKeyNetModule()
        {
            var compilation = CreateCompilation(
                options: TestOptions.DebugModule,
                source: @"
class C { }
");

            var reader = GetOptionsReader(compilation);
            Assert.Null(reader.GetPublicKey());
        }

        [Fact]
        public void OutputKind()
        {
            foreach (OutputKind kind in Enum.GetValues(typeof(OutputKind)))
            {
                var compilation = CreateCompilation(
                    options: new CSharpCompilationOptions(outputKind: kind),
                    source: @"
class Program {
    public static void Main() { }
}");
                var reader = GetOptionsReader(compilation);
                Assert.Equal(kind, reader.GetMetadataCompilationOptions().OptionToEnum<OutputKind>(CompilationOptionNames.OutputKind));
            }
        }
    }
}
