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

    [Serializable]
    public sealed class ModuleDataId
    {
        // Simple assembly name  ("foo") or module name ("bar.netmodule").
        public string FullName { get; }
        public Guid Mvid { get; }

        public ModuleDataId(string fullName, Guid mvid)
        {
            FullName = fullName;
            Mvid = mvid;
        }

        public override string ToString()
        {
            return $"{FullName} - {Mvid}";
        }
    }

    [Serializable, DebuggerDisplay("{GetDebuggerDisplay()}")]
    public sealed class ModuleData : ISerializable
    {
        public readonly ModuleDataId Id;

        public readonly OutputKind Kind;
        public readonly ImmutableArray<byte> Image;
        public readonly ImmutableArray<byte> Pdb;
        public readonly bool InMemoryModule;

        public string FullName => Id.FullName;
        public Guid Mvid => Id.Mvid;

        public ModuleData(string netModuleName, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule)
        {
            this.Id = new ModuleDataId(netModuleName, GetMvid(image));
            this.Kind = OutputKind.NetModule;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
        }

        public ModuleData(AssemblyIdentity identity, OutputKind kind, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule)
        {
            this.Id = new ModuleDataId(identity.GetDisplayName(), GetMvid(image));
            this.Kind = kind;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
        }

        private static Guid GetMvid(ImmutableArray<byte> image)
        {
            using (var metadata = ModuleMetadata.CreateFromImage(image))
            {
                return metadata.GetModuleVersionId();
            }
        }

        private string GetDebuggerDisplay()
        {
            return FullName + " {" + Mvid + "}";
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), this.Id);
            info.AddValue(nameof(Kind), (int)this.Kind);
            info.AddByteArray(nameof(Image), this.Image);
            info.AddByteArray(nameof(Pdb), this.Pdb);
            info.AddValue(nameof(InMemoryModule), this.InMemoryModule);
        }

        private ModuleData(SerializationInfo info, StreamingContext context)
        {
            this.Id = (ModuleDataId)info.GetValue(nameof(Id), typeof(ModuleDataId));
            this.Kind = (OutputKind)info.GetInt32(nameof(Kind));
            this.Image = info.GetByteArray(nameof(Image));
            this.Pdb = info.GetByteArray(nameof(Pdb));
            this.InMemoryModule = info.GetBoolean(nameof(InMemoryModule));
        }
    }
}
