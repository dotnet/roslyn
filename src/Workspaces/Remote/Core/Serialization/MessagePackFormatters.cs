// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Defines MessagePack formatters for public types without a public constructor suitable for deserialization.
    /// Roslyn internal types should always be annotated with <see cref="DataContractAttribute"/> and have the right constructor.
    /// </summary>
    internal sealed class MessagePackFormatters
    {
        private static readonly ImmutableArray<IMessagePackFormatter> s_formatters = ImmutableArray.Create<IMessagePackFormatter>(
            SolutionIdFormatter.Instance,
            ProjectIdFormatter.Instance,
            DocumentIdFormatter.Instance,
            EnumFormatters.AnalysisKind,
            EnumFormatters.AnalysisKind.CreateNullable(),
            EnumFormatters.HighlightSpanKind,
            EnumFormatters.Scope,
            EnumFormatters.RelatedLocationType,
            EnumFormatters.SearchKind,
            EnumFormatters.NavigateToMatchKind,
            EnumFormatters.Glyph,
            EnumFormatters.Glyph.CreateNullable(),
            EnumFormatters.TaggedTextStyle,
            EnumFormatters.ValueUsageInfo,
            EnumFormatters.ValueUsageInfo.CreateNullable(),
            EnumFormatters.TypeOrNamespaceUsageInfo,
            EnumFormatters.TypeOrNamespaceUsageInfo.CreateNullable(),
            EnumFormatters.AddImportFixKind,
            EnumFormatters.CodeActionPriority,
            EnumFormatters.DependentTypesKind);

        private static readonly ImmutableArray<IFormatterResolver> s_resolvers = ImmutableArray.Create<IFormatterResolver>(
            ImmutableCollectionMessagePackResolver.Instance,
            StandardResolverAllowPrivate.Instance);

        internal static readonly IFormatterResolver DefaultResolver = CompositeResolver.Create(s_formatters, s_resolvers);

        internal static IFormatterResolver CreateResolver(ImmutableArray<IMessagePackFormatter> additionalFormatters, ImmutableArray<IFormatterResolver> additionalResolvers)
            => (additionalFormatters.IsEmpty && additionalResolvers.IsEmpty) ? DefaultResolver : CompositeResolver.Create(s_formatters.AddRange(additionalFormatters), s_resolvers.AddRange(additionalResolvers));

        // TODO: remove https://github.com/neuecc/MessagePack-CSharp/issues/1025
        internal static class EnumFormatters
        {
            public static readonly EnumFormatter<AnalysisKind> AnalysisKind = new(value => (int)value, value => (AnalysisKind)value);
            public static readonly EnumFormatter<HighlightSpanKind> HighlightSpanKind = new(value => (int)value, value => (HighlightSpanKind)value);
            public static readonly EnumFormatter<Scope> Scope = new(value => (int)value, value => (Scope)value);
            public static readonly EnumFormatter<RelatedLocationType> RelatedLocationType = new(value => (int)value, value => (RelatedLocationType)value);
            public static readonly EnumFormatter<SearchKind> SearchKind = new(value => (int)value, value => (SearchKind)value);
            public static readonly EnumFormatter<NavigateToMatchKind> NavigateToMatchKind = new(value => (int)value, value => (NavigateToMatchKind)value);
            public static readonly EnumFormatter<Glyph> Glyph = new(value => (int)value, value => (Glyph)value);
            public static readonly EnumFormatter<TaggedTextStyle> TaggedTextStyle = new(value => (int)value, value => (TaggedTextStyle)value);
            public static readonly EnumFormatter<ValueUsageInfo> ValueUsageInfo = new(value => (int)value, value => (ValueUsageInfo)value);
            public static readonly EnumFormatter<TypeOrNamespaceUsageInfo> TypeOrNamespaceUsageInfo = new(value => (int)value, value => (TypeOrNamespaceUsageInfo)value);
            public static readonly EnumFormatter<AddImportFixKind> AddImportFixKind = new(value => (int)value, value => (AddImportFixKind)value);
            public static readonly EnumFormatter<CodeActionPriority> CodeActionPriority = new(value => (int)value, value => (CodeActionPriority)value);
            public static readonly EnumFormatter<DependentTypesKind> DependentTypesKind = new(value => (int)value, value => (DependentTypesKind)value);
        }

        internal sealed class SolutionIdFormatter : IMessagePackFormatter<SolutionId?>
        {
            public static readonly SolutionIdFormatter Instance = new SolutionIdFormatter();

            public SolutionId? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                try
                {
                    if (reader.TryReadNil())
                    {
                        return null;
                    }

                    Contract.ThrowIfFalse(reader.ReadArrayHeader() == 2);
                    var id = GuidFormatter.Instance.Deserialize(ref reader, options);
                    var debugName = reader.ReadString();

                    return SolutionId.CreateFromSerialized(id, debugName);
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }

            public void Serialize(ref MessagePackWriter writer, SolutionId? value, MessagePackSerializerOptions options)
            {
                try
                {
                    if (value is null)
                    {
                        writer.WriteNil();
                    }
                    else
                    {
                        writer.WriteArrayHeader(2);
                        GuidFormatter.Instance.Serialize(ref writer, value.Id, options);
                        writer.Write(value.DebugName);
                    }
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }
        }

        internal sealed class ProjectIdFormatter : IMessagePackFormatter<ProjectId?>
        {
            public static readonly ProjectIdFormatter Instance = new ProjectIdFormatter();

            public ProjectId? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                try
                {
                    if (reader.TryReadNil())
                    {
                        return null;
                    }

                    Contract.ThrowIfFalse(reader.ReadArrayHeader() == 2);
                    var id = GuidFormatter.Instance.Deserialize(ref reader, options);
                    var debugName = reader.ReadString();

                    return ProjectId.CreateFromSerialized(id, debugName);
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }

            public void Serialize(ref MessagePackWriter writer, ProjectId? value, MessagePackSerializerOptions options)
            {
                try
                {
                    if (value is null)
                    {
                        writer.WriteNil();
                    }
                    else
                    {
                        writer.WriteArrayHeader(2);
                        GuidFormatter.Instance.Serialize(ref writer, value.Id, options);
                        writer.Write(value.DebugName);
                    }
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }
        }

        internal sealed class DocumentIdFormatter : IMessagePackFormatter<DocumentId?>
        {
            public static readonly DocumentIdFormatter Instance = new DocumentIdFormatter();

            public DocumentId? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                try
                {
                    if (reader.TryReadNil())
                    {
                        return null;
                    }

                    Contract.ThrowIfFalse(reader.ReadArrayHeader() == 3);

                    var projectId = ProjectIdFormatter.Instance.Deserialize(ref reader, options);
                    Contract.ThrowIfNull(projectId);

                    var id = GuidFormatter.Instance.Deserialize(ref reader, options);
                    var debugName = reader.ReadString();

                    return DocumentId.CreateFromSerialized(projectId, id, debugName);
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }

            public void Serialize(ref MessagePackWriter writer, DocumentId? value, MessagePackSerializerOptions options)
            {
                try
                {
                    if (value is null)
                    {
                        writer.WriteNil();
                    }
                    else
                    {
                        writer.WriteArrayHeader(3);
                        ProjectIdFormatter.Instance.Serialize(ref writer, value.ProjectId, options);
                        GuidFormatter.Instance.Serialize(ref writer, value.Id, options);
                        writer.Write(value.DebugName);
                    }
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }
        }

        // TODO: remove https://github.com/neuecc/MessagePack-CSharp/issues/1025
        internal sealed class EnumFormatter<TEnum> : IMessagePackFormatter<TEnum>
            where TEnum : struct
        {
            private readonly Func<TEnum, int> _toInt;
            private readonly Func<int, TEnum> _toEnum;

            static EnumFormatter()
            {
                var underlyingType = typeof(TEnum).GetEnumUnderlyingType();
                Contract.ThrowIfTrue(underlyingType == typeof(long) || underlyingType == typeof(ulong));
            }

            public EnumFormatter(Func<TEnum, int> toInt, Func<int, TEnum> toEnum)
            {
                _toInt = toInt;
                _toEnum = toEnum;
            }

            public TEnum Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                try
                {
                    return _toEnum(reader.ReadInt32());
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }

            public void Serialize(ref MessagePackWriter writer, TEnum value, MessagePackSerializerOptions options)
            {
                try
                {
                    writer.WriteInt32(_toInt(value));
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }

            public NullableEnum CreateNullable()
                => new NullableEnum(_toInt, _toEnum);

            internal sealed class NullableEnum : IMessagePackFormatter<TEnum?>
            {
                private readonly Func<TEnum, int> _toInt;
                private readonly Func<int, TEnum> _toEnum;

                public NullableEnum(Func<TEnum, int> toInt, Func<int, TEnum> toEnum)
                {
                    _toInt = toInt;
                    _toEnum = toEnum;
                }

                public TEnum? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    try
                    {
                        return reader.TryReadNil() ? null : _toEnum(reader.ReadInt32());
                    }
                    catch (Exception e) when (e is not MessagePackSerializationException)
                    {
                        throw new MessagePackSerializationException(e.Message, e);
                    }
                }

                public void Serialize(ref MessagePackWriter writer, TEnum? value, MessagePackSerializerOptions options)
                {
                    try
                    {
                        if (value == null)
                        {
                            writer.WriteNil();
                        }
                        else
                        {
                            writer.WriteInt32(_toInt(value.Value));
                        }
                    }
                    catch (Exception e) when (e is not MessagePackSerializationException)
                    {
                        throw new MessagePackSerializationException(e.Message, e);
                    }
                }
            }
        }
    }
}
