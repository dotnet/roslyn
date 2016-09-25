// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Arguments
{
    #region Common Arguments

    /// <summary>
    /// Arguments to pass from client to server when performing operations
    /// </summary>
    internal class SerializableProjectId
    {
        public Guid Id;
        public string DebugName;

        public static SerializableProjectId Dehydrate(ProjectId id)
        {
            return new SerializableProjectId { Id = id.Id, DebugName = id.DebugName };
        }

        public ProjectId Rehydrate()
        {
            return ProjectId.CreateFromSerialized(Id, DebugName);
        }
    }

    internal class SerializableDocumentId
    {
        public SerializableProjectId ProjectId;
        public Guid Id;
        public string DebugName;

        public static SerializableDocumentId Dehydrate(Document document)
        {
            return Dehydrate(document.Id);
        }

        public static SerializableDocumentId Dehydrate(DocumentId id)
        {
            return new SerializableDocumentId
            {
                ProjectId = SerializableProjectId.Dehydrate(id.ProjectId),
                Id = id.Id,
                DebugName = id.DebugName
            };
        }

        public DocumentId Rehydrate()
        {
            return DocumentId.CreateFromSerialized(
                ProjectId.Rehydrate(), Id, DebugName);
        }
    }

    internal class SerializableTextSpan
    {
        public int Start;
        public int Length;

        public static SerializableTextSpan Dehydrate(TextSpan textSpan)
        {
            return new SerializableTextSpan { Start = textSpan.Start, Length = textSpan.Length };
        }

        public TextSpan Rehydrate()
        {
            return new TextSpan(Start, Length);
        }
    }

    internal class SerializableTaggedText
    {
        public string Tag;
        public string Text;

        public static SerializableTaggedText Dehydrate(TaggedText taggedText)
        {
            return new SerializableTaggedText { Tag = taggedText.Tag, Text = taggedText.Text };
        }

        internal static SerializableTaggedText[] Dehydrate(ImmutableArray<TaggedText> displayTaggedParts)
        {
            return displayTaggedParts.Select(Dehydrate).ToArray();
        }

        public TaggedText Rehydrate()
        {
            return new TaggedText(Tag, Text);
        }
    }

    #endregion

    #region SymbolSearch

    internal class SerializablePackageWithTypeResult
    {
        public string PackageName;
        public string TypeName;
        public string Version;
        public int Rank;
        public string[] ContainingNamespaceNames;

        public static SerializablePackageWithTypeResult Dehydrate(PackageWithTypeResult result)
        {
            return new SerializablePackageWithTypeResult
            {
                PackageName = result.PackageName,
                TypeName = result.TypeName,
                Version = result.Version,
                Rank = result.Rank,
                ContainingNamespaceNames = result.ContainingNamespaceNames.ToArray(),
            };
        }

        public PackageWithTypeResult Rehydrate()
        {
            return new PackageWithTypeResult(
                PackageName, TypeName, Version, Rank, ContainingNamespaceNames);
        }
    }

    internal class SerializableReferenceAssemblyWithTypeResult
    {
        public string AssemblyName;
        public string TypeName;
        public string[] ContainingNamespaceNames;

        public static SerializableReferenceAssemblyWithTypeResult Dehydrate(
            ReferenceAssemblyWithTypeResult result)
        {
            return new SerializableReferenceAssemblyWithTypeResult
            {
                ContainingNamespaceNames = result.ContainingNamespaceNames.ToArray(),
                AssemblyName = result.AssemblyName,
                TypeName = result.TypeName
            };
        }

        public ReferenceAssemblyWithTypeResult Rehydrate()
        {
            return new ReferenceAssemblyWithTypeResult(AssemblyName, TypeName, ContainingNamespaceNames);
        }
    }

    #endregion
}