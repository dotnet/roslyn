' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SynthesizedPrivateImplementationDetailsSharedConstructor
        Inherits SynthesizedGlobalMethodBase

        Private ReadOnly _containingModule As SourceModuleSymbol
        Private ReadOnly _privateImplementationType As PrivateImplementationDetails
        Private ReadOnly _voidType As TypeSymbol

        Friend Sub New(
            containingModule As SourceModuleSymbol,
            privateImplementationType As PrivateImplementationDetails,
            voidType As NamedTypeSymbol
        )
            MyBase.New(containingModule, WellKnownMemberNames.StaticConstructorName, privateImplementationType)

            _containingModule = containingModule
            _privateImplementationType = privateImplementationType
            _voidType = voidType
        End Sub

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _voidType
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.SharedConstructor
            End Get
        End Property

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            methodBodyBinder = Nothing

            Dim factory As New SyntheticBoundNodeFactory(Me, Me, VisualBasicSyntaxTree.Dummy.GetRoot(), compilationState, diagnostics)
            Dim body As ArrayBuilder(Of BoundStatement) = ArrayBuilder(Of BoundStatement).GetInstance()

            ' Initialize the payload root for each kind of dynamic analysis instrumentation.
            ' A payload root is an array of arrays of per-method instrumentation payloads.
            ' For each kind of instrumentation:
            '
            '     payloadRoot = New T(MaximumMethodDefIndex)() {}
            '
            ' where T Is the type of the payload at each instrumentation point, and MaximumMethodDefIndex is the
            ' index portion of the greatest method definition token in the compilation. This guarantees that any
            ' method can use the index portion of its own method definition token as an index into the payload array.

            For Each payloadRoot As KeyValuePair(Of Integer, InstrumentationPayloadRootField) In _privateImplementationType.GetInstrumentationPayloadRoots()

                Dim analysisKind As Integer = payloadRoot.Key
                Dim payloadArrayType As ArrayTypeSymbol = DirectCast(payloadRoot.Value.Type.GetInternalSymbol(), ArrayTypeSymbol)

                body.Add(
                    factory.Assignment(
                        factory.InstrumentationPayloadRoot(analysisKind, payloadArrayType, isLValue:=True),
                        factory.Array(payloadArrayType.ElementType, ImmutableArray.Create(factory.MaximumMethodDefIndex()), ImmutableArray(Of BoundExpression).Empty)))
            Next

            ' Initialize the module version ID (MVID) field. Dynamic instrumentation requires the MVID of the executing module, and this field makes that accessible.
            ' MVID = New Guid(ModuleVersionIdString)

            Dim guidConstructor As MethodSymbol = factory.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Guid__ctor)
            If guidConstructor IsNot Nothing Then
                body.Add(
                    factory.Assignment(
                       factory.ModuleVersionId(isLValue:=True),
                       factory.[New](guidConstructor, factory.ModuleVersionIdString())))
            End If

            body.Add(factory.Return())

            Return factory.Block(body.ToImmutableAndFree())
        End Function

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return _containingModule
            End Get
        End Property
    End Class
End Namespace
