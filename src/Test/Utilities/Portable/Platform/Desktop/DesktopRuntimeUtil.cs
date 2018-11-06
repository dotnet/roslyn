#if NET472
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Roslyn.Test.Utilities.Desktop
{
    public static class DesktopRuntimeUtil
    {
        /// <summary>
        /// Creates a reference to a single-module assembly or a standalone module stored in memory
        /// from a hex-encoded byte stream representing a gzipped assembly image.
        /// </summary>
        /// <param name="image">
        /// A string containing a hex-encoded byte stream representing a gzipped assembly image. 
        /// Hex digits are case-insensitive and can be separated by spaces or newlines.
        /// Cannot be null.
        /// </param>
        /// <param name="properties">Reference properties (extern aliases, type embedding, <see cref="MetadataImageKind"/>).</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="filePath">Optional path that describes the location of the metadata. The file doesn't need to exist on disk. The path is opaque to the compiler.</param>
        internal static PortableExecutableReference CreateMetadataReferenceFromHexGZipImage(
            string image,
            MetadataReferenceProperties properties = default(MetadataReferenceProperties),
            DocumentationProvider documentation = null,
            string filePath = null)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            using (var compressed = new MemoryStream(SoapHexBinary.Parse(image).Value))
            using (var gzipStream = new GZipStream(compressed, CompressionMode.Decompress))
            using (var uncompressed = new MemoryStream())
            {
                gzipStream.CopyTo(uncompressed);
                uncompressed.Position = 0;
                return MetadataReference.CreateFromStream(uncompressed, properties, documentation, filePath);
            }
        }

        internal static MetadataReference CreateReflectionEmitAssembly(Action<ModuleBuilder> create)
        {
            using (var file = new DisposableFile(extension: ".dll"))
            {
                var name = Path.GetFileName(file.Path);
                var appDomain = AppDomain.CurrentDomain;
                var assembly = appDomain.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Save, Path.GetDirectoryName(file.Path));
                var module = assembly.DefineDynamicModule(CommonTestBase.GetUniqueName(), name);
                create(module);
                assembly.Save(name);

                var image = CommonTestBase.ReadFromFile(file.Path);
                return MetadataReference.CreateFromImage(image);
            }
        }

        /// <summary>
        /// Loads given array of bytes as an assembly image using <see cref="Assembly.Load(byte[])"/> or <see cref="Assembly.ReflectionOnlyLoad(byte[])"/>.
        /// </summary>
        internal static Assembly LoadAsAssembly(string moduleName, ImmutableArray<byte> rawAssembly, bool reflectionOnly = false)
        {
            Debug.Assert(!rawAssembly.IsDefault);

            byte[] bytes = rawAssembly.ToArray();

            try
            {
                if (reflectionOnly)
                {
                    return Assembly.ReflectionOnlyLoad(bytes);
                }
                else
                {
                    return Assembly.Load(bytes);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception loading {moduleName} reflectionOnly:{reflectionOnly}", ex);
            }
        }

    }
}

#endif
