using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Roslyn.Utilities;

namespace Roslyn.Services.OptionService
{
    internal class OptionMigrationService
    {
        private readonly IEnumerable<Lazy<IOptionMigrator, IOptionMigratorMetadata>> featureOptionsMigrators;

        public OptionMigrationService(IEnumerable<Lazy<IOptionMigrator, IOptionMigratorMetadata>> featureOptionsMigrators)
        {
            featureOptionsMigrators.RealizeImports();
            this.featureOptionsMigrators = featureOptionsMigrators;
        }

        // TODO : should we support contentType and feature also being changed?
        //        for now, let's assume it can't changed
        public string Migrate(Version versionFrom, Version versionTo, string feature, string data)
        {
            if (versionFrom == null)
            {
                throw new ArgumentNullException("versionFrom");
            }

            if (versionTo == null)
            {
                throw new ArgumentNullException("versionTo");
            }

            if (feature == null)
            {
                throw new ArgumentNullException("feature");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            // get migrators for given content type and feature
            var migrators = GetMigrators(feature);

            // create a graph among given migrators
            var migrationGraph = new MigrationGraph(migrators);

            // get shortest path for the migration between migrators
            var shortestMigrationPath = migrationGraph.GetShortestMigrationPath(versionFrom, versionTo).ToArray();
            if (shortestMigrationPath.Length == 0)
            {
                // TODO : what should we do when we can't migrate? how can we notify the user?
                return null;
            }

            var currentVersion = versionFrom;
            var currentData = data;
            foreach (var migrator in shortestMigrationPath)
            {
                // move to next version's format
                currentData = migrator.Value.Migrate(currentVersion, currentData);
                currentVersion = new Version(migrator.Metadata.VersionTo);
            }

            return currentData;
        }

        private IEnumerable<Lazy<IOptionMigrator, IOptionMigratorMetadata>> GetMigrators(
            string feature)
        {
            Contract.ThrowIfNull(feature);

            var filteredMigrators = from migrator in this.featureOptionsMigrators
                                    where migrator.Metadata.ApplicableFeature.Equals(feature)
                                    select migrator;

            return filteredMigrators;
        }

        private class MigrationGraph : IEqualityComparer<MigrationGraph.Edge>
        {
            public class Edge
            {
                public readonly Version VersionFrom;
                public readonly Version VersionTo;

                public Edge(
                    Version versionFrom,
                    Version versionTo)
                {
                    this.VersionFrom = versionFrom;
                    this.VersionTo = versionTo;
                }
            }

            private readonly Version undefinedVersion = null;

            private readonly HashSet<Version> nodes;
            private readonly Dictionary<Edge, Lazy<IOptionMigrator, IOptionMigratorMetadata>> edges;

            public MigrationGraph(IEnumerable<Lazy<IOptionMigrator, IOptionMigratorMetadata>> migrators)
            {
                this.nodes = new HashSet<Version>();
                this.edges = new Dictionary<Edge, Lazy<IOptionMigrator, IOptionMigratorMetadata>>(this);

                // build graph
                foreach (var migrator in migrators)
                {
                    foreach (var versionFromString in migrator.Metadata.VersionsFrom)
                    {
                        var versionFrom = new Version(versionFromString);
                        var versionTo = new Version(migrator.Metadata.VersionTo);

                        if (versionFrom.CompareTo(versionTo) >= 0)
                        {
                            // TODO : should I throw here or just move on?
                            throw new ArgumentException("versionFrom must be smaller than versionTo");
                        }

                        // add node to node list
                        this.nodes.Add(versionFrom);
                        this.nodes.Add(versionTo);

                        // add edge to edge list
                        var edge = new Edge(versionFrom, versionTo);
                        if (this.edges.ContainsKey(edge))
                        {
                            // there are more than one migrator defined that can migrate data from versions.
                            // just take the first one and move on.
                            continue;
                        }

                        this.edges.Add(edge, migrator);
                    }
                }
            }

            public IEnumerable<Lazy<IOptionMigrator, IOptionMigratorMetadata>> GetShortestMigrationPath(Version versionFrom, Version versionTo)
            {
                if (versionFrom.CompareTo(versionTo) >= 0)
                {
                    throw new ArgumentException("versionFrom must be smaller than versionTo");
                }

                // make sure given version exist in nodes
                if (!this.nodes.Contains(versionFrom) ||
                    !this.nodes.Contains(versionTo))
                {
                    // graph doesn't contain given versions
                    yield break;
                }

                var previousNodeInShortestPathFromNodeMap = BuildDijkstraPathMap(versionFrom, versionTo);
                if (previousNodeInShortestPathFromNodeMap == null)
                {
                    // no shortest path exist
                    yield break;
                }

                var shortestPathList = new LinkedList<Version>();
                var currentNode = versionTo;

                while (previousNodeInShortestPathFromNodeMap[currentNode] != this.undefinedVersion)
                {
                    // we are walking backward, put things in head
                    shortestPathList.AddFirst(currentNode);
                    currentNode = previousNodeInShortestPathFromNodeMap[currentNode];
                }

                // add last node
                shortestPathList.AddFirst(currentNode);

                // check obvious stuff
                Contract.ThrowIfFalse(shortestPathList.Count > 1);
                Contract.ThrowIfFalse(shortestPathList.First.Value.Equals(versionFrom));
                Contract.ThrowIfFalse(shortestPathList.Last.Value.Equals(versionTo));

                // now go through list to create migration path
                var enumerator = shortestPathList.GetEnumerator();
                Contract.ThrowIfFalse(enumerator.MoveNext());

                currentNode = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    var nextNode = enumerator.Current;
                    var key = new Edge(currentNode, nextNode);
                    currentNode = nextNode;

                    Contract.ThrowIfFalse(this.edges.ContainsKey(key));
                    yield return this.edges[key];
                }
            }

            private Dictionary<Version, Version> BuildDijkstraPathMap(Version versionFrom, Version versionTo)
            {
                // using Dijkstra's shortest path algorithm

                // helper data structure
                var sourceToNodeDistanceMap = new Dictionary<Version, int>();
                var previousNodeInShortestPathFromNodeMap = new Dictionary<Version, Version>();

                // initialize helper data structure
                foreach (var node in this.nodes)
                {
                    sourceToNodeDistanceMap.Add(node, int.MaxValue);
                    previousNodeInShortestPathFromNodeMap.Add(node, this.undefinedVersion);
                }

                // set source to source distance as 0 so that it becomes starting point
                sourceToNodeDistanceMap[versionFrom] = 0;

                var unprocessedNodes = new HashSet<Version>(this.nodes);
                while (unprocessedNodes.Count > 0)
                {
                    var node = GetNodeWithSmallestDistanceFromSource(sourceToNodeDistanceMap, unprocessedNodes);
                    if (sourceToNodeDistanceMap[node] == int.MaxValue)
                    {
                        // couldnt find path to go next. no shortest path exist
                        return null;
                    }
                    else if (node.Equals(versionTo))
                    {
                        // found shortest path, return path map
                        return previousNodeInShortestPathFromNodeMap;
                    }

                    // remove node from unprocessed node list
                    unprocessedNodes.Remove(node);

                    // get neighbor node that is not processed yet
                    foreach (var neighborNode in GetNeighborNodes(node).Where(neighbor => unprocessedNodes.Contains(neighbor)))
                    {
                        // calculate new distance from source to neighborNode
                        var newDistance = sourceToNodeDistanceMap[node] + 1;

                        // see if this path has smaller distance than other path to the neighbor node
                        if (newDistance < sourceToNodeDistanceMap[neighborNode])
                        {
                            // if so, record this path
                            sourceToNodeDistanceMap[neighborNode] = newDistance;
                            previousNodeInShortestPathFromNodeMap[neighborNode] = node;
                        }
                    }
                }

                return Contract.FailWithReturn<Dictionary<Version, Version>>("should never hit this");
            }

            private IEnumerable<Version> GetNeighborNodes(Version node)
            {
                // for now, we don't preprocess this to cache it, but if it becomes perf bottleneck,
                // we can preprocess and cache the map
                foreach (var edge in this.edges.Keys)
                {
                    if (edge.VersionFrom.Equals(node))
                    {
                        yield return edge.VersionTo;
                    }
                }
            }

            private Version GetNodeWithSmallestDistanceFromSource(
                Dictionary<Version, int> sourceToNodeDistanceMap,
                HashSet<Version> unprocessedNodes)
            {
                Contract.ThrowIfNull(sourceToNodeDistanceMap);
                Contract.ThrowIfFalse(unprocessedNodes.Count > 0);

                var nodeWithSmallestDistance = unprocessedNodes.First();
                foreach (var node in unprocessedNodes)
                {
                    // if it is new smallest, hold onto the new smallest distance node
                    if (sourceToNodeDistanceMap[node] < sourceToNodeDistanceMap[nodeWithSmallestDistance])
                    {
                        nodeWithSmallestDistance = node;
                    }
                }

                // now we found smallest distance node in unprocessedNode.
                return nodeWithSmallestDistance;
            }

            public bool Equals(Edge x, Edge y)
            {
                return x.VersionFrom.Equals(y.VersionFrom) && x.VersionTo.Equals(y.VersionTo);
            }

            public int GetHashCode(Edge obj)
            {
                return obj.VersionFrom.GetHashCode() << 1 + obj.VersionTo.GetHashCode();
            }
        }
    }
}
