// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;
using PDB::Roslyn.Test.MetadataUtilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
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

        static internal CompileResult CompileExpression(
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
        static internal CompileResult CompileExpression(
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
                var locals = scope.GetLocals();
                foreach (var local in locals)
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
            this ImmutableArray<byte> assembly,
            string qualifiedName,
            string expectedIL,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
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
                var typeDef = reader.GetTypeDef(parts[0]);
                var methodName = parts[1];
                var methodHandle = reader.GetMethodDefHandle(typeDef, methodName);
                var methodBody = module.GetMethodBodyOrThrow(methodHandle);

                var pooled = PooledStringBuilder.GetInstance();
                var builder = pooled.Builder;
                var writer = new StringWriter(pooled.Builder);
                var visualizer = new MetadataVisualizer(reader, writer);
                visualizer.VisualizeMethodBody(methodBody, methodHandle, emitHeader: false);
                var actualIL = pooled.ToStringAndFree();

                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, escapeQuotes: true, expectedValueSourcePath: expectedValueSourcePath, expectedValueSourceLine: expectedValueSourceLine);
            }
        }

        internal static bool EmitAndGetReferences(
            this Compilation compilation,
            out byte[] exeBytes,
            out byte[] pdbBytes,
            out ImmutableArray<MetadataReference> references)
        {
            using (var pdbStream = new MemoryStream())
            {
                using (var exeStream = new MemoryStream())
                {
                    var result = compilation.Emit(
                        peStream: exeStream,
                        pdbStream: pdbStream,
                        xmlDocumentationStream: null,
                        win32Resources: null,
                        manifestResources: null,
                        options: EmitOptions.Default,
                        debugEntryPoint: null,
                        testData: null,
                        getHostDiagnostics: null,
                        cancellationToken: default(CancellationToken));

                    if (!result.Success)
                    {
                        result.Diagnostics.Verify();
                        exeBytes = null;
                        pdbBytes = null;
                        references = default(ImmutableArray<MetadataReference>);
                        return false;
                    }

                    exeBytes = exeStream.ToArray();
                    pdbBytes = pdbStream.ToArray();
                }
            }

            // Determine the set of references that were actually used
            // and ignore any references that were dropped in emit.
            HashSet<string> referenceNames;
            using (var metadata = ModuleMetadata.CreateFromImage(exeBytes))
            {
                var reader = metadata.MetadataReader;
                referenceNames = new HashSet<string>(reader.AssemblyReferences.Select(h => GetAssemblyReferenceName(reader, h)));
            }

            references = ImmutableArray.CreateRange(compilation.References.Where(r => IsReferenced(r, referenceNames)));
            return true;
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
            var result = scopes.SelectAsArray(s => new Scope(s.GetStartOffset(), s.GetEndOffset(), s.GetLocals().SelectAsArray(l => l.GetName()), isEndInclusive));
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
            var assemblyMetadata = ((PortableExecutableReference)reference).GetMetadata() as AssemblyMetadata;
            if (assemblyMetadata == null)
            {
                // Netmodule. Assume it is referenced.
                return true;
            }
            var name = assemblyMetadata.GetAssembly().Identity.Name;
            return referenceNames.Contains(name);
        }

        /// <summary>
        /// Verify the set of module metadata blocks
        /// contains all blocks referenced by the set.
        /// </summary>
        internal static void VerifyAllModules(this ImmutableArray<ModuleInstance> modules)
        {
            var blocks = modules.SelectAsArray(m => m.MetadataBlock).SelectAsArray(b => ModuleMetadata.CreateFromMetadata(b.Pointer, b.Size));
            var names = new HashSet<string>(blocks.Select(b => b.Name));
            foreach (var block in blocks)
            {
                foreach (var name in block.GetModuleNames())
                {
                    Assert.True(names.Contains(name));
                }
            }
        }

        internal static ModuleMetadata GetModuleMetadata(this MetadataReference reference)
        {
            var metadata = ((MetadataImageReference)reference).GetMetadata();
            var assemblyMetadata = metadata as AssemblyMetadata;
            Assert.True((assemblyMetadata == null) || (assemblyMetadata.GetModules().Length == 1));
            return (assemblyMetadata == null) ? (ModuleMetadata)metadata : assemblyMetadata.GetModules()[0];
        }

        internal static ModuleInstance ToModuleInstance(
            this MetadataReference reference,
            byte[] fullImage,
            object symReader,
            bool includeLocalSignatures = true)
        {
            var moduleMetadata = reference.GetModuleMetadata();
            var moduleId = moduleMetadata.Module.GetModuleVersionIdOrThrow();
            // The Expression Compiler expects metadata only, no headers or IL.
            var metadataBytes = moduleMetadata.Module.PEReaderOpt.GetMetadata().GetContent().ToArray();
            return new ModuleInstance(
                reference,
                moduleMetadata,
                moduleId,
                fullImage,
                metadataBytes,
                symReader,
                includeLocalSignatures && (fullImage != null));
        }

        internal static AssemblyIdentity GetAssemblyIdentity(this MetadataReference reference)
        {
            var moduleMetadata = reference.GetModuleMetadata();
            var reader = moduleMetadata.MetadataReader;
            return reader.ReadAssemblyIdentityOrThrow();
        }

        internal static Guid GetModuleVersionId(this MetadataReference reference)
        {
            var moduleMetadata = reference.GetModuleMetadata();
            var reader = moduleMetadata.MetadataReader;
            return reader.GetModuleVersionIdOrThrow();
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
            where TMethodSymbol : IMethodSymbol
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

        internal static ISymUnmanagedReader ConstructSymReaderWithImports(byte[] exeBytes, string methodName, params string[] importStrings)
        {
            using (var peReader = new PEReader(ImmutableArray.Create(exeBytes)))
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
                        ? sequencePoints.Where(sp => sp.StartLine != SequencePointList.HiddenSequencePointLine).Select(sp => sp.Offset).FirstOrDefault()
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
    }
}
