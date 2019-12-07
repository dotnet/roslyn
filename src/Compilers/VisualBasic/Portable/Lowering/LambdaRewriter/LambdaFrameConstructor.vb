' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class SynthesizedLambdaConstructor
        Inherits SynthesizedMethod
        Implements ISynthesizedMethodBodyImplementationSymbol

        Friend Sub New(
            syntaxNode As SyntaxNode,
            containingType As LambdaFrame
        )
            MyBase.New(syntaxNode, containingType, WellKnownMemberNames.InstanceConstructorName, False)
        End Sub

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Constructor
            End Get
        End Property

        Friend Function AsMember(frameType As NamedTypeSymbol) As MethodSymbol
            ' ContainingType is always a Frame here which is a type definition so we can use "Is"
            If frameType Is ContainingType Then
                Return Me
            End If

            Dim substituted = DirectCast(frameType, SubstitutedNamedType)
            Return DirectCast(substituted.GetMemberForDefinition(Me), MethodSymbol)
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return False
        End Function

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            ' Dev11 adds DebuggerNonUserCode; there is no reason to do so since:
            ' - we emit no debug info for the body
            ' - the code doesn't call any user code that could inspect the stack and find the accessor's frame
            ' - the code doesn't throw exceptions whose stack frames we would need to hide
            ' 
            ' C# also doesn't add DebuggerHidden nor DebuggerNonUserCode attributes.
        End Sub

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbolInternal Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Dim symbol As ISynthesizedMethodBodyImplementationSymbol = CType(ContainingSymbol, ISynthesizedMethodBodyImplementationSymbol)
                Return symbol.Method
            End Get
        End Property
    End Class
End Namespace
