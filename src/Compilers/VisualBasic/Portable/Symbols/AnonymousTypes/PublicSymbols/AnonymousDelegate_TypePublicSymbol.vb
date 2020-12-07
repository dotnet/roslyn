' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend NotInheritable Class AnonymousTypeManager

        Friend NotInheritable Class AnonymousDelegatePublicSymbol
            Inherits AnonymousTypeOrDelegatePublicSymbol

            Private ReadOnly _members As ImmutableArray(Of SynthesizedDelegateMethodSymbol)

            Public Sub New(manager As AnonymousTypeManager, typeDescr As AnonymousTypeDescriptor)
                MyBase.New(manager, typeDescr)

                Debug.Assert(typeDescr.IsImplicitlyDeclared)
                Debug.Assert(typeDescr.Parameters.Length > 0)
                Debug.Assert(typeDescr.Parameters.Last().Name Is AnonymousTypeDescriptor.FunctionReturnParameterName OrElse
                             typeDescr.Parameters.Last().Name Is AnonymousTypeDescriptor.SubReturnParameterName)

                Dim parameterDescriptors As ImmutableArray(Of AnonymousTypeField) = typeDescr.Parameters
                Dim returnType As TypeSymbol = If(parameterDescriptors.IsSubDescription(), DirectCast(manager.System_Void, TypeSymbol), parameterDescriptors.Last.Type)
                Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance(parameterDescriptors.Length + 1)
                Dim i As Integer

                ' A delegate has the following members: (see CLI spec 13.6)
                ' (1) a method named Invoke with the specified signature
                Dim delegateInvoke = New SynthesizedDelegateMethodSymbol(WellKnownMemberNames.DelegateInvokeName,
                                                                         Me,
                                                                         SourceNamedTypeSymbol.DelegateCommonMethodFlags Or SourceMemberFlags.MethodKindDelegateInvoke,
                                                                         returnType)

                For i = 0 To parameterDescriptors.Length - 2
                    parameters.Add(ParameterFromField(delegateInvoke, parameterDescriptors(i), i))
                Next

                delegateInvoke.SetParameters(parameters.ToImmutable())
                parameters.Clear()

                ' (2) a constructor with argument types (object, System.IntPtr)
                Dim delegateCtor = New SynthesizedDelegateMethodSymbol(
                                                WellKnownMemberNames.InstanceConstructorName, Me,
                                                SourceNamedTypeSymbol.DelegateConstructorMethodFlags, manager.System_Void)
                delegateCtor.SetParameters(
                    ImmutableArray.Create(Of ParameterSymbol)(
                           New SynthesizedParameterSymbol(delegateCtor, manager.System_Object, 0,
                                                          False, StringConstants.DelegateConstructorInstanceParameterName),
                           New SynthesizedParameterSymbol(delegateCtor, manager.System_IntPtr, 1,
                                                          False, StringConstants.DelegateConstructorMethodParameterName)
                           ))

                Dim delegateBeginInvoke As SynthesizedDelegateMethodSymbol
                Dim delegateEndInvoke As SynthesizedDelegateMethodSymbol

                ' Don't add Begin/EndInvoke members to winmd compilations.
                ' Invoke must be the last member, regardless.
                If Me.IsCompilationOutputWinMdObj() Then
                    delegateBeginInvoke = Nothing
                    delegateEndInvoke = Nothing

                    parameters.Free()

                    _members = ImmutableArray.Create(delegateCtor, delegateInvoke)
                Else
                    ' (3) BeginInvoke
                    delegateBeginInvoke = New SynthesizedDelegateMethodSymbol(
                                                        WellKnownMemberNames.DelegateBeginInvokeName, Me,
                                                        SourceNamedTypeSymbol.DelegateCommonMethodFlags Or SourceMemberFlags.MethodKindOrdinary,
                                                        manager.System_IAsyncResult)

                    For i = 0 To parameterDescriptors.Length - 2
                        parameters.Add(ParameterFromField(delegateBeginInvoke, parameterDescriptors(i), i))
                    Next

                    parameters.Add(New SynthesizedParameterSymbol(delegateBeginInvoke, manager.System_AsyncCallback, i,
                                                                  False, StringConstants.DelegateMethodCallbackParameterName))
                    i += 1
                    parameters.Add(New SynthesizedParameterSymbol(delegateBeginInvoke, manager.System_Object, i,
                                                                  False, StringConstants.DelegateMethodInstanceParameterName))
                    delegateBeginInvoke.SetParameters(parameters.ToImmutable())
                    parameters.Clear()

                    ' and (4) EndInvoke methods
                    delegateEndInvoke = New SynthesizedDelegateMethodSymbol(
                                                        WellKnownMemberNames.DelegateEndInvokeName, Me,
                                                        SourceNamedTypeSymbol.DelegateCommonMethodFlags Or SourceMemberFlags.MethodKindOrdinary,
                                                        returnType)

                    Dim ordinal As Integer = 0
                    For i = 0 To parameterDescriptors.Length - 2
                        If parameterDescriptors(i).IsByRef Then
                            parameters.Add(ParameterFromField(delegateEndInvoke, parameterDescriptors(i), ordinal))
                            ordinal += 1
                        End If
                    Next

                    parameters.Add(New SynthesizedParameterSymbol(delegateEndInvoke, manager.System_IAsyncResult, ordinal,
                                                                  False, StringConstants.DelegateMethodResultParameterName))
                    delegateEndInvoke.SetParameters(parameters.ToImmutableAndFree())

                    _members = ImmutableArray.Create(delegateCtor, delegateBeginInvoke, delegateEndInvoke, delegateInvoke)
                End If

#If DEBUG Then
                For Each m In _members
                    Debug.Assert(m IsNot Nothing)
                Next
#End If
            End Sub

            Public Overrides ReadOnly Property TypeKind As TypeKind
                Get
                    Return TypeKind.Delegate
                End Get
            End Property

            Friend Overrides ReadOnly Property IsInterface As Boolean
                Get
                    Return False
                End Get
            End Property

            Private Shared Function ParameterFromField(container As SynthesizedDelegateMethodSymbol, field As AnonymousTypeField, ordinal As Integer) As ParameterSymbol
                Return New SynthesizedParameterWithLocationSymbol(container, field.Type, ordinal, field.IsByRef, field.Name, field.Location)
            End Function

            Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
                Return StaticCast(Of Symbol).From(_members)
            End Function

            Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
                Dim newDescriptor As New AnonymousTypeDescriptor
                If Not Me.TypeDescriptor.SubstituteTypeParametersIfNeeded(substitution, newDescriptor) Then
                    Return New TypeWithModifiers(Me)
                End If

                Return New TypeWithModifiers(Me.Manager.ConstructAnonymousDelegateSymbol(newDescriptor))
            End Function

            Public Overrides Function MapToImplementationSymbol() As NamedTypeSymbol
                Return Me.Manager.ConstructAnonymousDelegateImplementationSymbol(Me)
            End Function

            Friend Overrides Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
                Return Manager.System_MulticastDelegate
            End Function

            Friend Overrides Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overrides ReadOnly Property DelegateInvokeMethod As MethodSymbol
                Get
                    ' In both regular and winmd, the last member is invoke
                    Return _members(_members.Length - 1)
                End Get
            End Property

        End Class

    End Class
End Namespace
