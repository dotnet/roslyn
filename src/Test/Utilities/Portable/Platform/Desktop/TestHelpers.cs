#if NET472
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32;

namespace Roslyn.Test.Utilities
{
    public static class DesktopTestHelpers
    {
        public static IEnumerable<Type> GetAllTypesImplementingGivenInterface(Assembly assembly, Type interfaceType)
        {
            if (assembly == null || interfaceType == null || !interfaceType.IsInterface)
            {
                throw new ArgumentException("interfaceType is not an interface.", nameof(interfaceType));
            }

            return assembly.GetTypes().Where((t) =>
            {
                // simplest way to get types that implement mef type
                // we might need to actually check whether type export the interface type later
                if (t.IsAbstract)
                {
                    return false;
                }

                var candidate = t.GetInterface(interfaceType.ToString());
                return candidate != null && candidate.Equals(interfaceType);
            }).ToList();
        }

        public static IEnumerable<Type> GetAllTypesSubclassingType(Assembly assembly, Type type)
        {
            if (assembly == null || type == null)
            {
                throw new ArgumentException("Invalid arguments");
            }

            return (from t in assembly.GetTypes()
                    where !t.IsAbstract
                    where type.IsAssignableFrom(t)
                    select t).ToList();
        }

        public static TempFile CreateCSharpAnalyzerAssemblyWithTestAnalyzer(TempDirectory dir, string assemblyName)
        {
            var analyzerSource = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
    public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
}";

            dir.CopyFile(typeof(System.Reflection.Metadata.MetadataReader).Assembly.Location);
            var immutable = dir.CopyFile(typeof(ImmutableArray).Assembly.Location);
            var analyzer = dir.CopyFile(typeof(DiagnosticAnalyzer).Assembly.Location);
            dir.CopyFile(typeof(Memory<>).Assembly.Location);
            dir.CopyFile(typeof(System.Runtime.CompilerServices.Unsafe).Assembly.Location);

            var analyzerCompilation = CSharpCompilation.Create(
                assemblyName,
                new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(analyzerSource) },
                new MetadataReference[]
                {
                    TestReferences.NetStandard20.NetStandard,
                    TestReferences.NetStandard20.SystemRuntimeRef,
                    MetadataReference.CreateFromFile(immutable.Path),
                    MetadataReference.CreateFromFile(analyzer.Path)
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return dir.CreateFile(assemblyName + ".dll").WriteAllBytes(analyzerCompilation.EmitToArray());
        }

        public static ImmutableArray<byte> CreateCSharpAnalyzerNetStandard13(string analyzerAssemblyName)
        {
            var minSystemCollectionsImmutableSource = @"
[assembly: System.Reflection.AssemblyVersion(""1.2.3.0"")]

namespace System.Collections.Immutable
{
    public struct ImmutableArray<T>
    {
    }
}
";

            var minCodeAnalysisSource = @"
using System;

[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")]

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DiagnosticAnalyzerAttribute : Attribute
    {
        public DiagnosticAnalyzerAttribute(string firstLanguage, params string[] additionalLanguages) {}
    }

    public abstract class DiagnosticAnalyzer
    {
        public abstract System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        public abstract void Initialize(AnalysisContext context);
    }

    public abstract class AnalysisContext
    {
    }
}

namespace Microsoft.CodeAnalysis
{
    public sealed class DiagnosticDescriptor
    {
    }
}
";
            var minSystemCollectionsImmutableImage = CSharpCompilation.Create(
                "System.Collections.Immutable",
                new[] { SyntaxFactory.ParseSyntaxTree(minSystemCollectionsImmutableSource) },
                new[] { MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Runtime) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoPublicKey: TestResources.TestKeys.PublicKey_b03f5f7f11d50a3a)).EmitToArray();

            var minSystemCollectionsImmutableRef = MetadataReference.CreateFromImage(minSystemCollectionsImmutableImage);

            var minCodeAnalysisImage = CSharpCompilation.Create(
                "Microsoft.CodeAnalysis",
                new[] { SyntaxFactory.ParseSyntaxTree(minCodeAnalysisSource) },
                new[]
                {
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Runtime),
                    minSystemCollectionsImmutableRef
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoPublicKey: TestResources.TestKeys.PublicKey_31bf3856ad364e35)).EmitToArray();

            var minCodeAnalysisRef = MetadataReference.CreateFromImage(minCodeAnalysisImage);

            var analyzerSource = @"
using System;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Win32.SafeHandles;

[DiagnosticAnalyzer(""C#"")]
public class TestAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException(new[]                                 
    {                                                                                                                                   
        typeof(Win32Exception),           // Microsoft.Win32.Primitives
        typeof(AppContext),               // System.AppContext
        typeof(Console),                  // System.Console
        typeof(ValueTuple),               // System.ValueTuple
        typeof(FileVersionInfo),          // System.Diagnostics.FileVersionInfo
        typeof(Process),                  // System.Diagnostics.Process
        typeof(ChineseLunisolarCalendar), // System.Globalization.Calendars
        typeof(ZipArchive),               // System.IO.Compression
        typeof(ZipFile),                  // System.IO.Compression.ZipFile
        typeof(FileOptions),              // System.IO.FileSystem
        typeof(FileAttributes),           // System.IO.FileSystem.Primitives
        typeof(HttpClient),               // System.Net.Http
        typeof(AuthenticatedStream),      // System.Net.Security
        typeof(IOControlCode),            // System.Net.Sockets
        typeof(RuntimeInformation),       // System.Runtime.InteropServices.RuntimeInformation
        typeof(SerializationException),   // System.Runtime.Serialization.Primitives
        typeof(GenericIdentity),          // System.Security.Claims
        typeof(Aes),                      // System.Security.Cryptography.Algorithms
        typeof(CspParameters),            // System.Security.Cryptography.Csp
        typeof(AsnEncodedData),           // System.Security.Cryptography.Encoding
        typeof(AsymmetricAlgorithm),      // System.Security.Cryptography.Primitives
        typeof(SafeX509ChainHandle),      // System.Security.Cryptography.X509Certificates
        typeof(IXmlLineInfo),             // System.Xml.ReaderWriter
        typeof(XmlNode),                  // System.Xml.XmlDocument
        typeof(XPathDocument),            // System.Xml.XPath
        typeof(XDocumentExtensions),      // System.Xml.XPath.XDocument
        typeof(CodePagesEncodingProvider),// System.Text.Encoding.CodePages
        typeof(ValueTask<>),              // System.Threading.Tasks.Extensions

        // csc doesn't ship with facades for the following assemblies. 
        // Analyzers can't use them unless they carry the facade with them.

        // typeof(SafePipeHandle),           // System.IO.Pipes
        // typeof(StackFrame),               // System.Diagnostics.StackTrace
        // typeof(BindingFlags),             // System.Reflection.TypeExtensions
        // typeof(AccessControlActions),     // System.Security.AccessControl
        // typeof(SafeAccessTokenHandle),    // System.Security.Principal.Windows
        // typeof(Thread),                   // System.Threading.Thread
    }.Length.ToString());

    public override void Initialize(AnalysisContext context)
    {
    }
}";

            var analyzerImage = CSharpCompilation.Create(
                analyzerAssemblyName,
                new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(analyzerSource) },
                new MetadataReference[]
                {
                    minCodeAnalysisRef,
                    minSystemCollectionsImmutableRef,
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.Microsoft_Win32_Primitives),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_AppContext),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Console),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard10.System_ValueTuple),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Diagnostics_FileVersionInfo),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Diagnostics_Process),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Diagnostics_StackTrace),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Globalization_Calendars),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_IO_Compression),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_IO_Compression_ZipFile),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_IO_FileSystem),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_IO_FileSystem_Primitives),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_IO_Pipes),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Net_Http),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Net_Security),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Net_Sockets),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Reflection_TypeExtensions),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Runtime),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard11.System_Runtime_InteropServices_RuntimeInformation),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Runtime_Serialization_Primitives),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Security_AccessControl),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Security_Claims),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Security_Cryptography_Algorithms),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Security_Cryptography_Csp),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Security_Cryptography_Encoding),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Security_Cryptography_Primitives),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Security_Cryptography_X509Certificates),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Security_Principal_Windows),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Threading_Thread),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard10.System_Threading_Tasks_Extensions),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Xml_ReaderWriter),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Xml_XmlDocument),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Xml_XPath),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Xml_XPath_XDocument),
                    MetadataReference.CreateFromImage(TestResources.NetFX.netstandard13.System_Text_Encoding_CodePages)
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).EmitToArray();

            return analyzerImage;
        }

        public static string GetMSBuildDirectory()
        {
            var vsVersion = Environment.GetEnvironmentVariable("VisualStudioVersion") ?? "14.0";
            using (var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\{vsVersion}", false))
            {
                if (key != null)
                {
                    var toolsPath = key.GetValue("MSBuildToolsPath");
                    if (toolsPath != null)
                    {
                        return toolsPath.ToString();
                    }
                }
            }

            return null;
        }
    }
}

#endif
