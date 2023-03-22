// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    public readonly struct ModuleDataId
    {
        public string SimpleName { get; }
        public string FullName { get; }
        public Guid Mvid { get; }

        public ModuleDataId(Assembly assembly, Guid mvid)
        {
            SimpleName = assembly.GetName().Name;
            FullName = assembly.FullName;
            Mvid = mvid;
        }

        public ModuleDataId(string simpleName, string fullName, Guid mvid)
        {
            SimpleName = simpleName;
            FullName = fullName;
            Mvid = mvid;
        }

        public override string ToString()
        {
            return $"{FullName} - {Mvid}";
        }
    }

    [DebuggerDisplay("{GetDebuggerDisplay()}")]
    public sealed class ModuleData
    {
        public readonly ModuleDataId Id;

        public readonly OutputKind Kind;
        public readonly ImmutableArray<byte> Image;
        public readonly ImmutableArray<byte> Pdb;
        public readonly bool InMemoryModule;
        public readonly bool IsCorLib;

        public string SimpleName => Id.SimpleName;
        public string FullName => Id.FullName;
        public Guid Mvid => Id.Mvid;

        public ModuleData(string netModuleName, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule, bool isCorLib)
        {
            this.Id = new ModuleDataId(netModuleName, netModuleName, GetMvid(image));
            this.Kind = OutputKind.NetModule;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
            this.IsCorLib = isCorLib;
        }

        public ModuleData(AssemblyIdentity identity, OutputKind kind, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule, bool isCorLib)
        {
            this.Id = new ModuleDataId(identity.Name, identity.GetDisplayName(), GetMvid(image));
            this.Kind = kind;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
            this.IsCorLib = isCorLib;
        }

        public ModuleData(ModuleDataId id, OutputKind kind, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule, bool isCorLib)
        {
            this.Id = id;
            this.Kind = kind;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
            this.IsCorLib = isCorLib;
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
    }
}
