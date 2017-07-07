﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Partial Private NotInheritable Class AnonymousTypeGetHashCodeMethodSymbol
            Inherits SynthesizedRegularMethodBase

            Public Sub New(container As AnonymousTypeTemplateSymbol)
                MyBase.New(VisualBasicSyntaxTree.Dummy.GetRoot(), container, WellKnownMemberNames.ObjectGetHashCode)
            End Sub

            Private ReadOnly Property AnonymousType As AnonymousTypeTemplateSymbol
                Get
                    Return DirectCast(Me.m_containingType, AnonymousTypeTemplateSymbol)
                End Get
            End Property

            Public Overrides ReadOnly Property IsOverrides As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property OverriddenMethod As MethodSymbol
                Get
                    Return Me.AnonymousType.Manager.System_Object__GetHashCode
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return Accessibility.Public
                End Get
            End Property

            Public Overrides ReadOnly Property IsSub As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Return AnonymousType.Manager.System_Int32
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(compilationState, attributes)

                Dim compilation = DirectCast(Me.ContainingType, AnonymousTypeTemplateSymbol).Manager.Compilation
                AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerHiddenAttribute())
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
    End Class
End Namespace
