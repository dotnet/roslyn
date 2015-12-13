using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        internal void WriteTo(ObjectWriter writer)
        {
            writer.WriteInt32(_allLowerCaseCharacters.Length);
            foreach (var c in _allLowerCaseCharacters)
            {
                writer.WriteChar(c);
            }

            writer.WriteInt32(this._nodes.Length);
            foreach (var node in _nodes)
            {
                node.WriteTo(writer);
            }

            writer.WriteInt32(this._edges.Length);
            foreach (var edge in _edges)
            {
                edge.WriteTo(writer);
            }
        }

        internal static BKTree ReadFrom(ObjectReader reader)
        {
            var allLowerCaseCharacters = new char[reader.ReadInt32()];
            for (var i = 0; i < allLowerCaseCharacters.Length; i++)
            {
                allLowerCaseCharacters[i] = reader.ReadChar();
            }

            var nodeCount = reader.ReadInt32();
            var nodes = ImmutableArray.CreateBuilder<Node>(nodeCount);
            for (var i = 0; i < nodeCount; i++)
            {
                nodes.Add(Node.ReadFrom(reader));
            }

            var edgeCount = reader.ReadInt32();
            var edges = ImmutableArray.CreateBuilder<Edge>(edgeCount);
            for (var i = 0; i < edgeCount; i++)
            {
                edges.Add(Edge.ReadFrom(reader));
            }

            return new BKTree(allLowerCaseCharacters, nodes.MoveToImmutable(), edges.MoveToImmutable());
        }
    }
}
