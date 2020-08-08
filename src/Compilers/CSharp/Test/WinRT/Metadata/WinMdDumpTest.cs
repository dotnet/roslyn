// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Resources.Proprietary;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata
{
    public class WinMdDumpTest : CSharpTestBase
    {
        private readonly MetadataReference _windowsRef = MetadataReference.CreateFromImage(TestResources.WinRt.Windows.AsImmutableOrNull());
        private readonly MetadataReference _systemRuntimeRef = MetadataReference.CreateFromImage(TestMetadata.ResourcesNet451.SystemRuntime.AsImmutableOrNull());
        private readonly MetadataReference _systemObjectModelRef = MetadataReference.CreateFromImage(TestMetadata.ResourcesNet451.SystemObjectModel.AsImmutableOrNull());
        private readonly MetadataReference _windowsRuntimeUIXamlRef = MetadataReference.CreateFromImage(ProprietaryTestResources.v4_0_30319_17929.System_Runtime_WindowsRuntime_UI_Xaml.AsImmutableOrNull());
        private readonly MetadataReference _interopServicesWindowsRuntimeRef = MetadataReference.CreateFromImage(TestMetadata.ResourcesNet451.SystemRuntimeInteropServicesWindowsRuntime.AsImmutableOrNull());

        private void AppendMembers(StringBuilder result, NamespaceOrTypeSymbol container, string indent)
        {
            string memberIndent;
            if (container is NamedTypeSymbol)
            {
                memberIndent = indent + "  ";

                result.Append(indent);
                result.AppendLine("{");

                AppendCustomAttributes(result, container, indent, inBlock: true);

                if (container.GetAttributes().Length > 0)
                {
                    result.AppendLine();
                }
            }
            else
            {
                memberIndent = indent;
            }


            foreach (var member in container.GetMembers().OrderBy(m => m.Name, System.StringComparer.InvariantCulture))
            {
                switch (member.Kind)
                {
                    case SymbolKind.NamedType:
                        var namedType = (PENamedTypeSymbol)member;
                        result.Append(memberIndent);
                        result.Append(".class ");
                        MetadataSignatureHelper.AppendTypeAttributes(result, namedType.Flags);
                        result.Append(" ");
                        result.Append(member);

                        if ((object)namedType.BaseType() != null)
                        {
                            result.AppendLine();
                            result.Append(memberIndent);
                            result.Append("       extends ");
                            result.Append(namedType.BaseType());
                        }

                        if (namedType.Interfaces().Length > 0)
                        {
                            result.AppendLine();
                            result.Append(memberIndent);
                            result.Append("       implements ");
                            result.Append(string.Join(", ", namedType.Interfaces()));
                        }

                        result.AppendLine();

                        AppendMembers(result, namedType, memberIndent);
                        break;

                    case SymbolKind.Namespace:
                        var ns = member as PENamespaceSymbol;
                        if ((object)ns != null)
                        {
                            AppendMembers(result, ns, indent);
                        }
                        break;

                    case SymbolKind.Method:
                        var method = member as PEMethodSymbol;
                        if ((object)method != null && method.AssociatedSymbol == null)
                        {
                            result.Append(memberIndent);
                            result.Append(".method ");
                            AppendMethod(result, method, memberIndent);

                            AppendCustomAttributes(result, member, memberIndent, inBlock: false);
                        }
                        break;

                    case SymbolKind.Field:
                        var field = (PEFieldSymbol)member;
                        result.Append(memberIndent);
                        result.Append(".field ");

                        MetadataSignatureHelper.AppendFieldAttributes(result, field.Flags);
                        result.Append(" ");

                        result.Append(field.TypeWithAnnotations);
                        result.Append(" ");
                        result.Append(member.Name);
                        result.AppendLine();

                        AppendCustomAttributes(result, member, memberIndent, inBlock: false);
                        break;

                    case SymbolKind.Property:
                        var property = (PEPropertySymbol)member;
                        string propertyName;

                        result.Append(memberIndent);
                        result.Append(".property ");

                        PropertyAttributes propertyAttrs;
                        ((PEModuleSymbol)container.ContainingModule).Module.GetPropertyDefPropsOrThrow(property.Handle, out propertyName, out propertyAttrs);
                        if (MetadataSignatureHelper.AppendPropertyAttributes(result, propertyAttrs))
                        {
                            result.Append(" ");
                        }

                        result.Append(property.TypeWithAnnotations);
                        result.Append(" ");
                        result.Append(property.Name);
                        result.AppendLine();

                        result.Append(memberIndent);
                        result.AppendLine("{");

                        AppendCustomAttributes(result, member, memberIndent, inBlock: true);

                        if (property.GetMethod != null)
                        {
                            result.Append(memberIndent);
                            result.Append("  .get ");
                            AppendMethod(result, (PEMethodSymbol)property.GetMethod, memberIndent);
                        }

                        if (property.SetMethod != null)
                        {
                            result.Append(memberIndent);
                            result.Append("  .set ");
                            AppendMethod(result, (PEMethodSymbol)property.SetMethod, memberIndent);
                        }

                        result.Append(memberIndent);
                        result.AppendLine("}");
                        break;

                    case SymbolKind.Event:
                        var evnt = (PEEventSymbol)member;

                        result.Append(memberIndent);
                        result.Append(".event ");

                        string eventName;
                        EventAttributes eventAttrs;
                        EntityHandle type;
                        ((PEModuleSymbol)container.ContainingModule).Module.GetEventDefPropsOrThrow(evnt.Handle, out eventName, out eventAttrs, out type);

                        if (MetadataSignatureHelper.AppendEventAttributes(result, eventAttrs))
                        {
                            result.Append(" ");
                        }

                        result.Append(evnt.TypeWithAnnotations);
                        result.Append(" ");
                        result.Append(evnt.Name);
                        result.AppendLine();

                        result.Append(memberIndent);
                        result.Append("{");
                        result.AppendLine();

                        AppendCustomAttributes(result, member, memberIndent, inBlock: true);

                        if (evnt.RemoveMethod != null)
                        {
                            result.Append(memberIndent);
                            result.Append("  .removeon ");
                            AppendMethod(result, (PEMethodSymbol)evnt.RemoveMethod, memberIndent);
                        }

                        if (evnt.AddMethod != null)
                        {
                            result.Append(memberIndent);
                            result.Append("  .addon ");
                            AppendMethod(result, (PEMethodSymbol)evnt.AddMethod, memberIndent);
                        }

                        result.Append(memberIndent);
                        result.AppendLine("}");
                        break;
                }
            }

            if (container is NamedTypeSymbol)
            {
                result.Append(indent);
                result.AppendLine("}");
            }
        }

        private static void AppendCustomAttributes(StringBuilder result, Symbol symbol, string indent, bool inBlock)
        {
            var attributes = symbol.GetAttributes();
            if (attributes.Length == 0)
            {
                return;
            }

            if (!inBlock)
            {
                result.Append(indent);
                result.AppendLine("{");
            }

            string memberIndent = indent + "  ";

            foreach (var attribute in attributes)
            {
                result.Append(memberIndent);
                result.Append(".custom ");
                if (attribute.AttributeConstructor == null)
                {
                    result.Append("[Missing: ");
                    result.Append(attribute.AttributeClass);
                    result.Append("]");
                }
                else
                {
                    AppendMethod(result, (PEMethodSymbol)attribute.AttributeConstructor, indent: null, includeTypeName: true);
                }

                result.Append(" = (");

                int i = 0;
                foreach (var arg in attribute.ConstructorArguments)
                {
                    if (i > 0)
                    {
                        result.Append(", ");
                    }

                    AppendConstant(result, arg);
                    i++;
                }

                foreach (var arg in attribute.NamedArguments)
                {
                    if (i > 0)
                    {
                        result.Append(", ");
                    }

                    result.Append(arg.Key);
                    result.Append(" = ");
                    AppendConstant(result, arg.Value);
                    i++;
                }

                result.Append(")");
                result.AppendLine();
            }

            if (!inBlock)
            {
                result.Append(indent);
                result.AppendLine("}");
            }
        }

        private static void AppendConstant(StringBuilder result, TypedConstant constant)
        {
            switch (constant.Kind)
            {
                case TypedConstantKind.Array:
                    result.Append("{");
                    int i = 0;

                    foreach (var item in constant.Values)
                    {
                        if (i > 0)
                        {
                            result.Append(", ");
                        }

                        AppendConstant(result, item);
                    }

                    result.Append("}");
                    break;

                case TypedConstantKind.Type:
                    result.Append("typeof(");
                    result.Append(constant.Value);
                    result.Append(")");
                    break;

                case TypedConstantKind.Enum:
                case TypedConstantKind.Primitive:
                    var value = constant.Value;
                    if (value.GetType() == typeof(string))
                    {
                        result.Append("\"");
                        result.Append(value);
                        result.Append("\"");
                    }
                    else if (value.GetType() == typeof(bool))
                    {
                        result.Append(value.ToString().ToLower());
                    }
                    else
                    {
                        result.Append(value);
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(constant.Kind);
            }
        }

        private static void AppendMethod(StringBuilder result, PEMethodSymbol method, string indent, bool includeTypeName = false)
        {
            MetadataSignatureHelper.AppendMethodAttributes(result, method.Flags);
            result.Append(" ");
            AppendSignatureType(result, method.ReturnType, RefKind.None);
            result.Append(" ");

            if (includeTypeName)
            {
                result.Append(method.ContainingType);
                result.Append("::");
            }

            result.Append(method.Name);

            result.Append("(");

            bool hasParameterAttributes = false;
            int i = 0;
            foreach (PEParameterSymbol parameter in method.Parameters)
            {
                if (i > 0)
                {
                    result.Append(", ");
                }

                if (parameter.GetAttributes().Length > 0)
                {
                    hasParameterAttributes = true;
                }

                if (MetadataSignatureHelper.AppendParameterAttributes(result, parameter.Flags, all: true))
                {
                    result.Append(" ");
                }

                AppendSignatureType(result, parameter.Type, parameter.RefKind);
                result.Append(" ");
                result.Append(parameter.Name);
                i++;
            }

            result.Append(") ");
            MetadataSignatureHelper.AppendMethodImplAttributes(result, method.ImplementationAttributes);

            if (indent != null)
            {
                result.AppendLine();

                if (hasParameterAttributes)
                {
                    result.Append(indent);
                    result.AppendLine("{");

                    string memberIndent = indent + "  ";

                    i = 1;
                    foreach (PEParameterSymbol parameter in method.Parameters)
                    {
                        if (parameter.GetAttributes().Length > 0)
                        {
                            result.Append(memberIndent);
                            result.AppendFormat(".param [{0}]", i);
                            result.AppendLine();

                            AppendCustomAttributes(result, parameter, indent, inBlock: true);
                        }

                        i++;
                    }

                    result.Append(indent);
                    result.AppendLine("}");
                }
            }
        }

        private static void AppendSignatureType(StringBuilder result, TypeSymbol type, RefKind refKind)
        {
            result.Append(type);

            if (refKind != RefKind.None)
            {
                result.Append("&");
            }
        }

        private void AppendAssemblyRefs(StringBuilder result, PEAssemblySymbol assembly)
        {
            foreach (var a in assembly.PrimaryModule.GetReferencedAssemblies())
            {
                result.Append(".assembly extern ");
                result.AppendLine(a.GetDisplayName(fullKey: true));
            }
        }

        private string Dump(MetadataReference winmd, MetadataReference[] additionalRefs = null)
        {
            IEnumerable<MetadataReference> references = new[] { MscorlibRef_v4_0_30316_17626, _systemRuntimeRef, _systemObjectModelRef, _windowsRuntimeUIXamlRef, _interopServicesWindowsRuntimeRef, winmd };

            if (additionalRefs != null)
            {
                references = references.Concat(additionalRefs);
            }

            var comp = CreateEmptyCompilation("", references, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var writer = new StringBuilder();
            AppendAssemblyRefs(writer, (PEAssemblySymbol)comp.GetReferencedAssemblySymbol(winmd));

            AppendMembers(writer, comp.GetReferencedAssemblySymbol(winmd).GlobalNamespace, "");
            return writer.ToString();
        }

        private void AssertDumpsEqual(string expected, string actual)
        {
            if (expected != actual)
            {
                string fileExpected = Path.Combine(Path.GetTempPath(), "roslyn_winmd_dump.expected.txt");
                string fileActual = Path.Combine(Path.GetTempPath(), "roslyn_winmd_dump.actual.txt");
                File.WriteAllText(fileExpected, expected);
                File.WriteAllText(fileActual, actual);
                Assert.True(false, "Dump is different. To investigate compare files:\r\n\"" + fileExpected + "\" \"" + fileActual + "\"");
            }
        }

        [Fact]
        public void DumpWindowsWinMD()
        {
            var expected = Encoding.UTF8.GetString(TestResources.WinRt.Windows_dump);
            var actual = Dump(_windowsRef);
            AssertDumpsEqual(expected, actual);
        }

        [Fact]
        public void DumpWinMDPrefixing()
        {
            var winmd = MetadataReference.CreateFromImage(TestResources.WinRt.WinMDPrefixing.AsImmutableOrNull());
            var actual = Dump(winmd, new[] { _windowsRef });
            var expected = Encoding.UTF8.GetString(TestResources.WinRt.WinMDPrefixing_dump);
            AssertDumpsEqual(expected, actual);
        }
    }
}
