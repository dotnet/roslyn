' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents an attribute applied to a Symbol.
    ''' </summary>
    Friend MustInherit Class VisualBasicAttributeData
        Inherits AttributeData

        Private _lazyIsSecurityAttribute As ThreeState = ThreeState.Unknown

        ''' <summary>
        ''' Gets the attribute class being applied.
        ''' </summary>
        Public MustOverride Shadows ReadOnly Property AttributeClass As NamedTypeSymbol

        ''' <summary>
        ''' Gets the constructor used in this application of the attribute.
        ''' </summary>
        Public MustOverride Shadows ReadOnly Property AttributeConstructor As MethodSymbol

        ''' <summary>
        ''' Gets a reference to the source for this application of the attribute. Returns null for applications of attributes on metadata Symbols.
        ''' </summary>
        Public MustOverride Shadows ReadOnly Property ApplicationSyntaxReference As SyntaxReference

        ''' <summary>
        ''' Gets the list of constructor arguments specified by this application of the attribute.  This list contains both positional arguments
        ''' and named arguments that are formal parameters to the constructor.
        ''' </summary>
        Public Shadows ReadOnly Property ConstructorArguments As IEnumerable(Of TypedConstant)
            Get
                Return Me.CommonConstructorArguments
            End Get
        End Property

        ''' <summary>
        ''' Gets the list of named field or property value arguments specified by this application of the attribute.
        ''' </summary>
        Public Shadows ReadOnly Property NamedArguments As IEnumerable(Of KeyValuePair(Of String, TypedConstant))
            Get
                Return Me.CommonNamedArguments
            End Get
        End Property

        Friend MustOverride Overrides ReadOnly Property HasErrors As Boolean

        Friend MustOverride ReadOnly Property ErrorInfo As DiagnosticInfo

        Friend MustOverride Overrides ReadOnly Property IsConditionallyOmitted As Boolean

        ''' <summary>
        ''' Compares the namespace and type name with the attribute's namespace and type name.  Returns true if they are the same.
        ''' </summary>
        Friend MustOverride Function IsTargetAttribute(
            namespaceName As String,
            typeName As String,
            Optional ignoreCase As Boolean = False
        ) As Boolean

        Friend Function IsTargetAttribute(description As AttributeDescription) As Boolean
            Return GetTargetAttributeSignatureIndex(description) <> -1
        End Function

        Friend MustOverride Function GetTargetAttributeSignatureIndex(description As AttributeDescription) As Integer

        ''' <summary>
        ''' Checks if an applied attribute with the given attributeType matches the namespace name and type name of the given early attribute's description
        ''' and the attribute description has a signature with parameter count equal to the given attribute syntax's argument list count.
        ''' NOTE: We don't allow early decoded attributes to have optional parameters.
        ''' </summary>
        Friend Overloads Shared Function IsTargetEarlyAttribute(attributeType As NamedTypeSymbol, attributeSyntax As AttributeSyntax, description As AttributeDescription) As Boolean
            Debug.Assert(Not attributeType.IsErrorType())

            Dim argumentCount As Integer = If(attributeSyntax.ArgumentList IsNot Nothing,
                                              attributeSyntax.ArgumentList.Arguments.Where(Function(arg) arg.Kind = SyntaxKind.SimpleArgument AndAlso Not arg.IsNamed).Count,
                                              0)
            Return AttributeData.IsTargetEarlyAttribute(attributeType, argumentCount, description)
        End Function

        ''' <summary>
        ''' Returns the <see cref="System.String"/> that represents the current AttributeData.
        ''' </summary>
        ''' <returns>A <see cref="System.String"/> that represents the current AttributeData.</returns>
        Public Overrides Function ToString() As String
            If Me.AttributeClass IsNot Nothing Then
                Dim className As String = Me.AttributeClass.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat)

                If Not Me.CommonConstructorArguments.Any() And Not Me.CommonNamedArguments.Any() Then
                    Return className
                End If

                Dim pooledStrbuilder = PooledStringBuilder.GetInstance()
                Dim stringBuilder As StringBuilder = pooledStrbuilder.Builder

                stringBuilder.Append(className)
                stringBuilder.Append("(")

                Dim first As Boolean = True

                For Each constructorArgument In Me.CommonConstructorArguments
                    If Not first Then
                        stringBuilder.Append(", ")
                    End If

                    stringBuilder.Append(constructorArgument.ToVisualBasicString())
                    first = False
                Next

                For Each namedArgument In Me.CommonNamedArguments
                    If Not first Then
                        stringBuilder.Append(", ")
                    End If

                    stringBuilder.Append(namedArgument.Key)
                    stringBuilder.Append(":=")
                    stringBuilder.Append(namedArgument.Value.ToVisualBasicString())
                    first = False
                Next

                stringBuilder.Append(")")

                Return pooledStrbuilder.ToStringAndFree()
            End If

            Return MyBase.ToString()
        End Function

#Region "AttributeData Implementation"

        ''' <summary>
        ''' Gets the attribute class being applied as an <see cref="INamedTypeSymbol"/>
        ''' </summary>
        Protected Overrides ReadOnly Property CommonAttributeClass As INamedTypeSymbol
            Get
                Return AttributeClass()
            End Get
        End Property

        ''' <summary>
        ''' Gets the constructor used in this application of the attribute as an <see cref="IMethodSymbol"/>.
        ''' </summary>
        Protected Overrides ReadOnly Property CommonAttributeConstructor As IMethodSymbol
            Get
                Return AttributeConstructor()
            End Get
        End Property

        ''' <summary>
        ''' Gets a reference to the source for this application of the attribute. Returns null for applications of attributes on metadata Symbols.
        ''' </summary>
        Protected Overrides ReadOnly Property CommonApplicationSyntaxReference As SyntaxReference
            Get
                Return ApplicationSyntaxReference()
            End Get
        End Property
#End Region

#Region "Attribute Decoding"

        Friend Function IsSecurityAttribute(comp As VisualBasicCompilation) As Boolean
            ' CLI spec (Partition II Metadata), section 21.11 "DeclSecurity : 0x0E" states:
            ' SPEC:    If the attribute's type is derived (directly or indirectly) from System.Security.Permissions.SecurityAttribute then
            ' SPEC:    it is a security custom attribute and requires special treatment.

            If _lazyIsSecurityAttribute = ThreeState.Unknown Then
                _lazyIsSecurityAttribute = Me.AttributeClass.IsOrDerivedFromWellKnownClass(WellKnownType.System_Security_Permissions_SecurityAttribute, comp, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded).ToThreeState()
            End If

            Return _lazyIsSecurityAttribute.Value
        End Function

        Friend Sub DecodeSecurityAttribute(Of T As {WellKnownAttributeData, ISecurityAttributeTarget, New})(targetSymbol As Symbol, compilation As VisualBasicCompilation, ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Dim hasErrors As Boolean = False
            Dim action As DeclarativeSecurityAction = Me.DecodeSecurityAttributeAction(targetSymbol, compilation, arguments.AttributeSyntaxOpt, hasErrors, DirectCast(arguments.Diagnostics, BindingDiagnosticBag))
            If Not hasErrors Then
                Dim data As T = arguments.GetOrCreateData(Of T)()
                Dim securityData As SecurityWellKnownAttributeData = data.GetOrCreateData()
                securityData.SetSecurityAttribute(arguments.Index, action, arguments.AttributesCount)

                If Me.IsTargetAttribute(AttributeDescription.PermissionSetAttribute) Then
                    Dim resolvedPathForFixup As String = Me.DecodePermissionSetAttribute(compilation, arguments)
                    If resolvedPathForFixup IsNot Nothing Then
                        securityData.SetPathForPermissionSetAttributeFixup(arguments.Index, resolvedPathForFixup, arguments.AttributesCount)
                    End If
                End If
            End If
        End Sub

        Private Function DecodeSecurityAttributeAction(
            targetSymbol As Symbol,
            compilation As VisualBasicCompilation,
            nodeOpt As AttributeSyntax,
            ByRef hasErrors As Boolean,
            diagnostics As BindingDiagnosticBag
        ) As DeclarativeSecurityAction
            Debug.Assert(Not hasErrors)
            Debug.Assert(Me.IsSecurityAttribute(compilation))
            Debug.Assert(targetSymbol.Kind = SymbolKind.Assembly OrElse targetSymbol.Kind = SymbolKind.NamedType OrElse targetSymbol.Kind = SymbolKind.Method)

            If Me.AttributeConstructor.ParameterCount = 0 Then
                ' NOTE:    Security custom attributes must have a valid SecurityAction as its first argument, we have none here.
                ' NOTE:    Ideally, we should always generate 'BC31205: First argument to a security attribute must be a valid SecurityAction' for this case.
                ' NOTE:    However, native compiler allows applying System.Security.Permissions.HostProtectionAttribute attribute without any argument and uses 
                ' NOTE:    SecurityAction.LinkDemand as the default SecurityAction in this case. We maintain compatibility with the native compiler for this case.

                ' BREAKING CHANGE: Even though the native compiler intends to allow only HostProtectionAttribute to be applied without any arguments,
                '                  it doesn't quite do this correctly 

                ' The implementation issue leads to the native compiler allowing any user defined security attribute with a parameterless constructor and a named property argument as the first
                ' attribute argument to have the above mentioned behavior, even though the comment clearly mentions that this behavior was intended only for the HostProtectionAttribute.
                ' We currently allow this case only for the HostProtectionAttribute. In future if need arises, we can exactly match native compiler's behavior.

                If Me.IsTargetAttribute(AttributeDescription.HostProtectionAttribute) Then
                    Return DeclarativeSecurityAction.LinkDemand
                End If
            Else
                Dim firstArg As TypedConstant = Me.CommonConstructorArguments.FirstOrDefault()
                Dim firstArgType = DirectCast(firstArg.TypeInternal, TypeSymbol)
                Dim useSiteInfo As New CompoundUseSiteInfo(Of AssemblySymbol)(diagnostics, compilation.Assembly)
                If firstArgType IsNot Nothing AndAlso firstArgType.IsOrDerivedFromWellKnownClass(WellKnownType.System_Security_Permissions_SecurityAction, compilation, useSiteInfo) Then
                    Return ValidateSecurityAction(firstArg, targetSymbol, nodeOpt, diagnostics, hasErrors)
                End If

                diagnostics.Add(If(nodeOpt IsNot Nothing, nodeOpt.Name.GetLocation, NoLocation.Singleton), useSiteInfo)
            End If

            ' BC31211: First argument to a security attribute must be a valid SecurityAction
            diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_SecurityAttributeMissingAction),
                            If(nodeOpt IsNot Nothing, nodeOpt.Name.GetLocation, NoLocation.Singleton))

            hasErrors = True

            Return Nothing
        End Function

        Friend Shared Function GetArgumentLocation(nodeOpt As AttributeSyntax, argumentIndex As Integer) As Location
            Return GetArgumentDisplayAndLocation(nodeOpt, 0, argumentIndex).Location
        End Function

        Private Shared Function GetArgumentDisplayAndLocation(nodeOpt As AttributeSyntax, value As Integer, argumentIndex As Integer) As (ArgumentDisplay As String, Location As Location)
            If nodeOpt IsNot Nothing Then
                If nodeOpt.ArgumentList IsNot Nothing AndAlso nodeOpt.ArgumentList.Arguments.Count > argumentIndex Then
                    Dim arg As ArgumentSyntax = nodeOpt.ArgumentList.Arguments(argumentIndex)
                    Return (arg.ToString(), arg.GetLocation())
                Else
                    Return (value.ToString(Globalization.CultureInfo.InvariantCulture), nodeOpt.GetLocation())
                End If
            Else
                Return ("", NoLocation.Singleton)
            End If
        End Function

        Friend Shared Function GetFirstArgumentLocation(nodeOpt As AttributeSyntax) As Location
            Return GetArgumentLocation(nodeOpt, argumentIndex:=0)
        End Function

        Private Shared Function GetFirstArgumentDisplayAndLocation(nodeOpt As AttributeSyntax, value As Integer) As (ArgumentDisplay As String, Location As Location)
            Return GetArgumentDisplayAndLocation(nodeOpt, value, argumentIndex:=0)
        End Function

        Private Function ValidateSecurityAction(
            typedValue As TypedConstant,
            targetSymbol As Symbol,
            nodeOpt As AttributeSyntax,
            diagnostics As BindingDiagnosticBag,
            <Out> ByRef hasErrors As Boolean
        ) As DeclarativeSecurityAction
            Debug.Assert(targetSymbol.Kind = SymbolKind.Assembly OrElse targetSymbol.Kind = SymbolKind.NamedType OrElse targetSymbol.Kind = SymbolKind.Method)

            Dim securityAction As Integer = CInt(typedValue.ValueInternal)
            hasErrors = False
            Dim isPermissionRequestAction As Boolean

            Select Case securityAction
                Case DeclarativeSecurityAction.InheritanceDemand,
                     DeclarativeSecurityAction.LinkDemand

                    If Me.IsTargetAttribute(AttributeDescription.PrincipalPermissionAttribute) Then
                        ' BC31215: SecurityAction value '{0}' is invalid for PrincipalPermission attribute
                        Dim displayAndLocation As (ArgumentDisplay As String, Location As Location) = GetFirstArgumentDisplayAndLocation(nodeOpt, securityAction)
                        diagnostics.Add(ERRID.ERR_PrincipalPermissionInvalidAction, displayAndLocation.Location, displayAndLocation.ArgumentDisplay)

                        hasErrors = True
                        Return DeclarativeSecurityAction.None
                    End If

                    isPermissionRequestAction = False

                Case 1
                    ' Native compiler allows security action value 1 even though there is no corresponding field in 
                    ' System.Security.Permissions.SecurityAction enum.
                    ' We will maintain compatibility.

                Case DeclarativeSecurityAction.Assert,
                     DeclarativeSecurityAction.Demand,
                     DeclarativeSecurityAction.PermitOnly,
                     DeclarativeSecurityAction.Deny

                    isPermissionRequestAction = False

                Case DeclarativeSecurityAction.RequestMinimum,
                     DeclarativeSecurityAction.RequestOptional,
                     DeclarativeSecurityAction.RequestRefuse

                    isPermissionRequestAction = True

                Case Else
                    ' BC31214: SecurityAction value '{0}' is invalid for security attributes applied to a type or a method.
                    Dim displayAndLocation As (ArgumentDisplay As String, Location As Location) = GetFirstArgumentDisplayAndLocation(nodeOpt, securityAction)
                    diagnostics.Add(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, displayAndLocation.Location, displayAndLocation.ArgumentDisplay)

                    hasErrors = True
                    Return DeclarativeSecurityAction.None
            End Select

            If isPermissionRequestAction Then
                If targetSymbol.Kind = SymbolKind.NamedType OrElse targetSymbol.Kind = SymbolKind.Method Then
                    ' Types and methods cannot take permission requests.

                    ' BC31214: SecurityAction value '{0}' is invalid for security attributes applied to a type or a method.
                    Dim displayAndLocation As (ArgumentDisplay As String, Location As Location) = GetFirstArgumentDisplayAndLocation(nodeOpt, securityAction)
                    diagnostics.Add(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, displayAndLocation.Location, displayAndLocation.ArgumentDisplay)

                    hasErrors = True
                    Return DeclarativeSecurityAction.None
                End If

            ElseIf targetSymbol.Kind = SymbolKind.Assembly Then
                ' Assemblies cannot take declarative security.

                ' BC31213: SecurityAction value '{0}' is invalid for security attributes applied to an assembly.
                Dim displayAndLocation As (ArgumentDisplay As String, Location As Location) = GetFirstArgumentDisplayAndLocation(nodeOpt, securityAction)
                diagnostics.Add(ERRID.ERR_SecurityAttributeInvalidActionAssembly, displayAndLocation.Location, displayAndLocation.ArgumentDisplay)

                hasErrors = True
                Return DeclarativeSecurityAction.None
            End If

            Return CType(securityAction, DeclarativeSecurityAction)
        End Function

        ''' <summary>
        ''' Decodes PermissionSetAttribute applied in source to determine if it needs any fixup during codegen.
        ''' </summary>
        ''' <remarks>
        ''' PermissionSetAttribute needs fixup when it contains an assignment to the 'File' property as a single named attribute argument.
        ''' Fixup performed is ported from SecurityAttributes::FixUpPermissionSetAttribute.
        ''' It involves following steps:
        '''  1) Verifying that the specified file name resolves to a valid path.
        '''  2) Reading the contents of the file into a byte array.
        '''  3) Convert each byte in the file content into two bytes containing hexa-decimal characters.
        '''  4) Replacing the 'File = fileName' named argument with 'Hex = hexFileContent' argument, where hexFileContent is the converted output from step 3) above.
        '''
        ''' Step 1) is performed in this method, i.e. during binding.
        ''' Remaining steps are performed during serialization as we want to avoid retaining the entire file contents throughout the binding/codegen pass.
        ''' See <see cref="Microsoft.CodeAnalysis.CodeGen.PermissionSetAttributeWithFileReference"/> for remaining fixup steps.
        ''' </remarks>
        ''' <returns>String containing the resolved file path if PermissionSetAttribute needs fixup during codegen, null otherwise.</returns>
        Friend Function DecodePermissionSetAttribute(compilation As VisualBasicCompilation, ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation)) As String
            Dim resolvedFilePath As String = Nothing
            Dim namedArgs = Me.CommonNamedArguments

            If namedArgs.Length = 1 Then
                Dim namedArg = namedArgs(0)
                Dim attrType As NamedTypeSymbol = Me.AttributeClass
                Dim filePropName As String = PermissionSetAttributeWithFileReference.FilePropertyName
                Dim hexPropName As String = PermissionSetAttributeWithFileReference.HexPropertyName

                If namedArg.Key = filePropName AndAlso
                    PermissionSetAttributeTypeHasRequiredProperty(attrType, filePropName) Then

                    ' resolve file prop path
                    Dim fileName = DirectCast(namedArg.Value.ValueInternal, String)
                    Dim resolver = compilation.Options.XmlReferenceResolver
                    resolvedFilePath = If(resolver IsNot Nothing, resolver.ResolveReference(fileName, baseFilePath:=Nothing), Nothing)

                    If resolvedFilePath Is Nothing Then

                        ' BC31216: Unable to resolve file path '{0}' specified for the named argument '{1}' for PermissionSet attribute.
                        Dim argSyntaxLocation As Location = If(arguments.AttributeSyntaxOpt IsNot Nothing,
                                                               arguments.AttributeSyntaxOpt.ArgumentList.Arguments(1).GetLocation(),
                                                               NoLocation.Singleton)
                        DirectCast(arguments.Diagnostics, BindingDiagnosticBag).Add(ERRID.ERR_PermissionSetAttributeInvalidFile, argSyntaxLocation, If(fileName, "<empty>"), filePropName)

                    ElseIf (Not PermissionSetAttributeTypeHasRequiredProperty(attrType, hexPropName)) Then

                        ' PermissionSetAttribute was defined in user source, but doesn't have the required Hex property.
                        ' Native compiler still emits the file content as named assignment to 'Hex' property, but this leads to a runtime exception.
                        ' We instead skip the fixup and emit the file property.

                        ' CONSIDER: We may want to consider taking a breaking change and generating an error here.

                        Return Nothing
                    End If
                End If
            End If

            Return resolvedFilePath
        End Function

        ' This method checks if the given PermissionSetAttribute type has a property member with the given propName which is 
        ' writable, non-generic, public and of string type.
        Private Shared Function PermissionSetAttributeTypeHasRequiredProperty(permissionSetType As NamedTypeSymbol, propName As String) As Boolean
            Dim members = permissionSetType.GetMembers(propName)
            If members.Length = 1 AndAlso members(0).Kind = SymbolKind.Property Then
                Dim [property] = DirectCast(members(0), PropertySymbol)
                If [property].Type IsNot Nothing AndAlso [property].Type.SpecialType = SpecialType.System_String AndAlso
                    [property].DeclaredAccessibility = Accessibility.Public AndAlso [property].GetArity() = 0 AndAlso
                    [property].HasSet AndAlso [property].SetMethod.DeclaredAccessibility = Accessibility.Public Then

                    Return True
                End If
            End If

            Return False
        End Function

        Friend Sub DecodeClassInterfaceAttribute(nodeOpt As AttributeSyntax, diagnostics As BindingDiagnosticBag)
            Debug.Assert(Not Me.HasErrors)

            Dim ctorArgument As TypedConstant = Me.CommonConstructorArguments(0)
            Debug.Assert(ctorArgument.Kind = TypedConstantKind.Enum OrElse ctorArgument.Kind = TypedConstantKind.Primitive)

            Dim interfaceType As ClassInterfaceType = If(ctorArgument.Kind = TypedConstantKind.Enum,
                                                         ctorArgument.DecodeValue(Of ClassInterfaceType)(SpecialType.System_Enum),
                                                         CType(ctorArgument.DecodeValue(Of Short)(SpecialType.System_Int16), ClassInterfaceType))

            Select Case interfaceType
                Case ClassInterfaceType.None, Cci.Constants.ClassInterfaceType_AutoDispatch, Cci.Constants.ClassInterfaceType_AutoDual
                    Exit Select
                Case Else
                    Dim location As Location = GetFirstArgumentLocation(nodeOpt)
                    diagnostics.Add(ERRID.ERR_BadAttribute1, location, Me.AttributeClass)
            End Select
        End Sub

        Friend Sub DecodeInterfaceTypeAttribute(node As AttributeSyntax, diagnostics As BindingDiagnosticBag)
            Dim discarded As ComInterfaceType = Nothing
            If Not DecodeInterfaceTypeAttribute(discarded) Then
                Dim location As Location = GetFirstArgumentLocation(node)
                diagnostics.Add(ERRID.ERR_BadAttribute1, location, Me.AttributeClass)
            End If
        End Sub

        Friend Function DecodeInterfaceTypeAttribute(<Out> ByRef interfaceType As ComInterfaceType) As Boolean
            Debug.Assert(Not Me.HasErrors)

            Dim ctorArgument As TypedConstant = Me.CommonConstructorArguments(0)
            Debug.Assert(ctorArgument.Kind = TypedConstantKind.Enum OrElse ctorArgument.Kind = TypedConstantKind.Primitive)

            interfaceType = If(ctorArgument.Kind = TypedConstantKind.Enum,
                               ctorArgument.DecodeValue(Of ComInterfaceType)(SpecialType.System_Enum),
                               CType(ctorArgument.DecodeValue(Of Short)(SpecialType.System_Int16), ComInterfaceType))

            Select Case interfaceType
                Case Cci.Constants.ComInterfaceType_InterfaceIsDual, Cci.Constants.ComInterfaceType_InterfaceIsIDispatch, ComInterfaceType.InterfaceIsIInspectable, ComInterfaceType.InterfaceIsIUnknown
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Friend Function DecodeTypeLibTypeAttribute() As Cci.TypeLibTypeFlags
            Debug.Assert(Not Me.HasErrors)

            Dim ctorArgument As TypedConstant = Me.CommonConstructorArguments(0)
            Debug.Assert(ctorArgument.Kind = TypedConstantKind.Enum OrElse ctorArgument.Kind = TypedConstantKind.Primitive)

            Return If(ctorArgument.Kind = TypedConstantKind.Enum,
                      ctorArgument.DecodeValue(Of Cci.TypeLibTypeFlags)(SpecialType.System_Enum),
                      CType(ctorArgument.DecodeValue(Of Short)(SpecialType.System_Int16), Cci.TypeLibTypeFlags))
        End Function

        Friend Function DecodeGuidAttribute(nodeOpt As AttributeSyntax, diagnostics As BindingDiagnosticBag) As String
            Debug.Assert(Not Me.HasErrors)

            Dim guidString As String = Me.GetConstructorArgument(Of String)(0, SpecialType.System_String)

            ' Native compiler allows only a specific GUID format: "D" format (32 digits separated by hyphens)
            Dim guidVal As Guid
            If Not Guid.TryParseExact(guidString, "D", guidVal) Then
                Dim location As Location = GetFirstArgumentLocation(nodeOpt)
                diagnostics.Add(ERRID.ERR_BadAttributeUuid2, location, Me.AttributeClass, If(guidString, ObjectDisplay.NullLiteral))
                guidString = String.Empty
            End If

            Return guidString
        End Function

        Friend Function DecodeDefaultMemberAttribute() As String
            Debug.Assert(Not Me.HasErrors)

            Return Me.GetConstructorArgument(Of String)(0, SpecialType.System_String)
        End Function

        Private Protected NotOverridable Overrides Function IsStringProperty(memberName As String) As Boolean
            If AttributeClass IsNot Nothing Then
                For Each member In AttributeClass.GetMembers(memberName)
                    Dim prop = TryCast(member, PropertySymbol)
                    If prop?.Type.SpecialType = SpecialType.System_String Then
                        Return True
                    End If
                Next
            End If

            Return False
        End Function
#End Region

        ''' <summary>
        '''  This method determines if an applied attribute must be emitted. 
        ''' Some attributes appear in symbol model to reflect the source code, but should not be emitted.
        '''  </summary>
        Friend Function ShouldEmitAttribute(target As Symbol, isReturnType As Boolean, emittingAssemblyAttributesInNetModule As Boolean) As Boolean
            Debug.Assert(TypeOf target Is SourceAssemblySymbol OrElse TypeOf target.ContainingAssembly Is SourceAssemblySymbol)

            ' Attribute type is conditionally omitted if both the following are true:
            '  (a) It has at least one applied conditional attribute AND
            '  (b) None of conditional symbols are true at the attribute source location.
            If Me.IsConditionallyOmitted Then
                Return False
            End If

            '     // UNDONE:harishk - spec. issue
            '     // how to deal with CLS Compliant attributes present on both Modules and Assemblies ?
            '     // Also this might be a issue for other well known attributes too ?
            '     //
            '     // For CLSCompliance - Ignore Module attributes for Assemblies and vice-versa

            Select Case target.Kind
                Case SymbolKind.Assembly
                    If (Not emittingAssemblyAttributesInNetModule AndAlso
                            (IsTargetAttribute(AttributeDescription.AssemblyCultureAttribute) OrElse
                             IsTargetAttribute(AttributeDescription.AssemblyVersionAttribute) OrElse
                             IsTargetAttribute(AttributeDescription.AssemblyFlagsAttribute) OrElse
                             IsTargetAttribute(AttributeDescription.AssemblyAlgorithmIdAttribute))) OrElse
                       (IsTargetAttribute(AttributeDescription.CLSCompliantAttribute) AndAlso
                            target.DeclaringCompilation.Options.OutputKind = OutputKind.NetModule) OrElse
                       IsTargetAttribute(AttributeDescription.TypeForwardedToAttribute) OrElse
                       Me.IsSecurityAttribute(target.DeclaringCompilation) Then
                        Return False
                    End If

                Case SymbolKind.Event
                    If IsTargetAttribute(AttributeDescription.SpecialNameAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.NonSerializedAttribute) Then
                        Return False
                    End If

                Case SymbolKind.Field
                    If IsTargetAttribute(AttributeDescription.SpecialNameAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.NonSerializedAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.FieldOffsetAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.MarshalAsAttribute) Then
                        Return False
                    End If

                Case SymbolKind.Method
                    If isReturnType Then
                        If IsTargetAttribute(AttributeDescription.MarshalAsAttribute) Then
                            Return False
                        End If
                    Else
                        If IsTargetAttribute(AttributeDescription.SpecialNameAttribute) OrElse
                           IsTargetAttribute(AttributeDescription.MethodImplAttribute) OrElse
                           IsTargetAttribute(AttributeDescription.DllImportAttribute) OrElse
                           IsTargetAttribute(AttributeDescription.PreserveSigAttribute) OrElse
                           Me.IsSecurityAttribute(target.DeclaringCompilation) Then
                            Return False
                        End If
                    End If

                Case SymbolKind.NetModule
                    If (IsTargetAttribute(AttributeDescription.CLSCompliantAttribute) AndAlso
                            target.DeclaringCompilation.Options.OutputKind <> OutputKind.NetModule) Then
                        Return False
                    End If
                Case SymbolKind.NamedType
                    If IsTargetAttribute(AttributeDescription.SpecialNameAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.ComImportAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.SerializableAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.StructLayoutAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.WindowsRuntimeImportAttribute) OrElse
                       Me.IsSecurityAttribute(target.DeclaringCompilation) Then
                        Return False
                    End If

                Case SymbolKind.Parameter
                    If IsTargetAttribute(AttributeDescription.OptionalAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.MarshalAsAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.InAttribute) OrElse
                       IsTargetAttribute(AttributeDescription.OutAttribute) Then
                        Return False
                    End If

                Case SymbolKind.Property
                    If IsTargetAttribute(AttributeDescription.SpecialNameAttribute) Then
                        Return False
                    End If
            End Select

            Return True
        End Function
    End Class

    Friend Module AttributeDataExtensions
        <System.Runtime.CompilerServices.Extension()>
        Public Function IndexOfAttribute(attributes As ImmutableArray(Of VisualBasicAttributeData), description As AttributeDescription) As Integer
            For i As Integer = 0 To attributes.Length - 1
                If attributes(i).IsTargetAttribute(description) Then
                    Return i
                End If
            Next

            Return -1
        End Function
    End Module
End Namespace
