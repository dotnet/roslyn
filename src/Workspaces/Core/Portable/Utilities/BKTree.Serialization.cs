using System;
using System.Collections.Generic;
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

            var nodes = new Node[reader.ReadInt32()];
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = Node.ReadFrom(reader);
            }

            var edges = new Edge[reader.ReadInt32()];
            for (var i = 0; i < edges.Length; i++)
            {
                edges[i] = Edge.ReadFrom(reader);
            }

            return new BKTree(allLowerCaseCharacters, nodes, edges);
        }
    }
}
