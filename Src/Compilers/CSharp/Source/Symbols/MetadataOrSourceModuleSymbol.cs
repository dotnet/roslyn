using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.Internal;
using Roslyn.Compilers.Internal.MetadataReader;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Represents source or metadata module.
    /// </summary>
    /// <remarks></remarks>
    internal abstract class MetadataOrSourceModuleSymbol
        : ModuleSymbol 
    {
        /// <summary>
        /// The system assembly, which provides primitive types like Object, String, etc., think mscorlib.dll. 
        /// The value is provided by AssemblyManager and must not be modified. The AssemblySymbol must match
        /// one of the referenced assemblies returned by GetReferencedAssemblySymbols() method or the owning
        /// assembly. If none of the candidate assemblies can be used as a source for the primitive types, 
        /// the value is a null reference. 
        /// </summary>
        private AssemblySymbol coreLibrary;

        /// <summary>
        /// The system assembly, which provides primitive types like Object, String, etc., think mscorlib.dll. 
        /// The value is a null reference if none of the referenced assemblies can be used as a source for the 
        /// primitive types and the owning assembly cannot be used as the source too. Otherwise, it is one of 
        /// the referenced assemblies returned by GetReferencedAssemblySymbols() method or the owning assembly.
        /// </summary>
        internal AssemblySymbol CorLibrary
        {
            get
            {
                System.Diagnostics.Debug.Assert(referencedAssemblies != null &&
                                                referencedAssemblySymbols != null &&
                                                referencedAssemblies.Length ==
                                                referencedAssemblySymbols.Length);
                return coreLibrary;
            }
        }

        /// <summary>
        /// A helper method for AssemblyManager to set assembly identities for assemblies 
        /// referenced by this module and corresponding AssemblySymbols.
        /// </summary>
        /// <param name="names"></param>
        /// <param name="symbols"></param>
        internal override void SetReferences(
            System.Reflection.AssemblyName[] names,
            AssemblySymbol[] symbols)
        {
            base.SetReferences(names, symbols);

            System.Diagnostics.Debug.Assert(coreLibrary == null);
            coreLibrary = null;
        }

        /// <summary>
        /// A helper method for AssemblyManager to set the system assembly, which provides primitive 
        /// types like Object, String, etc., think mscorlib.dll. 
        /// </summary>
        /// <param name="corLibrary"></param>
        internal void SetCorLibrary(
            AssemblySymbol corLibrary)
        {
            Contract.ThrowIfNull(referencedAssemblies);
            Contract.ThrowIfNull(referencedAssemblySymbols);

            if (corLibrary != null && !ReferenceEquals(corLibrary, this.ContainingAssembly))
            {
                bool corLibraryIsOk = false;

                foreach (var asm in referencedAssemblySymbols)
                {
                    if (object.ReferenceEquals(asm, corLibrary))
                    {
                        corLibraryIsOk = true;
                        break;
                    }
                }

                Contract.ThrowIfFalse(corLibraryIsOk);
            }

            System.Diagnostics.Debug.Assert(coreLibrary == null);
            coreLibrary = corLibrary;
        }

        /// <summary>
        /// Get symbol for predefined type from Cor Library referenced by this module.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal NamedTypeSymbol GetCorLibType(CorLibTypes.TypeId type)
        {
            var mscorlibAssembly = CorLibrary;

            if (mscorlibAssembly == null)
            {
                int arity = 0;
                var actualName = Utilities.GetActualTypeNameFromEmittedTypeName(type.GetEmittedName(), -1, out arity);
                return new MissingMetadataTypeSymbol(new System.Reflection.AssemblyName("mscorlib"),
                                                      actualName, arity);
            }
            else
            {
                return mscorlibAssembly.GetDeclaredCorType(type);
            }
        }

        /// <summary>
        /// Return type's type code in context of this module.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal Microsoft.Cci.PrimitiveTypeCode GetTypeCodeOfType(NamedTypeSymbol type)
        {
            var mscorlibAssembly = CorLibrary;

            if (mscorlibAssembly == null)
            {
                return Microsoft.Cci.PrimitiveTypeCode.NotPrimitive;
            }
            else
            {
                return mscorlibAssembly.GetTypeCodeOfDeclaredType(type);
            }
        }

        /// <summary>
        /// Return type's type code in context of this module.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal Microsoft.Cci.PrimitiveTypeCode GetTypeCodeOfType(TypeSymbol type)
        {
            var namedType = type as NamedTypeSymbol;

            if (namedType == null)
            {
                return Microsoft.Cci.PrimitiveTypeCode.NotPrimitive;
            }
            else
            {
                return GetTypeCodeOfType(namedType);
            }
        }


        /// <summary>
        /// Return type's Cor TypeId in context of this module.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal CorLibTypes.TypeId GetCorTypeIdOfType(NamedTypeSymbol type)
        {
            var mscorlibAssembly = CorLibrary;

            if (mscorlibAssembly == null)
            {
                return CorLibTypes.TypeId.None;
            }
            else
            {
                return mscorlibAssembly.GetCorTypeIdOfDeclaredType(type);
            }
        }

        /// <summary>
        /// Return type's Cor TypeId in context of this module.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal CorLibTypes.TypeId GetCorTypeIdOfType(TypeSymbol type)
        {
            var namedType = type as NamedTypeSymbol;

            if (namedType == null)
            {
                return CorLibTypes.TypeId.None;
            }
            else
            {
                return GetCorTypeIdOfType(namedType);
            }
        }
    }
}
