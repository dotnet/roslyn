' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a compiler "MyGroupCollection" property accessor.
    ''' </summary>
    Friend MustInherit Class SynthesizedMyGroupCollectionPropertyAccessorSymbol
        Inherits SynthesizedPropertyAccessorBase(Of SynthesizedMyGroupCollectionPropertySymbol)

        Private ReadOnly _createOrDisposeMethod As String

        Public Sub New(container As SourceNamedTypeSymbol, [property] As SynthesizedMyGroupCollectionPropertySymbol, createOrDisposeMethod As String)
            MyBase.New(container, [property])
            Debug.Assert(createOrDisposeMethod IsNot Nothing AndAlso createOrDisposeMethod.Length > 0)
            _createOrDisposeMethod = createOrDisposeMethod
        End Sub

        Friend Overrides ReadOnly Property BackingFieldSymbol As FieldSymbol
            Get
                Return PropertyOrEvent.AssociatedField
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            ' Note, Dev11 emits DebuggerNonUserCodeAttribute, but we are using DebuggerHiddenAttribute instead.
            AddSynthesizedAttribute(attributes, Me.DeclaringCompilation.SynthesizeDebuggerHiddenAttribute())
        End Sub

        Private Shared Function MakeSafeName(name As String) As String
            If SyntaxFacts.GetKeywordKind(name) <> SyntaxKind.None Then
                Return "[" & name & "]"
            End If

            Return name
        End Function

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, <Out()> Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock

            Dim containingType = DirectCast(Me.ContainingType, SourceNamedTypeSymbol)
            Dim containingTypeName As String = MakeSafeName(containingType.Name)

            Dim targetTypeName As String = PropertyOrEvent.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            Debug.Assert(targetTypeName.StartsWith("Global.", StringComparison.Ordinal))

            Dim propertyName As String = MakeSafeName(PropertyOrEvent.Name)

            Dim fieldName As String = PropertyOrEvent.AssociatedField.Name

            Dim codeToParse As String =
                "Partial Class " & containingTypeName & vbCrLf &
                    "Property " & propertyName & vbCrLf &
                        GetMethodBlock(fieldName, MakeSafeName(_createOrDisposeMethod), targetTypeName) &
                    "End Property" & vbCrLf &
                "End Class" & vbCrLf

            ' TODO: It looks like Dev11 respects project level conditional compilation here.
            Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.From(codeToParse, Encoding.UTF8, SourceHashAlgorithms.Default))
            Dim attributeSyntax = PropertyOrEvent.AttributeSyntax.GetVisualBasicSyntax()
            Dim diagnosticLocation As Location = attributeSyntax.GetLocation()
            Dim root As CompilationUnitSyntax = tree.GetCompilationUnitRoot()
            Dim hasErrors As Boolean = False

            For Each diag As Diagnostic In tree.GetDiagnostics(root)
                Dim vbdiag = DirectCast(diag, VBDiagnostic)
                Debug.Assert(Not vbdiag.HasLazyInfo,
                             "If we decide to allow lazy syntax diagnostics, we'll have to check all call sites of SyntaxTree.GetDiagnostics")

                diagnostics.Add(vbdiag.WithLocation(diagnosticLocation))

                If diag.Severity = DiagnosticSeverity.Error Then
                    hasErrors = True
                End If
            Next

            Dim classBlock = DirectCast(root.Members(0), ClassBlockSyntax)
            Dim propertyBlock = DirectCast(classBlock.Members(0), PropertyBlockSyntax)
            Dim accessorBlock As AccessorBlockSyntax = propertyBlock.Accessors(0)

            Dim boundStatement As BoundStatement

            If hasErrors Then
                boundStatement = New BoundBadStatement(accessorBlock, ImmutableArray(Of BoundNode).Empty)
            Else
                Dim typeBinder As Binder = BinderBuilder.CreateBinderForType(containingType.ContainingSourceModule, PropertyOrEvent.AttributeSyntax.SyntaxTree, containingType)
                methodBodyBinder = BinderBuilder.CreateBinderForMethodBody(Me, accessorBlock, typeBinder)

                Dim bindingDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=diagnostics.AccumulatesDependencies)
#If DEBUG Then
                ' Enable DEBUG check for ordering of simple name binding.
                methodBodyBinder.EnableSimpleNameBindingOrderChecks(True)
#End If

                boundStatement = methodBodyBinder.BindStatement(accessorBlock, bindingDiagnostics)

#If DEBUG Then
                methodBodyBinder.EnableSimpleNameBindingOrderChecks(False)
#End If

                For Each diag As VBDiagnostic In bindingDiagnostics.DiagnosticBag.AsEnumerable()
                    diagnostics.Add(diag.WithLocation(diagnosticLocation))
                Next

                diagnostics.AddDependencies(bindingDiagnostics)
                bindingDiagnostics.Free()

                If boundStatement.Kind = BoundKind.Block Then
                    Return DirectCast(boundStatement, BoundBlock)
                End If
            End If

            Return New BoundBlock(accessorBlock, Nothing, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(Of BoundStatement)(boundStatement))
        End Function

        Friend NotOverridable Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected MustOverride Function GetMethodBlock(fieldName As String, createOrDisposeMethodName As String, targetTypeName As String) As String

    End Class

    Friend Class SynthesizedMyGroupCollectionPropertyGetAccessorSymbol
        Inherits SynthesizedMyGroupCollectionPropertyAccessorSymbol

        Public Sub New(container As SourceNamedTypeSymbol, [property] As SynthesizedMyGroupCollectionPropertySymbol, createMethod As String)
            MyBase.New(container, [property], createMethod)
        End Sub

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.PropertyGet
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return PropertyOrEvent.Type
            End Get
        End Property

        Protected Overrides Function GetMethodBlock(fieldName As String, createMethodName As String, targetTypeName As String) As String
            ' See Bindable::GenMyGroupCollectionGetCode.

            ' Get
            '    <backingField> = <CreateMethod>(Of <TargetType>)(<backingField>)
            '    return <backingField>
            ' End Get

            Return "Get" & vbCrLf &
                       fieldName & " = " & createMethodName & "(Of " & targetTypeName & ")(" & fieldName & ")" & vbCrLf &
                       "Return " & fieldName & vbCrLf &
                   "End Get" & vbCrLf
        End Function
    End Class

    Friend Class SynthesizedMyGroupCollectionPropertySetAccessorSymbol
        Inherits SynthesizedMyGroupCollectionPropertyAccessorSymbol

        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

        Public Sub New(container As SourceNamedTypeSymbol, [property] As SynthesizedMyGroupCollectionPropertySymbol, disposeMethod As String)
            MyBase.New(container, [property], disposeMethod)

            Dim params() As ParameterSymbol = {SynthesizedParameterSymbol.CreateSetAccessorValueParameter(Me, [property], StringConstants.ValueParameterName)}
            _parameters = params.AsImmutableOrNull()
        End Sub

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.PropertySet
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                ' There is no reason to specially complain about missing/bad System.Void because we require presence of constructor,
                ' which also returns void. The error reported on the constructor is sufficient.
                Return ContainingAssembly.GetSpecialType(SpecialType.System_Void)
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Protected Overrides Function GetMethodBlock(fieldName As String, disposeMethodName As String, targetTypeName As String) As String
            ' See Bindable::GenMyGroupCollectionSetCode.

            ' Set(ByVal <Value> As <TargetType>)
            '    If <Value> Is <backingField>
            '        return
            '    End If
            '    If Not <Value> Is Nothing Then
            '        Throw New ArgumentException("Property can only be set to Nothing.")
            '    End If
            '    <DisposeMethod>(Of <TargetType>)(<backingField>)
            ' End Set

            Return "Set(ByVal " & StringConstants.ValueParameterName & " As " & targetTypeName & ")" & vbCrLf &
                       "If " & StringConstants.ValueParameterName & " Is " & fieldName & vbCrLf &
                           "Return" & vbCrLf &
                       "End If" & vbCrLf &
                       "If " & StringConstants.ValueParameterName & " IsNot Nothing Then" & vbCrLf &
                           "Throw New Global.System.ArgumentException(""Property can only be set to Nothing"")" & vbCrLf &
                       "End If" & vbCrLf &
                       disposeMethodName & "(Of " & targetTypeName & ")(" & fieldName & ")" & vbCrLf &
                   "End Set" & vbCrLf
        End Function
    End Class

End Namespace
