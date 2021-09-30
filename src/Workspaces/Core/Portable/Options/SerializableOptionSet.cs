// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
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
        /// Fallback option set for non-serializable options. See comments on <see cref="WorkspaceOptionSet"/> for more details.
        /// </summary>
        private readonly WorkspaceOptionSet _workspaceOptionSet;

        /// <summary>
        /// Prefetched option values applicable for <see cref="_languages"/>.
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

        /// <summary>
        /// Set of languages referenced in <see cref="_serializableOptionValues"/>.  Cached
        /// only so we can shortcircuit <see cref="UnionWithLanguages"/>.
        /// </summary>
        private readonly Lazy<ImmutableHashSet<string>> _languages;

        private SerializableOptionSet(
            WorkspaceOptionSet workspaceOptionSet,
            ImmutableDictionary<OptionKey, object?> values,
            ImmutableHashSet<OptionKey> changedOptionKeysSerializable,
            ImmutableHashSet<OptionKey> changedOptionKeysNonSerializable)
        {
            _workspaceOptionSet = workspaceOptionSet;
            _serializableOptionValues = values;
            _changedOptionKeysSerializable = changedOptionKeysSerializable;
            _changedOptionKeysNonSerializable = changedOptionKeysNonSerializable;

            Debug.Assert(values.Keys.All(ShouldSerialize));
            Debug.Assert(changedOptionKeysSerializable.All(optionKey => ShouldSerialize(optionKey)));
            Debug.Assert(changedOptionKeysNonSerializable.All(optionKey => !ShouldSerialize(optionKey)));

            _languages = new Lazy<ImmutableHashSet<string>>(() => this.GetLanguagesAndValuesToSerialize(includeValues: false).languages);
        }

        internal SerializableOptionSet(
            IOptionService optionService,
            ImmutableDictionary<OptionKey, object?> values,
            ImmutableHashSet<OptionKey> changedOptionKeysSerializable)
            : this(new WorkspaceOptionSet(optionService), values, changedOptionKeysSerializable, changedOptionKeysNonSerializable: ImmutableHashSet<OptionKey>.Empty)
        {
        }

        /// <summary>
        /// Returns an option set with all the serializable option values prefetched for given <paramref name="languages"/>,
        /// while also retaining all the explicitly changed option values in this option set for any language.
        /// Note: All the provided <paramref name="languages"/> must be <see cref="RemoteSupportedLanguages.IsSupported(string)"/>.
        /// </summary>
        public SerializableOptionSet UnionWithLanguages(ImmutableHashSet<string> languages)
        {
            Debug.Assert(languages.All(RemoteSupportedLanguages.IsSupported));

            if (_languages.Value.IsSupersetOf(languages))
                return this;

            // First create a base option set for the given languages.
            languages = languages.Union(_languages.Value);
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
            => _serializableOptionValues.ContainsKey(optionKey) &&
               (!optionKey.Option.IsPerLanguage || RemoteSupportedLanguages.IsSupported(optionKey.Language));

        public override OptionSet WithChangedOption(OptionKey optionKey, object? value)
        {
            // Make sure we first load this in current optionset
            var currentValue = this.GetOption(optionKey);

            // Check if the new value is the same as the current value.
            if (Equals(value, currentValue))
            {
                // Return a cloned option set as the public API 'WithChangedOption' guarantees a new option set is returned.
                return new SerializableOptionSet(
                    _workspaceOptionSet, _serializableOptionValues, _changedOptionKeysSerializable, _changedOptionKeysNonSerializable);
            }

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

            return new SerializableOptionSet(
                workspaceOptionSet, serializableOptionValues, changedOptionKeysSerializable, changedOptionKeysNonSerializable);
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

        private (ImmutableHashSet<string> languages, SortedDictionary<OptionKey, (OptionValueKind, object?)> values) GetLanguagesAndValuesToSerialize(bool includeValues)
        {
            var valuesBuilder = new SortedDictionary<OptionKey, (OptionValueKind, object?)>(OptionKeyComparer.Instance);
            var languages = ImmutableHashSet<string>.Empty;

            foreach (var (optionKey, value) in _serializableOptionValues)
            {
                Debug.Assert(ShouldSerialize(optionKey));

                Debug.Assert(!optionKey.Option.IsPerLanguage || RemoteSupportedLanguages.IsSupported(optionKey.Language));
                if (optionKey.Language != null)
                    languages = languages.Add(optionKey.Language);

                if (includeValues)
                {
                    OptionValueKind kind;
                    switch (value)
                    {
                        case ICodeStyleOption:
                            if (optionKey.Option.Type.GenericTypeArguments.Length != 1)
                                continue;

                            kind = OptionValueKind.CodeStyleOption;
                            break;

                        case NamingStylePreferences:
                            kind = OptionValueKind.NamingStylePreferences;
                            break;

                        default:
                            kind = value != null && value.GetType().IsEnum ? OptionValueKind.Enum : OptionValueKind.Object;
                            break;
                    }

                    valuesBuilder.Add(optionKey, (kind, value));
                }
            }

            return (languages, valuesBuilder);
        }

        public string GetDebugString()
        {
            // NOTE: keep this in sync with Serialize below.

            using var _ = PooledStringBuilder.GetInstance(out var sb);

            var (languages, values) = this.GetLanguagesAndValuesToSerialize(includeValues: true);

            sb.AppendLine($"languages count: {languages.Count}");
            foreach (var language in languages.Order())
            {
                Debug.Assert(RemoteSupportedLanguages.IsSupported(language));
                sb.AppendLine(language);
            }

            sb.AppendLine();
            sb.AppendLine($"values count: {values.Count}");
            foreach (var (optionKey, (kind, value)) in values)
            {
                SerializeOptionKey(optionKey);

                sb.Append($"{kind}: ");
                if (kind == OptionValueKind.Enum)
                {
                    RoslynDebug.Assert(value != null);
                    sb.AppendLine(value.ToString());
                }
                else if (kind is OptionValueKind.CodeStyleOption)
                {
                    RoslynDebug.Assert(value != null);
                    var codeStyleOption = (ICodeStyleOption)value;
                    sb.AppendLine(codeStyleOption.ToXElement().ToString());
                }
                else if (kind is OptionValueKind.NamingStylePreferences)
                {
                    RoslynDebug.Assert(value != null);
                    var namingStylePreferences = (NamingStylePreferences)value;
                    sb.AppendLine(namingStylePreferences.CreateXElement().ToString());
                }
                else
                {
                    sb.AppendLine($"{value}");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine($"changed options count: {_changedOptionKeysSerializable.Count}");
            foreach (var changedKey in _changedOptionKeysSerializable.OrderBy(OptionKeyComparer.Instance))
                SerializeOptionKey(changedKey);

            return sb.ToString();

            void SerializeOptionKey(OptionKey optionKey)
            {
                Debug.Assert(ShouldSerialize(optionKey));

                sb.AppendLine($"{optionKey.Option.Name} {optionKey.Option.Feature} {optionKey.Option.IsPerLanguage} {optionKey.Language}");
            }
        }

        public void Serialize(ObjectWriter writer, CancellationToken cancellationToken)
        {
            // We serialize the following contents from this option set:
            //  1. Languages
            //  2. Prefetched serializable option key-value pairs
            //  3. Changed option keys.

            // NOTE: keep the serialization in sync with Deserialize method below.
            // NOTE: keep this in sync with GetDebugString above.

            cancellationToken.ThrowIfCancellationRequested();

            var (languages, values) = this.GetLanguagesAndValuesToSerialize(includeValues: true);

            writer.WriteInt32(languages.Count);
            foreach (var language in languages.Order())
            {
                Debug.Assert(RemoteSupportedLanguages.IsSupported(language));
                writer.WriteString(language);
            }

            writer.WriteInt32(values.Count);
            foreach (var (optionKey, (kind, value)) in values)
            {
                SerializeOptionKey(optionKey);

                writer.WriteInt32((int)kind);
                if (kind == OptionValueKind.Enum)
                {
                    RoslynDebug.Assert(value != null);
                    writer.WriteInt32((int)value);
                }
                else if (kind is OptionValueKind.CodeStyleOption or OptionValueKind.NamingStylePreferences)
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
                SerializeOptionKey(changedKey);

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
                var optionKeyOpt = TryDeserializeOptionKey(reader, lookup);

                var kind = (OptionValueKind)reader.ReadInt32();
                var readValue = kind switch
                {
                    OptionValueKind.Enum => reader.ReadInt32(),
                    OptionValueKind.CodeStyleOption => CodeStyleOption2<object>.ReadFrom(reader),
                    OptionValueKind.NamingStylePreferences => NamingStylePreferences.ReadFrom(reader),
                    _ => reader.ReadValue(),
                };

                if (optionKeyOpt == null)
                    continue;

                var optionKey = optionKeyOpt.Value;
                if (!serializableOptions.Contains(optionKey.Option))
                    continue;

                object? optionValue;
                switch (kind)
                {
                    case OptionValueKind.CodeStyleOption:
                        if (optionKey.Option.DefaultValue is not ICodeStyleOption defaultValue ||
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
                        var enumType = optionKey.Option.Type;
                        if (enumType.IsGenericType && enumType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            enumType = enumType.GetGenericArguments()[0];
                        }

                        optionValue = Enum.ToObject(enumType, readValue);
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
                if (TryDeserializeOptionKey(reader, lookup) is { } optionKey)
                    changedKeysBuilder.Add(optionKey);
            }

            var serializableOptionValues = builder.ToImmutable();
            var changedOptionKeysSerializable = changedKeysBuilder.ToImmutable();
            var workspaceOptionSet = new WorkspaceOptionSet(optionService);

            return new SerializableOptionSet(
                workspaceOptionSet, serializableOptionValues, changedOptionKeysSerializable,
                changedOptionKeysNonSerializable: ImmutableHashSet<OptionKey>.Empty);

            static OptionKey? TryDeserializeOptionKey(ObjectReader reader, ILookup<string, IOption> lookup)
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
                        return new OptionKey(option, language);
                    }
                }

                Debug.Fail($"Failed to deserialize: {name}-{feature}-{isPerLanguage}-{language}");
                return null;
            }
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public struct TestAccessor
        {
            private readonly SerializableOptionSet _serializableOptionSet;

            public TestAccessor(SerializableOptionSet serializableOptionSet)
            {
                _serializableOptionSet = serializableOptionSet;
            }

            public ImmutableHashSet<string> Languages
                => _serializableOptionSet.GetLanguagesAndValuesToSerialize(includeValues: true).languages;
        }

        private enum OptionValueKind
        {
            CodeStyleOption,
            NamingStylePreferences,
            Object,
            Enum
        }

        private sealed class OptionKeyComparer : IComparer<OptionKey>
        {
            public static readonly OptionKeyComparer Instance = new();
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
