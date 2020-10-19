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
            if (ModelHasErrors(_tree))
            {
                Console.WriteLine("Encountered xml errors, not generating");
                return;
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

            void writeHeader()
            {
                WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
                WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
                WriteLine("// See the LICENSE file in the project root for more information.");
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
            if (PortedTypes.Contains(node.Name))
            {
                WriteLine("#nullable enable");
            }

            WriteComments(node.Comments, getNodeKinds(node), writeReservedRemark: true);

            WriteObsoleteIfNecessary(node.Obsolete);
            WriteLine($"{(node.IsInternal ? "internal" : "public")} interface {node.Name} : {node.Base}");
            Brace();

            foreach (var property in node.Properties)
            {
                WriteInterfaceProperty(property);
            }

            Unbrace();

            if (PortedTypes.Contains(node.Name))
            {
                WriteLine("#nullable disable");
            }

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

                    string[] separators = new[] { "\r", "\n", "\r\n" };
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
            if (prop.IsInternal)
                return;
            WriteComments(prop.Comments, operationKinds: Enumerable.Empty<string>(), writeReservedRemark: false);
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
                if (type.SkipClassGeneration)
                    continue;

                if (PortedTypes.Contains(type.Name))
                {
                    WriteClassNew(type);
                }
                else
                {
                    WriteClassOld(type);
                }
            }

            WriteLine("#endregion");
        }

        private void WriteClassNew(AbstractNode type)
        {
            WriteLine("#nullable enable");
            var allProps = GetAllProperties(type);
            bool hasSkippedProperties = !GetAllProperties(type, includeSkipGenerationProperties: true).SequenceEqual(allProps);
            var ioperationProperties = allProps.Where(p => IsIOperationType(p.Type)).ToList();
            var publicIOperationProps = ioperationProperties.Where(p => !p.IsInternal).ToList();
            string typeName = type.Name[1..];
            var lazyChildren = @"_lazyChildren";
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
                if (publicIOperationProps.Count != 0)
                {
                    WriteLine($"private IEnumerable<IOperation>? {lazyChildren};");
                }

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
                writeProperty(property, propExtensibility: string.Empty);
            }

            if (node != null)
            {
                if (publicIOperationProps.Count > 0)
                {
                    WriteLine("public override IEnumerable<IOperation> Children");
                    Brace();
                    WriteLine("get");
                    Brace();

                    WriteLine($"if ({lazyChildren} is null)");
                    Brace();
                    WriteLine($"var builder = ArrayBuilder<IOperation>.GetInstance({publicIOperationProps.Count});");
                    foreach (var prop in publicIOperationProps)
                    {
                        if (IsImmutableArray(prop.Type, out _))
                        {
                            WriteLine($"if (!{prop.Name}.IsEmpty) builder.AddRange({prop.Name});");
                        }
                        else
                        {
                            WriteLine($"if ({prop.Name} is not null) builder.Add({prop.Name});");
                        }
                    }

                    WriteLine($"Interlocked.CompareExchange(ref {lazyChildren}, builder.ToImmutableAndFree(), null);");
                    Unbrace();

                    WriteLine($"return {lazyChildren};");
                    Unbrace();
                    Unbrace();
                }
                else
                {
                    WriteLine("public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();");
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
            WriteLine("#nullable disable");

            void writeClassHeader(string extensibility, string @class, string baseType, string @interface)
            {
                WriteLine($"internal {extensibility} partial class {@class} : {baseType}, {@interface}");
                Brace();
            }

            void writeConstructor(string accessibility, string @class, IEnumerable<Property> properties, IEnumerable<Property>? baseProperties, AbstractNode type, bool hasType, bool hasConstantValue, bool multipleValidKinds)
            {
                Write($"{accessibility} {@class}(");
                foreach (var prop in properties)
                {
                    if (prop.Type == "CommonConversion")
                    {
                        Write($"IConvertibleConversion {prop.Name.ToCamelCase()}, ");
                    }
                    else
                    {
                        Write($"{prop.Type} {prop.Name.ToCamelCase()}, ");
                    }
                }

                if (type.IsAbstract || multipleValidKinds)
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
                    foreach (var prop in baseProperties)
                    {
                        Write($"{prop.Name.ToCamelCase()}, ");
                    }
                }
                Write("semanticModel, syntax, isImplicit)");

                Outdent();

                List<Property> propsToInitialize = type.Properties.Where(p => !p.SkipGeneration).ToList();

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
                        if (prop.Type == "CommonConversion")
                        {
                            WriteLine($"{prop.Name}Convertible = {prop.Name.ToCamelCase()};");
                        }
                        else
                        {
                            var initializer = IsIOperationType(prop.Type) ?
                                $"SetParentOperation({prop.Name.ToCamelCase()}, this)" :
                                prop.Name.ToCamelCase();
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
                else
                {
                    WriteLine($"public {propExtensibility}{prop.Type} {prop.Name} {{ get; }}");
                }
            }

            void writeAcceptMethods(string visitorName)
            {
                WriteLine($"public override void Accept(OperationVisitor visitor) => visitor.{visitorName}(this);");
                WriteLine($"public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.{visitorName}(this, argument);");
            }

            string getKind(Node node)
            {
                while (node.OperationKind?.Include == false)
                {
                    node = (Node?)_typeMap[node.Base] ??
                        throw new InvalidOperationException($"{node.Name} is not being included in OperationKind, but has no base type!");
                }

                if (node.OperationKind?.Entries.Count > 0)
                {
                    return node.OperationKind.Entries.Where(e => e.EditorBrowsable != false).Single().Name;
                }

                return GetSubName(node.Name);
            }
        }

        private static bool HasMultipleValidKinds(AbstractNode type)
        {
            return (type.OperationKind?.Entries?.Where(e => e.EditorBrowsable != false).Count() ?? 0) > 1;
        }

        private void WriteClassOld(AbstractNode type)
        {
            var allProps = GetAllProperties(type);
            bool hasSkippedProperties = !GetAllProperties(type, includeSkipGenerationProperties: true).SequenceEqual(allProps);
            var ioperationProperties = allProps.Where(p => IsIOperationType(p.Type)).ToList();
            var publicIOperationProps = ioperationProperties.Where(p => !p.IsInternal).ToList();
            var hasIOpChildren = ioperationProperties.Count != 0;
            var constructorAccessibility = type.IsAbstract ? "protected" : "internal";
            string typeName = type.Name[1..];

            IEnumerable<Property>? baseProperties = null;
            if (_typeMap[type.Base] is { } baseNode)
            {
                baseProperties = GetAllProperties(baseNode);
            }

            // Start by generating any necessary base classes
            if (hasIOpChildren || type.IsAbstract)
            {
                var @class = $"Base{typeName}";
                var baseType = type.Base[1..];
                if (baseType != "Operation")
                {
                    baseType = $"Base{baseType}";
                }
                else
                {
                    baseType = "OperationOld";
                }

                writeClassHeader("abstract", @class, baseType, type.Name);

                writeConstructor(constructorAccessibility, @class, allProps, baseProperties, type, ClassType.Abstract);

                foreach (var prop in type.Properties)
                {
                    if (prop.SkipGeneration)
                        continue;
                    writeProperty(prop, propExtensibility: IsIOperationType(prop.Type) ? "abstract " : string.Empty);
                }

                if (type is Node node)
                {
                    if (!node.SkipChildrenGeneration)
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

                            WriteLine("public override IEnumerable<IOperation> Children");
                            Brace();
                            WriteLine("get");
                            Brace();

                            foreach (var property in orderedProperties)
                            {
                                if (IsImmutableArray(property.Type, out _))
                                {
                                    WriteLine($"foreach (var child in {property.Name})");
                                    Brace();
                                    writeIfCheck("child");
                                    Unbrace();
                                }
                                else
                                {
                                    writeIfCheck(property.Name);
                                }

                                void writeIfCheck(string memberName)
                                {
                                    WriteLine($"if ({memberName} is object) yield return {memberName};");
                                }
                            }
                            Unbrace();
                            Unbrace();
                        }
                        else
                        {
                            WriteLine("public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();");
                        }
                    }

                    var visitorName = GetVisitorName(node);
                    writeAcceptMethods(visitorName);
                }

                Unbrace();
            }

            if (type.IsAbstract)
                return;

            // Generate the non-lazy class. Nested block to allow for duplicate variable names
            {
                var @class = typeName;
                var @base = hasIOpChildren ? @class : type.Base[1..];
                if (@base != "Operation")
                {
                    @base = $"Base{@base}";
                }
                else
                {
                    @base = "OperationOld";
                }

                writeClassHeader("sealed", @class, @base, type.Name);
                writeConstructor(
                    constructorAccessibility,
                    @class,
                    allProps,
                    hasIOpChildren ? allProps : baseProperties,
                    type,
                    ClassType.NonLazy,
                    includeKind: !hasIOpChildren);

                if (hasIOpChildren)
                {
                    foreach (var property in ioperationProperties)
                    {
                        writeProperty(property, propExtensibility: "override ");
                    }
                }
                else
                {
                    foreach (var property in type.Properties)
                    {
                        if (property.SkipGeneration)
                            continue;
                        writeProperty(property, propExtensibility: string.Empty);
                    }

                    var node = (Node)type;
                    WriteLine("public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();");
                    writeAcceptMethods(GetVisitorName(node));
                }
                Unbrace();
            }

            // Generate the lazy classes if necessary
            if (hasIOpChildren)
            {
                var @class = $"Lazy{typeName}";
                var @base = $"Base{typeName}";

                writeClassHeader("abstract", @class, @base, type.Name);

                var propertiesAndFieldNames = ioperationProperties.Select(i => (i, $"_lazy{i.Name}", $"s_unset{GetSubName(i.Type)}")).ToList();

                foreach (var (prop, name, unset) in propertiesAndFieldNames)
                {
                    var assignment = string.Empty;
                    if (!IsImmutableArray(prop.Type, out _))
                    {
                        assignment = $" = {unset}";
                    }

                    WriteLine($"private {prop.Type} {name}{assignment};");
                }

                writeConstructor(constructorAccessibility, @class, allProps, allProps, type, ClassType.Lazy, includeKind: false);

                foreach (var (prop, fieldName, unset) in propertiesAndFieldNames)
                {
                    WriteLine($"protected abstract {prop.Type} Create{prop.Name}();");
                    WriteLine($"public override {prop.Type} {prop.Name}");
                    Brace();
                    WriteLine("get");
                    Brace();
                    if (IsImmutableArray(prop.Type, out _))
                    {
                        WriteLine($"if ({fieldName}.IsDefault)");
                        Brace();
                        var localName = prop.Name.ToCamelCase();
                        WriteLine($"{prop.Type} {localName} = Create{prop.Name}();");
                        WriteLine($"SetParentOperation({localName}, this);");
                        WriteLine($"ImmutableInterlocked.InterlockedInitialize(ref {fieldName}, {localName});");
                        Unbrace();

                    }
                    else
                    {
                        WriteLine($"if ({fieldName} == {unset})");
                        Brace();
                        var localName = prop.Name.ToCamelCase();
                        WriteLine($"{prop.Type} {localName} = Create{prop.Name}();");
                        WriteLine($"SetParentOperation({localName}, this);");
                        WriteLine($"Interlocked.CompareExchange(ref {fieldName}, {localName}, {unset});");
                        Unbrace();
                    }

                    WriteLine($"return {fieldName};");
                    Unbrace();
                    Unbrace();
                }

                Unbrace();
            }

            void writeClassHeader(string extensibility, string @class, string baseType, string @interface)
            {
                WriteLine($"internal {extensibility} partial class {@class} : {baseType}, {@interface}");
                Brace();
            }

            void writeConstructor(string accessibility, string @class, IEnumerable<Property> properties, IEnumerable<Property>? baseProperties, AbstractNode type, ClassType classType, bool includeKind = true)
            {
                Write($"{accessibility} {@class}(");
                foreach (var prop in properties)
                {
                    if (classType != ClassType.NonLazy && IsIOperationType(prop.Type))
                        continue;
                    if (prop.Type == "CommonConversion")
                    {
                        Write($"IConvertibleConversion {prop.Name.ToCamelCase()}, ");
                    }
                    else
                    {
                        Write($"{prop.Type} {prop.Name.ToCamelCase()}, ");
                    }
                }

                var multipleValidKinds = (type.OperationKind?.Entries?.Where(e => e.EditorBrowsable != false).Count() ?? 0) > 1;
                if (type.IsAbstract || multipleValidKinds)
                {
                    Write("OperationKind kind, ");
                }
                Write("SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit");

                WriteLine(")");
                Indent();
                Write(": base(");

                var @base = _typeMap[type.Base];
                if (baseProperties is object)
                {
                    foreach (var prop in baseProperties.Where(p => !IsIOperationType(p.Type)))
                    {
                        Write($"{prop.Name.ToCamelCase()}, ");
                    }
                }

                var kind = type switch
                {
                    { IsAbstract: true } => "kind",
                    { } when multipleValidKinds => "kind",
                    { IsInternal: true } => "OperationKind.None",
                    _ => $"OperationKind.{getKind(type!)}"
                };
                Debug.Assert(type is object);
                Write($"{(includeKind || multipleValidKinds ? $"{kind}, " : string.Empty)}semanticModel, syntax, type, constantValue, isImplicit)");

                Outdent();

                // Lazy constructors never initialize anything
                if (classType == ClassType.Lazy)
                {
                    WriteLine("{ }");
                    return;
                }

                // For leaf types, we need to initialize all IOperations from any parent classes, and our own. If this class has no IOperation
                // children, then that means there's no lazy version of the class, and we need to initialize all the properties defined on this
                // interface instead.
                var propsAreIOperations = true;
                List<Property> propsToInitialize;
                if (classType == ClassType.NonLazy)
                {
                    propsToInitialize = GetAllGeneratedIOperationProperties(type);
                    if (propsToInitialize.Count == 0)
                    {
                        propsToInitialize = type.Properties.Where(p => !p.SkipGeneration).ToList();
                        propsAreIOperations = false;
                    }
                }
                else
                {
                    propsToInitialize = type.Properties.Where(p => !IsIOperationType(p.Type) && !p.SkipGeneration).ToList();
                    propsAreIOperations = false;
                }

                if (propsToInitialize.Count == 0)
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
                        if (prop.Type == "CommonConversion")
                        {
                            WriteLine($"{prop.Name}Convertible = {prop.Name.ToCamelCase()};");
                        }
                        else
                        {
                            var initializer = propsAreIOperations ?
                                $"SetParentOperation({prop.Name.ToCamelCase()}, this)" :
                                prop.Name.ToCamelCase();
                            WriteLine($"{prop.Name} = {initializer};");
                        }
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
                else
                {
                    WriteLine($"public {propExtensibility}{prop.Type} {prop.Name} {{ get; }}");
                }
            }

            void writeAcceptMethods(string visitorName)
            {
                WriteLine($"public override void Accept(OperationVisitor visitor) => visitor.{visitorName}(this);");
                WriteLine($"public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.{visitorName}(this, argument);");

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

        private void WriteCloner()
        {
            WriteLine("#region Cloner");
            WriteLine("#nullable enable");

            WriteLine(@"internal sealed partial class OperationCloner : OperationVisitor<object?, IOperation>");
            Brace();

            WriteLine("private static readonly OperationCloner s_instance = new OperationCloner();");
            WriteLine("/// <summary>Deep clone given IOperation</summary>");
            WriteLine("public static T CloneOperation<T>(T operation) where T : IOperation => s_instance.Visit(operation);");
            WriteLine("public OperationCloner() { }");
            WriteLine("private T Visit<T>(T node) where T : IOperation? => (T)Visit(node, argument: null);");
            WriteLine("public override IOperation DefaultVisit(IOperation operation, object? argument) => throw ExceptionUtilities.Unreachable;");
            WriteLine("private ImmutableArray<T> VisitArray<T>(ImmutableArray<T> nodes) where T : IOperation => nodes.SelectAsArray((n, @this) => @this.Visit(n), this);");
            WriteLine("private ImmutableArray<(ISymbol, T)> VisitArray<T>(ImmutableArray<(ISymbol, T)> nodes) where T : IOperation => nodes.SelectAsArray((n, @this) => (n.Item1, @this.Visit(n.Item2)), this);");

            foreach (var node in _tree.Types.OfType<Node>())
            {
                const string internalName = "internalOperation";

                if (!PortedTypes.Contains(node.Name))
                {
                    continue;
                }

                string nameMinusI = node.Name[1..];
                WriteLine($"{(node.IsInternal ? "internal" : "public")} override IOperation {GetVisitorName(node)}({node.Name} operation, object? argument)");
                Brace();
                WriteLine($"var {internalName} = ({nameMinusI})operation;");
                Write($"return new {nameMinusI}(");

                foreach (var prop in GetAllProperties(node))
                {
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

            WriteLine("#nullable disable");
            WriteLine("#endregion");
            WriteLine("");
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
                if (type.SkipInVisitor)
                    continue;

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
                if (type.SkipInVisitor)
                    continue;

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
            return _typeMap.ContainsKey(getTypeName(typeName)) ||
              (IsImmutableArray(typeName, out var innerType) && IsIOperationType(getTypeName(innerType)));

            static string getTypeName(string typeName) => typeName[^1] == '?' ? typeName[..^1] : typeName;
        }

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
