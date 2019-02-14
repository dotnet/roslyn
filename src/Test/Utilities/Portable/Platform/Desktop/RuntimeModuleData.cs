// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#if NET472

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

namespace Roslyn.Test.Utilities.Desktop
{
    [Serializable]
    public struct RuntimeModuleDataId : ISerializable
    {
        public ModuleDataId Id { get; }

        public RuntimeModuleDataId(ModuleDataId id)
        {
            Id = id;
        }

        private RuntimeModuleDataId(SerializationInfo info, StreamingContext context)
        {
            var simpleName = info.GetString(nameof(Id.SimpleName));
            var fullName = info.GetString(nameof(Id.FullName));
            var mvid = (Guid)info.GetValue(nameof(Id.Mvid), typeof(Guid));
            Id = new ModuleDataId(simpleName, fullName, mvid);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id.SimpleName), Id.SimpleName);
            info.AddValue(nameof(Id.FullName), Id.FullName);
            info.AddValue(nameof(Id.Mvid), Id.Mvid);
        }
    }

    [Serializable, DebuggerDisplay("{GetDebuggerDisplay()}")]
    public sealed class RuntimeModuleData : ISerializable
    {
        public ModuleData Data { get; }

        public RuntimeModuleData(ModuleData data)
        {
            Data = data;
        }

        private RuntimeModuleData(SerializationInfo info, StreamingContext context)
        {
            var id = (RuntimeModuleDataId)info.GetValue(nameof(ModuleData.Id), typeof(RuntimeModuleDataId));
            var kind = (OutputKind)info.GetInt32(nameof(ModuleData.Kind));
            var image = info.GetByteArray(nameof(ModuleData.Image));
            var pdb = info.GetByteArray(nameof(ModuleData.Pdb));
            var inMemoryModule = info.GetBoolean(nameof(ModuleData.InMemoryModule));
            Data = new ModuleData(id.Id, kind, image, pdb, inMemoryModule);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ModuleData.Id), new RuntimeModuleDataId(Data.Id));
            info.AddValue(nameof(ModuleData.Kind), (int)Data.Kind);
            info.AddByteArray(nameof(ModuleData.Image), Data.Image);
            info.AddByteArray(nameof(ModuleData.Pdb), Data.Pdb);
            info.AddValue(nameof(ModuleData.InMemoryModule), Data.InMemoryModule);
        }
    }
}

#endif
