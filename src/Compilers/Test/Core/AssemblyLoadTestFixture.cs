// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class AssemblyLoadTestFixture : IDisposable
    {
        private static readonly CSharpCompilationOptions s_dllWithMaxWarningLevel = new(OutputKind.DynamicallyLinkedLibrary, warningLevel: Diagnostic.MaxWarningLevel);

        private readonly TempRoot _temp;
        private readonly TempDirectory _directory;

        public TempFile Delta1 { get; }
        public TempFile Gamma { get; }
        public TempFile Beta { get; }
        public TempFile Alpha { get; }

        public TempFile Delta2 { get; }
        public TempFile Epsilon { get; }

        public AssemblyLoadTestFixture()
        {
            _temp = new TempRoot();
            _directory = _temp.CreateDirectory();

            Delta1 = GenerateDll("Delta", _directory, @"
using System.Text;

[assembly: System.Reflection.AssemblyTitle(""Delta"")]
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]

namespace Delta
{
    public class D
    {
        public void Write(StringBuilder sb, string s)
        {
            sb.AppendLine(""Delta: "" + s);
        }
    }
}
");
            var delta1Reference = MetadataReference.CreateFromFile(Delta1.Path);
            Gamma = GenerateDll("Gamma", _directory, @"
using System.Text;
using Delta;

namespace Gamma
{
    public class G
    {
        public void Write(StringBuilder sb, string s)
        {
            D d = new D();

            d.Write(sb, ""Gamma: "" + s);
        }
    }
}
", delta1Reference);

            var gammaReference = MetadataReference.CreateFromFile(Gamma.Path);
            Beta = GenerateDll("Beta", _directory, @"
using System.Text;
using Gamma;

namespace Beta
{
    public class B
    {
        public void Write(StringBuilder sb, string s)
        {
            G g = new G();

            g.Write(sb, ""Beta: "" + s);
        }
    }
}
", gammaReference);

            Alpha = GenerateDll("Alpha", _directory, @"
using System.Text;
using Gamma;

namespace Alpha
{
    public class A
    {
        public void Write(StringBuilder sb, string s)
        {
            G g = new G();
            g.Write(sb, ""Alpha: "" + s);
        }
    }
}
", gammaReference);

            var v2Directory = _directory.CreateDirectory("Version2");
            Delta2 = GenerateDll("Delta", v2Directory, @"
using System.Text;

[assembly: System.Reflection.AssemblyTitle(""Delta"")]
[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")]

namespace Delta
{
    public class D
    {
        public void Write(StringBuilder sb, string s)
        {
            sb.AppendLine(""Delta.2: "" + s);
        }
    }
}
");
            var delta2Reference = MetadataReference.CreateFromFile(Delta2.Path);
            Epsilon = GenerateDll("Epsilon", v2Directory, @"
using System.Text;
using Delta;

namespace Epsilon
{
    public class E
    {
        public void Write(StringBuilder sb, string s)
        {
            D d = new D();

            d.Write(sb, ""Epsilon: "" + s);
        }
    }
}
", delta2Reference);
        }

        private static TempFile GenerateDll(string assemblyName, TempDirectory directory, string csSource, params MetadataReference[] additionalReferences)
        {
            var analyzerDependencyCompilation = CSharpCompilation.Create(
                assemblyName: assemblyName,
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(csSource) },
                references: new MetadataReference[]
                {
                    TestMetadata.NetStandard20.mscorlib,
                    TestMetadata.NetStandard20.netstandard,
                    TestMetadata.NetStandard20.SystemRuntime
                }.Concat(additionalReferences),
                options: s_dllWithMaxWarningLevel);

            var tempFile = directory.CreateFile($"{assemblyName}.dll");
            tempFile.WriteAllBytes(analyzerDependencyCompilation.EmitToArray());
            return tempFile;
        }

        public void Dispose()
        {
            _temp.Dispose();
        }
    }
}
