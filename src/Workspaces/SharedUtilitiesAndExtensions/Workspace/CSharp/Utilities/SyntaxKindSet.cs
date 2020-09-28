// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal class SyntaxKindSet
    {
        public static readonly ISet<SyntaxKind> AllTypeModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.AbstractKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.SealedKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
                SyntaxKind.ReadOnlyKeyword,
                SyntaxKind.RefKeyword
            };

        public static readonly ISet<SyntaxKind> AllMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.AbstractKeyword,
                SyntaxKind.AsyncKeyword,
                SyntaxKind.ExternKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.OverrideKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.ReadOnlyKeyword,
                SyntaxKind.SealedKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
                SyntaxKind.VirtualKeyword,
                SyntaxKind.VolatileKeyword,
            };

        public static readonly ISet<SyntaxKind> AllGlobalMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.ExternKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.OverrideKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ReadOnlyKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
                SyntaxKind.VolatileKeyword,
            };

        public static readonly ISet<SyntaxKind> AllLocalFunctionModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.AsyncKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.ExternKeyword,
            SyntaxKind.StaticKeyword
        };

        public static readonly ISet<SyntaxKind> AllTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.EnumDeclaration,
        };

        public static readonly ISet<SyntaxKind> ClassInterfaceStructRecordTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.StructDeclaration,
        };

        public static readonly ISet<SyntaxKind> ClassInterfaceRecordTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.RecordDeclaration,
        };

        public static readonly ISet<SyntaxKind> ClassStructRecordTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.ClassDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.StructDeclaration,
        };

        public static readonly ISet<SyntaxKind> StructOnlyTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.StructDeclaration,
        };
    }
}
