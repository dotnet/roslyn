// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static class SymbolSourceFileFinder
    {
        private static bool AddDocumentsfromMethodDebugInformation(MethodDefinitionHandle methodHandle, MetadataReader pdbReader, List<DocumentHandle> docList)
        {

            var debugInfo = pdbReader.GetMethodDebugInformation(methodHandle);
            if (!debugInfo.Document.IsNil && docList.Count < 1)
            {
                // duplicates empty or not :: x
                docList.Add(debugInfo.Document);
                return true;
            }

            if (!debugInfo.SequencePointsBlob.IsNil)
            {

                // check duplicates :: x
                foreach (var point in debugInfo.GetSequencePoints())
                {
                    if (!point.Document.IsNil)
                    {
                        // Hash set instead of list for time. :: x
                        if (!docList.Contains(point.Document))
                        {
                            docList.Add(point.Document);
                        }
                    }

                }
                return true;
            }
            return false;
        }

        private static bool AddTypeDocumentsInCustomDebugInformation(TypeDefinitionHandle typeHandle, MetadataReader pdbReader, List<DocumentHandle> docList)
        {
            foreach (var handle in pdbReader.GetCustomDebugInformation(typeHandle))
            {
                var typeId = pdbReader.GetCustomDebugInformation(handle).Parent;

                if (((TypeDefinitionHandle)typeId).Equals(typeHandle))
                {
                    var blob = pdbReader.GetCustomDebugInformation(handle).Value;
                    var reader = pdbReader.GetBlobReader(blob);
                    while (reader.RemainingBytes > 0)
                    {
                        docList.Add(MetadataTokens.DocumentHandle(reader.ReadCompressedInteger()));
                    }
                    return true;
                }
            }
            return false;
        }

        private static void AddTypeDocuments(PENamedTypeSymbol typeSymbol, MetadataReader pdbReader, List<DocumentHandle> docList)
        {
            if (AddTypeDocumentsInCustomDebugInformation(typeSymbol.Handle, pdbReader, docList))
            {
                return;
            }
            foreach (var typeMethod in typeSymbol.GetMethodsToEmit())
            {
                // Test case where method does not have body, where can you find document?
                // duplicate 
                if (AddDocumentsfromMethodDebugInformation(((PEMethodSymbol)typeMethod).Handle, pdbReader, docList))
                {
                }
            }
        }

        internal static List<DocumentHandle> FindSourceDocuments(Symbol symbol, MetadataReader pdbReader)
        {
            var docList = new List<DocumentHandle>();
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    var method = (PEMethodSymbol)symbol;
                    if (AddDocumentsfromMethodDebugInformation(method.Handle, pdbReader, docList))
                    {
                    }
                    else
                    {
                        var typeM = (PENamedTypeSymbol)(method.ContainingType);
                        AddTypeDocumentsInCustomDebugInformation(typeM.Handle, pdbReader, docList);
                    }
                    break;
                case SymbolKind.Field:
                    var field = (PEFieldSymbol)symbol;
                    var typeF = (PENamedTypeSymbol)field.ContainingType;
                    AddTypeDocuments(typeF, pdbReader, docList);
                    break;
                case SymbolKind.Property:
                    var propertyMethod = (PEMethodSymbol)((PEPropertySymbol)symbol).GetMethod;
                    if (propertyMethod.Equals(null))
                    {
                        propertyMethod = (PEMethodSymbol)((PEPropertySymbol)symbol).SetMethod;
                        if (propertyMethod.Equals(null))
                        {
                            // throw error
                        }
                    }
                    // check get method if null, use set vice versa. Also check for bad metadata. :: x
                    AddDocumentsfromMethodDebugInformation(propertyMethod.Handle, pdbReader, docList);
                    break;
                case SymbolKind.Event:
                    var eventMethod = (PEMethodSymbol)((PEEventSymbol)symbol).AddMethod;
                    if (AddDocumentsfromMethodDebugInformation(eventMethod.Handle, pdbReader, docList) && !eventMethod.Equals(null)) { }
                    else
                    {
                        eventMethod = (PEMethodSymbol)((PEEventSymbol)symbol).RemoveMethod;
                        if (AddDocumentsfromMethodDebugInformation(eventMethod.Handle, pdbReader, docList) && !eventMethod.Equals(null)) { }
                        else
                        {
                            var typeE = (PENamedTypeSymbol)(eventMethod.ContainingType);
                            AddTypeDocumentsInCustomDebugInformation(typeE.Handle, pdbReader, docList);
                        }
                    }
                    break;
                case SymbolKind.NamedType:
                    var typeT = (PENamedTypeSymbol)symbol;
                    AddTypeDocuments(typeT, pdbReader, docList);
                    break;
                case SymbolKind.Parameter:
                    var parameterSymbol = (PEParameterSymbol)symbol;
                    var parameterContainingSymbol = parameterSymbol.ContainingSymbol;
                    switch (parameterContainingSymbol.Kind)
                    {
                        case SymbolKind.Method:
                            var methodP = (PEMethodSymbol)parameterContainingSymbol;
                            if (!AddDocumentsfromMethodDebugInformation(methodP.Handle, pdbReader, docList))
                            {
                                var typeM = (PENamedTypeSymbol)(methodP.ContainingType);
                                AddTypeDocuments(typeM, pdbReader, docList);
                            }
                            break;
                        case SymbolKind.Property:
                            var property = (PEPropertySymbol)parameterContainingSymbol;
                            var propertyMethodP = (PEMethodSymbol)(property.GetMethod ?? property.SetMethod);
                            if (propertyMethodP != null)
                            {
                                AddDocumentsfromMethodDebugInformation(propertyMethodP.Handle, pdbReader, docList);
                            }
                            break;
                        default:
                            // error message

                            break;

                    }
                    // check kind of symbol, property or methods :: x
                    break;
                // Parameter :: x
                default:
                    // error message for incorrect type
                    break;
            }

            return docList;
        }

        public static IEnumerable<string> FindSourceFiles(ISymbol symbol, string pdbPath)
        {
            var csharpSymbol = symbol.GetSymbol();
            using var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(pdbPath));

            var pdbReader = pdbReaderProvider.GetMetadataReader();
            var documentHandles = FindSourceDocuments(csharpSymbol, pdbReader);

            var result = documentHandles.Select(h => pdbReader.GetString(pdbReader.GetDocument(h).Name)).ToArray();
            pdbReaderProvider.Dispose();
            return result;
        }
    }
}
