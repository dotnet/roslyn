// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class AggregateJsonConverter : JsonConverter
    {
        partial void AppendRoslynSpecificJsonConverters(ImmutableDictionary<Type, JsonConverter>.Builder builder)
        {
            Add(builder, new HighlightSpanJsonConverter());
            Add(builder, new TaggedTextJsonConverter());

            Add(builder, new TodoCommentDescriptorJsonConverter());
            Add(builder, new TodoCommentJsonConverter());
            Add(builder, new DesignerAttributeResultJsonConverter());

            Add(builder, new PackageSourceJsonConverter());
            Add(builder, new PackageWithTypeResultJsonConverter());
            Add(builder, new PackageWithAssemblyResultJsonConverter());

            Add(builder, new ReferenceAssemblyWithTypeResultJsonConverter());
            Add(builder, new AddImportFixDataJsonConverter());

            Add(builder, new AnalyzerPerformanceInfoConverter());
        }

        private class TodoCommentDescriptorJsonConverter : BaseJsonConverter<TodoCommentDescriptor>
        {
            protected override TodoCommentDescriptor ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var text = ReadProperty<string>(reader);
                var priority = ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TodoCommentDescriptor(text, (int)priority);
            }

            protected override void WriteValue(JsonWriter writer, TodoCommentDescriptor descriptor, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("text");
                writer.WriteValue(descriptor.Text);

                writer.WritePropertyName("priority");
                writer.WriteValue(descriptor.Priority);

                writer.WriteEndObject();
            }
        }

        private class TodoCommentJsonConverter : BaseJsonConverter<TodoComment>
        {
            protected override TodoComment ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var descriptor = ReadProperty<TodoCommentDescriptor>(reader, serializer);
                var message = ReadProperty<string>(reader);
                var position = ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TodoComment(descriptor, message, (int)position);
            }

            protected override void WriteValue(JsonWriter writer, TodoComment todoComment, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(TodoComment.Descriptor));
                serializer.Serialize(writer, todoComment.Descriptor);

                writer.WritePropertyName(nameof(TodoComment.Message));
                writer.WriteValue(todoComment.Message);

                writer.WritePropertyName(nameof(TodoComment.Position));
                writer.WriteValue(todoComment.Position);

                writer.WriteEndObject();
            }
        }

        private class DesignerAttributeResultJsonConverter : BaseJsonConverter<DesignerAttributeResult>
        {
            protected override DesignerAttributeResult ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var designerAttributeArgument = ReadProperty<string>(reader);
                var containsErrors = ReadProperty<bool>(reader);
                var applicable = ReadProperty<bool>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new DesignerAttributeResult(designerAttributeArgument, containsErrors, applicable);
            }

            protected override void WriteValue(JsonWriter writer, DesignerAttributeResult result, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(DesignerAttributeResult.DesignerAttributeArgument));
                writer.WriteValue(result.DesignerAttributeArgument);

                writer.WritePropertyName(nameof(DesignerAttributeResult.ContainsErrors));
                writer.WriteValue(result.ContainsErrors);

                writer.WritePropertyName(nameof(DesignerAttributeResult.Applicable));
                writer.WriteValue(result.Applicable);

                writer.WriteEndObject();
            }
        }

        private class PackageSourceJsonConverter : BaseJsonConverter<PackageSource>
        {
            protected override PackageSource ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var name = ReadProperty<string>(reader);
                var source = ReadProperty<string>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new PackageSource(name, source);
            }

            protected override void WriteValue(JsonWriter writer, PackageSource source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(PackageSource.Name));
                writer.WriteValue(source.Name);

                writer.WritePropertyName(nameof(PackageSource.Source));
                writer.WriteValue(source.Source);

                writer.WriteEndObject();
            }
        }

        private class HighlightSpanJsonConverter : BaseJsonConverter<HighlightSpan>
        {
            protected override HighlightSpan ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var textSpan = ReadProperty<TextSpan>(reader, serializer);
                var kind = (HighlightSpanKind)ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new HighlightSpan(textSpan, kind);
            }

            protected override void WriteValue(JsonWriter writer, HighlightSpan source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(HighlightSpan.TextSpan));
                serializer.Serialize(writer, source.TextSpan);

                writer.WritePropertyName(nameof(HighlightSpan.Kind));
                writer.WriteValue(source.Kind);

                writer.WriteEndObject();
            }
        }

        private class PackageWithTypeResultJsonConverter : BaseJsonConverter<PackageWithTypeResult>
        {
            protected override PackageWithTypeResult ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var packageName = ReadProperty<string>(reader);
                var typeName = ReadProperty<string>(reader);
                var version = ReadProperty<string>(reader);
                var rank = (int)ReadProperty<long>(reader);
                var containingNamespaceNames = ReadProperty<IList<string>>(reader, serializer);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new PackageWithTypeResult(packageName, typeName, version, rank, containingNamespaceNames);
            }

            protected override void WriteValue(JsonWriter writer, PackageWithTypeResult source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(PackageWithTypeResult.PackageName));
                writer.WriteValue(source.PackageName);

                writer.WritePropertyName(nameof(PackageWithTypeResult.TypeName));
                writer.WriteValue(source.TypeName);

                writer.WritePropertyName(nameof(PackageWithTypeResult.Version));
                writer.WriteValue(source.Version);

                writer.WritePropertyName(nameof(PackageWithTypeResult.Rank));
                writer.WriteValue(source.Rank);

                writer.WritePropertyName(nameof(PackageWithTypeResult.ContainingNamespaceNames));
                serializer.Serialize(writer, source.ContainingNamespaceNames);

                writer.WriteEndObject();
            }
        }

        private class PackageWithAssemblyResultJsonConverter : BaseJsonConverter<PackageWithAssemblyResult>
        {
            protected override PackageWithAssemblyResult ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var packageName = ReadProperty<string>(reader);
                var version = ReadProperty<string>(reader);
                var rank = (int)ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new PackageWithAssemblyResult(packageName, version, rank);
            }

            protected override void WriteValue(JsonWriter writer, PackageWithAssemblyResult source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(PackageWithAssemblyResult.PackageName));
                writer.WriteValue(source.PackageName);

                writer.WritePropertyName(nameof(PackageWithAssemblyResult.Version));
                writer.WriteValue(source.Version);

                writer.WritePropertyName(nameof(PackageWithAssemblyResult.Rank));
                writer.WriteValue(source.Rank);

                writer.WriteEndObject();
            }
        }

        private class ReferenceAssemblyWithTypeResultJsonConverter : BaseJsonConverter<ReferenceAssemblyWithTypeResult>
        {
            protected override ReferenceAssemblyWithTypeResult ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var assemblyName = ReadProperty<string>(reader);
                var typeName = ReadProperty<string>(reader);
                var containingNamespaceNames = ReadProperty<IList<string>>(reader, serializer);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new ReferenceAssemblyWithTypeResult(assemblyName, typeName, containingNamespaceNames);
            }

            protected override void WriteValue(JsonWriter writer, ReferenceAssemblyWithTypeResult source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(ReferenceAssemblyWithTypeResult.AssemblyName));
                writer.WriteValue(source.AssemblyName);

                writer.WritePropertyName(nameof(ReferenceAssemblyWithTypeResult.TypeName));
                writer.WriteValue(source.TypeName);

                writer.WritePropertyName(nameof(ReferenceAssemblyWithTypeResult.ContainingNamespaceNames));
                serializer.Serialize(writer, source.ContainingNamespaceNames);

                writer.WriteEndObject();
            }
        }

        private class TaggedTextJsonConverter : BaseJsonConverter<TaggedText>
        {
            protected override TaggedText ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var tag = ReadProperty<string>(reader);
                var text = ReadProperty<string>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TaggedText(tag, text);
            }

            protected override void WriteValue(JsonWriter writer, TaggedText source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(TaggedText.Tag));
                writer.WriteValue(source.Tag);

                writer.WritePropertyName(nameof(TaggedText.Text));
                writer.WriteValue(source.Text);

                writer.WriteEndObject();
            }
        }

        private class AddImportFixDataJsonConverter : BaseJsonConverter<AddImportFixData>
        {
            protected override AddImportFixData ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var kind = (AddImportFixKind)ReadProperty<long>(reader);
                var textChanges = ReadProperty<IList<TextChange>>(reader, serializer).ToImmutableArrayOrEmpty();
                var title = ReadProperty<string>(reader);
                var tags = ReadProperty<IList<string>>(reader, serializer).ToImmutableArrayOrEmpty();
                var priority = (CodeActionPriority)ReadProperty<long>(reader);

                var projectReferenceToAdd = ReadProperty<ProjectId>(reader, serializer);

                var portableExecutableReferenceProjectId = ReadProperty<ProjectId>(reader, serializer);
                var portableExecutableReferenceFilePathToAdd = ReadProperty<string>(reader);

                var assemblyReferenceAssemblyName = ReadProperty<string>(reader);
                var assemblyReferenceFullyQualifiedTypeName = ReadProperty<string>(reader);

                var packageSource = ReadProperty<string>(reader);
                var packageName = ReadProperty<string>(reader);
                var packageVersionOpt = ReadProperty<string>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return kind switch
                {
                    AddImportFixKind.ProjectSymbol => AddImportFixData.CreateForProjectSymbol(textChanges, title, tags, priority, projectReferenceToAdd),
                    AddImportFixKind.MetadataSymbol => AddImportFixData.CreateForMetadataSymbol(textChanges, title, tags, priority, portableExecutableReferenceProjectId, portableExecutableReferenceFilePathToAdd),
                    AddImportFixKind.PackageSymbol => AddImportFixData.CreateForPackageSymbol(textChanges, packageSource, packageName, packageVersionOpt),
                    AddImportFixKind.ReferenceAssemblySymbol => AddImportFixData.CreateForReferenceAssemblySymbol(textChanges, title, assemblyReferenceAssemblyName, assemblyReferenceFullyQualifiedTypeName),
                    _ => throw ExceptionUtilities.Unreachable,
                };
            }

            protected override void WriteValue(JsonWriter writer, AddImportFixData source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(AddImportFixData.Kind));
                writer.WriteValue((int)source.Kind);

                writer.WritePropertyName(nameof(AddImportFixData.TextChanges));
                serializer.Serialize(writer, source.TextChanges ?? SpecializedCollections.EmptyList<TextChange>());

                writer.WritePropertyName(nameof(AddImportFixData.Title));
                writer.WriteValue(source.Title);

                writer.WritePropertyName(nameof(AddImportFixData.Tags));
                serializer.Serialize(writer, source.Tags ?? SpecializedCollections.EmptyList<string>());

                writer.WritePropertyName(nameof(AddImportFixData.Priority));
                writer.WriteValue((int)source.Priority);

                writer.WritePropertyName(nameof(AddImportFixData.ProjectReferenceToAdd));
                serializer.Serialize(writer, source.ProjectReferenceToAdd);

                writer.WritePropertyName(nameof(AddImportFixData.PortableExecutableReferenceProjectId));
                serializer.Serialize(writer, source.PortableExecutableReferenceProjectId);

                writer.WritePropertyName(nameof(AddImportFixData.PortableExecutableReferenceFilePathToAdd));
                writer.WriteValue(source.PortableExecutableReferenceFilePathToAdd);

                writer.WritePropertyName(nameof(AddImportFixData.AssemblyReferenceAssemblyName));
                writer.WriteValue(source.AssemblyReferenceAssemblyName);

                writer.WritePropertyName(nameof(AddImportFixData.AssemblyReferenceFullyQualifiedTypeName));
                writer.WriteValue(source.AssemblyReferenceFullyQualifiedTypeName);

                writer.WritePropertyName(nameof(AddImportFixData.PackageSource));
                writer.WriteValue(source.PackageSource);

                writer.WritePropertyName(nameof(AddImportFixData.PackageName));
                writer.WriteValue(source.PackageName);

                writer.WritePropertyName(nameof(AddImportFixData.PackageVersionOpt));
                writer.WriteValue(source.PackageVersionOpt);

                writer.WriteEndObject();
            }
        }

        private class AnalyzerPerformanceInfoConverter : BaseJsonConverter<AnalyzerPerformanceInfo>
        {
            protected override AnalyzerPerformanceInfo ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var analyzerid = ReadProperty<string>(reader);
                var builtIn = ReadProperty<bool>(reader);
                var timeSpan = ReadProperty<TimeSpan>(reader, serializer);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new AnalyzerPerformanceInfo(analyzerid, builtIn, timeSpan);
            }

            protected override void WriteValue(JsonWriter writer, AnalyzerPerformanceInfo info, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(AnalyzerPerformanceInfo.AnalyzerId));
                writer.WriteValue(info.AnalyzerId);

                writer.WritePropertyName(nameof(AnalyzerPerformanceInfo.BuiltIn));
                writer.WriteValue(info.BuiltIn);

                writer.WritePropertyName(nameof(AnalyzerPerformanceInfo.TimeSpan));
                serializer.Serialize(writer, info.TimeSpan);

                writer.WriteEndObject();
            }
        }
    }
}
