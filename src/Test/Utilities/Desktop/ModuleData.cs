// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ModuleExtension
    {
        public static readonly string EXE = ".exe";
        public static readonly string DLL = ".dll";
        public static readonly string NETMODULE = ".netmodule";
    }

    [Serializable, DebuggerDisplay("{GetDebuggerDisplay()}")]
    public sealed class ModuleData : ISerializable
    {
        // Simple assembly name  ("foo") or module name ("bar.netmodule").
        public readonly string FullName;

        public readonly OutputKind Kind;
        public readonly ImmutableArray<byte> Image;
        public readonly ImmutableArray<byte> Pdb;
        public readonly bool InMemoryModule;
        private Guid? _mvid;

        public ModuleData(string netModuleName, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule)
        {
            this.FullName = netModuleName;
            this.Kind = OutputKind.NetModule;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
        }

        public ModuleData(AssemblyIdentity identity, OutputKind kind, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule)
        {
            this.FullName = identity.GetDisplayName();
            this.Kind = kind;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
        }

        public Guid Mvid
        {
            get
            {
                if (_mvid == null)
                {
                    using (var metadata = ModuleMetadata.CreateFromImage(Image))
                    {
                        _mvid = metadata.GetModuleVersionId();
                    }
                }

                return _mvid.Value;
            }
        }

        private string GetDebuggerDisplay()
        {
            return FullName + " {" + Mvid + "}";
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //public readonly string FullName;
            info.AddValue("FullName", this.FullName);

            //public readonly OutputKind Kind;
            info.AddValue("kind", (int)this.Kind);

            //public readonly ImmutableArray<byte> Image;
            info.AddByteArray("Image", this.Image);

            //public readonly ImmutableArray<byte> PDB;
            info.AddByteArray("PDB", this.Pdb);

            //public readonly bool InMemoryModule;
            info.AddValue("InMemoryModule", this.InMemoryModule);

            //private Guid? mvid;
            info.AddValue("mvid", _mvid, typeof(Guid?));
        }

        private ModuleData(SerializationInfo info, StreamingContext context)
        {
            //public readonly string FullName;
            this.FullName = info.GetString("FullName");

            //public readonly OutputKind Kind;
            this.Kind = (OutputKind)info.GetInt32("kind");

            //public readonly ImmutableArray<byte> Image;
            this.Image = info.GetByteArray("Image");

            //public readonly ImmutableArray<byte> PDB;
            this.Pdb = info.GetByteArray("PDB");

            //public readonly bool InMemoryModule;
            this.InMemoryModule = info.GetBoolean("InMemoryModule");

            //private Guid? mvid;
            _mvid = (Guid?)info.GetValue("mvid", typeof(Guid?));
        }
    }
}
