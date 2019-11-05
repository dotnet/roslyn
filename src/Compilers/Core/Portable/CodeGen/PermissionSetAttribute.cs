// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// This class represents the PermissionSetAttribute specified in source which needs fixup during codegen.
    /// </summary>
    /// <remarks>
    /// PermissionSetAttribute needs fixup when it contains an assignment to the 'File' property as a single named attribute argument.
    /// Fixup performed is ported from SecurityAttributes::FixUpPermissionSetAttribute at ndp\clr\src\vm\securityattributes.cpp.
    /// It involves following steps:
    /// 1) Verifying that the specified file name resolves to a valid path: This is done during binding.
    /// 2) Reading the contents of the file into a byte array.
    /// 3) Convert each byte in the file content into two bytes containing hexadecimal characters (see method <see cref="ConvertToHex"/>).
    /// 4) Replacing the 'File = fileName' named argument with 'Hex = hexFileContent' argument, where hexFileContent is the converted output from step 3) above.
    /// </remarks>
    internal class PermissionSetAttributeWithFileReference : Cci.ICustomAttribute
    {
        private readonly Cci.ICustomAttribute _sourceAttribute;
        private readonly string _resolvedPermissionSetFilePath;
        internal const string FilePropertyName = "File";
        internal const string HexPropertyName = "Hex";

        public PermissionSetAttributeWithFileReference(Cci.ICustomAttribute sourceAttribute, string resolvedPermissionSetFilePath)
        {
            RoslynDebug.Assert(resolvedPermissionSetFilePath != null);

            _sourceAttribute = sourceAttribute;
            _resolvedPermissionSetFilePath = resolvedPermissionSetFilePath;
        }

        /// <summary>
        /// Zero or more positional arguments for the attribute constructor.
        /// </summary>
        public ImmutableArray<Cci.IMetadataExpression> GetArguments(EmitContext context)
        {
            return _sourceAttribute.GetArguments(context);
        }

        /// <summary>
        /// A reference to the constructor that will be used to instantiate this custom attribute during execution (if the attribute is inspected via Reflection).
        /// </summary>
        public Cci.IMethodReference Constructor(EmitContext context, bool reportDiagnostics)
            => _sourceAttribute.Constructor(context, reportDiagnostics);

        /// <summary>
        /// Zero or more named arguments that specify values for fields and properties of the attribute.
        /// </summary>
        public ImmutableArray<Cci.IMetadataNamedArgument> GetNamedArguments(EmitContext context)
        {
            // Perform fixup 
            Cci.ITypeReference stringType = context.Module.GetPlatformType(Cci.PlatformType.SystemString, context);

#if DEBUG
            // Must have exactly 1 named argument.
            var namedArgs = _sourceAttribute.GetNamedArguments(context);
            Debug.Assert(namedArgs.Length == 1);

            // Named argument must be 'File' property of string type
            var fileArg = namedArgs.First();
            Debug.Assert(fileArg.ArgumentName == FilePropertyName);
            Debug.Assert(context.Module.IsPlatformType(fileArg.Type, Cci.PlatformType.SystemString));

            // Named argument value must be a non-empty string
            Debug.Assert(fileArg.ArgumentValue is MetadataConstant);
            var fileName = (string?)((MetadataConstant)fileArg.ArgumentValue).Value;
            Debug.Assert(!String.IsNullOrEmpty(fileName));

            // PermissionSetAttribute type must have a writable public string type property member 'Hex'
            ISymbol iSymbol = ((ISymbolInternal)_sourceAttribute.GetType(context)).GetISymbol();
            Debug.Assert(((INamedTypeSymbol)iSymbol).GetMembers(HexPropertyName).Any(
                member => member.Kind == SymbolKind.Property && ((IPropertySymbol)member).Type.SpecialType == SpecialType.System_String));
#endif

            string hexFileContent;

            // Read the file contents at the resolved file path into a byte array.
            // May throw PermissionSetFileReadException, which is handled in Compilation.Emit.
            var resolver = context.Module.CommonCompilation.Options.XmlReferenceResolver;

            // If the resolver isn't available we won't get here since we had to use it to resolve the path.
            RoslynDebug.Assert(resolver != null);

            try
            {
                using (Stream stream = resolver.OpenReadChecked(_resolvedPermissionSetFilePath))
                {
                    // Convert the byte array contents into a string in hexadecimal format.
                    hexFileContent = ConvertToHex(stream);
                }
            }
            catch (IOException e)
            {
                throw new PermissionSetFileReadException(e.Message, _resolvedPermissionSetFilePath);
            }

            // Synthesize a named attribute argument "Hex = hexFileContent".
            return ImmutableArray.Create<Cci.IMetadataNamedArgument>(new HexPropertyMetadataNamedArgument(stringType, new MetadataConstant(stringType, hexFileContent)));
        }

        // internal for testing purposes.
        internal static string ConvertToHex(Stream stream)
        {
            RoslynDebug.Assert(stream != null);

            var pooledStrBuilder = PooledStringBuilder.GetInstance();
            StringBuilder stringBuilder = pooledStrBuilder.Builder;

            int b;
            while ((b = stream.ReadByte()) >= 0)
            {
                stringBuilder.Append(ConvertHexToChar((b >> 4) & 0xf));
                stringBuilder.Append(ConvertHexToChar(b & 0xf));
            }

            return pooledStrBuilder.ToStringAndFree();
        }

        private static char ConvertHexToChar(int b)
        {
            Debug.Assert(b < 0x10);
            return (char)(b < 10 ? '0' + b : 'a' + b - 10);
        }

        /// <summary>
        /// The number of positional arguments.
        /// </summary>
        public int ArgumentCount => _sourceAttribute.ArgumentCount;

        /// <summary>
        /// The number of named arguments.
        /// </summary>
        public ushort NamedArgumentCount
        {
            get
            {
                Debug.Assert(_sourceAttribute.NamedArgumentCount == 1);
                return 1;
            }
        }

        /// <summary>
        /// The type of the attribute. For example System.AttributeUsageAttribute.
        /// </summary>
        public Cci.ITypeReference GetType(EmitContext context) => _sourceAttribute.GetType(context);

        public bool AllowMultiple => _sourceAttribute.AllowMultiple;

        private struct HexPropertyMetadataNamedArgument : Cci.IMetadataNamedArgument
        {
            private readonly Cci.ITypeReference _type;
            private readonly Cci.IMetadataExpression _value;

            public HexPropertyMetadataNamedArgument(Cci.ITypeReference type, Cci.IMetadataExpression value)
            {
                _type = type;
                _value = value;
            }

            public string ArgumentName { get { return HexPropertyName; } }
            public Cci.IMetadataExpression ArgumentValue { get { return _value; } }
            public bool IsField { get { return false; } }

            Cci.ITypeReference Cci.IMetadataExpression.Type { get { return _type; } }

            void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor)
            {
                visitor.Visit(this);
            }
        }
    }

    /// <summary>
    /// Exception class to enable generating ERR_PermissionSetAttributeFileReadError while reading the file for PermissionSetAttribute fixup.
    /// </summary>
    internal class PermissionSetFileReadException : Exception
    {
        private readonly string _file;

        public PermissionSetFileReadException(string message, string file)
            : base(message)
        {
            _file = file;
        }

        public string FileName => _file;

        public string PropertyName => PermissionSetAttributeWithFileReference.FilePropertyName;
    }
}
