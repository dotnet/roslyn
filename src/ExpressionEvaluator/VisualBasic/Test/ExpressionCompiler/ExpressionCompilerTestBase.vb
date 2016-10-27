﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.DiaSymReader
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests
    Public MustInherit Class ExpressionCompilerTestBase
        Inherits BasicTestBase
        Implements IDisposable

        Private ReadOnly _runtimeInstances As ArrayBuilder(Of IDisposable) = ArrayBuilder(Of IDisposable).GetInstance()

        Friend Shared ReadOnly NoAliases As ImmutableArray(Of [Alias]) = ImmutableArray(Of [Alias]).Empty

        Protected Sub New()
            ' We never want to swallow Exceptions (generate a non-fatal Watson) when running tests.
            ExpressionEvaluatorFatalError.IsFailFastEnabled = True
        End Sub

        Public Overrides Sub Dispose()
            MyBase.Dispose()

            For Each instance In _runtimeInstances
                instance.Dispose()
            Next
            _runtimeInstances.Free()
        End Sub

        ' TODO: remove -- workaround for bug https://github.com/dotnet/roslyn/issues/8473 in the VB compiler
        ' https://github.com/dotnet/roslyn/issues/8473
        Friend Shared Sub WithRuntimeInstancePortableBug(compilation As Compilation, validator As Action(Of RuntimeInstance))
            Using instance = RuntimeInstance.Create(compilation, Nothing, DebugInformationFormat.Pdb, True)
                validator(instance)
            End Using
        End Sub

        Friend Shared Sub WithRuntimeInstance(compilation As Compilation, validator As Action(Of RuntimeInstance))
            WithRuntimeInstance(compilation, Nothing, True, validator)
        End Sub

        Friend Shared Sub WithRuntimeInstance(compilation As Compilation, references As IEnumerable(Of MetadataReference), validator As Action(Of RuntimeInstance))
            WithRuntimeInstance(compilation, references, True, validator)
        End Sub

        Friend Shared Sub WithRuntimeInstance(compilation As Compilation, references As IEnumerable(Of MetadataReference), includeLocalSignatures As Boolean, validator As Action(Of RuntimeInstance))
            For Each debugFormat In {DebugInformationFormat.Pdb, DebugInformationFormat.PortablePdb}
                Using instance = RuntimeInstance.Create(compilation, references, debugFormat, includeLocalSignatures)
                    validator(instance)
                End Using
            Next
        End Sub

        Friend Function CreateRuntimeInstance(modules As IEnumerable(Of ModuleInstance)) As RuntimeInstance
            Dim instance = RuntimeInstance.Create(modules)
            _runtimeInstances.Add(instance)
            Return instance
        End Function

        Friend Function CreateRuntimeInstance(
            compilation As Compilation,
            Optional references As IEnumerable(Of MetadataReference) = Nothing,
            Optional debugFormat As DebugInformationFormat = DebugInformationFormat.Pdb,
            Optional includeLocalSignatures As Boolean = True) As RuntimeInstance

            Dim instance = RuntimeInstance.Create(compilation, references, debugFormat, includeLocalSignatures)
            _runtimeInstances.Add(instance)
            Return instance
        End Function

        Friend Function CreateRuntimeInstance(
            [module] As ModuleInstance,
            Optional references As IEnumerable(Of MetadataReference) = Nothing) As RuntimeInstance

            Dim instance = RuntimeInstance.Create([module], references, DebugInformationFormat.Pdb)
            _runtimeInstances.Add(instance)
            Return instance
        End Function

        Friend Shared Sub GetContextState(
            runtime As RuntimeInstance,
            methodOrTypeName As String,
            <Out> ByRef blocks As ImmutableArray(Of MetadataBlock),
            <Out> ByRef moduleVersionId As Guid,
            <Out> ByRef symReader As ISymUnmanagedReader,
            <Out> ByRef methodOrTypeToken As Integer,
            <Out> ByRef localSignatureToken As Integer)

            Dim moduleInstances = runtime.Modules
            blocks = moduleInstances.SelectAsArray(Function(m) m.MetadataBlock)

            Dim compilation = blocks.ToCompilation()
            Dim methodOrType = GetMethodOrTypeBySignature(compilation, methodOrTypeName)
            Dim [module] = DirectCast(methodOrType.ContainingModule, PEModuleSymbol)
            Dim id = [module].Module.GetModuleVersionIdOrThrow()
            Dim moduleInstance = moduleInstances.First(Function(m) m.ModuleVersionId = id)

            moduleVersionId = id
            symReader = DirectCast(moduleInstance.SymReader, ISymUnmanagedReader)

            Dim methodOrTypeHandle As EntityHandle
            If methodOrType.Kind = SymbolKind.Method Then
                methodOrTypeHandle = DirectCast(methodOrType, PEMethodSymbol).Handle
                localSignatureToken = moduleInstance.GetLocalSignatureToken(CType(methodOrTypeHandle, MethodDefinitionHandle))
            Else
                methodOrTypeHandle = DirectCast(methodOrType, PENamedTypeSymbol).Handle
                localSignatureToken = -1
            End If

            Dim reader As MetadataReader = Nothing ' Nothing should be okay.
            methodOrTypeToken = reader.GetToken(methodOrTypeHandle)
        End Sub

        Friend Shared Function CreateMethodContext(
            runtime As RuntimeInstance,
            methodName As String,
            Optional atLineNumber As Integer = -1,
            Optional lazyAssemblyReaders As Lazy(Of ImmutableArray(Of AssemblyReaders)) = Nothing) As EvaluationContext

            Dim blocks As ImmutableArray(Of MetadataBlock) = Nothing
            Dim moduleVersionId As Guid = Nothing
            Dim symReader As ISymUnmanagedReader = Nothing
            Dim methodToken = 0
            Dim localSignatureToken = 0
            GetContextState(runtime, methodName, blocks, moduleVersionId, symReader, methodToken, localSignatureToken)
            Const methodVersion = 1

            Dim ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader, atLineNumber)

            Return EvaluationContext.CreateMethodContext(
                Nothing,
                blocks,
                If(lazyAssemblyReaders, MakeDummyLazyAssemblyReaders()),
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken)
        End Function

        Friend Shared Function MakeDummyLazyAssemblyReaders() As Lazy(Of ImmutableArray(Of AssemblyReaders))
            Dim f As Func(Of ImmutableArray(Of AssemblyReaders)) =
                Function()
                    ' The vast majority of tests should not trigger evaluation of the Lazy.
                    Throw ExceptionUtilities.Unreachable
                End Function
            Return New Lazy(Of ImmutableArray(Of AssemblyReaders))(f)
        End Function

        Friend Shared Function CreateTypeContext(
            runtime As RuntimeInstance,
            typeName As String) As EvaluationContext

            Dim blocks As ImmutableArray(Of MetadataBlock) = Nothing
            Dim moduleVersionId As Guid = Nothing
            Dim symReader As ISymUnmanagedReader = Nothing
            Dim typeToken = 0
            Dim localSignatureToken = 0
            GetContextState(runtime, typeName, blocks, moduleVersionId, symReader, typeToken, localSignatureToken)
            Return EvaluationContext.CreateTypeContext(
                Nothing,
                blocks,
                moduleVersionId,
                typeToken)
        End Function

        Friend Function Evaluate(
            source As String,
            outputKind As OutputKind,
            methodName As String,
            expr As String,
            Optional atLineNumber As Integer = -1,
            Optional includeSymbols As Boolean = True) As CompilationTestData

            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim result = Evaluate(source, outputKind, methodName, expr, resultProperties, errorMessage, atLineNumber, includeSymbols)
            Assert.Null(errorMessage)
            Return result
        End Function

        Friend Function Evaluate(
            source As String,
            outputKind As OutputKind,
            methodName As String,
            expr As String,
            <Out> ByRef resultProperties As ResultProperties,
            <Out> ByRef errorMessage As String,
            Optional atLineNumber As Integer = -1,
            Optional includeSymbols As Boolean = True) As CompilationTestData

            Dim compilation0 = CreateCompilationWithReferences(
                {Parse(source)},
                {MscorlibRef_v4_0_30316_17626, SystemRef, MsvbRef},
                options:=If(outputKind = OutputKind.DynamicallyLinkedLibrary, TestOptions.DebugDll, TestOptions.DebugExe))

            Dim runtime = CreateRuntimeInstance(compilation0, debugFormat:=If(includeSymbols, DebugInformationFormat.Pdb, Nothing))
            Dim context = CreateMethodContext(runtime, methodName, atLineNumber)
            Dim testData = New CompilationTestData()
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim result = context.CompileExpression(
                    expr,
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    resultProperties,
                    errorMessage,
                    missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData)
            Assert.Empty(missingAssemblyIdentities)
            Return testData
        End Function

        ''' <summary>
        ''' Verify all type parameters from the method are from that method or containing types.
        ''' </summary>
        Friend Shared Sub VerifyTypeParameters(method As Symbols.MethodSymbol)
            Assert.True(method.IsContainingSymbolOfAllTypeParameters(method.ReturnType))
            AssertEx.All(method.TypeParameters, Function(typeParameter) method.IsContainingSymbolOfAllTypeParameters(typeParameter))
            AssertEx.All(method.TypeArguments, Function(typeArgument) method.IsContainingSymbolOfAllTypeParameters(typeArgument))
            AssertEx.All(method.Parameters, Function(parameter) method.IsContainingSymbolOfAllTypeParameters(parameter.Type))
            VerifyTypeParameters(method.ContainingType)
        End Sub

        ''' <summary>
        ''' Verify all type parameters from the type are from that type or containing types.
        ''' </summary>
        Friend Shared Sub VerifyTypeParameters(type As Symbols.NamedTypeSymbol)
            AssertEx.All(type.TypeParameters, Function(typeParameter) type.IsContainingSymbolOfAllTypeParameters(typeParameter))
            AssertEx.All(type.TypeArguments, Function(typeArgument) type.IsContainingSymbolOfAllTypeParameters(typeArgument))
            Dim container = type.ContainingType
            If container IsNot Nothing Then
                VerifyTypeParameters(container)
            End If
        End Sub

        Friend Shared Function MakeSources(source As String, Optional assemblyName As String = Nothing) As XElement
            Return <compilation name=<%= If(assemblyName, ExpressionCompilerUtilities.GenerateUniqueName()) %>>
                       <file name="a.vb">
                           <%= source %>
                       </file>
                   </compilation>
        End Function

        Friend Shared Function GetAllXmlReferences() As ImmutableArray(Of MetadataReference)
            Dim builder = ArrayBuilder(Of MetadataReference).GetInstance()
            builder.Add(MscorlibRef)
            builder.Add(MsvbRef)
            builder.AddRange(XmlReferences)
            Return builder.ToImmutableAndFree()
        End Function

        Friend Shared Sub VerifyLocal(
            testData As CompilationTestData,
            typeName As String,
            localAndMethod As LocalAndMethod,
            expectedMethodName As String,
            expectedLocalName As String,
            Optional expectedLocalDisplayName As String = Nothing,
            Optional expectedFlags As DkmClrCompilationResultFlags = DkmClrCompilationResultFlags.None,
            Optional expectedILOpt As String = Nothing,
            Optional expectedGeneric As Boolean = False,
            <CallerFilePath> Optional expectedValueSourcePath As String = Nothing,
            <CallerLineNumber> Optional expectedValueSourceLine As Integer = 0)

            ExpressionCompilerTestHelpers.VerifyLocal(Of MethodSymbol)(
                testData,
                typeName,
                localAndMethod,
                expectedMethodName,
                expectedLocalName,
                If(expectedLocalDisplayName, expectedLocalName),
                expectedFlags,
                AddressOf VerifyTypeParameters,
                expectedILOpt,
                expectedGeneric,
                expectedValueSourcePath,
                expectedValueSourceLine)
        End Sub

        Friend Shared Function GetMethodOrTypeBySignature(compilation As Compilation, signature As String) As Symbol
            Dim parameterTypeNames() As String = Nothing
            Dim methodOrTypeName = ExpressionCompilerTestHelpers.GetMethodOrTypeSignatureParts(signature, parameterTypeNames)

            Dim candidates = compilation.GetMembers(methodOrTypeName)
            Dim methodOrType = If(parameterTypeNames Is Nothing,
                candidates.FirstOrDefault(),
                candidates.FirstOrDefault(Function(c) parameterTypeNames.SequenceEqual(DirectCast(c, MethodSymbol).Parameters.Select(Function(p) p.Type.Name))))

            Assert.False(methodOrType Is Nothing, "Could not find method or type with signature '" + signature + "'.")
            Return methodOrType
        End Function

        Friend Shared Function VariableAlias(name As String, Optional type As Type = Nothing) As [Alias]
            Return VariableAlias(name, If(type, GetType(Object)).AssemblyQualifiedName)
        End Function

        Friend Shared Function VariableAlias(name As String, typeAssemblyQualifiedName As String) As [Alias]
            Return New [Alias](DkmClrAliasKind.Variable, name, name, typeAssemblyQualifiedName, Nothing, Nothing)
        End Function

        Friend Shared Function ObjectIdAlias(id As UInteger, Optional type As Type = Nothing) As [Alias]
            Return ObjectIdAlias(id, If(type, GetType(Object)).AssemblyQualifiedName)
        End Function

        Friend Shared Function ObjectIdAlias(id As UInteger, typeAssemblyQualifiedName As String) As [Alias]
            Assert.NotEqual(Of UInteger)(0, id) ' Not a valid id.
            Dim name = $"${id}"
            Return New [Alias](DkmClrAliasKind.ObjectId, name, name, typeAssemblyQualifiedName, Nothing, Nothing)
        End Function

        Friend Shared Function ReturnValueAlias(Optional id As Integer = -1, Optional type As Type = Nothing) As [Alias]
            Return ReturnValueAlias(id, If(type, GetType(Object)).AssemblyQualifiedName)
        End Function

        Friend Shared Function ReturnValueAlias(id As Integer, typeAssemblyQualifiedName As String) As [Alias]
            Dim name = $"Method M{If(id < 0, "", id.ToString())} returned"
            Dim fullName = If(id < 0, "$ReturnValue", $"$ReturnValue{id}")
            Return New [Alias](DkmClrAliasKind.ReturnValue, name, fullName, typeAssemblyQualifiedName, Nothing, Nothing)
        End Function

        Friend Shared Function ExceptionAlias(Optional type As Type = Nothing, Optional stowed As Boolean = False) As [Alias]
            Return ExceptionAlias(If(type, GetType(Exception)).AssemblyQualifiedName, stowed)
        End Function

        Friend Shared Function ExceptionAlias(typeAssemblyQualifiedName As String, Optional stowed As Boolean = False) As [Alias]
            Dim fullName = If(stowed, "$stowedexception", "$exception")
            Const name = "Error"
            Dim kind = If(stowed, DkmClrAliasKind.StowedException, DkmClrAliasKind.Exception)
            Return New [Alias](kind, name, fullName, typeAssemblyQualifiedName, Nothing, Nothing)
        End Function

        Friend Shared Function GetMethodDebugInfo(runtime As RuntimeInstance, qualifiedMethodName As String, Optional ilOffset As Integer = 0) As MethodDebugInfo(Of TypeSymbol, LocalSymbol)
            Dim peCompilation = runtime.Modules.SelectAsArray(Function(m) m.MetadataBlock).ToCompilation()
            Dim peMethod = peCompilation.GlobalNamespace.GetMember(Of PEMethodSymbol)(qualifiedMethodName)
            Dim peModule = DirectCast(peMethod.ContainingModule, PEModuleSymbol)
            Dim symReader = runtime.Modules.Single(Function(mi) mi.ModuleVersionId = peModule.Module.GetModuleVersionIdOrThrow()).SymReader
            Dim symbolProvider = New VisualBasicEESymbolProvider(peModule, peMethod)
            Return MethodDebugInfo(Of TypeSymbol, LocalSymbol).ReadMethodDebugInfo(DirectCast(symReader, ISymUnmanagedReader3), symbolProvider, MetadataTokens.GetToken(peMethod.Handle), methodVersion:=1, ilOffset:=ilOffset, isVisualBasicMethod:=True)
        End Function

        Friend Shared Function GetTupleElementNamesAttributeIfAny(method As IMethodSymbol) As SynthesizedAttributeData
            Return GetAttributeIfAny(method, "System.Runtime.CompilerServices.TupleElementNamesAttribute")
        End Function

        Friend Shared Function GetAttributeIfAny(method As IMethodSymbol, typeName As String) As SynthesizedAttributeData
            Return method.GetSynthesizedAttributes(forReturnType:=True).
                Where(Function(a) a.AttributeClass.ToTestDisplayString() = typeName).
                SingleOrDefault()
        End Function
    End Class
End Namespace
