// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Cci
{
    internal enum PlatformType
    {
        SystemObject = Microsoft.CodeAnalysis.SpecialType.System_Object,
        SystemDecimal = Microsoft.CodeAnalysis.SpecialType.System_Decimal,
        SystemTypedReference = Microsoft.CodeAnalysis.SpecialType.System_TypedReference,
        SystemType = Microsoft.CodeAnalysis.WellKnownType.System_Type,
        SystemInt32 = Microsoft.CodeAnalysis.SpecialType.System_Int32,
        SystemVoid = Microsoft.CodeAnalysis.SpecialType.System_Void,
        SystemString = Microsoft.CodeAnalysis.SpecialType.System_String,
    }

    /// <summary>
    /// A description of the lexical scope in which a namespace type has been nested. This scope is tied to a particular
    /// method body, so that partial types can be accommodated.
    /// </summary>
    internal interface INamespaceScope
    {
        /// <summary>
        /// Zero or more used namespaces. These correspond to using clauses in C#.
        /// </summary>
        ImmutableArray<IUsedNamespaceOrType> UsedNamespaces { get; }
    }

    /// <summary>
    /// A namespace that is used (imported) inside a namespace scope.
    /// 
    /// Kind            | Example                   | Alias     | TargetName
    /// ----------------+---------------------------+-----------+-------------------
    /// Namespace       | using System;             | null      | "System"
    /// NamespaceAlias  | using S = System;         | "S"       | "System"
    /// ExternNamespace | extern alias LibV1;       | "LibV1"   | null
    /// TypeAlias       | using C = System.Console; | "C"       | "System.Console"
    /// </summary>
    internal interface IUsedNamespaceOrType
    {
        /// <summary>
        /// An alias for a namespace. For example the "x" of "using x = y.z;" in C#. Empty if no alias is present.
        /// </summary>
        string Alias { get; }

        /// <summary>
        /// The name of a namepace that has been aliased.  For example the "y.z" of "using x = y.z;" or "using y.z" in C#.
        /// </summary>
        string TargetName { get; }

        /// <summary>
        /// The name of an extern alias that has been used to qualify a name.  For example the "Q" of "using x = Q::y.z;" or "using Q::y.z" in C#.
        /// </summary>
        string ExternAlias { get; }

        /// <summary>
        /// Distinguishes the various kinds of targets.
        /// </summary>
        UsedNamespaceOrTypeKind Kind { get; }

        /// <summary>
        /// Indicates whether the import was specified on a project level, or on file level (used for VB only)
        /// </summary>
        bool ProjectLevel { get; }

        /// <summary>
        /// The encoded name for this used type or namespace. The encoding is dependent on the UsedNamespaceOrTypeKind.
        /// </summary>
        string FullName { get; }
    }

    /// <summary>
    /// Represents an assembly reference with an alias (i.e. an extern alias in C#).
    /// </summary>
    internal interface IExternNamespace
    {
        /// <summary>
        /// An alias for the global namespace of the assembly.
        /// </summary>
        string NamespaceAlias { get; }

        /// <summary>
        /// The name of the referenced assembly.
        /// </summary>
        string AssemblyName { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal enum UsedNamespaceOrTypeKind
    {
        CSNamespace, // e.g. using System;
        CSNamespaceAlias, // e.g. using S = System;
        CSExternNamespace, //e.g. extern alias CorLib;
        CSTypeAlias, // e.g. using IntList = System.Collections.Generic.List<int>;        
        VBNamespace, // e.g. Imports System.Collection
        VBType, // e.g. Imports System.Collection.ArrayList
        VBNamespaceOrTypeAlias, // e.g. Imports Foo=System.Collection or Imports Foo=System.Collection.ArrayList
        VBXmlNamespace, // e.g. Imports <xmlns:ns="http://NewNamespace"> (VB only)
        VBCurrentNamespace, // the current namespace of the method's container
        VBDefaultNamespace, // the default namespace of the project
    }

    /// <summary>
    /// A range of CLR IL operations that comprise a lexical scope, specified as an IL offset and a length.
    /// </summary>
    internal interface ILocalScope
    {
        /// <summary>
        /// The offset of the first operation in the scope.
        /// </summary>
        uint Offset { get; }

        /// <summary>
        /// The length of the scope. Offset+Length equals the offset of the first operation outside the scope, or equals the method body length.
        /// </summary>
        uint Length { get; }

        /// <summary>
        /// The definition of the method in which this local scope is defined.
        /// </summary>
        IMethodDefinition MethodDefinition
        {
            get;
        }

        /// <summary>
        /// Returns zero or more local constant definitions that are local to the given scope.
        /// </summary>
        IEnumerable<Microsoft.Cci.ILocalDefinition> Constants
        {
            get;
        }

        /// <summary>
        /// Returns zero or more local variable definitions that are local to the given scope.
        /// </summary>
        IEnumerable<Microsoft.Cci.ILocalDefinition> Variables
        {
            get;
        }
    }

    internal static class TypeHelper
    {
        /// <summary>
        /// Returns a reference to the unit that defines the given referenced type. If the referenced type is a structural type, such as a pointer or a generic type instance,
        /// then the result is null.
        /// </summary>
        public static IUnitReference/*?*/ GetDefiningUnitReference(ITypeReference typeReference, Microsoft.CodeAnalysis.Emit.Context context)
        {
            INestedTypeReference/*?*/ nestedTypeReference = typeReference.AsNestedTypeReference;
            while (nestedTypeReference != null)
            {
                if (nestedTypeReference.AsGenericTypeInstanceReference != null)
                {
                    return null;
                }

                typeReference = nestedTypeReference.GetContainingType(context);
                nestedTypeReference = typeReference.AsNestedTypeReference;
            }

            INamespaceTypeReference/*?*/ namespaceTypeReference = typeReference.AsNamespaceTypeReference;
            if (namespaceTypeReference == null)
            {
                return null;
            }

            Debug.Assert(namespaceTypeReference.AsGenericTypeInstanceReference == null);

            return namespaceTypeReference.GetUnit(context);
        }
    }

    /// <summary>
    /// A range of source text that corresponds to an identifiable entity.
    /// </summary>
    internal interface ISourceLocation
    {
        /// <summary>
        /// True if the source at the given location is completely contained by the source at this location.
        /// </summary>
        bool Contains(ISourceLocation location);

        /// <summary>
        /// Copies the specified number of characters to the destination character array, starting
        /// at the specified offset from the start if the source location. Returns the number of
        /// characters actually copied. This number will be greater than zero as long as position is
        /// less than this.Length. The number will be precisely the number asked for unless there
        /// are not enough characters left in the document.
        /// </summary>
        /// <param name="offset">The starting index to copy from. Must be greater than zero and less than this.Length.</param>
        /// <param name="destination">The destination array. Must have at least destinationOffset+length elements.</param>
        /// <param name="destinationOffset">The starting index where the characters must be copied to in the destination array.</param>
        /// <param name="length">The maximum number of characters to copy.</param>
        int CopyTo(int offset, char[] destination, int destinationOffset, int length);

        // ^ requires 0 <= offset;
        // ^ requires 0 <= destinationOffset;
        // ^ requires 0 <= length;
        // ^ requires 0 <= offset+length;
        // ^ requires 0 <= destinationOffset+length;
        // ^ requires offset <= this.Length;
        // ^ requires destinationOffset+length <= destination.Length;
        // ^ ensures 0 <= result && result <= length && offset+result <= this.Length;
        // ^ ensures result < length ==> offset+result == this.Length;

        /// <summary>
        /// The character index after the last character of this location, when treating the source document as a single string.
        /// </summary>
        int EndIndex
        {
            get;

            // ^ ensures result >= 0 && result <= this.SourceDocument.Length;
            // ^ ensures result == this.StartIndex + this.Length;
        }

        /// <summary>
        /// The number of characters in this source location.
        /// </summary>
        int Length
        {
            get;

            // ^ ensures result >= 0;
            // ^ ensures this.StartIndex+result <= this.SourceDocument.Length;
        }

        /// <summary>
        /// The document containing the source text of which this location is a subrange.
        /// </summary>
        ISourceDocument SourceDocument
        {
            get;
        }

        /// <summary>
        /// The source text corresponding to this location.
        /// </summary>
        string Source
        {
            get;

            // ^ ensures result.Length == this.Length;
        }

        /// <summary>
        /// The character index of the first character of this location, when treating the source document as a single string.
        /// </summary>
        int StartIndex
        {
            get;

            // ^ ensures result >= 0 && (result < this.SourceDocument.Length || result == 0);
        }
    }

    /// <summary>
    /// An object that represents a source document, such as a text file containing C# source code.
    /// </summary>
    internal interface ISourceDocument : Document
    {
        /// <summary>
        /// Copies no more than the specified number of characters to the destination character array, starting at the specified position in the source document.
        /// Returns the actual number of characters that were copied. This number will be greater than zero as long as position is less than this.Length.
        /// The number will be precisely the number asked for unless there are not enough characters left in the document.
        /// </summary>
        /// <param name="position">The starting index to copy from. Must be greater than or equal to zero and position+length must be less than or equal to this.Length;</param>
        /// <param name="destination">The destination array.</param>
        /// <param name="destinationOffset">The starting index where the characters must be copied to in the destination array.</param>
        /// <param name="length">The maximum number of characters to copy. Must be greater than 0 and less than or equal to the number elements of the destination array.</param>
        int CopyTo(int position, char[] destination, int destinationOffset, int length);

        // ^ requires 0 <= position;
        // ^ requires 0 <= length;
        // ^ requires 0 <= position+length;
        // ^ requires position <= this.Length;
        // ^ requires 0 <= destinationOffset;
        // ^ requires 0 <= destinationOffset+length;
        // ^ requires destinationOffset+length <= destination.Length;
        // ^ ensures 0 <= result;
        // ^ ensures result <= length;
        // ^ ensures position+result <= this.Length;

        /// <summary>
        /// Returns a source location in this document that corresponds to the given source location from a previous version
        /// of this document.
        /// </summary>
        ISourceLocation GetCorrespondingSourceLocation(ISourceLocation sourceLocationInPreviousVersionOfDocument);

        // ^ requires this.IsUpdatedVersionOf(sourceLocationInPreviousVersionOfDocument.SourceDocument);

        /// <summary>
        /// Obtains a source location instance that corresponds to the substring of the document specified by the given start position and length.
        /// </summary>
        ISourceLocation GetSourceLocation(int position, int length);

        // ^ requires 0 <= position && (position < this.Length || position == 0);
        // ^ requires 0 <= length;
        // ^ requires length <= this.Length;
        // ^ requires position+length <= this.Length;
        // ^ ensures result.SourceDocument == this;
        // ^ ensures result.StartIndex == position;
        // ^ ensures result.Length == length;

        /// <summary>
        /// Returns the source text of the document in string form. Each call may do significant work, so be sure to cache this.
        /// </summary>
        string GetText();

        // ^ ensures result.Length == this.Length;

        /// <summary>
        /// Returns true if this source document has been created by editing the given source document (or an updated
        /// version of the given source document).
        /// </summary>
        //// ^ [Confined]
        bool IsUpdatedVersionOf(ISourceDocument sourceDocument);

        /// <summary>
        /// The length of the source string.
        /// </summary>
        int Length
        {
            get;

            // ^ ensures result >= 0;
        }

        /// <summary>
        /// The language that determines how the document is parsed and what it means.
        /// </summary>
        string SourceLanguage { get; }

        /// <summary>
        /// A source location corresponding to the entire document.
        /// </summary>
        ISourceLocation SourceLocation { get; }
    }

    /// <summary>
    /// An object that represents a source document corresponding to a user accessible entity such as file.
    /// </summary>
    internal interface IPrimarySourceDocument : ISourceDocument
    {
        /// <summary>
        /// A Guid that identifies the kind of document to applications such as a debugger. Typically System.Diagnostics.SymbolStore.SymDocumentType.Text.
        /// </summary>
        Guid DocumentType { get; }

        /// <summary>
        /// A Guid that identifies the programming language used in the source document. Typically used by a debugger to locate language specific logic.
        /// </summary>
        Guid Language { get; }

        /// <summary>
        /// A Guid that identifies the compiler vendor programming language used in the source document. Typically used by a debugger to locate vendor specific logic.
        /// </summary>
        Guid LanguageVendor { get; }

        /// <summary>
        /// A source location corresponding to the entire document.
        /// </summary>
        SequencePoint PrimarySourceLocation { get; }

        /// <summary>
        /// Obtains a source location instance that corresponds to the substring of the document specified by the given start position and length.
        /// </summary>
        SequencePoint GetPrimarySourceLocation(int position, int length);

        // ^ requires 0 <= position && (position < this.Length || position == 0);
        // ^ requires 0 <= length;
        // ^ requires length <= this.Length;
        // ^ requires position+length <= this.Length;
        // ^ ensures result.SourceDocument == this;
        // ^ ensures result.StartIndex == position;
        // ^ ensures result.Length == length;

        /// <summary>
        /// Maps the given (zero based) source position to a (one based) line and column, by scanning the source character by character, counting
        /// new lines until the given source position is reached. The source position and corresponding line+column are remembered and scanning carries
        /// on where it left off when this routine is called next. If the given position precedes the last given position, scanning restarts from the start.
        /// Optimal use of this method requires the client to sort calls in order of position.
        /// </summary>
        void ToLineColumn(int position, out int line, out int column);

        // ^ requires position >= 0;
        // ^ requires position <= this.Length;
        // ^ ensures line >= 1 && column >= 1;
    }
}