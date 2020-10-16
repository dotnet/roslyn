﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias PDB;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Microsoft.Metadata.Tools;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;
using PDB::Roslyn.Test.Utilities;
using PDB::Roslyn.Test.PdbUtilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class Scope
    {
        internal readonly int StartOffset;
        internal readonly int EndOffset;
        internal readonly ImmutableArray<string> Locals;

        internal Scope(int startOffset, int endOffset, ImmutableArray<string> locals, bool isEndInclusive)
        {
            this.StartOffset = startOffset;
            this.EndOffset = endOffset + (isEndInclusive ? 1 : 0);
            this.Locals = locals;
        }

        internal int Length
        {
            get { return this.EndOffset - this.StartOffset + 1; }
        }

        internal bool Contains(int offset)
        {
            return (offset >= this.StartOffset) && (offset < this.EndOffset);
        }
    }

    internal static class ExpressionCompilerTestHelpers
    {
        internal static CompileResult CompileAssignment(
            this EvaluationContextBase context,
            string target,
            string expr,
            out string error,
            CompilationTestData testData = null,
            DiagnosticFormatter formatter = null)
        {
            ResultProperties resultProperties;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var result = context.CompileAssignment(
                target,
                expr,
                ImmutableArray<Alias>.Empty,
                formatter ?? DebuggerDiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            // This is a crude way to test the language, but it's convenient to share this test helper.
            var isCSharp = context.GetType().Namespace.IndexOf("csharp", StringComparison.OrdinalIgnoreCase) >= 0;
            var expectedFlags = error != null
                ? DkmClrCompilationResultFlags.None
                : isCSharp
                    ? DkmClrCompilationResultFlags.PotentialSideEffect
                    : DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult;
            Assert.Equal(expectedFlags, resultProperties.Flags);
            Assert.Equal(default(DkmEvaluationResultCategory), resultProperties.Category);
            Assert.Equal(default(DkmEvaluationResultAccessType), resultProperties.AccessType);
            Assert.Equal(default(DkmEvaluationResultStorageType), resultProperties.StorageType);
            Assert.Equal(default(DkmEvaluationResultTypeModifierFlags), resultProperties.ModifierFlags);
            return result;
        }

        internal static CompileResult CompileAssignment(
            this EvaluationContextBase context,
            string target,
            string expr,
            ImmutableArray<Alias> aliases,
            DiagnosticFormatter formatter,
            out ResultProperties resultProperties,
            out string error,
            out ImmutableArray<AssemblyIdentity> missingAssemblyIdentities,
            CultureInfo preferredUICulture,
            CompilationTestData testData)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var result = context.CompileAssignment(target, expr, aliases, diagnostics, out resultProperties, testData);
            if (diagnostics.HasAnyErrors())
            {
                bool useReferencedModulesOnly;
                error = context.GetErrorMessageAndMissingAssemblyIdentities(diagnostics, formatter, preferredUICulture, EvaluationContextBase.SystemCoreIdentity, out useReferencedModulesOnly, out missingAssemblyIdentities);
            }
            else
            {
                error = null;
                missingAssemblyIdentities = ImmutableArray<AssemblyIdentity>.Empty;
            }
            diagnostics.Free();
            return result;
        }

        internal static ReadOnlyCollection<byte> CompileGetLocals(
            this EvaluationContextBase context,
            ArrayBuilder<LocalAndMethod> locals,
            bool argumentsOnly,
            out string typeName,
            CompilationTestData testData,
            DiagnosticDescription[] expectedDiagnostics = null)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var result = context.CompileGetLocals(
                locals,
                argumentsOnly,
                ImmutableArray<Alias>.Empty,
                diagnostics,
                out typeName,
                testData);
            diagnostics.Verify(expectedDiagnostics ?? DiagnosticDescription.None);
            diagnostics.Free();
            return result;
        }

        internal static CompileResult CompileExpression(
            this EvaluationContextBase context,
            string expr,
            out string error,
            CompilationTestData testData = null,
            DiagnosticFormatter formatter = null)
        {
            ResultProperties resultProperties;
            return CompileExpression(context, expr, out resultProperties, out error, testData, formatter);
        }

        internal static CompileResult CompileExpression(
            this EvaluationContextBase context,
            string expr,
            out ResultProperties resultProperties,
            out string error,
            CompilationTestData testData = null,
            DiagnosticFormatter formatter = null)
        {
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var result = context.CompileExpression(
                expr,
                DkmEvaluationFlags.TreatAsExpression,
                ImmutableArray<Alias>.Empty,
                formatter ?? DebuggerDiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            return result;
        }

        internal static CompileResult CompileExpression(
            this EvaluationContextBase evaluationContext,
            string expr,
            DkmEvaluationFlags compilationFlags,
            ImmutableArray<Alias> aliases,
            out string error,
            CompilationTestData testData = null,
            DiagnosticFormatter formatter = null)
        {
            ResultProperties resultProperties;
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var result = evaluationContext.CompileExpression(
                expr,
                compilationFlags,
                aliases,
                formatter ?? DebuggerDiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            return result;
        }

        /// <summary>
        /// Compile C# expression and emit assembly with evaluation method.
        /// </summary>
        /// <returns>
        /// Result containing generated assembly, type and method names, and any format specifiers.
        /// </returns>
        internal static CompileResult CompileExpression(
            this EvaluationContextBase evaluationContext,
            string expr,
            DkmEvaluationFlags compilationFlags,
            ImmutableArray<Alias> aliases,
            DiagnosticFormatter formatter,
            out ResultProperties resultProperties,
            out string error,
            out ImmutableArray<AssemblyIdentity> missingAssemblyIdentities,
            CultureInfo preferredUICulture,
            CompilationTestData testData)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var result = evaluationContext.CompileExpression(expr, compilationFlags, aliases, diagnostics, out resultProperties, testData);
            if (diagnostics.HasAnyErrors())
            {
                bool useReferencedModulesOnly;
                error = evaluationContext.GetErrorMessageAndMissingAssemblyIdentities(diagnostics, formatter, preferredUICulture, EvaluationContextBase.SystemCoreIdentity, out useReferencedModulesOnly, out missingAssemblyIdentities);
            }
            else
            {
                error = null;
                missingAssemblyIdentities = ImmutableArray<AssemblyIdentity>.Empty;
            }
            diagnostics.Free();
            return result;
        }

        internal static CompileResult CompileExpressionWithRetry(
            ImmutableArray<MetadataBlock> metadataBlocks,
            EvaluationContextBase context,
            ExpressionCompiler.CompileDelegate<CompileResult> compile,
            DkmUtilities.GetMetadataBytesPtrFunction getMetaDataBytesPtr,
            out string errorMessage)
        {
            return ExpressionCompiler.CompileWithRetry(
                metadataBlocks,
                DebuggerDiagnosticFormatter.Instance,
                (blocks, useReferencedModulesOnly) => context,
                compile,
                getMetaDataBytesPtr,
                out errorMessage);
        }

        internal static CompileResult CompileExpressionWithRetry(
            ImmutableArray<MetadataBlock> metadataBlocks,
            string expr,
            ImmutableArray<Alias> aliases,
            ExpressionCompiler.CreateContextDelegate createContext,
            DkmUtilities.GetMetadataBytesPtrFunction getMetaDataBytesPtr,
            out string errorMessage,
            out CompilationTestData testData)
        {
            var r = ExpressionCompiler.CompileWithRetry(
                metadataBlocks,
                DebuggerDiagnosticFormatter.Instance,
                createContext,
                (context, diagnostics) =>
                {
                    var td = new CompilationTestData();
                    ResultProperties resultProperties;
                    var compileResult = context.CompileExpression(
                        expr,
                        DkmEvaluationFlags.TreatAsExpression,
                        aliases,
                        diagnostics,
                        out resultProperties,
                        td);
                    return new CompileExpressionResult(compileResult, td);
                },
                getMetaDataBytesPtr,
                out errorMessage);
            testData = r.TestData;
            return r.CompileResult;
        }

        private struct CompileExpressionResult
        {
            internal readonly CompileResult CompileResult;
            internal readonly CompilationTestData TestData;

            internal CompileExpressionResult(CompileResult compileResult, CompilationTestData testData)
            {
                this.CompileResult = compileResult;
                this.TestData = testData;
            }
        }

        internal static TypeDefinition GetTypeDef(this MetadataReader reader, string typeName)
        {
            return reader.TypeDefinitions.Select(reader.GetTypeDefinition).First(t => reader.StringComparer.Equals(t.Name, typeName));
        }

        internal static MethodDefinition GetMethodDef(this MetadataReader reader, TypeDefinition typeDef, string methodName)
        {
            return typeDef.GetMethods().Select(reader.GetMethodDefinition).First(m => reader.StringComparer.Equals(m.Name, methodName));
        }

        internal static MethodDefinitionHandle GetMethodDefHandle(this MetadataReader reader, TypeDefinition typeDef, string methodName)
        {
            return typeDef.GetMethods().First(h => reader.StringComparer.Equals(reader.GetMethodDefinition(h).Name, methodName));
        }

        internal static void CheckTypeParameters(this MetadataReader reader, GenericParameterHandleCollection genericParameters, params string[] expectedNames)
        {
            var actualNames = genericParameters.Select(reader.GetGenericParameter).Select(tp => reader.GetString(tp.Name)).ToArray();
            Assert.True(expectedNames.SequenceEqual(actualNames));
        }

        internal static AssemblyName GetAssemblyName(this byte[] exeBytes)
        {
            using (var reader = new PEReader(ImmutableArray.CreateRange(exeBytes)))
            {
                var metadataReader = reader.GetMetadataReader();
                var def = metadataReader.GetAssemblyDefinition();
                var name = metadataReader.GetString(def.Name);
                return new AssemblyName() { Name = name, Version = def.Version };
            }
        }

        internal static Guid GetModuleVersionId(this byte[] exeBytes)
        {
            using (var reader = new PEReader(ImmutableArray.CreateRange(exeBytes)))
            {
                return reader.GetMetadataReader().GetModuleVersionId();
            }
        }

        internal static ImmutableArray<string> GetLocalNames(this ISymUnmanagedReader symReader, int methodToken, int methodVersion = 1)
        {
            var method = symReader.GetMethodByVersion(methodToken, methodVersion);
            if (method == null)
            {
                return ImmutableArray<string>.Empty;
            }
            var scopes = ArrayBuilder<ISymUnmanagedScope>.GetInstance();
            method.GetAllScopes(scopes);
            var names = ArrayBuilder<string>.GetInstance();
            foreach (var scope in scopes)
            {
                foreach (var local in scope.GetLocals())
                {
                    var name = local.GetName();
                    int slot;
                    local.GetAddressField1(out slot);
                    while (names.Count <= slot)
                    {
                        names.Add(null);
                    }
                    names[slot] = name;
                }
            }
            scopes.Free();
            return names.ToImmutableAndFree();
        }

        internal static void VerifyIL(
            this byte[] assembly,
            int methodToken,
            string qualifiedName,
            string expectedIL,
            [CallerLineNumber] int expectedValueSourceLine = 0,
            [CallerFilePath] string expectedValueSourcePath = null)
        {
            var parts = qualifiedName.Split('.');
            if (parts.Length != 2)
            {
                throw new NotImplementedException();
            }

            using (var metadata = ModuleMetadata.CreateFromImage(assembly))
            {
                var module = metadata.Module;
                var reader = module.MetadataReader;
                var methodHandle = (MethodDefinitionHandle)MetadataTokens.Handle(methodToken);
                var methodDef = reader.GetMethodDefinition(methodHandle);
                var typeDef = reader.GetTypeDefinition(methodDef.GetDeclaringType());
                Assert.True(reader.StringComparer.Equals(typeDef.Name, parts[0]));
                Assert.True(reader.StringComparer.Equals(methodDef.Name, parts[1]));
                var methodBody = module.GetMethodBodyOrThrow(methodHandle);

                var pooled = PooledStringBuilder.GetInstance();
                var builder = pooled.Builder;

                if (!methodBody.LocalSignature.IsNil)
                {
                    var visualizer = new MetadataVisualizer(reader, new StringWriter(), MetadataVisualizerOptions.NoHeapReferences);
                    var signature = reader.GetStandaloneSignature(methodBody.LocalSignature);
                    builder.AppendFormat("Locals: {0}", visualizer.StandaloneSignature(signature.Signature));
                    builder.AppendLine();
                }

                ILVisualizer.Default.DumpMethod(
                    builder,
                    methodBody.MaxStack,
                    methodBody.GetILContent(),
                    ImmutableArray.Create<ILVisualizer.LocalInfo>(),
                    ImmutableArray.Create<ILVisualizer.HandlerSpan>());

                var actualIL = pooled.ToStringAndFree();

                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, escapeQuotes: true, expectedValueSourcePath: expectedValueSourcePath, expectedValueSourceLine: expectedValueSourceLine);
            }
        }

        internal static ImmutableArray<MetadataReference> GetEmittedReferences(Compilation compilation, MetadataReader mdReader)
        {
            // Determine the set of references that were actually used
            // and ignore any references that were dropped in emit.
            var referenceNames = new HashSet<string>(mdReader.AssemblyReferences.Select(h => GetAssemblyReferenceName(mdReader, h)));
            return ImmutableArray.CreateRange(compilation.References.Where(r => IsReferenced(r, referenceNames)));
        }

        internal static ImmutableArray<Scope> GetScopes(this ISymUnmanagedReader symReader, int methodToken, int methodVersion, bool isEndInclusive)
        {
            var method = symReader.GetMethodByVersion(methodToken, methodVersion);
            if (method == null)
            {
                return ImmutableArray<Scope>.Empty;
            }
            var scopes = ArrayBuilder<ISymUnmanagedScope>.GetInstance();
            method.GetAllScopes(scopes);
            var result = scopes.SelectAsArray(s => new Scope(s.GetStartOffset(), s.GetEndOffset(), ImmutableArray.CreateRange(s.GetLocals().Select(l => l.GetName())), isEndInclusive));
            scopes.Free();
            return result;
        }

        internal static Scope GetInnermostScope(this ImmutableArray<Scope> scopes, int offset)
        {
            Scope result = null;
            foreach (var scope in scopes)
            {
                if (scope.Contains(offset))
                {
                    if ((result == null) || (result.Length > scope.Length))
                    {
                        result = scope;
                    }
                }
            }
            return result;
        }

        private static string GetAssemblyReferenceName(MetadataReader reader, AssemblyReferenceHandle handle)
        {
            var reference = reader.GetAssemblyReference(handle);
            return reader.GetString(reference.Name);
        }

        private static bool IsReferenced(MetadataReference reference, HashSet<string> referenceNames)
        {
            var assemblyMetadata = ((PortableExecutableReference)reference).GetMetadataNoCopy() as AssemblyMetadata;
            if (assemblyMetadata == null)
            {
                // Netmodule. Assume it is referenced.
                return true;
            }
            var name = assemblyMetadata.GetAssembly().Identity.Name;
            return referenceNames.Contains(name);
        }

        internal static ModuleInstance ToModuleInstance(this MetadataReference reference)
        {
            return ModuleInstance.Create((PortableExecutableReference)reference);
        }

        internal static ModuleInstance ToModuleInstance(
            this Compilation compilation,
            DebugInformationFormat debugFormat = DebugInformationFormat.Pdb,
            bool includeLocalSignatures = true)
        {
            var pdbStream = (debugFormat != 0) ? new MemoryStream() : null;
            var peImage = compilation.EmitToArray(new EmitOptions(debugInformationFormat: debugFormat), pdbStream: pdbStream);
            var symReader = (debugFormat != 0) ? SymReaderFactory.CreateReader(pdbStream, new PEReader(peImage)) : null;

            return ModuleInstance.Create(peImage, symReader, includeLocalSignatures);
        }

        internal static ModuleInstance GetModuleInstanceForIL(string ilSource)
        {
            ImmutableArray<byte> peBytes;
            ImmutableArray<byte> pdbBytes;
            CommonTestBase.EmitILToArray(ilSource, appendDefaultHeader: true, includePdb: true, assemblyBytes: out peBytes, pdbBytes: out pdbBytes);
            return ModuleInstance.Create(peBytes, SymReaderFactory.CreateReader(pdbBytes), includeLocalSignatures: true);
        }

        internal static AssemblyIdentity GetAssemblyIdentity(this MetadataReference reference)
        {
            using (var moduleMetadata = GetManifestModuleMetadata(reference))
            {
                return moduleMetadata.MetadataReader.ReadAssemblyIdentityOrThrow();
            }
        }

        internal static Guid GetModuleVersionId(this MetadataReference reference)
        {
            using (var moduleMetadata = GetManifestModuleMetadata(reference))
            {
                return moduleMetadata.MetadataReader.GetModuleVersionIdOrThrow();
            }
        }

        private static ModuleMetadata GetManifestModuleMetadata(MetadataReference reference)
        {
            // make a copy to avoid disposing shared reference metadata:
            var metadata = ((MetadataImageReference)reference).GetMetadata();
            return (metadata as AssemblyMetadata)?.GetModules()[0] ?? (ModuleMetadata)metadata;
        }

        internal static void VerifyLocal<TMethodSymbol>(
            this CompilationTestData testData,
            string typeName,
            LocalAndMethod localAndMethod,
            string expectedMethodName,
            string expectedLocalName,
            string expectedLocalDisplayName,
            DkmClrCompilationResultFlags expectedFlags,
            Action<TMethodSymbol> verifyTypeParameters,
            string expectedILOpt,
            bool expectedGeneric,
            string expectedValueSourcePath,
            int expectedValueSourceLine)
            where TMethodSymbol : IMethodSymbolInternal
        {
            Assert.Equal(expectedLocalName, localAndMethod.LocalName);
            Assert.Equal(expectedLocalDisplayName, localAndMethod.LocalDisplayName);
            Assert.True(expectedMethodName.StartsWith(localAndMethod.MethodName, StringComparison.Ordinal), expectedMethodName + " does not start with " + localAndMethod.MethodName); // Expected name may include type arguments and parameters.
            Assert.Equal(expectedFlags, localAndMethod.Flags);
            var methodData = testData.GetMethodData(typeName + "." + expectedMethodName);
            verifyTypeParameters((TMethodSymbol)methodData.Method);
            if (expectedILOpt != null)
            {
                string actualIL = methodData.GetMethodIL();
                AssertEx.AssertEqualToleratingWhitespaceDifferences(
                    expectedILOpt,
                    actualIL,
                    escapeQuotes: true,
                    expectedValueSourcePath: expectedValueSourcePath,
                    expectedValueSourceLine: expectedValueSourceLine);
            }

            Assert.Equal(((Cci.IMethodDefinition)methodData.Method).CallingConvention, expectedGeneric ? Cci.CallingConvention.Generic : Cci.CallingConvention.Default);
        }

        internal static void VerifyResolutionRequests(EEMetadataReferenceResolver resolver, (AssemblyIdentity, AssemblyIdentity, int)[] expectedRequests)
        {
#if DEBUG
            var expected = ArrayBuilder<(AssemblyIdentity, AssemblyIdentity, int)>.GetInstance();
            var actual = ArrayBuilder<(AssemblyIdentity, AssemblyIdentity, int)>.GetInstance();
            expected.AddRange(expectedRequests);
            sort(expected);
            actual.AddRange(resolver.Requests.Select(pair => (pair.Key, pair.Value.Identity, pair.Value.Count)));
            sort(actual);
            AssertEx.Equal(expected, actual);
            actual.Free();
            expected.Free();

            void sort(ArrayBuilder<(AssemblyIdentity, AssemblyIdentity, int)> builder)
            {
                builder.Sort((x, y) => AssemblyIdentityComparer.SimpleNameComparer.Compare(x.Item1.GetDisplayName(), y.Item1.GetDisplayName()));
            }
#endif
        }

        internal static void VerifyAppDomainMetadataContext<TAssemblyContext>(MetadataContext<TAssemblyContext> metadataContext, Guid[] moduleVersionIds)
            where TAssemblyContext : struct
        {
            var actualIds = metadataContext.AssemblyContexts.Keys.Select(key => key.ModuleVersionId.ToString()).ToArray();
            Array.Sort(actualIds);
            var expectedIds = moduleVersionIds.Select(mvid => mvid.ToString()).ToArray();
            Array.Sort(expectedIds);
            AssertEx.Equal(expectedIds, actualIds);
        }

        internal static ISymUnmanagedReader ConstructSymReaderWithImports(ImmutableArray<byte> peImage, string methodName, params string[] importStrings)
        {
            using (var peReader = new PEReader(peImage))
            {
                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = metadataReader.MethodDefinitions.Single(h => metadataReader.StringComparer.Equals(metadataReader.GetMethodDefinition(h).Name, methodName));
                var methodToken = metadataReader.GetToken(methodHandle);

                return new MockSymUnmanagedReader(new Dictionary<int, MethodDebugInfoBytes>
                {
                    { methodToken, new MethodDebugInfoBytes.Builder(new [] { importStrings }).Build() },
                }.ToImmutableDictionary());
            }
        }

        internal const uint NoILOffset = 0xffffffff;

        internal static readonly MetadataReference IntrinsicAssemblyReference = GetIntrinsicAssemblyReference();

        internal static ImmutableArray<MetadataReference> AddIntrinsicAssembly(this ImmutableArray<MetadataReference> references)
        {
            var builder = ArrayBuilder<MetadataReference>.GetInstance();
            builder.AddRange(references);
            builder.Add(IntrinsicAssemblyReference);
            return builder.ToImmutableAndFree();
        }

        private static MetadataReference GetIntrinsicAssemblyReference()
        {
            var source =
@".assembly extern mscorlib { }
.class public Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods
{
  .method public static object GetObjectAtAddress(uint64 address)
  {
    ldnull
    throw
  }
  .method public static class [mscorlib]System.Exception GetException()
  {
    ldnull
    throw
  }
  .method public static class [mscorlib]System.Exception GetStowedException()
  {
    ldnull
    throw
  }
  .method public static object GetReturnValue(int32 index)
  {
    ldnull
    throw
  }
  .method public static void CreateVariable(class [mscorlib]System.Type 'type', string name, valuetype [mscorlib]System.Guid customTypeInfoPayloadTypeId, uint8[] customTypeInfoPayload)
  {
    ldnull
    throw
  }
  .method public static object GetObjectByAlias(string name)
  {
    ldnull
    throw
  }
  .method public static !!T& GetVariableAddress<T>(string name)
  {
    ldnull
    throw
  }
}";
            return CommonTestBase.CompileIL(source);
        }

        /// <summary>
        /// Return MetadataReferences to the .winmd assemblies
        /// for the given namespaces.
        /// </summary>
        internal static ImmutableArray<MetadataReference> GetRuntimeWinMds(params string[] namespaces)
        {
            var paths = new HashSet<string>();
            foreach (var @namespace in namespaces)
            {
                foreach (var path in WindowsRuntimeMetadata.ResolveNamespace(@namespace, null))
                {
                    paths.Add(path);
                }
            }
            return ImmutableArray.CreateRange(paths.Select(GetAssembly));
        }

        private const string Version1_3CLRString = "WindowsRuntime 1.3;CLR v4.0.30319";
        private const string Version1_3String = "WindowsRuntime 1.3";
        private const string Version1_4String = "WindowsRuntime 1.4";
        private static readonly int s_versionStringLength = Version1_3CLRString.Length;

        private static readonly byte[] s_version1_3CLRBytes = ToByteArray(Version1_3CLRString, s_versionStringLength);
        private static readonly byte[] s_version1_3Bytes = ToByteArray(Version1_3String, s_versionStringLength);
        private static readonly byte[] s_version1_4Bytes = ToByteArray(Version1_4String, s_versionStringLength);

        private static byte[] ToByteArray(string str, int length)
        {
            var bytes = new byte[length];
            for (int i = 0; i < str.Length; i++)
            {
                bytes[i] = (byte)str[i];
            }
            return bytes;
        }

        internal static byte[] ToVersion1_3(byte[] bytes)
        {
            return ToVersion(bytes, s_version1_3CLRBytes, s_version1_3Bytes);
        }

        internal static byte[] ToVersion1_4(byte[] bytes)
        {
            return ToVersion(bytes, s_version1_3CLRBytes, s_version1_4Bytes);
        }

        private static byte[] ToVersion(byte[] bytes, byte[] from, byte[] to)
        {
            int n = bytes.Length;
            var copy = new byte[n];
            Array.Copy(bytes, copy, n);
            int index = IndexOf(copy, from);
            Array.Copy(to, 0, copy, index, to.Length);
            return copy;
        }

        private static int IndexOf(byte[] a, byte[] b)
        {
            int m = b.Length;
            int n = a.Length - m;
            for (int x = 0; x < n; x++)
            {
                var matches = true;
                for (int y = 0; y < m; y++)
                {
                    if (a[x + y] != b[y])
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                {
                    return x;
                }
            }
            return -1;
        }

        private static MetadataReference GetAssembly(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var metadata = ModuleMetadata.CreateFromImage(bytes);
            return metadata.GetReference(filePath: path);
        }

        internal static uint GetOffset(int methodToken, ISymUnmanagedReader symReader, int atLineNumber = -1)
        {
            int ilOffset;
            if (symReader == null)
            {
                ilOffset = 0;
            }
            else
            {
                var symMethod = symReader.GetMethod(methodToken);
                if (symMethod == null)
                {
                    ilOffset = 0;
                }
                else
                {
                    var sequencePoints = symMethod.GetSequencePoints();
                    ilOffset = atLineNumber < 0
                        ? sequencePoints.Where(sp => sp.StartLine != Cci.SequencePoint.HiddenLine).Select(sp => sp.Offset).FirstOrDefault()
                        : sequencePoints.First(sp => sp.StartLine == atLineNumber).Offset;
                }
            }
            Assert.InRange(ilOffset, 0, int.MaxValue);
            return (uint)ilOffset;
        }

        internal static string GetMethodOrTypeSignatureParts(string signature, out string[] parameterTypeNames)
        {
            var parameterListStart = signature.IndexOf('(');
            if (parameterListStart < 0)
            {
                parameterTypeNames = null;
                return signature;
            }

            var parameters = signature.Substring(parameterListStart + 1, signature.Length - parameterListStart - 2);
            var methodName = signature.Substring(0, parameterListStart);
            parameterTypeNames = (parameters.Length == 0) ?
                new string[0] :
                parameters.Split(',');
            return methodName;
        }

        internal static unsafe ModuleMetadata ToModuleMetadata(this PEMemoryBlock metadata, bool ignoreAssemblyRefs)
        {
            return ModuleMetadata.CreateFromMetadata(
                (IntPtr)metadata.Pointer,
                metadata.Length,
                includeEmbeddedInteropTypes: false,
                ignoreAssemblyRefs: ignoreAssemblyRefs);
        }

        internal static unsafe MetadataReader ToMetadataReader(this PEMemoryBlock metadata)
        {
            return new MetadataReader(metadata.Pointer, metadata.Length, MetadataReaderOptions.None);
        }

        internal static void EmitCorLibWithAssemblyReferences(
            Compilation comp,
            string pdbPath,
            Func<CommonPEModuleBuilder, EmitOptions, CommonPEModuleBuilder> getModuleBuilder,
            out ImmutableArray<byte> peBytes,
            out ImmutableArray<byte> pdbBytes)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var emitOptions = EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0").WithDebugInformationFormat(DebugInformationFormat.PortablePdb);
            var moduleBuilder = comp.CheckOptionsAndCreateModuleBuilder(
                diagnostics,
                null,
                emitOptions,
                null,
                null,
                null,
                null,
                default(CancellationToken));

            // Wrap the module builder in a module builder that
            // reports the "System.Object" type as having no base type.
            moduleBuilder = getModuleBuilder(moduleBuilder, emitOptions);
            bool result = comp.Compile(
                moduleBuilder,
                emittingPdb: pdbPath != null,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: default(CancellationToken));

            using (var peStream = new MemoryStream())
            {
                using (var pdbStream = new MemoryStream())
                {
                    PeWriter.WritePeToStream(
                        new EmitContext(moduleBuilder, null, diagnostics, metadataOnly: false, includePrivateMembers: true),
                        comp.MessageProvider,
                        () => peStream,
                        () => pdbStream,
                        null, null,
                        metadataOnly: true,
                        isDeterministic: false,
                        emitTestCoverageData: false,
                        privateKeyOpt: null,
                        cancellationToken: default(CancellationToken));

                    peBytes = peStream.ToImmutable();
                    pdbBytes = pdbStream.ToImmutable();
                }
            }

            diagnostics.Verify();
            diagnostics.Free();
        }
    }
}
