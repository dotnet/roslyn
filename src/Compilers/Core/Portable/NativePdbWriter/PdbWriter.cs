// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.Cci
{
    internal sealed class PdbWriter : IDisposable
    {
        internal const uint Age = 1;

        private readonly HashAlgorithmName _hashAlgorithmNameOpt;
        private readonly string _fileName;
        private readonly Func<ISymWriterMetadataProvider, SymUnmanagedWriter> _symWriterFactory;
        private readonly Dictionary<DebugSourceDocument, int> _documentIndex;
        private MetadataWriter _metadataWriter;
        private SymUnmanagedWriter _symWriter;
        private SymUnmanagedSequencePointsWriter _sequencePointsWriter;

        // { INamespace or ITypeReference -> qualified name }
        private readonly Dictionary<object, string> _qualifiedNameCache;

        // in support of determinism
        private bool IsDeterministic { get => _hashAlgorithmNameOpt.Name != null; }

        public PdbWriter(string fileName, Func<ISymWriterMetadataProvider, SymUnmanagedWriter> symWriterFactory, HashAlgorithmName hashAlgorithmNameOpt)
        {
            _fileName = fileName;
            _symWriterFactory = symWriterFactory;
            _hashAlgorithmNameOpt = hashAlgorithmNameOpt;
            _documentIndex = new Dictionary<DebugSourceDocument, int>();
            _qualifiedNameCache = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
        }

        public void WriteTo(Stream stream)
        {
            _symWriter.WriteTo(stream);
        }

        public void Dispose()
        {
            _symWriter?.Dispose();
        }

        private CommonPEModuleBuilder Module => Context.Module;
        private EmitContext Context => _metadataWriter.Context;

        public void SerializeDebugInfo(IMethodBody methodBody, StandaloneSignatureHandle localSignatureHandleOpt, CustomDebugInfoWriter customDebugInfoWriter)
        {
            Debug.Assert(_metadataWriter != null);
            var methodHandle = (MethodDefinitionHandle)_metadataWriter.GetMethodHandle(methodBody.MethodDefinition);

            // A state machine kickoff method doesn't have sequence points as it only contains generated code.
            // We could avoid emitting debug info for it if the corresponding MoveNext method had no sequence points,
            // but there is no real need for such optimization.
            //
            // Special case a hidden entry point (#line hidden applied) that would otherwise have no debug info.
            // This is to accommodate for a requirement of Windows PDB writer that the entry point method must have some debug information.
            bool isKickoffMethod = methodBody.StateMachineTypeName != null;
            bool emitAllDebugInfo = isKickoffMethod || !methodBody.SequencePoints.IsEmpty ||
                methodBody.MethodDefinition == (Context.Module.DebugEntryPoint ?? Context.Module.PEEntryPoint);

            var compilationOptions = Context.Module.CommonCompilation.Options;

            // We need to avoid emitting CDI DynamicLocals = 5 and EditAndContinueLocalSlotMap = 6 for files processed by WinMDExp until
            // bug #1067635 is fixed and available in SDK.
            bool suppressNewCustomDebugInfo = compilationOptions.OutputKind == OutputKind.WindowsRuntimeMetadata;

            bool emitDynamicAndTupleInfo = emitAllDebugInfo && !suppressNewCustomDebugInfo;

            // Emit EnC info for all methods even if they do not have sequence points.
            // The information facilitates reusing lambdas and closures. The reuse is important for runtimes that can't add new members (e.g. Mono).
            bool emitEncInfo = compilationOptions.EnableEditAndContinue && _metadataWriter.IsFullMetadata && !suppressNewCustomDebugInfo;

            byte[] blob = customDebugInfoWriter.SerializeMethodDebugInfo(Context, methodBody, methodHandle, emitStateMachineInfo: emitAllDebugInfo, emitEncInfo, emitDynamicAndTupleInfo, out bool emitExternNamespaces);
            Debug.Assert(emitAllDebugInfo || !emitExternNamespaces);

            if (!emitAllDebugInfo && blob.Length == 0)
            {
                return;
            }

            int methodToken = MetadataTokens.GetToken(methodHandle);
            OpenMethod(methodToken);

            if (emitAllDebugInfo)
            {
                var localScopes = methodBody.LocalScopes;

                // Define locals, constants and namespaces in the outermost local scope (opened in OpenMethod):
                if (localScopes.Length > 0)
                {
                    DefineScopeLocals(localScopes[0], localSignatureHandleOpt);
                }

                if (!isKickoffMethod && methodBody.ImportScope != null)
                {
                    if (customDebugInfoWriter.ShouldForwardNamespaceScopes(Context, methodBody, methodHandle, out IMethodDefinition forwardToMethod))
                    {
                        if (forwardToMethod != null)
                        {
                            UsingNamespace("@" + MetadataTokens.GetToken(_metadataWriter.GetMethodHandle(forwardToMethod)), methodBody.MethodDefinition);
                        }
                        // otherwise, the forwarding is done via custom debug info
                    }
                    else
                    {
                        DefineNamespaceScopes(methodBody);
                    }
                }

                DefineLocalScopes(localScopes, localSignatureHandleOpt);
                EmitSequencePoints(methodBody.SequencePoints);

                if (methodBody.MoveNextBodyInfo is AsyncMoveNextBodyDebugInfo asyncMoveNextInfo)
                {
                    _symWriter.SetAsyncInfo(
                        methodToken,
                        MetadataTokens.GetToken(_metadataWriter.GetMethodHandle(asyncMoveNextInfo.KickoffMethod)),
                        asyncMoveNextInfo.CatchHandlerOffset,
                        asyncMoveNextInfo.YieldOffsets.AsSpan(),
                        asyncMoveNextInfo.ResumeOffsets.AsSpan());
                }

                if (emitExternNamespaces)
                {
                    DefineAssemblyReferenceAliases();
                }
            }

            if (blob.Length > 0)
            {
                const int limit = 0x10_000;
                if (blob.Length > limit)
                {
                    throw new SymUnmanagedWriterException(string.Format(
                        CodeAnalysisResources.SymWriterMetadataOverLimit,
                        methodBody.MethodDefinition,
                        blob.Length,
                        limit));
                }

                _symWriter.DefineCustomMetadata(blob);
            }

            CloseMethod(methodBody.IL.Length);
        }

        private void DefineNamespaceScopes(IMethodBody methodBody)
        {
            var module = Module;
            bool isVisualBasic = module.GenerateVisualBasicStylePdb;

            IMethodDefinition method = methodBody.MethodDefinition;

            var namespaceScopes = methodBody.ImportScope;

            PooledHashSet<string> lazyDeclaredExternAliases = null;
            if (!isVisualBasic)
            {
                for (var scope = namespaceScopes; scope != null; scope = scope.Parent)
                {
                    foreach (var import in scope.GetUsedNamespaces(Context))
                    {
                        if (import.TargetNamespaceOpt == null && import.TargetTypeOpt == null)
                        {
                            Debug.Assert(import.AliasOpt != null);
                            Debug.Assert(import.TargetAssemblyOpt == null);

                            if (lazyDeclaredExternAliases == null)
                            {
                                lazyDeclaredExternAliases = PooledHashSet<string>.GetInstance();
                            }

                            lazyDeclaredExternAliases.Add(import.AliasOpt);
                        }
                    }
                }
            }

            // file and namespace level
            for (IImportScope scope = namespaceScopes; scope != null; scope = scope.Parent)
            {
                foreach (UsedNamespaceOrType import in scope.GetUsedNamespaces(Context))
                {
                    var importString = TryEncodeImport(import, lazyDeclaredExternAliases, isProjectLevel: false);
                    if (importString != null)
                    {
                        UsingNamespace(importString, method);
                    }
                }
            }

            lazyDeclaredExternAliases?.Free();

            // project level
            if (isVisualBasic)
            {
                string defaultNamespace = module.DefaultNamespace;

                if (!string.IsNullOrEmpty(defaultNamespace))
                {
                    // VB marks the default/root namespace with an asterisk
                    UsingNamespace("*" + defaultNamespace, module);
                }

                foreach (string assemblyName in module.LinkedAssembliesDebugInfo)
                {
                    UsingNamespace("&" + assemblyName, module);
                }

                foreach (UsedNamespaceOrType import in module.GetImports())
                {
                    var importString = TryEncodeImport(import, null, isProjectLevel: true);
                    if (importString != null)
                    {
                        UsingNamespace(importString, method);
                    }
                }

                // VB current namespace -- VB appends the namespace of the container without prefixes
                UsingNamespace(GetOrCreateSerializedNamespaceName(method.ContainingNamespace), method);
            }
        }

        private void DefineAssemblyReferenceAliases()
        {
            foreach (AssemblyReferenceAlias alias in Module.GetAssemblyReferenceAliases(Context))
            {
                UsingNamespace("Z" + alias.Name + " " + alias.Assembly.Identity.GetDisplayName(), Module);
            }
        }

        private string TryEncodeImport(UsedNamespaceOrType import, HashSet<string> declaredExternAliasesOpt, bool isProjectLevel)
        {
            // NOTE: Dev12 has related cases "I" and "O" in EMITTER::ComputeDebugNamespace,
            // but they were probably implementation details that do not affect Roslyn.

            if (Module.GenerateVisualBasicStylePdb)
            {
                // VB doesn't support extern aliases
                Debug.Assert(import.TargetAssemblyOpt == null);
                Debug.Assert(declaredExternAliasesOpt == null);

                if (import.TargetTypeOpt != null)
                {
                    Debug.Assert(import.TargetNamespaceOpt == null);
                    Debug.Assert(import.TargetAssemblyOpt == null);

                    // Native compiler doesn't write imports with generic types to PDB.
                    if (import.TargetTypeOpt.IsTypeSpecification())
                    {
                        return null;
                    }

                    string typeName = GetOrCreateSerializedTypeName(import.TargetTypeOpt);

                    if (import.AliasOpt != null)
                    {
                        return (isProjectLevel ? "@PA:" : "@FA:") + import.AliasOpt + "=" + typeName;
                    }
                    else
                    {
                        return (isProjectLevel ? "@PT:" : "@FT:") + typeName;
                    }
                }

                if (import.TargetNamespaceOpt != null)
                {
                    string namespaceName = GetOrCreateSerializedNamespaceName(import.TargetNamespaceOpt);

                    if (import.AliasOpt == null)
                    {
                        return (isProjectLevel ? "@P:" : "@F:") + namespaceName;
                    }
                    else
                    {
                        return (isProjectLevel ? "@PA:" : "@FA:") + import.AliasOpt + "=" + namespaceName;
                    }
                }

                Debug.Assert(import.AliasOpt != null);
                Debug.Assert(import.TargetXmlNamespaceOpt != null);

                return (isProjectLevel ? "@PX:" : "@FX:") + import.AliasOpt + "=" + import.TargetXmlNamespaceOpt;
            }

            Debug.Assert(import.TargetXmlNamespaceOpt == null);

            if (import.TargetTypeOpt != null)
            {
                Debug.Assert(import.TargetNamespaceOpt == null);
                Debug.Assert(import.TargetAssemblyOpt == null);

                string typeName = GetOrCreateSerializedTypeName(import.TargetTypeOpt);

                return (import.AliasOpt != null) ?
                    "A" + import.AliasOpt + " T" + typeName :
                    "T" + typeName;
            }

            if (import.TargetNamespaceOpt != null)
            {
                string namespaceName = GetOrCreateSerializedNamespaceName(import.TargetNamespaceOpt);

                if (import.AliasOpt != null)
                {
                    return (import.TargetAssemblyOpt != null) ?
                        "A" + import.AliasOpt + " E" + namespaceName + " " + GetAssemblyReferenceAlias(import.TargetAssemblyOpt, declaredExternAliasesOpt) :
                        "A" + import.AliasOpt + " U" + namespaceName;
                }
                else
                {
                    return (import.TargetAssemblyOpt != null) ?
                        "E" + namespaceName + " " + GetAssemblyReferenceAlias(import.TargetAssemblyOpt, declaredExternAliasesOpt) :
                        "U" + namespaceName;
                }
            }

            Debug.Assert(import.AliasOpt != null);
            Debug.Assert(import.TargetAssemblyOpt == null);
            return "X" + import.AliasOpt;
        }

        internal string GetOrCreateSerializedNamespaceName(INamespace @namespace)
        {
            string result;
            if (!_qualifiedNameCache.TryGetValue(@namespace, out result))
            {
                result = TypeNameSerializer.BuildQualifiedNamespaceName(@namespace);
                _qualifiedNameCache.Add(@namespace, result);
            }

            return result;
        }

        internal string GetOrCreateSerializedTypeName(ITypeReference typeReference)
        {
            string result;
            if (!_qualifiedNameCache.TryGetValue(typeReference, out result))
            {
                if (Module.GenerateVisualBasicStylePdb)
                {
                    result = SerializeVisualBasicImportTypeReference(typeReference);
                }
                else
                {
                    result = typeReference.GetSerializedTypeName(Context);
                }

                _qualifiedNameCache.Add(typeReference, result);
            }

            return result;
        }

        private string SerializeVisualBasicImportTypeReference(ITypeReference typeReference)
        {
            Debug.Assert(!(typeReference is IArrayTypeReference));
            Debug.Assert(!(typeReference is IPointerTypeReference));
            Debug.Assert(!typeReference.IsTypeSpecification());

            var result = PooledStringBuilder.GetInstance();
            ArrayBuilder<string> nestedNamesReversed;

            INestedTypeReference nestedType = typeReference.AsNestedTypeReference;
            if (nestedType != null)
            {
                nestedNamesReversed = ArrayBuilder<string>.GetInstance();

                while (nestedType != null)
                {
                    nestedNamesReversed.Add(nestedType.Name);
                    typeReference = nestedType.GetContainingType(_metadataWriter.Context);
                    nestedType = typeReference.AsNestedTypeReference;
                }
            }
            else
            {
                nestedNamesReversed = null;
            }

            INamespaceTypeReference namespaceType = typeReference.AsNamespaceTypeReference;
            Debug.Assert(namespaceType != null);

            string namespaceName = namespaceType.NamespaceName;
            if (namespaceName.Length != 0)
            {
                result.Builder.Append(namespaceName);
                result.Builder.Append('.');
            }

            result.Builder.Append(namespaceType.Name);

            if (nestedNamesReversed != null)
            {
                for (int i = nestedNamesReversed.Count - 1; i >= 0; i--)
                {
                    result.Builder.Append('.');
                    result.Builder.Append(nestedNamesReversed[i]);
                }

                nestedNamesReversed.Free();
            }

            return result.ToStringAndFree();
        }

#nullable enable

        private string GetAssemblyReferenceAlias(IAssemblyReference assembly, HashSet<string>? declaredExternAliases)
        {
            var allAliases = _metadataWriter.Context.Module.GetAssemblyReferenceAliases(_metadataWriter.Context);

            if (declaredExternAliases is not null)
            {
                foreach (AssemblyReferenceAlias alias in allAliases)
                {
                    // Multiple aliases may be given to an assembly reference.
                    // We find one that is in scope (was imported via extern alias directive).
                    // If multiple are in scope then use the first one.

                    // NOTE: Dev12 uses the one that appeared in source, whereas we use
                    // the first one that COULD have appeared in source.  (DevDiv #913022)
                    // The reason we're not just using the alias from the syntax is that
                    // it is non-trivial to locate.  In particular, since "." may be used in
                    // place of "::", determining whether the first identifier in the name is
                    // the alias requires binding.  For example, "using A.B;" could refer to
                    // either "A::B" or "global::A.B".

                    if (assembly == alias.Assembly && declaredExternAliases.Contains(alias.Name))
                    {
                        return alias.Name;
                    }
                }
            }

            // no alias defined in scope for given assembly -> must be a 'global' using, use the first defined alias
            foreach (AssemblyReferenceAlias alias in allAliases)
            {
                if (assembly == alias.Assembly)
                {
                    return alias.Name;
                }
            }

            // no alias defined for given assembly -> error in compiler
            throw ExceptionUtilities.Unreachable();
        }

#nullable disable

        private void DefineLocalScopes(ImmutableArray<LocalScope> scopes, StandaloneSignatureHandle localSignatureHandleOpt)
        {
            // VB scope ranges are end-inclusive
            bool endInclusive = this.Module.GenerateVisualBasicStylePdb;

            // The order of OpenScope and CloseScope calls must follow the scope nesting.
            var scopeStack = ArrayBuilder<LocalScope>.GetInstance();

            for (int i = 1; i < scopes.Length; i++)
            {
                var currentScope = scopes[i];

                // Close any scopes that have finished.
                while (scopeStack.Count > 0)
                {
                    LocalScope topScope = scopeStack.Last();
                    if (currentScope.StartOffset < topScope.StartOffset + topScope.Length)
                    {
                        break;
                    }

                    scopeStack.RemoveLast();
                    _symWriter.CloseScope(endInclusive ? topScope.EndOffset - 1 : topScope.EndOffset);
                }

                // Open this scope.
                scopeStack.Add(currentScope);
                _symWriter.OpenScope(currentScope.StartOffset);
                DefineScopeLocals(currentScope, localSignatureHandleOpt);
            }

            // Close remaining scopes.
            for (int i = scopeStack.Count - 1; i >= 0; i--)
            {
                LocalScope scope = scopeStack[i];
                _symWriter.CloseScope(endInclusive ? scope.EndOffset - 1 : scope.EndOffset);
            }

            scopeStack.Free();
        }

        private void DefineScopeLocals(LocalScope currentScope, StandaloneSignatureHandle localSignatureHandleOpt)
        {
            foreach (ILocalDefinition scopeConstant in currentScope.Constants)
            {
                var signatureHandle = _metadataWriter.SerializeLocalConstantStandAloneSignature(scopeConstant);
                if (!_metadataWriter.IsLocalNameTooLong(scopeConstant))
                {
                    _symWriter.DefineLocalConstant(
                        scopeConstant.Name,
                        scopeConstant.CompileTimeValue.Value,
                        MetadataTokens.GetToken(signatureHandle));
                }
            }

            foreach (ILocalDefinition scopeLocal in currentScope.Variables)
            {
                if (!_metadataWriter.IsLocalNameTooLong(scopeLocal))
                {
                    Debug.Assert(scopeLocal.SlotIndex >= 0);

                    _symWriter.DefineLocalVariable(
                        scopeLocal.SlotIndex,
                        scopeLocal.Name,
                        (int)scopeLocal.PdbAttributes,
                        localSignatureHandleOpt.IsNil ? 0 : MetadataTokens.GetToken(localSignatureHandleOpt));
                }
            }
        }

        public void SetMetadataEmitter(MetadataWriter metadataWriter)
        {
            // Do not look for COM registered diasymreader when determinism is needed as it doesn't support it.
            var options =
                (IsDeterministic ? SymUnmanagedWriterCreationOptions.Deterministic : SymUnmanagedWriterCreationOptions.UseComRegistry) |
                SymUnmanagedWriterCreationOptions.UseAlternativeLoadPath;

            var metadataProvider = new SymWriterMetadataProvider(metadataWriter);

            SymUnmanagedWriter symWriter;
            try
            {
                symWriter = (_symWriterFactory != null) ? _symWriterFactory(metadataProvider) : SymUnmanagedWriterFactory.CreateWriter(metadataProvider, options);
            }
            catch (DllNotFoundException e)
            {
                throw new SymUnmanagedWriterException(e.Message);
            }
            catch (SymUnmanagedWriterException e) when (e.InnerException is NotSupportedException)
            {
                var message = IsDeterministic ? CodeAnalysisResources.SymWriterNotDeterministic : CodeAnalysisResources.SymWriterOlderVersionThanRequired;
                throw new SymUnmanagedWriterException(string.Format(message, e.ImplementationModuleName));
            }

            _metadataWriter = metadataWriter;
            _symWriter = symWriter;
            _sequencePointsWriter = new SymUnmanagedSequencePointsWriter(symWriter, capacity: 64);
        }

        public BlobContentId GetContentId()
        {
            BlobContentId contentId;

            if (IsDeterministic)
            {
                // Calculate hash of the stream content.
                // Note: all bits of the signature currently stored in the PDB stream were initialized to 1 by InitializeDeterministic.
                contentId = BlobContentId.FromHash(CryptographicHashProvider.ComputeHash(_hashAlgorithmNameOpt, _symWriter.GetUnderlyingData()));

                _symWriter.UpdateSignature(contentId.Guid, contentId.Stamp, age: 1);
            }
            else
            {
                _symWriter.GetSignature(out Guid guid, out uint stamp, out int age);
                Debug.Assert(age == Age);
                contentId = new BlobContentId(guid, stamp);
            }

            // Once we calculate the content id we shall not write more data to the writer.
            // Note that the underlying stream is accessible for reading even after the writer is disposed.
            _symWriter.Dispose();

            return contentId;
        }

        public void SetEntryPoint(int entryMethodToken)
        {
            _symWriter.SetEntryPoint(entryMethodToken);
        }

        private int GetDocumentIndex(DebugSourceDocument document)
        {
            if (_documentIndex.TryGetValue(document, out int documentIndex))
            {
                return documentIndex;
            }

            return AddDocumentIndex(document);
        }

        private int AddDocumentIndex(DebugSourceDocument document)
        {
            Guid algorithmId;
            ReadOnlySpan<byte> checksum;
            ReadOnlySpan<byte> embeddedSource;

            DebugSourceInfo info = document.GetSourceInfo();
            if (!info.Checksum.IsDefault)
            {
                algorithmId = info.ChecksumAlgorithmId;
                checksum = info.Checksum.AsSpan();
            }
            else
            {
                algorithmId = default;
                checksum = null;
            }

            if (!info.EmbeddedTextBlob.IsDefault)
            {
                embeddedSource = info.EmbeddedTextBlob.AsSpan();
            }
            else
            {
                embeddedSource = null;
            }

            int documentIndex = _symWriter.DefineDocument(
                document.Location,
                document.Language,
                document.LanguageVendor,
                document.DocumentType,
                algorithmId,
                checksum,
                embeddedSource);

            _documentIndex.Add(document, documentIndex);
            return documentIndex;
        }

        private void OpenMethod(int methodToken)
        {
            _symWriter.OpenMethod(methodToken);

            // open outermost scope:
            _symWriter.OpenScope(startOffset: 0);
        }

        private void CloseMethod(int ilLength)
        {
            // close the root scope:
            _symWriter.CloseScope(endOffset: ilLength);

            _symWriter.CloseMethod();
        }

        private void UsingNamespace(string fullName, INamedEntity errorEntity)
        {
            if (!_metadataWriter.IsUsingStringTooLong(fullName, errorEntity))
            {
                _symWriter.UsingNamespace(fullName);
            }
        }

        private void EmitSequencePoints(ImmutableArray<SequencePoint> sequencePoints)
        {
            int lastDocumentIndex = -1;
            DebugSourceDocument lastDocument = null;

            foreach (var sequencePoint in sequencePoints)
            {
                Debug.Assert(sequencePoint.Document != null);

                var document = sequencePoint.Document;

                int documentIndex;
                if (lastDocument == document)
                {
                    documentIndex = lastDocumentIndex;
                }
                else
                {
                    lastDocument = document;
                    documentIndex = lastDocumentIndex = GetDocumentIndex(lastDocument);
                }

                _sequencePointsWriter.Add(
                    documentIndex,
                    sequencePoint.Offset,
                    sequencePoint.StartLine,
                    sequencePoint.StartColumn,
                    sequencePoint.EndLine,
                    sequencePoint.EndColumn);
            }

            _sequencePointsWriter.Flush();
        }

        [Conditional("DEBUG")]
        // Used to catch cases where file2definitions contain nonwritable definitions early
        // If left unfixed, such scenarios will lead to crashes if happen in winmdobj projects
        public void AssertAllDefinitionsHaveTokens(MultiDictionary<DebugSourceDocument, DefinitionWithLocation> file2definitions)
        {
            foreach (var kvp in file2definitions)
            {
                foreach (var definition in kvp.Value)
                {
                    EntityHandle handle = _metadataWriter.GetDefinitionHandle(definition.Definition);
                    Debug.Assert(!handle.IsNil);
                }
            }
        }

        // Note: only used for WinMD
        public void WriteDefinitionLocations(MultiDictionary<DebugSourceDocument, DefinitionWithLocation> file2definitions)
        {
            // Only open and close the map if we have any mapping.
            bool open = false;

            foreach (var kvp in file2definitions)
            {
                foreach (var definition in kvp.Value)
                {
                    if (!open)
                    {
                        _symWriter.OpenTokensToSourceSpansMap();
                        open = true;
                    }

                    int token = MetadataTokens.GetToken(_metadataWriter.GetDefinitionHandle(definition.Definition));
                    Debug.Assert(token != 0);

                    _symWriter.MapTokenToSourceSpan(
                        token,
                        GetDocumentIndex(kvp.Key),
                        definition.StartLine + 1,
                        definition.StartColumn + 1,
                        definition.EndLine + 1,
                        definition.EndColumn + 1);
                }
            }

            if (open)
            {
                _symWriter.CloseTokensToSourceSpansMap();
            }
        }

        public void EmbedSourceLink(Stream stream)
        {
            byte[] bytes;

            try
            {
                bytes = stream.ReadAllBytes();
            }
            catch (Exception e)
            {
                throw new SymUnmanagedWriterException(e.Message, e);
            }

            try
            {
                _symWriter.SetSourceLinkData(bytes);
            }
            catch (SymUnmanagedWriterException e) when (e.InnerException is NotSupportedException)
            {
                throw new SymUnmanagedWriterException(string.Format(CodeAnalysisResources.SymWriterDoesNotSupportSourceLink, e.ImplementationModuleName));
            }
        }

        /// <summary>
        /// Write document entries for all debug documents that do not yet have an entry.
        /// </summary>
        /// <remarks>
        /// This is done after serializing method debug info to ensure that we embed all requested
        /// text even if there are no corresponding sequence points.
        /// </remarks>
        public void WriteRemainingDebugDocuments(IReadOnlyDictionary<string, DebugSourceDocument> documents)
        {
            foreach (var kvp in documents
                .Where(kvp => !_documentIndex.ContainsKey(kvp.Value))
                .OrderBy(kvp => kvp.Key))
            {
                AddDocumentIndex(kvp.Value);
            }
        }

        public void WriteCompilerVersion(string language)
        {
            var compilerAssembly = typeof(Compilation).Assembly;
            var fileVersion = Version.Parse(compilerAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
            var versionString = compilerAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            _symWriter.AddCompilerInfo((ushort)fileVersion.Major, (ushort)fileVersion.Minor, (ushort)fileVersion.Build, (ushort)fileVersion.Revision, $"{language} - {versionString}");
        }
    }
}
