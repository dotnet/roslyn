' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedEvent
        Inherits EmbeddedTypesManager.CommonEmbeddedEvent

        Public Sub New(underlyingEvent As EventSymbol, adder As EmbeddedMethod, remover As EmbeddedMethod, caller As EmbeddedMethod)
            MyBase.New(underlyingEvent, adder, remover, caller)
        End Sub

        Protected Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingEvent.GetCustomAttributesToEmit(moduleBuilder.CompilationState)
        End Function

        Protected Overrides ReadOnly Property IsRuntimeSpecial As Boolean
            Get
                Return UnderlyingEvent.HasRuntimeSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSpecialName As Boolean
            Get
                Return UnderlyingEvent.HasSpecialName
            End Get
        End Property

        Protected Overrides Function [GetType](moduleBuilder As PEModuleBuilder, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As Cci.ITypeReference
            Return moduleBuilder.Translate(UnderlyingEvent.Type, syntaxNodeOpt, diagnostics)
        End Function

        Protected Overrides ReadOnly Property ContainingType As EmbeddedType
            Get
                Return AnAccessor.ContainingType
            End Get
        End Property

        Protected Overrides ReadOnly Property Visibility As Cci.TypeMemberVisibility
            Get
                Return PEModuleBuilder.MemberVisibility(UnderlyingEvent)
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingEvent.MetadataName
            End Get
        End Property

        Protected Overrides Sub EmbedCorrespondingComEventInterfaceMethodInternal(syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag, isUsedForComAwareEventBinding As Boolean)
            ' If the event happens to belong to a class with a ComEventInterfaceAttribute, there will also be
            ' a paired method living on its source interface. The ComAwareEventInfo class expects to find this 
            ' method through reflection. If we embed an event, therefore, we must ensure that the associated source
            ' interface method is also included, even if it is not otherwise referenced in the embedding project.
            Dim underlyingContainingType = ContainingType.UnderlyingNamedType

            For Each attrData In underlyingContainingType.GetAttributes()
                If attrData.IsTargetAttribute(underlyingContainingType, AttributeDescription.ComEventInterfaceAttribute) Then
                    Dim foundMatch = False
                    Dim sourceInterface As NamedTypeSymbol = Nothing

                    If attrData.CommonConstructorArguments.Length = 2 Then
                        sourceInterface = TryCast(attrData.CommonConstructorArguments(0).ValueInternal, NamedTypeSymbol)
                        If sourceInterface IsNot Nothing Then
                            foundMatch = EmbedMatchingInterfaceMethods(sourceInterface, syntaxNodeOpt, diagnostics)

                            For Each source In sourceInterface.AllInterfacesNoUseSiteDiagnostics
                                If EmbedMatchingInterfaceMethods(source, syntaxNodeOpt, diagnostics) Then
                                    foundMatch = True
                                End If
                            Next
                        End If
                    End If

                    If Not foundMatch AndAlso isUsedForComAwareEventBinding Then
                        If sourceInterface Is Nothing Then
                            ' ERRID_SourceInterfaceMustBeInterface/ERR_MissingSourceInterface
                            EmbeddedTypesManager.ReportDiagnostic(diagnostics, ERRID.ERR_SourceInterfaceMustBeInterface, syntaxNodeOpt, underlyingContainingType, UnderlyingEvent)
                        Else
                            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                            sourceInterface.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                            diagnostics.Add(If(syntaxNodeOpt Is Nothing, NoLocation.Singleton, syntaxNodeOpt.GetLocation()), useSiteDiagnostics)

                            ' ERRID_EventNoPIANoBackingMember/ERR_MissingMethodOnSourceInterface
                            EmbeddedTypesManager.ReportDiagnostic(diagnostics, ERRID.ERR_EventNoPIANoBackingMember, syntaxNodeOpt, sourceInterface, UnderlyingEvent.MetadataName, UnderlyingEvent)
                        End If
                    End If

                    Exit For
                End If
            Next
        End Sub

        Private Function EmbedMatchingInterfaceMethods(sourceInterface As NamedTypeSymbol, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As Boolean
            Dim foundMatch = False
            For Each m In sourceInterface.GetMembers(UnderlyingEvent.MetadataName)
                If m.Kind = SymbolKind.Method Then
                    TypeManager.EmbedMethodIfNeedTo(DirectCast(m, MethodSymbol), syntaxNodeOpt, diagnostics)
                    foundMatch = True
                End If
            Next
            Return foundMatch
        End Function

    End Class

End Namespace
