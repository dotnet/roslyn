// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

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

        public static readonly ISet<SyntaxKind> LocalFunctionModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.AsyncKeyword,
            SyntaxKind.UnsafeKeyword
        };

        public static readonly ISet<SyntaxKind> AllTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.EnumDeclaration
        };

        public static readonly ISet<SyntaxKind> ClassInterfaceStructTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
        };

        public static readonly ISet<SyntaxKind> ClassInterfaceTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.ClassDeclaration,
        };

        public static readonly ISet<SyntaxKind> ClassStructTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
        };

        public static readonly ISet<SyntaxKind> StructOnlyTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.StructDeclaration,
        };
    }
}
