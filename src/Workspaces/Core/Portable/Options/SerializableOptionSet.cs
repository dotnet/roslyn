// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Serializable implementation of <see cref="OptionSet"/> for <see cref="Solution.Options"/>.
    /// It contains prepopulated fetched option values for all serializable options and values, and delegates to <see cref="WorkspaceOptionSet"/> for non-serializable values.
    /// It ensures a contract that values are immutable from this instance once observed.
    /// </summary>
    internal sealed partial class SerializableOptionSet : OptionSet
    {
        /// <summary>
        /// Languages for which all the applicable serializable options have been prefetched and saved in <see cref="_serializableOptionValues"/>.
        /// </summary>
        private readonly ImmutableHashSet<string> _languages;

        /// <summary>
        /// Fallback option set for non-serializable options. See comments on <see cref="WorkspaceOptionSet"/> for more details.
        /// </summary>
        private readonly WorkspaceOptionSet _workspaceOptionSet;

        /// <summary>
        /// All serializable options for <see cref="_languages"/>.
        /// </summary>
        private readonly ImmutableHashSet<IOption> _serializableOptions;

        /// <summary>
        /// Prefetched option values for all <see cref="_serializableOptions"/> applicable for <see cref="_languages"/>.
        /// </summary>
        private readonly ImmutableDictionary<OptionKey, object?> _serializableOptionValues;

        /// <summary>
        /// Set of changed options in this option set which are serializable.
        /// </summary>
        private readonly ImmutableHashSet<OptionKey> _changedOptionKeysSerializable;

        /// <summary>
        /// Set of changed options in this option set which are non-serializable.
        /// </summary>
        private readonly ImmutableHashSet<OptionKey> _changedOptionKeysNonSerializable;

        private SerializableOptionSet(
            ImmutableHashSet<string> languages,
            WorkspaceOptionSet workspaceOptionSet,
            ImmutableHashSet<IOption> serializableOptions,
            ImmutableDictionary<OptionKey, object?> values,
            ImmutableHashSet<OptionKey> changedOptionKeysSerializable,
            ImmutableHashSet<OptionKey> changedOptionKeysNonSerializable)
        {
            Debug.Assert(languages.All(RemoteSupportedLanguages.IsSupported));

            _languages = languages;
            _workspaceOptionSet = workspaceOptionSet;
            _serializableOptions = serializableOptions;
            _serializableOptionValues = values;
            _changedOptionKeysSerializable = changedOptionKeysSerializable;
            _changedOptionKeysNonSerializable = changedOptionKeysNonSerializable;

            Debug.Assert(values.Keys.All(ShouldSerialize));
            Debug.Assert(changedOptionKeysSerializable.All(optionKey => ShouldSerialize(optionKey)));
            Debug.Assert(changedOptionKeysNonSerializable.All(optionKey => !ShouldSerialize(optionKey)));
        }

        internal SerializableOptionSet(
            ImmutableHashSet<string> languages,
            IOptionService optionService,
            ImmutableHashSet<IOption> serializableOptions,
            ImmutableDictionary<OptionKey, object?> values,
            ImmutableHashSet<OptionKey> changedOptionKeysSerializable)
            : this(languages, new WorkspaceOptionSet(optionService), serializableOptions, values, changedOptionKeysSerializable, changedOptionKeysNonSerializable: ImmutableHashSet<OptionKey>.Empty)
        {
        }

        /// <summary>
        /// Returns an option set with all the serializable option values prefetched for given <paramref name="languages"/>,
        /// while also retaining all the explicitly changed option values in this option set for any language.
        /// NOTE: All the provided <paramref name="languages"/> must be <see cref="RemoteSupportedLanguages.IsSupported(string)"/>.
        /// </summary>
        public SerializableOptionSet WithLanguages(ImmutableHashSet<string> languages)
        {
            Debug.Assert(languages.All(RemoteSupportedLanguages.IsSupported));
            if (_languages.SetEquals(languages))
            {
                return this;
            }

            // First create a base option set for the given languages.
            var newOptionSet = _workspaceOptionSet.OptionService.GetSerializableOptionsSnapshot(languages);

            // Then apply all the changed options from the current option set to the new option set.
            foreach (var changedOption in this.GetChangedOptions())
            {
                var valueInNewOptionSet = newOptionSet.GetOption(changedOption);
                var changedValueInThisOptionSet = this.GetOption(changedOption);

                if (!Equals(changedValueInThisOptionSet, valueInNewOptionSet))
                {
                    newOptionSet = (SerializableOptionSet)newOptionSet.WithChangedOption(changedOption, changedValueInThisOptionSet);
                }
            }

            return newOptionSet;
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
        private protected override object? GetOptionCore(OptionKey optionKey)
        {
            if (_serializableOptionValues.TryGetValue(optionKey, out var value))
            {
                return value;
            }

            return _workspaceOptionSet.GetOption(optionKey);
        }

        private bool ShouldSerialize(OptionKey optionKey)
            => _serializableOptions.Contains(optionKey.Option) &&
               (!optionKey.Option.IsPerLanguage || _languages.Contains(optionKey.Language!));

        public override OptionSet WithChangedOption(OptionKey optionKey, object? value)
        {
            // make sure we first load this in current optionset
            this.GetOption(optionKey);

            WorkspaceOptionSet workspaceOptionSet;
            ImmutableDictionary<OptionKey, object?> serializableOptionValues;
            ImmutableHashSet<OptionKey> changedOptionKeysSerializable;
            ImmutableHashSet<OptionKey> changedOptionKeysNonSerializable;
            if (ShouldSerialize(optionKey))
            {
                workspaceOptionSet = _workspaceOptionSet;
                serializableOptionValues = _serializableOptionValues.SetItem(optionKey, value);
                changedOptionKeysSerializable = _changedOptionKeysSerializable.Add(optionKey);
                changedOptionKeysNonSerializable = _changedOptionKeysNonSerializable;
            }
            else
            {
                workspaceOptionSet = (WorkspaceOptionSet)_workspaceOptionSet.WithChangedOption(optionKey, value);
                serializableOptionValues = _serializableOptionValues;
                changedOptionKeysSerializable = _changedOptionKeysSerializable;
                changedOptionKeysNonSerializable = _changedOptionKeysNonSerializable.Add(optionKey);
            }

            return new SerializableOptionSet(_languages, workspaceOptionSet, _serializableOptions,
                serializableOptionValues, changedOptionKeysSerializable, changedOptionKeysNonSerializable);
        }

        /// <summary>
        /// Gets a list of all the options that were changed.
        /// </summary>
        internal IEnumerable<OptionKey> GetChangedOptions()
            => _changedOptionKeysSerializable.Concat(_changedOptionKeysNonSerializable);

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet? optionSet)
        {
            if (optionSet == this)
            {
                yield break;
            }

            foreach (var key in GetChangedOptions())
            {
                var currentValue = optionSet?.GetOption(key);
                var changedValue = this.GetOption(key);
                if (!object.Equals(currentValue, changedValue))
                {
                    yield return key;
                }
            }
        }

        public void Serialize(ObjectWriter writer, CancellationToken cancellationToken)
        {
            // We serialize the following contents from this option set:
            //  1. Languages
            //  2. Prefetched serializable option key-value pairs
            //  3. Changed option keys.

            // NOTE: keep the serialization in sync with Deserialize method below.

            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32(_languages.Count);
            foreach (var language in _languages.Order())
            {
                Debug.Assert(RemoteSupportedLanguages.IsSupported(language));
                writer.WriteString(language);
            }

            var valuesBuilder = new SortedDictionary<OptionKey, (OptionValueKind, object?)>(OptionKeyComparer.Instance);
            foreach (var (optionKey, value) in _serializableOptionValues)
            {
                Debug.Assert(ShouldSerialize(optionKey));

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
                            valueToWrite = codeStyleOption;
                            break;

                        case NamingStylePreferences stylePreferences:
                            kind = OptionValueKind.NamingStylePreferences;
                            valueToWrite = stylePreferences;
                            break;

                        case string str:
                            kind = OptionValueKind.String;
                            valueToWrite = str;
                            break;

                        default:
                            var type = value.GetType();
                            if (type.IsEnum)
                            {
                                kind = OptionValueKind.Enum;
                                valueToWrite = (int)value;
                                break;
                            }

                            if (optionKey.Option.Type.IsSerializable)
                            {
                                kind = OptionValueKind.Serializable;
                                valueToWrite = value;
                                break;
                            }

                            continue;
                    }
                }

                valuesBuilder.Add(optionKey, (kind, valueToWrite));
            }

            writer.WriteInt32(valuesBuilder.Count);
            foreach (var (optionKey, (kind, value)) in valuesBuilder)
            {
                SerializeOptionKey(optionKey);

                writer.WriteInt32((int)kind);
                if (kind == OptionValueKind.Enum)
                {
                    RoslynDebug.Assert(value != null);
                    writer.WriteInt32((int)value);
                }
                else if (kind == OptionValueKind.CodeStyleOption || kind == OptionValueKind.NamingStylePreferences)
                {
                    RoslynDebug.Assert(value != null);
                    ((IObjectWritable)value).WriteTo(writer);
                }
                else
                {
                    writer.WriteValue(value);
                }
            }

            writer.WriteInt32(_changedOptionKeysSerializable.Count);
            foreach (var changedKey in _changedOptionKeysSerializable.OrderBy(OptionKeyComparer.Instance))
            {
                SerializeOptionKey(changedKey);
            }

            return;

            void SerializeOptionKey(OptionKey optionKey)
            {
                Debug.Assert(ShouldSerialize(optionKey));

                writer.WriteString(optionKey.Option.Name);
                writer.WriteString(optionKey.Option.Feature);
                writer.WriteBoolean(optionKey.Option.IsPerLanguage);
                if (optionKey.Option.IsPerLanguage)
                {
                    writer.WriteString(optionKey.Language);
                }
            }
        }

        public static SerializableOptionSet Deserialize(ObjectReader reader, IOptionService optionService, CancellationToken cancellationToken)
        {
            // We deserialize the following contents from this option set:
            //  1. Languages
            //  2. Prefetched serializable option key-value pairs
            //  3. Changed option keys.

            // NOTE: keep the deserialization in sync with Serialize method above.

            cancellationToken.ThrowIfCancellationRequested();

            var count = reader.ReadInt32();
            var languagesBuilder = ImmutableHashSet.CreateBuilder<string>();
            for (var i = 0; i < count; i++)
            {
                var language = reader.ReadString();
                Debug.Assert(RemoteSupportedLanguages.IsSupported(language));
                languagesBuilder.Add(language);
            }

            var languages = languagesBuilder.ToImmutable();

            var serializableOptions = optionService.GetRegisteredSerializableOptions(languages);
            var lookup = serializableOptions.ToLookup(o => o.Name);

            count = reader.ReadInt32();
            var builder = ImmutableDictionary.CreateBuilder<OptionKey, object?>();
            for (var i = 0; i < count; i++)
            {
                if (!TryDeserializeOptionKey(reader, lookup, out var optionKey))
                {
                    continue;
                }

                var kind = (OptionValueKind)reader.ReadInt32();
                var readValue = kind switch
                {
                    OptionValueKind.Enum => reader.ReadInt32(),
                    OptionValueKind.CodeStyleOption => CodeStyleOption2<object>.ReadFrom(reader),
                    OptionValueKind.NamingStylePreferences => NamingStylePreferences.ReadFrom(reader),
                    _ => reader.ReadValue(),
                };

                if (!serializableOptions.Contains(optionKey.Option))
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

                        var parsedCodeStyleOption = (CodeStyleOption2<object>)readValue;
                        var value = parsedCodeStyleOption.Value;
                        var type = optionKey.Option.Type.GenericTypeArguments[0];
                        var convertedValue = type.IsEnum ? Enum.ToObject(type, value) : Convert.ChangeType(value, type);
                        optionValue = defaultValue.WithValue(convertedValue).WithNotification(parsedCodeStyleOption.Notification);
                        break;

                    case OptionValueKind.NamingStylePreferences:
                        optionValue = (NamingStylePreferences)readValue;
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

            count = reader.ReadInt32();
            var changedKeysBuilder = ImmutableHashSet.CreateBuilder<OptionKey>();
            for (var i = 0; i < count; i++)
            {
                if (TryDeserializeOptionKey(reader, lookup, out var optionKey))
                {
                    changedKeysBuilder.Add(optionKey);
                }
            }

            var serializableOptionValues = builder.ToImmutable();
            var changedOptionKeysSerializable = changedKeysBuilder.ToImmutable();
            var workspaceOptionSet = new WorkspaceOptionSet(optionService);

            return new SerializableOptionSet(languages, workspaceOptionSet, serializableOptions, serializableOptionValues,
                changedOptionKeysSerializable, changedOptionKeysNonSerializable: ImmutableHashSet<OptionKey>.Empty);

            static bool TryDeserializeOptionKey(ObjectReader reader, ILookup<string, IOption> lookup, out OptionKey deserializedOptionKey)
            {
                var name = reader.ReadString();
                var feature = reader.ReadString();
                var isPerLanguage = reader.ReadBoolean();
                var language = isPerLanguage ? reader.ReadString() : null;

                foreach (var option in lookup[name])
                {
                    if (option.Feature == feature &&
                        option.IsPerLanguage == isPerLanguage)
                    {
                        deserializedOptionKey = new OptionKey(option, language);
                        return true;
                    }
                }

                deserializedOptionKey = default;
                return false;
            }
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
