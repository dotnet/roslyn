' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports Microsoft.CodeAnalysis.CodeGen

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SynthesizedPrivateImplementationDetailsSharedConstructor
        Inherits SynthesizedConstructorSymbol

        Private _containingModule As SourceModuleSymbol
        Private _privateImplementationType As PrivateImplementationDetails

        Friend Sub New(
            containingModule As SourceModuleSymbol,
            privateImplementationType As PrivateImplementationDetails,
            diagnostics As DiagnosticBag,
            voidType As NamedTypeSymbol
        )
            MyBase.New(Nothing, Nothing, True, False, Nothing, diagnostics)

            _containingModule = containingModule
            _privateImplementationType = privateImplementationType
        End Sub

        Friend Overrides Function GetBoundMethodBody(diagnostics As DiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            methodBodyBinder = Nothing

            ' Initialize the payload root for for each kind of dynamic analysis instrumentation.
            ' A payload root Is an array of arrays of per-method instrumentation payloads.
            ' For each kind of instrumentation:
            '
            '     payloadRoot = New T(MaximumMethodDefIndex)() {}
            '
            ' where T Is the type of the payload at each instrumentation point, And MaximumMethodDefIndex Is the 
            ' index portion of the greatest method definition token in the compilation. This guarantees that any
            ' method can use the index portion of its own method definition token as an index into the payload array.

            Dim payloadRootFields As IReadOnlyCollection(Of KeyValuePair(Of Integer, InstrumentationPayloadRootField)) = _privateImplementationType.GetInstrumentationPayloadRoots()
            Debug.Assert(payloadRootFields.Count > 0)

            Dim body As ArrayBuilder(Of BoundStatement) = ArrayBuilder(Of BoundStatement).GetInstance(2 + payloadRootFields.Count)

            For Each payloadRoot As KeyValuePair(Of Integer, InstrumentationPayloadRootField) In payloadRootFields.OrderBy(Function(analysis) analysis.Key)

                Dim analysisKind As Integer = payloadRoot.Key
                Dim payloadArrayType As ArrayTypeSymbol = DirectCast(payloadRoot.Value.Type, ArrayTypeSymbol)

                Dim maxMethodIndex As BoundExpression = New BoundMaximumMethodDefIndex(Syntax, factory.SpecialType(SpecialType.System_Int32))
                maxMethodIndex.SetWasCompilerGenerated()

                Dim payloadArray As BoundExpression = New BoundArrayCreation(Syntax, ImmutableArray.Create(maxMethodIndex), Nothing, payloadArrayType)
                payloadArray.SetWasCompilerGenerated()

                Dim payloadRootReference As BoundExpression = New BoundInstrumentationPayloadRoot(Syntax, analysisKind, payloadArrayType)
                payloadRootReference.SetWasCompilerGenerated()

                Dim payloadAssignment As BoundExpression = New BoundAssignmentOperator(Syntax, payloadRootReference, payloadArray, True)
                payloadAssignment.SetWasCompilerGenerated()

                Dim payloadAssignmentStatement As BoundStatement = New BoundExpressionStatement(Syntax, payloadAssignment)
                payloadAssignmentStatement.SetWasCompilerGenerated()

                body.Add(payloadAssignmentStatement)
            Next

            ' Initialize the module version ID (MVID) field. Dynamic instrumentation requires the MVID of the executing module, and this field makes that accessible.
            ' MVID = Guid.Parse(ModuleVersionIdString)

            Dim moduleVersionIdString As BoundExpression = New BoundModuleVersionIdString(Syntax, factory.SpecialType(SpecialType.System_String))
            moduleVersionIdString.SetWasCompilerGenerated()

            Dim guidParse As BoundExpression = New BoundCall(Syntax, factory.WellKnownMethod(WellKnownMember.System_Guid__Parse), Nothing, Nothing, ImmutableArray.Create(moduleVersionIdString), Nothing, True, factory.WellKnownType(WellKnownType.System_Guid))
            guidParse.SetWasCompilerGenerated()

            Dim moduleVersionId As BoundExpression = New BoundModuleVersionId(Syntax, factory.WellKnownType(WellKnownType.System_Guid))
            moduleVersionId.SetWasCompilerGenerated()

            Dim moduleVersionIdAssignment As BoundExpression = New BoundAssignmentOperator(Syntax, moduleVersionId, guidParse, True)
            moduleVersionIdAssignment.SetWasCompilerGenerated()

            Dim moduleVersionIdAssignmentStatement As BoundStatement = New BoundExpressionStatement(Syntax, moduleVersionIdAssignment)
            moduleVersionIdAssignmentStatement.SetWasCompilerGenerated()

            body.Add(moduleVersionIdAssignmentStatement)

            Dim returnStatement = New BoundReturnStatement(Me.Syntax, Nothing, Nothing, Nothing)
            returnStatement.SetWasCompilerGenerated()
            body.Add(returnStatement)

            Return New BoundBlock(Me.Syntax, Nothing, ImmutableArray(Of LocalSymbol).Empty, body.ToImmutableAndFree())
        End Function
    End Class
End Namespace
