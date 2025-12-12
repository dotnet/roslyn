// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BuildValidator
{
    internal static class Util
    {
        internal static PortableExecutableInfo? GetPortableExecutableInfo(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var peReader = new PEReader(stream);
            if (GetMvid(peReader) is { } mvid)
            {
                var isReadyToRun = IsReadyToRunImage(peReader);
                var isRefAssembly = IsReferenceAssembly(peReader);
                return new PortableExecutableInfo(filePath, mvid, isReadyToRun, isRefAssembly);
            }

            return null;
        }

        internal static Guid? GetMvid(PEReader peReader)
        {
            if (peReader.HasMetadata)
            {
                var metadataReader = peReader.GetMetadataReader();
                var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
                return metadataReader.GetGuid(mvidHandle);
            }
            else
            {
                return null;
            }
        }

        internal static bool IsReferenceAssembly(this PEReader peReader)
        {
            var reader = peReader.GetMetadataReader();
            foreach (var attributeHandle in reader.GetCustomAttributes(Handle.AssemblyDefinition))
            {
                var attribute = reader.GetCustomAttribute(attributeHandle);
                if (GetAttributeFullName(reader, attribute) == "System.Runtime.CompilerServices.ReferenceAssemblyAttribute")
                {
                    return true;
                }
            }

            return false;
        }

        internal static string GetAttributeFullName(MetadataReader reader, CustomAttribute attribute)
        {
            switch (attribute.Constructor.Kind)
            {
                case HandleKind.MethodDefinition:
                    {
                        var methodDef = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                        var declaringTypeHandle = methodDef.GetDeclaringType();
                        var typeDefinition = reader.GetTypeDefinition(declaringTypeHandle);
                        var @namespace = reader.GetString(typeDefinition.Namespace);
                        var name = reader.GetString(typeDefinition.Name);
                        return $"{@namespace}.{name}";
                    }
                case HandleKind.MemberReference:
                    {
                        var memberRef = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                        Debug.Assert(memberRef.Parent.Kind == HandleKind.TypeReference);
                        var typeRef = reader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);
                        var @namespace = reader.GetString(typeRef.Namespace);
                        var name = reader.GetString(typeRef.Name);
                        return $"{@namespace}.{name}";
                    }
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static bool IsReadyToRunImage(PEReader peReader)
        {
            if (peReader.PEHeaders is null ||
                peReader.PEHeaders.PEHeader is null ||
                peReader.PEHeaders.CorHeader is null)
            {
                return false;
            }

            if ((peReader.PEHeaders.CorHeader.Flags & CorFlags.ILLibrary) == 0)
            {
                PEExportTable exportTable = peReader.GetExportTable();
                return exportTable.TryGetValue("RTR_HEADER", out _);
            }
            else
            {
                return peReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size != 0;
            }
        }
    }
}
