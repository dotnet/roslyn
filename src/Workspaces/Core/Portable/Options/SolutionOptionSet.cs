// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An implementation of <see cref="OptionSet"/> that fetches values it doesn't know about to the workspace's option service. It ensures a contract
    /// that values are immutable from this instance once observed.
    /// </summary>
    internal sealed class SolutionOptionSet : SerializableOptionSet
    {
        private readonly WorkspaceOptionSet _workspaceOptionSet;
        private readonly ImmutableHashSet<IOption> _serializableOptions;

        private ImmutableDictionary<OptionKey, object?> _serializableOptionValues;

        internal SolutionOptionSet(WorkspaceOptionSet workspaceOptionSet, ImmutableHashSet<IOption> serializableOptions, ImmutableDictionary<OptionKey, object?> values)
        {
            _workspaceOptionSet = workspaceOptionSet;
            _serializableOptions = serializableOptions;
            _serializableOptionValues = values;
        }

        private SolutionOptionSet(WorkspaceOptionSet workspaceOptionSet, ImmutableHashSet<IOption> serializableOptions)
        {
            _workspaceOptionSet = workspaceOptionSet;
            _serializableOptions = serializableOptions;
            _serializableOptionValues = ImmutableDictionary<OptionKey, object?>.Empty;
        }

        public static SolutionOptionSet Deserialize(ObjectReader reader, IOptionService optionService, CancellationToken cancellationToken)
        {
            var options = new SolutionOptionSet(optionService.GetOptions(), optionService.GetRegisteredSerializableOptions());
            options.Deserialize(reader, cancellationToken);
            return options;
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
        public override object? GetOption(OptionKey optionKey)
        {
            if (_serializableOptionValues.TryGetValue(optionKey, out var value))
            {
                return value;
            }

            return _workspaceOptionSet.GetOption(optionKey);
        }

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
        {
            // make sure we first load this in current optionset
            this.GetOption(optionAndLanguage);

            var workspaceOptionSet = (WorkspaceOptionSet)_workspaceOptionSet.WithChangedOption(optionAndLanguage, value);
            var serializableOptionValues = _serializableOptions.Contains(optionAndLanguage.Option)
                ? _serializableOptionValues.SetItem(optionAndLanguage, value)
                : ImmutableDictionary.CreateRange(_serializableOptionValues);
            return new SolutionOptionSet(workspaceOptionSet, _serializableOptions, serializableOptionValues);
        }

        /// <summary>
        /// Gets a list of all the options that were accessed.
        /// </summary>
        internal IEnumerable<OptionKey> GetChangedOptions()
            => GetChangedOptions(_workspaceOptionSet);

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet? optionSet)
        {
            if (optionSet == this)
            {
                yield break;
            }

            foreach (var kvp in _serializableOptionValues)
            {
                var currentValue = optionSet?.GetOption(kvp.Key);
                if (!object.Equals(currentValue, kvp.Value))
                {
                    yield return kvp.Key;
                }
            }

            foreach (var changedOption in _workspaceOptionSet.GetChangedOptions(optionSet))
            {
                yield return changedOption;
            }
        }

        public override void Serialize(ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var builder = new SortedDictionary<OptionKey, (OptionValueKind, object?)>(OptionKeyComparer.Instance);
            foreach (var (optionKey, value) in _serializableOptionValues)
            {
                if (!_serializableOptions.Contains(optionKey.Option))
                {
                    continue;
                }

                var kind = OptionValueKind.Null;
                object? valueToWrite = null;
                if (value != null)
                {
                    switch (value)
                    {
                        case ICodeStyleOption codeStyleOption:
                            if (optionKey.Option.Type.GenericTypeArguments.Length != 1)
                            {
                                continue;
                            }

                            kind = OptionValueKind.CodeStyleOption;
                            valueToWrite = codeStyleOption.ToXElement().ToString();
                            break;

                        case NamingStylePreferences stylePreferences:
                            kind = OptionValueKind.NamingStylePreferences;
                            valueToWrite = stylePreferences.CreateXElement().ToString();
                            break;

                        case string str:
                            kind = OptionValueKind.String;
                            valueToWrite = str;
                            break;

                        default:
                            var typeInfo = value.GetType().GetTypeInfo();
                            if (typeInfo.IsEnum)
                            {
                                kind = OptionValueKind.Enum;
                                valueToWrite = (int)value;
                                break;
                            }
                            else if (optionKey.Option.Type.IsSerializable)
                            {
                                kind = OptionValueKind.Serializable;
                                valueToWrite = value;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                    }
                }

                builder.Add(optionKey, (kind, valueToWrite));
            }

            writer.WriteInt32(builder.Count);
            foreach (var (optionKey, (kind, value)) in builder)
            {
                writer.WriteString(optionKey.Option.Name);
                writer.WriteString(optionKey.Option.Feature);
                writer.WriteBoolean(optionKey.Option.IsPerLanguage);
                if (optionKey.Option.IsPerLanguage)
                {
                    writer.WriteString(optionKey.Language);
                }
                writer.WriteInt32((int)kind);
                if (kind == OptionValueKind.Enum)
                {
                    RoslynDebug.Assert(value != null);
                    writer.WriteInt32((int)value);
                }
                else
                {
                    writer.WriteValue(value);
                }
            }
        }

        public override void Deserialize(ObjectReader reader, CancellationToken cancellationToken)
        {
            Debug.Assert(_serializableOptionValues.IsEmpty);

            cancellationToken.ThrowIfCancellationRequested();

            var lookup = _serializableOptions.ToLookup(o => o.Name);
            var count = reader.ReadInt32();
            var builder = ImmutableDictionary.CreateBuilder<OptionKey, object?>();
            for (var i = 0; i < count; i++)
            {
                var name = reader.ReadString();
                var feature = reader.ReadString();
                var isPerLanguage = reader.ReadBoolean();
                var language = isPerLanguage ? reader.ReadString() : null;
                var kind = (OptionValueKind)reader.ReadInt32();
                var readValue = kind == OptionValueKind.Enum ? reader.ReadInt32() : reader.ReadValue();

                OptionKey optionKey = default;
                foreach (var option in lookup[name])
                {
                    if (option.Feature == feature &&
                        option.IsPerLanguage == isPerLanguage)
                    {
                        optionKey = new OptionKey(option, language);
                        break;
                    }
                }

                if (optionKey == default ||
                    !_serializableOptions.Contains(optionKey.Option))
                {
                    continue;
                }

                object? optionValue;
                switch (kind)
                {
                    case OptionValueKind.CodeStyleOption:
                        var defaultValue = optionKey.Option.DefaultValue as ICodeStyleOption;
                        if (defaultValue == null ||
                            optionKey.Option.Type.GenericTypeArguments.Length != 1)
                        {
                            continue;
                        }

                        var value = CodeStyleOption<object>.FromXElement(XElement.Parse((string)readValue)).Value;
                        var type = optionKey.Option.Type.GenericTypeArguments[0];
                        var convertedValue = type.IsEnum ? Enum.ToObject(type, value) : Convert.ChangeType(value, type);
                        optionValue = defaultValue.WithValue(convertedValue);
                        break;

                    case OptionValueKind.NamingStylePreferences:
                        optionValue = NamingStylePreferences.FromXElement(XElement.Parse((string)readValue));
                        break;

                    case OptionValueKind.Enum:
                        optionValue = Enum.ToObject(optionKey.Option.Type, readValue);
                        break;

                    case OptionValueKind.Null:
                        optionValue = null;
                        break;

                    default:
                        optionValue = readValue;
                        break;
                }

                builder[optionKey] = optionValue;
            }

            Interlocked.Exchange(ref _serializableOptionValues, builder.ToImmutable());
        }

        private enum OptionValueKind
        {
            Null,
            CodeStyleOption,
            NamingStylePreferences,
            Serializable,
            String,
            Enum
        }

        private sealed class OptionKeyComparer : IComparer<OptionKey>
        {
            public static readonly OptionKeyComparer Instance = new OptionKeyComparer();
            private OptionKeyComparer() { }

            public int Compare(OptionKey x, OptionKey y)
            {
                if (x.Option.Name != y.Option.Name)
                {
                    return StringComparer.Ordinal.Compare(x.Option.Name, y.Option.Name);
                }

                if (x.Option.Feature != y.Option.Feature)
                {
                    return StringComparer.Ordinal.Compare(x.Option.Feature, y.Option.Feature);
                }

                if (x.Language != y.Language)
                {
                    return StringComparer.Ordinal.Compare(x.Language, y.Language);
                }

                return Comparer.Default.Compare(x.GetHashCode(), y.GetHashCode());
            }
        }
    }
}
