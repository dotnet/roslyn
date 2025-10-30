' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
Imports System.Threading

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    Friend NotInheritable Class DescriptionBuilder
        Inherits AbstractDescriptionBuilder

        Public Sub New(
            description As IVsObjectBrowserDescription3,
            libraryManager As ObjectBrowserLibraryManager,
            listItem As ObjectListItem,
            project As Project)

            MyBase.New(description, libraryManager, listItem, project)
        End Sub

        Protected Overrides Sub BuildNamespaceDeclaration(namespaceSymbol As INamespaceSymbol, options As _VSOBJDESCOPTIONS)
            AddText("Namespace ")
            AddName(namespaceSymbol.ToDisplayString())
        End Sub

        Protected Overrides Async Function BuildDelegateDeclarationAsync(
                typeSymbol As INamedTypeSymbol,
                options As _VSOBJDESCOPTIONS,
                cancellationToken As CancellationToken) As Task
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
                Await BuildTypeParameterListAsync(typeSymbol.TypeParameters, cancellationToken).ConfigureAwait(True)
                AddText(")")
            End If

            AddText("(")
            Await BuildParameterListAsync(delegateInvokeMethod.Parameters, cancellationToken).ConfigureAwait(True)
            AddText(")")

            If Not delegateInvokeMethod.ReturnsVoid Then
                AddText(" As ")
                Await AddTypeLinkAsync(delegateInvokeMethod.ReturnType, LinkFlags.None, cancellationToken).ConfigureAwait(True)
            End If
        End Function

        Protected Overrides Async Function BuildTypeDeclarationAsync(
                typeSymbol As INamedTypeSymbol,
                options As _VSOBJDESCOPTIONS,
                cancellationToken As CancellationToken) As Task
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
                Await BuildTypeParameterListAsync(typeSymbol.TypeParameters, cancellationToken).ConfigureAwait(True)
                AddText(")")
            End If

            If typeSymbol.TypeKind = TypeKind.Class Then
                Dim baseType = typeSymbol.BaseType
                If baseType IsNot Nothing Then
                    AddLineBreak()
                    AddIndent()
                    AddIndent()
                    AddText("Inherits ")
                    Await AddTypeLinkAsync(baseType, LinkFlags.None, cancellationToken).ConfigureAwait(True)
                End If
            End If

            If typeSymbol.TypeKind = TypeKind.Enum Then
                Dim underlyingType = typeSymbol.EnumUnderlyingType
                If underlyingType IsNot Nothing AndAlso underlyingType.SpecialType <> SpecialType.System_Int32 Then
                    AddText(" As ")
                    Await AddTypeLinkAsync(underlyingType, LinkFlags.None, cancellationToken).ConfigureAwait(True)
                End If
            End If
        End Function

        Protected Overrides Async Function BuildMethodDeclarationAsync(
                methodSymbol As IMethodSymbol,
                options As _VSOBJDESCOPTIONS,
                cancellationToken As CancellationToken) As Task
            Select Case methodSymbol.MethodKind
                Case MethodKind.Conversion, MethodKind.UserDefinedOperator
                    Await BuildOperatorDeclarationAsync(methodSymbol, cancellationToken).ConfigureAwait(True)
                Case MethodKind.DeclareMethod
                    Await BuildDeclareMethodDeclarationAsync(methodSymbol, cancellationToken).ConfigureAwait(True)
                Case Else
                    Await BuildRegularMethodDeclarationAsync(methodSymbol, cancellationToken).ConfigureAwait(True)
            End Select
        End Function

        Private Async Function BuildOperatorDeclarationAsync(
                methodSymbol As IMethodSymbol,
                cancellationToken As CancellationToken) As Task
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
            Await BuildParameterListAsync(methodSymbol.Parameters, cancellationToken).ConfigureAwait(True)
            AddText(")")

            If Not methodSymbol.ReturnsVoid Then
                AddText(" As ")
                Await AddTypeLinkAsync(methodSymbol.ReturnType, LinkFlags.None, cancellationToken).ConfigureAwait(True)
            End If
        End Function

        Private Async Function BuildDeclareMethodDeclarationAsync(
                methodSymbol As IMethodSymbol,
                cancellationToken As CancellationToken) As Task
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
            Await BuildParameterListAsync(methodSymbol.Parameters, cancellationToken).ConfigureAwait(True)
            AddText(")")

            If Not methodSymbol.ReturnsVoid Then
                AddText(" As ")
                Await AddTypeLinkAsync(methodSymbol.ReturnType, LinkFlags.None, cancellationToken).ConfigureAwait(True)
            End If
        End Function

        Private Async Function BuildRegularMethodDeclarationAsync(
                methodSymbol As IMethodSymbol,
                cancellationToken As CancellationToken) As Task
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
                Await BuildTypeParameterListAsync(methodSymbol.TypeParameters, cancellationToken).ConfigureAwait(True)
                AddText(")")
            End If

            AddText("(")
            Await BuildParameterListAsync(methodSymbol.Parameters, cancellationToken).ConfigureAwait(True)
            AddText(")")

            If Not methodSymbol.ReturnsVoid Then
                AddText(" As ")
                Await AddTypeLinkAsync(methodSymbol.ReturnType, LinkFlags.None, cancellationToken).ConfigureAwait(True)
            End If
        End Function

        Protected Overrides Async Function BuildFieldDeclarationAsync(
                fieldSymbol As IFieldSymbol,
                options As _VSOBJDESCOPTIONS,
                cancellationToken As CancellationToken) As Task
            BuildMemberModifiers(fieldSymbol)

            AddText(fieldSymbol.Name)

            AddText(" As ")
            Await AddTypeLinkAsync(fieldSymbol.Type, LinkFlags.None, cancellationToken).ConfigureAwait(True)

            If fieldSymbol.HasConstantValue Then
                AddText(" = ")

                If fieldSymbol.ConstantValue Is Nothing Then
                    AddText("Nothing")
                Else
                    AddText(fieldSymbol.ConstantValue.ToString())
                End If
            End If
        End Function

        Protected Overrides Async Function BuildPropertyDeclarationAsync(
                propertySymbol As IPropertySymbol,
                options As _VSOBJDESCOPTIONS,
                cancellationToken As CancellationToken) As Task
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
                Await BuildParameterListAsync(propertySymbol.Parameters, cancellationToken).ConfigureAwait(True)
                AddText(")")
            End If

            AddText(" As ")
            Await AddTypeLinkAsync(propertySymbol.Type, LinkFlags.None, cancellationToken).ConfigureAwait(True)
        End Function

        Protected Overrides Async Function BuildEventDeclarationAsync(
                eventSymbol As IEventSymbol,
                options As _VSOBJDESCOPTIONS,
                cancellationToken As CancellationToken) As Task
            BuildMemberModifiers(eventSymbol)

            AddText("Event ")

            AddName(eventSymbol.Name)

            AddText("(")

            Dim eventType = eventSymbol.Type
            If eventType IsNot Nothing AndAlso eventType.TypeKind = TypeKind.Delegate Then
                Dim delegateInvokeMethod = CType(eventType, INamedTypeSymbol).DelegateInvokeMethod
                If delegateInvokeMethod IsNot Nothing Then
                    Await BuildParameterListAsync(delegateInvokeMethod.Parameters, cancellationToken).ConfigureAwait(True)
                End If
            End If

            AddText(")")
        End Function

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

        Private Async Function BuildParameterListAsync(
                parameters As ImmutableArray(Of IParameterSymbol),
                cancellationToken As CancellationToken) As Task
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
                Await AddTypeLinkAsync(current.Type, LinkFlags.None, cancellationToken).ConfigureAwait(True)

                If current.HasExplicitDefaultValue Then
                    AddText(" = ")
                    If current.ExplicitDefaultValue Is Nothing Then
                        AddText("Nothing")
                    Else
                        AddText(current.ExplicitDefaultValue.ToString())
                    End If
                End If
            Next
        End Function

        Private Async Function BuildTypeParameterListAsync(
                typeParameters As ImmutableArray(Of ITypeParameterSymbol),
                cancellationToken As CancellationToken) As Task
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
                Await AddConstraintsAsync(current, cancellationToken).ConfigureAwait(True)
            Next
        End Function

        Private Async Function AddConstraintsAsync(
                typeParameter As ITypeParameterSymbol,
                cancellationToken As CancellationToken) As Task
            Dim count = CountConstraints(typeParameter)
            If count = 0 Then
                Return
            End If

            If count = 1 Then
                Await AddSingleConstraintAsync(typeParameter, cancellationToken).ConfigureAwait(True)
            Else
                Await AddMultipleConstraintsAsync(typeParameter, cancellationToken).ConfigureAwait(True)
            End If
        End Function

        Private Async Function AddSingleConstraintAsync(
                typeParameter As ITypeParameterSymbol,
                cancellationToken As CancellationToken) As Task
            AddName(" As ")

            If typeParameter.HasReferenceTypeConstraint Then
                AddName("Class")
            ElseIf typeParameter.HasValueTypeConstraint Then
                AddName("Structure")
            ElseIf typeParameter.HasConstructorConstraint Then
                AddName("New")
            Else
                Debug.Assert(typeParameter.ConstraintTypes.Length = 1)
                Await AddTypeLinkAsync(typeParameter.ConstraintTypes(0), LinkFlags.None, cancellationToken).ConfigureAwait(True)
            End If
        End Function

        Private Async Function AddMultipleConstraintsAsync(
                typeParameter As ITypeParameterSymbol,
                cancellationToken As CancellationToken) As Task
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

                    Await AddTypeLinkAsync(constraintType, LinkFlags.None, cancellationToken).ConfigureAwait(True)
                    constraintAdded = True
                Next
            End If

            AddName("}")
        End Function

        Private Shared Function CountConstraints(typeParameter As ITypeParameterSymbol) As Integer
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
