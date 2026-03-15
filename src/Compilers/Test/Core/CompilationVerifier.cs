// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader.Tools;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed partial class CompilationVerifier
    {
        /// <summary>
        /// When non-null this will dump assemblies to disk in the given path
        /// </summary>
        internal static string? DumpAssemblyLocation { get; set; } = Environment.GetEnvironmentVariable("ROSLYN_TEST_DUMP_PATH");

        private static int s_dumpCount;

        private readonly Compilation _compilation;
        private readonly IEnumerable<ModuleData>? _dependencies;
        private IModuleSymbol? _lazyModuleSymbol;
        private EmitData? _emitData;
        private readonly Func<IModuleSymbol, CompilationTestData.MethodData, IReadOnlyDictionary<int, string>?, bool, string>? _visualizeRealIL;

        public Compilation Compilation => _compilation;
        public ImmutableArray<byte> EmittedAssemblyData => GetEmitData().EmittedAssemblyData;
        public ImmutableArray<byte> EmittedAssemblyPdb => GetEmitData().EmittedAssemblyPdb;
        public ImmutableArray<Diagnostic> Diagnostics => GetEmitData().Diagnostics;
        internal CompilationTestData TestData => GetEmitData().TestData;

        internal CompilationVerifier(
            Compilation compilation,
            Func<IModuleSymbol, CompilationTestData.MethodData, IReadOnlyDictionary<int, string>?, bool, string>? visualizeRealIL = null,
            IEnumerable<ModuleData>? dependencies = null)
        {
            _compilation = compilation;
            _dependencies = dependencies;
            _visualizeRealIL = visualizeRealIL;
        }

        private EmitData GetEmitData() => _emitData ?? throw new InvalidOperationException("Must call Emit first");

        internal PortableExecutableReference GetImageReference(
            bool embedInteropTypes = false,
            ImmutableArray<string> aliases = default,
            DocumentationProvider? documentation = null)
        {
            if (Compilation.Options.OutputKind == OutputKind.NetModule)
            {
                return ModuleMetadata.CreateFromImage(EmittedAssemblyData).GetReference(documentation, display: Compilation.MakeSourceModuleName());
            }
            else
            {
                return AssemblyMetadata.CreateFromImage(EmittedAssemblyData).GetReference(documentation, aliases: aliases, embedInteropTypes: embedInteropTypes, display: Compilation.MakeSourceAssemblySimpleName());
            }
        }

        internal Metadata GetMetadata()
        {
            var emitData = GetEmitData();
            if (_compilation.Options.OutputKind.IsNetModule())
            {
                var metadata = ModuleMetadata.CreateFromImage(emitData.EmittedAssemblyData);
                metadata.Module.PretendThereArentNoPiaLocalTypes();
                return metadata;
            }
            else
            {
                List<ImmutableArray<byte>> images =
                [
                    emitData.EmittedAssemblyData,
                    .. emitData.Modules.Where(m => m.Kind == OutputKind.NetModule).Select(m => m.Image)
                ];

                return AssemblyMetadata.Create(images.Select(image =>
                {
                    var metadata = ModuleMetadata.CreateFromImage(image);
                    metadata.Module.PretendThereArentNoPiaLocalTypes();
                    return metadata;
                }));
            }
        }

        public string Dump(string? methodName = null)
        {
            var emitData = Emit(manifestResources: null, EmitOptions.Default);
            var dumpDir = DumpAssemblyData(emitData.Modules, DumpAssemblyLocation ?? "");
            string extension = emitData.EmittedModule.Kind == OutputKind.ConsoleApplication ? ".exe" : ".dll";
            string modulePath = Path.Combine(dumpDir, emitData.EmittedModule.SimpleName + extension);

            var decompiler = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler(modulePath,
                new ICSharpCode.Decompiler.DecompilerSettings() { AsyncAwait = false });

            if (methodName != null)
            {
                var map = new Dictionary<string, ICSharpCode.Decompiler.TypeSystem.IMethod>();
                listMethods(decompiler.TypeSystem.MainModule.RootNamespace, map);

                if (map.TryGetValue(methodName, out var method))
                {
                    return decompiler.DecompileAsString(method.MetadataToken);
                }
                else
                {
                    throw new Exception($"Didn't find method '{methodName}'. Available/distinguishable methods are: {Environment.NewLine}{string.Join(Environment.NewLine, map.Keys)}");
                }
            }

            return decompiler.DecompileWholeModuleAsString();

            void listMethods(ICSharpCode.Decompiler.TypeSystem.INamespace @namespace, Dictionary<string, ICSharpCode.Decompiler.TypeSystem.IMethod> result)
            {
                foreach (var nestedNS in @namespace.ChildNamespaces)
                {
                    if (nestedNS.FullName != "System" &&
                        nestedNS.FullName != "Microsoft")
                    {
                        listMethods(nestedNS, result);
                    }
                }

                foreach (var type in @namespace.Types)
                {
                    listMethodsInType(type, result);
                }
            }

            void listMethodsInType(ICSharpCode.Decompiler.TypeSystem.ITypeDefinition type, Dictionary<string, ICSharpCode.Decompiler.TypeSystem.IMethod> result)
            {
                foreach (var nestedType in type.NestedTypes)
                {
                    listMethodsInType(nestedType, result);
                }

                foreach (var method in type.Methods)
                {
                    if (result.ContainsKey(method.FullName))
                    {
                        // There is a bug with FullName on methods in generic types
                        result.Remove(method.FullName);
                    }
                    else
                    {
                        result.Add(method.FullName, method);
                    }
                }
            }
        }

        public string DumpIL()
        {
            var output = new ICSharpCode.Decompiler.PlainTextOutput();
            var emitData = Emit(manifestResources: null, EmitOptions.Default);
            using var moduleMetadata = ModuleMetadata.CreateFromImage(emitData.EmittedAssemblyData);
            var peFile = new PEFile(emitData.EmittedModule.Id.FullName, moduleMetadata.Module.PEReaderOpt);
            var disassembler = new ICSharpCode.Decompiler.Disassembler.ReflectionDisassembler(output, default);
            disassembler.WriteModuleContents(peFile);
            return output.ToString();
        }

        public static string DumpAssemblyData(IEnumerable<ModuleData> modules, string dumpBasePath)
        {
            var dumpCount = Interlocked.Increment(ref s_dumpCount);
            var dumpDirectory = Path.Combine(dumpBasePath is "" ? TempRoot.Root : dumpBasePath, "dumps", dumpCount.ToString());
            _ = Directory.CreateDirectory(dumpDirectory);

            // Limit the number of dumps to 10. After 10 we're likely in a bad state and are 
            // dumping lots of unnecessary data to disk.
            if (dumpCount > 10)
            {
                return dumpDirectory;
            }

            var sb = new StringBuilder();
            foreach (var module in modules)
            {
                if (module.InMemoryModule)
                {
                    string fileName;
                    if (module.Kind == OutputKind.NetModule)
                    {
                        fileName = module.FullName;
                    }
                    else
                    {
                        fileName = AssemblyIdentity.TryParseDisplayName(module.FullName, out var identity)
                            ? identity.Name
                            : "";
                    }

                    string pePath = Path.Combine(dumpDirectory, fileName + module.Kind.GetDefaultExtension());
                    try
                    {
                        module.Image.WriteToFile(pePath);
                    }
                    catch (ArgumentException e)
                    {
                        pePath = $"<unable to write file: '{pePath}' -- {e.Message}>";
                    }
                    catch (IOException e)
                    {
                        pePath = $"<unable to write file: '{pePath}' -- {e.Message}>";
                    }

                    string? pdbPath;
                    if (!module.Pdb.IsDefaultOrEmpty)
                    {
                        pdbPath = Path.Combine(dumpDirectory, fileName + ".pdb");

                        try
                        {
                            module.Pdb.WriteToFile(pdbPath);
                        }
                        catch (ArgumentException e)
                        {
                            pdbPath = $"<unable to write file: '{pdbPath}' -- {e.Message}>";
                        }
                        catch (IOException e)
                        {
                            pdbPath = $"<unable to write file: '{pdbPath}' -- {e.Message}>";
                        }
                    }
                    else
                    {
                        pdbPath = null;
                    }

                    sb.Append("PE(" + module.Kind + "): ");
                    sb.AppendLine(pePath);
                    if (pdbPath != null)
                    {
                        sb.Append("PDB: ");
                        sb.AppendLine(pdbPath);
                    }
                }
            }

            if (sb.Length > 0)
            {
                File.WriteAllText(Path.Combine(dumpDirectory, "log.txt"), sb.ToString());
            }

            return dumpDirectory;
        }

        /// <summary>
        /// Asserts that the emitted IL for a type is the same as the expected IL.
        /// Many core library types are in different assemblies on .Net Framework, and .Net Core.
        /// Therefore this test is likely to fail unless you  only run it only only on one of these frameworks,
        /// or you run it on both, but provide a different expected output string for each.
        /// See <see cref="ExecutionConditionUtil"/>.
        /// </summary>
        /// <param name="typeName">The non-fully-qualified name of the type</param>
        /// <param name="expected">The expected IL</param>
        public void VerifyTypeIL(string typeName, string expected)
        {
            VerifyTypeIL(typeName, output =>
            {
                // All our tests predate ilspy adding `// Header size: ...` to the contents.  So trim that out since we
                // really don't need to validate superfluous IL comments
                expected = RemoveHeaderComments(expected);
                output = RemoveHeaderComments(output);

                output = FixupCodeSizeComments(output);

                AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, output, escapeQuotes: false);
            });
        }

        private static readonly Regex s_headerCommentsRegex = new("""^\s*// Header size: [0-9]+\s*$""", RegexOptions.Multiline);
        private static readonly Regex s_codeSizeCommentsRegex = new("""^\s*// Code size(:) [0-9]+\s*""", RegexOptions.Multiline);

        private static string RemoveHeaderComments(string value)
        {
            return s_headerCommentsRegex.Replace(value, "");
        }

        private static string FixupCodeSizeComments(string output)
        {
            // We use the form `// Code size 7 (0x7)` while ilspy moved to the form `// Code size: 7 (0x7)` (with an
            // extra colon).  Strip the colon to make these match.
            return s_codeSizeCommentsRegex.Replace(output, match => match.Groups[0].Value.Replace(match.Groups[1].Value, ""));
        }

        /// <summary>
        /// Invokes <paramref name="validateExpected"/> with the emitted IL for a type to validate it's expected.
        /// Many core library types are in different assemblies on .Net Framework, and .Net Core.
        /// Therefore this test is likely to fail unless you  only run it only only on one of these frameworks,
        /// or you run it on both, but provide a different expected output string for each.
        /// See <see cref="ExecutionConditionUtil"/>.
        /// </summary>
        /// <param name="typeName">The non-fully-qualified name of the type</param>
        /// <param name="validateExpected">An action to invoke with the emitted IL.</param>
        public void VerifyTypeIL(string typeName, Action<string> validateExpected)
        {
            var output = new ICSharpCode.Decompiler.PlainTextOutput() { IndentationString = "    " };
            var emitData = Emit(manifestResources: null, EmitOptions.Default);
            using (var moduleMetadata = ModuleMetadata.CreateFromImage(emitData.EmittedAssemblyData))
            {
                var peFile = new PEFile(emitData.EmittedModule.Id.FullName, moduleMetadata.Module.PEReaderOpt);
                var metadataReader = moduleMetadata.GetMetadataReader();

                bool found = false;
                foreach (var typeDefHandle in metadataReader.TypeDefinitions)
                {
                    var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
                    if (metadataReader.GetString(typeDef.Name) == typeName)
                    {
                        var disassembler = new ICSharpCode.Decompiler.Disassembler.ReflectionDisassembler(output, default);
                        disassembler.DisassembleType(peFile, typeDefHandle);
                        found = true;
                        break;
                    }
                }
                Assert.True(found, "Could not find type named " + typeName);
            }

            validateExpected(output.ToString());
        }

        public void EmitAndVerify(
            string? expectedOutput,
            bool trimOutput,
            int? expectedReturnCode,
            string[]? args,
            IEnumerable<ResourceDescription>? manifestResources,
            EmitOptions? emitOptions,
            Verification peVerify,
            SignatureDescription[]? expectedSignatures)
        {

            var emitData = Emit(manifestResources, emitOptions);
            using var testEnvironment = CreateRuntimeEnvironment(emitData.EmittedModule, emitData.Modules);

            try
            {
                testEnvironment.Verify(peVerify);
            }
            catch (Exception)
            {
                if (DumpAssemblyLocation is string dumpPath)
                {
                    DumpAssemblyData(emitData.Modules, dumpPath);
                }

                if (peVerify.Status.HasFlag(VerificationStatus.PassesOrFailFast))
                {
                    var il = DumpIL();
                    Console.WriteLine(il);

                    Environment.FailFast("Investigating flaky IL verification issue. Tracked by https://github.com/dotnet/roslyn/issues/63782");
                }

                throw;
            }

            if (expectedSignatures != null)
            {
                MetadataSignatureUnitTestHelper.VerifyMemberSignatures(testEnvironment, expectedSignatures);
            }

            if (expectedOutput != null || expectedReturnCode != null)
            {
                var (exitCode, output, errorOutput) = testEnvironment.Execute(args ?? []);
                if (expectedReturnCode.HasValue)
                {
                    Assert.Equal(expectedReturnCode.Value, exitCode);
                }

                if (expectedOutput != null)
                {
                    if (trimOutput)
                    {
                        expectedOutput = expectedOutput.Trim();
                        output = output.Trim();
                    }

                    AssertEx.Equal(expectedOutput, output);
                    Assert.Empty(errorOutput);
                }
            }
        }

        private sealed class Resolver : ILVerify.IResolver
        {
            private readonly Dictionary<string, PEReader> _readersByName;

            internal Resolver(Dictionary<string, PEReader> readersByName)
            {
                _readersByName = readersByName;
            }

            public PEReader ResolveAssembly(AssemblyNameInfo assemblyName)
            {
                Debug.Assert(assemblyName.Name is not null);
                return Resolve(assemblyName.Name);
            }

            public PEReader ResolveModule(AssemblyNameInfo referencingAssembly, string fileName)
            {
                throw new NotImplementedException();
            }

            public PEReader Resolve(string simpleName)
            {
                if (_readersByName.TryGetValue(simpleName, out var reader))
                {
                    return reader;
                }

                throw new Exception($"ILVerify was not able to resolve a module named '{simpleName}'");
            }
        }

        internal static void ILVerify(Verification verification, ModuleData mainModule, ImmutableArray<ModuleData> modules)
        {
            if (verification.Status.HasFlag(VerificationStatus.Skipped))
            {
                return;
            }

            var readersByName = new Dictionary<string, PEReader>(StringComparer.OrdinalIgnoreCase);
            foreach (var module in modules)
            {
                string name = module.SimpleName;
                if (readersByName.ContainsKey(name))
                {
                    if (verification.Status.HasFlag(VerificationStatus.FailsILVerify) && verification.ILVerifyMessage is null)
                    {
                        return;
                    }

                    throw new Exception($"Multiple modules named '{name}' were found");
                }
                readersByName.Add(name, new PEReader(module.Image));
            }

            var resolver = new Resolver(readersByName);
            var verifier = new ILVerify.Verifier(resolver);
            var mscorlibModule = modules.SingleOrDefault(m => m.IsCorLib);
            if (mscorlibModule is null)
            {
                if (verification.Status.HasFlag(VerificationStatus.FailsILVerify) && verification.ILVerifyMessage is null)
                {
                    return;
                }

                throw new Exception("No corlib found");
            }

            // Main module is the first one
            var mainModuleReader = resolver.Resolve(mainModule.SimpleName);

            var (actualSuccess, actualMessage) = verify(verifier, mscorlibModule.FullName, mainModuleReader);
            var expectedSuccess = !verification.Status.HasFlag(VerificationStatus.FailsILVerify);

            if (actualSuccess != expectedSuccess)
            {
                throw new Exception(expectedSuccess ?
                    $"IL Verify failed unexpectedly:{Environment.NewLine}{actualMessage}" :
                    "IL Verify succeeded unexpectedly");
            }

            if (!actualSuccess && verification.ILVerifyMessage != null && !IsEnglishLocal.Instance.ShouldSkip)
            {
                if (!verification.IncludeTokensAndModuleIds)
                {
                    actualMessage = Regex.Replace(actualMessage, @"\[[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\]", "");
                }

                AssertEx.AssertEqualToleratingWhitespaceDifferences(verification.ILVerifyMessage, actualMessage);
            }

            return;

            static (bool, string) verify(ILVerify.Verifier verifier, string corlibName, PEReader mainModule)
            {
                IEnumerable<ILVerify.VerificationResult>? result = null;
                int errorCount = 0;
                try
                {
                    verifier.SetSystemModuleName(AssemblyNameInfo.Parse(corlibName));
                    result = verifier.Verify(mainModule);
                    errorCount = result.Count();
                }
                catch (Exception e)
                {
                    return (false, e.Message);
                }

                if (errorCount > 0)
                {
                    var metadataReader = mainModule.GetMetadataReader();
                    return (false, printVerificationResult(result, metadataReader));
                }

                return (true, string.Empty);
            }

            static string printVerificationResult(IEnumerable<ILVerify.VerificationResult> result, MetadataReader metadataReader)
            {
                return string.Join(Environment.NewLine, result.Select(r => printMethod(r.Method, metadataReader) + r.Message + printErrorArguments(r.ErrorArguments)));
            }

            static string printMethod(MethodDefinitionHandle method, MetadataReader metadataReader)
            {
                if (method.IsNil)
                {
                    return "";
                }

                var methodName = metadataReader.GetString(metadataReader.GetMethodDefinition(method).Name);
                return $"[{methodName}]: ";
            }

            static string printErrorArguments(ILVerify.ErrorArgument[] errorArguments)
            {
                if (errorArguments is null
                    || errorArguments.Length == 0)
                {
                    return "";
                }

                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                builder.Append(" { ");
                var x = errorArguments.Select(a => printErrorArgument(a)).ToArray();
                for (int i = 0; i < x.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(x[i]);
                }
                builder.Append(" }");

                return pooledBuilder.ToStringAndFree();
            }

            static string printErrorArgument(ILVerify.ErrorArgument errorArgument)
            {
                var name = errorArgument.Name;

                string? value;
                if (name == "Offset" && errorArgument.Value is int i)
                {
                    value = "0x" + Convert.ToString(i, 16);
                }
                else
                {
                    Debug.Assert(errorArgument.Value != null);
                    value = errorArgument.Value.ToString();
                }

                return name + " = " + value;
            }
        }

        // TODO(tomat): Fold into CompileAndVerify. 
        // Replace bool verify parameter with string[] expectedPeVerifyOutput. If null, no verification. If empty verify have to succeed. Otherwise compare errors.
        public void EmitAndVerify(params string[] expectedPeVerifyOutput)
        {
            var emitData = Emit(null, null);
            using var testEnvironment = CreateRuntimeEnvironment(emitData.EmittedModule, emitData.Modules);
            string[] actualOutput = testEnvironment.VerifyModules([emitData.EmittedModule.FullName]);
            Assert.Equal(expectedPeVerifyOutput, actualOutput);
        }

        private EmitData Emit(IEnumerable<ResourceDescription>? manifestResources, EmitOptions? emitOptions)
        {
            var testData = new CompilationTestData();
            var diagnostics = DiagnosticBag.GetInstance();
            var dependencyList = new List<ModuleData>();
            var emitOutput = EmitCompilation(
                _compilation,
                manifestResources,
                dependencyList,
                diagnostics,
                testData,
                emitOptions);

            if (emitOutput is { } e)
            {
                var corLibIdentity = _compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly.Identity;
                var identity = _compilation.Assembly.Identity;
                var moduleData = new ModuleData(
                    identity,
                    _compilation.Options.OutputKind,
                    e.Assembly,
                    pdb: e.Pdb,
                    inMemoryModule: true,
                    isCorLib: corLibIdentity == identity);

                // We need to add the main module so that it gets checked against already loaded assembly names.
                // If an assembly is loaded directly via PEVerify(image) another assembly of the same full name
                // can't be loaded as a dependency (via Assembly.ReflectionOnlyLoad) in the same domain.
                dependencyList.Insert(0, moduleData);

                if (DumpAssemblyLocation is string dumpAssemblyLocation)
                {
                    DumpAssemblyData(dependencyList, dumpAssemblyLocation);
                }

                _emitData = new EmitData(
                    moduleData,
                    dependencyList.ToImmutableArray(),
                    diagnostics.ToReadOnlyAndFree(),
                    testData);
                return _emitData;
            }
            else
            {
                var dumpDir = DumpAssemblyLocation is string dumpAssemblyLocation ? DumpAssemblyData(dependencyList, dumpAssemblyLocation) : null;
                throw new EmitException(diagnostics.ToReadOnlyAndFree(), dumpDir);
            }
        }

        private IRuntimeEnvironment CreateRuntimeEnvironment(ModuleData mainModule, ImmutableArray<ModuleData> modules)
        {
            if (_dependencies is not null)
            {
                modules = [.. modules, .. _dependencies];
            }

            return RuntimeUtilities.CreateRuntimeEnvironment(mainModule, modules);
        }

        /// <summary>
        /// Obsolete. Use <see cref="VerifyMethodBody(string, string, bool, string, int, SymbolDisplayFormat?)"/> instead.
        /// </summary>
        public CompilationVerifier VerifyIL(
            string qualifiedMethodName,
            XCData expectedIL,
            bool realIL = false,
            SequencePointDisplayMode sequencePointDisplay = SequencePointDisplayMode.None,
            [CallerFilePath] string? callerPath = null,
            [CallerLineNumber] int callerLine = 0)
        {
            return VerifyILImpl(qualifiedMethodName, expectedIL.Value, realIL, sequencePointDisplay, callerPath, callerLine, escapeQuotes: false, ilFormat: null);
        }

        /// <summary>
        /// Obsolete. Use <see cref="VerifyMethodBody(string, string, bool, string, int, SymbolDisplayFormat?)"/> instead.
        /// </summary>
        public CompilationVerifier VerifyIL(
            string qualifiedMethodName,
            string expectedIL,
            bool realIL = false,
            SequencePointDisplayMode sequencePointDisplay = SequencePointDisplayMode.None,
            [CallerFilePath] string? callerPath = null,
            [CallerLineNumber] int callerLine = 0,
            SymbolDisplayFormat? ilFormat = null)
        {
            return VerifyILImpl(qualifiedMethodName, expectedIL, realIL, sequencePointDisplay, callerPath, callerLine, escapeQuotes: false, ilFormat);
        }

        public CompilationVerifier VerifyMethodBody(
            string qualifiedMethodName,
            string expectedILWithSequencePoints,
            bool realIL = false,
            [CallerFilePath] string? callerPath = null,
            [CallerLineNumber] int callerLine = 0,
            SymbolDisplayFormat? ilFormat = null)
        {
            return VerifyILImpl(qualifiedMethodName, expectedILWithSequencePoints, realIL, sequencePointDisplay: SequencePointDisplayMode.Enhanced, callerPath, callerLine, escapeQuotes: false, ilFormat);
        }

        public void VerifyILMultiple(params string[] qualifiedMethodNamesAndExpectedIL)
        {
            var names = ArrayBuilder<string>.GetInstance();
            var expected = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();
            var actual = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();
            var charPooledAllocs = ArrayBuilder<char[]>.GetInstance();
            var anyDifferent = false;
            for (int i = 0; i < qualifiedMethodNamesAndExpectedIL.Length;)
            {
                var qualifiedName = qualifiedMethodNamesAndExpectedIL[i++];
                names.Add(qualifiedName);
                var actualValue = AssertEx.NormalizeWhitespace(VisualizeIL(qualifiedName).AsSpan(), out var pooled1);
                var expectedValue = AssertEx.NormalizeWhitespace(qualifiedMethodNamesAndExpectedIL[i++].AsSpan(), out var pooled2);
                actual.Add(actualValue);
                expected.Add(expectedValue);
                charPooledAllocs.Add(pooled1);
                charPooledAllocs.Add(pooled2);
                if (!anyDifferent)
                {
                    anyDifferent = !actualValue.Span.SequenceEqual(expectedValue.Span);
                }
            }
            if (anyDifferent)
            {
                var builder = new StringBuilder();
                for (int i = 0; i < expected.Count; i++)
                {
                    builder.AppendLine(AssertEx.GetAssertMessage(expected[i].Span.ToString(), actual[i].Span.ToString(), prefix: names[i], escapeQuotes: true));
                }
                Assert.True(false, builder.ToString());
            }
            actual.Free();
            expected.Free();
            names.Free();
            foreach (var x in charPooledAllocs)
            {
                if (x != null)
                {
                    ArrayPool<char>.Shared.Return(x);
                }
            }
            charPooledAllocs.Free();
        }

        public CompilationVerifier VerifyMissing(
            string qualifiedMethodName)
        {
            Assert.False(GetEmitData().TestData.TryGetMethodData(qualifiedMethodName, out _));
            return this;
        }

        public void VerifyLocalSignature(
            string qualifiedMethodName,
            string expectedSignature,
            [CallerLineNumber] int callerLine = 0,
            [CallerFilePath] string? callerPath = null)
        {
            var ilBuilder = GetEmitData().TestData.GetMethodData(qualifiedMethodName).ILBuilder;
            string actualSignature = ILBuilderVisualizer.LocalSignatureToString(ilBuilder);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, actualSignature, escapeQuotes: true, expectedValueSourcePath: callerPath, expectedValueSourceLine: callerLine);
        }

        /// <summary>
        /// Visualizes the IL for a given method, and ensures that it matches the expected IL.
        /// </summary>
        /// <param name="realIL">Controls whether the IL stream contains pseudo-tokens or real tokens.</param>
        private CompilationVerifier VerifyILImpl(
            string qualifiedMethodName,
            string expectedIL,
            bool realIL,
            SequencePointDisplayMode sequencePointDisplay,
            string? callerPath,
            int callerLine,
            bool escapeQuotes,
            SymbolDisplayFormat? ilFormat)
        {
            string? actualIL = VisualizeIL(qualifiedMethodName, realIL, sequencePointDisplay, ilFormat);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, message: null, escapeQuotes, callerPath, callerLine);
            return this;
        }

        public string VisualizeIL(string qualifiedMethodName, bool realIL = false, SequencePointDisplayMode sequencePointDisplay = SequencePointDisplayMode.None, SymbolDisplayFormat? ilFormat = null)
            => VisualizeIL(GetEmitData().TestData.GetMethodData(qualifiedMethodName), realIL, sequencePointDisplay, ilFormat);

        internal string VisualizeIL(CompilationTestData.MethodData methodData, bool realIL = false, SequencePointDisplayMode sequencePointDisplay = SequencePointDisplayMode.None, SymbolDisplayFormat? ilFormat = null)
        {
            Dictionary<int, string>? markers = null;

            var emitData = GetEmitData();
            if (sequencePointDisplay != SequencePointDisplayMode.None)
            {
                var actualPdbXml = PdbToXmlConverter.ToXml(
                    pdbStream: new MemoryStream(emitData.EmittedAssemblyPdb.ToArray()),
                    peStream: new MemoryStream(emitData.EmittedAssemblyData.ToArray()),
                    options: PdbToXmlOptions.ResolveTokens |
                             PdbToXmlOptions.ThrowOnError |
                             PdbToXmlOptions.ExcludeCustomDebugInformation |
                             PdbToXmlOptions.ExcludeScopes |
                             PdbToXmlOptions.IncludeTokens);

                if (actualPdbXml.StartsWith("<error>"))
                {
                    throw new Exception($"Failed to extract PDB information. PdbToXmlConverter returned:{Environment.NewLine}{actualPdbXml}");
                }

                var method = methodData.Method.PartialDefinitionPart ?? methodData.Method;
                var methodDef = (Cci.IMethodDefinition)method.GetCciAdapter();
                var methodToken = MetadataTokens.GetToken(emitData.TestData.MetadataWriter!.GetMethodDefinitionOrReferenceHandle(methodDef));
                var xmlDocument = XElement.Parse(actualPdbXml);
                var xmlMethod = ILValidation.GetMethodElement(xmlDocument, methodToken);

                // method may not have any debug info and thus no sequence points
                if (xmlMethod != null)
                {
                    var documentMap = ILValidation.GetDocumentIdToPathMap(xmlDocument);

                    markers = sequencePointDisplay == SequencePointDisplayMode.Enhanced ?
                        ILValidation.GetSequencePointMarkers(xmlMethod, id => _compilation.SyntaxTrees.Single(tree => tree.FilePath == documentMap[id]).GetText()) :
                        ILValidation.GetSequencePointMarkers(xmlMethod);
                }
            }

            if (!realIL)
            {
                return ILBuilderVisualizer.ILBuilderToString(methodData.ILBuilder, markers: markers, ilFormat: ilFormat);
            }

            if (_lazyModuleSymbol == null)
            {
                var targetReference = LoadTestEmittedExecutableForSymbolValidation(emitData.EmittedAssemblyData, _compilation.Options.OutputKind, display: _compilation.AssemblyName);
                _lazyModuleSymbol = GetSymbolFromMetadata(targetReference, MetadataImportOptions.All);
            }

            if (_lazyModuleSymbol != null)
            {
                if (_visualizeRealIL == null)
                {
                    throw new InvalidOperationException("IL visualization function is not set");
                }

                return _visualizeRealIL(_lazyModuleSymbol, methodData, markers, emitData.TestData.Module!.GetMethodBody(methodData.Method)!.AreLocalsZeroed);
            }

            return "";
        }

        public CompilationVerifier VerifyMemberInIL(string methodName, bool expected)
        {
            Assert.Equal(expected, GetEmitData().TestData.GetMethodsByName().ContainsKey(methodName));
            return this;
        }

        public CompilationVerifier VerifyDiagnostics(params DiagnosticDescription[] expected)
        {
            GetEmitData().Diagnostics.Verify(expected);
            return this;
        }

        internal IModuleSymbol GetSymbolFromMetadata(MetadataReference metadataReference, MetadataImportOptions importOptions)
        {
            var dummy = _compilation
                .RemoveAllSyntaxTrees()
                .AddReferences(metadataReference)
                .WithAssemblyName("Dummy")
                .WithOptions(_compilation.Options.WithMetadataImportOptions(importOptions));

            var symbol = dummy.GetAssemblyOrModuleSymbol(metadataReference);
            Debug.Assert(symbol is not null);

            if (metadataReference.Properties.Kind == MetadataImageKind.Assembly)
            {
                return ((IAssemblySymbol)symbol).Modules.First();
            }
            else
            {
                return (IModuleSymbol)symbol;
            }
        }

        internal static MetadataReference LoadTestEmittedExecutableForSymbolValidation(
            ImmutableArray<byte> image,
            OutputKind outputKind,
            string? display = null)
        {
            var moduleMetadata = ModuleMetadata.CreateFromImage(image);
            moduleMetadata.Module.PretendThereArentNoPiaLocalTypes();

            if (outputKind == OutputKind.NetModule)
            {
                return moduleMetadata.GetReference(display: display);
            }
            else
            {
                return AssemblyMetadata.Create(moduleMetadata).GetReference(display: display);
            }
        }

        public void VerifyOperationTree(string expectedOperationTree, bool skipImplicitlyDeclaredSymbols = false)
        {
            _compilation.VerifyOperationTree(expectedOperationTree, skipImplicitlyDeclaredSymbols);
        }

        public void VerifyOperationTree(string symbolToVerify, string expectedOperationTree, bool skipImplicitlyDeclaredSymbols = false)
        {
            _compilation.VerifyOperationTree(symbolToVerify, expectedOperationTree, skipImplicitlyDeclaredSymbols);
        }

        /// <summary>
        /// Useful for verifying the expected variables are hoisted for closures, async, and iterator methods.
        /// </summary>
        public void VerifySynthesizedFields(string containingTypeName, params string[] expectedFields)
        {
            var testData = GetEmitData().TestData;
            var types = testData.Module!.GetAllSynthesizedMembers();
            Assert.Contains(types.Keys, t => containingTypeName == t.ToString());
            var members = testData.Module.GetAllSynthesizedMembers()
                .Where(e => e.Key.ToString() == containingTypeName)
                .Single()
                .Value
                .Where(s => s.Kind == SymbolKind.Field)
                .Select(f => $"{((IFieldSymbol)f.GetISymbol()).Type.ToString()} {f.Name}")
                .ToList();
            AssertEx.SetEqual(expectedFields, members);
        }

        /// <summary>
        /// Emit all of the references which are not directly or indirectly a <see cref="Compilation"/> value.
        /// </summary>
        internal static void EmitReferences(Compilation compilation, HashSet<string> fullNameSet, List<ModuleData> dependencies, AssemblyIdentity corLibIdentity)
        {
            // NOTE: specifically don't need to consider previous submissions since they will always be compilations.
            foreach (var metadataReference in compilation.References)
            {
                if (metadataReference is CompilationReference)
                {
                    continue;
                }

                var peRef = (PortableExecutableReference)metadataReference;
                var metadata = peRef.GetMetadataNoCopy();
                var isManifestModule = peRef.Properties.Kind == MetadataImageKind.Assembly;
                var identity = isManifestModule
                    ? ((AssemblyMetadata)metadata).GetAssembly()!.Identity
                    : null;

                // If this is an indirect reference to a Compilation then it is already been emitted 
                // so no more work to be done.
                if (isManifestModule && fullNameSet.Contains(identity!.GetDisplayName()))
                {
                    continue;
                }

                var isCorLib = isManifestModule && corLibIdentity == identity;
                foreach (var module in enumerateModules(metadata))
                {
                    ImmutableArray<byte> bytes = module.Module.PEReaderOpt.GetEntireImage().GetContent();
                    ModuleData moduleData;
                    if (isManifestModule)
                    {
                        fullNameSet.Add(identity!.GetDisplayName());
                        moduleData = new ModuleData(identity,
                                                    OutputKind.DynamicallyLinkedLibrary,
                                                    bytes,
                                                    pdb: default(ImmutableArray<byte>),
                                                    inMemoryModule: true,
                                                    isCorLib);
                    }
                    else
                    {
                        moduleData = new ModuleData(module.Name,
                                                    bytes,
                                                    pdb: default(ImmutableArray<byte>),
                                                    inMemoryModule: true,
                                                    isCorLib: false);
                    }

                    dependencies.Add(moduleData);
                    isManifestModule = false;
                }
            }

            static IEnumerable<ModuleMetadata> enumerateModules(Metadata metadata)
            {
                return (metadata.Kind == MetadataImageKind.Assembly) ? ((AssemblyMetadata)metadata).GetModules().AsEnumerable() : SpecializedCollections.SingletonEnumerable((ModuleMetadata)metadata);
            }
        }

        internal static EmitOutput? EmitCompilation(
            Compilation compilation,
            IEnumerable<ResourceDescription>? manifestResources,
            List<ModuleData> dependencies,
            DiagnosticBag diagnostics,
            CompilationTestData? testData,
            EmitOptions? emitOptions)
        {
            var corLibIdentity = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly.Identity;

            // A Compilation can appear multiple times in a dependency graph as both a Compilation and as a MetadataReference
            // value.  Iterate the Compilations eagerly so they are always emitted directly and later references can re-use 
            // the value.  This gives better, and consistent, diagnostic information.
            var referencedCompilations = findReferencedCompilations(compilation);
            var fullNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var referencedCompilation in referencedCompilations)
            {
                var emitData = emitCompilationCore(referencedCompilation, null, diagnostics, null, emitOptions);
                if (emitData.HasValue)
                {
                    var identity = referencedCompilation.Assembly.Identity;
                    var moduleData = new ModuleData(identity,
                                                    OutputKind.DynamicallyLinkedLibrary,
                                                    emitData.Value.Assembly,
                                                    pdb: default(ImmutableArray<byte>),
                                                    inMemoryModule: true,
                                                    isCorLib: corLibIdentity == identity);
                    fullNameSet.Add(moduleData.Id.FullName);
                    dependencies.Add(moduleData);
                }
            }

            // Now that the Compilation values have been emitted, emit the non-compilation references
            foreach (var current in (new[] { compilation }).Concat(referencedCompilations))
            {
                EmitReferences(current, fullNameSet, dependencies, corLibIdentity);
            }

            return emitCompilationCore(compilation, manifestResources, diagnostics, testData, emitOptions);

            // Find all of the <see cref="Compilation"/> values reachable from this instance.
            static List<Compilation> findReferencedCompilations(Compilation original)
            {
                var list = new List<Compilation>();
                var toVisit = new Queue<Compilation>(findDirectReferencedCompilations(original));

                while (toVisit.Count > 0)
                {
                    var current = toVisit.Dequeue();
                    if (list.Contains(current))
                    {
                        continue;
                    }

                    list.Add(current);

                    foreach (var other in findDirectReferencedCompilations(current))
                    {
                        toVisit.Enqueue(other);
                    }
                }

                return list;
            }

            static List<Compilation> findDirectReferencedCompilations(Compilation compilation)
            {
                var list = new List<Compilation>();
                var previousCompilation = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
                if (previousCompilation != null)
                {
                    list.Add(previousCompilation);
                }

                foreach (var reference in compilation.References.OfType<CompilationReference>())
                {
                    list.Add(reference.Compilation);
                }

                return list;
            }

            static EmitOutput? emitCompilationCore(
                Compilation compilation,
                IEnumerable<ResourceDescription>? manifestResources,
                DiagnosticBag diagnostics,
                CompilationTestData? testData,
                EmitOptions? emitOptions)
            {
                emitOptions ??= EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded);

                using var executableStream = new MemoryStream();

                var pdb = default(ImmutableArray<byte>);
                var assembly = default(ImmutableArray<byte>);
                var pdbStream = (emitOptions.DebugInformationFormat != DebugInformationFormat.Embedded) ? new MemoryStream() : null;

                // Note: don't forget to name the source inputs to get them embedded for debugging
                var embeddedTexts = compilation.SyntaxTrees
                    .Select(t => (filePath: t.FilePath, text: t.GetText()))
                    .Where(t => t.text.CanBeEmbedded && !string.IsNullOrEmpty(t.filePath))
                    .Select(t => EmbeddedText.FromSource(t.filePath, t.text))
                    .ToImmutableArray();

                EmitResult result;
                try
                {
                    result = compilation.Emit(
                        executableStream,
                        metadataPEStream: null,
                        pdbStream: pdbStream,
                        xmlDocumentationStream: null,
                        win32Resources: null,
                        manifestResources: manifestResources,
                        options: emitOptions,
                        debugEntryPoint: null,
                        sourceLinkStream: null,
                        embeddedTexts,
                        rebuildData: null,
                        testData: testData,
                        cancellationToken: default);
                }
                finally
                {
                    if (pdbStream != null)
                    {
                        pdb = pdbStream.ToImmutable();
                        pdbStream.Dispose();
                    }
                }

                diagnostics.AddRange(result.Diagnostics);
                assembly = executableStream.ToImmutable();

                if (result.Success)
                {
                    return new EmitOutput(assembly, pdb);
                }

                return null;
            }
        }
    }
}
