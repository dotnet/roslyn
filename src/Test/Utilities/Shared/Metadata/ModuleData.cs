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
    public struct ModuleDataId
    {
        public string SimpleName { get; }
        public string FullName { get; }
        public Guid Mvid { get; }

        public ModuleDataId(Assembly assembly)
        {
            SimpleName = assembly.GetName().Name;
            FullName = assembly.FullName;
            // Replace with mvid
            Mvid = default(Guid);
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

        public string SimpleName => Id.SimpleName;
        public string FullName => Id.FullName;
        public Guid Mvid => Id.Mvid;

        public ModuleData(string netModuleName, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule)
        {
            this.Id = new ModuleDataId(netModuleName, netModuleName, GetMvid(image));
            this.Kind = OutputKind.NetModule;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
        }

        public ModuleData(AssemblyIdentity identity, OutputKind kind, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule)
        {
            this.Id = new ModuleDataId(identity.Name, identity.GetDisplayName(), GetMvid(image));
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
    }
}
