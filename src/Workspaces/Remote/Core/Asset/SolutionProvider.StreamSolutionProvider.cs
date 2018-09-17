using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Shared;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Roslyn.Assets
{
    public static partial class SolutionProvider
    {
        private class StreamSolutionProvider : ISolutionProvider
        {
            private readonly Stream _stream;
            private readonly ImmutableArray<Assembly> _hostAssemblies;

            public StreamSolutionProvider(Stream stream, IEnumerable<Assembly> hostAssemblies)
            {
                _stream = stream;
                _hostAssemblies = hostAssemblies.ToImmutableArray();
            }

            public async Task<Solution> CreateSolutionAsync(CancellationToken cancellationToken)
            {
                var adhocWorkspace = new AdhocWorkspace(MefHostServices.Create(ExternalHostAssemblies.Concat(_hostAssemblies).Distinct()));
                var serializer = adhocWorkspace.Services.GetService<ISerializerService>();

                var solutionChecksum = default(Checksum);
                var assetMap = new Dictionary<Checksum, object>();
                var idMap = new Dictionary<(string, string), ProjectOrDocumentId>();

                using (var compressed = new DeflateStream(_stream, CompressionMode.Decompress))
                using (var reader = ObjectReader.TryGetReader(compressed, cancellationToken))
                {
                    var idMapSupport = reader.ReadBoolean();

                    solutionChecksum = ReadAssetMap(serializer, reader, assetMap, cancellationToken);

                    if (idMapSupport)
                    {
                        ReadIdMap(reader, idMap);
                    }
                }

                var assetSource = new SimpleAssetSource(AssetStorage.Default, assetMap);
                var assetService = new AssetService(AssetStorage.Default, serializer);

                var solutionCreator = new SolutionCreator(assetService, adhocWorkspace.CurrentSolution, cancellationToken);

                // check whether solution is update to the given base solution
                if (await solutionCreator.IsIncrementalUpdateAsync(solutionChecksum).ConfigureAwait(false))
                {
                    // create updated solution off the baseSolution
                    return await solutionCreator.CreateSolutionAsync(solutionChecksum).ConfigureAwait(false);
                }

                // get new solution info
                var solutionInfo = await solutionCreator.CreateSolutionInfoAsync(solutionChecksum).ConfigureAwait(false);

                // otherwise, just return new solution
                return adhocWorkspace.AddSolution(solutionInfo);
            }

            private static Checksum ReadAssetMap(ISerializerService serializer, ObjectReader reader, Dictionary<Checksum, object> map, CancellationToken cancellationToken)
            {
                // save root checksum and number of entries
                var solutionChecksum = Checksum.ReadFrom(reader);

                // number of items in the package
                var count = reader.ReadInt32();

                for (var i = 0; i < count; i++)
                {
                    var itemChecksum = Checksum.ReadFrom(reader);

                    var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                    // in service hub, cancellation means simply closed stream
                    var @object = serializer.Deserialize<object>(kind, reader, cancellationToken);

                    map.Add(itemChecksum, @object);
                }

                return solutionChecksum;
            }

            private void ReadIdMap(ObjectReader reader, Dictionary<(string, string), ProjectOrDocumentId> idMap)
            {
                var count = reader.ReadInt32();

                for (var i = 0; i < count; i++)
                {
                    var projectKey = reader.ReadString();
                    var documentKey = reader.ReadString();
                    var projectOrDocumentId = ProjectOrDocumentId.ReadFrom(reader);

                    idMap.Add((projectKey, documentKey), projectOrDocumentId);
                }
            }
        }
    }
}
