﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Debugging
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.DiaSymReader
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class EvaluationContext
        Inherits EvaluationContextBase

        ' These names are arbitrary, so we'll just use the same names as
        ' the C# expression compiler.
        Private Const s_typeName = "<>x"
        Private Const s_methodName = "<>m0"

        Friend ReadOnly MethodContextReuseConstraints As MethodContextReuseConstraints?
        Friend ReadOnly Compilation As VisualBasicCompilation

        Private ReadOnly _currentFrame As MethodSymbol
        Private ReadOnly _currentSourceMethod As MethodSymbol
        Private ReadOnly _locals As ImmutableArray(Of LocalSymbol)
        Private ReadOnly _inScopeHoistedLocalSlots As ImmutableSortedSet(Of Integer)
        Private ReadOnly _methodDebugInfo As MethodDebugInfo(Of TypeSymbol, LocalSymbol)

        Private Sub New(
            methodContextReuseConstraints As MethodContextReuseConstraints?,
            compilation As VisualBasicCompilation,
            currentFrame As MethodSymbol,
            currentSourceMethod As MethodSymbol,
            locals As ImmutableArray(Of LocalSymbol),
            inScopeHoistedLocalSlots As ImmutableSortedSet(Of Integer),
            methodDebugInfo As MethodDebugInfo(Of TypeSymbol, LocalSymbol))

            Me.MethodContextReuseConstraints = methodContextReuseConstraints
            Me.Compilation = compilation
            _currentFrame = currentFrame
            _currentSourceMethod = currentSourceMethod
            _locals = locals
            _inScopeHoistedLocalSlots = inScopeHoistedLocalSlots
            _methodDebugInfo = methodDebugInfo
        End Sub

        ''' <summary>
        ''' Create a context for evaluating expressions at a type scope.
        ''' </summary>
        ''' <param name="compilation">Compilation.</param>
        ''' <param name="moduleVersionId">Module containing type.</param>
        ''' <param name="typeToken">Type metadata token.</param>
        ''' <returns>Evaluation context.</returns>
        ''' <remarks>
        ''' No locals since locals are associated with methods, not types.
        ''' </remarks>
        Friend Shared Function CreateTypeContext(
            compilation As VisualBasicCompilation,
            moduleVersionId As Guid,
            typeToken As Integer) As EvaluationContext

            Debug.Assert(MetadataTokens.Handle(typeToken).Kind = HandleKind.TypeDefinition)

            Dim currentType = compilation.GetType(moduleVersionId, typeToken)
            Debug.Assert(currentType IsNot Nothing)

            Dim currentFrame = New SynthesizedContextMethodSymbol(currentType)
            Return New EvaluationContext(
                Nothing,
                compilation,
                currentFrame,
                Nothing,
                locals:=Nothing,
                inScopeHoistedLocalSlots:=Nothing,
                methodDebugInfo:=MethodDebugInfo(Of TypeSymbol, LocalSymbol).None)
        End Function

        ''' <summary>
        ''' Create a context for evaluating expressions within a method scope.
        ''' </summary>
        ''' <param name="previous">Previous context, if any, for possible re-use.</param>
        ''' <param name="metadataBlocks">Module metadata.</param>
        ''' <param name="symReader"><see cref="ISymUnmanagedReader"/> for PDB associated with <paramref name="moduleVersionId"/>.</param>
        ''' <param name="moduleVersionId">Module containing method.</param>
        ''' <param name="methodToken">Method metadata token.</param>
        ''' <param name="methodVersion">Method version.</param>
        ''' <param name="ilOffset">IL offset of instruction pointer in method.</param>
        ''' <param name="localSignatureToken">Method local signature token.</param>
        ''' <returns>Evaluation context.</returns>
        Friend Shared Function CreateMethodContext(
            previous As VisualBasicMetadataContext,
            metadataBlocks As ImmutableArray(Of MetadataBlock),
            lazyAssemblyReaders As Lazy(Of ImmutableArray(Of AssemblyReaders)),
            symReader As Object,
            moduleVersionId As Guid,
            methodToken As Integer,
            methodVersion As Integer,
            ilOffset As UInteger,
            localSignatureToken As Integer) As EvaluationContext

            Dim offset = NormalizeILOffset(ilOffset)

            Dim compilation As VisualBasicCompilation = metadataBlocks.ToCompilation(Nothing, MakeAssemblyReferencesKind.AllAssemblies)

            Return CreateMethodContext(
                compilation,
                lazyAssemblyReaders,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                offset,
                localSignatureToken)
        End Function

        ''' <summary>
        ''' Create a context for evaluating expressions within a method scope.
        ''' </summary>
        ''' <param name="compilation">Compilation.</param>
        ''' <param name="symReader"><see cref="ISymUnmanagedReader"/> for PDB associated with <paramref name="moduleVersionId"/>.</param>
        ''' <param name="moduleVersionId">Module containing method.</param>
        ''' <param name="methodToken">Method metadata token.</param>
        ''' <param name="methodVersion">Method version.</param>
        ''' <param name="ilOffset">IL offset of instruction pointer in method.</param>
        ''' <param name="localSignatureToken">Method local signature token.</param>
        ''' <returns>Evaluation context.</returns>
        Friend Shared Function CreateMethodContext(
            compilation As VisualBasicCompilation,
            lazyAssemblyReaders As Lazy(Of ImmutableArray(Of AssemblyReaders)),
            symReader As Object,
            moduleVersionId As Guid,
            methodToken As Integer,
            methodVersion As Integer,
            ilOffset As Integer,
            localSignatureToken As Integer) As EvaluationContext

            Dim methodHandle = CType(MetadataTokens.Handle(methodToken), MethodDefinitionHandle)
            Dim currentSourceMethod = compilation.GetSourceMethod(moduleVersionId, methodHandle)
            Dim localSignatureHandle = If(localSignatureToken <> 0, CType(MetadataTokens.Handle(localSignatureToken), StandaloneSignatureHandle), Nothing)

            Dim currentFrame = compilation.GetMethod(moduleVersionId, methodHandle)
            Debug.Assert(currentFrame IsNot Nothing)
            Dim symbolProvider = New VisualBasicEESymbolProvider(DirectCast(currentFrame.ContainingModule, PEModuleSymbol), currentFrame)

            Dim metadataDecoder = New MetadataDecoder(DirectCast(currentFrame.ContainingModule, PEModuleSymbol), currentFrame)
            Dim localInfo = metadataDecoder.GetLocalInfo(localSignatureHandle)

            Dim debugInfo As MethodDebugInfo(Of TypeSymbol, LocalSymbol)

            If IsDteeEntryPoint(currentFrame) Then
                debugInfo = SynthesizeMethodDebugInfoForDtee(lazyAssemblyReaders.Value)
            Else
                Dim typedSymReader = DirectCast(symReader, ISymUnmanagedReader3)
                debugInfo = MethodDebugInfo(Of TypeSymbol, LocalSymbol).ReadMethodDebugInfo(typedSymReader, symbolProvider, methodToken, methodVersion, ilOffset, isVisualBasicMethod:=True)
            End If

            Dim reuseSpan = debugInfo.ReuseSpan

            Dim inScopeHoistedLocalSlots As ImmutableSortedSet(Of Integer)
            If debugInfo.HoistedLocalScopeRecords.IsDefault Then
                inScopeHoistedLocalSlots = GetInScopeHoistedLocalSlots(debugInfo.LocalVariableNames)
            Else
                inScopeHoistedLocalSlots = debugInfo.GetInScopeHoistedLocalIndices(ilOffset, reuseSpan)
            End If

            Dim localNames = debugInfo.LocalVariableNames.WhereAsArray(
                Function(name) name Is Nothing OrElse Not name.StartsWith(StringConstants.StateMachineHoistedUserVariablePrefix, StringComparison.Ordinal))

            Dim localsBuilder = ArrayBuilder(Of LocalSymbol).GetInstance()
            MethodDebugInfo(Of TypeSymbol, LocalSymbol).GetLocals(localsBuilder, symbolProvider, localNames, localInfo, Nothing, debugInfo.TupleLocalMap)

            GetStaticLocals(localsBuilder, currentFrame, methodHandle, metadataDecoder)
            localsBuilder.AddRange(debugInfo.LocalConstants)

            Return New EvaluationContext(
                New MethodContextReuseConstraints(moduleVersionId, methodToken, methodVersion, reuseSpan),
                compilation,
                currentFrame,
                currentSourceMethod,
                localsBuilder.ToImmutableAndFree(),
                inScopeHoistedLocalSlots,
                debugInfo)
        End Function

        Private Shared Function GetInScopeHoistedLocalSlots(allLocalNames As ImmutableArray(Of String)) As ImmutableSortedSet(Of Integer)
            Dim builder = ArrayBuilder(Of Integer).GetInstance()
            For Each localName In allLocalNames
                Dim hoistedLocalName As String = Nothing
                Dim hoistedLocalSlot As Integer = 0
                If localName IsNot Nothing AndAlso GeneratedNames.TryParseStateMachineHoistedUserVariableName(localName, hoistedLocalName, hoistedLocalSlot) Then
                    builder.Add(hoistedLocalSlot)
                End If
            Next

            Dim result = builder.ToImmutableSortedSet()
            builder.Free()
            Return result
        End Function

        ''' <summary>
        ''' When using DTEE with the hosting process enabled, if the assembly being debugged doesn't have
        ''' a Main method, the debugger will actually stop in a driver method in the hosting process.
        ''' As in the native EE, we detect such methods by name (respecting case).
        ''' </summary>
        ''' <remarks>
        ''' Logic copied from ProcedureContext::IsDteeEntryPoint.
        ''' Friend for testing.
        ''' </remarks>
        ''' <seealso cref="SynthesizeMethodDebugInfoForDtee"/>
        Friend Shared Function IsDteeEntryPoint(currentFrame As MethodSymbol) As Boolean
            Dim typeName = currentFrame.ContainingType.Name
            Dim methodName = currentFrame.Name
            Return _
                (String.Equals(typeName, "HostProc", StringComparison.Ordinal) AndAlso ' DLL
                    String.Equals(methodName, "BreakForDebugger", StringComparison.Ordinal)) OrElse
                (String.Equals(typeName, "AppDomain", StringComparison.Ordinal) AndAlso ' WPF app
                    String.Equals(methodName, "ExecuteAssembly", StringComparison.Ordinal))
        End Function

        ''' <summary>
        ''' When using DTEE with the hosting process enabled, if the assembly being debugged doesn't have
        ''' a Main method, the debugger will actually stop in a driver method in the hosting process.
        ''' (This condition is detected by <see cref="IsDteeEntryPoint"/>.)
        ''' 
        ''' Since such driver methods have no knowledge of the assembly being debugged, we synthesize 
        ''' imports to bring symbols from the target assembly into scope (for convenience).  In particular,
        ''' we import the root namespace of any assembly that isn't obviously a framework assembly and
        ''' any namespace containing a module type.
        ''' </summary>
        ''' <remarks>
        ''' Logic copied from ProcedureContext::LoadImportsAndDefaultNamespaceForDteeEntryPoint.
        ''' Friend for testing.
        ''' </remarks>
        ''' <seealso cref="IsDteeEntryPoint"/>
        ''' <seealso cref="PENamedTypeSymbol.TypeKind"/>
        Friend Shared Function SynthesizeMethodDebugInfoForDtee(assemblyReaders As ImmutableArray(Of AssemblyReaders)) As MethodDebugInfo(Of TypeSymbol, LocalSymbol)
            Dim [imports] = PooledHashSet(Of String).GetInstance()

            For Each readers In assemblyReaders
                Dim metadataReader = readers.MetadataReader
                Dim symReader = DirectCast(readers.SymReader, ISymUnmanagedReader3)

                ' Ignore assemblies for which we don't have PDBs.
                If symReader Is Nothing Then Continue For

                Try
                    For Each typeDefHandle In metadataReader.TypeDefinitions
                        Dim typeDef = metadataReader.GetTypeDefinition(typeDefHandle)

                        ' VB does ignores the StandardModuleAttribute on interfaces and nested
                        ' or generic types (see PENamedTypeSymbol.TypeKind).
                        If Not PEModule.IsNested(typeDef.Attributes) AndAlso
                            typeDef.GetGenericParameters().Count = 0 AndAlso
                            (typeDef.Attributes And TypeAttributes.[Interface]) = 0 AndAlso
                            PEModule.FindTargetAttribute(metadataReader, typeDefHandle, AttributeDescription.StandardModuleAttribute).HasValue Then

                            Dim namespaceName = metadataReader.GetString(typeDef.Namespace)
                            [imports].Add(namespaceName)
                        End If
                    Next

                    For Each methodDefHandle In metadataReader.MethodDefinitions
                        ' TODO: this can be done better
                        ' EnC can't change the default namespace of the assembly, so version 1 will suffice.
                        Dim debugInfo = MethodDebugInfo(Of TypeSymbol, LocalSymbol).ReadMethodDebugInfo(symReader,
                                                                                                        Nothing,
                                                                                                        metadataReader.GetToken(methodDefHandle),
                                                                                                        methodVersion:=1,
                                                                                                        ilOffset:=0,
                                                                                                        isVisualBasicMethod:=True)

                        ' Some methods aren't decorated with import custom debug info.
                        If Not String.IsNullOrEmpty(debugInfo.DefaultNamespaceName) Then

                            ' NOTE: We're adding it as a project-level import, not as the default namespace
                            ' (because there's one for each assembly and they can't all be the default).
                            [imports].Add(debugInfo.DefaultNamespaceName)

                            ' The default namespace should be the same for all methods, so we only need to check one.
                            Exit For
                        End If
                    Next

                Catch ex As BadImageFormatException
                    ' This will only prevent us from synthesizing imports for the module types in
                    ' one assembly - it is decidedly recoverable.
                End Try
            Next

            Dim projectLevelImportRecords = ImmutableArray.CreateRange([imports].Select(
                Function(namespaceName) New ImportRecord(ImportTargetKind.Namespace,
                                                         alias:=Nothing,
                                                         targetType:=Nothing,
                                                         targetString:=namespaceName,
                                                         targetAssembly:=Nothing,
                                                         targetAssemblyAlias:=Nothing)))

            [imports].Free()
            Dim fileLevelImportRecords = ImmutableArray(Of ImportRecord).Empty

            Dim importRecordGroups = ImmutableArray.Create(fileLevelImportRecords, projectLevelImportRecords)

            Return New MethodDebugInfo(Of TypeSymbol, LocalSymbol)(
                hoistedLocalScopeRecords:=ImmutableArray(Of HoistedLocalScopeRecord).Empty,
                importRecordGroups:=importRecordGroups,
                defaultNamespaceName:="",
                externAliasRecords:=ImmutableArray(Of ExternAliasRecord).Empty,
                dynamicLocalMap:=Nothing,
                tupleLocalMap:=Nothing,
                localVariableNames:=ImmutableArray(Of String).Empty,
                localConstants:=ImmutableArray(Of LocalSymbol).Empty,
                reuseSpan:=Nothing)
        End Function

        Friend Function CreateCompilationContext(withSyntax As Boolean) As CompilationContext
            Return New CompilationContext(
                Compilation,
                _currentFrame,
                _currentSourceMethod,
                _locals,
                _inScopeHoistedLocalSlots,
                _methodDebugInfo,
                withSyntax)
        End Function

        Friend Overrides Function CompileExpression(
            expr As String,
            compilationFlags As DkmEvaluationFlags,
            aliases As ImmutableArray(Of [Alias]),
            diagnostics As DiagnosticBag,
            <Out> ByRef resultProperties As ResultProperties,
            testData As Microsoft.CodeAnalysis.CodeGen.CompilationTestData) As CompileResult

            resultProperties = Nothing
            Dim formatSpecifiers As ReadOnlyCollection(Of String) = Nothing
            Dim syntax = If((compilationFlags And DkmEvaluationFlags.TreatAsExpression) <> 0,
                    expr.ParseExpression(diagnostics, allowFormatSpecifiers:=True, formatSpecifiers:=formatSpecifiers),
                    expr.ParseStatement(diagnostics))
            If syntax Is Nothing Then
                Return Nothing
            End If

            If Not IsSupportedDebuggerStatement(syntax) Then
                diagnostics.Add(New SimpleMessageDiagnostic(String.Format(Resources.InvalidDebuggerStatement, syntax.Kind)))
                Return Nothing
            End If

            Dim context = Me.CreateCompilationContext(withSyntax:=True)
            Dim synthesizedMethod As EEMethodSymbol = Nothing
            Dim moduleBuilder = context.Compile(DirectCast(syntax, ExecutableStatementSyntax), s_typeName, s_methodName, aliases, testData, diagnostics, synthesizedMethod)
            If moduleBuilder Is Nothing Then
                Return Nothing
            End If

            Using stream As New MemoryStream()
                Cci.PeWriter.WritePeToStream(
                        New EmitContext(moduleBuilder, Nothing, diagnostics, metadataOnly:=False, includePrivateMembers:=True),
                        context.MessageProvider,
                        Function() stream,
                        getPortablePdbStreamOpt:=Nothing,
                        nativePdbWriterOpt:=Nothing,
                        pdbOptionsBlobReader:=Nothing,
                        pdbPathOpt:=Nothing,
                        metadataOnly:=False,
                        isDeterministic:=False,
                        emitTestCoverageData:=False,
                        privateKeyOpt:=Nothing,
                        cancellationToken:=Nothing)

                If diagnostics.HasAnyErrors() Then
                    Return Nothing
                End If

                Debug.Assert(synthesizedMethod.ContainingType.MetadataName = s_typeName)
                Debug.Assert(synthesizedMethod.MetadataName = s_methodName)

                resultProperties = synthesizedMethod.ResultProperties
                Return New VisualBasicCompileResult(
                        stream.ToArray(),
                        synthesizedMethod,
                        formatSpecifiers)
            End Using
        End Function

        Friend Overrides Function CompileAssignment(
            target As String,
            expr As String,
            aliases As ImmutableArray(Of [Alias]),
            diagnostics As DiagnosticBag,
            <Out> ByRef resultProperties As ResultProperties,
            testData As Microsoft.CodeAnalysis.CodeGen.CompilationTestData) As CompileResult

            Dim assignment = target.ParseAssignment(expr, diagnostics)
            If assignment Is Nothing Then
                Return Nothing
            End If

            Dim context = Me.CreateCompilationContext(withSyntax:=True)
            Dim synthesizedMethod As EEMethodSymbol = Nothing
            Dim modulebuilder = context.Compile(assignment, s_typeName, s_methodName, aliases, testData, diagnostics, synthesizedMethod)
            If modulebuilder Is Nothing Then
                Return Nothing
            End If

            Using stream As New MemoryStream()
                Cci.PeWriter.WritePeToStream(
                        New EmitContext(modulebuilder, Nothing, diagnostics, metadataOnly:=False, includePrivateMembers:=True),
                        context.MessageProvider,
                        Function() stream,
                        getPortablePdbStreamOpt:=Nothing,
                        nativePdbWriterOpt:=Nothing,
                        pdbOptionsBlobReader:=Nothing,
                        pdbPathOpt:=Nothing,
                        metadataOnly:=False,
                        isDeterministic:=False,
                        emitTestCoverageData:=False,
                        privateKeyOpt:=Nothing,
                        cancellationToken:=Nothing)

                If diagnostics.HasAnyErrors() Then
                    Return Nothing
                End If

                Debug.Assert(synthesizedMethod.ContainingType.MetadataName = s_typeName)
                Debug.Assert(synthesizedMethod.MetadataName = s_methodName)

                Dim properties = synthesizedMethod.ResultProperties
                resultProperties = New ResultProperties(
                        properties.Flags Or DkmClrCompilationResultFlags.PotentialSideEffect,
                        properties.Category,
                        properties.AccessType,
                        properties.StorageType,
                        properties.ModifierFlags)
                Return New VisualBasicCompileResult(
                        stream.ToArray(),
                        synthesizedMethod,
                        formatSpecifiers:=Nothing)
            End Using
        End Function

        Private Shared ReadOnly s_emptyBytes As New ReadOnlyCollection(Of Byte)(Array.Empty(Of Byte))

        Friend Overrides Function CompileGetLocals(
            locals As ArrayBuilder(Of LocalAndMethod),
            argumentsOnly As Boolean,
            aliases As ImmutableArray(Of [Alias]),
            diagnostics As DiagnosticBag,
            <Out> ByRef typeName As String,
            testData As CompilationTestData) As ReadOnlyCollection(Of Byte)

            Dim context = Me.CreateCompilationContext(withSyntax:=False)
            Dim modulebuilder = context.CompileGetLocals(s_typeName, locals, argumentsOnly, aliases, testData, diagnostics)
            Dim assembly As ReadOnlyCollection(Of Byte) = Nothing

            If modulebuilder IsNot Nothing AndAlso locals.Count > 0 Then
                Using stream As New MemoryStream()
                    Cci.PeWriter.WritePeToStream(
                        New EmitContext(modulebuilder, Nothing, diagnostics, metadataOnly:=False, includePrivateMembers:=True),
                        context.MessageProvider,
                        Function() stream,
                        getPortablePdbStreamOpt:=Nothing,
                        nativePdbWriterOpt:=Nothing,
                        pdbOptionsBlobReader:=Nothing,
                        pdbPathOpt:=Nothing,
                        metadataOnly:=False,
                        isDeterministic:=False,
                        emitTestCoverageData:=False,
                        privateKeyOpt:=Nothing,
                        cancellationToken:=Nothing)

                    If Not diagnostics.HasAnyErrors() Then
                        assembly = New ReadOnlyCollection(Of Byte)(stream.ToArray())
                    End If
                End Using
            End If

            If assembly Is Nothing Then
                locals.Clear()
                assembly = s_emptyBytes
            End If

            typeName = EvaluationContext.s_typeName
            Return assembly
        End Function

        ''' <summary>
        ''' Include static locals for the given method. Static locals
        ''' are represented as fields on the containing class named
        ''' "$STATIC$[methodname]$[methodsignature]$[localname]".
        ''' </summary>
        Private Shared Sub GetStaticLocals(
            builder As ArrayBuilder(Of LocalSymbol),
            method As MethodSymbol,
            methodHandle As MethodDefinitionHandle,
            metadataDecoder As MetadataDecoder)

            Dim type = method.ContainingType
            If type.TypeKind <> TypeKind.Class Then
                Return
            End If

            For Each member In type.GetMembers()
                If member.Kind <> SymbolKind.Field Then
                    Continue For
                End If
                Dim methodName As String = Nothing
                Dim methodSignature As String = Nothing
                Dim localName As String = Nothing
                If GeneratedNames.TryParseStaticLocalFieldName(member.Name, methodName, methodSignature, localName) AndAlso
                    String.Equals(methodName, method.Name, StringComparison.Ordinal) AndAlso
                    String.Equals(methodSignature, GetMethodSignatureString(metadataDecoder, methodHandle), StringComparison.Ordinal) Then
                    builder.Add(New EEStaticLocalSymbol(method, DirectCast(member, FieldSymbol), localName))
                End If
            Next
        End Sub

        Private Shared Function GetMethodSignatureString(metadataDecoder As MetadataDecoder, methodHandle As MethodDefinitionHandle) As String
            Dim [module] = metadataDecoder.Module
            Dim signatureHandle = [module].GetMethodSignatureOrThrow(methodHandle)
            Dim signatureReader = [module].GetMemoryReaderOrThrow(signatureHandle)
            Dim signature = signatureReader.ReadBytes(signatureReader.Length)
            Return GeneratedNames.MakeSignatureString(signature)
        End Function

        Friend Overrides Function HasDuplicateTypesOrAssemblies(diagnostic As Diagnostic) As Boolean
            Select Case CType(diagnostic.Code, ERRID)
                Case ERRID.ERR_DuplicateReference2,
                     ERRID.ERR_DuplicateReferenceStrong,
                     ERRID.ERR_AmbiguousInUnnamedNamespace1,
                     ERRID.ERR_AmbiguousInNamespace2,
                     ERRID.ERR_NoMostSpecificOverload2,
                     ERRID.ERR_AmbiguousInModules2
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Friend Overrides Function GetMissingAssemblyIdentities(diagnostic As Diagnostic, linqLibrary As AssemblyIdentity) As ImmutableArray(Of AssemblyIdentity)
            Return GetMissingAssemblyIdentitiesHelper(CType(diagnostic.Code, ERRID), diagnostic.Arguments, Me.Compilation.GlobalNamespace, linqLibrary)
        End Function

        ''' <remarks>
        ''' Friend for testing.
        ''' </remarks>
        Friend Shared Function GetMissingAssemblyIdentitiesHelper(code As ERRID, arguments As IReadOnlyList(Of Object), globalNamespace As NamespaceSymbol, linqLibrary As AssemblyIdentity) As ImmutableArray(Of AssemblyIdentity)
            Debug.Assert(linqLibrary IsNot Nothing)

            Select Case code
                Case ERRID.ERR_UnreferencedAssemblyEvent3, ERRID.ERR_UnreferencedAssembly3
                    For Each argument As Object In arguments
                        Dim identity = If(TryCast(argument, AssemblyIdentity), TryCast(argument, AssemblySymbol)?.Identity)
                        If IsValidMissingAssemblyIdentity(identity) Then
                            Return ImmutableArray.Create(identity)
                        End If
                    Next
                Case ERRID.ERR_ForwardedTypeUnavailable3
                    If arguments.Count = 3 Then
                        Dim identity As AssemblyIdentity = TryCast(arguments(2), AssemblySymbol)?.Identity
                        If IsValidMissingAssemblyIdentity(identity) Then
                            Return ImmutableArray.Create(identity)
                        End If
                    End If
                Case ERRID.ERR_NameNotMember2
                    If arguments.Count = 2 Then
                        Dim namespaceName = TryCast(arguments(0), String)
                        Dim containingNamespace = TryCast(arguments(1), NamespaceSymbol)
                        If namespaceName IsNot Nothing AndAlso containingNamespace IsNot Nothing AndAlso HasConstituentFromWindowsAssembly(containingNamespace) Then
                            ' This is just a heuristic, but it has the advantage of being portable, particularly
                            ' across different versions of (desktop) windows.
                            Dim identity = New AssemblyIdentity($"{containingNamespace.ToDisplayString}.{namespaceName}", contentType:=AssemblyContentType.WindowsRuntime)
                            Return ImmutableArray.Create(identity)
                        Else
                            ' Maybe it's a missing LINQ extension method.  Let's try adding the "LINQ library" to see if that helps.
                            Return ImmutableArray.Create(linqLibrary)
                        End If
                    End If
                Case ERRID.ERR_UndefinedType1
                    If arguments.Count = 1 Then
                        Dim qualifiedName = TryCast(arguments(0), String)
                        If Not String.IsNullOrEmpty(qualifiedName) Then
                            Dim nameParts = qualifiedName.Split("."c)
                            Dim numParts = nameParts.Length
                            Dim pos = 0
                            If CaseInsensitiveComparison.Comparer.Equals(nameParts(0), "global") Then
                                pos = 1
                                Debug.Assert(pos < numParts)
                            End If
                            Dim currNamespace = globalNamespace
                            While pos < numParts
                                Dim nextNamespace = currNamespace.GetMembers(nameParts(pos)).OfType(Of NamespaceSymbol).SingleOrDefault()
                                If nextNamespace Is Nothing Then
                                    Exit While
                                End If
                                pos += 1
                                currNamespace = nextNamespace
                            End While

                            If currNamespace IsNot globalNamespace AndAlso HasConstituentFromWindowsAssembly(currNamespace) AndAlso pos < numParts Then
                                Dim nextNamePart = nameParts(pos)
                                If nextNamePart.All(AddressOf SyntaxFacts.IsIdentifierPartCharacter) Then
                                    ' This is just a heuristic, but it has the advantage of being portable, particularly
                                    ' across different versions of (desktop) windows.
                                    Dim identity = New AssemblyIdentity($"{currNamespace.ToDisplayString}.{nameParts(pos)}", contentType:=AssemblyContentType.WindowsRuntime)
                                    Return ImmutableArray.Create(identity)
                                End If
                            End If
                        End If
                    End If
                Case ERRID.ERR_XmlFeaturesNotAvailable
                    Return ImmutableArray.Create(SystemIdentity, linqLibrary, SystemXmlIdentity, SystemXmlLinqIdentity)
                Case ERRID.ERR_MissingRuntimeHelper
                    Return ImmutableArray.Create(MicrosoftVisualBasicIdentity)
            End Select

            Return Nothing
        End Function

        Private Shared Function HasConstituentFromWindowsAssembly(namespaceSymbol As NamespaceSymbol) As Boolean
            Return namespaceSymbol.ConstituentNamespaces.Any(Function(n) n.ContainingAssembly.Identity.IsWindowsAssemblyIdentity)
        End Function

        Private Shared Function IsValidMissingAssemblyIdentity(identity As AssemblyIdentity) As Boolean
            Return identity IsNot Nothing AndAlso Not identity.Equals(MissingCorLibrarySymbol.Instance.Identity)
        End Function

        Private Shared Function GetSynthesizedMethod(moduleBuilder As CommonPEModuleBuilder) As MethodSymbol
            Dim method = DirectCast(moduleBuilder, EEAssemblyBuilder).Methods.Single(Function(m) m.MetadataName = s_methodName)
            Debug.Assert(method.ContainingType.MetadataName = s_typeName)
            Return method
        End Function

    End Class

End Namespace
