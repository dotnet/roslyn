// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents an attribute applied to a Symbol.
    /// </summary>
    internal abstract partial class CSharpAttributeData : AttributeData
    {
        private ThreeState _lazyIsSecurityAttribute = ThreeState.Unknown;

        /// <summary>
        /// Gets the attribute class being applied.
        /// </summary>
        public new abstract NamedTypeSymbol? AttributeClass { get; }

        /// <summary>
        /// Gets the constructor used in this application of the attribute.
        /// </summary>
        public new abstract MethodSymbol? AttributeConstructor { get; }

        /// <summary>
        /// Gets a reference to the source for this application of the attribute. Returns null for applications of attributes on metadata Symbols.
        /// </summary>
        public new abstract SyntaxReference? ApplicationSyntaxReference { get; }

        // Overridden to be able to apply MemberNotNull to the new members
        [MemberNotNullWhen(true, nameof(AttributeClass), nameof(AttributeConstructor))]
        internal override bool HasErrors
        {
            get
            {
                var hasErrors = base.HasErrors;
                if (!hasErrors)
                {
                    Debug.Assert(AttributeClass is not null);
                    Debug.Assert(AttributeConstructor is not null);
                }

                return hasErrors;
            }
        }

        /// <summary>
        /// Gets the list of constructor arguments specified by this application of the attribute.  This list contains both positional arguments
        /// and named arguments that are formal parameters to the constructor.
        /// </summary>
        public new IEnumerable<TypedConstant> ConstructorArguments
        {
            get { return this.CommonConstructorArguments; }
        }

        /// <summary>
        /// Gets the list of named field or property value arguments specified by this application of the attribute.
        /// </summary>
        public new IEnumerable<KeyValuePair<string, TypedConstant>> NamedArguments
        {
            get { return this.CommonNamedArguments; }
        }

        /// <summary>
        /// Compares the namespace and type name with the attribute's namespace and type name.
        /// Returns true if they are the same.
        /// </summary>
        internal virtual bool IsTargetAttribute(string namespaceName, string typeName)
        {
            Debug.Assert(this.AttributeClass is object);

            if (!this.AttributeClass.Name.Equals(typeName))
            {
                return false;
            }

            if (this.AttributeClass.IsErrorType() && !(this.AttributeClass is MissingMetadataTypeSymbol))
            {
                // Can't guarantee complete name information.
                return false;
            }

            return this.AttributeClass.HasNameQualifier(namespaceName);
        }

        internal bool IsTargetAttribute(Symbol targetSymbol, AttributeDescription description)
        {
            return GetTargetAttributeSignatureIndex(targetSymbol, description) != -1;
        }

        internal abstract int GetTargetAttributeSignatureIndex(Symbol targetSymbol, AttributeDescription description);

        /// <summary>
        /// Checks if an applied attribute with the given attributeType matches the namespace name and type name of the given early attribute's description
        /// and the attribute description has a signature with parameter count equal to the given attribute syntax's argument list count.
        /// NOTE: We don't allow early decoded attributes to have optional parameters.
        /// </summary>
        internal static bool IsTargetEarlyAttribute(NamedTypeSymbol attributeType, AttributeSyntax attributeSyntax, AttributeDescription description)
        {
            Debug.Assert(!attributeType.IsErrorType());

            int argumentCount = (attributeSyntax.ArgumentList != null) ?
                attributeSyntax.ArgumentList.Arguments.Count<AttributeArgumentSyntax>((arg) => arg.NameEquals == null) :
                0;
            return AttributeData.IsTargetEarlyAttribute(attributeType, argumentCount, description);
        }

        // Security attributes, i.e. attributes derived from well-known SecurityAttribute, are matched by type, not constructor signature.
        internal bool IsSecurityAttribute(CSharpCompilation compilation)
        {
            if (_lazyIsSecurityAttribute == ThreeState.Unknown)
            {
                Debug.Assert(!this.HasErrors);

                // CLI spec (Partition II Metadata), section 21.11 "DeclSecurity : 0x0E" states:
                // SPEC:    If the attribute's type is derived (directly or indirectly) from System.Security.Permissions.SecurityAttribute then
                // SPEC:    it is a security custom attribute and requires special treatment.

                // NOTE:    The native C# compiler violates the above and considers only those attributes whose type derives from
                // NOTE:    System.Security.Permissions.CodeAccessSecurityAttribute as security custom attributes.
                // NOTE:    We will follow the specification.
                // NOTE:    See Devdiv Bug #13762 "Custom security attributes deriving from SecurityAttribute are not treated as security attributes" for details.

                // Well-known type SecurityAttribute is optional.
                // Native compiler doesn't generate a use-site error if it is not found, we do the same.
                var wellKnownType = compilation.GetWellKnownType(WellKnownType.System_Security_Permissions_SecurityAttribute);
                Debug.Assert(AttributeClass is object);
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                _lazyIsSecurityAttribute = AttributeClass.IsDerivedFrom(wellKnownType, TypeCompareKind.ConsiderEverything, useSiteInfo: ref discardedUseSiteInfo).ToThreeState();
            }

            return _lazyIsSecurityAttribute.Value();
        }

        // for testing and debugging only

        /// <summary>
        /// Returns the <see cref="System.String"/> that represents the current AttributeData.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current AttributeData.</returns>
        public override string? ToString()
        {
            if (this.AttributeClass is object)
            {
                string className = this.AttributeClass.ToDisplayString(SymbolDisplayFormat.TestFormat);

                if (!this.CommonConstructorArguments.Any() & !this.CommonNamedArguments.Any())
                {
                    return className;
                }

                var pooledStrbuilder = PooledStringBuilder.GetInstance();
                StringBuilder stringBuilder = pooledStrbuilder.Builder;

                stringBuilder.Append(className);
                stringBuilder.Append("(");

                bool first = true;

                foreach (var constructorArgument in this.CommonConstructorArguments)
                {
                    if (!first)
                    {
                        stringBuilder.Append(", ");
                    }

                    stringBuilder.Append(constructorArgument.ToCSharpString());
                    first = false;
                }

                foreach (var namedArgument in this.CommonNamedArguments)
                {
                    if (!first)
                    {
                        stringBuilder.Append(", ");
                    }

                    stringBuilder.Append(namedArgument.Key);
                    stringBuilder.Append(" = ");
                    stringBuilder.Append(namedArgument.Value.ToCSharpString());
                    first = false;
                }

                stringBuilder.Append(")");

                return pooledStrbuilder.ToStringAndFree();
            }

            return base.ToString();
        }

        #region AttributeData Implementation

        /// <summary>
        /// Gets the attribute class being applied as an <see cref="INamedTypeSymbol"/>
        /// </summary>
        protected override INamedTypeSymbol? CommonAttributeClass
        {
            get { return this.AttributeClass.GetPublicSymbol(); }
        }

        /// <summary>
        /// Gets the constructor used in this application of the attribute as an <see cref="IMethodSymbol"/>.
        /// </summary>
        protected override IMethodSymbol? CommonAttributeConstructor
        {
            get { return this.AttributeConstructor.GetPublicSymbol(); }
        }

        /// <summary>
        /// Gets a reference to the source for this application of the attribute. Returns null for applications of attributes on metadata Symbols.
        /// </summary>
        protected override SyntaxReference? CommonApplicationSyntaxReference
        {
            get { return this.ApplicationSyntaxReference; }
        }
        #endregion

        #region Attribute Decoding

        internal void DecodeSecurityAttribute<T>(Symbol targetSymbol, CSharpCompilation compilation, ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
            where T : WellKnownAttributeData, ISecurityAttributeTarget, new()
        {
            Debug.Assert(!this.HasErrors);
            Debug.Assert(arguments.Diagnostics is BindingDiagnosticBag);

            bool hasErrors;
            DeclarativeSecurityAction action = DecodeSecurityAttributeAction(targetSymbol, compilation, arguments.AttributeSyntaxOpt, out hasErrors, (BindingDiagnosticBag)arguments.Diagnostics);

            if (!hasErrors)
            {
                T data = arguments.GetOrCreateData<T>();
                SecurityWellKnownAttributeData securityData = data.GetOrCreateData();
                securityData.SetSecurityAttribute(arguments.Index, action, arguments.AttributesCount);

                if (this.IsTargetAttribute(targetSymbol, AttributeDescription.PermissionSetAttribute))
                {
                    string? resolvedPathForFixup = DecodePermissionSetAttribute(compilation, arguments.AttributeSyntaxOpt, (BindingDiagnosticBag)arguments.Diagnostics);
                    if (resolvedPathForFixup != null)
                    {
                        securityData.SetPathForPermissionSetAttributeFixup(arguments.Index, resolvedPathForFixup, arguments.AttributesCount);
                    }
                }
            }
        }

        internal static void DecodeSkipLocalsInitAttribute<T>(CSharpCompilation compilation, ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
            where T : WellKnownAttributeData, ISkipLocalsInitAttributeTarget, new()
        {
            arguments.GetOrCreateData<T>().HasSkipLocalsInitAttribute = true;
            if (!compilation.Options.AllowUnsafe)
            {
                Debug.Assert(arguments.AttributeSyntaxOpt is object);
                ((BindingDiagnosticBag)arguments.Diagnostics).Add(ErrorCode.ERR_IllegalUnsafe, arguments.AttributeSyntaxOpt.Location);
            }
        }

        internal static void DecodeMemberNotNullAttribute<T>(TypeSymbol type, ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
            where T : WellKnownAttributeData, IMemberNotNullAttributeTarget, new()
        {
            var value = arguments.Attribute.CommonConstructorArguments[0];
            if (value.IsNull)
            {
                return;
            }

            if (value.Kind != TypedConstantKind.Array)
            {
                string? memberName = value.DecodeValue<string>(SpecialType.System_String);
                if (memberName is object)
                {
                    arguments.GetOrCreateData<T>().AddNotNullMember(memberName);
                    ReportBadNotNullMemberIfNeeded(type, arguments, memberName);
                }
            }
            else
            {
                var builder = ArrayBuilder<string>.GetInstance();
                foreach (var member in value.Values)
                {
                    var memberName = member.DecodeValue<string>(SpecialType.System_String);
                    if (memberName is object)
                    {
                        builder.Add(memberName);
                        ReportBadNotNullMemberIfNeeded(type, arguments, memberName);
                    }
                }

                arguments.GetOrCreateData<T>().AddNotNullMember(builder);
                builder.Free();
            }
        }

        private static void ReportBadNotNullMemberIfNeeded(TypeSymbol type, DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments, string memberName)
        {
            foreach (Symbol foundMember in type.GetMembers(memberName))
            {
                if (foundMember.Kind == SymbolKind.Field || foundMember.Kind == SymbolKind.Property)
                {
                    return;
                }
            }

            Debug.Assert(arguments.AttributeSyntaxOpt is object);
            ((BindingDiagnosticBag)arguments.Diagnostics).Add(ErrorCode.WRN_MemberNotNullBadMember, arguments.AttributeSyntaxOpt.Location, memberName);
        }

        internal static void DecodeMemberNotNullWhenAttribute<T>(TypeSymbol type, ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
            where T : WellKnownAttributeData, IMemberNotNullAttributeTarget, new()
        {
            var value = arguments.Attribute.CommonConstructorArguments[1];
            if (value.IsNull)
            {
                return;
            }

            var sense = arguments.Attribute.CommonConstructorArguments[0].DecodeValue<bool>(SpecialType.System_Boolean);
            if (value.Kind != TypedConstantKind.Array)
            {
                var memberName = value.DecodeValue<string>(SpecialType.System_String);
                if (memberName is object)
                {
                    arguments.GetOrCreateData<T>().AddNotNullWhenMember(sense, memberName);
                    ReportBadNotNullMemberIfNeeded(type, arguments, memberName);
                }
            }
            else
            {
                var builder = ArrayBuilder<string>.GetInstance();
                foreach (var member in value.Values)
                {
                    var memberName = member.DecodeValue<string>(SpecialType.System_String);
                    if (memberName is object)
                    {
                        builder.Add(memberName);
                        ReportBadNotNullMemberIfNeeded(type, arguments, memberName);
                    }
                }

                arguments.GetOrCreateData<T>().AddNotNullWhenMember(sense, builder);
                builder.Free();
            }
        }

        private DeclarativeSecurityAction DecodeSecurityAttributeAction(Symbol targetSymbol, CSharpCompilation compilation, AttributeSyntax? nodeOpt, out bool hasErrors, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert((object)targetSymbol != null);
            Debug.Assert(targetSymbol.Kind == SymbolKind.Assembly || targetSymbol.Kind == SymbolKind.NamedType || targetSymbol.Kind == SymbolKind.Method);
            Debug.Assert(this.IsSecurityAttribute(compilation));

            var ctorArgs = this.CommonConstructorArguments;
            if (!ctorArgs.Any())
            {
                // NOTE:    Security custom attributes must have a valid SecurityAction as its first argument, we have none here.
                // NOTE:    Ideally, we should always generate 'CS7048: First argument to a security attribute must be a valid SecurityAction' for this case.
                // NOTE:    However, native compiler allows applying System.Security.Permissions.HostProtectionAttribute attribute without any argument and uses 
                // NOTE:    SecurityAction.LinkDemand as the default SecurityAction in this case. We maintain compatibility with the native compiler for this case.

                // BREAKING CHANGE: Even though the native compiler intends to allow only HostProtectionAttribute to be applied without any arguments,
                //                  it doesn't quite do this correctly 

                // The implementation issue leads to the native compiler allowing any user defined security attribute with a parameterless constructor and a named property argument as the first
                // attribute argument to have the above mentioned behavior, even though the comment clearly mentions that this behavior was intended only for the HostProtectionAttribute.
                // We currently allow this case only for the HostProtectionAttribute. In future if need arises, we can exactly match native compiler's behavior.

                if (this.IsTargetAttribute(targetSymbol, AttributeDescription.HostProtectionAttribute))
                {
                    hasErrors = false;
                    return DeclarativeSecurityAction.LinkDemand;
                }
            }
            else
            {
                TypedConstant firstArg = ctorArgs.First();
                var firstArgType = (TypeSymbol?)firstArg.TypeInternal;
                if (firstArgType is object && firstArgType.Equals(compilation.GetWellKnownType(WellKnownType.System_Security_Permissions_SecurityAction)))
                {
                    return DecodeSecurityAction(firstArg, targetSymbol, nodeOpt, diagnostics, out hasErrors);
                }
            }

            // CS7048: First argument to a security attribute must be a valid SecurityAction
            diagnostics.Add(ErrorCode.ERR_SecurityAttributeMissingAction, nodeOpt != null ? nodeOpt.Name.Location : NoLocation.Singleton);
            hasErrors = true;
            return DeclarativeSecurityAction.None;
        }

        private DeclarativeSecurityAction DecodeSecurityAction(TypedConstant typedValue, Symbol targetSymbol, AttributeSyntax? nodeOpt, BindingDiagnosticBag diagnostics, out bool hasErrors)
        {
            Debug.Assert((object)targetSymbol != null);
            Debug.Assert(targetSymbol.Kind == SymbolKind.Assembly || targetSymbol.Kind == SymbolKind.NamedType || targetSymbol.Kind == SymbolKind.Method);
            Debug.Assert(typedValue.ValueInternal is object);

            int securityAction = (int)typedValue.ValueInternal;
            bool isPermissionRequestAction;

            switch (securityAction)
            {
                case (int)DeclarativeSecurityAction.InheritanceDemand:
                case (int)DeclarativeSecurityAction.LinkDemand:
                    if (this.IsTargetAttribute(targetSymbol, AttributeDescription.PrincipalPermissionAttribute))
                    {
                        // CS7052: SecurityAction value '{0}' is invalid for PrincipalPermission attribute
                        object displayString;
                        Location syntaxLocation = GetSecurityAttributeActionSyntaxLocation(nodeOpt, typedValue, out displayString);
                        diagnostics.Add(ErrorCode.ERR_PrincipalPermissionInvalidAction, syntaxLocation, displayString);
                        hasErrors = true;
                        return DeclarativeSecurityAction.None;
                    }

                    isPermissionRequestAction = false;
                    break;

                case 1:
                // Native compiler allows security action value 1 for security attributes on types/methods, even though there is no corresponding field in System.Security.Permissions.SecurityAction enum.
                // We will maintain compatibility.

                case (int)DeclarativeSecurityAction.Assert:
                case (int)DeclarativeSecurityAction.Demand:
                case (int)DeclarativeSecurityAction.PermitOnly:
                case (int)DeclarativeSecurityAction.Deny:
                    isPermissionRequestAction = false;
                    break;

                case (int)DeclarativeSecurityAction.RequestMinimum:
                case (int)DeclarativeSecurityAction.RequestOptional:
                case (int)DeclarativeSecurityAction.RequestRefuse:
                    isPermissionRequestAction = true;
                    break;

                default:
                    {
                        // CS7049: Security attribute '{0}' has an invalid SecurityAction value '{1}'
                        object displayString;
                        Location syntaxLocation = GetSecurityAttributeActionSyntaxLocation(nodeOpt, typedValue, out displayString);
                        diagnostics.Add(ErrorCode.ERR_SecurityAttributeInvalidAction, syntaxLocation, nodeOpt != null ? nodeOpt.GetErrorDisplayName() : "", displayString);
                        hasErrors = true;
                        return DeclarativeSecurityAction.None;
                    }
            }

            // Validate security action for symbol kind
            if (isPermissionRequestAction)
            {
                if (targetSymbol.Kind == SymbolKind.NamedType || targetSymbol.Kind == SymbolKind.Method)
                {
                    // Types and methods cannot take permission requests.

                    // CS7051: SecurityAction value '{0}' is invalid for security attributes applied to a type or a method
                    object displayString;
                    Location syntaxLocation = GetSecurityAttributeActionSyntaxLocation(nodeOpt, typedValue, out displayString);
                    diagnostics.Add(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, syntaxLocation, displayString);
                    hasErrors = true;
                    return DeclarativeSecurityAction.None;
                }
            }
            else
            {
                if (targetSymbol.Kind == SymbolKind.Assembly)
                {
                    // Assemblies cannot take declarative security.

                    // CS7050: SecurityAction value '{0}' is invalid for security attributes applied to an assembly
                    object displayString;
                    Location syntaxLocation = GetSecurityAttributeActionSyntaxLocation(nodeOpt, typedValue, out displayString);
                    diagnostics.Add(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, syntaxLocation, displayString);
                    hasErrors = true;
                    return DeclarativeSecurityAction.None;
                }
            }

            hasErrors = false;
            return (DeclarativeSecurityAction)securityAction;
        }

        private static Location GetSecurityAttributeActionSyntaxLocation(AttributeSyntax? nodeOpt, TypedConstant typedValue, out object displayString)
        {
            if (nodeOpt == null)
            {
                displayString = "";
                return NoLocation.Singleton;
            }

            var argList = nodeOpt.ArgumentList;
            if (argList == null || argList.Arguments.IsEmpty())
            {
                // Optional SecurityAction parameter with default value.
                displayString = (FormattableString)$"{typedValue.ValueInternal}";
                return nodeOpt.Location;
            }

            AttributeArgumentSyntax argSyntax = argList.Arguments[0];
            displayString = argSyntax.ToString();
            return argSyntax.Location;
        }

        /// <summary>
        /// Decodes PermissionSetAttribute applied in source to determine if it needs any fixup during codegen.
        /// </summary>
        /// <remarks>
        /// PermissionSetAttribute needs fixup when it contains an assignment to the 'File' property as a single named attribute argument.
        /// Fixup performed is ported from SecurityAttributes::FixUpPermissionSetAttribute.
        /// It involves following steps:
        ///  1) Verifying that the specified file name resolves to a valid path.
        ///  2) Reading the contents of the file into a byte array.
        ///  3) Convert each byte in the file content into two bytes containing hexadecimal characters.
        ///  4) Replacing the 'File = fileName' named argument with 'Hex = hexFileContent' argument, where hexFileContent is the converted output from step 3) above.
        ///
        /// Step 1) is performed in this method, i.e. during binding.
        /// Remaining steps are performed during serialization as we want to avoid retaining the entire file contents throughout the binding/codegen pass.
        /// See <see cref="Microsoft.CodeAnalysis.CodeGen.PermissionSetAttributeWithFileReference"/> for remaining fixup steps.
        /// </remarks>
        /// <returns>String containing the resolved file path if PermissionSetAttribute needs fixup during codegen, null otherwise.</returns>
        private string? DecodePermissionSetAttribute(CSharpCompilation compilation, AttributeSyntax? nodeOpt, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!this.HasErrors);

            string? resolvedFilePath = null;
            var namedArgs = this.CommonNamedArguments;

            if (namedArgs.Length == 1)
            {
                var namedArg = namedArgs[0];
                Debug.Assert(AttributeClass is object);
                NamedTypeSymbol attrType = this.AttributeClass;
                string filePropName = PermissionSetAttributeWithFileReference.FilePropertyName;
                string hexPropName = PermissionSetAttributeWithFileReference.HexPropertyName;

                if (namedArg.Key == filePropName &&
                    PermissionSetAttributeTypeHasRequiredProperty(attrType, filePropName))
                {
                    // resolve file prop path
                    var fileName = (string?)namedArg.Value.ValueInternal;
                    var resolver = compilation.Options.XmlReferenceResolver;

                    resolvedFilePath = (resolver != null && fileName != null) ? resolver.ResolveReference(fileName, baseFilePath: null) : null;

                    if (resolvedFilePath == null)
                    {
                        // CS7053: Unable to resolve file path '{0}' specified for the named argument '{1}' for PermissionSet attribute
                        Location argSyntaxLocation = nodeOpt?.GetNamedArgumentSyntax(filePropName)?.Location ?? NoLocation.Singleton;
                        diagnostics.Add(ErrorCode.ERR_PermissionSetAttributeInvalidFile, argSyntaxLocation, fileName ?? "<null>", filePropName);
                    }
                    else if (!PermissionSetAttributeTypeHasRequiredProperty(attrType, hexPropName))
                    {
                        // PermissionSetAttribute was defined in user source, but doesn't have the required Hex property.
                        // Native compiler still emits the file content as named assignment to 'Hex' property, but this leads to a runtime exception.
                        // We instead skip the fixup and emit the file property.

                        // CONSIDER: We may want to consider taking a breaking change and generating an error here.

                        return null;
                    }
                }
            }

            return resolvedFilePath;
        }

        // This method checks if the given PermissionSetAttribute type has a property member with the given propName which is writable, non-generic, public and of string type.
        private static bool PermissionSetAttributeTypeHasRequiredProperty(NamedTypeSymbol permissionSetType, string propName)
        {
            var members = permissionSetType.GetMembers(propName);
            if (members.Length == 1 && members[0].Kind == SymbolKind.Property)
            {
                var property = (PropertySymbol)members[0];
                if (property.TypeWithAnnotations.HasType && property.Type.SpecialType == SpecialType.System_String &&
                    property.DeclaredAccessibility == Accessibility.Public && property.GetMemberArity() == 0 &&
                    (object)property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public)
                {
                    return true;
                }
            }

            return false;
        }

        internal void DecodeClassInterfaceAttribute(AttributeSyntax? nodeOpt, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!this.HasErrors);

            TypedConstant ctorArgument = this.CommonConstructorArguments[0];
            Debug.Assert(ctorArgument.Kind == TypedConstantKind.Enum || ctorArgument.Kind == TypedConstantKind.Primitive);

            ClassInterfaceType interfaceType = ctorArgument.Kind == TypedConstantKind.Enum ?
                ctorArgument.DecodeValue<ClassInterfaceType>(SpecialType.System_Enum) :
                (ClassInterfaceType)ctorArgument.DecodeValue<short>(SpecialType.System_Int16);

            switch (interfaceType)
            {
                case ClassInterfaceType.None:
                case Cci.Constants.ClassInterfaceType_AutoDispatch:
                case Cci.Constants.ClassInterfaceType_AutoDual:
                    break;

                default:
                    // CS0591: Invalid value for argument to '{0}' attribute
                    Location attributeArgumentSyntaxLocation = this.GetAttributeArgumentSyntaxLocation(0, nodeOpt);
                    diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, attributeArgumentSyntaxLocation, nodeOpt != null ? nodeOpt.GetErrorDisplayName() : "");
                    break;
            }
        }

        internal void DecodeInterfaceTypeAttribute(AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!this.HasErrors);

            TypedConstant ctorArgument = this.CommonConstructorArguments[0];
            Debug.Assert(ctorArgument.Kind == TypedConstantKind.Enum || ctorArgument.Kind == TypedConstantKind.Primitive);

            ComInterfaceType interfaceType = ctorArgument.Kind == TypedConstantKind.Enum ?
                ctorArgument.DecodeValue<ComInterfaceType>(SpecialType.System_Enum) :
                (ComInterfaceType)ctorArgument.DecodeValue<short>(SpecialType.System_Int16);

            switch (interfaceType)
            {
                case Cci.Constants.ComInterfaceType_InterfaceIsDual:
                case Cci.Constants.ComInterfaceType_InterfaceIsIDispatch:
                case ComInterfaceType.InterfaceIsIInspectable:
                case ComInterfaceType.InterfaceIsIUnknown:
                    break;

                default:
                    // CS0591: Invalid value for argument to '{0}' attribute
                    CSharpSyntaxNode attributeArgumentSyntax = this.GetAttributeArgumentSyntax(0, node);
                    diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, attributeArgumentSyntax.Location, node.GetErrorDisplayName());
                    break;
            }
        }

        internal string DecodeGuidAttribute(AttributeSyntax? nodeOpt, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!this.HasErrors);

            var guidString = (string?)this.CommonConstructorArguments[0].ValueInternal;

            // Native compiler allows only a specific GUID format: "D" format (32 digits separated by hyphens)
            Guid guid;
            if (!Guid.TryParseExact(guidString, "D", out guid))
            {
                // CS0591: Invalid value for argument to '{0}' attribute
                Location attributeArgumentSyntaxLocation = this.GetAttributeArgumentSyntaxLocation(0, nodeOpt);
                diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, attributeArgumentSyntaxLocation, nodeOpt != null ? nodeOpt.GetErrorDisplayName() : "");
                guidString = String.Empty;
            }

            return guidString!;
        }

        internal CollectionBuilderAttributeData DecodeCollectionBuilderAttribute()
        {
            var builderType = (TypeSymbol?)CommonConstructorArguments[0].ValueInternal;
            var methodName = (string?)CommonConstructorArguments[1].ValueInternal;
            return new CollectionBuilderAttributeData(builderType, methodName);
        }

        private protected sealed override bool IsStringProperty(string memberName)
        {
            if (AttributeClass is object)
            {
                foreach (var member in AttributeClass.GetMembers(memberName))
                {
                    if (member is PropertySymbol { Type: { SpecialType: SpecialType.System_String } })
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        /// <summary>
        /// This method determines if an applied attribute must be emitted.
        /// Some attributes appear in symbol model to reflect the source code,
        /// but should not be emitted.
        /// </summary>
        internal bool ShouldEmitAttribute(Symbol target, bool isReturnType, bool emittingAssemblyAttributesInNetModule)
        {
            Debug.Assert(target is SourceAssemblySymbol || target.ContainingAssembly is SourceAssemblySymbol);

            if (HasErrors)
            {
                throw ExceptionUtilities.Unreachable();
            }

            // Attribute type is conditionally omitted if both the following are true:
            //  (a) It has at least one applied/inherited conditional attribute AND
            //  (b) None of conditional symbols are defined in the source file where the given attribute was defined.
            if (this.IsConditionallyOmitted)
            {
                return false;
            }

            switch (target.Kind)
            {
                case SymbolKind.Assembly:
                    if ((!emittingAssemblyAttributesInNetModule &&
                            (IsTargetAttribute(target, AttributeDescription.AssemblyCultureAttribute) ||
                             IsTargetAttribute(target, AttributeDescription.AssemblyVersionAttribute) ||
                             IsTargetAttribute(target, AttributeDescription.AssemblyFlagsAttribute) ||
                             IsTargetAttribute(target, AttributeDescription.AssemblyAlgorithmIdAttribute))) ||
                        IsTargetAttribute(target, AttributeDescription.TypeForwardedToAttribute) ||
                        IsSecurityAttribute(target.DeclaringCompilation))
                    {
                        return false;
                    }

                    break;

                case SymbolKind.Event:
                    if (IsTargetAttribute(target, AttributeDescription.SpecialNameAttribute))
                    {
                        return false;
                    }

                    break;

                case SymbolKind.Field:
                    if (IsTargetAttribute(target, AttributeDescription.SpecialNameAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.NonSerializedAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.FieldOffsetAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.MarshalAsAttribute))
                    {
                        return false;
                    }

                    break;

                case SymbolKind.Method:
                    if (isReturnType)
                    {
                        if (IsTargetAttribute(target, AttributeDescription.MarshalAsAttribute))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (IsTargetAttribute(target, AttributeDescription.SpecialNameAttribute) ||
                            IsTargetAttribute(target, AttributeDescription.MethodImplAttribute) ||
                            IsTargetAttribute(target, AttributeDescription.DllImportAttribute) ||
                            IsTargetAttribute(target, AttributeDescription.PreserveSigAttribute) ||
                            IsTargetAttribute(target, AttributeDescription.DynamicSecurityMethodAttribute) ||
                            IsSecurityAttribute(target.DeclaringCompilation))
                        {
                            return false;
                        }
                    }

                    break;

                case SymbolKind.NetModule:
                    // Note that DefaultCharSetAttribute is emitted to metadata, although it's also decoded and used when emitting P/Invoke
                    break;

                case SymbolKind.NamedType:
                    if (IsTargetAttribute(target, AttributeDescription.SpecialNameAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.ComImportAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.SerializableAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.StructLayoutAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.WindowsRuntimeImportAttribute) ||
                        IsSecurityAttribute(target.DeclaringCompilation))
                    {
                        return false;
                    }

                    break;

                case SymbolKind.Parameter:
                    if (IsTargetAttribute(target, AttributeDescription.OptionalAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.DefaultParameterValueAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.InAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.OutAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.MarshalAsAttribute))
                    {
                        return false;
                    }

                    break;

                case SymbolKind.Property:
                    if (IsTargetAttribute(target, AttributeDescription.IndexerNameAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.SpecialNameAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.DisallowNullAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.AllowNullAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.MaybeNullAttribute) ||
                        IsTargetAttribute(target, AttributeDescription.NotNullAttribute))
                    {
                        return false;
                    }

                    break;
            }

            return true;
        }

    }

    internal static class AttributeDataExtensions
    {
        internal static int IndexOfAttribute(this ImmutableArray<CSharpAttributeData> attributes, Symbol targetSymbol, AttributeDescription description)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].IsTargetAttribute(targetSymbol, description))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static CSharpSyntaxNode GetAttributeArgumentSyntax(this AttributeData attribute, int parameterIndex, AttributeSyntax attributeSyntax)
        {
            Debug.Assert(attribute is SourceAttributeData);
            return ((SourceAttributeData)attribute).GetAttributeArgumentSyntax(parameterIndex, attributeSyntax);
        }

        internal static string? DecodeNotNullIfNotNullAttribute(this CSharpAttributeData attribute)
        {
            var arguments = attribute.CommonConstructorArguments;
            return arguments.Length == 1 && arguments[0].TryDecodeValue(SpecialType.System_String, out string? value) ? value : null;
        }

        internal static Location GetAttributeArgumentSyntaxLocation(this AttributeData attribute, int parameterIndex, AttributeSyntax? attributeSyntaxOpt)
        {
            if (attributeSyntaxOpt == null)
            {
                return NoLocation.Singleton;
            }

            Debug.Assert(attribute is SourceAttributeData);
            return ((SourceAttributeData)attribute).GetAttributeArgumentSyntax(parameterIndex, attributeSyntaxOpt).Location;
        }
    }
}
