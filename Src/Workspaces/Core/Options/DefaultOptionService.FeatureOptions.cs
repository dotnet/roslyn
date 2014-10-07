using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Roslyn.Services.OptionService
{
    internal partial class DefaultOptionService
    {
        /// <summary>
        /// provide options for a specific feature and support a way to save/load them to/from a repository
        /// </summary>
        private class FeatureOptions : IFeatureOptions
        {
            private readonly OptionMigrationService migrationService;

            private readonly Lazy<IOptionProvider, IOptionProviderMetadata> defaultOptions;
            private readonly ConcurrentDictionary<string, object> featureOptionsMap;

            public FeatureOptions(
                OptionMigrationService migrationService,
                Lazy<IOptionProvider, IOptionProviderMetadata> defaultOptions,
                Persistence currentOptionsPersistenceStorage)
            {
                Contract.ThrowIfNull(migrationService);
                Contract.ThrowIfNull(defaultOptions);

                this.migrationService = migrationService;

                this.defaultOptions = defaultOptions;

                this.featureOptionsMap = new ConcurrentDictionary<string, object>();

                // if persistence storage for current saved option, use it to set current option values
                if (currentOptionsPersistenceStorage != null)
                {
                    LoadOptionsFrom(currentOptionsPersistenceStorage);
                }
            }

            public string Feature
            {
                get { return this.defaultOptions.Metadata.Name; }
            }

            public IEnumerable<OptionDescription> GetAllOptionDescriptions()
            {
                return this.defaultOptions.Value.GetAllOptionDescriptions();
            }

            public T GetOption<T>(OptionKey<T> key)
            {
                Contract.ThrowIfFalse(key.Feature.Equals(this.Feature));
                Contract.ThrowIfTrue(string.IsNullOrEmpty(key.Name));

                object value;

                // see whether we have a value different than default one
                if (this.featureOptionsMap.TryGetValue(key.Name, out value))
                {
                    Contract.ThrowIfNull(value);

                    if (value is T)
                    {
                        return (T)value;
                    }

                    throw new ArgumentException(
                        string.Format(
                            "type of the option is invalid - given type {0}, expected type {1}",
                            typeof(T).ToString(),
                            value.GetType().ToString()));
                }

                // give out default value
                return this.defaultOptions.Value.GetOptionDefaultValue(key);
            }

            public T SetOption<T>(OptionKey<T> key, T value)
            {
                Contract.ThrowIfFalse(key.Feature.Equals(this.Feature));
                Contract.ThrowIfTrue(string.IsNullOrEmpty(key.Name));

                var defaultValue = this.defaultOptions.Value.GetOptionDefaultValue(key);
                var canCheckType = typeof(T).IsValueType || defaultValue != null;

                if (canCheckType && !(defaultValue is T))
                {
                    throw new ArgumentException(
                        string.Format(
                            "type of the option is invalid - given type {0}, expected type {1}",
                            typeof(T).ToString(),
                            defaultValue.GetType().ToString()));
                }

                // if given value is same as default one, remove local copy
                if (object.Equals(value, defaultValue))
                {
                    object tempValue;
                    this.featureOptionsMap.TryRemove(key.Name, out tempValue);
                    return value;
                }

                this.featureOptionsMap[key.Name] = value;
                return value;
            }

            private void LoadOptionsFrom(Persistence repository)
            {
                Contract.ThrowIfNull(repository);

                Version version;
                string data;
                if (!repository.TryLoadOptions(this.Feature, out version, out data))
                {
                    // no saved information. use default options
                    return;
                }

                // okay, now check whether it gives us same version of options or not
                var featureVersion = new Version(this.defaultOptions.Metadata.Version);
                if (!featureVersion.Equals(version))
                {
                    // TODO : when error happens, how to notify error to user?
                    data = this.migrationService.Migrate(version, featureVersion, this.Feature, data);
                    if (data == null)
                    {
                        // use default options
                        return;
                    }
                }

                // load saved information
                var serializer = Activator.CreateInstance(this.defaultOptions.Metadata.FeatureOptionsSerializer) as IOptionSerializer;
                Contract.ThrowIfNull(serializer);

                foreach (var nameValuePairOptions in serializer.Deserialize(data))
                {
                    // update saved information
                    this.featureOptionsMap.AddOrUpdate(nameValuePairOptions.Key, nameValuePairOptions.Value, (key, value) => nameValuePairOptions.Value);
                }
            }

            public void SaveOptionsTo(Persistence repository)
            {
                Contract.ThrowIfNull(repository);

                if (this.featureOptionsMap.IsEmpty)
                {
                    return;
                }

                var featureVersion = new Version(this.defaultOptions.Metadata.Version);
                var serializer = Activator.CreateInstance(this.defaultOptions.Metadata.FeatureOptionsSerializer) as IOptionSerializer;
                Contract.ThrowIfNull(serializer);

                var data = serializer.Serialize(this.featureOptionsMap.ToArray());
                repository.SaveOption(this.Feature, featureVersion, data);
            }

            public void ResetOption<T>(OptionKey<T> key)
            {
                Contract.ThrowIfFalse(key.Feature.Equals(this.Feature));
                Contract.ThrowIfTrue(string.IsNullOrEmpty(key.Name));

                object temp;
                this.featureOptionsMap.TryRemove(key.Name, out temp);
            }

            public void ResetOptions()
            {
                // remove all options
                this.featureOptionsMap.Clear();
            }
        }
    }
}
