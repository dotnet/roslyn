// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A <see cref="MissingMetadataTypeSymbol"/> is a special kind of <see cref="ErrorTypeSymbol"/> that represents
    /// a type symbol that was attempted to be read from metadata, but couldn't be
    /// found, because:
    ///   a) The metadata file it lives in wasn't referenced
    ///   b) The metadata file was referenced, but didn't contain the type
    ///   c) The metadata file was referenced, contained the correct outer type, but
    ///      didn't contains a nested type in that outer type.
    /// </summary>
    internal abstract class MissingMetadataTypeSymbol : ErrorTypeSymbol
    {
        protected readonly string name;
        protected readonly int arity;
        protected readonly bool mangleName;

        private MissingMetadataTypeSymbol(string name, int arity, bool mangleName)
        {
            Debug.Assert(name != null);

            this.name = name;
            this.arity = arity;
            this.mangleName = (mangleName && arity > 0);
        }

        public override string Name
        {
            get { return name; }
        }

        internal override bool MangleName
        {
            get
            {
                return mangleName;
            }
        }
        /// <summary>
        /// Get the arity of the missing type.
        /// </summary>
        public override int Arity
        {
            get { return arity; }
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                AssemblySymbol containingAssembly = this.ContainingAssembly;

                // The Dev10 C# compiler produces errors based on what it was trying to do when 
                // the type could not be found. For example, if it could not resolve a base class
                // then it would report:
                //
                // error CS1714: The base class or interface of 'C' could not be resolved or is invalid
                //
                // Since we do not know what task was being performed, for now we just report a generic
                // "you must add a reference" error.

                if (containingAssembly.IsMissing)
                {
                    // error CS0012: The type 'Blah' is defined in an assembly that is not referenced. You must add a reference to assembly 'Goo'.
                    return new CSDiagnosticInfo(ErrorCode.ERR_NoTypeDef, this, containingAssembly.Identity);
                }
                else
                {
                    ModuleSymbol containingModule = this.ContainingModule;

                    if (containingModule.IsMissing)
                    {
                        // It looks like required module wasn't added to the compilation.
                        return new CSDiagnosticInfo(ErrorCode.ERR_NoTypeDefFromModule, this, containingModule.Name);
                    }

                    // Both the containing assembly and the module were resolved, but the type isn't.
                    //
                    // These are warnings in the native compiler, but they seem to always
                    // be accompanied by an error. It seems strange to make these warnings; something is
                    // seriously wrong in the program and it is unlikely that we'll be able to correctly
                    // generate metadata.

                    // NOTE: this is another case where we would like to base our decision on which compilation
                    // is the "current" compilation, but we don't want to force consumers of the API to specify.
                    if (containingAssembly.Dangerous_IsFromSomeCompilation)
                    {
                        // This scenario is quite tricky and involves a circular reference. Suppose we have
                        // assembly Alpha that has a type C. Assembly Beta refers to Alpha and uses type C.
                        // Now we create a new source assembly that replaces Alpha, and refers to Beta.
                        // The usage of C in Beta will be redirected to refer to the source assembly.
                        // If C is not in that source assembly then we give the following warning:

                        // CS7068: Reference to type 'C' claims it is defined in this assembly, but it is not defined in source or any added modules 
                        return new CSDiagnosticInfo(ErrorCode.ERR_MissingTypeInSource, this);
                    }
                    else
                    {
                        // The more straightforward scenario is that we compiled Beta against a version of Alpha
                        // that had C, and then added a reference to a different version of Alpha that
                        // lacks the type C:

                        // error CS7069: Reference to type 'C' claims it is defined in 'Alpha', but it could not be found
                        return new CSDiagnosticInfo(ErrorCode.ERR_MissingTypeInAssembly, this, containingAssembly.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Represents not nested missing type.
        /// </summary>
        internal class TopLevel : MissingMetadataTypeSymbol
        {
            private readonly string _namespaceName;
            private readonly ModuleSymbol _containingModule;
            private NamespaceSymbol _lazyContainingNamespace;

            /// <summary>
            /// Either <see cref="SpecialType"/>, <see cref="WellKnownType"/>, or -1 if not initialized.
            /// </summary>
            private int _lazyTypeId = -1;

            public TopLevel(ModuleSymbol module, string @namespace, string name, int arity, bool mangleName)
                : base(name, arity, mangleName)
            {
                Debug.Assert((object)module != null);
                Debug.Assert(@namespace != null);

                _namespaceName = @namespace;
                _containingModule = module;
            }

            public TopLevel(ModuleSymbol module, ref MetadataTypeName fullName)
                : this(module, ref fullName, -1)
            {
            }

            public TopLevel(ModuleSymbol module, ref MetadataTypeName fullName, SpecialType specialType)
                : this(module, ref fullName, (int)specialType)
            {
            }

            public TopLevel(ModuleSymbol module, ref MetadataTypeName fullName, WellKnownType wellKnownType)
                : this(module, ref fullName, (int)wellKnownType)
            {
            }

            private TopLevel(ModuleSymbol module, ref MetadataTypeName fullName, int typeId)
                : this(module, ref fullName, fullName.ForcedArity == -1 || fullName.ForcedArity == fullName.InferredArity)
            {
                Debug.Assert(typeId == -1 || typeId == (int)SpecialType.None || Arity == 0 || MangleName);
                _lazyTypeId = typeId;
            }

            private TopLevel(ModuleSymbol module, ref MetadataTypeName fullName, bool mangleName)
                : this(module, fullName.NamespaceName,
                       mangleName ? fullName.UnmangledTypeName : fullName.TypeName,
                       mangleName ? fullName.InferredArity : fullName.ForcedArity,
                       mangleName)
            {
            }

            /// <summary>
            /// This is the FULL namespace name (e.g., "System.Collections.Generic")
            /// of the type that couldn't be found.
            /// </summary>
            public string NamespaceName
            {
                get { return _namespaceName; }
            }

            internal override ModuleSymbol ContainingModule
            {
                get
                {
                    return _containingModule;
                }
            }

            public override AssemblySymbol ContainingAssembly
            {
                get
                {
                    return _containingModule.ContainingAssembly;
                }
            }

            public override Symbol ContainingSymbol
            {
                get
                {
                    if ((object)_lazyContainingNamespace == null)
                    {
                        NamespaceSymbol container = _containingModule.GlobalNamespace;

                        if (_namespaceName.Length > 0)
                        {
                            var namespaces = MetadataHelpers.SplitQualifiedName(_namespaceName);
                            int i;

                            for (i = 0; i < namespaces.Length; i++)
                            {
                                NamespaceSymbol newContainer = null;

                                foreach (NamespaceOrTypeSymbol symbol in container.GetMembers(namespaces[i]))
                                {
                                    if (symbol.Kind == SymbolKind.Namespace) // VB should also check name casing.
                                    {
                                        newContainer = (NamespaceSymbol)symbol;
                                        break;
                                    }
                                }

                                if ((object)newContainer == null)
                                {
                                    break;
                                }

                                container = newContainer;
                            }

                            // now create symbols we couldn't find.
                            for (; i < namespaces.Length; i++)
                            {
                                container = new MissingNamespaceSymbol(container, namespaces[i]);
                            }
                        }

                        Interlocked.CompareExchange(ref _lazyContainingNamespace, container, null);
                    }

                    return _lazyContainingNamespace;
                }
            }

            private int TypeId
            {
                get
                {
                    if (_lazyTypeId == -1)
                    {
                        SpecialType typeId = SpecialType.None;

                        AssemblySymbol containingAssembly = _containingModule.ContainingAssembly;

                        if ((Arity == 0 || MangleName) && (object)containingAssembly != null && ReferenceEquals(containingAssembly, containingAssembly.CorLibrary) && _containingModule.Ordinal == 0)
                        {
                            // Check the name 
                            string emittedName = MetadataHelpers.BuildQualifiedName(_namespaceName, MetadataName);
                            typeId = SpecialTypes.GetTypeFromMetadataName(emittedName);
                        }

                        Interlocked.CompareExchange(ref _lazyTypeId, (int)typeId, -1);
                    }

                    return _lazyTypeId;
                }
            }

            public override SpecialType SpecialType
            {
                get
                {
                    int typeId = TypeId;
                    return (typeId >= (int)WellKnownType.First) ? SpecialType.None : (SpecialType)_lazyTypeId;
                }
            }

            internal override DiagnosticInfo ErrorInfo
            {
                get
                {
                    if (this.TypeId != (int)SpecialType.None)
                    {
                        return new CSDiagnosticInfo(ErrorCode.ERR_PredefinedTypeNotFound, MetadataHelpers.BuildQualifiedName(_namespaceName, MetadataName));
                    }

                    return base.ErrorInfo;
                }
            }

            public override int GetHashCode()
            {
                // Inherit special behavior for the object type from NamedTypeSymbol.
                if (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                {
                    return (int)Microsoft.CodeAnalysis.SpecialType.System_Object;
                }

                return Hash.Combine(MetadataName, Hash.Combine(_containingModule, Hash.Combine(_namespaceName, arity)));
            }

            internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
            {
                if (ReferenceEquals(this, t2))
                {
                    return true;
                }

                // if ignoring dynamic, then treat dynamic the same as the type 'object'
                if ((comparison & TypeCompareKind.IgnoreDynamic) != 0 &&
                    (object)t2 != null &&
                    t2.TypeKind == TypeKind.Dynamic &&
                    this.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                {
                    return true;
                }

                var other = t2 as TopLevel;

                return (object)other != null &&
                    string.Equals(MetadataName, other.MetadataName, StringComparison.Ordinal) &&
                    arity == other.arity &&
                    string.Equals(_namespaceName, other.NamespaceName, StringComparison.Ordinal) &&
                    _containingModule.Equals(other._containingModule);
            }
        }

        internal class TopLevelWithCustomErrorInfo : TopLevel
        {
            private readonly DiagnosticInfo _errorInfo;

            public TopLevelWithCustomErrorInfo(ModuleSymbol module, ref MetadataTypeName emittedName, DiagnosticInfo errorInfo)
                : base(module, ref emittedName)
            {
                Debug.Assert(errorInfo != null);
                _errorInfo = errorInfo;
            }

            public TopLevelWithCustomErrorInfo(ModuleSymbol module, ref MetadataTypeName emittedName, DiagnosticInfo errorInfo, SpecialType typeId)
                : base(module, ref emittedName, typeId)
            {
                Debug.Assert(errorInfo != null);
                _errorInfo = errorInfo;
            }

            public TopLevelWithCustomErrorInfo(ModuleSymbol module, ref MetadataTypeName emittedName, DiagnosticInfo errorInfo, WellKnownType typeId)
                : base(module, ref emittedName, typeId)
            {
                Debug.Assert(errorInfo != null);
                _errorInfo = errorInfo;
            }

            internal override DiagnosticInfo ErrorInfo
            {
                get
                {
                    return _errorInfo;
                }
            }
        }

        /// <summary>
        /// Represents nested missing type.
        /// </summary>
        internal class Nested : MissingMetadataTypeSymbol
        {
            private readonly NamedTypeSymbol _containingType;

            public Nested(NamedTypeSymbol containingType, string name, int arity, bool mangleName)
                : base(name, arity, mangleName)
            {
                Debug.Assert((object)containingType != null);

                _containingType = containingType;
            }

            public Nested(NamedTypeSymbol containingType, ref MetadataTypeName emittedName)
                : this(containingType, ref emittedName, emittedName.ForcedArity == -1 || emittedName.ForcedArity == emittedName.InferredArity)
            {
            }

            private Nested(NamedTypeSymbol containingType, ref MetadataTypeName emittedName, bool mangleName)
                : this(containingType,
                       mangleName ? emittedName.UnmangledTypeName : emittedName.TypeName,
                       mangleName ? emittedName.InferredArity : emittedName.ForcedArity,
                       mangleName)
            {
            }

            public override Symbol ContainingSymbol
            {
                get
                {
                    return _containingType;
                }
            }


            public override SpecialType SpecialType
            {
                get
                {
                    return SpecialType.None; // do not have nested types among CORE types yet.
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_containingType, Hash.Combine(MetadataName, arity));
            }

            internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
            {
                if (ReferenceEquals(this, t2))
                {
                    return true;
                }

                var other = t2 as Nested;
                return (object)other != null && string.Equals(MetadataName, other.MetadataName, StringComparison.Ordinal) &&
                    arity == other.arity &&
                    _containingType.Equals(other._containingType, comparison, isValueTypeOverrideOpt);
            }
        }
    }
}
