' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a compiler generated "Main" sub.
    ''' </summary>
    Friend Class SynthesizedMainTypeEntryPoint
        Inherits SynthesizedRegularMethodBase

        Public Sub New(syntaxNode As VisualBasicSyntaxNode, container As SourceNamedTypeSymbol)
            MyBase.New(syntaxNode, container, WellKnownMemberNames.EntryPointMethodName, isShared:=True)
        End Sub

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Public
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                ' No reason to check Void type for errors because we require presence of constructor,
                ' which will complain.
                Return ContainingAssembly.GetSpecialType(SpecialType.System_Void)
            End Get
        End Property

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, <Out()> Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            methodBodyBinder = Nothing

            Dim syntaxNode As SyntaxNode = Me.Syntax
            Dim container = DirectCast(ContainingSymbol, SourceNamedTypeSymbol)
            Dim binder As Binder = BinderBuilder.CreateBinderForType(container.ContainingSourceModule, syntaxNode.SyntaxTree, container)

            Debug.Assert(binder.IsDefaultInstancePropertyAllowed)
            Dim defaultInstancePropertyDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=False, withDependencies:=diagnostics.AccumulatesDependencies)
            Dim instance As BoundExpression = binder.TryDefaultInstanceProperty(New BoundTypeExpression(syntaxNode, container), defaultInstancePropertyDiagnostics)

            If instance Is Nothing Then
                ' Default instance is not available, create instance by invoking constructor.
                instance = binder.BindObjectCreationExpression(syntaxNode, container, ImmutableArray(Of BoundExpression).Empty, diagnostics)
            Else
                diagnostics.AddDependencies(defaultInstancePropertyDiagnostics)
            End If

            defaultInstancePropertyDiagnostics.Free()

            ' Call System.Windows.Forms.Application.Run(<instance>)
            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = Nothing
            Dim runMethod = DirectCast(Binder.GetWellKnownTypeMember(container.DeclaringCompilation, WellKnownMember.System_Windows_Forms_Application__RunForm, useSiteInfo), MethodSymbol)
            Dim statement As BoundStatement

            If Not Binder.ReportUseSite(diagnostics, syntaxNode, useSiteInfo) Then
                statement = binder.BindInvocationExpression(syntaxNode, syntaxNode, TypeCharacter.None,
                                                            New BoundMethodGroup(syntaxNode, Nothing, ImmutableArray.Create(runMethod), LookupResultKind.Good, Nothing, QualificationKind.QualifiedViaTypeName),
                                                            ImmutableArray.Create(instance), Nothing, diagnostics,
                                                            callerInfoOpt:=Nothing).ToStatement()
            Else
                statement = New BoundBadStatement(syntaxNode, ImmutableArray(Of BoundNode).Empty, hasErrors:=True)
            End If

            Return New BoundBlock(syntaxNode, Nothing, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(statement, New BoundReturnStatement(syntaxNode, Nothing, Nothing, Nothing)))
        End Function

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            AddSynthesizedAttribute(attributes,
                                    DeclaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_STAThreadAttribute__ctor))
        End Sub

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace

