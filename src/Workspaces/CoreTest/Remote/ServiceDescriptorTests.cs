// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.VisualBasic.CodeGeneration;
using Microsoft.CodeAnalysis.VisualBasic.CodeStyle;
using Microsoft.CodeAnalysis.VisualBasic.Formatting;
using Microsoft.CodeAnalysis.VisualBasic.Simplification;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.UnitTests
{
    [UseExportProvider]
    public class ServiceDescriptorTests
    {
        public static IEnumerable<object[]> AllServiceDescriptors
            => ServiceDescriptors.Instance.GetTestAccessor().Descriptors
                .Select(descriptor => new object[] { descriptor.Key, descriptor.Value.descriptorCoreClr64, descriptor.Value.descriptorCoreClr64ServerGC });

        private static Dictionary<Type, MemberInfo> GetAllParameterTypesOfRemoteApis()
        {
            var interfaces = new List<Type>();

            foreach (var (serviceType, (descriptor, _)) in ServiceDescriptors.Instance.GetTestAccessor().Descriptors)
            {
                interfaces.Add(serviceType);
                if (descriptor.ClientInterface != null)
                {
                    interfaces.Add(descriptor.ClientInterface);
                }
            }

            var types = new Dictionary<Type, MemberInfo>();

            void AddTypeRecursive(Type type, MemberInfo declaringMember)
            {
                if (type.IsArray)
                {
                    type = type.GetElementType();
                }

                if (types.ContainsKey(type))
                {
                    return;
                }

                types.Add(type, declaringMember);

                if (type.IsGenericType)
                {
                    // Immutable collections and tuples have custom formatters which would fail during serialization if 
                    // formatters were not available for the element types.
                    if (type.Namespace == typeof(ImmutableArray<>).Namespace ||
                        type.GetGenericTypeDefinition() == typeof(Nullable<>) ||
                        type.Namespace == "System" && type.Name.StartsWith("ValueTuple", StringComparison.Ordinal) ||
                        type.Namespace == "System" && type.Name.StartsWith("Tuple", StringComparison.Ordinal))
                    {
                        foreach (var genericArgument in type.GetGenericArguments())
                        {
                            AddTypeRecursive(genericArgument, declaringMember);
                        }
                    }
                }

                foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.GetCustomAttributes<DataMemberAttribute>().Any())
                    {
                        AddTypeRecursive(field.FieldType, type);
                    }
                }

                foreach (var property in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.GetCustomAttributes<DataMemberAttribute>().Any())
                    {
                        AddTypeRecursive(property.PropertyType, type);
                    }
                }
            }

            foreach (var interfaceType in interfaces)
            {
                foreach (var method in interfaceType.GetMethods())
                {
                    if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                    {
                        AddTypeRecursive(method.ReturnType.GetGenericArguments().Single(), method);
                    }
                    else
                    {
                        // remote API must return ValueTask or ValueTask<T>
                        Assert.Equal(typeof(ValueTask), method.ReturnType);
                    }

                    foreach (var type in method.GetParameters().Select(p => p.ParameterType))
                    {
                        // types that are special cased by JSON-RPC for streaming APIs
                        if (type != typeof(Stream) &&
                            type != typeof(IDuplexPipe) &&
                            type != typeof(PipeReader) &&
                            type != typeof(PipeWriter))
                        {
                            AddTypeRecursive(type, method);
                        }
                    }
                }
            }

            types.Remove(typeof(CancellationToken));

            return types;
        }

        public static IEnumerable<object[]> GetEncodingTestCases()
            => EncodingTestHelpers.GetEncodingTestCases();

        [Theory]
        [MemberData(nameof(GetEncodingTestCases))]
        public void EncodingIsMessagePackSerializable(Encoding original)
        {
            var messagePackOptions = MessagePackSerializerOptions.Standard.WithResolver(MessagePackFormatters.DefaultResolver);

            using var stream = new MemoryStream();
            MessagePackSerializer.Serialize(stream, original, messagePackOptions);
            stream.Position = 0;

            var deserialized = (Encoding)MessagePackSerializer.Deserialize(typeof(Encoding), stream, messagePackOptions);
            EncodingTestHelpers.AssertEncodingsEqual(original, deserialized);
        }

        private sealed class TestEncoderFallback : EncoderFallback
        {
            public override int MaxCharCount => throw new NotImplementedException();
            public override EncoderFallbackBuffer CreateFallbackBuffer() => throw new NotImplementedException();
        }

        private sealed class TestDecoderFallback : DecoderFallback
        {
            public override int MaxCharCount => throw new NotImplementedException();
            public override DecoderFallbackBuffer CreateFallbackBuffer() => throw new NotImplementedException();
        }

        [Fact]
        public void EncodingIsMessagePackSerializable_WithCustomFallbacks()
        {
            var messagePackOptions = MessagePackSerializerOptions.Standard.WithResolver(MessagePackFormatters.DefaultResolver);

            var original = Encoding.GetEncoding(Encoding.ASCII.CodePage, new TestEncoderFallback(), new TestDecoderFallback());

            using var stream = new MemoryStream();
            MessagePackSerializer.Serialize(stream, original, messagePackOptions);
            stream.Position = 0;

            var deserialized = (Encoding)MessagePackSerializer.Deserialize(typeof(Encoding), stream, messagePackOptions);
            Assert.NotEqual(original, deserialized);

            // original throws from the custom fallback, deserialized has the default fallback:
            Assert.Throws<NotImplementedException>(() => original.GetBytes("\u1234"));
            AssertEx.Equal(new byte[] { 0x3f }, deserialized.GetBytes("\u1234"));
        }

        [Fact]
        public void OptionsAreMessagePackSerializable_LanguageAgnostic()
        {
            var messagePackOptions = MessagePackSerializerOptions.Standard.WithResolver(MessagePackFormatters.DefaultResolver);
            var options = new object[]
            {
                AddImportPlacementOptions.Default,
                LineFormattingOptions.Default,
                DocumentFormattingOptions.Default,
                HighlightingOptions.Default,
                DocumentationCommentOptions.Default
            };

            foreach (var original in options)
            {
                using var stream = new MemoryStream();
                MessagePackSerializer.Serialize(stream, original, messagePackOptions);
                stream.Position = 0;

                var deserialized = MessagePackSerializer.Deserialize(original.GetType(), stream, messagePackOptions);
                Assert.Equal(original, deserialized);
            }
        }

        [Theory]
        [InlineData(LanguageNames.CSharp)]
        [InlineData(LanguageNames.VisualBasic)]
        public void OptionsAreMessagePackSerializable(string language)
        {
            var messagePackOptions = MessagePackSerializerOptions.Standard.WithResolver(MessagePackFormatters.DefaultResolver);

            using var workspace = new AdhocWorkspace();
            var languageServices = workspace.Services.SolutionServices.GetLanguageServices(language);

            var options = new object[]
            {
                SimplifierOptions.GetDefault(languageServices),
                SyntaxFormattingOptions.GetDefault(languageServices),
                CodeCleanupOptions.GetDefault(languageServices),
                CodeGenerationOptions.GetDefault(languageServices),
                CodeActionOptions.Default,
                IndentationOptions.GetDefault(languageServices),
                ExtractMethodGenerationOptions.GetDefault(languageServices),

                // some non-default values:

                new CSharpSyntaxFormattingOptions()
                {
                    AccessibilityModifiersRequired = AccessibilityModifiersRequired.Always,
                    Indentation = IndentationPlacement.SwitchSection
                },

                new CSharpSimplifierOptions()
                {
                    QualifyFieldAccess = new CodeStyleOption2<bool>(true, NotificationOption2.Error)
                },

                new CSharpCodeGenerationOptions()
                {
                    NamingStyle = OptionsTestHelpers.GetNonDefaultNamingStylePreference(),
                    PreferExpressionBodiedIndexers = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.Error)
                },

                new CSharpSyntaxFormattingOptions()
                {
                    AccessibilityModifiersRequired = AccessibilityModifiersRequired.Always,
                    NewLines = NewLinePlacement.BeforeFinally
                },

                new VisualBasicSyntaxFormattingOptions()
                {
                    AccessibilityModifiersRequired = AccessibilityModifiersRequired.Always
                },

                new VisualBasicSimplifierOptions()
                {
                    QualifyFieldAccess = new CodeStyleOption2<bool>(true, NotificationOption2.Error)
                },

                new VisualBasicCodeGenerationOptions()
                {
                    NamingStyle = OptionsTestHelpers.GetNonDefaultNamingStylePreference()
                },
            };

            foreach (var original in options)
            {
                using var stream = new MemoryStream();
                MessagePackSerializer.Serialize(stream, original, messagePackOptions);
                stream.Position = 0;

                var deserialized = MessagePackSerializer.Deserialize(original.GetType(), stream, messagePackOptions);
                Assert.Equal(original, deserialized);
            }
        }

        [Fact]
        public void TypesUsedInRemoteApisMustBeMessagePackSerializable()
        {
            var types = GetAllParameterTypesOfRemoteApis();
            var resolver = MessagePackFormatters.DefaultResolver;

            var errors = new List<string>();

            foreach (var (type, declaringMember) in types)
            {
                try
                {
                    if (resolver.GetFormatterDynamic(type) == null)
                    {
                        errors.Add($"{type} referenced by {declaringMember} is not serializable");
                    }
                }
                catch (Exception e)
                {
                    // Known issues:
                    // Internal enums need a custom formatter: https://github.com/neuecc/MessagePack-CSharp/issues/1025
                    // This test fails with "... is attempting to implement an inaccessible interface." error message.
                    if (type.IsEnum && type.IsNotPublic ||
                        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                        type.GetGenericArguments().Single().IsEnum && type.GetGenericArguments().Single().IsNotPublic)
                    {
                        errors.Add($"{type} referenced by {declaringMember} is an internal enum and needs a custom formatter");
                    }
                    else if (type.IsAbstract)
                    {
                        // custom abstract types must be explicitly listed in MessagePackFormatters.AbstractTypeFormatters
                        if (!MessagePackFormatters.Formatters.Any(
                            formatter => formatter.GetType() is { IsGenericType: true } and var formatterType &&
                                         formatterType.GetGenericTypeDefinition() == typeof(ForceTypelessFormatter<>) &&
                                         formatterType.GenericTypeArguments[0] == type))
                        {
                            errors.Add($"{type} referenced by {declaringMember} is abstract but ForceTypelessFormatter<{type}> is not listed in {nameof(MessagePackFormatters)}.{nameof(MessagePackFormatters.Formatters)}");
                        }

                        continue;
                    }
                    else
                    {
                        errors.Add($"{type} referenced by {declaringMember} failed to serialize with exception: {e}");
                    }
                }
            }

            AssertEx.Empty(errors, "Types are not MessagePack-serializable");
        }

        [Theory]
        [MemberData(nameof(AllServiceDescriptors))]
        internal void GetFeatureDisplayName(
            Type serviceInterface,
            ServiceDescriptor descriptorCoreClr64,
            ServiceDescriptor descriptorCoreClr64ServerGC)
        {
            Assert.NotNull(serviceInterface);

            var expectedName = descriptorCoreClr64.GetFeatureDisplayName();

            // The service name couldn't be found. It may need to be added to RemoteWorkspacesResources.resx as FeatureName_{name}
            Assert.False(string.IsNullOrEmpty(expectedName), $"Service name for '{serviceInterface.GetType()}' not available.");

            Assert.Equal(expectedName, descriptorCoreClr64ServerGC.GetFeatureDisplayName());
        }

        [Fact]
        public void CallbackDispatchers()
        {
            var hostServices = FeaturesTestCompositions.Features.WithTestHostParts(Testing.TestHost.OutOfProcess).GetHostServices();
            var callbackDispatchers = ((IMefHostExportProvider)hostServices).GetExports<IRemoteServiceCallbackDispatcher, RemoteServiceCallbackDispatcherRegistry.ExportMetadata>();

            var descriptorsWithCallbackServiceTypes = ServiceDescriptors.Instance.GetTestAccessor().Descriptors
                .Where(d => d.Value.descriptorCoreClr64.ClientInterface != null).Select(d => d.Key);

            var callbackDispatcherServiceTypes = callbackDispatchers.Select(d => d.Metadata.ServiceInterface);
            AssertEx.SetEqual(descriptorsWithCallbackServiceTypes, callbackDispatcherServiceTypes);
        }
    }
}
