// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal sealed partial class IOperationClassWriter
    {
        private TextWriter _writer = null!;
        private readonly string _location;
        private readonly Tree _tree;
        private readonly Dictionary<string, AbstractNode?> _typeMap;

        private static string QuoteString(string value)
            => "@\"" + value.Replace("\"", "\"\"") + "\"";

        private IOperationClassWriter(Tree tree, string location)
        {
            _tree = tree;
            _location = location;
            _typeMap = _tree.Types.OfType<AbstractNode>().ToDictionary(t => t.Name, t => (AbstractNode?)t);
            _typeMap.Add("IOperation", null);
        }

        /// <summary>Returns true for success</summary>
        public static bool Write(Tree tree, string location)
        {
            return new IOperationClassWriter(tree, location).WriteFiles();
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

        /// <summary>Returns true for success</summary>
        private bool WriteFiles()
        {
            if (ModelHasErrors(_tree))
            {
                Console.WriteLine("Encountered xml errors, not generating");
                return false;
            }

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
                        WriteUsing("System.Threading");
                    }

                    WriteUsing("System.Collections.Immutable");
                    WriteUsing("System.Diagnostics.CodeAnalysis");

                    if (@namespace != "Operations")
                    {
                        WriteUsing("Microsoft.CodeAnalysis.Operations");
                    }
                    else
                    {
                        WriteUsing("Microsoft.CodeAnalysis.FlowAnalysis");
                    }

                    WriteUsing("Microsoft.CodeAnalysis.PooledObjects");

                    if (@namespace == "Operations")
                    {
                        WriteUsing("Roslyn.Utilities");
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
                        WriteCloner();
                        WriteVisitors();
                    }

                    WriteEndNamespace();
                    _writer.Flush();
                }
            }

            using (_writer = new StreamWriter(File.Open(Path.Combine(_location, "OperationKind.Generated.cs"), FileMode.Create), Encoding.UTF8))
            {
                writeHeader();
                WriteUsing("System");
                WriteUsing("System.ComponentModel");
                WriteUsing("System.Diagnostics.CodeAnalysis");
                WriteUsing("Microsoft.CodeAnalysis.FlowAnalysis");
                WriteUsing("Microsoft.CodeAnalysis.Operations");

                WriteStartNamespace(namespaceSuffix: null);

                WriteOperationKind();

                WriteEndNamespace();
            }

            return true;

            void writeHeader()
            {
                WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
                WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
                WriteLine("// See the LICENSE file in the project root for more information.");
                WriteLine("// < auto-generated />");
                WriteLine("#nullable enable");
                WriteLine("#pragma warning disable RSEXPERIMENTAL006 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.");
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
            WriteComments(node.Comments, getNodeKinds(node), writeReservedRemark: true);

            WriteExperimentalAttributeIfNeeded(node);
            WriteObsoleteIfNecessary(node.Obsolete);
            WriteLine($"{(node.IsInternal ? "internal" : "public")} interface {node.Name} : {node.Base}");
            Brace();

            foreach (var property in node.Properties)
            {
                WriteInterfaceProperty(property);
            }

            Unbrace();

            IEnumerable<string> getNodeKinds(AbstractNode node)
            {
                if (node.OperationKind is { } kind)
                {
                    if (kind.Include is false)
                        return Enumerable.Empty<string>();

                    return node.OperationKind.Entries.Select(entry => entry.Name);
                }

                if (node.IsAbstract || node.IsInternal)
                    return Enumerable.Empty<string>();

                return new[] { GetSubName(node.Name) };
            }
        }

        private void WriteComments(Comments? comments, IEnumerable<string> operationKinds, bool writeReservedRemark)
        {
            if (comments is object)
            {
                bool hasWrittenRemarks = false;

                foreach (var el in comments.Elements)
                {
                    WriteLine($"/// <{el.LocalName}>");

                    string[] separators = ["\r", "\n", "\r\n"];
                    string[] lines = el.InnerXml.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    int indentation = lines[0].Length - lines[0].TrimStart().Length;

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
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
                if (operationKinds.Any())
                {
                    WriteLine("/// <para>This node is associated with the following operation kinds:</para>");
                    WriteLine("/// <list type=\"bullet\">");

                    foreach (var kind in operationKinds)
                        WriteLine($"/// <item><description><see cref=\"OperationKind.{kind}\"/></description></item>");

                    WriteLine("/// </list>");
                }

                WriteLine("/// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to");
                WriteLine("/// change it in the future.</para>");
            }
        }

        private void WriteInterfaceProperty(Property prop)
        {
            if (prop.IsInternal || prop.IsOverride)
                return;
            WriteComments(prop.Comments, operationKinds: Enumerable.Empty<string>(), writeReservedRemark: false);
            WriteExperimentalAttributeIfNeeded(prop);
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
                                         node.Obsolete?.ErrorText,
                                         experimentalUrl: node.IsInternal ? null : node.Experimental);
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
                                     currentEntry.Obsolete?.ErrorText,
                                     experimentalUrl: currentEntry.IsInternal ? null : currentEntry.Experimental);
                    Debug.Assert(elementsToKindEnumerator.MoveNext() || i == numKinds);
                }
            }

            Unbrace();

            void writeEnumElement(string kind, int value, string operationName, string? extraText, bool editorBrowsable, string? obsoleteMessage, string? obsoleteError, string? experimentalUrl)
            {
                WriteLine($"/// <summary>Indicates an <see cref=\"{operationName}\"/>.{(extraText is object ? $" {extraText}" : "")}</summary>");

                if (!editorBrowsable)
                {
                    WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");
                }

                if (!string.IsNullOrEmpty(experimentalUrl))
                {
                    WriteLine($"[Experimental(global::Microsoft.CodeAnalysis.RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = {QuoteString(experimentalUrl)})]");
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
                if (type.SkipClassGeneration)
                    continue;

                writeClass(type);
            }

            WriteLine("#endregion");

            void writeClass(AbstractNode type)
            {
                var allProps = GetAllProperties(type);
                bool hasSkippedProperties = !GetAllProperties(type, includeSkipGenerationProperties: true).SequenceEqual(allProps);
                var ioperationProperties = allProps.Where(p => IsIOperationType(p.Type)).ToList();
                var publicIOperationProps = ioperationProperties.Where(p => !p.IsInternal).ToList();
                string typeName = type.Name[1..];
                var hasType = false;
                var hasConstantValue = false;
                var multipleValidKinds = HasMultipleValidKinds(type);

                IEnumerable<Property>? baseProperties = null;
                if (_typeMap[type.Base] is { } baseNode)
                {
                    baseProperties = GetAllProperties(baseNode);
                }

                var @class = type.IsAbstract ? $"Base{typeName}" : typeName;
                var @base = type.Base[1..];
                if (@base != "Operation")
                {
                    @base = $"Base{@base}";
                }

                writeClassHeader(type.IsAbstract ? "abstract" : "sealed", @class, @base, type.Name);

                if (type is Node and var node)
                {
                    hasType = node.HasType;
                    hasConstantValue = node.HasConstantValue;
                }
                else
                {
                    node = null;
                }

                writeConstructor(type.IsAbstract ? "protected" : "internal", @class, allProps, baseProperties, type, hasType, hasConstantValue, multipleValidKinds);

                foreach (var property in type.Properties.Where(p => !p.SkipGeneration))
                {
                    switch (property.MakeAbstract, type.IsAbstract)
                    {
                        case (true, true):
                            writeProperty(property, propExtensibility: "abstract ");
                            break;
                        case (true, false):
                            continue;
                        default:
                            writeProperty(property, propExtensibility: string.Empty);
                            break;
                    }
                }

                if (node != null)
                {
                    writeCountProperty(publicIOperationProps);
                    if (!node.SkipChildrenGeneration)
                    {
                        writeEnumeratorMethods(type, publicIOperationProps, node);
                    }

                    WriteLine($"public override ITypeSymbol? Type {(node.HasType ? "{ get; }" : "=> null;")}");

                    WriteLine($"internal override ConstantValue? OperationConstantValue {(hasConstantValue ? "{ get; }" : "=> null;")}");

                    if (multipleValidKinds)
                    {
                        WriteLine("public override OperationKind Kind { get; }");
                    }
                    else
                    {
                        var kind = node.IsInternal ? "None" : $"{getKind(node)}";
                        WriteLine($"public override OperationKind Kind => OperationKind.{kind};");
                    }

                    writeAcceptMethods(GetVisitorName(node));
                }

                Unbrace();

                void writeClassHeader(string extensibility, string @class, string baseType, string @interface)
                {
                    WriteLine($"internal {extensibility} partial class {@class} : {baseType}, {@interface}");
                    Brace();
                }

                void writeConstructor(string accessibility, string @class, IEnumerable<Property> properties, IEnumerable<Property>? baseProperties, AbstractNode type, bool hasType, bool hasConstantValue, bool multipleValidKinds)
                {
                    Write($"{accessibility} {@class}(");

                    var newProps = new HashSet<string>(StringComparer.Ordinal);

                    foreach (var prop in properties)
                    {
                        if (newProps.Contains(prop.Name))
                        {
                            continue;
                        }

                        if (prop.IsNew)
                        {
                            newProps.Add(prop.Name);
                        }

                        if (prop.Type == "CommonConversion")
                        {
                            Write($"IConvertibleConversion {prop.Name.ToCamelCase()}, ");
                        }
                        else if (prop.MakeAbstract)
                        {
                            continue;
                        }
                        else
                        {
                            Write($"{prop.Type} {prop.Name.ToCamelCase()}, ");
                        }
                    }

                    if (multipleValidKinds)
                    {
                        Write("OperationKind kind, ");
                    }

                    var typeParameterString = hasType ? "ITypeSymbol? type, " : string.Empty;
                    var constantValueString = hasConstantValue ? "ConstantValue? constantValue, " : string.Empty;
                    Write($"SemanticModel? semanticModel, SyntaxNode syntax, {typeParameterString}{constantValueString}bool isImplicit");

                    WriteLine(")");
                    Indent();
                    Write(": base(");

                    if (baseProperties is object)
                    {
                        // This will naturally pass all new'd parameters to the base
                        foreach (var prop in baseProperties)
                        {
                            if (prop.MakeAbstract)
                            {
                                continue;
                            }

                            Write($"{prop.Name.ToCamelCase()}, ");
                        }
                    }
                    Write("semanticModel, syntax, isImplicit)");

                    Outdent();

                    List<Property> propsToInitialize = type.Properties.Where(p => !p.SkipGeneration && !p.MakeAbstract).ToList();

                    if (propsToInitialize.Count == 0 && !hasType)
                    {
                        // Note: our formatting style is a space here
                        WriteLine(" { }");
                    }
                    else
                    {
                        Blank();
                        Brace();
                        foreach (var prop in propsToInitialize)
                        {
                            if (prop.IsNew)
                            {
                                continue;
                            }
                            else if (prop.Type == "CommonConversion")
                            {
                                WriteLine($"{prop.Name}Convertible = {prop.Name.ToCamelCase()};");
                            }
                            else
                            {
                                var initializer = IsIOperationType(prop.Type)
                                    ? $"SetParentOperation({prop.Name.ToCamelCase()}, this)"
                                    : prop.Name.ToCamelCase();
                                WriteLine($"{prop.Name} = {initializer};");
                            }
                        }

                        if (hasConstantValue)
                        {
                            WriteLine("OperationConstantValue = constantValue;");
                        }

                        if (hasType)
                        {
                            WriteLine("Type = type;");
                        }

                        if (multipleValidKinds)
                        {
                            WriteLine("Kind = kind;");
                        }

                        Unbrace();
                    }
                }

                void writeProperty(Property prop, string propExtensibility)
                {
                    if (prop.Type == "CommonConversion")
                    {
                        // Common conversions need an internal property for the IConvertibleConversion
                        // version of the property, and the public version needs to call ToCommonConversion
                        WriteLine($"internal IConvertibleConversion {prop.Name}Convertible {{ get; }}");
                        WriteLine($"public CommonConversion {prop.Name} => {prop.Name}Convertible.ToCommonConversion();");
                    }
                    else if (prop.IsNew)
                    {
                        Write($"public new {propExtensibility}{prop.Type} {prop.Name} => ");

                        // If the type that is being new'd is more specific, emit a cast. Otherwise, just delegate
                        Debug.Assert(baseProperties != null);
                        var baseProp = baseProperties.Single(p => p.Name == prop.Name);
                        var basePropTypeWithoutNullable = GetTypeNameWithoutNullable(baseProp.Type);
                        var propTypeWithoutNullable = GetTypeNameWithoutNullable(prop.Type);
                        if (basePropTypeWithoutNullable != propTypeWithoutNullable)
                        {
                            Write($"({prop.Type})");
                        }

                        Write($"base.{baseProp.Name}");

                        if (baseProp.Type[^1] == '?' && prop.Type[^1] != '?')
                        {
                            Write("!");
                        }

                        WriteLine(";");
                    }
                    else
                    {
                        if (prop.IsOverride)
                        {
                            propExtensibility += "override ";
                        }
                        WriteLine($"public {propExtensibility}{prop.Type} {prop.Name} {{ get; }}");
                    }
                }

                void writeAcceptMethods(string visitorName)
                {
                    WriteLine($"public override void Accept(OperationVisitor visitor) => visitor.{visitorName}(this);");
                    WriteLine($"public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.{visitorName}(this, argument);");
                }

                string getKind(AbstractNode node)
                {
                    while (node.OperationKind?.Include == false)
                    {
                        node = (AbstractNode?)_typeMap[node.Base] ??
                            throw new InvalidOperationException($"{node.Name} is not being included in OperationKind, but has no base type!");
                    }

                    if (node.OperationKind?.Entries.Count > 0)
                    {
                        return node.OperationKind.Entries.Where(e => e.EditorBrowsable != false).Single().Name;
                    }

                    return GetSubName(node.Name);
                }

                void writeCountProperty(List<Property> publicIOperationProps)
                {
                    Write("internal override int ChildOperationsCount =>");
                    if (publicIOperationProps.Count == 0)
                    {
                        WriteLine(" 0;");
                    }
                    else
                    {
                        WriteLine("");
                        Indent();
                        bool isFirst = true;
                        foreach (var prop in publicIOperationProps)
                        {
                            if (isFirst)
                            {
                                isFirst = false;
                            }
                            else
                            {
                                WriteLine(" +");
                            }

                            if (IsImmutableArray(prop.Type, out _))
                            {
                                Write($"{prop.Name}.Length");
                            }
                            else
                            {
                                Write($"({prop.Name} is null ? 0 : 1)");
                            }
                        }

                        WriteLine(";");
                        Outdent();
                    }
                }

                void writeEnumeratorMethods(AbstractNode type, List<Property> publicIOperationProps, Node node)
                {
                    if (publicIOperationProps.Count > 0)
                    {
                        var orderedProperties = new List<Property>();

                        if (publicIOperationProps.Count == 1)
                        {
                            orderedProperties.Add(publicIOperationProps.Single());
                        }
                        else
                        {
                            Debug.Assert(node.ChildrenOrder != null, $"Encountered null children order for {type.Name}, should have been caught in verifier!");
                            var childrenOrdered = GetPropertyOrder(node);

                            foreach (var childName in childrenOrdered)
                            {
                                orderedProperties.Add(publicIOperationProps.Find(p => p.Name == childName) ??
                                    throw new InvalidOperationException($"Cannot find property for {childName}"));
                            }
                        }

                        writeGetCurrent(orderedProperties);
                        writeMoveNext(orderedProperties);
                        writeMoveNextReversed(orderedProperties);
                    }
                    else
                    {
                        WriteLine("internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));");
                        WriteLine("internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);");
                        WriteLine("internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);");
                    }

                    void writeGetCurrent(List<Property> orderedProperties)
                    {
                        WriteLine("internal override IOperation GetCurrent(int slot, int index)");
                        Indent();
                        WriteLine("=> slot switch");
                        Brace();

                        for (int i = 0; i < orderedProperties.Count; i++)
                        {
                            var prop = orderedProperties[i];

                            Write($"{i} when ");

                            if (IsImmutableArray(prop.Type, out _))
                            {
                                WriteLine($"index < {prop.Name}.Length");
                                Indent();
                                WriteLine($"=> {prop.Name}[index],");
                                Outdent();
                            }
                            else
                            {
                                WriteLine($"{prop.Name} != null");
                                Indent();
                                WriteLine($"=> {prop.Name},");
                                Outdent();
                            }
                        }

                        WriteLine("_ => throw ExceptionUtilities.UnexpectedValue((slot, index)),");
                        Outdent();
                        WriteLine("};");
                        Outdent();
                    }

                    void writeMoveNext(List<Property> orderedProperties)
                    {
                        WriteLine("internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)");
                        Brace();
                        WriteLine("switch (previousSlot)");
                        Brace();

                        int slot = 0;
                        for (; slot < orderedProperties.Count; slot++)
                        {
                            // Operation.ChildCollection.Enumerator starts indexes at -1. For a given property, the general pseudocode is:

                            // case previousSlot:
                            //     if (element i is valid) return (true, i, 0);
                            //     else goto i;

                            // If i is an IOperation, is valid means not null. If i is an ImmutableArray, it means not empty.
                            // As IOperation is fully nullable-enabled, and the abstract `Current` method is nullable, we'll
                            // get a warning if it attempts to return a null IOperation from such an array, so we don't need
                            // to have explicit Debug.Assert code for this.

                            // Then, if the property is an immutable array:
                            // case i when previousIndex + 1 < property[i].Length:
                            //    return (true, i, previousIndex + 1);

                            // While the next index is still valid, this will hit this case for i, only moving to the next
                            // element after the array is exhausted.

                            var previousSlot = slot - 1;
                            var prop = orderedProperties[slot];

                            WriteLine($"case {previousSlot}:");
                            Indent();

                            bool isImmutableArray = IsImmutableArray(prop.Type, out _);
                            if (isImmutableArray)
                            {
                                WriteLine($"if (!{prop.Name}.IsEmpty) return (true, {slot}, 0);");
                            }
                            else
                            {
                                WriteLine($"if ({prop.Name} != null) return (true, {slot}, 0);");
                            }

                            WriteLine($"else goto case {slot};");

                            Outdent();

                            if (isImmutableArray)
                            {
                                WriteLine($"case {slot} when previousIndex + 1 < {prop.Name}.Length:");
                                Indent();
                                WriteLine($"return (true, {slot}, previousIndex + 1);");
                                Outdent();
                            }
                        }

                        // We introduce an explicit "eof" slot, that indicates the enumerator has moved beyond
                        // the end of the sequence. This allows us to differentiate between repeated calls to
                        // MoveNext, which are valid and always return false and the "eof" slot, and invalid
                        // usage of the API (which may give us a slot that we are not expecting.)
                        int lastSlot = slot - 1;
                        WriteLine($"case {lastSlot}:");
                        WriteLine($"case {slot}:");
                        Indent();
                        WriteLine($"return (false, {slot}, 0);");
                        Outdent();

                        WriteLine("default:");
                        Indent();
                        WriteLine("throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));");
                        Outdent();
                        Unbrace();
                        Unbrace();
                    }

                    void writeMoveNextReversed(List<Property> orderedProperties)
                    {
                        WriteLine("internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)");
                        Brace();
                        WriteLine("switch (previousSlot)");
                        Brace();

                        int slot = orderedProperties.Count - 1;
                        for (; slot >= 0; slot--)
                        {
                            // Operation.ChildCollection.Reversed.Enumerator starts indexes at int.MaxValue. For a given property, the general pseudocode is:

                            // case previousSlot:
                            //     if (element i is valid) return (true, i, 0);
                            //     else goto i;

                            // If i is an IOperation, is valid means not null. If i is an ImmutableArray, it means not empty.
                            // As IOperation is fully nullable-enabled, and the abstract `Current` method is nullable, we'll
                            // get a warning if it attempts to return a null IOperation from such an array, so we don't need
                            // to have explicit Debug.Assert code for this.

                            // Then, if the property is an immutable array:
                            // case i when previousIndex > 0:
                            //    return (true, i, previousIndex - 1);

                            // While the next index is still valid, this will hit this case for i, only moving to the next
                            // element (meaning i - 1) after the array is exhausted.

                            var previousSlot = slot == (orderedProperties.Count - 1) ? "int.MaxValue" : (slot + 1).ToString();
                            var prop = orderedProperties[slot];

                            WriteLine($"case {previousSlot}:");
                            Indent();

                            bool isImmutableArray = IsImmutableArray(prop.Type, out _);
                            if (isImmutableArray)
                            {
                                WriteLine($"if (!{prop.Name}.IsEmpty) return (true, {slot}, {prop.Name}.Length - 1);");
                            }
                            else
                            {
                                WriteLine($"if ({prop.Name} != null) return (true, {slot}, 0);");
                            }

                            WriteLine($"else goto case {slot};");

                            Outdent();

                            if (isImmutableArray)
                            {
                                WriteLine($"case {slot} when previousIndex > 0:");
                                Indent();
                                WriteLine($"return (true, {slot}, previousIndex - 1);");
                                Outdent();
                            }
                        }

                        // We introduce an explicit "eof" slot, that indicates the enumerator has moved beyond
                        // the end of the sequence. This allows us to differentiate between repeated calls to
                        // MoveNext, which are valid and always return false and the "eof" slot, and invalid
                        // usage of the API (which may give us a slot that we are not expecting.)
                        WriteLine("case 0:");
                        WriteLine("case -1:");
                        Indent();
                        WriteLine($"return (false, -1, 0);");
                        Outdent();

                        WriteLine("default:");
                        Indent();
                        WriteLine("throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));");
                        Outdent();
                        Unbrace();
                        Unbrace();
                    }
                }
            }
        }

        private static bool HasMultipleValidKinds(AbstractNode type)
        {
            return (type.OperationKind?.Entries?.Where(e => e.EditorBrowsable != false).Count() ?? 0) > 1;
        }

        private void WriteCloner()
        {
            WriteLine("#region Cloner");

            WriteLine(@"internal sealed partial class OperationCloner : OperationVisitor<object?, IOperation>");
            Brace();

            WriteLine("private static readonly OperationCloner s_instance = new OperationCloner();");
            WriteLine("/// <summary>Deep clone given IOperation</summary>");
            WriteLine("public static T CloneOperation<T>(T operation) where T : IOperation => s_instance.Visit(operation);");
            WriteLine("public OperationCloner() { }");
            WriteLine(@"[return: NotNullIfNotNull(""node"")]");
            WriteLine("private T? Visit<T>(T? node) where T : IOperation? => (T?)Visit(node, argument: null);");
            WriteLine("public override IOperation DefaultVisit(IOperation operation, object? argument) => throw ExceptionUtilities.Unreachable();");
            WriteLine("private ImmutableArray<T> VisitArray<T>(ImmutableArray<T> nodes) where T : IOperation => nodes.SelectAsArray((n, @this) => @this.Visit(n), this)!;");
            WriteLine("private ImmutableArray<(ISymbol, T)> VisitArray<T>(ImmutableArray<(ISymbol, T)> nodes) where T : IOperation => nodes.SelectAsArray((n, @this) => (n.Item1, @this.Visit(n.Item2)), this)!;");

            foreach (var node in _tree.Types.OfType<Node>())
            {
                const string internalName = "internalOperation";

                if (node.SkipClassGeneration || node.SkipInCloner)
                {
                    continue;
                }

                string nameMinusI = node.Name[1..];
                WriteLine($"{(node.IsInternal ? "internal" : "public")} override IOperation {GetVisitorName(node)}({node.Name} operation, object? argument)");
                Brace();
                WriteLine($"var {internalName} = ({nameMinusI})operation;");
                Write($"return new {nameMinusI}(");

                var newProps = new HashSet<string>(StringComparer.Ordinal);
                foreach (var prop in GetAllProperties(node))
                {
                    if (prop.MakeAbstract || newProps.Contains(prop.Name))
                    {
                        continue;
                    }

                    if (prop.IsNew)
                    {
                        newProps.Add(prop.Name);
                    }

                    if (IsIOperationType(prop.Type))
                    {
                        Write(IsImmutableArray(prop.Type, out _) ? "VisitArray" : "Visit");
                        Write($"({internalName}.{prop.Name}), ");
                    }
                    else if (prop.Type == "CommonConversion")
                    {
                        Write($"{internalName}.{prop.Name}Convertible, ");
                    }
                    else
                    {
                        Write($"{internalName}.{prop.Name}, ");
                    }
                }

                if (HasMultipleValidKinds(node))
                {
                    Write($"{internalName}.Kind, ");
                }

                Write($"{internalName}.OwningSemanticModel, {internalName}.Syntax, ");

                if (node.HasType)
                {
                    Write($"{internalName}.Type, ");
                }

                if (node.HasConstantValue)
                {
                    Write($"{internalName}.OperationConstantValue, ");
                }

                WriteLine($"{internalName}.IsImplicit);");
                Unbrace();
            }

            Unbrace();

            WriteLine("#endregion");
            WriteLine("");
        }

        private void WriteVisitors()
        {
            WriteLine("#region Visitors");
            WriteLine(@"public abstract partial class OperationVisitor
    {
        public virtual void Visit(IOperation? operation) => operation?.Accept(this);
        public virtual void DefaultVisit(IOperation operation) { /* no-op */ }
        internal virtual void VisitNoneOperation(IOperation operation) { /* no-op */ }");
            Indent();

            var types = _tree.Types.OfType<Node>();
            foreach (var type in types)
            {
                if (type.SkipInVisitor)
                    continue;

                WriteExperimentalAttributeIfNeeded(type);
                WriteObsoleteIfNecessary(type.Obsolete);
                var accessibility = type.IsInternal ? "internal" : "public";
                var baseName = GetSubName(type.Name);
                WriteLine($"{accessibility} virtual void {GetVisitorName(type)}({type.Name} operation) => DefaultVisit(operation);");
            }

            Unbrace();

            WriteLine(@"public abstract partial class OperationVisitor<TArgument, TResult>
    {
        public virtual TResult? Visit(IOperation? operation, TArgument argument) => operation is null ? default(TResult) : operation.Accept(this, argument);
        public virtual TResult? DefaultVisit(IOperation operation, TArgument argument) => default(TResult);
        internal virtual TResult? VisitNoneOperation(IOperation operation, TArgument argument) => default(TResult);");
            Indent();

            foreach (var type in types)
            {
                if (type.SkipInVisitor)
                    continue;

                WriteExperimentalAttributeIfNeeded(type);
                WriteObsoleteIfNecessary(type.Obsolete);
                var accessibility = type.IsInternal ? "internal" : "public";
                WriteLine($"{accessibility} virtual TResult? {GetVisitorName(type)}({type.Name} operation, TArgument argument) => DefaultVisit(operation, argument);");
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

        private void WriteExperimentalAttributeIfNeeded(TreeType node)
        {
            if (!node.IsInternal && !string.IsNullOrEmpty(node.Experimental))
            {
                WriteLine($"[Experimental(global::Microsoft.CodeAnalysis.RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = {QuoteString(node.Experimental)})]");
            }
        }

        private void WriteExperimentalAttributeIfNeeded(Property prop)
        {
            if (!prop.IsInternal && !prop.IsOverride && !string.IsNullOrEmpty(prop.Experimental))
            {
                WriteLine($"[Experimental(global::Microsoft.CodeAnalysis.RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = {QuoteString(prop.Experimental)})]");
            }
        }

        private string GetVisitorName(Node type)
        {
            return type.VisitorName ?? $"Visit{GetSubName(type.Name)}";
        }

        private List<Property> GetAllProperties(AbstractNode node, bool includeSkipGenerationProperties = false)
        {
            var properties = node.Properties.Where(p => !p.SkipGeneration || includeSkipGenerationProperties).ToList();

            AbstractNode? @base = node;
            while (true)
            {
                string baseName = @base.Base;
                @base = _typeMap[baseName];
                if (@base is null)
                    break;
                properties.AddRange(@base.Properties.Where(p => !p.SkipGeneration || includeSkipGenerationProperties));
            }

            return properties;
        }

        private List<Property> GetAllGeneratedIOperationProperties(AbstractNode node)
        {
            return GetAllProperties(node).Where(p => IsIOperationType(p.Type)).ToList();
        }

        private string GetSubName(string operationName) => operationName[1..^9];

        private bool IsIOperationType(string typeName)
        {
            Debug.Assert(typeName.Length > 0);
            return _typeMap.ContainsKey(GetTypeNameWithoutNullable(typeName)) ||
              (IsImmutableArray(typeName, out var innerType) && IsIOperationType(GetTypeNameWithoutNullable(innerType)));
        }

        private static string GetTypeNameWithoutNullable(string typeName) => typeName[^1] == '?' ? typeName[..^1] : typeName;

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

        private static List<string> GetPropertyOrder(Node node) => node.ChildrenOrder?.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>();

        private enum ClassType
        {
            Abstract,
            NonLazy,
            Lazy
        }
    }

    internal static class Extensions
    {
        internal static string ToCamelCase(this string name)
        {
            var camelCased = char.ToLowerInvariant(name[0]) + name.Substring(1);
            if (camelCased.IsCSharpKeyword())
            {
                return "@" + camelCased;
            }

            return camelCased;
        }

        internal static bool IsCSharpKeyword(this string name)
        {
            switch (name)
            {
                case "bool":
                case "byte":
                case "sbyte":
                case "short":
                case "ushort":
                case "int":
                case "uint":
                case "long":
                case "ulong":
                case "double":
                case "float":
                case "decimal":
                case "string":
                case "char":
                case "object":
                case "typeof":
                case "sizeof":
                case "null":
                case "true":
                case "false":
                case "if":
                case "else":
                case "while":
                case "for":
                case "foreach":
                case "do":
                case "switch":
                case "case":
                case "default":
                case "lock":
                case "try":
                case "throw":
                case "catch":
                case "finally":
                case "goto":
                case "break":
                case "continue":
                case "return":
                case "public":
                case "private":
                case "internal":
                case "protected":
                case "static":
                case "readonly":
                case "sealed":
                case "const":
                case "new":
                case "override":
                case "abstract":
                case "virtual":
                case "partial":
                case "ref":
                case "out":
                case "in":
                case "where":
                case "params":
                case "this":
                case "base":
                case "namespace":
                case "using":
                case "class":
                case "struct":
                case "interface":
                case "delegate":
                case "checked":
                case "get":
                case "set":
                case "add":
                case "remove":
                case "operator":
                case "implicit":
                case "explicit":
                case "fixed":
                case "extern":
                case "event":
                case "enum":
                case "unsafe":
                    return true;
                default:
                    return false;
            }
        }
    }
}
