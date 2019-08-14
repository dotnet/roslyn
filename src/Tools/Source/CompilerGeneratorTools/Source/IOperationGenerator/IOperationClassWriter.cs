// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private readonly Dictionary<string, AbstractNode?> _typeMap;

        private IOperationClassWriter(Tree tree, string location)
        {
            _tree = tree;
            _location = location;
            _typeMap = _tree.Types.OfType<AbstractNode>().ToDictionary(t => t.Name, t => (AbstractNode?)t);
            _typeMap.Add("IOperation", null);
        }

        public static void Write(Tree tree, string location)
        {
            new IOperationClassWriter(tree, location).WriteFiles();
        }

        #region Writing helpers
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
        #endregion

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

                    if (@namespace == "Operations")
                    {
                        WriteUsing("System.Collections.Generic");
                    }

                    WriteUsing("System.Collections.Immutable");

                    if (@namespace != "Operations")
                    {
                        WriteUsing("Microsoft.CodeAnalysis.Operations");
                    }
                    else
                    {
                        WriteUsing("Microsoft.CodeAnalysis.FlowAnalysis");
                    }

                    Blank();
                    WriteStartNamespace(@namespace);

                    WriteLine("#region Interfaces");
                    foreach (var node in grouping)
                    {
                        WriteInterface(node);
                    }

                    WriteLine("#endregion");

                    if (@namespace == "Operations")
                    {
                        Blank();
                        WriteClasses();
                        WriteVisitors();
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

            WriteObsoleteIfNecessary(node.Obsolete);
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
                        writeEnumElement(entry.Name,
                                         value: i,
                                         node.Name,
                                         entry.ExtraDescription,
                                         entry.EditorBrowsable ?? true,
                                         node.Obsolete?.Message,
                                         node.Obsolete?.ErrorText);
                    }
                }
                else
                {
                    var currentEntry = elementsToKindEnumerator.Current;
                    writeEnumElement(GetSubName(currentEntry.Name),
                                     value: i,
                                     currentEntry.Name,
                                     currentEntry.OperationKind?.ExtraDescription,
                                     editorBrowsable: true,
                                     currentEntry.Obsolete?.Message,
                                     currentEntry.Obsolete?.ErrorText);
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

        private void WriteClasses()
        {
            WriteLine("#region Implementations");
            foreach (var type in _tree.Types.OfType<AbstractNode>())
            {
                if (type.SkipClassGeneration) continue;

                var allProps = GetAllProperties(type);
                var ioperationTypes = allProps.Where(p => IsIOperationType(p.Type)).ToList();
                if (ioperationTypes.Count == 0)
                {
                    // No children, no need for lazy generation
                    var @class = (type.IsAbstract ? "Base" : "") + type.Name.Substring(1);
                    var extensibility = type.IsAbstract ? "abstract" : "sealed";
                    var baseType = type.Base.Substring(1);
                    if (baseType != "Operation")
                    {
                        baseType = $"Base{baseType}";
                    }

                    WriteLine($"internal {extensibility} partial class {@class} : {baseType}, {type.Name}");
                    Brace();

                    var accessibility = type.IsAbstract ? "protected" : "internal";
                    Write($"{accessibility} {@class}(");

                    foreach (var prop in allProps)
                    {
                        Debug.Assert(!IsIOperationType(prop.Type));
                        Write($"{prop.Type} {prop.Name.ToCamelCase()}, ");
                    }

                    writeStandardConstructorParameters(type.IsAbstract);
                    Indent();
                    Write(": base(");

                    var @base = _typeMap[type.Base];
                    if (@base is object)
                    {
                        foreach (var prop in GetAllProperties(@base).Where(p => !IsIOperationType(p.Type)))
                        {
                            Write($"{prop.Name.ToCamelCase()}, ");
                        }
                    }

                    writeStandardBaseArguments(type switch
                    {
                        { IsAbstract: true } => "kind",
                        { IsInternal: true } => "OperationKind.None",
                        _ => $"OperationKind.{getKind(type)}"
                    });
                    Outdent();
                    // The base class is responsible for initializing its own properties,
                    // so we only initialize our own properties here.
                    if (type.Properties.Count == 0)
                    {
                        // Note: our formatting style is a space here
                        WriteLine(" { }");
                    }
                    else
                    {
                        Blank();
                        Brace();
                        foreach (var prop in type.Properties)
                        {
                            WriteLine($"{prop.Name} = {prop.Name.ToCamelCase()};");
                        }
                        Unbrace();
                    }

                    foreach (var prop in type.Properties)
                    {
                        WriteLine($"public {prop.Type} {prop.Name} {{ get; }}");
                    }

                    if (type is Node node)
                    {

                        var visitorName = GetVisitorName(node);
                        WriteLine($@"public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.{visitorName}(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.{visitorName}(this, argument);");
                    }

                    Unbrace();
                }
            }

            WriteLine("#endregion");

            void writeStandardConstructorParameters(bool isAbstract)
            {
                if (isAbstract)
                {
                    Write("OperationKind kind, ");
                }

                WriteLine("SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)");
            }

            void writeStandardBaseArguments(string kind)
            {
                Write($"{kind}, semanticModel, syntax, type, constantValue, isImplicit)");
            }

            string getKind(AbstractNode node)
            {
                while (node.OperationKind?.Include == false)
                {
                    node = _typeMap[node.Base] ??
                        throw new InvalidOperationException($"{node.Name} is not being included in OperationKind, but has no base type!");
                }

                if (node.OperationKind?.Entries.Count > 0)
                {
                    return node.OperationKind.Entries.Where(e => e.EditorBrowsable != false).Single().Name;
                }

                return GetSubName(node.Name);
            }
        }

        private void WriteVisitors()
        {
            WriteLine("#region Visitors");
            WriteLine(@"public abstract partial class OperationVisitor
    {
        public virtual void Visit(IOperation operation) => operation?.Accept(this);
        public virtual void DefaultVisit(IOperation operation) { /* no-op */ }
        internal virtual void VisitNoneOperation(IOperation operation) { /* no-op */ }");
            Indent();

            var types = _tree.Types.OfType<Node>();
            foreach (var type in types)
            {
                if (type.SkipInVisitor) continue;

                WriteObsoleteIfNecessary(type.Obsolete);
                var accessibility = type.IsInternal ? "internal" : "public";
                var baseName = GetSubName(type.Name);
                WriteLine($"{accessibility} virtual void {GetVisitorName(type)}({type.Name} operation) => DefaultVisit(operation);");
            }

            Unbrace();

            WriteLine(@"public abstract partial class OperationVisitor<TArgument, TResult>
    {
        public virtual TResult Visit(IOperation operation, TArgument argument) => operation is null ? default(TResult) : operation.Accept(this, argument);
        public virtual TResult DefaultVisit(IOperation operation, TArgument argument) => default(TResult);
        internal virtual TResult VisitNoneOperation(IOperation operation, TArgument argument) => default(TResult);");
            Indent();

            foreach (var type in types)
            {
                if (type.SkipInVisitor) continue;

                WriteObsoleteIfNecessary(type.Obsolete);
                var accessibility = type.IsInternal ? "internal" : "public";
                WriteLine($"{accessibility} virtual TResult {GetVisitorName(type)}({type.Name} operation, TArgument argument) => DefaultVisit(operation, argument);");
            }

            Unbrace();
            WriteLine("#endregion");
        }

        private void WriteObsoleteIfNecessary(ObsoleteTag? tag)
        {
            if (tag is object)
            {
                WriteLine($"[Obsolete({tag.Message}, error: {tag.ErrorText})]");
            }
        }

        private string GetVisitorName(Node type)
        {
            Debug.Assert(!type.SkipInVisitor, $"{type.Name} is marked as skipped in visitor, cannot generate classes.");
            return type.VisitorName ?? $"Visit{GetSubName(type.Name)}";
        }

        private List<Property> GetAllProperties(AbstractNode node)
        {
            var properties = node.Properties.ToList();

            AbstractNode? @base = node;
            while (true)
            {
                string baseName = @base.Base;
                @base = _typeMap[baseName];
                if (@base is null) break;
                properties.AddRange(@base.Properties);
            }

            return properties;
        }

        private static string GetSubName(string operationName) => operationName[1..^9];

        private bool IsIOperationType(string typeName) => _typeMap.ContainsKey(typeName) ||
                                                          (IsImmutableArray(typeName, out var innerType) && IsIOperationType(innerType));

        private static bool IsImmutableArray(string typeName, [NotNullWhen(true)] out string? arrayType)
        {
            const string ImmutableArrayPrefix = "ImmutableArray<";
            if (typeName.StartsWith(ImmutableArrayPrefix, StringComparison.Ordinal))
            {
                arrayType = typeName[ImmutableArrayPrefix.Length..^1];
                return true;
            }

            arrayType = null;
            return false;
        }
    }

    internal static class Extensions
    {
        internal static string ToCamelCase(this string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
