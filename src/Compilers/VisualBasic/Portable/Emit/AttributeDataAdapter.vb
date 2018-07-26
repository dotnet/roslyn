' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Partial Class VisualBasicAttributeData
        Implements Cci.ICustomAttribute

        Private Function GetArguments1(context As EmitContext) As ImmutableArray(Of Cci.IMetadataExpression) Implements Cci.ICustomAttribute.GetArguments
            Return CommonConstructorArguments.SelectAsArray(Function(arg) CreateMetadataExpression(arg, context))
        End Function

        Private Function Constructor1(context As EmitContext, reportDiagnostics As Boolean) As Cci.IMethodReference Implements Cci.ICustomAttribute.Constructor
            If Me.AttributeConstructor.IsDefaultValueTypeConstructor() Then
                ' Parameter constructors for structs exist in symbol table, but are not emitted.
                ' Produce an error since we cannot use it (instead of crashing):
                ' Details: https://github.com/dotnet/roslyn/issues/19394

                If reportDiagnostics Then
                    context.Diagnostics.Add(ERRID.ERR_AttributeMustBeClassNotStruct1, If(context.SyntaxNodeOpt?.GetLocation(), NoLocation.Singleton), Me.AttributeClass)
                End If

                Return Nothing
            End If

            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return moduleBeingBuilt.Translate(AttributeConstructor, needDeclaration:=False,
                                              syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private Function GetNamedArguments1(context As EmitContext) As ImmutableArray(Of Cci.IMetadataNamedArgument) Implements Cci.ICustomAttribute.GetNamedArguments
            Return CommonNamedArguments.SelectAsArray(Function(namedArgument) CreateMetadataNamedArgument(namedArgument.Key, namedArgument.Value, context))
        End Function

        Private ReadOnly Property ArgumentCount As Integer Implements Cci.ICustomAttribute.ArgumentCount
            Get
                Return CommonConstructorArguments.Length
            End Get
        End Property

        Private ReadOnly Property NamedArgumentCount As UShort Implements Cci.ICustomAttribute.NamedArgumentCount
            Get
                Return CType(CommonNamedArguments.Length, UShort)
            End Get
        End Property

        Private Function GetType1(context As EmitContext) As Cci.ITypeReference Implements Cci.ICustomAttribute.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return moduleBeingBuilt.Translate(AttributeClass, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property AllowMultiple1 As Boolean Implements Cci.ICustomAttribute.AllowMultiple
            Get
                Return Me.AttributeClass.GetAttributeUsageInfo().AllowMultiple
            End Get
        End Property

        Private Function CreateMetadataExpression(argument As TypedConstant, context As EmitContext) As Cci.IMetadataExpression
            If argument.IsNull Then
                Return CreateMetadataConstant(argument.Type, Nothing, context)
            End If

            Select Case argument.Kind
                Case TypedConstantKind.Array
                    Return CreateMetadataArray(argument, context)
                Case TypedConstantKind.Type
                    Return CreateType(argument, context)
                Case Else
                    Return CreateMetadataConstant(argument.Type, argument.Value, context)
            End Select
        End Function


        Private Function CreateMetadataArray(argument As TypedConstant, context As EmitContext) As MetadataCreateArray
            Debug.Assert(Not argument.Values.IsDefault)

            Dim values = argument.Values
            Dim moduleBeingBuilt = DirectCast(context.Module, PEModuleBuilder)
            Dim arrayType = moduleBeingBuilt.Translate(DirectCast(argument.Type, ArrayTypeSymbol))

            If values.Length = 0 Then
                Return New MetadataCreateArray(arrayType,
                                               arrayType.GetElementType(context),
                                               ImmutableArray(Of Cci.IMetadataExpression).Empty)
            End If

            Dim metadataExprs = New Cci.IMetadataExpression(values.Length - 1) {}
            For i = 0 To values.Length - 1
                metadataExprs(i) = CreateMetadataExpression(values(i), context)
            Next

            Return New MetadataCreateArray(arrayType,
                                           arrayType.GetElementType(context),
                                           metadataExprs.AsImmutableOrNull)
        End Function

        Private Function CreateType(argument As TypedConstant, context As EmitContext) As MetadataTypeOf
            Debug.Assert(argument.Value IsNot Nothing)

            Dim moduleBeingBuilt = DirectCast(context.Module, PEModuleBuilder)
            Dim syntaxNodeOpt = DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode)
            Dim diagnostics = context.Diagnostics
            Return New MetadataTypeOf(moduleBeingBuilt.Translate(DirectCast(argument.Value, TypeSymbol), syntaxNodeOpt, diagnostics),
                                      moduleBeingBuilt.Translate(DirectCast(argument.Type, TypeSymbol), syntaxNodeOpt, diagnostics))
        End Function

        Private Function CreateMetadataConstant(type As ITypeSymbol, value As Object, context As EmitContext) As MetadataConstant
            Dim moduleBeingBuilt = DirectCast(context.Module, PEModuleBuilder)
            Return moduleBeingBuilt.CreateConstant(DirectCast(type, TypeSymbol), value, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function


        Private Function CreateMetadataNamedArgument(name As String, argument As TypedConstant, context As EmitContext) As Cci.IMetadataNamedArgument
            Dim sym = LookupName(name)
            Dim value = CreateMetadataExpression(argument, context)
            Dim type As TypeSymbol
            Dim fieldSymbol = TryCast(sym, FieldSymbol)
            If fieldSymbol IsNot Nothing Then
                type = fieldSymbol.Type
            Else
                type = DirectCast(sym, PropertySymbol).Type
            End If

            Dim moduleBeingBuilt = DirectCast(context.Module, PEModuleBuilder)
            Return New MetadataNamedArgument(sym, moduleBeingBuilt.Translate(type, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics), value)
        End Function

        Private Function LookupName(name As String) As Symbol
            Dim type = AttributeClass
            Do
                For Each member In type.GetMembers(name)
                    If member.DeclaredAccessibility = Accessibility.Public Then
                        Return member
                    End If
                Next

                type = type.BaseTypeNoUseSiteDiagnostics
            Loop While type IsNot Nothing

            Debug.Assert(False, "Name does not match an attribute field or a property.  How can that be?")
            Return ErrorTypeSymbol.UnknownResultType
        End Function
    End Class
End Namespace
