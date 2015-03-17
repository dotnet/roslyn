// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.SymReaderInterop;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EvaluationContext : EvaluationContextBase
    {
        private const string TypeName = "<>x";
        private const string MethodName = "<>m0";
        internal const bool IsLocalScopeEndInclusive = false;

        internal readonly ImmutableArray<MetadataBlock> MetadataBlocks;
        internal readonly MethodContextReuseConstraints? MethodContextReuseConstraints;
        internal readonly CSharpCompilation Compilation;

        private readonly MetadataDecoder _metadataDecoder;
        private readonly MethodSymbol _currentFrame;
        private readonly ImmutableArray<LocalSymbol> _locals;
        private readonly ImmutableSortedSet<int> _inScopeHoistedLocalIndices;
        private readonly MethodDebugInfo _methodDebugInfo;

        private EvaluationContext(
            ImmutableArray<MetadataBlock> metadataBlocks,
            MethodContextReuseConstraints? methodContextReuseConstraints,
            CSharpCompilation compilation,
            MetadataDecoder metadataDecoder,
            MethodSymbol currentFrame,
            ImmutableArray<LocalSymbol> locals,
            ImmutableSortedSet<int> inScopeHoistedLocalIndices,
            MethodDebugInfo methodDebugInfo)
        {
            Debug.Assert(inScopeHoistedLocalIndices != null);

            this.MetadataBlocks = metadataBlocks;
            this.MethodContextReuseConstraints = methodContextReuseConstraints;
            this.Compilation = compilation;
            _metadataDecoder = metadataDecoder;
            _currentFrame = currentFrame;
            _locals = locals;
            _inScopeHoistedLocalIndices = inScopeHoistedLocalIndices;
            _methodDebugInfo = methodDebugInfo;
        }

        /// <summary>
        /// Create a context for evaluating expressions at a type scope.
        /// </summary>
        /// <param name="previous">Previous context, if any, for possible re-use.</param>
        /// <param name="metadataBlocks">Module metadata</param>
        /// <param name="moduleVersionId">Module containing type</param>
        /// <param name="typeToken">Type metadata token</param>
        /// <returns>Evaluation context</returns>
        /// <remarks>
        /// No locals since locals are associated with methods, not types.
        /// </remarks>
        internal static EvaluationContext CreateTypeContext(
            CSharpMetadataContext previous,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            int typeToken)
        {
            Debug.Assert(MetadataTokens.Handle(typeToken).Kind == HandleKind.TypeDefinition);

            // Re-use the previous compilation if possible.
            var compilation = metadataBlocks.HaveNotChanged(previous) ?
                previous.Compilation :
                metadataBlocks.ToCompilation();

            MetadataDecoder metadataDecoder;
            var currentType = compilation.GetType(moduleVersionId, typeToken, out metadataDecoder);
            Debug.Assert((object)currentType != null);
            Debug.Assert(metadataDecoder != null);
            var currentFrame = new SynthesizedContextMethodSymbol(currentType);
            return new EvaluationContext(
                metadataBlocks,
                null,
                compilation,
                metadataDecoder,
                currentFrame,
                default(ImmutableArray<LocalSymbol>),
                ImmutableSortedSet<int>.Empty,
                default(MethodDebugInfo));
        }

        /// <summary>
        /// Create a context for evaluating expressions within a method scope.
        /// </summary>
        /// <param name="previous">Previous context, if any, for possible re-use.</param>
        /// <param name="metadataBlocks">Module metadata</param>
        /// <param name="symReader"><see cref="ISymUnmanagedReader"/> for PDB associated with <paramref name="moduleVersionId"/></param>
        /// <param name="moduleVersionId">Module containing method</param>
        /// <param name="methodToken">Method metadata token</param>
        /// <param name="methodVersion">Method version.</param>
        /// <param name="ilOffset">IL offset of instruction pointer in method</param>
        /// <param name="localSignatureToken">Method local signature token</param>
        /// <returns>Evaluation context</returns>
        internal static EvaluationContext CreateMethodContext(
            CSharpMetadataContext previous,
            ImmutableArray<MetadataBlock> metadataBlocks,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            int ilOffset,
            int localSignatureToken)
        {
            Debug.Assert(MetadataTokens.Handle(methodToken).Kind == HandleKind.MethodDefinition);

            // Re-use the previous compilation if possible.
            CSharpCompilation compilation;
            if (metadataBlocks.HaveNotChanged(previous))
            {
                // Re-use entire context if method scope has not changed.
                var previousContext = previous.EvaluationContext;
                if (previousContext != null &&
                    previousContext.MethodContextReuseConstraints.HasValue &&
                    previousContext.MethodContextReuseConstraints.GetValueOrDefault().AreSatisfied(methodToken, methodVersion, ilOffset))
                {
                    return previousContext;
                }
                compilation = previous.Compilation;
            }
            else
            {
                compilation = metadataBlocks.ToCompilation();
            }

            var typedSymReader = (ISymUnmanagedReader)symReader;
            var allScopes = ArrayBuilder<ISymUnmanagedScope>.GetInstance();
            var containingScopes = ArrayBuilder<ISymUnmanagedScope>.GetInstance();
            typedSymReader.GetScopes(methodToken, methodVersion, ilOffset, IsLocalScopeEndInclusive, allScopes, containingScopes);
            var methodContextReuseConstraints = allScopes.GetReuseConstraints(methodToken, methodVersion, ilOffset, IsLocalScopeEndInclusive);
            allScopes.Free();

            var localNames = containingScopes.GetLocalNames();

            var inScopeHoistedLocalIndices = ImmutableSortedSet<int>.Empty;
            var methodDebugInfo = default(MethodDebugInfo);

            if (typedSymReader != null)
            {
                try
                {
                    // TODO (https://github.com/dotnet/roslyn/issues/702): switch on the type of typedSymReader and call the appropriate helper.
                    methodDebugInfo = typedSymReader.GetMethodDebugInfo(methodToken, methodVersion, localNames.FirstOrDefault());
                    inScopeHoistedLocalIndices = methodDebugInfo.GetInScopeHoistedLocalIndices(ilOffset, ref methodContextReuseConstraints);
                }
                catch (InvalidOperationException)
                {
                    // bad CDI, ignore
                }
            }

            var methodHandle = (MethodDefinitionHandle)MetadataTokens.Handle(methodToken);
            var currentFrame = compilation.GetMethod(moduleVersionId, methodHandle);
            Debug.Assert((object)currentFrame != null);
            var metadataDecoder = new MetadataDecoder((PEModuleSymbol)currentFrame.ContainingModule, currentFrame);
            var localInfo = metadataDecoder.GetLocalInfo(localSignatureToken);
            var localBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            var sourceAssembly = compilation.SourceAssembly;
            GetLocals(localBuilder, currentFrame, localNames, localInfo, methodDebugInfo.DynamicLocalMap, sourceAssembly);
            GetConstants(localBuilder, currentFrame, containingScopes.GetConstantSignatures(), metadataDecoder, methodDebugInfo.DynamicLocalConstantMap, sourceAssembly);
            containingScopes.Free();

            var locals = localBuilder.ToImmutableAndFree();

            return new EvaluationContext(
                metadataBlocks,
                methodContextReuseConstraints,
                compilation,
                metadataDecoder,
                currentFrame,
                locals,
                inScopeHoistedLocalIndices,
                methodDebugInfo);
        }

        internal CompilationContext CreateCompilationContext(CSharpSyntaxNode syntax)
        {
            return new CompilationContext(
                this.Compilation,
                _metadataDecoder,
                _currentFrame,
                _locals,
                _inScopeHoistedLocalIndices,
                _methodDebugInfo,
                syntax);
        }

        internal override CompileResult CompileExpression(
            InspectionContext inspectionContext,
            string expr,
            DkmEvaluationFlags compilationFlags,
            DiagnosticFormatter formatter,
            out ResultProperties resultProperties,
            out string error,
            out ImmutableArray<AssemblyIdentity> missingAssemblyIdentities,
            System.Globalization.CultureInfo preferredUICulture,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData)
        {
            resultProperties = default(ResultProperties);
            var diagnostics = DiagnosticBag.GetInstance();
            try
            {
                ReadOnlyCollection<string> formatSpecifiers;
                var syntax = Parse(expr, (compilationFlags & DkmEvaluationFlags.TreatAsExpression) != 0, diagnostics, out formatSpecifiers);
                if (syntax == null)
                {
                    error = GetErrorMessageAndMissingAssemblyIdentities(diagnostics, formatter, preferredUICulture, out missingAssemblyIdentities);
                    return default(CompileResult);
                }

                var context = this.CreateCompilationContext(syntax);
                ResultProperties properties;
                var moduleBuilder = context.CompileExpression(inspectionContext, TypeName, MethodName, testData, diagnostics, out properties);
                if (moduleBuilder == null)
                {
                    error = GetErrorMessageAndMissingAssemblyIdentities(diagnostics, formatter, preferredUICulture, out missingAssemblyIdentities);
                    return default(CompileResult);
                }

                using (var stream = new MemoryStream())
                {
                    Cci.PeWriter.WritePeToStream(
                        new EmitContext((Cci.IModule)moduleBuilder, null, diagnostics),
                        context.MessageProvider,
                        () => stream,
                        nativePdbWriterOpt: null,
                        allowMissingMethodBodies: false,
                        deterministic: false,
                        cancellationToken: default(CancellationToken));

                    if (diagnostics.HasAnyErrors())
                    {
                        error = GetErrorMessageAndMissingAssemblyIdentities(diagnostics, formatter, preferredUICulture, out missingAssemblyIdentities);
                        return default(CompileResult);
                    }

                    resultProperties = properties;
                    error = null;
                    missingAssemblyIdentities = ImmutableArray<AssemblyIdentity>.Empty;
                    return new CompileResult(
                        stream.ToArray(),
                        typeName: TypeName,
                        methodName: MethodName,
                        formatSpecifiers: formatSpecifiers);
                }
            }
            finally
            {
                diagnostics.Free();
            }
        }

        private static CSharpSyntaxNode Parse(
            string expr,
            bool treatAsExpression,
            DiagnosticBag diagnostics,
            out ReadOnlyCollection<string> formatSpecifiers)
        {
            if (treatAsExpression)
            {
                return expr.ParseExpression(diagnostics, allowFormatSpecifiers: true, formatSpecifiers: out formatSpecifiers);
            }
            else
            {
                // Try to parse as an expression. If that fails, parse as a statement.
                var exprDiagnostics = DiagnosticBag.GetInstance();
                ReadOnlyCollection<string> exprFormatSpecifiers;
                CSharpSyntaxNode syntax = expr.ParseExpression(exprDiagnostics, allowFormatSpecifiers: true, formatSpecifiers: out exprFormatSpecifiers);
                Debug.Assert((syntax == null) || !exprDiagnostics.HasAnyErrors());
                exprDiagnostics.Free();
                if (syntax != null)
                {
                    Debug.Assert(!diagnostics.HasAnyErrors());
                    formatSpecifiers = exprFormatSpecifiers;
                    return syntax;
                }
                formatSpecifiers = null;
                syntax = expr.ParseStatement(diagnostics);
                if ((syntax != null) && (syntax.Kind() != SyntaxKind.LocalDeclarationStatement))
                {
                    diagnostics.Add(ErrorCode.ERR_ExpressionOrDeclarationExpected, Location.None);
                    return null;
                }
                return syntax;
            }
        }

        internal override CompileResult CompileAssignment(
            InspectionContext inspectionContext,
            string target,
            string expr,
            DiagnosticFormatter formatter,
            out ResultProperties resultProperties,
            out string error,
            out ImmutableArray<AssemblyIdentity> missingAssemblyIdentities,
            System.Globalization.CultureInfo preferredUICulture,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            try
            {
                var assignment = target.ParseAssignment(expr, diagnostics);
                if (assignment == null)
                {
                    error = GetErrorMessageAndMissingAssemblyIdentities(diagnostics, formatter, preferredUICulture, out missingAssemblyIdentities);
                    resultProperties = default(ResultProperties);
                    return default(CompileResult);
                }

                var context = this.CreateCompilationContext(assignment);
                ResultProperties properties;
                var moduleBuilder = context.CompileAssignment(inspectionContext, TypeName, MethodName, testData, diagnostics, out properties);
                if (moduleBuilder == null)
                {
                    error = GetErrorMessageAndMissingAssemblyIdentities(diagnostics, formatter, preferredUICulture, out missingAssemblyIdentities);
                    resultProperties = default(ResultProperties);
                    return default(CompileResult);
                }

                using (var stream = new MemoryStream())
                {
                    Cci.PeWriter.WritePeToStream(
                        new EmitContext((Cci.IModule)moduleBuilder, null, diagnostics),
                        context.MessageProvider,
                        () => stream,
                        nativePdbWriterOpt: null,
                        allowMissingMethodBodies: false,
                        deterministic: false,
                        cancellationToken: default(CancellationToken));

                    if (diagnostics.HasAnyErrors())
                    {
                        error = GetErrorMessageAndMissingAssemblyIdentities(diagnostics, formatter, preferredUICulture, out missingAssemblyIdentities);
                        resultProperties = default(ResultProperties);
                        return default(CompileResult);
                    }

                    resultProperties = properties;
                    error = null;
                    missingAssemblyIdentities = ImmutableArray<AssemblyIdentity>.Empty;
                    return new CompileResult(
                        stream.ToArray(),
                        typeName: TypeName,
                        methodName: MethodName,
                        formatSpecifiers: null);
                }
            }
            finally
            {
                diagnostics.Free();
            }
        }

        private static readonly ReadOnlyCollection<byte> s_emptyBytes = new ReadOnlyCollection<byte>(new byte[0]);

        internal override ReadOnlyCollection<byte> CompileGetLocals(
            ArrayBuilder<LocalAndMethod> locals,
            bool argumentsOnly,
            out string typeName,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData = null)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var context = this.CreateCompilationContext(null);
            var moduleBuilder = context.CompileGetLocals(TypeName, locals, argumentsOnly, testData, diagnostics);
            ReadOnlyCollection<byte> assembly = null;

            if ((moduleBuilder != null) && (locals.Count > 0))
            {
                using (var stream = new MemoryStream())
                {
                    Cci.PeWriter.WritePeToStream(
                        new EmitContext((Cci.IModule)moduleBuilder, null, diagnostics),
                        context.MessageProvider,
                        () => stream,
                        nativePdbWriterOpt: null,
                        allowMissingMethodBodies: false,
                        deterministic: false,
                        cancellationToken: default(CancellationToken));

                    if (!diagnostics.HasAnyErrors())
                    {
                        assembly = new ReadOnlyCollection<byte>(stream.ToArray());
                    }
                }
            }

            diagnostics.Free();

            if (assembly == null)
            {
                locals.Clear();
                assembly = s_emptyBytes;
            }

            typeName = TypeName;
            return assembly;
        }

        /// <summary>
        /// Returns symbols for the locals emitted in the original method,
        /// based on the local signatures from the IL and the names and
        /// slots from the PDB. The actual locals are needed to ensure the
        /// local slots in the generated method match the original.
        /// </summary>
        private static void GetLocals(
            ArrayBuilder<LocalSymbol> builder,
            MethodSymbol method,
            ImmutableArray<string> names,
            ImmutableArray<LocalInfo<TypeSymbol>> localInfo,
            ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap,
            SourceAssemblySymbol containingAssembly)
        {
            if (localInfo.Length == 0)
            {
                // When debugging a .dmp without a heap, localInfo will be empty although
                // names may be non-empty if there is a PDB. Since there's no type info, the
                // locals are dropped. Note this means the local signature of any generated
                // method will not match the original signature, so new locals will overlap
                // original locals. That is ok since there is no live process for the debugger
                // to update (any modified values exist in the debugger only).
                return;
            }

            Debug.Assert(localInfo.Length >= names.Length);

            for (int i = 0; i < localInfo.Length; i++)
            {
                var name = (i < names.Length) ? names[i] : null;
                var info = localInfo[i];
                var isPinned = info.IsPinned;

                LocalDeclarationKind kind;
                RefKind refKind;
                TypeSymbol type;
                if (info.IsByRef && isPinned)
                {
                    kind = LocalDeclarationKind.FixedVariable;
                    refKind = RefKind.None;
                    type = new PointerTypeSymbol(info.Type);
                }
                else
                {
                    kind = LocalDeclarationKind.RegularVariable;
                    refKind = info.IsByRef ? RefKind.Ref : RefKind.None;
                    type = info.Type;
                }

                ImmutableArray<bool> dynamicFlags;
                if (dynamicLocalMap != null && dynamicLocalMap.TryGetValue(i, out dynamicFlags))
                {
                    type = DynamicTypeDecoder.TransformTypeWithoutCustomModifierFlags(
                        type,
                        containingAssembly,
                        refKind,
                        dynamicFlags);
                }

                // Custom modifiers can be dropped since binding ignores custom
                // modifiers from locals and since we only need to preserve
                // the type of the original local in the generated method.
                builder.Add(new EELocalSymbol(method, EELocalSymbol.NoLocations, name, i, kind, type, refKind, isPinned, isCompilerGenerated: false, canScheduleToStack: false));
            }
        }

        private static void GetConstants(
            ArrayBuilder<LocalSymbol> builder,
            MethodSymbol method,
            ImmutableArray<NamedLocalConstant> constants,
            MetadataDecoder metadataDecoder,
            ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMap,
            SourceAssemblySymbol containingAssembly)
        {
            foreach (var constant in constants)
            {
                var info = metadataDecoder.GetLocalInfo(constant.Signature);
                Debug.Assert(!info.IsByRef);
                Debug.Assert(!info.IsPinned);
                var type = info.Type;

                ImmutableArray<bool> dynamicFlags;
                if (dynamicLocalConstantMap != null && dynamicLocalConstantMap.TryGetValue(constant.Name, out dynamicFlags))
                {
                    type = DynamicTypeDecoder.TransformTypeWithoutCustomModifierFlags(
                        type,
                        containingAssembly,
                        RefKind.None,
                        dynamicFlags);
                }

                var constantValue = ReinterpretConstantValue(constant.Value, type.SpecialType);
                builder.Add(new EELocalConstantSymbol(method, constant.Name, type, constantValue));
            }
        }

        internal override ImmutableArray<AssemblyIdentity> GetMissingAssemblyIdentities(Diagnostic diagnostic)
        {
            return GetMissingAssemblyIdentitiesHelper((ErrorCode)diagnostic.Code, diagnostic.Arguments);
        }

        /// <remarks>
        /// Internal for testing.
        /// </remarks>
        internal static ImmutableArray<AssemblyIdentity> GetMissingAssemblyIdentitiesHelper(ErrorCode code, IReadOnlyList<object> arguments)
        {
            switch (code)
            {
                case ErrorCode.ERR_NoTypeDef:
                case ErrorCode.ERR_GlobalSingleTypeNameNotFoundFwd:
                case ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd:
                case ErrorCode.ERR_SingleTypeNameNotFoundFwd:
                case ErrorCode.ERR_NameNotInContextPossibleMissingReference: // Probably can't happen.
                    foreach (var argument in arguments)
                    {
                        var identity = (argument as AssemblyIdentity) ?? (argument as AssemblySymbol)?.Identity;
                        if (identity != null && !identity.Equals(MissingCorLibrarySymbol.Instance.Identity))
                        {
                            return ImmutableArray.Create(identity);
                        }
                    }
                    break;
                case ErrorCode.ERR_DottedTypeNameNotFoundInNS:
                    if (arguments.Count == 2)
                    {
                        var namespaceName = arguments[0] as string;
                        var containingNamespace = arguments[1] as NamespaceSymbol;
                        if (namespaceName != null && (object)containingNamespace != null &&
                            containingNamespace.ConstituentNamespaces.Any(n => n.ContainingAssembly.Identity.IsWindowsAssemblyIdentity()))
                        {
                            // This is just a heuristic, but it has the advantage of being portable, particularly 
                            // across different versions of (desktop) windows.
                            var identity = new AssemblyIdentity($"{containingNamespace.ToDisplayString()}.{namespaceName}", contentType: System.Reflection.AssemblyContentType.WindowsRuntime);
                            return ImmutableArray.Create(identity);
                        }
                    }
                    break;
                case ErrorCode.ERR_DynamicAttributeMissing:
                case ErrorCode.ERR_DynamicRequiredTypesMissing:
                // MSDN says these might come from System.Dynamic.Runtime
                case ErrorCode.ERR_QueryNoProviderStandard:
                case ErrorCode.ERR_ExtensionAttrNotFound: // Probably can't happen.
                    return ImmutableArray.Create(SystemCoreIdentity);
                case ErrorCode.ERR_BadAwaitArg_NeedSystem:
                    Debug.Assert(false, "Roslyn no longer produces ERR_BadAwaitArg_NeedSystem");
                    break;
            }

            return default(ImmutableArray<AssemblyIdentity>);
        }
    }
}
