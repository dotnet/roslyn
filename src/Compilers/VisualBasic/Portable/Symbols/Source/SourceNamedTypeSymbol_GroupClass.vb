' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend Class SourceNamedTypeSymbol

        Protected Overrides Sub AddGroupClassMembersIfNeeded(membersBuilder As MembersAndInitializersBuilder, diagnostics As BindingDiagnosticBag)
            ' For reference, see Bindable::IsMyGroupCollection and Bindable::CrackAttributesOnAllSymbolsInContainer in native code.
            If Me.TypeKind = TypeKind.Class AndAlso Not Me.IsGenericType Then

                Dim binder As Binder = Nothing
                Dim attributeSyntax As AttributeSyntax = Nothing
                Dim attributeData As VisualBasicAttributeData = GetMyGroupCollectionAttributeData(diagnostics, binder, attributeSyntax)

                If attributeData IsNot Nothing Then
                    ' Ok, this looks like a group class. Let's inspect the attribute data (Attribute::CrackArgBlob).

                    ' Attribute arguments are comma-separated lists.
                    Dim separatorComma = {","c}
                    Dim separatorDot = {"."c}
                    Dim baseTypeNames() As String = If(attributeData.GetConstructorArgument(Of String)(0, SpecialType.System_String), "").Split(separatorComma, StringSplitOptions.None)
                    Dim createMethods() As String = If(attributeData.GetConstructorArgument(Of String)(1, SpecialType.System_String), "").Split(separatorComma, StringSplitOptions.None)
                    Dim disposeMethods() As String = If(attributeData.GetConstructorArgument(Of String)(2, SpecialType.System_String), "").Split(separatorComma, StringSplitOptions.None)

                    ' DefaultInstanceAliases are respected only for attributes applied in MyTemplate.
                    Dim defaultInstances() As String

                    If attributeSyntax.SyntaxTree.IsMyTemplate Then
                        defaultInstances = If(attributeData.GetConstructorArgument(Of String)(3, Microsoft.CodeAnalysis.SpecialType.System_String), "").Split(separatorComma, StringSplitOptions.None)
                    Else
                        defaultInstances = Array.Empty(Of String)()
                    End If

                    ' Types matching each name in baseTypeNames will be grouped together in the builder below and groups will be separated by Nothing item.
                    ' See comment inside the loop about why there could be more than one.
                    Dim baseTypes As ArrayBuilder(Of NamedTypeSymbol) = ArrayBuilder(Of NamedTypeSymbol).GetInstance()
                    Dim haveBaseTypes As Boolean = False

                    For i As Integer = 0 To Math.Min(baseTypeNames.Length, createMethods.Length) - 1
                        baseTypeNames(i) = baseTypeNames(i).Trim()
                        createMethods(i) = createMethods(i).Trim()

                        If baseTypeNames(i).Length = 0 OrElse createMethods(i).Length = 0 Then
                            ' This might feel strange, but native compiler stops processing arguments
                            ' as soon as it encounters an empty base type name or create method name
                            Exit For
                        Else
                            If i < disposeMethods.Length Then
                                disposeMethods(i) = disposeMethods(i).Trim()
                            End If

                            If i < defaultInstances.Length Then
                                defaultInstances(i) = defaultInstances(i).Trim()
                            End If

                            ' See if we can locate the base class this group class is associated with. This might feel strange,
                            ' but native compiler does the match by fully qualified name without generic parameters, therefore we might 
                            ' find more than one base type from different assemblies or types with different arities, all are valid candidates.
                            FindGroupClassBaseTypes(baseTypeNames(i).Split(separatorDot, StringSplitOptions.None), DeclaringCompilation.GlobalNamespace, 0, baseTypes)

                            If Not haveBaseTypes AndAlso baseTypes.Count > 0 AndAlso baseTypes.Last() IsNot Nothing Then
                                haveBaseTypes = True
                            End If
                        End If

                        baseTypes.Add(Nothing)
                    Next

                    If haveBaseTypes Then
                        ' Now, iterate over all top level types declared in this module and pick those
                        ' inheriting from the bases we found (Bindable::ScanAndLoadMyGroupCollectionMembers). 
                        Dim collectionTypes = ArrayBuilder(Of KeyValuePair(Of NamedTypeSymbol, Integer)).GetInstance()
                        GetMyGroupCollectionTypes(Me.ContainingModule.GlobalNamespace, baseTypes, binder, collectionTypes)

                        If collectionTypes.Count > 0 Then
                            ' See Bindable::GenSyntheticMyGroupCollectionProperties

                            ' sort members to handle simple name clashes. Having all the classes with the same simple name
                            ' grouped together helps decide in one step if mangling is necessary.
                            ' It is only a gain of  O(n*n) -> O(n*log(n))
                            collectionTypes.Sort(GroupCollectionComparer.Singleton)

                            For i As Integer = 0 To collectionTypes.Count - 1
                                Dim current As KeyValuePair(Of NamedTypeSymbol, Integer) = collectionTypes(i)

                                ' members with the same simple name are grouped together.
                                ' if two adjacent members with the same simple name then use name mangling
                                Dim mangleNames As Boolean = (i > 0 AndAlso IdentifierComparison.Equals(current.Key.Name, collectionTypes(i - 1).Key.Name)) OrElse
                                                             (i < collectionTypes.Count - 1 AndAlso IdentifierComparison.Equals(current.Key.Name, collectionTypes(i + 1).Key.Name))

                                AddSyntheticMyGroupCollectionProperty(current.Key, mangleNames,
                                                                      createMethods(current.Value),
                                                                      If(current.Value < disposeMethods.Length,
                                                                         disposeMethods(current.Value),
                                                                         ""),
                                                                      If(current.Value < defaultInstances.Length,
                                                                         defaultInstances(current.Value),
                                                                         ""),
                                                                      membersBuilder,
                                                                      binder,
                                                                      attributeSyntax,
                                                                      diagnostics)
                            Next
                        End If

                        collectionTypes.Free()
                    End If

                    baseTypes.Free()
                End If
            End If
        End Sub

        Private Function GetMyGroupCollectionAttributeData(diagnostics As BindingDiagnosticBag, <Out> ByRef binder As Binder, <Out> ByRef attributeSyntax As AttributeSyntax) As VisualBasicAttributeData

            ' Calling GetAttributes() here is likely to get us in a cycle. Also, we want this function to be 
            ' as cheap as possible, it is called for every class and we don't want to bind all attributes
            ' attached to the declaration (even when it doesn't cause a cycle) before we able to create symbol's
            ' members. So, we will have to manually bind only those attributes that potentially could be 
            ' MyGroupCollectionAttribute, by examining syntax ourselves.
            Dim attributeLists As ImmutableArray(Of SyntaxList(Of AttributeListSyntax)) = GetAttributeDeclarations()
            Dim attributeData As VisualBasicAttributeData = Nothing

            For Each list As SyntaxList(Of AttributeListSyntax) In attributeLists
                If list.Any() Then
                    binder = GetAttributeBinder(list, ContainingSourceModule)
                    Dim quickChecker As QuickAttributeChecker = binder.QuickAttributeChecker

                    For Each attrList In list
                        For Each attr In attrList.Attributes
                            If (quickChecker.CheckAttribute(attr) And QuickAttributes.MyGroupCollection) <> 0 Then
                                ' This attribute syntax might be an application of MyGroupCollectionAttribute.
                                ' Let's bind it.
                                Dim attributeType As NamedTypeSymbol = Binder.BindAttributeType(binder, attr, Me, BindingDiagnosticBag.Discarded)
                                If Not attributeType.IsErrorType() Then
                                    If VisualBasicAttributeData.IsTargetEarlyAttribute(attributeType, attr, AttributeDescription.MyGroupCollectionAttribute) Then
                                        ' Calling GetAttribute can still get us into cycle if MyGroupCollectionAttribute is applied to itself.
                                        If attributeType Is Me Then
                                            binder.ReportDiagnostic(diagnostics, attr, ERRID.ERR_MyGroupCollectionAttributeCycle)
                                            Debug.Assert(attributeData Is Nothing)
                                            GoTo DoneWithBindingAttributes
                                        End If

                                        ' Or if any argument expression refers to a member of this type. Therefore, as a simple solution,
                                        ' we will allow only literals as the arguments.
                                        For Each argumentSyntax As ArgumentSyntax In attr.ArgumentList.Arguments
                                            Dim expression As ExpressionSyntax

                                            Select Case argumentSyntax.Kind
                                                Case SyntaxKind.SimpleArgument
                                                    expression = DirectCast(argumentSyntax, SimpleArgumentSyntax).Expression

                                                Case SyntaxKind.OmittedArgument
                                                    expression = Nothing

                                                Case Else
                                                    Throw ExceptionUtilities.UnexpectedValue(argumentSyntax.Kind)
                                            End Select

                                            If expression IsNot Nothing AndAlso
                                               Not TypeOf expression Is LiteralExpressionSyntax Then
                                                binder.ReportDiagnostic(diagnostics, expression, ERRID.ERR_LiteralExpected)
                                                attributeData = Nothing
                                                GoTo DoneWithBindingAttributes
                                            End If
                                        Next

                                        Dim generatedDiagnostics As Boolean = False
                                        Dim data As VisualBasicAttributeData = (New EarlyWellKnownAttributeBinder(Me, binder)).GetAttribute(attr, attributeType, generatedDiagnostics)
                                        If Not data.HasErrors AndAlso Not generatedDiagnostics AndAlso
                                           data.IsTargetAttribute(AttributeDescription.MyGroupCollectionAttribute) Then
                                            ' Looks like we've found MyGroupCollectionAttribute
                                            If attributeData IsNot Nothing Then
                                                ' Ambiguity, the attribute cannot be applied multiple times. Let's ignore all of them,
                                                ' an error about multiple applications will be reported later, when all attributes are 
                                                ' bound and validated.
                                                attributeData = Nothing
                                                GoTo DoneWithBindingAttributes
                                            End If

                                            attributeData = data
                                            attributeSyntax = attr
                                        End If
                                    End If
                                End If
                            End If
                        Next
                    Next
                End If
            Next

DoneWithBindingAttributes:
            If attributeData Is Nothing Then
                binder = Nothing
                attributeSyntax = Nothing
            End If

            Return attributeData
        End Function

        Private Shared Sub FindGroupClassBaseTypes(nameParts() As String, current As NamespaceOrTypeSymbol, nextPart As Integer, candidates As ArrayBuilder(Of NamedTypeSymbol))
            ' Bindable::FindBaseInMyGroupCollection

            If nextPart = nameParts.Length Then
                If current.Kind = SymbolKind.NamedType Then
                    Dim named = DirectCast(current, NamedTypeSymbol)

                    If named.TypeKind = TypeKind.Class AndAlso Not named.IsNotInheritable Then
                        candidates.Add(named)
                    End If
                End If

                Return
            End If

            Dim name As String = nameParts(nextPart)
            nextPart += 1
            For Each member As Symbol In current.GetMembers(name)
                Select Case member.Kind
                    Case SymbolKind.Namespace, SymbolKind.NamedType
                        FindGroupClassBaseTypes(nameParts, DirectCast(member, NamespaceOrTypeSymbol), nextPart, candidates)
                End Select
            Next
        End Sub

        Private Shared Sub GetMyGroupCollectionTypes(
            ns As NamespaceSymbol,
            baseTypes As ArrayBuilder(Of NamedTypeSymbol),
            binder As Binder,
            collectionTypes As ArrayBuilder(Of KeyValuePair(Of NamedTypeSymbol, Integer))
        )
            For Each member As Symbol In ns.GetMembersUnordered()
                Select Case member.Kind
                    Case SymbolKind.NamedType
                        Dim named = TryCast(member, SourceNamedTypeSymbol)

                        ' See Bindable::ScanAndLoadMyGroupCollectionMembers, Bindable::CanBeMyGroupCollectionMember
                        ' CONSIDER: The IsAccessible check is probably redundant because top level types are always accessible within the same module.
                        '           We can remove it if it becomes a perf issue.
                        If named IsNot Nothing AndAlso
                           Not named.IsImplicitlyDeclared AndAlso
                           named.TypeKind = TypeKind.Class AndAlso
                           Not named.IsGenericType AndAlso
                           Not named.IsMustInherit AndAlso
                           binder.IsAccessible(named, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded) Then
                            Dim matchingItem As Integer = FindBaseInMyGroupCollection(named, baseTypes)

                            If matchingItem >= 0 AndAlso
                               MyGroupCollectionCandidateHasPublicParameterlessConstructor(named) Then
                                collectionTypes.Add(New KeyValuePair(Of NamedTypeSymbol, Integer)(named, matchingItem))
                            End If
                        End If

                    Case SymbolKind.Namespace
                        GetMyGroupCollectionTypes(DirectCast(member, NamespaceSymbol), baseTypes, binder, collectionTypes)
                End Select
            Next
        End Sub

        Private Shared Function FindBaseInMyGroupCollection(classType As NamedTypeSymbol, bases As ArrayBuilder(Of NamedTypeSymbol)) As Integer
            ' See Bindable::FindBaseInMyGroupCollection
            ' Names in the MyGroupCollectionAttribute are matched from left to right to bases from most derived to the least derived.
            classType = classType.BaseTypeNoUseSiteDiagnostics

            While classType IsNot Nothing AndAlso Not classType.IsObjectType()
                Dim result As Integer = 0
                For Each candidate As NamedTypeSymbol In bases
                    If candidate Is Nothing Then
                        ' This means that we are switching to the next item in the comma-separated list of base type names.
                        result += 1
                    ElseIf classType.OriginalDefinition Is candidate Then
                        Return result
                    End If
                Next

                classType = classType.BaseTypeNoUseSiteDiagnostics
            End While

            Return -1 ' Haven't found a match.
        End Function

        Private Shared Function MyGroupCollectionCandidateHasPublicParameterlessConstructor(candidate As SourceNamedTypeSymbol) As Boolean
            ' Simply calling HasPublicParameterlessConstructor might get us in a cycle.
            Debug.Assert(candidate.TypeKind = TypeKind.Class)
            If candidate.MembersHaveBeenCreated Then
                Return HasPublicParameterlessConstructor(candidate) = ConstructorConstraintError.None
            Else
                Return candidate.InferFromSyntaxIfClassWillHavePublicParameterlessConstructor()
            End If
        End Function

#If DEBUG Then
        Protected Overrides Sub VerifyMembers()
            If Me.TypeKind = TypeKind.Class Then
                Debug.Assert(MembersHaveBeenCreated)
                Dim constructorConstraintError As ConstructorConstraintError = HasPublicParameterlessConstructor(Me)
                If InferFromSyntaxIfClassWillHavePublicParameterlessConstructor() Then
                    Debug.Assert(constructorConstraintError = ConstructorConstraintError.None OrElse constructorConstraintError = ConstructorConstraintError.HasRequiredMembers)
                Else
                    Debug.Assert(constructorConstraintError = ConstructorConstraintError.NoPublicParameterlessConstructor)
                End If
            End If
        End Sub
#End If

        Friend Function InferFromSyntaxIfClassWillHavePublicParameterlessConstructor() As Boolean
            Debug.Assert(Me.TypeKind = TypeKind.Class)
            Dim diagnostics = DiagnosticBag.GetInstance()
            Dim haveInstanceConstructor As Boolean = False

            For Each syntaxRef In SyntaxReferences
                Dim node = syntaxRef.GetSyntax()

                ' Set up a binder for this part of the type.
                Dim binder As Binder = BinderBuilder.CreateBinderForType(ContainingSourceModule, syntaxRef.SyntaxTree, Me)

                Dim typeBlock = DirectCast(node, TypeBlockSyntax)
                For Each memberSyntax In typeBlock.Members
                    Dim constructorSyntax As SubNewStatementSyntax

                    Select Case memberSyntax.Kind
                        Case SyntaxKind.ConstructorBlock
                            constructorSyntax = DirectCast(memberSyntax, ConstructorBlockSyntax).SubNewStatement
                        Case SyntaxKind.SubNewStatement
                            constructorSyntax = DirectCast(memberSyntax, SubNewStatementSyntax)
                        Case Else
                            constructorSyntax = Nothing
                    End Select

                    If constructorSyntax IsNot Nothing Then
                        Dim modifiers As SourceMemberFlags = SourceMethodSymbol.DecodeConstructorModifiers(constructorSyntax.Modifiers, Me, binder, diagnostics).AllFlags

                        If (modifiers And SourceMemberFlags.Shared) = 0 Then
                            If constructorSyntax.ParameterList Is Nothing OrElse constructorSyntax.ParameterList.Parameters.Count = 0 Then
                                diagnostics.Free()
                                Return (modifiers And SourceMemberFlags.AccessibilityMask) = SourceMemberFlags.AccessibilityPublic
                            End If

                            haveInstanceConstructor = True
                        End If
                    End If
                Next
            Next

            diagnostics.Free()
            Return Not haveInstanceConstructor AndAlso Not Me.IsMustInherit
        End Function

        ''' <summary>
        ''' Used to sort types - members of group collection.
        ''' </summary>
        Private Class GroupCollectionComparer
            Implements IComparer(Of KeyValuePair(Of NamedTypeSymbol, Integer))

            Public Shared ReadOnly Singleton As New GroupCollectionComparer

            Private Sub New()
            End Sub

            Public Function Compare(x As KeyValuePair(Of NamedTypeSymbol, Integer), y As KeyValuePair(Of NamedTypeSymbol, Integer)) As Integer Implements IComparer(Of KeyValuePair(Of NamedTypeSymbol, Integer)).Compare
                Return IdentifierComparison.Compare(x.Key.Name, y.Key.Name)
            End Function
        End Class

        Private Sub AddSyntheticMyGroupCollectionProperty(
            targetType As NamedTypeSymbol,
            mangleNames As Boolean,
            createMethod As String,
            disposeMethod As String,
            defaultInstanceAlias As String,
            membersBuilder As MembersAndInitializersBuilder,
            binder As Binder,
            attributeSyntax As AttributeSyntax,
            diagnostics As BindingDiagnosticBag
        )
            Dim propertyName As String

            If mangleNames Then
                propertyName = targetType.ToDisplayString()
                propertyName = propertyName.Replace("."c, "_"c)
            Else
                propertyName = targetType.Name
            End If

            Dim fieldName As String = "m_" & propertyName

            ' For now let reject any clash. The new member is a synthetic property and no overloads or other members with the same name should
            ' be allowed
            Dim conflictsWith As Symbol = Nothing
            Dim nestedTypes = GetTypeMembersDictionary()
            Dim isWinMd = Me.IsCompilationOutputWinMdObj()

            If ConflictsWithExistingMemberOrType(propertyName, membersBuilder, nestedTypes, conflictsWith) OrElse
               ConflictsWithExistingMemberOrType(binder.GetAccessorName(propertyName, MethodKind.PropertyGet, False), membersBuilder, nestedTypes, conflictsWith) OrElse
               (disposeMethod.Length > 0 AndAlso ConflictsWithExistingMemberOrType(binder.GetAccessorName(propertyName, MethodKind.PropertySet, isWinMd), membersBuilder, nestedTypes, conflictsWith)) OrElse
               ConflictsWithExistingMemberOrType(fieldName, membersBuilder, nestedTypes, conflictsWith) Then
                binder.ReportDiagnostic(diagnostics, attributeSyntax, ERRID.ERR_PropertyNameConflictInMyCollection, conflictsWith, targetType)
                Return
            End If

            Dim prop As New SynthesizedMyGroupCollectionPropertySymbol(Me, attributeSyntax, propertyName, fieldName, targetType, createMethod, disposeMethod, defaultInstanceAlias)

            AddMember(prop.AssociatedField, binder, membersBuilder, omitDiagnostics:=True)
            AddMember(prop, binder, membersBuilder, omitDiagnostics:=True)
            AddMember(prop.GetMethod, binder, membersBuilder, omitDiagnostics:=True)

            If prop.SetMethod IsNot Nothing Then
                AddMember(prop.SetMethod, binder, membersBuilder, omitDiagnostics:=True)
            End If
        End Sub

        Private Shared Function ConflictsWithExistingMemberOrType(
            name As String,
            membersBuilder As MembersAndInitializersBuilder,
            nestedTypes As Dictionary(Of String, ImmutableArray(Of NamedTypeSymbol)),
            <Out> ByRef conflictsWith As Symbol
        ) As Boolean
            Dim members As ArrayBuilder(Of Symbol) = Nothing
            Dim types As ImmutableArray(Of NamedTypeSymbol) = Nothing

            If membersBuilder.Members.TryGetValue(name, members) Then
                conflictsWith = members(0)

            ElseIf nestedTypes.TryGetValue(name, types) Then
                conflictsWith = types(0)

            Else
                conflictsWith = Nothing
            End If

            Return conflictsWith IsNot Nothing
        End Function

    End Class

End Namespace
