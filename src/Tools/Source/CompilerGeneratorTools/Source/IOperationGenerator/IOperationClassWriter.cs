// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace IOperationGenerator
{
    internal enum NullHandling
    {
        Allow,
        Disallow,
        Always,
        NotApplicable // for value types
    }

    internal sealed class IOperationClassWriter
    {
        private TextWriter _writer = null!;
        private readonly string _location;
        private readonly Tree _tree;

        private IOperationClassWriter(Tree tree, string location)
        {
            _tree = tree;
            _location = location;
        }

        public static void Write(Tree tree, string location)
        {
            new IOperationClassWriter(tree, location).WriteFiles();
        }

        private int _indent;
        private bool _needsIndent = true;

        private void Write(string format)
        {
            if (_needsIndent)
            {
                _writer.Write(new string(' ', _indent * 4));
                _needsIndent = false;
            }
            _writer.Write(format);
        }

        private void WriteLine(string format)
        {
            Write(format);
            _writer.WriteLine();
            _needsIndent = true;
        }

        private void Blank()
        {
            _writer.WriteLine();
            _needsIndent = true;
        }

        private void Brace()
        {
            WriteLine("{");
            Indent();
        }

        private void Unbrace()
        {
            Outdent();
            WriteLine("}");
        }

        private void Indent()
        {
            ++_indent;
        }

        private void Outdent()
        {
            --_indent;
        }

        private void WriteFiles()
        {
            foreach (var grouping in _tree.Types.OfType<AbstractNode>().GroupBy(n => n.Namespace))
            {
                var @namespace = grouping.Key ?? "Operations";
                var outFileName = Path.Combine(_location, $"{@namespace}.Generated.cs");
                using (_writer = new StreamWriter(File.Open(outFileName, FileMode.Create), Encoding.UTF8))
                {
                    writeHeader();
                    WriteUsing("System");
                    WriteUsing("System.Collections.Immutable");

                    if (grouping.Key is object)
                    {
                        WriteUsing("Microsoft.CodeAnalysis.Operations");
                    }

                    Blank();
                    WriteStartNamespace(@namespace);

                    foreach (var node in grouping)
                    {
                        WriteInterface(node);
                    }

                    WriteEndNamespace();
                    _writer.Flush();
                }
            }

            using (_writer = new StreamWriter(File.Open(Path.Combine(_location, "OperationKind.Generated.cs"), FileMode.Create)))
            {
                writeHeader();
                WriteUsing("System");
                WriteUsing("System.ComponentModel");
                WriteUsing("Microsoft.CodeAnalysis.FlowAnalysis");
                WriteUsing("Microsoft.CodeAnalysis.Operations");

                WriteStartNamespace(namespaceSuffix: null);

                WriteOperationKind();

                WriteEndNamespace();
            }

            void writeHeader()
            {
                WriteLine("// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.");
                WriteLine("// < auto-generated />");
            }
        }

        private void WriteUsing(string nsName)
        {
            WriteLine($"using {nsName};");
        }

        private void WriteStartNamespace(string? namespaceSuffix)
        {
            WriteLine($"namespace Microsoft.CodeAnalysis{(namespaceSuffix is null ? "" : $".{namespaceSuffix}")}");
            Brace();
        }

        private void WriteEndNamespace()
        {
            Unbrace();
        }

        private void WriteInterface(AbstractNode node)
        {
            WriteComments(node.Comments, writeReservedRemark: true);

            if (node.Obsolete is { } obsolete)
            {
                WriteLine($"[Obsolete({obsolete.Message}, error: {obsolete.ErrorText})]");
            }

            WriteLine($"{(node.IsInternal ? "internal" : "public")} interface {node.Name} : {node.Base}");
            Brace();

            foreach (var property in node.Properties)
            {
                WriteInterfaceProperty(property);
            }

            Unbrace();
        }

        private void WriteComments(Comments? comments, bool writeReservedRemark)
        {
            if (comments is object)
            {
                bool hasWrittenRemarks = false;

                foreach (var el in comments.Elements)
                {
                    WriteLine($"/// <{el.LocalName}>");

                    string[] separators = new[] { "\r", "\n", "\r\n" };
                    string[] lines = el.InnerXml.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    int indentation = lines[0].Length - lines[0].TrimStart().Length;

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        WriteLine($"/// {line.Substring(indentation)}");
                    }

                    if (el.LocalName == "remarks" && writeReservedRemark)
                    {
                        hasWrittenRemarks = true;
                        writeReservedRemarkText();
                    }

                    WriteLine($"/// </{el.LocalName}>");
                }

                if (writeReservedRemark && !hasWrittenRemarks)
                {
                    WriteLine("/// <remarks>");
                    writeReservedRemarkText();
                    WriteLine("/// </remarks>");
                }
            }

            void writeReservedRemarkText()
            {
                WriteLine("/// This interface is reserved for implementation by its associated APIs. We reserve the right to");
                WriteLine("/// change it in the future.");
            }
        }

        private void WriteInterfaceProperty(Property prop)
        {
            WriteComments(prop.Comments, writeReservedRemark: false);
            var modifiers = prop.IsNew ? "new " : "";
            WriteLine($"{modifiers}{prop.Type} {prop.Name} {{ get; }}");
        }

        private void WriteOperationKind()
        {
            WriteLine("/// <summary>");
            WriteLine("/// All of the kinds of operations, including statements and expressions.");
            WriteLine("/// </summary>");
            WriteLine("public enum OperationKind");
            Brace();

            WriteLine("/// <summary>Indicates an <see cref=\"IOperation\"/> for a construct that is not implemented yet.</summary>");
            WriteLine("None = 0x0,");

            Dictionary<int, IEnumerable<(OperationKindEntry, AbstractNode)>> explicitKinds = _tree.Types.OfType<AbstractNode>()
                .Where(n => n.OperationKind?.Entries is object)
                .SelectMany(n => n.OperationKind!.Entries.Select(e => (entry: e, node: n)))
                .GroupBy(e => e.entry.Value)
                .ToDictionary(g => g.Key, g => g.Select(k => (entryName: k.entry, k.node)));

            // Conditions for inclusion in the OperationKind enum:
            //  1. Concrete Node types that do not have an explicit false include flag OR AbstractNodes that have an explicit true include flag
            //  2. No explicit kind entries: those are handled above.
            //  3. No internal nodes.
            List<AbstractNode> elementsToKind = _tree.Types.OfType<AbstractNode>()
                .Where(n => ((n is Node && (n.OperationKind?.Include != false)) ||
                             n.OperationKind?.Include == true) &&
                            (n.OperationKind?.Entries is null || n.OperationKind?.Entries.Count == 0) &&
                            !n.IsInternal)
                .ToList();

            var unusedKinds = _tree.UnusedOperationKinds.Entries?.Select(e => e.Value).ToList() ?? new List<int>();

            using var elementsToKindEnumerator = elementsToKind.GetEnumerator();
            Debug.Assert(elementsToKindEnumerator.MoveNext());

            int numKinds = elementsToKind.Count + explicitKinds.Count + unusedKinds.Count;

            for (int i = 1; i <= numKinds; i++)
            {
                if (unusedKinds.Contains(i))
                {
                    WriteLine($"// Unused: {i:x}");
                }
                else if (explicitKinds.TryGetValue(i, out var kinds))
                {
                    foreach (var (entry, node) in kinds)
                    {
                        writeEnumElement(entry.Name, i, node.Name, entry.ExtraDescription, entry.EditorBrowsable ?? true, node.Obsolete?.Message, node.Obsolete?.ErrorText);
                    }
                }
                else
                {
                    var currentEntry = elementsToKindEnumerator.Current;
                    writeEnumElement(GetKindName(currentEntry.Name), i, currentEntry.Name, currentEntry.OperationKind?.ExtraDescription, editorBrowsable: true, currentEntry.Obsolete?.Message, currentEntry.Obsolete?.ErrorText);
                    Debug.Assert(elementsToKindEnumerator.MoveNext() || i == numKinds);
                }
            }

            Unbrace();

            void writeEnumElement(string kind, int value, string operationName, string? extraText, bool editorBrowsable, string? obsoleteMessage, string? obsoleteError)
            {
                WriteLine($"/// <summary>Indicates an <see cref=\"{operationName}\"/>.{(extraText is object ? $" {extraText}" : "")}</summary>");

                if (!editorBrowsable)
                {
                    WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");
                }

                if (obsoleteMessage is object)
                {
                    WriteLine($"[Obsolete({obsoleteMessage}, error: {obsoleteError})]");
                }

                WriteLine($"{kind} = 0x{value:x},");
            }
        }

        private string GetKindName(string operationName) => operationName[1..^9];

        private bool IsImmutableArray(string typeName)
        {
            return typeName.StartsWith("ImmutableArray<", StringComparison.Ordinal);
        }
    }
}
