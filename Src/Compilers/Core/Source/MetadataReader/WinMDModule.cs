using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.MetadataReader.UtilityDataStructures;
using WinMdAssembliesOffsets = Microsoft.CodeAnalysis.MetadataReader.WinRTProjectedTypes.WinMdAssemblyOffsets;
using Microsoft.CodeAnalysis.MetadataReader.PESignatures;

namespace Microsoft.CodeAnalysis.MetadataReader
{
    internal sealed class WinMDModule : PEModule
    {
        private readonly WinMDScenario scenario;

        private const string clrPrefix = "<CLR>";
        private const string winRtPrefix = "<WinRT>";

        /// <summary>
        /// WinMD files require a number of additional references to be added
        /// alongside the original. This lists the number of additional references.
        /// </summary>
        public const uint WinMdExtraRefs = 5;

        private readonly uint winMdStartIndex;

        private readonly byte[] OperatorPrefixUtf8 = new byte[] { (byte)'o', (byte)'p', (byte)'_' };
        
        private static readonly string[] operatorNames = new[] {
            // !!WARNING!! THIS LIST IS SORTED FOR FAST LOOKUP
            "Addition",
            "AdditionAssignment",
            "AddressOf",
            "Assign",
            "BitwiseAnd",
            "BitwiseAndAssignment",
            "BitwiseOr",
            "BitwiseOrAssignment",
            "Comma",
            "Decrement",
            "Division",
            "DivisionAssignment",
            "Equality",
            "ExclusiveOr",
            "ExclusiveOrAssignment",
            "Explicit",
            "False",
            "GreaterThan",
            "GreaterThanOrEqual",
            "Implicit",
            "Increment",
            "Inequality",
            "LeftShift",
            "LeftShiftAssignment",
            "LessThan",
            "LessThanOrEqual",
            "LogicalAnd",
            "LogicalNot",
            "LogicalOr",
            "MemberSelection",
            "Modulus",
            "ModulusAssignment",
            "MultiplicationAssignment",
            "Multiply",
            "OnesComplement",
            "PointerDereference",
            "PointerToMemberSelection",
            "RightShift",
            "RightShiftAssignment",
            "SignedRightShift",
            "Subtraction",
            "SubtractionAssignment",
            "True",
            "UnaryNegation",
            "UnaryPlus",
            "UnsignedRightShift",
            "UnsignedRightShiftAssignment" };

        public WinMDModule(
            ModuleMetadata owner,
            AbstractMemoryBlock memoryBlock,
            PEFileReader peFileReader,
            ReadOnlyArray<AssemblyIdentity> referencedAssemblies,
            WinMDScenario scenario)
            : base(owner, memoryBlock, peFileReader, referencedAssemblies)
        {
            this.scenario = scenario;
            this.winMdStartIndex = (uint)referencedAssemblies.Count - WinMdExtraRefs;
        }

        public static void AddWinMdAssemblies(AssemblyIdentity[] identities, int offset)
        {
            Contract.Assert(offset + WinMdExtraRefs <= identities.Length);

            // There are five different assemblies that need to be added to the
            // table if we reference Windows.winmd. These provide forwarded type
            // definitions and such.
            // 
            // They are (in order):
            // System.Runtime.WindowsRuntime
            // System.Runtime
            // System.ObjectModel
            // System.Runtime.WindowsRuntime.UI.Xaml
            // System.Runtime.InteropServices.WindowsRuntime
            // 
            // We load them here.

            var key = new byte[] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 }.AsReadOnlyWrap();
            var key1 = new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }.AsReadOnlyWrap();

            //System.Runtime.Windowsruntime
            var sysrunwin = new AssemblyIdentity("System.Runtime.WindowsRuntime", cultureName: "neutral",
               hasPublicKey: false, version: new Version("4.0.0.0"), publicKeyOrToken: key);

            identities[WinMdAssembliesOffsets.SystemRuntimeWindowsRuntime + offset] = sysrunwin;

            //System.Runtime
            var sysrun = new AssemblyIdentity("System.Runtime", cultureName: "neutral",
               hasPublicKey: false, version: new Version("4.0.0.0"), publicKeyOrToken: key1);

            identities[WinMdAssembliesOffsets.SystemRuntime + offset] = sysrun;

            //System.ObjectModel
            var objectmodel = new AssemblyIdentity("System.ObjectModel", cultureName: "neutral",
               hasPublicKey: false, version: new Version("4.0.0.0"), publicKeyOrToken: key1);

            identities[WinMdAssembliesOffsets.SystemObject + offset] = objectmodel;

            //System.Runtime.WindowsRuntime.UI.Xaml
            var uixaml = new AssemblyIdentity("System.Runtime.WindowsRuntime.UI.Xaml", cultureName: "neutral",
                hasPublicKey: false, version: new Version("4.0.0.0"), publicKeyOrToken: key);

            identities[WinMdAssembliesOffsets.SystemRuntimeWindowsUiXaml + offset] = uixaml;

            //System.Runtime.InteropServices.WindowsRuntime
            var interop = new AssemblyIdentity("System.Runtime.InteropServices.WindowsRuntime", cultureName: "neutral",
                hasPublicKey: false, version: new Version("4.0.0.0"), publicKeyOrToken: key1);

            identities[WinMdAssembliesOffsets.SystemRuntimeInterop + offset] = interop;
        }

        public override bool IsWinMd
        {
            get { return true; }
        }

        private TypeDefTreatment GetTypeDefTreatment(
            TypeHandle typeDef,
            TypeAttributes flags,
            string name,
            string namespaceName,
            MetadataToken extends)
        {
            TypeDefTreatment treatment;

            // Does the type def have the WindowsRuntime bit set?
            if (flags.IsWindowsRuntime())
            {
                if (scenario == WinMDScenario.NormalWinMD)
                {
                    treatment = WinRTProjectedTypes.GetTypeDefinitionTreatment(name, namespaceName);
                    if (treatment != TypeDefTreatment.None)
                    {
                        return treatment;
                    }

                    // Is this an attribute?
                    if (extends.HandleType == HandleType.TypeReference && GetTypeRefTreatment((TypeReferenceHandle)extends) == TypeRefTreatment.SystemAttribute)
                    {
                        treatment = TypeDefTreatment.NormalAttribute;
                    }
                    else
                    {
                        treatment = TypeDefTreatment.NormalNonAttribute;
                    }
                }
                else if (scenario == WinMDScenario.WinMDExp && !flags.IsNested() && flags.IsPublic() && flags.IsSpecialName())
                {
                    treatment = TypeDefTreatment.PrefixWinRTName;
                }
                else
                {
                    treatment = TypeDefTreatment.None;
                }

                // Scan through Custom Attributes on type, looking for interesting bits. We only
                // need to do this for RuntimeClasses
                if (treatment == TypeDefTreatment.PrefixWinRTName || treatment == TypeDefTreatment.NormalNonAttribute)
                {
                    if (!flags.IsInterface() && HasAttribute(typeDef, "Windows.UI.Xaml", "TreatAsAbstractComposableClassAttribute"))
                    {
                        treatment |= TypeDefTreatment.MarkAbstractFlag;
                    }
                }
            }
            else if (this.scenario == WinMDScenario.WinMDExp && IsClrImplementationType(name, flags))
            {
                treatment = TypeDefTreatment.UnmangleWinRTName;
            }
            else
            {
                treatment = TypeDefTreatment.None;
            }

            return treatment;
        }

        // TODO (tomat): disable the warning temporarily until we move this code to the metadata reader
#pragma warning disable 618
        private bool HasAttribute(MetadataToken token, string namespaceName, string typeName)
        {
            foreach (var caHandle in peFileReader.GetCustomAttributes(token))
            {
                StringHandle namespaceHandle, nameHandle;
                if (peFileReader.GetCustomAttribute(caHandle).GetFullTypeName(out namespaceHandle, out nameHandle) &&
                    peFileReader.StringStream.CheckForText(namespaceHandle, namespaceName) &&
                    peFileReader.StringStream.CheckForText(nameHandle, typeName))
                {
                    return true;
                }
            }

            return false;
        }
#pragma warning restore 618

        private static bool IsClrImplementationType(string name, TypeAttributes attr)
        {
            if ((attr & (TypeAttributesMissing.NestedMask | TypeAttributes.Interface | TypeAttributes.Public | TypeAttributes.SpecialName)) != 
                TypeAttributes.SpecialName)
            {
                return false;
            }

            return name.StartsWith(clrPrefix, StringComparison.Ordinal);
        }

        public override void GetTypeRefProps(
            TypeReferenceHandle typeRef,
            out string name,
            out string @namespace,
            out MetadataToken resolutionScope)
        {
            base.GetTypeRefProps(typeRef, out name, out @namespace, out resolutionScope);

// TODO (tomat): disable the warning temporarily until we move this code to the metadata reader
#pragma warning disable 618 // obsolete warning reported when RowIds are used
            uint assemblyRefOffset;
            string clrName, clrNamespace;
            if (WinRTProjectedTypes.ResolveWinRTTypeReference(name, @namespace, out clrName, out clrNamespace, out assemblyRefOffset))
            {
                name = clrName;
                @namespace = clrNamespace;
                uint assemblyRefRid = this.winMdStartIndex + assemblyRefOffset + 1;
                resolutionScope = AssemblyReferenceHandle.FromRowId(assemblyRefRid);
            }
            else if (GetTypeRefTreatment(typeRef) != TypeRefTreatment.None)
            {
                uint assemblyRefRid = this.winMdStartIndex + (uint)WinMdAssembliesOffsets.SystemRuntime + 1;
                resolutionScope = AssemblyReferenceHandle.FromRowId(assemblyRefRid);
            }
#pragma warning restore 618 // obsolete warning reported when RowIds are used
        }

        public override string GetTypeDefName(TypeHandle typeDef)
        {
            string name, @namespace;
            TypeAttributes flags;
            MetadataToken extends;
            GetTypeDefProps(
                typeDef,
                out name,
                out @namespace,
                out flags,
                out extends);
            return name;
        }

        public override TypeAttributes GetTypeDefFlags(TypeHandle typeDef)
        {
            string name, @namespace;
            TypeAttributes flags;
            MetadataToken extends;
            GetTypeDefProps(
                typeDef,
                out name,
                out @namespace,
                out flags,
                out extends);
            return flags;
        }

        public override void GetTypeDefProps(
            TypeHandle typeDef,
            out string name,
            out string namespaceName,
            out TypeAttributes flags,
            out MetadataToken extends)
        {
            base.GetTypeDefProps(typeDef,
                                 out name,
                                 out namespaceName,
                                 out flags,
                                 out extends);

            TypeDefTreatment treatment =
                GetTypeDefTreatment(typeDef, flags, name, namespaceName, extends);

            switch (treatment & TypeDefTreatment.TreatmentMask)
            {
                case TypeDefTreatment.None:
                    break;

                case TypeDefTreatment.NormalNonAttribute:
                    flags |= TypeAttributes.WindowsRuntime | TypeAttributes.Import;
                    break;

                case TypeDefTreatment.NormalAttribute:
                    flags |= TypeAttributes.WindowsRuntime | TypeAttributes.Sealed;
                    break;

                case TypeDefTreatment.UnmangleWinRTName:
                    name = name.Substring(clrPrefix.Length);
                    flags |= TypeAttributes.Public;
                    break;

                case TypeDefTreatment.PrefixWinRTName:
                    name = winRtPrefix + name;
                    flags &= TypeAttributes.Public;
                    flags |= TypeAttributes.Import;
                    break;

                case TypeDefTreatment.RedirectedToCLRType:
                    flags &= ~TypeAttributes.Public;
                    flags |= TypeAttributes.Import;
                    break;

                case TypeDefTreatment.RedirectedToCLRAttribute:
                    flags &= ~TypeAttributes.Public;
                    break;
            }

            if (treatment.HasFlag(TypeDefTreatment.MarkAbstractFlag))
            {
                flags |= TypeAttributes.Abstract;
            }

            if (treatment.HasFlag(TypeDefTreatment.MarkInternalFlag))
            {
                flags &= ~TypeAttributes.Public;
            }
        }

        private TypeRefTreatment GetTypeRefTreatment(TypeReferenceHandle typeRef)
        {
            TypeReference reference = peFileReader.GetTypeReference(typeRef);
            if (peFileReader.StringStream.CheckForText(reference.Namespace, "System"))
            {
                if (peFileReader.StringStream.CheckForText(reference.Name, "MulticastDelegate"))
                {
                    return TypeRefTreatment.SystemDelegate;
                }
                
                if (peFileReader.StringStream.CheckForText(reference.Name, "Attribute"))
                {
                    return TypeRefTreatment.SystemAttribute;
                }
            }

            return TypeRefTreatment.None;
        }

        private MethodDefTreatment ComputeMethodDefTreatment(MethodHandle methodDef)
        {
            MethodDefTreatment treatment = MethodDefTreatment.Implementation;

            TypeDefinition parentType = peFileReader.GetTypeDefinition(peFileReader.FindMethodContainer(methodDef));
            TypeAttributes parentFlags = parentType.Flags;

            if (parentFlags.IsWindowsRuntime())
            {
                if (IsClrImplementationType(peFileReader.StringStream[parentType.Name], parentFlags))
                {
                    treatment = MethodDefTreatment.Implementation;
                }
                else if (parentFlags.IsNested())
                {
                    treatment = MethodDefTreatment.Implementation;
                }
                else if (parentFlags.IsInterface())
                {
                    treatment = MethodDefTreatment.Interface;
                }
                else if (scenario == WinMDScenario.WinMDExp &&
                         !parentFlags.IsPublic())
                {
                    treatment = MethodDefTreatment.Implementation;
                }
                else
                {
                    treatment = MethodDefTreatment.Other;

                    var parentBaseType = parentType.BaseType;
                    if (HandleType.TypeReference == parentBaseType.HandleType)
                    {
                        switch (GetTypeRefTreatment((TypeReferenceHandle)parentBaseType))
                        {
                            case TypeRefTreatment.SystemAttribute:
                                treatment = MethodDefTreatment.Attribute;
                                break;

                            case TypeRefTreatment.SystemDelegate:
                                treatment = MethodDefTreatment.Delegate;
                                break;
                        }
                    }
                }
            }

            if (treatment == MethodDefTreatment.Other)
            {
                // we want to hide the method if it implements
                // only redirected interfaces
                // We also want to check if the methodImpl is IClosable.Close,
                // so we can change the name
                bool seenRedirectedInterfaces = false;
                bool seenNonRedirectedInterfaces = false;

                bool isIClosableClose = false;

                foreach (var methodImplHandle in parentType.GetMethodImplementations())
                {
                    MethodImpl methodImpl = peFileReader.GetMethodImplementation(methodImplHandle);
                    if (methodImpl.MethodBody == methodDef)
                    {
                        MetadataToken declaration = methodImpl.MethodDeclaration;

                        // See if this MethodImpl implements a redirected interface
                        // In WinMD, MethodImpl will always use MemberRef and TypeRefs to refer to redirected interfaces,
                        // even if they are in the same module.
                        if (declaration.HandleType == HandleType.MemberReference && 
                            ImplementsRedirectedInterface((MemberReferenceHandle)declaration, out isIClosableClose))
                        {
                            seenRedirectedInterfaces = true;
                            if (isIClosableClose)
                            {
                                // This method implements IClosable.Close
                                // Let's rename to IDisposable later
                                // Once we know this implements IClosable.Close, we are done
                                // looking
                                break;
                            }
                        }
                        else
                        {
                            // Now we know this implements a non-redirected interface
                            // But we need to keep looking, just in case we got a methodimpl that
                            // implements the IClosable.Close method and needs to be renamed
                            seenNonRedirectedInterfaces = true;
                        }
                    }
                }

                if (isIClosableClose)
                {
                    treatment = MethodDefTreatment.RenameToDisposeMethod;
                }
                else if (seenRedirectedInterfaces && !seenNonRedirectedInterfaces)
                {
                    // Only hide if all the interfaces implemented are redirected
                    treatment = MethodDefTreatment.HiddenImpl;
                }
            }

            // If treatment is other, then this is a non-managed WinRT runtime class definition
            // Find out about various bits that we apply via attributes and name parsing
            if (treatment == MethodDefTreatment.Other)
            {
                treatment |= GetMethodTreatmentFromCustomAttributes(methodDef);

                Method method = peFileReader.GetMethod(methodDef);
                if (method.Flags != MethodAttributes.SpecialName)
                {
                    StringHandle methodName = method.Name;
                    if (peFileReader.StringStream.StartsWith(methodName, OperatorPrefixUtf8))
                    {
                        // TODO (tomat): consider avoiding name allocation by encoding the operator names in UTF8
                        string nameAfterOp = peFileReader.StringStream.GetSuffix(methodName, OperatorPrefixUtf8.Length);
                        if (Array.BinarySearch<string>(operatorNames, nameAfterOp) < 0)
                        {
                            treatment |= MethodDefTreatment.MarkSpecialName;
                        }
                    }
                }
            }

            return treatment;
        }

#pragma warning disable 618 // obsolete warning reported when RowIds are used
        private MethodDefTreatment GetMethodTreatmentFromCustomAttributes(MethodHandle methodDef)
        {
            MethodDefTreatment treatment = 0;

            foreach (var caHandle in peFileReader.GetCustomAttributes(methodDef))
            {
                CustomAttribute ca = peFileReader.GetCustomAttribute(caHandle);
                StringHandle namespaceHandle, nameHandle;
                if (!ca.GetFullTypeName(out namespaceHandle, out nameHandle))
                {
                    continue;
                }

                if (peFileReader.StringStream.CheckForText(namespaceHandle, "Windows.UI.Xaml"))
                {
                    if (peFileReader.StringStream.CheckForText(nameHandle, "TreatAsPublicMethodAttribute"))
                    {
                        treatment |= MethodDefTreatment.MarkPublicFlag;
                    }

                    if (peFileReader.StringStream.CheckForText(nameHandle, "TreatAsAbstractMethodAttribute"))
                    {
                        treatment |= MethodDefTreatment.MarkAbstractFlag;
                    }
                }
            }

            return treatment;
        }
#pragma warning restore 618 // obsolete warning reported when RowIds are used

        public override MethodAttributes GetMethodDefFlags(MethodHandle methodDef)
        {
            string name;
            MethodImplAttributes implFlags;
            MethodAttributes flags;
            int rva;

            GetMethodDefProps(methodDef, out name, out implFlags, out flags, out rva);

            return flags;
        }

        public override void GetMethodDefProps(
            MethodHandle methodDef,
            out string name,
            out MethodImplAttributes implFlags,
            out MethodAttributes flags,
            out int rva)
        {
            base.GetMethodDefProps(methodDef, out name, out implFlags, out flags, out rva);
            ModifyMethodProps(methodDef, ref name, ref implFlags, ref flags, ref rva);
        }

        private void ModifyMethodProps(
            MethodHandle methodDef,
            ref string name,
            ref MethodImplAttributes implFlags,
            ref MethodAttributes flags,
            ref int rva)
        {
            MethodDefTreatment treatment =
                ComputeMethodDefTreatment(methodDef);

            switch (treatment & MethodDefTreatment.TreatmentMask)
            {
                case MethodDefTreatment.Interface:
                    // Method is declared on an interface
                    implFlags |= MethodImplAttributes.Runtime |
                                 MethodImplAttributes.InternalCall;
                    break;

                case MethodDefTreatment.Delegate:
                    // Method is declared on a delegate
                    flags &= ~MethodAttributes.MemberAccessMask;
                    flags |= MethodAttributes.Public;
                    rva = 0;
                    implFlags |= MethodImplAttributes.Runtime;
                    break;

                case MethodDefTreatment.Attribute:
                    // Method is declared on an attribute
                    rva = 0;
                    implFlags |= MethodImplAttributes.Runtime |
                                 MethodImplAttributes.InternalCall;
                    break;

                case MethodDefTreatment.Implementation:
                    // CLR implemenation class. Needs no adjustment
                    break;

                case MethodDefTreatment.HiddenImpl:
                    // Implements a hidden WinMD interface
                    flags &= ~MethodAttributes.MemberAccessMask;
                    flags |= MethodAttributes.Private;
                    goto case MethodDefTreatment.Other;

                case MethodDefTreatment.Other:
                    // All other cases
                    rva = 0;
                    implFlags |= MethodImplAttributes.Runtime |
                                 MethodImplAttributes.InternalCall;

                    if (treatment.HasFlag(MethodDefTreatment.MarkAbstractFlag))
                    {
                        flags |= MethodAttributes.Abstract;
                    }

                    if (treatment.HasFlag(MethodDefTreatment.MarkPublicFlag))
                    {
                        flags &= ~MethodAttributes.MemberAccessMask;
                        flags |= MethodAttributes.Public;
                    }

                    if (treatment.HasFlag(MethodDefTreatment.MarkSpecialName))
                    {
                        flags |= MethodAttributes.SpecialName;
                    }

                    break;

                case MethodDefTreatment.RenameToDisposeMethod:
                    rva = 0;
                    implFlags |= MethodImplAttributes.Runtime |
                                 MethodImplAttributes.InternalCall;
                    name = "Dispose";
                    break;

            }

            flags |= MethodAttributes.HideBySig;

            // Make WinRT delegate constructors public
            if (flags.HasFlag(MethodAttributes.RTSpecialName) &&
                flags.HasFlag(MethodAttributes.SpecialName) &&
                MethodDefTreatment.Delegate == treatment &&
                ".ctor" == name)
            {
                flags = flags & ~(MethodAttributes.Private);
                flags |= MethodAttributes.Public;
            }
        }

        public override FieldAttributes GetFieldDefFlags(FieldHandle fieldDef)
        {
            string unused;
            FieldAttributes flags;
            GetFieldDefProps(fieldDef, out unused, out flags);
            return flags;
        }

        public override string GetFieldDefName(FieldHandle fieldDef)
        {
            string name;
            FieldAttributes unused;
            GetFieldDefProps(fieldDef, out name, out unused);
            return name;
        }

        public override void GetFieldDefProps(FieldHandle fieldDef, out string name, out FieldAttributes flags)
        {
            base.GetFieldDefProps(fieldDef, out name, out flags);
            ModifyFieldDefProps(fieldDef, name, ref flags);
        }

        /// <summary>
        /// The backing field of a WinRT enumeration type is not public although the backing fields
        /// of managed enumerations are. To allow managed languages to directly access this field,
        /// it is made public by the metadata adapter.
        /// </summary>
        private void ModifyFieldDefProps(FieldHandle fieldDef, string name, ref FieldAttributes flags)
        {
            if (name == "value__" && flags.HasFlag(FieldAttributes.RTSpecialName))
            {
                TypeHandle typeDef = FindContainingType(fieldDef);
                Debug.Assert(!typeDef.IsNil);

                MetadataToken extendsRef = GetTypeDefExtends(typeDef);
                if (extendsRef.HandleType == HandleType.TypeReference)
                {
                    string extendsName, extendsNamespace;
                    MetadataToken unused;
                    GetTypeRefProps(
                        (TypeReferenceHandle)extendsRef,
                        out extendsName,
                        out extendsNamespace,
                        out unused);
                    if (extendsName == "Enum" && extendsNamespace == "System")
                    {
                        flags = flags & ~FieldAttributes.Private;
                        flags |= FieldAttributes.Public;
                    }
                }
            }
        }

        public override string GetMemberRefName(MemberReferenceHandle memberRef)
        {
            string name;
            // We need to rename the MemberRef for IClosable.Close as well
            // so that the MethodImpl for the Dispose method can be correctly shown
            // as IDisposable.Dispose instead of IDisposable.Close
            bool isIDisposable;
            if (ImplementsRedirectedInterface(memberRef, out isIDisposable) && isIDisposable)
            {
                name = "Dispose";
            }
            else
            {
                name = base.GetMemberRefName(memberRef);
            }

            return name;
        }

        /// <summary>
        /// We want to know if a given method implements a redirected interface.
        /// For example, if we are given the method RemoveAt on a class "A" 
        /// which implements the IVector interface (which is redirected
        /// to IList in .NET) then this method would return true. The most 
        /// likely reason why we would want to know this is that we wish to hide
        /// (mark private) all methods which implement methods on a redirected 
        /// interface.
        /// </summary>
        /// <param name="memberRef">The declaration token for the method</param>
        /// <param name="isIDisposable">
        /// Returns true if the redirected interface is <see cref="IDisposable"/>.
        /// </param>
        /// <returns>True if the method implements a method on a redirected interface.
        /// False otherwise.</returns>
        private bool ImplementsRedirectedInterface(MemberReferenceHandle memberRef, out bool isIDisposable)
        {
            isIDisposable = false;

            MetadataToken parent = peFileReader.GetMemberReference((MemberReferenceHandle)memberRef).Parent;

            TypeReferenceHandle typeRef;
            if (parent.HandleType == HandleType.TypeReference)
            {
                typeRef = (TypeReferenceHandle)parent;
            }
            else if (parent.HandleType == HandleType.TypeSpecification)
            {
                BlobHandle blob = peFileReader.GetTypeSpecification((TypeSpecificationHandle)parent);
                MemoryBlock sigBlock = peFileReader.BlobStream.GetMemoryBlockAt(blob);

                if (sigBlock.Length < 2)
                    return false;

                MemoryReader sig = new MemoryReader(sigBlock);

                if (sig.ReadByte() != (byte)CorElementType.ELEMENT_TYPE_GENERICINST ||
                    sig.ReadByte() != (byte)CorElementType.ELEMENT_TYPE_CLASS)
                {
                    return false;
                }

                MetadataToken token = SignatureHelpers.DecodeToken(ref sig);

                if (token.HandleType != HandleType.TypeReference)
                    return false;

                typeRef = (TypeReferenceHandle)token;
            }
            else
            {
                return false;
            }

            TypeReference reference = peFileReader.GetTypeReference(typeRef);
            if (reference.Namespace.IsNil)
            {
                return false;
            }

            string name = peFileReader.StringStream[reference.Name];
            string @namespace = peFileReader.StringStream[reference.Namespace];
            return WinRTProjectedTypes.IsWinRTTypeReference(name, @namespace, out isIDisposable);
        }

        public enum WinMDScenario
        {
            NormalWinMD,    // File is a normal .winmd file
            WinMDExp,       // File is the output of winmdexp
        }
    }
}
