// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Completion;
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

            Add(builder, new CompletionChangeConverter());
            Add(builder, new CompletionItemConverter());
            Add(builder, new CompletionItemRulesConverter());
            Add(builder, new CharacterSetModificationRuleConverter());

            Add(builder, new CompletionDescriptionConverter());
            Add(builder, new CompletionTriggerConverter());
            Add(builder, new CompletionListConverter());

            Add(builder, new CompletionRulesConverter());
        }

        private class CompletionRulesConverter : BaseJsonConverter<CompletionRules>
        {
            protected override CompletionRules ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var dismissIfEmpty = ReadProperty<bool>(reader);
                var dismissIfLastCharacterDeleted = ReadProperty<bool>(reader);
                var defaultCommitCharacters = ReadProperty<IList<char>>(reader, serializer).ToImmutableArray();

                var defaultEnterKeyRule = (EnterKeyRule)ReadProperty<long>(reader);
                var snippetsRule = (SnippetsRule)ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return CompletionRules.Create(
                    dismissIfEmpty, dismissIfLastCharacterDeleted, defaultCommitCharacters, defaultEnterKeyRule, snippetsRule);
            }

            protected override void WriteValue(JsonWriter writer, CompletionRules value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(value.DismissIfEmpty));
                writer.WriteValue(value.DismissIfEmpty);

                writer.WritePropertyName(nameof(value.DismissIfLastCharacterDeleted));
                writer.WriteValue(value.DismissIfLastCharacterDeleted);

                writer.WritePropertyName(nameof(value.DefaultCommitCharacters));
                serializer.Serialize(writer, (IList<char>)value.DefaultCommitCharacters);

                writer.WritePropertyName(nameof(value.DefaultEnterKeyRule));
                writer.WriteValue((int)value.DefaultEnterKeyRule);

                writer.WritePropertyName(nameof(value.SnippetsRule));
                writer.WriteValue((int)value.SnippetsRule);

                writer.WriteEndObject();
            }
        }

        private class CompletionListConverter : BaseJsonConverter<CompletionList>
        {
            protected override CompletionList ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var items = ReadProperty<IList<CompletionItem>>(reader, serializer).ToImmutableArray();
                var span = ReadProperty<TextSpan>(reader, serializer);
                var rules = ReadProperty<CompletionRules>(reader, serializer);
                var suggestionModeItem = ReadProperty<CompletionItem>(reader, serializer);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return CompletionList.Create(span, items, rules, suggestionModeItem);
            }

            protected override void WriteValue(JsonWriter writer, CompletionList value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(value.Items));
                serializer.Serialize(writer, (IList<CompletionItem>)value.Items);

                writer.WritePropertyName(nameof(value.Span));
                serializer.Serialize(writer, value.Span);

                writer.WritePropertyName(nameof(value.Rules));
                serializer.Serialize(writer, value.Rules);

                writer.WritePropertyName(nameof(value.SuggestionModeItem));
                serializer.Serialize(writer, value.SuggestionModeItem);

                writer.WriteEndObject();
            }
        }

        private class CompletionTriggerConverter : BaseJsonConverter<CompletionTrigger>
        {
            protected override CompletionTrigger ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var trigger = (CompletionTriggerKind)ReadProperty<long>(reader);
                var character = ReadProperty<string>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new CompletionTrigger(trigger, GetCharacter(character));
            }

            protected override void WriteValue(JsonWriter writer, CompletionTrigger value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(value.Kind));
                writer.WriteValue((int)value.Kind);

                writer.WritePropertyName(nameof(value.Character));
                writer.WriteValue(value.Character);

                writer.WriteEndObject();
            }
        }

        private class CompletionDescriptionConverter : BaseJsonConverter<CompletionDescription>
        {
            protected override CompletionDescription ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var taggedParts = ReadProperty<IList<TaggedText>>(reader, serializer).ToImmutableArray();

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return CompletionDescription.Create(taggedParts);
            }

            protected override void WriteValue(JsonWriter writer, CompletionDescription value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(value.TaggedParts));
                serializer.Serialize(writer, (IList<TaggedText>)value.TaggedParts);

                writer.WriteEndObject();
            }
        }

        private class CharacterSetModificationRuleConverter : BaseJsonConverter<CharacterSetModificationRule>
        {
            protected override CharacterSetModificationRule ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var characterSetModificationKind = (CharacterSetModificationKind)ReadProperty<long>(reader);
                var characters = ReadProperty<IList<char>>(reader, serializer).ToImmutableArray();

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return CharacterSetModificationRule.Create(characterSetModificationKind, characters);
            }

            protected override void WriteValue(JsonWriter writer, CharacterSetModificationRule value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(value.Kind));
                writer.WriteValue((int)value.Kind);

                writer.WritePropertyName(nameof(value.Characters));
                serializer.Serialize(writer, (IList<char>)value.Characters);

                writer.WriteEndObject();
            }
        }

        private class CompletionItemRulesConverter : BaseJsonConverter<CompletionItemRules>
        {
            protected override CompletionItemRules ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var filterCharacterRules = ReadProperty<IList<CharacterSetModificationRule>>(reader, serializer).ToImmutableArray();
                var commitCharacterRules = ReadProperty<IList<CharacterSetModificationRule>>(reader, serializer).ToImmutableArray();
                var enterKeyRule = (EnterKeyRule)ReadProperty<long>(reader);
                var formatOnCommit = ReadProperty<bool>(reader);
                var matchPriority = ReadProperty<long>(reader);
                var selectionBehavior = (CompletionItemSelectionBehavior)ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return CompletionItemRules.Create(filterCharacterRules, commitCharacterRules, enterKeyRule, formatOnCommit, (int)matchPriority, selectionBehavior);
            }

            protected override void WriteValue(JsonWriter writer, CompletionItemRules value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(value.FilterCharacterRules));
                serializer.Serialize(writer, (IList<CharacterSetModificationRule>)value.FilterCharacterRules);

                writer.WritePropertyName(nameof(value.CommitCharacterRules));
                serializer.Serialize(writer, (IList<CharacterSetModificationRule>)value.CommitCharacterRules);

                writer.WritePropertyName(nameof(value.EnterKeyRule));
                writer.WriteValue((int)value.EnterKeyRule);

                writer.WritePropertyName(nameof(value.FormatOnCommit));
                writer.WriteValue(value.FormatOnCommit);

                writer.WritePropertyName(nameof(value.MatchPriority));
                writer.WriteValue(value.MatchPriority);

                writer.WritePropertyName(nameof(value.SelectionBehavior));
                writer.WriteValue((int)value.SelectionBehavior);

                writer.WriteEndObject();
            }
        }

        private class CompletionItemConverter : BaseJsonConverter<CompletionItem>
        {
            protected override CompletionItem ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var displayText = ReadProperty<string>(reader);
                var filterText = ReadProperty<string>(reader);
                var sortText = ReadProperty<string>(reader);
                var span = ReadProperty<TextSpan>(reader, serializer);
                var properties = ReadProperty<IDictionary<string, string>>(reader, serializer).ToImmutableDictionary();
                var tags = ReadProperty<IList<string>>(reader, serializer).ToImmutableArray();
                var rules = ReadProperty<CompletionItemRules>(reader, serializer);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                var item = CompletionItem.Create(displayText, filterText, sortText, properties, tags, rules);

                // not sure why we have this kind of pattern.
                item.Span = span;

                return item;
            }

            protected override void WriteValue(JsonWriter writer, CompletionItem value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(value.DisplayText));
                writer.WriteValue(value.DisplayText);

                writer.WritePropertyName(nameof(value.FilterText));
                writer.WriteValue(value.FilterText);

                writer.WritePropertyName(nameof(value.SortText));
                writer.WriteValue(value.SortText);

                writer.WritePropertyName(nameof(value.Span));
                serializer.Serialize(writer, value.Span);

                writer.WritePropertyName(nameof(value.Properties));
                serializer.Serialize(writer, (IDictionary<string, string>)value.Properties);

                writer.WritePropertyName(nameof(value.Tags));
                serializer.Serialize(writer, (IList<string>)value.Tags);

                writer.WritePropertyName(nameof(value.Rules));
                serializer.Serialize(writer, value.Rules);

                writer.WriteEndObject();
            }
        }

        private class CompletionChangeConverter : BaseJsonConverter<CompletionChange>
        {
            protected override CompletionChange ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var textChange = ReadProperty<TextChange>(reader, serializer);
                var newPosition = ReadProperty<long>(reader);
                var includesCommitCharacter = ReadProperty<bool>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return CompletionChange.Create(textChange, (newPosition == int.MinValue) ? (int?)null : (int)newPosition, includesCommitCharacter);
            }

            protected override void WriteValue(JsonWriter writer, CompletionChange value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(value.TextChange));
                serializer.Serialize(writer, value.TextChange);

                writer.WritePropertyName(nameof(value.NewPosition));
                writer.WriteValue(value.NewPosition ?? int.MinValue);

                writer.WritePropertyName(nameof(value.IncludesCommitCharacter));
                writer.WriteValue(value.IncludesCommitCharacter);

                writer.WriteEndObject();
            }
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

                switch (kind)
                {
                    case AddImportFixKind.ProjectSymbol:
                        return AddImportFixData.CreateForProjectSymbol(textChanges, title, tags, priority, projectReferenceToAdd);

                    case AddImportFixKind.MetadataSymbol:
                        return AddImportFixData.CreateForMetadataSymbol(textChanges, title, tags, priority, portableExecutableReferenceProjectId, portableExecutableReferenceFilePathToAdd);

                    case AddImportFixKind.PackageSymbol:
                        return AddImportFixData.CreateForPackageSymbol(textChanges, packageSource, packageName, packageVersionOpt);

                    case AddImportFixKind.ReferenceAssemblySymbol:
                        return AddImportFixData.CreateForReferenceAssemblySymbol(textChanges, title, assemblyReferenceAssemblyName, assemblyReferenceFullyQualifiedTypeName);
                }

                throw ExceptionUtilities.Unreachable;
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
