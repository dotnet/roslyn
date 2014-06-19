// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    //Catch all of the exceptions originating from writing PDBs and 
    //surface them as PDB-writing failure diagnostics to the user. 
    //Unfortunately, an exception originating in a user-implemented
    //Stream derivation will come out of the symbol writer as a COMException
    //missing all of the original exception info.

    internal sealed class PdbWritingException : Exception
    {
        internal PdbWritingException(Exception inner) :
            base(inner.Message, inner)
        {
        }
    }

    internal sealed class PdbWriter : IDisposable
    {
        private static Type lazyCorSymWriterSxSType;

        private readonly ComStreamWrapper stream;
        private readonly string fileName;
        private readonly Func<object> symWriterFactory;
        private PeWriter peWriter;
        private ISymUnmanagedWriter2 symWriter;

        private readonly Dictionary<DebugSourceDocument, ISymUnmanagedDocumentWriter> documentMap = new Dictionary<DebugSourceDocument, ISymUnmanagedDocumentWriter>();

        // sequence point buffers:
        private uint[] sequencePointOffsets;
        private uint[] sequencePointStartLines;
        private uint[] sequencePointStartColumns;
        private uint[] sequencePointEndLines;
        private uint[] sequencePointEndColumns;

        public PdbWriter(string fileName, Stream stream, Func<object> symWriterFactory = null)
        {
            this.stream = new ComStreamWrapper(stream);
            this.fileName = fileName;
            this.symWriterFactory = symWriterFactory;
            CreateSequencePointBuffers(capacity: 64);
        }

        public void Dispose()
        {
            this.Close();
            GC.SuppressFinalize(this);
        }

        ~PdbWriter()
        {
            this.Close();
        }

        private void Close()
        {
            if (this.symWriter != null)
            {
                try
                {
                    this.symWriter.Close();
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }
        }

        public void SerializeDebugInfo(IMethodBody methodBody, uint localSignatureToken, CustomDebugInfoWriter customDebugInfoWriter)
        {
            Debug.Assert(peWriter != null);

            bool isIterator = methodBody.IteratorClassName != null;
            bool emitDebugInfo = isIterator || methodBody.HasAnyLocations;

            if (!emitDebugInfo)
            {
                return;
            }

            uint methodToken = peWriter.GetMethodToken(methodBody.MethodDefinition);

            OpenMethod(methodToken);

            var localScopes = methodBody.LocalScopes;

            // CCI originally didn't have the notion of the default scope that is open
            // when a method is opened. In order to reproduce CSC PDBs, this must be added. Otherwise
            // a seemingly unecessary scope that contains only other scopes is put in the PDB.
            if (localScopes.Length > 0)
            {
                this.DefineScopeLocals(localScopes[0], localSignatureToken);
            }

            // NOTE: This is an attempt to match Dev10's apparent behavior.  For iterator methods (i.e. the method
            // that appears in source, not the synthesized ones), Dev10 only emits the ForwardIterator and IteratorLocal
            // custom debug info (e.g. there will be no information about the usings that were in scope).
            if (!isIterator)
            {
                IMethodDefinition forwardToMethod;
                if (customDebugInfoWriter.ShouldForwardNamespaceScopes(methodBody, methodToken, out forwardToMethod))
                {
                    if (forwardToMethod != null)
                    {
                        string usingString = "@" + peWriter.GetMethodToken(forwardToMethod);
                        Debug.Assert(!peWriter.IsUsingStringTooLong(usingString));
                        UsingNamespace(usingString, methodBody.MethodDefinition.Name);
                    }

                    // otherwise, the forwarding is done via custom debug info
                }
                else
                {
                    this.DefineNamespaceScopes(methodBody);
                }
            }

            DefineLocalScopes(localScopes, localSignatureToken);

            EmitSequencePoints(methodBody.GetSequencePoints());

            AsyncMethodBodyDebugInfo asyncDebugInfo = methodBody.AsyncMethodDebugInfo;
            if (asyncDebugInfo != null)
            {
                SetAsyncInfo(
                    methodToken,
                    peWriter.GetMethodToken(asyncDebugInfo.KickoffMethod),
                    asyncDebugInfo.CatchHandlerOffset,
                    asyncDebugInfo.YieldOffsets,
                    asyncDebugInfo.ResumeOffsets);
            }

            var module = peWriter.Context.Module;

            bool emitExternNamespaces;
            byte[] blob = customDebugInfoWriter.SerializeMethodDebugInfo(module, methodBody, methodToken, out emitExternNamespaces);
            if (blob != null)
            {
                DefineCustomMetadata("MD2", blob);
            }

            if (emitExternNamespaces)
            {
                this.DefineExternAliases(module);
            }

            // TODO: it's not clear why we are closing a scope here with IL length:
            CloseScope((uint)methodBody.IL.Length);

            CloseMethod();
        }

        private void DefineNamespaceScopes(IMethodBody methodBody)
        {
            // TODO: add nopia namespaces 

            // in case that the using namespace is too long, the native API SymWriter::UsingNamespace() would return 
            // an E_OUTOFMEMORY which will be translated to an OutOfMemory exception.
            // To avoid this we're checking the length before calling. However if _MAX_SYM_SIZE ever gets changed in
            // the native API this code gets out of sync. But there is no other alternative. Checking for E_OUTOFMEMORY and
            // continuing does not work because then the native API will work with uninitialized memory which showed issues
            // in x64 builds.
            // Pointers: 
            //   - symwrite.cpp, SymWriter::UsingNamespace(const WCHAR *fullName)
            //        (uninitialized memory by calling "use = m_usings.next()" without setting use->m_name AND
            //         not setting use->m_scope to a default value)
            //   - symwrite.h, SymWriter::AddName
            //        (returning false for too long strings)
            //   - symwrite.cpp, SymWriter::EmitChildren( size_t i )
            //        (accessing m_string with offset of use->m_name (uninitialized) when use->m_scope == for loop index. So
            //         whenever the unitialized value is a low number (0 ..) then uninitialized memory is accessed -> access violation
            //
            // DevDiv Bug 479703: "Possible access violation because of uninitialized data in string pool of SymWriter" (Resolved in RTM)

            IMethodDefinition methodDefinition = methodBody.MethodDefinition;
            foreach (NamespaceScope namespaceScope in methodBody.NamespaceScopes)
            {
                foreach (UsedNamespaceOrType used in namespaceScope.UsedNamespaces)
                {
                    string usingString = used.Encode();
                    if (!peWriter.IsUsingStringTooLong(usingString, methodDefinition))
                    {
                        UsingNamespace(usingString, used.Alias);
                    }
                }
            }
        }

        private void DefineExternAliases(IModule module)
        {
            foreach (ExternNamespace @extern in module.ExternNamespaces)
            {
                string usingString = "Z" + @extern.NamespaceAlias + " " + @extern.AssemblyName;
                if (!peWriter.IsUsingStringTooLong(usingString))
                {
                    UsingNamespace(usingString, @extern.NamespaceAlias);
                }
            }
        }

        private void DefineLocalScopes(ImmutableArray<LocalScope> scopes, uint localSignatureToken)
        {
            // The order of OpenScope and CloseScope calls must follow the scope nesting.
            var scopeStack = ArrayBuilder<LocalScope>.GetInstance();

            for (int i = 1; i < scopes.Length; i++)
            {
                var currentScope = scopes[i];

                // Close any scopes that have finished.
                while (scopeStack.Count > 0)
                {
                    LocalScope topScope = scopeStack.Last();
                    if (currentScope.Offset < topScope.Offset + topScope.Length)
                    {
                        break;
                    }

                    scopeStack.RemoveLast();
                    CloseScope(topScope.Offset + topScope.Length);
                }

                // Open this scope.
                scopeStack.Add(currentScope);
                OpenScope(currentScope.Offset);
                this.DefineScopeLocals(currentScope, localSignatureToken);
            }

            // Close remaining scopes.
            for (int i = scopeStack.Count - 1; i >= 0; i--)
            {
                LocalScope scope = scopeStack[i];
                CloseScope(scope.Offset + scope.Length);
            }

            scopeStack.Free();
        }

        private void DefineScopeLocals(LocalScope currentScope, uint localSignatureToken)
        {
            foreach (ILocalDefinition scopeConstant in currentScope.Constants)
            {
                uint token = peWriter.SerializeLocalConstantSignature(scopeConstant);
                if (!peWriter.IsLocalNameTooLong(scopeConstant))
                {
                    DefineLocalConstant(scopeConstant.Name, scopeConstant.CompileTimeValue.Value, peWriter.GetConstantTypeCode(scopeConstant), token);
                }
            }

            foreach (ILocalDefinition scopeLocal in currentScope.Variables)
            {
                if (!peWriter.IsLocalNameTooLong(scopeLocal))
                {
                    Debug.Assert(scopeLocal.SlotIndex >= 0);
                    DefineLocalVariable((uint)scopeLocal.SlotIndex, scopeLocal.Name, scopeLocal.IsCompilerGenerated, localSignatureToken);
                }
            }
        }

        #region SymWriter calls

        private static Type GetCorSymWriterSxSType()
        {
            if (lazyCorSymWriterSxSType == null)
            {
                // If an exception is thrown we propagate it - we want to report it every time. 
                lazyCorSymWriterSxSType = Marshal.GetTypeFromCLSID(new Guid("0AE2DEB0-F901-478b-BB9F-881EE8066788"));
            }

            return lazyCorSymWriterSxSType;
        }

        public void SetMetadataEmitter(PeWriter peWriter)
        {
            try
            {
                var instance = (ISymUnmanagedWriter2)(symWriterFactory != null ? symWriterFactory() : Activator.CreateInstance(GetCorSymWriterSxSType()));
                instance.Initialize(new PdbMetadataWrapper(peWriter), this.fileName, this.stream, true);

                this.peWriter = peWriter;
                this.symWriter = instance;
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public unsafe PeDebugDirectory GetDebugDirectory()
        {
            ImageDebugDirectory debugDir = new ImageDebugDirectory();
            uint dataCount = 0;

            try
            {
                this.symWriter.GetDebugInfo(ref debugDir, 0, out dataCount, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }

            // See symwrite.cpp - the data don't depend on the content of metadata tables or IL
            // 
            // struct RSDSI                     
            // {
            //     DWORD dwSig;                 // "RSDS"
            //     GUID guidSig;
            //     DWORD age;
            //     char szPDB[0];               // zero-terminated UTF8 file name
            // };
            //
            byte[] data = new byte[dataCount];

            fixed (byte* pb = data)
            {
                try
                {
                    this.symWriter.GetDebugInfo(ref debugDir, dataCount, out dataCount, (IntPtr)pb);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }

            PeDebugDirectory result = new PeDebugDirectory();
            result.AddressOfRawData = (uint)debugDir.AddressOfRawData;
            result.Characteristics = (uint)debugDir.Characteristics;
            result.Data = data;
            result.MajorVersion = (ushort)debugDir.MajorVersion;
            result.MinorVersion = (ushort)debugDir.MinorVersion;
            result.PointerToRawData = (uint)debugDir.PointerToRawData;
            result.SizeOfData = (uint)debugDir.SizeOfData;
            result.TimeDateStamp = (uint)debugDir.TimeDateStamp;
            result.Type = (uint)debugDir.Type;

            return result;
        }

        public void SetEntryPoint(uint entryMethodToken)
        {
            try
            {
                this.symWriter.SetUserEntryPoint(entryMethodToken);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private ISymUnmanagedDocumentWriter GetDocumentWriter(DebugSourceDocument document)
        {
            ISymUnmanagedDocumentWriter writer;
            if (!this.documentMap.TryGetValue(document, out writer))
            {
                Guid language = document.Language;
                Guid vendor = document.LanguageVendor;
                Guid type = document.DocumentType;

                try
                {
                    writer = this.symWriter.DefineDocument(document.Location, ref language, ref vendor, ref type);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }

                this.documentMap.Add(document, writer);

                var checkSum = document.SourceHash;
                if (!checkSum.IsDefault)
                {
                    Guid algoId = document.SourceHashKind;
                    try
                    {
                        writer.SetCheckSum(algoId, (uint)checkSum.Length, checkSum.ToArray());
                    }
                    catch (Exception ex)
                    {
                        throw new PdbWritingException(ex);
                    }
                }
            }

            return writer;
        }

        private void OpenMethod(uint methodToken)
        {
            try
            {
                this.symWriter.OpenMethod(methodToken);
                this.symWriter.OpenScope(0);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void CloseMethod()
        {
            try
            {
                this.symWriter.CloseMethod();
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void OpenScope(uint offset)
        {
            try
            {
                this.symWriter.OpenScope(offset);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void CloseScope(uint offset)
        {
            try
            {
                this.symWriter.CloseScope(offset);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void UsingNamespace(string fullName, string nameForDiagnosticMessage)
        {
            try
            {
                this.symWriter.UsingNamespace(fullName);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void CreateSequencePointBuffers(int capacity)
        {
            this.sequencePointOffsets = new uint[capacity];
            this.sequencePointStartLines = new uint[capacity];
            this.sequencePointStartColumns = new uint[capacity];
            this.sequencePointEndLines = new uint[capacity];
            this.sequencePointEndColumns = new uint[capacity];
        }

        private void ResizeSequencePointBuffers()
        {
            int newCapacity = (sequencePointOffsets.Length + 1) * 2;
            Array.Resize(ref sequencePointOffsets, newCapacity);
            Array.Resize(ref sequencePointStartLines, newCapacity);
            Array.Resize(ref sequencePointStartColumns, newCapacity);
            Array.Resize(ref sequencePointEndLines, newCapacity);
            Array.Resize(ref sequencePointEndColumns, newCapacity);
        }

        private void EmitSequencePoints(ImmutableArray<SequencePoint> sequencePoints)
        {
            DebugSourceDocument document = null;
            ISymUnmanagedDocumentWriter symDocumentWriter = null;

            int i = 0;
            foreach (var sequencePoint in sequencePoints)
            {
                Debug.Assert(sequencePoint.Document != null);

                if (document != sequencePoint.Document)
                {
                    if (i > 0)
                    {
                        WriteSequencePoints(symDocumentWriter, i);
                    }

                    document = sequencePoint.Document;
                    symDocumentWriter = GetDocumentWriter(document);
                    i = 0;
                }

                if (i == sequencePointOffsets.Length)
                {
                    ResizeSequencePointBuffers();
                }

                this.sequencePointOffsets[i] = (uint)sequencePoint.Offset;
                this.sequencePointStartLines[i] = (uint)sequencePoint.StartLine;
                this.sequencePointStartColumns[i] = (uint)sequencePoint.StartColumn;
                this.sequencePointEndLines[i] = (uint)sequencePoint.EndLine;
                this.sequencePointEndColumns[i] = (uint)sequencePoint.EndColumn;
                i++;
            }

            if (i > 0)
            {
                WriteSequencePoints(symDocumentWriter, i);
            }
        }

        private void WriteSequencePoints(ISymUnmanagedDocumentWriter symDocument, int count)
        {
            try
            {
                symWriter.DefineSequencePoints(
                    symDocument,
                    (uint)count,
                    sequencePointOffsets,
                    sequencePointStartLines,
                    sequencePointStartColumns,
                    sequencePointEndLines,
                    sequencePointEndColumns);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private unsafe void DefineCustomMetadata(string name, byte[] metadata)
        {
            fixed (byte* pb = metadata)
            {
                try
                {
                    // parent parameter is not used, it must be zero or the current method token passed to OpenMetod.
                    this.symWriter.SetSymAttribute(0, name, (uint)metadata.Length, (IntPtr)pb);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }
        }

        private void DefineLocalConstant(string name, object value, PrimitiveTypeCode typeCode, uint constantSignatureToken)
        {
            if (value == null)
            {
                // ISymUnmanagedWriter2.DefineConstant2 throws an ArgumentException
                // if you pass in null - Dev10 appears to use 0 instead.
                // (See EMITTER::VariantFromConstVal)
                value = 0;
                typeCode = PrimitiveTypeCode.Int32;
            }

            if (typeCode == PrimitiveTypeCode.String)
            {
                DefineLocalStringConstant(name, (string)value, constantSignatureToken);
            }
            else
            {
                try
                {
                    this.symWriter.DefineConstant2(name, value, constantSignatureToken);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }
        }

        private void DefineLocalStringConstant(string name, string value, uint constantSignatureToken)
        {
            Debug.Assert(value != null);

            // ISymUnmanagedWriter2 doesn't handle unicode strings with unmatched unicode surrogates.
            // We use the .NET UTF8 encoder to replace unmatched unicode surrogates with unicode replacement character.
            if (!MetadataHelpers.IsValidUnicodeString(value))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                value = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            }

            // EDMAURER If defining a string constant and it is too long (length limit is undocumented), this method throws
            // an ArgumentException.
            // (see EMITTER::EmitDebugLocalConst)

            try
            {
                this.symWriter.DefineConstant2(name, value, constantSignatureToken);
            }
            catch (ArgumentException)
            {
                // writing the constant value into the PDB failed because the string value was most probably too long.
                // We will report a warning for this issue and continue writing the PDB. 
                // The effect on the debug experience is that the symbol for the constant will not be shown in the local
                // window of the debugger. Nor will the user be able to bind to it in expressions in the EE.

                //The triage team has deemed this new warning undesirable. The effects are not significant. The warning
                //is showing up in the DevDiv build more often than expected. We never warned on it before and nobody cared.
                //The proposed warning is not actionable with no source location.
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void DefineLocalVariable(uint index, string name, bool isCompilerGenerated, uint localVariablesSignatureToken)
        {
            const uint ADDR_IL_OFFSET = 1;
            uint attributes = isCompilerGenerated ? 1u : 0u;
            try
            {
                this.symWriter.DefineLocalVariable2(name, attributes, localVariablesSignatureToken, ADDR_IL_OFFSET, index, 0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void SetAsyncInfo(
            uint thisMethodToken,
            uint kickoffMethodToken,
            int catchHandlerOffset,
            ImmutableArray<int> yieldOffsets,
            ImmutableArray<int> resumeOffsets)
        {
            var asyncMethodPropertyWriter = symWriter as ISymUnmanagedAsyncMethodPropertiesWriter;
            if (asyncMethodPropertyWriter != null)
            {
                Debug.Assert(yieldOffsets.IsEmpty == resumeOffsets.IsEmpty);
                if (!yieldOffsets.IsEmpty)
                {
                    int count = yieldOffsets.Length;

                    uint[] yields = new uint[count];
                    uint[] resumes = new uint[count];
                    uint[] methods = new uint[count];

                    for (int i = 0; i < count; i++)
                    {
                        yields[i] = (uint)yieldOffsets[i];
                        resumes[i] = (uint)resumeOffsets[i];
                        methods[i] = (uint)thisMethodToken;
                    }

                    try
                    {
                        asyncMethodPropertyWriter.DefineAsyncStepInfo((uint)count, yields, resumes, methods);
                    }
                    catch (Exception ex)
                    {
                        throw new PdbWritingException(ex);
                    }
                }

                try
                {
                    if (catchHandlerOffset >= 0)
                    {
                        asyncMethodPropertyWriter.DefineCatchHandlerILOffset((uint)catchHandlerOffset);
                    }
                    asyncMethodPropertyWriter.DefineKickoffMethod(kickoffMethodToken);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }
        }

        public void WriteDefinitionLocations(MultiDictionary<DebugSourceDocument, DefinitionWithLocation> file2definitions)
        {
            var writer5 = this.symWriter as ISymUnmanagedWriter5;

            if ((object)writer5 != null)
            {
                // NOTE: ISymUnmanagedWriter5 reports HRESULT = 0x806D000E in case we open and close 
                //       the map without writing any resords with MapTokenToSourceSpan(...)
                bool open = false;

                foreach (var doc in file2definitions.Keys)
                {
                    ISymUnmanagedDocumentWriter docWriter = GetDocumentWriter(doc);
                    foreach (var definition in file2definitions[doc])
                    {
                        if (!open)
                        {
                            try
                            {
                                writer5.OpenMapTokensToSourceSpans();
                            }
                            catch (Exception ex)
                            {
                                throw new PdbWritingException(ex);
                            }

                            open = true;
                        }

                        uint token = peWriter.GetTokenForDefinition(definition.Definition);
                        Debug.Assert(token != 0);

                        try
                        {
                            writer5.MapTokenToSourceSpan(token, docWriter,
                                definition.StartLine + 1, definition.StartColumn + 1, definition.EndLine + 1, definition.EndColumn + 1);
                        }
                        catch (Exception ex)
                        {
                            throw new PdbWritingException(ex);
                        }
                    }
                }

                if (open)
                {
                    try
                    {
                        writer5.CloseMapTokensToSourceSpans();
                    }
                    catch (Exception ex)
                    {
                        throw new PdbWritingException(ex);
                    }
                }
            }
        }

        #endregion
    }
}