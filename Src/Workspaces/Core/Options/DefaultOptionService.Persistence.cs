using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Roslyn.Services.Persistence;
using Roslyn.Utilities;

namespace Roslyn.Services.OptionService
{
    internal partial class DefaultOptionService
    {
        /// <summary>
        /// repository takes care of persisting options in the option service
        /// </summary>
        private class Persistence
        {
            // constants
            private const string PersistenceName = "FeatureOptions";
            private const string ConfigurationString = "Configuration";
            private const string FeatureString = "Feature";
            private const string VersionString = "Version";
            private const string NameString = "Name";

            // current configuration file version
            private static readonly Version ConfigurationFileVersion = new Version("1.1");

            // lock object
            private readonly NonReentrantLock syncRoot = new NonReentrantLock();

            // options state
            private readonly Dictionary<string, ValueTuple<Version, string>> optionsAtStartUp;
            private readonly Dictionary<string, ValueTuple<Version, string>> optionsChangedFromDefaults;

            public Persistence()
            {
                this.optionsChangedFromDefaults = new Dictionary<string, ValueTuple<Version, string>>();
                this.optionsAtStartUp = new Dictionary<string, ValueTuple<Version, string>>();

                Workspace.GetPrimaryWorkspaceAsync().SafeContinueWith(t => LoadAllAsync(t.Result), TaskScheduler.Default);
            }

            public void SaveOption(string feature, Version version, string data)
            {
                Contract.ThrowIfNull(feature);
                Contract.ThrowIfNull(version);
                Contract.ThrowIfNull(data);

                using (this.syncRoot.DisposableWait())
                {
                    this.optionsChangedFromDefaults[feature] = ValueTuple.Create(version, data);
                }
            }

            public bool TryLoadOptions(string feature, out Version version, out string data)
            {
                Contract.ThrowIfNull(feature);

                // initialize
                version = default(Version);
                data = default(string);

                ValueTuple<Version, string> value;
                if (!this.optionsAtStartUp.TryGetValue(feature, out value))
                {
                    // no loaded options at start up
                    return false;
                }

                // set return value
                version = value.Item1;
                data = value.Item2;

                return true;
            }

            private IPersistenceService GetPersistenceService(IWorkspace workspace)
            {
                return workspace.WorkspaceServices.GetService<IPersistenceService>();
            }

            /// <summary>
            /// persist current states to storage
            /// </summary>
            public async Task PersistAsync(CancellationToken cancellationToken)
            {
                using (this.syncRoot.DisposableWait())
                {
                    // okay, before actually saving things to the file, check whether options are different than the one
                    // we read at start up
                    if (this.optionsAtStartUp.DictionaryEquals(this.optionsChangedFromDefaults))
                    {
                        return;
                    }

                    // if nothing is changed from default, don't bother to save any thing
                    if (this.optionsChangedFromDefaults.Count <= 0 && this.optionsAtStartUp.Count <= 0)
                    {
                        return;
                    }

                    var workspace = Roslyn.Services.Workspace.PrimaryWorkspace;
                    if (workspace == null)
                    {
                        return;
                    }

                    // try save the option. if failed, just ignore
                    var stream = new MemoryStream();
                    var xml = CreateXmlDocumentFromOptions();
                    xml.Save(stream, SaveOptions.None);
                    stream = new MemoryStream(stream.ToArray());

                    var persistenceService = GetPersistenceService(workspace);
                    await persistenceService.WriteStreamAsync(PersistenceName, stream, cancellationToken).ConfigureAwait(false);
                }
            }

            private XDocument CreateXmlDocumentFromOptions()
            {
                var xmlConfiguration = new XElement(ConfigurationString, new XAttribute(VersionString, ConfigurationFileVersion));
                var xmlDocument = new XDocument(xmlConfiguration);

                foreach (var pair in this.optionsChangedFromDefaults)
                {
                    var xmlElement = new XElement(
                        FeatureString,
                        new XAttribute(NameString, pair.Key),
                        new XAttribute(VersionString, pair.Value.Item1.ToString()),
                        new XCData(pair.Value.Item2));

                    xmlConfiguration.Add(xmlElement);
                }

                return xmlDocument;
            }

            private async Task LoadAllAsync(IWorkspace workspace)
            {
                Contract.ThrowIfNull(workspace);

                // for now, we will load all saved options at start up and keep it in memory
                using (this.syncRoot.DisposableWait(CancellationToken.None))
                {
                    // load it from persistence service
                    var persistenceService = GetPersistenceService(workspace);
                    using (var stream = await persistenceService.ReadStreamAsync(PersistenceName, CancellationToken.None).ConfigureAwait(false))
                    {
                        if (stream != null)
                        {
                            try
                            {
                                var xmlDocument = XElement.Load(new XmlTextReader(stream));

                                if (!ConfigurationString.Equals(xmlDocument.Name.LocalName) ||
                                    !ConfigurationFileVersion.Equals(new Version(xmlDocument.Attribute(VersionString).Value)))
                                {
                                    return;
                                }

                                var featureVersionDataPairs = from element in xmlDocument.Elements(FeatureString)
                                                                select GetFeatureOptions(element);

                                foreach (var tuple in featureVersionDataPairs)
                                {
                                    this.optionsAtStartUp.Add(tuple.Item1, ValueTuple.Create(tuple.Item2, tuple.Item3));
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }

            private ValueTuple<string, Version, string> GetFeatureOptions(XElement element)
            {
                var feature = element.Attribute(NameString).Value;
                var versionString = element.Attribute(VersionString).Value;
                var version = new Version(versionString);

                Contract.Requires(feature != null);
                Contract.Requires(versionString != null);
                Contract.Requires(version != null);

                var data = element.Value;
                return ValueTuple.Create(feature, version, data);
            }
        }
    }
}
