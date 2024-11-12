// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Debugging;

namespace Microsoft.CodeAnalysis.PdbSourceDocument;

internal static class SymbolSourceDocumentFinder
{
    public static HashSet<DocumentHandle> FindDocumentHandles(EntityHandle handle, MetadataReader dllReader, MetadataReader pdbReader)
    {
        var docList = new HashSet<DocumentHandle>();

        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
                ProcessMethodDef((MethodDefinitionHandle)handle, dllReader, pdbReader, docList, processDeclaringType: true);
                break;
            case HandleKind.TypeDefinition:
                ProcessTypeDef((TypeDefinitionHandle)handle, dllReader, pdbReader, docList);
                break;
            case HandleKind.FieldDefinition:
                ProcessFieldDef((FieldDefinitionHandle)handle, dllReader, pdbReader, docList);
                break;
            case HandleKind.PropertyDefinition:
                ProcessPropertyDef((PropertyDefinitionHandle)handle, dllReader, pdbReader, docList);
                break;
            case HandleKind.EventDefinition:
                ProcessEventDef((EventDefinitionHandle)handle, dllReader, pdbReader, docList);
                break;
        }

        return docList;
    }

    private static void ProcessMethodDef(MethodDefinitionHandle methodDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList, bool processDeclaringType)
    {
        var mdi = pdbReader.GetMethodDebugInformation(methodDefHandle);
        if (!mdi.Document.IsNil)
        {
            docList.Add(mdi.Document);
            return;
        }

        if (!mdi.SequencePointsBlob.IsNil)
        {
            foreach (var point in mdi.GetSequencePoints())
            {
                if (!point.Document.IsNil)
                {
                    docList.Add(point.Document);
                    // No need to check the type if we found a document
                    processDeclaringType = false;
                }
            }
        }

        // Not all methods have document info, for example synthesized constructors, so we also want
        // to get any documents from the declaring type
        if (processDeclaringType)
        {
            var methodDef = dllReader.GetMethodDefinition(methodDefHandle);
            var typeDefHandle = methodDef.GetDeclaringType();
            ProcessTypeDef(typeDefHandle, dllReader, pdbReader, docList);
        }
    }

    private static void ProcessEventDef(EventDefinitionHandle eventDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList)
    {
        var eventDef = dllReader.GetEventDefinition(eventDefHandle);
        var accessors = eventDef.GetAccessors();
        if (!accessors.Adder.IsNil)
        {
            ProcessMethodDef(accessors.Adder, dllReader, pdbReader, docList, processDeclaringType: true);
        }

        if (!accessors.Remover.IsNil)
        {
            ProcessMethodDef(accessors.Remover, dllReader, pdbReader, docList, processDeclaringType: true);
        }

        if (!accessors.Raiser.IsNil)
        {
            ProcessMethodDef(accessors.Raiser, dllReader, pdbReader, docList, processDeclaringType: true);
        }

        foreach (var other in accessors.Others)
        {
            ProcessMethodDef(other, dllReader, pdbReader, docList, processDeclaringType: true);
        }
    }

    private static void ProcessPropertyDef(PropertyDefinitionHandle propertyDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList)
    {
        var propertyDef = dllReader.GetPropertyDefinition(propertyDefHandle);
        var accessors = propertyDef.GetAccessors();
        if (!accessors.Getter.IsNil)
        {
            ProcessMethodDef(accessors.Getter, dllReader, pdbReader, docList, processDeclaringType: true);
        }

        if (!accessors.Setter.IsNil)
        {
            ProcessMethodDef(accessors.Setter, dllReader, pdbReader, docList, processDeclaringType: true);
        }

        foreach (var other in accessors.Others)
        {
            ProcessMethodDef(other, dllReader, pdbReader, docList, processDeclaringType: true);
        }
    }

    private static void ProcessFieldDef(FieldDefinitionHandle fieldDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList)
    {
        var fieldDef = dllReader.GetFieldDefinition(fieldDefHandle);
        var typeDefHandle = fieldDef.GetDeclaringType();
        ProcessTypeDef(typeDefHandle, dllReader, pdbReader, docList);
    }

    private static void ProcessTypeDef(TypeDefinitionHandle typeDefHandle, MetadataReader dllReader, MetadataReader pdbReader, HashSet<DocumentHandle> docList, bool processContainingType = true)
    {
        AddDocumentsFromTypeDefinitionDocuments(typeDefHandle, pdbReader, docList);

        // We don't necessarily have all of the documents associated with the type
        var typeDef = dllReader.GetTypeDefinition(typeDefHandle);
        foreach (var methodDefHandle in typeDef.GetMethods())
        {
            ProcessMethodDef(methodDefHandle, dllReader, pdbReader, docList, processDeclaringType: false);
        }

        if (processContainingType && typeDef.IsNested)
        {
            // If this is a nested type, then we want to check the outer type too
            var containingType = typeDef.GetDeclaringType();
            if (!containingType.IsNil)
            {
                ProcessTypeDef(containingType, dllReader, pdbReader, docList);
            }
        }

        // And of course if this is an outer type, the only document info might be from methods in
        // nested types
        var nestedTypes = typeDef.GetNestedTypes();
        foreach (var nestedType in nestedTypes)
        {
            ProcessTypeDef(nestedType, dllReader, pdbReader, docList, processContainingType: false);
        }
    }

    private static void AddDocumentsFromTypeDefinitionDocuments(TypeDefinitionHandle typeDefHandle, MetadataReader pdbReader, HashSet<DocumentHandle> docList)
    {
        var handles = pdbReader.GetCustomDebugInformation(typeDefHandle);
        foreach (var cdiHandle in handles)
        {
            var cdi = pdbReader.GetCustomDebugInformation(cdiHandle);
            var guid = pdbReader.GetGuid(cdi.Kind);
            if (guid == PortableCustomDebugInfoKinds.TypeDefinitionDocuments)
            {
                if (((TypeDefinitionHandle)cdi.Parent).Equals(typeDefHandle))
                {
                    var reader = pdbReader.GetBlobReader(cdi.Value);
                    while (reader.RemainingBytes > 0)
                    {
                        docList.Add(MetadataTokens.DocumentHandle(reader.ReadCompressedInteger()));
                    }
                }
            }
        }
    }
}
