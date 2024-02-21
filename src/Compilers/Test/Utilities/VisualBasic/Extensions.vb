' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Xunit

Friend Module Extensions
    <Extension>
    Public Function GetReferencedAssemblySymbol(compilation As Compilation, reference As MetadataReference) As AssemblySymbol
        Return DirectCast(compilation.GetAssemblyOrModuleSymbol(reference), AssemblySymbol)
    End Function

    <Extension>
    Public Function GetReferencedModuleSymbol(compilation As Compilation, reference As MetadataReference) As ModuleSymbol
        Return DirectCast(compilation.GetAssemblyOrModuleSymbol(reference), ModuleSymbol)
    End Function

    Private Function SplitMemberName(qualifiedName As String) As ImmutableArray(Of String)
        Dim builder = ArrayBuilder(Of String).GetInstance()
        Dim curr = qualifiedName
        While curr.Length > 0
            builder.Add(MetadataHelpers.SplitQualifiedName(curr, curr))
        End While
        builder.ReverseContents()
        Return builder.ToImmutableAndFree()
    End Function

    <Extension>
    Public Function GetMember(comp As Compilation, qualifiedName As String) As Symbol
        Return DirectCast(comp, VisualBasicCompilation).GlobalNamespace.GetMember(qualifiedName)
    End Function

    <Extension>
    Public Function GetMember(Of T As Symbol)(comp As Compilation, qualifiedName As String) As T
        Return DirectCast(DirectCast(comp, VisualBasicCompilation).GlobalNamespace.GetMember(qualifiedName), T)
    End Function

    <Extension>
    Public Function GetMembers(comp As Compilation, qualifiedName As String) As ImmutableArray(Of Symbol)
        Dim lastContainer As NamespaceOrTypeSymbol = Nothing
        Return GetMembers(DirectCast(comp, VisualBasicCompilation).GlobalNamespace, qualifiedName, lastContainer)
    End Function

    Private Function GetMembers(container As NamespaceOrTypeSymbol, qualifiedName As String, <Out> ByRef lastContainer As NamespaceOrTypeSymbol) As ImmutableArray(Of Symbol)
        Dim parts = SplitMemberName(qualifiedName)

        lastContainer = container
        For i = 0 To parts.Length - 2
            lastContainer = DirectCast(lastContainer.GetMember(parts(i)), NamespaceOrTypeSymbol)
        Next

        Return lastContainer.GetMembers(parts(parts.Length - 1))
    End Function

    <Extension>
    Public Function GetMember(container As NamespaceOrTypeSymbol, qualifiedName As String) As Symbol
        Dim lastContainer As NamespaceOrTypeSymbol = Nothing
        Dim members = GetMembers(container, qualifiedName, lastContainer)
        If members.Length = 0 Then
            Return Nothing
        ElseIf members.Length > 1 Then
            Assert.True(False, "Found multiple members of specified name:" & vbCrLf + String.Join(vbCrLf, members))
        End If

        Return members.Single()
    End Function

    <Extension>
    Public Function GetMember(Of T As Symbol)(container As NamespaceOrTypeSymbol, qualifiedName As String) As T
        Return DirectCast(container.GetMember(qualifiedName), T)
    End Function

    <Extension>
    Public Function GetProperty(symbol As TypeSymbol, name As String) As PropertySymbol
        Return DirectCast(symbol.GetMembers(name).Single(), PropertySymbol)
    End Function

    <Extension>
    Public Function GetEvent(symbol As TypeSymbol, name As String) As EventSymbol
        Return DirectCast(symbol.GetMembers(name).Single(), EventSymbol)
    End Function

    <Extension>
    Public Function GetMethod(symbol As TypeSymbol, name As String) As MethodSymbol
        Return DirectCast(symbol.GetMembers(name).Single(), MethodSymbol)
    End Function

    <Extension>
    Public Function GetField(symbol As TypeSymbol, name As String) As FieldSymbol
        Return DirectCast(symbol.GetMembers(name).Single(), FieldSymbol)
    End Function

    <Extension>
    Friend Function GetTypeMember(this As NamespaceOrTypeSymbol, name As String) As NamedTypeSymbol
        Return this.GetTypeMembers(name).Single
    End Function

    <Extension>
    Friend Function GetNamespace(this As NamespaceSymbol, name As String) As NamespaceSymbol
        Return DirectCast(this.GetMembers(name).Single(), NamespaceSymbol)
    End Function

    <Extension>
    Friend Function GetFieldNames(this As ModuleSymbol, qualifiedTypeName As String) As String()
        Dim type = DirectCast(this.GlobalNamespace.GetMember(qualifiedName:=qualifiedTypeName), NamedTypeSymbol)
        Return type.GetMembers().OfType(Of FieldSymbol)().Select(Of String)(Function(f) f.Name).ToArray()
    End Function

    <Extension>
    Friend Function GetFieldNamesAndTypes(this As ModuleSymbol, qualifiedTypeName As String) As String()
        Dim type = DirectCast(this.GlobalNamespace.GetMember(qualifiedName:=qualifiedTypeName), NamedTypeSymbol)
        Return type.GetMembers().OfType(Of FieldSymbol)().Select(Of String)(Function(f) f.Name + ": " + f.Type.ToDisplayString(SymbolDisplayFormat.TestFormat)).ToArray()
    End Function

    <Extension>
    Friend Function GetAttribute(this As Symbol, c As NamedTypeSymbol) As VisualBasicAttributeData
        Return this.GetAttributes().Where(Function(a) TypeSymbol.Equals(a.AttributeClass, c, TypeCompareKind.ConsiderEverything)).First()
    End Function

    <Extension>
    Friend Function GetAttribute(this As Symbol, m As MethodSymbol) As VisualBasicAttributeData
        Return this.GetAttributes().Where(Function(a) a.AttributeConstructor = m).First()
    End Function

    <Extension>
    Friend Function GetAttributes(this As Symbol, c As NamedTypeSymbol) As IEnumerable(Of VisualBasicAttributeData)
        Return this.GetAttributes().Where(Function(a) TypeSymbol.Equals(a.AttributeClass, c, TypeCompareKind.ConsiderEverything))
    End Function

    <Extension>
    Friend Function GetAttributes(this As Symbol, m As MethodSymbol) As IEnumerable(Of VisualBasicAttributeData)
        Return this.GetAttributes().Where(Function(a) a.AttributeConstructor = m)
    End Function

    <Extension>
    Friend Function GetAttributes(this As Symbol, namespaceName As String, typeName As String) As IEnumerable(Of VisualBasicAttributeData)
        Return this.GetAttributes().Where(Function(a) a.IsTargetAttribute(namespaceName, typeName))
    End Function

    <Extension>
    Friend Function GetAttributes(this As Symbol, description As AttributeDescription) As IEnumerable(Of VisualBasicAttributeData)
        Return this.GetAttributes().Where(Function(a) a.IsTargetAttribute(description))
    End Function

    <Extension>
    Friend Sub VerifyValue(Of T)(ByVal attr As VisualBasicAttributeData, ByVal i As Integer, ByVal kind As TypedConstantKind, ByVal v As T)
        Dim arg = attr.CommonConstructorArguments(i)
        Assert.Equal(kind, arg.Kind)
        Assert.True(IsEqual(Of T)(arg, v))
    End Sub

    <Extension>
    Friend Sub VerifyValue(Of T)(ByVal attr As VisualBasicAttributeData, ByVal i As Integer, ByVal name As String, ByVal kind As TypedConstantKind, ByVal v As T)
        Dim namedArg = attr.CommonNamedArguments(i)
        Assert.Equal(namedArg.Key, name)
        Dim arg = namedArg.Value
        Assert.Equal(arg.Kind, kind)
        Assert.True(IsEqual(Of T)(arg, v))
    End Sub

    <Extension>
    Friend Sub VerifyNamedArgumentValue(Of T)(ByVal attr As VisualBasicAttributeData, i As Integer, name As String, kind As TypedConstantKind, v As T)
        Dim namedArg = attr.CommonNamedArguments(i)
        Assert.Equal(namedArg.Key, name)
        Dim arg = namedArg.Value
        Assert.Equal(arg.Kind, kind)
        Assert.True(IsEqual(arg, v))
    End Sub

    Private Function IsEqual(Of T)(ByVal arg As TypedConstant, ByVal expected As T) As Boolean

        Select Case arg.Kind
            Case TypedConstantKind.Array
                Return AreEqual(arg.Values, expected)
            Case TypedConstantKind.Enum
                Return expected.Equals(arg.Value)
            Case TypedConstantKind.Type
                Dim typeSym = TryCast(arg.Value, TypeSymbol)
                If typeSym Is Nothing Then
                    Return False
                End If

                Dim expTypeSym = TryCast(expected, TypeSymbol)
                If typeSym.Equals(expTypeSym) Then
                    Return True
                End If

                ' TODO: improve the comparison mechanism for generic types.
                If typeSym.Kind = SymbolKind.NamedType AndAlso
                    DirectCast(typeSym, NamedTypeSymbol).IsGenericType() Then

                    Dim s1 = typeSym.ToDisplayString(SymbolDisplayFormat.TestFormat)
                    Dim s2 = expected.ToString()
                    If (s1 = s2) Then
                        Return True
                    End If

                End If

                Dim expType = TryCast(expected, Type)
                If expType Is Nothing Then
                    Return False
                End If
                'Can't always simply compare string as <T>.ToString() is IL format
                Return IsEqual(typeSym, expType)
            Case Else
                'Assert.Equal(expected, CType(arg.Value, T))
                Return If(expected Is Nothing, arg.Value Is Nothing, expected.Equals(CType(arg.Value, T)))
        End Select

    End Function

    ''' For argument is not simple 'Type' (generic or array)
    Private Function IsEqual(typeSym As TypeSymbol, expType As Type) As Boolean
        Dim expTypeInfo = expType.GetTypeInfo()
        Dim typeSymTypeKind As TypeKind = typeSym.TypeKind
        If typeSymTypeKind = TypeKind.Interface OrElse typeSymTypeKind = TypeKind.Class OrElse
            typeSymTypeKind = TypeKind.Structure OrElse typeSymTypeKind = TypeKind.Delegate Then

            Dim namedType = DirectCast(typeSym, NamedTypeSymbol)
            ' name should be same if it's not generic (NO ByRef in attribute)
            If (namedType.Arity = 0) Then
                Return typeSym.Name = expType.Name
            End If
            ' generic
            If Not (expTypeInfo.IsGenericType) Then
                Return False
            End If

            Dim nameOnly = expType.Name
            'generic <Name>'1
            Dim idx = expType.Name.LastIndexOfAny(New Char() {"`"c})
            If (idx > 0) Then
                nameOnly = expType.Name.Substring(0, idx)
            End If
            If Not (typeSym.Name = nameOnly) Then
                Return False
            End If
            Dim expArgs = expTypeInfo.GenericTypeArguments()
            Dim actArgs = namedType.TypeArguments()
            If Not (expArgs.Count = actArgs.Length) Then
                Return False
            End If

            For i = 0 To expArgs.Count - 1
                If Not IsEqual(actArgs(i), expArgs(i)) Then
                    Return False
                End If
            Next
            Return True
            ' array type
        ElseIf typeSymTypeKind = TypeKind.Array Then
            If Not expType.IsArray Then
                Return False
            End If
            Dim arySym = DirectCast(typeSym, ArrayTypeSymbol)
            If Not IsEqual(arySym.ElementType, expType.GetElementType()) Then
                Return False
            End If
            If Not IsEqual(arySym.BaseType, expTypeInfo.BaseType) Then
                Return False
            End If
            Return arySym.Rank = expType.GetArrayRank()
        End If

        Return False
    End Function

    ' Compare an Object with a TypedConstant.  This compares the TypeConstant's value and ignores the TypeConstant's type.
    Private Function AreEqual(tc As ImmutableArray(Of TypedConstant), o As Object) As Boolean

        If o Is Nothing Then
            Return tc.IsDefault
        ElseIf tc.IsDefault Then
            Return False
        End If

        If Not o.GetType.IsArray Then
            Return False
        End If

        Dim a = DirectCast(o, Array)
        Dim ret As Boolean = True
        For i = 0 To a.Length - 1
            Dim v = a.GetValue(i)
            Dim c = tc(i)
            ret = ret And IsEqual(c, v)
        Next
        Return ret
    End Function

    <Extension>
    Public Function BaseType(symbol As TypeSymbol) As NamedTypeSymbol
        Return symbol.BaseTypeNoUseSiteDiagnostics
    End Function

    <Extension>
    Public Function Interfaces(symbol As TypeSymbol) As ImmutableArray(Of NamedTypeSymbol)
        Return symbol.InterfacesNoUseSiteDiagnostics
    End Function

    <Extension>
    Public Function AllInterfaces(symbol As TypeSymbol) As ImmutableArray(Of NamedTypeSymbol)
        Return symbol.AllInterfacesNoUseSiteDiagnostics
    End Function

    <Extension>
    Public Function TypeArguments(symbol As NamedTypeSymbol) As ImmutableArray(Of TypeSymbol)
        Return symbol.TypeArgumentsNoUseSiteDiagnostics
    End Function

    <Extension>
    Public Function ConstraintTypes(symbol As TypeParameterSymbol) As ImmutableArray(Of TypeSymbol)
        Return symbol.ConstraintTypesNoUseSiteDiagnostics
    End Function

    <Extension>
    Friend Function ParameterSymbols(this As IMethodSymbolInternal) As ImmutableArray(Of ParameterSymbol)
        Return DirectCast(this, MethodSymbol).Parameters
    End Function

    <Extension>
    Friend Function ReturnType(this As IMethodSymbolInternal) As TypeSymbol
        Return DirectCast(this, MethodSymbol).ReturnType
    End Function

    <Extension>
    Friend Function ReturnsVoid(this As IMethodSymbolInternal) As Boolean
        Return DirectCast(this, IMethodSymbol).ReturnsVoid
    End Function

    <Extension>
    Friend Function RefKind(this As ParameterSymbol) As RefKind
        Return DirectCast(this, IParameterSymbol).RefKind
    End Function

    <Extension>
    Friend Function ReduceExtensionMethod(this As MethodSymbol, instanceType As TypeSymbol) As MethodSymbol
        Return this.ReduceExtensionMethod(instanceType, CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
    End Function

    <Extension>
    Friend Function ReduceExtensionMethod(this As MethodSymbol, instanceType As TypeSymbol, proximity As Integer) As MethodSymbol
        Return this.ReduceExtensionMethod(instanceType, proximity, CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
    End Function

    <Extension>
    Friend Function GetUseSiteErrorInfo(this As Symbol) As DiagnosticInfo
        Return this.GetUseSiteInfo().DiagnosticInfo
    End Function

    <Extension>
    Friend Sub Verify(this As ReadOnlyBindingDiagnostic(Of AssemblySymbol), ParamArray expected As DiagnosticDescription())
        this.Diagnostics.Verify(expected)
    End Sub

    <Extension>
    Friend Sub LookupMember(this As Binder,
                            lookupResult As LookupResult,
                            container As NamespaceOrTypeSymbol,
                            name As String,
                            arity As Integer,
                            options As LookupOptions,
                            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
        Dim useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol) = Nothing
        this.LookupMember(lookupResult, container, name, arity, options, useSiteInfo)
        AddDiagnosticInfos(useSiteDiagnostics, useSiteInfo)
    End Sub

    Private Sub AddDiagnosticInfos(<[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo), useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
        If useSiteInfo.Diagnostics IsNot Nothing Then
            If useSiteDiagnostics Is Nothing Then
                useSiteDiagnostics = DirectCast(useSiteInfo.Diagnostics, HashSet(Of DiagnosticInfo))
            Else
                useSiteDiagnostics.AddAll(useSiteInfo.Diagnostics)
            End If
        End If
    End Sub

    <Extension()>
    Public Function IsBaseTypeOf(this As TypeSymbol, subType As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
        Dim useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol) = Nothing
        Dim result = this.IsBaseTypeOf(subType, useSiteInfo)
        AddDiagnosticInfos(useSiteDiagnostics, useSiteInfo)
        Return result
    End Function

    <Extension()>
    Public Function IsOrDerivedFrom(this As TypeSymbol, baseType As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
        Dim useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol) = Nothing
        Dim result = this.IsOrDerivedFrom(baseType, useSiteInfo)
        AddDiagnosticInfos(useSiteDiagnostics, useSiteInfo)
        Return result
    End Function

    <Extension>
    Friend Sub Lookup(this As Binder,
                      lookupResult As LookupResult,
                      name As String,
                      arity As Integer,
                      options As LookupOptions,
                      <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
        Dim useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol) = Nothing
        this.Lookup(lookupResult, name, arity, options, useSiteInfo)
        AddDiagnosticInfos(useSiteDiagnostics, useSiteInfo)
    End Sub

    <Extension>
    Friend Function GetBoundMethodBody(this As MethodSymbol, compilationState As TypeCompilationState, diagnostics As DiagnosticBag, <Out()> Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
        Dim builder = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)
        Dim result = this.GetBoundMethodBody(compilationState, builder, methodBodyBinder)
        diagnostics.AddRange(builder.DiagnosticBag)
        builder.Free()
        Return result
    End Function

End Module
