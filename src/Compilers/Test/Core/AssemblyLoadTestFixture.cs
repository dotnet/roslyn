// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class AssemblyLoadTestFixture : IDisposable
    {
        private readonly TempRoot _temp;
        private readonly TempDirectory _directory;

        /// <summary>
        /// An assembly with no references, assembly version 1.
        /// </summary>
        public TempFile Delta1 { get; }

        /// <summary>
        /// An assembly with no references, assembly version 1, and public signed.
        /// </summary>
        public TempFile DeltaPublicSigned1 { get; }

        /// <summary>
        /// An assembly with a reference to <see cref="Delta1"/>.
        /// </summary>
        public TempFile Gamma { get; }

        /// <summary>
        /// An assembly with a reference to <see cref="DeltaPublicSigned1"/>.
        /// </summary>
        public TempFile GammaReferencingPublicSigned { get; }

        /// <summary>
        /// An assembly with a reference to <see cref="Gamma"/>.
        /// </summary>
        public TempFile Beta { get; }

        /// <summary>
        /// An assembly with a reference to <see cref="Gamma"/>.
        /// </summary>
        public TempFile Alpha { get; }

        /// <summary>
        /// An assembly with no references, assembly version 2.
        /// </summary>
        public TempFile Delta2 { get; }

        /// <summary>
        /// An assembly with no references, assembly version 2, and public signed.
        /// </summary>
        public TempFile DeltaPublicSigned2 { get; }

        /// <summary>
        /// An assembly with a reference to <see cref="Delta2"/>.
        /// </summary>
        public TempFile Epsilon { get; }

        /// <summary>
        /// An assembly with a reference to <see cref="DeltaPublicSigned2"/>.
        /// </summary>
        public TempFile EpsilonReferencingPublicSigned { get; }

        /// <summary>
        /// An assembly with no references, assembly version 2. The implementation however is different than
        /// <see cref="Delta2"/> so we can test having two assemblies that look the same but aren't.
        /// </summary>
        public TempFile Delta2B { get; }

        /// <summary>
        /// An assembly with no references, assembly version 3.
        /// </summary>
        public TempFile Delta3 { get; }

        public TempFile UserSystemCollectionsImmutable { get; }

        /// <summary>
        /// An analyzer which uses members in its referenced version of System.Collections.Immutable
        /// that are not present in the compiler's version of System.Collections.Immutable.
        /// </summary>
        public TempFile AnalyzerReferencesSystemCollectionsImmutable1 { get; }

        /// <summary>
        /// An analyzer which uses members in its referenced version of System.Collections.Immutable
        /// which have different behavior than the same members in compiler's version of System.Collections.Immutable.
        /// </summary>
        public TempFile AnalyzerReferencesSystemCollectionsImmutable2 { get; }

        public TempFile AnalyzerReferencesDelta1 { get; }

        public TempFile FaultyAnalyzer { get; }

        public TempFile AnalyzerWithDependency { get; }
        public TempFile AnalyzerDependency { get; }

        public TempFile AnalyzerWithNativeDependency { get; }

        public TempFile AnalyzerWithFakeCompilerDependency { get; }

        public TempFile AnalyzerWithLaterFakeCompilerDependency { get; }

        public AssemblyLoadTestFixture()
        {
            _temp = new TempRoot();
            _directory = _temp.CreateDirectory();

            const string Delta1Source = @"
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
";

            Delta1 = GenerateDll("Delta", _directory, Delta1Source);
            var delta1Reference = MetadataReference.CreateFromFile(Delta1.Path);
            DeltaPublicSigned1 = GenerateDll("DeltaPublicSigned", _directory.CreateDirectory("Delta1PublicSigned"), Delta1Source, publicKeyOpt: SigningTestHelpers.PublicKey);

            const string GammaSource = @"
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
";
            Gamma = GenerateDll("Gamma", _directory, GammaSource, delta1Reference);
            GammaReferencingPublicSigned = GenerateDll("GammaReferencingPublicSigned", _directory.CreateDirectory("GammaReferencingPublicSigned"), GammaSource, MetadataReference.CreateFromFile(DeltaPublicSigned1.Path));

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

            const string Delta2Source = @"
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
";

            var v2Directory = _directory.CreateDirectory("Version2");
            Delta2 = GenerateDll("Delta", v2Directory, Delta2Source);
            var v2PublicSignedDirectory = _directory.CreateDirectory("Version2PublicSigned");
            DeltaPublicSigned2 = GenerateDll("DeltaPublicSigned", v2PublicSignedDirectory, Delta2Source, publicKeyOpt: SigningTestHelpers.PublicKey);

            var delta2Reference = MetadataReference.CreateFromFile(Delta2.Path);

            const string EpsilonSource = @"
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
";
            Epsilon = GenerateDll("Epsilon", v2Directory, EpsilonSource, delta2Reference);
            EpsilonReferencingPublicSigned = GenerateDll("EpsilonReferencingPublicSigned", v2PublicSignedDirectory, EpsilonSource, MetadataReference.CreateFromFile(DeltaPublicSigned2.Path));

            var v2BDirectory = _directory.CreateDirectory("Version2B");
            Delta2B = GenerateDll("Delta", v2BDirectory, @"
using System.Text;

[assembly: System.Reflection.AssemblyTitle(""Delta"")]
[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")]

namespace Delta
{
    public class D
    {
        public void Write(StringBuilder sb, string s)
        {
            sb.AppendLine(""Delta.2B: "" + s);
        }
    }
}
");

            var v3Directory = _directory.CreateDirectory("Version3");
            Delta3 = GenerateDll("Delta", v3Directory, @"
using System.Text;

[assembly: System.Reflection.AssemblyTitle(""Delta"")]
[assembly: System.Reflection.AssemblyVersion(""3.0.0.0"")]

namespace Delta
{
    public class D
    {
        public void Write(StringBuilder sb, string s)
        {
            sb.AppendLine(""Delta.3: "" + s);
        }
    }
}
");

            var sciUserDirectory = _directory.CreateDirectory("SCIUser");
            var compilerReference = MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.SyntaxNode).Assembly.Location);

            UserSystemCollectionsImmutable = GenerateDll("System.Collections.Immutable", sciUserDirectory, @"
namespace System.Collections.Immutable
{
    public static class ImmutableArray
    {
        public static ImmutableArray<T> Create<T>(T t) => new();
    }

    public struct ImmutableArray<T>
    {
        public int Length => 42;

        public static int MyMethod() => 42;
    }
}
", compilerReference);

            var userSystemCollectionsImmutableReference = MetadataReference.CreateFromFile(UserSystemCollectionsImmutable.Path);
            AnalyzerReferencesSystemCollectionsImmutable1 = GenerateDll("AnalyzerUsesSystemCollectionsImmutable1", sciUserDirectory, @"
using System.Text;
using System.Collections.Immutable;

public class Analyzer
{
    public void Method(StringBuilder sb)
    {
        sb.Append(ImmutableArray<object>.MyMethod());
    }
}
", userSystemCollectionsImmutableReference, compilerReference);

            AnalyzerReferencesSystemCollectionsImmutable2 = GenerateDll("AnalyzerUsesSystemCollectionsImmutable2", sciUserDirectory, @"
using System.Text;
using System.Collections.Immutable;

public class Analyzer
{
    public void Method(StringBuilder sb)
    {
        sb.Append(ImmutableArray.Create(""a"").Length);
    }
}
", userSystemCollectionsImmutableReference, compilerReference);

            var analyzerReferencesDelta1Directory = _directory.CreateDirectory("AnalyzerReferencesDelta1");
            var delta1InAnalyzerReferencesDelta1 = analyzerReferencesDelta1Directory.CopyFile(Delta1.Path);

            AnalyzerReferencesDelta1 = GenerateDll("AnalyzerReferencesDelta1", _directory, @"
using System.Text;
using Delta;

public class Analyzer
{
    public void Method(StringBuilder sb)
    {
        var d = new D();
        d.Write(sb, ""Hello"");
    }
}
", MetadataReference.CreateFromFile(delta1InAnalyzerReferencesDelta1.Path), compilerReference);

            var faultyAnalyzerDirectory = _directory.CreateDirectory("FaultyAnalyzer");
            FaultyAnalyzer = GenerateDll("FaultyAnalyzer", faultyAnalyzerDirectory, @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public abstract class TestAnalyzer : DiagnosticAnalyzer
{
}
", compilerReference);

            var realSciReference = MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location);
            var analyzerWithDependencyDirectory = _directory.CreateDirectory("AnalyzerWithDependency");
            AnalyzerDependency = GenerateDll("AnalyzerDependency", analyzerWithDependencyDirectory, @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class AbstractTestAnalyzer : DiagnosticAnalyzer
{
    protected static string SomeString = nameof(SomeString);
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
    public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
}
", realSciReference, compilerReference);

            AnalyzerWithDependency = GenerateDll("Analyzer", analyzerWithDependencyDirectory, @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestAnalyzer : AbstractTestAnalyzer
{
    private static string SomeString2 = AbstractTestAnalyzer.SomeString;
}", realSciReference, compilerReference, MetadataReference.CreateFromFile(AnalyzerDependency.Path));

            AnalyzerWithNativeDependency = GenerateDll("AnalyzerWithNativeDependency", _directory, @"
using System;
using System.Runtime.InteropServices;

public class Class1
{
    [DllImport(""kernel32.dll"", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetFileAttributesW(string lpFileName);

    public int GetFileAttributes(string path)
    {
        return GetFileAttributesW(path);
    }
}

");


            var analyzerWithFakeCompilerDependencyDirectory = _directory.CreateDirectory("AnalyzerWithFakeCompilerDependency");
            var fakeCompilerAssembly = GenerateDll("Microsoft.CodeAnalysis", analyzerWithFakeCompilerDependencyDirectory, publicKeyOpt: typeof(SyntaxNode).Assembly.GetName().GetPublicKey()?.ToImmutableArray() ?? default, csSource: @"
using System;
using System.Reflection;

[assembly: AssemblyVersionAttribute(""2.0.0.0"")]

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class DiagnosticAnalyzerAttribute : Attribute
    {
        public DiagnosticAnalyzerAttribute(string firstLanguage, params string[] additionalLanguages) { }
    }

    public class DiagnosticAnalyzer
    {
    }
}
");
            var fakeCompilerReference = MetadataReference.CreateFromFile(fakeCompilerAssembly.Path);
            AnalyzerWithFakeCompilerDependency = GenerateDll("AnalyzerWithFakeCompilerDependency", analyzerWithFakeCompilerDependencyDirectory, @"
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(""C#"")]
public class Analyzer : DiagnosticAnalyzer
{
}", fakeCompilerReference);


            var analyzerWithLaterFakeCompileDirectory = _directory.CreateDirectory("AnalyzerWithLaterFakeCompilerDependency");
            var laterFakeCompilerAssembly = GenerateDll("Microsoft.CodeAnalysis", analyzerWithLaterFakeCompileDirectory, publicKeyOpt: typeof(SyntaxNode).Assembly.GetName().GetPublicKey()?.ToImmutableArray() ?? default, csSource: @"
using System;
using System.Reflection;

[assembly: AssemblyVersionAttribute(""100.0.0.0"")]

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class DiagnosticAnalyzerAttribute : Attribute
    {
        public DiagnosticAnalyzerAttribute(string firstLanguage, params string[] additionalLanguages) { }
    }

    public class DiagnosticAnalyzer
    {
    }
}
");
            var laterCompilerReference = MetadataReference.CreateFromFile(laterFakeCompilerAssembly.Path);
            AnalyzerWithLaterFakeCompilerDependency = GenerateDll("AnalyzerWithLaterFakeCompilerDependency", analyzerWithLaterFakeCompileDirectory, @"
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(""C#"")]
public class Analyzer : DiagnosticAnalyzer
{
}", laterCompilerReference);
        }

        private static TempFile GenerateDll(string assemblyName, TempDirectory directory, string csSource, params MetadataReference[] additionalReferences)
        {
            return GenerateDll(assemblyName, directory, csSource, publicKeyOpt: default, additionalReferences);
        }

        private static TempFile GenerateDll(string assemblyName, TempDirectory directory, string csSource, ImmutableArray<byte> publicKeyOpt, params MetadataReference[] additionalReferences)
        {
            CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary, warningLevel: Diagnostic.MaxWarningLevel);

            if (!publicKeyOpt.IsDefault)
            {
                options = options.WithPublicSign(true).WithCryptoPublicKey(publicKeyOpt);
            }

            var analyzerDependencyCompilation = CSharpCompilation.Create(
                assemblyName: assemblyName,
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(csSource) },
                references: (new MetadataReference[]
                {
                    NetStandard20.mscorlib,
                    NetStandard20.netstandard,
                    NetStandard20.SystemRuntime
                }).Concat(additionalReferences),
                options: options);

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
