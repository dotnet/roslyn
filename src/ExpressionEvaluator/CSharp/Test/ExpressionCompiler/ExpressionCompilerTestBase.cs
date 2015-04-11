// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;
using Roslyn.Test.PdbUtilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class ExpressionCompilerTestBase : CSharpTestBase, IDisposable
    {
        private readonly ArrayBuilder<IDisposable> _runtimeInstances = ArrayBuilder<IDisposable>.GetInstance();

        public override void Dispose()
        {
            base.Dispose();

            foreach (var instance in _runtimeInstances)
            {
                instance.Dispose();
            }
            _runtimeInstances.Free();
        }

        internal RuntimeInstance CreateRuntimeInstance(
            Compilation compilation,
            bool includeSymbols = true)
        {
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> references;
            compilation.EmitAndGetReferences(out exeBytes, out pdbBytes, out references);
            return CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                references.AddIntrinsicAssembly(),
                exeBytes,
                includeSymbols ? new SymReader(pdbBytes, exeBytes) : null);
        }

        internal RuntimeInstance CreateRuntimeInstance(
            string assemblyName,
            ImmutableArray<MetadataReference> references,
            byte[] exeBytes,
            ISymUnmanagedReader symReader,
            bool includeLocalSignatures = true)
        {
            var exeReference = AssemblyMetadata.CreateFromImage(exeBytes).GetReference(display: assemblyName);
            var modulesBuilder = ArrayBuilder<ModuleInstance>.GetInstance();
            // Create modules for the references
            modulesBuilder.AddRange(references.Select(r => r.ToModuleInstance(fullImage: null, symReader: null, includeLocalSignatures: includeLocalSignatures)));
            // Create a module for the exe.
            modulesBuilder.Add(exeReference.ToModuleInstance(exeBytes, symReader, includeLocalSignatures: includeLocalSignatures));

            var modules = modulesBuilder.ToImmutableAndFree();
            modules.VerifyAllModules();

            var instance = new RuntimeInstance(modules);
            _runtimeInstances.Add(instance);
            return instance;
        }

        internal static void GetContextState(
            RuntimeInstance runtime,
            string methodOrTypeName,
            out ImmutableArray<MetadataBlock> blocks,
            out Guid moduleVersionId,
            out ISymUnmanagedReader symReader,
            out int methodOrTypeToken,
            out int localSignatureToken)
        {
            var moduleInstances = runtime.Modules;
            blocks = moduleInstances.SelectAsArray(m => m.MetadataBlock);

            var compilation = blocks.ToCompilation();

            var methodOrType = GetMethodOrTypeBySignature(compilation, methodOrTypeName);

            var module = (PEModuleSymbol)methodOrType.ContainingModule;
            var id = module.Module.GetModuleVersionIdOrThrow();
            var moduleInstance = moduleInstances.First(m => m.ModuleVersionId == id);

            moduleVersionId = id;
            symReader = (ISymUnmanagedReader)moduleInstance.SymReader;

            Handle methodOrTypeHandle;
            if (methodOrType.Kind == SymbolKind.Method)
            {
                methodOrTypeHandle = ((PEMethodSymbol)methodOrType).Handle;
                localSignatureToken = moduleInstance.GetLocalSignatureToken((MethodDefinitionHandle)methodOrTypeHandle);
            }
            else
            {
                methodOrTypeHandle = ((PENamedTypeSymbol)methodOrType).Handle;
                localSignatureToken = -1;
            }

            MetadataReader reader = null; // null should be ok
            methodOrTypeToken = reader.GetToken(methodOrTypeHandle);
        }

        internal static EvaluationContext CreateMethodContext(
            RuntimeInstance runtime,
            string methodName,
            int atLineNumber = -1)
        {
            ImmutableArray<MetadataBlock> blocks;
            Guid moduleVersionId;
            ISymUnmanagedReader symReader;
            int methodToken;
            int localSignatureToken;
            GetContextState(runtime, methodName, out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);

            int ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader, atLineNumber);

            return EvaluationContext.CreateMethodContext(
                default(CSharpMetadataContext),
                blocks,
                symReader,
                moduleVersionId,
                methodToken: methodToken,
                methodVersion: 1,
                ilOffset: ilOffset,
                localSignatureToken: localSignatureToken);
        }

        internal static EvaluationContext CreateTypeContext(
            RuntimeInstance runtime,
            string typeName)
        {
            ImmutableArray<MetadataBlock> blocks;
            Guid moduleVersionId;
            ISymUnmanagedReader symReader;
            int typeToken;
            int localSignatureToken;
            GetContextState(runtime, typeName, out blocks, out moduleVersionId, out symReader, out typeToken, out localSignatureToken);
            return EvaluationContext.CreateTypeContext(
                default(CSharpMetadataContext),
                blocks,
                moduleVersionId,
                typeToken);
        }

        internal CompilationTestData Evaluate(
            string source,
            OutputKind outputKind,
            string methodName,
            string expr,
            int atLineNumber = -1,
            bool includeSymbols = true)
        {
            ResultProperties resultProperties;
            string error;
            var result = Evaluate(source, outputKind, methodName, expr, out resultProperties, out error, atLineNumber, DefaultInspectionContext.Instance, includeSymbols);
            Assert.Null(error);
            return result;
        }

        internal CompilationTestData Evaluate(
            string source,
            OutputKind outputKind,
            string methodName,
            string expr,
            out ResultProperties resultProperties,
            out string error,
            int atLineNumber = -1,
            InspectionContext inspectionContext = null,
            bool includeSymbols = true)
        {
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: (outputKind == OutputKind.DynamicallyLinkedLibrary) ? TestOptions.DebugDll : TestOptions.DebugExe);

            var runtime = CreateRuntimeInstance(compilation0, includeSymbols);
            var context = CreateMethodContext(runtime, methodName, atLineNumber);
            var testData = new CompilationTestData();
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var result = context.CompileExpression(
                inspectionContext ?? DefaultInspectionContext.Instance,
                expr,
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            return testData;
        }

        /// <summary>
        /// Verify all type parameters from the method
        /// are from that method or containing types.
        /// </summary>
        internal static void VerifyTypeParameters(MethodSymbol method)
        {
            Assert.True(method.IsContainingSymbolOfAllTypeParameters(method.ReturnType));
            AssertEx.All(method.TypeParameters, typeParameter => method.IsContainingSymbolOfAllTypeParameters(typeParameter));
            AssertEx.All(method.TypeArguments, typeArgument => method.IsContainingSymbolOfAllTypeParameters(typeArgument));
            AssertEx.All(method.Parameters, parameter => method.IsContainingSymbolOfAllTypeParameters(parameter.Type));
            VerifyTypeParameters(method.ContainingType);
        }

        internal static void VerifyLocal(
            CompilationTestData testData,
            string typeName,
            LocalAndMethod localAndMethod,
            string expectedMethodName,
            string expectedLocalName,
            DkmClrCompilationResultFlags expectedFlags = DkmClrCompilationResultFlags.None,
            string expectedILOpt = null,
            bool expectedGeneric = false,
            [CallerFilePath]string expectedValueSourcePath = null,
            [CallerLineNumber]int expectedValueSourceLine = 0)
        {
            ExpressionCompilerTestHelpers.VerifyLocal<MethodSymbol>(
                testData,
                typeName,
                localAndMethod,
                expectedMethodName,
                expectedLocalName,
                expectedFlags,
                VerifyTypeParameters,
                expectedILOpt,
                expectedGeneric,
                expectedValueSourcePath,
                expectedValueSourceLine);
        }

        /// <summary>
        /// Verify all type parameters from the type
        /// are from that type or containing types.
        /// </summary>
        internal static void VerifyTypeParameters(NamedTypeSymbol type)
        {
            AssertEx.All(type.TypeParameters, typeParameter => type.IsContainingSymbolOfAllTypeParameters(typeParameter));
            AssertEx.All(type.TypeArguments, typeArgument => type.IsContainingSymbolOfAllTypeParameters(typeArgument));
            var container = type.ContainingType;
            if ((object)container != null)
            {
                VerifyTypeParameters(container);
            }
        }

        internal static Symbol GetMethodOrTypeBySignature(Compilation compilation, string signature)
        {
            string methodOrTypeName = signature;
            string[] parameterTypeNames = null;
            var parameterListStart = methodOrTypeName.IndexOf('(');
            if (parameterListStart > -1)
            {
                parameterTypeNames = methodOrTypeName.Substring(parameterListStart).Trim('(', ')').Split(',');
                methodOrTypeName = methodOrTypeName.Substring(0, parameterListStart);
            }

            var candidates = compilation.GetMembers(methodOrTypeName);
            Assert.Equal(parameterTypeNames == null, candidates.Length == 1);

            Symbol methodOrType = null;
            foreach (var candidate in candidates)
            {
                methodOrType = candidate;
                if ((parameterTypeNames == null) ||
                    parameterTypeNames.SequenceEqual(methodOrType.GetParameters().Select(p => p.Type.Name)))
                {
                    // Found a match.
                    break;
                }
            }
            Assert.False(methodOrType == null, "Could not find method or type with signature '" + signature + "'.");

            return methodOrType;
        }
    }
}
