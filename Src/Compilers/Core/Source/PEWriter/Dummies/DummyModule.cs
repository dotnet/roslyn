using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyModule : IModule
    {
        #region IModule Members

        public string ModuleName
        {
            get
            {
                return Dummy.Name;
            }
        }

        public IAssembly/*?*/ ContainingAssembly
        {
            get
            {
                return null;
            }
        }

        IAssemblyReference/*?*/ IModuleReference.ContainingAssembly
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<IAssemblyReference> AssemblyReferences
        {
            get { return IteratorHelper.GetEmptyEnumerable<IAssemblyReference>(); }
        }

        public ulong BaseAddress
        {
            get { return 0; }
        }

        public ushort DllCharacteristics
        {
            get { return 0; }
        }

        public IMethodReference EntryPoint
        {
            get { return Dummy.MethodReference; }
        }

        public uint FileAlignment
        {
            get { return 0; }
        }

        public bool ILOnly
        {
            get { return false; }
        }

        public ModuleKind Kind
        {
            get { return ModuleKind.ConsoleApplication; }
        }

        public byte LinkerMajorVersion
        {
            get { return 0; }
        }

        public byte LinkerMinorVersion
        {
            get { return 0; }
        }

        public byte MetadataFormatMajorVersion
        {
            get { return 0; }
        }

        public byte MetadataFormatMinorVersion
        {
            get { return 0; }
        }

        public IEnumerable<ICustomAttribute> ModuleAttributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public IEnumerable<IModuleReference> ModuleReferences
        {
            get { return IteratorHelper.GetEmptyEnumerable<IModuleReference>(); }
        }

        public Guid PersistentIdentifier
        {
            get { return Guid.Empty; }
        }

        public bool RequiresAmdInstructionSet
        {
            get { return false; }
        }

        public bool Requires32bits
        {
            get { return false; }
        }

        public bool Requires64bits
        {
            get { return false; }
        }

        public ulong SizeOfHeapReserve
        {
            get { return 0; }
        }

        public ulong SizeOfHeapCommit
        {
            get { return 0; }
        }

        public ulong SizeOfStackReserve
        {
            get { return 0; }
        }

        public ulong SizeOfStackCommit
        {
            get { return 0; }
        }

        public string TargetRuntimeVersion
        {
            get { return string.Empty; }
        }

        public bool TrackDebugData
        {
            get { return false; }
        }

        public bool UsePublicKeyTokensForAssemblyReferences
        {
            get { return false; }
        }

        public IEnumerable<IWin32Resource> Win32Resources
        {
            get { return IteratorHelper.GetEmptyEnumerable<IWin32Resource>(); }
        }

        public IEnumerable<string> GetStrings()
        {
            return IteratorHelper.GetEmptyEnumerable<string>();
        }

        public IEnumerable<INamedTypeDefinition> GetAllTypes()
        {
            return IteratorHelper.GetEmptyEnumerable<INamedTypeDefinition>();
        }

        #endregion

        #region IUnit Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion

        #region IDoubleDispatcher Members

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        #endregion

        #region IUnitReference Members

        #endregion

        #region IReference Members

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        #endregion

        public IAssembly AsAssembly
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IAssemblyReference> IModule.AssemblyReferences
        {
            get { throw new NotImplementedException(); }
        }

        ulong IModule.BaseAddress
        {
            get { throw new NotImplementedException(); }
        }

        IAssembly IModule.ContainingAssembly
        {
            get { throw new NotImplementedException(); }
        }

        ushort IModule.DllCharacteristics
        {
            get { throw new NotImplementedException(); }
        }

        IMethodReference IModule.EntryPoint
        {
            get { throw new NotImplementedException(); }
        }

        uint IModule.FileAlignment
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<string> IModule.GetStrings()
        {
            throw new NotImplementedException();
        }

        IEnumerable<INamedTypeDefinition> IModule.GetAllTypes()
        {
            throw new NotImplementedException();
        }

        bool IModule.ILOnly
        {
            get { throw new NotImplementedException(); }
        }

        ModuleKind IModule.Kind
        {
            get { throw new NotImplementedException(); }
        }

        byte IModule.LinkerMajorVersion
        {
            get { throw new NotImplementedException(); }
        }

        byte IModule.LinkerMinorVersion
        {
            get { throw new NotImplementedException(); }
        }

        byte IModule.MetadataFormatMajorVersion
        {
            get { throw new NotImplementedException(); }
        }

        byte IModule.MetadataFormatMinorVersion
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ICustomAttribute> IModule.ModuleAttributes
        {
            get { throw new NotImplementedException(); }
        }

        string IModule.ModuleName
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IModuleReference> IModule.ModuleReferences
        {
            get { throw new NotImplementedException(); }
        }

        Guid IModule.PersistentIdentifier
        {
            get { throw new NotImplementedException(); }
        }

        bool IModule.RequiresAmdInstructionSet
        {
            get { throw new NotImplementedException(); }
        }

        bool IModule.Requires32bits
        {
            get { throw new NotImplementedException(); }
        }

        bool IModule.Requires64bits
        {
            get { throw new NotImplementedException(); }
        }

        ulong IModule.SizeOfHeapCommit
        {
            get { throw new NotImplementedException(); }
        }

        ulong IModule.SizeOfHeapReserve
        {
            get { throw new NotImplementedException(); }
        }

        ulong IModule.SizeOfStackCommit
        {
            get { throw new NotImplementedException(); }
        }

        ulong IModule.SizeOfStackReserve
        {
            get { throw new NotImplementedException(); }
        }

        string IModule.TargetRuntimeVersion
        {
            get { throw new NotImplementedException(); }
        }

        bool IModule.TrackDebugData
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IWin32Resource> IModule.Win32Resources
        {
            get { throw new NotImplementedException(); }
        }

        IAssembly IModule.AsAssembly
        {
            get { throw new NotImplementedException(); }
        }

        ITypeReference IModule.PlatformType(PlatformType t)
        {
            throw new NotImplementedException();
        }

        IEnumerable<ICustomAttribute> IReference.Attributes
        {
            get { throw new NotImplementedException(); }
        }

        void IReference.Dispatch(IMetadataVisitor visitor)
        {
            throw new NotImplementedException();
        }

        IEnumerable<ILocation> IObjectWithLocations.Locations
        {
            get { throw new NotImplementedException(); }
        }

        string INamedEntity.Name
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IReference> IModule.ReferencesInIL(out int count)
        {
            throw new NotImplementedException();
        }
    }
}