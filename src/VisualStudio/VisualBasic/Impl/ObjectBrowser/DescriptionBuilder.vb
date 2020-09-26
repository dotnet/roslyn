' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    Friend Class DescriptionBuilder
        Inherits AbstractDescriptionBuilder

        Public Sub New(
            description As IVsObjectBrowserDescription3,
            libraryManager As ObjectBrowserLibraryManager,
            listItem As ObjectListItem,
            project As Project
        )

            MyBase.New(description, libraryManager, listItem, project)
        End Sub

        Protected Overrides Sub BuildNamespaceDeclaration(namespaceSymbol As INamespaceSymbol, options As _VSOBJDESCOPTIONS)
            AddText("Namespace ")
            AddName(namespaceSymbol.ToDisplayString())
        End Sub

        Protected Overrides Sub BuildDelegateDeclaration(typeSymbol As INamedTypeSymbol, options As _VSOBJDESCOPTIONS)
            Debug.Assert(typeSymbol.TypeKind = TypeKind.Delegate)

            BuildTypeModifiers(typeSymbol)
            AddText("Delegate ")

            Dim delegateInvokeMethod = typeSymbol.DelegateInvokeMethod
            If delegateInvokeMethod.ReturnsVoid Then
                AddText("Sub ")
            Else
                AddText("Function ")
            End If

            Dim typeNameFormat = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions:=SymbolDisplayGenericsOptions.None)

            AddName(typeSymbol.ToDisplayString(typeNameFormat))

            If typeSymbol.TypeParameters.Length > 0 Then
                AddText("(Of ")
                BuildTypeParameterList(typeSymbol.TypeParameters)
                AddText(")")
            End If

            AddText("(")
            BuildParameterList(delegateInvokeMethod.Parameters)
            AddText(")")

            If Not delegateInvokeMethod.ReturnsVoid Then
                AddText(" As ")
                AddTypeLink(delegateInvokeMethod.ReturnType, LinkFlags.None)
            End If
        End Sub

        Protected Overrides Sub BuildTypeDeclaration(typeSymbol As INamedTypeSymbol, options As _VSOBJDESCOPTIONS)
            BuildTypeModifiers(typeSymbol)

            Select Case typeSymbol.TypeKind
                Case TypeKind.Class
                    AddText("Class ")
                Case TypeKind.Interface
                    AddText("Interface ")
                Case TypeKind.Module
                    AddText("Module ")
                Case TypeKind.Structure
                    AddText("Structure ")
                Case TypeKind.Enum
                    AddText("Enum ")
                Case Else
                    Debug.Fail("Invalid type kind encountered: " & typeSymbol.TypeKind.ToString())
            End Select

            Dim typeNameFormat = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions:=SymbolDisplayGenericsOptions.None)

            AddName(typeSymbol.ToDisplayString(typeNameFormat))

            If typeSymbol.TypeParameters.Length > 0 Then
                AddText("(Of ")
                BuildTypeParameterList(typeSymbol.TypeParameters)
                AddText(")")
            End If

            If typeSymbol.TypeKind = TypeKind.Class Then
                Dim baseType = typeSymbol.BaseType
                If baseType IsNot Nothing Then
                    AddLineBreak()
                    AddIndent()
                    AddIndent()
                    AddText("Inherits ")
                    AddTypeLink(baseType, LinkFlags.None)
                End If
            End If

            If typeSymbol.TypeKind = TypeKind.Enum Then
                Dim underlyingType = typeSymbol.EnumUnderlyingType
                If underlyingType IsNot Nothing AndAlso underlyingType.SpecialType <> SpecialType.System_Int32 Then
                    AddText(" As ")
                    AddTypeLink(underlyingType, LinkFlags.None)
                End If
            End If
        End Sub

        Protected Overrides Sub BuildMethodDeclaration(methodSymbol As IMethodSymbol, options As _VSOBJDESCOPTIONS)
            Select Case methodSymbol.MethodKind
                Case MethodKind.Conversion, MethodKind.UserDefinedOperator
                    BuildOperatorDeclaration(methodSymbol)
                Case MethodKind.DeclareMethod
                    BuildDeclareMethodDeclaration(methodSymbol)
                Case Else
                    BuildRegularMethodDeclaration(methodSymbol)
            End Select
        End Sub

        Private Sub BuildOperatorDeclaration(methodSymbol As IMethodSymbol)
            BuildMemberModifiers(methodSymbol)

            Select Case methodSymbol.Name
                Case WellKnownMemberNames.ImplicitConversionName
                    AddText("Widening ")
                Case WellKnownMemberNames.ExplicitConversionName
                    AddText("Narrowing ")
            End Select

            AddText("Operator ")

            Dim methodNameFormat = New SymbolDisplayFormat()
            AddName(methodSymbol.ToDisplayString(methodNameFormat))

            AddText("(")
            BuildParameterList(methodSymbol.Parameters)
            AddText(")")

            If Not methodSymbol.ReturnsVoid Then
                AddText(" As ")
                AddTypeLink(methodSymbol.ReturnType, LinkFlags.None)
            End If
        End Sub

        Private Sub BuildDeclareMethodDeclaration(methodSymbol As IMethodSymbol)
            BuildMemberModifiers(methodSymbol)

            AddText("Declare ")

            Dim dllImportData = methodSymbol.GetDllImportData()

            Select Case dllImportData.CharacterSet
                Case System.Runtime.InteropServices.CharSet.Ansi
                    AddText("Ansi ")
                Case System.Runtime.InteropServices.CharSet.Auto
                    AddText("Auto ")
                Case System.Runtime.InteropServices.CharSet.Unicode
                    AddText("Unicode ")
            End Select

            If methodSymbol.ReturnsVoid Then
                AddText("Sub ")
            Else
                AddText("Function ")
            End If

            Dim methodNameFormat = New SymbolDisplayFormat()
            AddName(methodSymbol.ToDisplayString(methodNameFormat))

            If dllImportData.ModuleName IsNot Nothing Then
                AddText(" Lib """)
                AddText(dllImportData.ModuleName)
                AddText("""")
            End If

            If dllImportData.EntryPointName IsNot Nothing Then
                AddText(" Alias """)
                AddText(dllImportData.EntryPointName)
                AddText("""")
            End If

            AddText("(")
            BuildParameterList(methodSymbol.Parameters)
            AddText(")")

            If Not methodSymbol.ReturnsVoid Then
                AddText(" As ")
                AddTypeLink(methodSymbol.ReturnType, LinkFlags.None)
            End If
        End Sub

        Private Sub BuildRegularMethodDeclaration(methodSymbol As IMethodSymbol)
            BuildMemberModifiers(methodSymbol)

            If methodSymbol.ReturnsVoid Then
                AddText("Sub ")
            Else
                AddText("Function ")
            End If

            Dim methodNameFormat = New SymbolDisplayFormat()
            AddName(methodSymbol.ToDisplayString(methodNameFormat))

            If methodSymbol.TypeParameters.Length > 0 Then
                AddText("(Of ")
                BuildTypeParameterList(methodSymbol.TypeParameters)
                AddText(")")
            End If

            AddText("(")
            BuildParameterList(methodSymbol.Parameters)
            AddText(")")

            If Not methodSymbol.ReturnsVoid Then
                AddText(" As ")
                AddTypeLink(methodSymbol.ReturnType, LinkFlags.None)
            End If
        End Sub

        Protected Overrides Sub BuildFieldDeclaration(fieldSymbol As IFieldSymbol, options As _VSOBJDESCOPTIONS)
            BuildMemberModifiers(fieldSymbol)

            AddText(fieldSymbol.Name)

            AddText(" As ")
            AddTypeLink(fieldSymbol.Type, LinkFlags.None)

            If fieldSymbol.HasConstantValue Then
                AddText(" = ")

                If fieldSymbol.ConstantValue Is Nothing Then
                    AddText("Nothing")
                Else
                    AddText(fieldSymbol.ConstantValue.ToString())
                End If
            End If
        End Sub

        Protected Overrides Sub BuildPropertyDeclaration(propertySymbol As IPropertySymbol, options As _VSOBJDESCOPTIONS)
            BuildMemberModifiers(propertySymbol)

            If propertySymbol.GetMethod IsNot Nothing Then
                If propertySymbol.SetMethod Is Nothing Then
                    AddText("ReadOnly ")
                End If
            ElseIf propertySymbol.SetMethod IsNot Nothing Then
                AddText("WriteOnly ")
            End If

            AddText("Property ")

            AddName(propertySymbol.Name)

            If propertySymbol.Parameters.Length > 0 Then
                AddText("(")
                BuildParameterList(propertySymbol.Parameters)
                AddText(")")
            End If

            AddText(" As ")
            AddTypeLink(propertySymbol.Type, LinkFlags.None)
        End Sub

        Protected Overrides Sub BuildEventDeclaration(eventSymbol As IEventSymbol, options As _VSOBJDESCOPTIONS)
            BuildMemberModifiers(eventSymbol)

            AddText("Event ")

            AddName(eventSymbol.Name)

            AddText("(")

            Dim eventType = eventSymbol.Type
            If eventType IsNot Nothing AndAlso eventType.TypeKind = TypeKind.Delegate Then
                Dim delegateInvokeMethod = CType(eventType, INamedTypeSymbol).DelegateInvokeMethod
                If delegateInvokeMethod IsNot Nothing Then
                    BuildParameterList(delegateInvokeMethod.Parameters)
                End If
            End If

            AddText(")")
        End Sub

        Private Sub BuildAccessibility(symbol As ISymbol)
            Select Case symbol.DeclaredAccessibility
                Case Accessibility.Public
                    AddText("Public ")
                Case Accessibility.Private
                    AddText("Private ")
                Case Accessibility.Friend
                    AddText("Friend ")
                Case Accessibility.Protected
                    AddText("Protected ")
                Case Accessibility.ProtectedOrFriend
                    AddText("Protected Friend ")
                Case Accessibility.ProtectedAndFriend
                    AddText("Private Protected ")
                Case Else
                    AddText("Friend ")
            End Select
        End Sub

        Private Sub BuildTypeModifiers(typeSymbol As INamedTypeSymbol)
            BuildAccessibility(typeSymbol)

            ' Note: There are no "Shared" types in VB.

            If typeSymbol.IsAbstract AndAlso
               typeSymbol.TypeKind <> TypeKind.Interface Then
                AddText("MustInherit ")
            End If

            If typeSymbol.IsSealed AndAlso
               typeSymbol.TypeKind <> TypeKind.Delegate AndAlso
               typeSymbol.TypeKind <> TypeKind.Module AndAlso
               typeSymbol.TypeKind <> TypeKind.Enum AndAlso
               typeSymbol.TypeKind <> TypeKind.Structure Then

                AddText("NotInheritable ")
            End If
        End Sub

        Private Sub BuildMemberModifiers(memberSymbol As ISymbol)
            If memberSymbol.ContainingType IsNot Nothing And memberSymbol.ContainingType.TypeKind = TypeKind.Interface Then
                Return
            End If

            Dim fieldSymbol = TryCast(memberSymbol, IFieldSymbol)
            Dim methodSymbol = TryCast(memberSymbol, IMethodSymbol)

            BuildAccessibility(memberSymbol)

            ' TODO 'Shadows' modifier isn't exposed on symbols. Do we need it?

            If memberSymbol.IsStatic AndAlso
               (methodSymbol Is Nothing OrElse Not methodSymbol.MethodKind = MethodKind.DeclareMethod) AndAlso
               (fieldSymbol Is Nothing OrElse Not fieldSymbol.IsConst) Then

                AddText("Shared ")
            End If

            If fieldSymbol IsNot Nothing AndAlso fieldSymbol.IsReadOnly Then
                AddText("ReadOnly ")
            End If

            If fieldSymbol IsNot Nothing AndAlso fieldSymbol.IsConst Then
                AddText("Const ")
            End If

            If memberSymbol.IsAbstract Then
                AddText("MustOverride ")
            End If

            If memberSymbol.IsOverride Then
                AddText("Overrides ")
            End If

            If memberSymbol.IsVirtual Then
                AddText("Overridable ")
            End If

            If memberSymbol.IsSealed Then
                AddText("NotOverridable ")
            End If
        End Sub

        Private Sub BuildParameterList(parameters As ImmutableArray(Of IParameterSymbol))
            Dim count = parameters.Length
            If count = 0 Then
                Return
            End If

            For i = 0 To count - 1
                If i > 0 Then
                    AddComma()
                End If

                Dim current = parameters(i)

                If current.IsOptional Then
                    AddText("Optional ")
                End If

                If current.RefKind = RefKind.Ref Then
                    ' TODO: Declare methods may implicitly make string parameters ByRef. To fix this,
                    ' we'll need support from the compiler to expose IsExplicitByRef or something similar
                    ' (Note: symbol display uses IsExplicitByRef to handle this case).
                    AddText("ByRef ")
                End If

                If current.IsParams Then
                    AddText("ParamArray ")
                End If

                AddParam(current.Name)
                AddText(" As ")
                AddTypeLink(current.Type, LinkFlags.None)

                If current.HasExplicitDefaultValue Then
                    AddText(" = ")
                    If current.ExplicitDefaultValue Is Nothing Then
                        AddText("Nothing")
                    Else
                        AddText(current.ExplicitDefaultValue.ToString())
                    End If
                End If
            Next
        End Sub

        Private Sub BuildTypeParameterList(typeParameters As ImmutableArray(Of ITypeParameterSymbol))
            Dim count = typeParameters.Length
            If count = 0 Then
                Return
            End If

            For i = 0 To count - 1
                If i > 0 Then
                    AddName(", ")
                End If

                Dim current = typeParameters(i)

                AddName(current.Name)
                AddConstraints(current)
            Next
        End Sub

        Private Sub AddConstraints(typeParameter As ITypeParameterSymbol)
            Dim count = CountConstraints(typeParameter)
            If count = 0 Then
                Return
            End If

            If count = 1 Then
                AddSingleConstraint(typeParameter)
            Else
                AddMultipleConstraints(typeParameter)
            End If
        End Sub

        Private Sub AddSingleConstraint(typeParameter As ITypeParameterSymbol)
            AddName(" As ")

            If typeParameter.HasReferenceTypeConstraint Then
                AddName("Class")
            ElseIf typeParameter.HasValueTypeConstraint Then
                AddName("Structure")
            ElseIf typeParameter.HasConstructorConstraint Then
                AddName("New")
            Else
                Debug.Assert(typeParameter.ConstraintTypes.Length = 1)
                AddTypeLink(typeParameter.ConstraintTypes(0), LinkFlags.None)
            End If
        End Sub

        Private Sub AddMultipleConstraints(typeParameter As ITypeParameterSymbol)
            AddName(" As {")

            Dim constraintAdded = False

            If typeParameter.HasReferenceTypeConstraint Then
                AddName("Class")
                constraintAdded = True
            End If

            If typeParameter.HasValueTypeConstraint Then
                If constraintAdded Then
                    AddName(", ")
                End If

                AddName("Structure")
                constraintAdded = True
            End If

            If typeParameter.HasConstructorConstraint Then
                If constraintAdded Then
                    AddName(", ")
                End If

                AddName("New")
                constraintAdded = True
            End If

            If typeParameter.ConstraintTypes.Length > 0 Then
                For Each constraintType In typeParameter.ConstraintTypes
                    If constraintAdded Then
                        AddName(", ")
                    End If

                    AddTypeLink(constraintType, LinkFlags.None)
                    constraintAdded = True
                Next
            End If

            AddName("}")
        End Sub

        Private Function CountConstraints(typeParameter As ITypeParameterSymbol) As Integer
            Dim result = typeParameter.ConstraintTypes.Length

            If typeParameter.HasReferenceTypeConstraint Then
                result += 1
            End If

            If typeParameter.HasValueTypeConstraint Then
                result += 1
            End If

            If typeParameter.HasConstructorConstraint Then
                result += 1
            End If

            Return result
        End Function
    End Class
End Namespace
