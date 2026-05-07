// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal static class CommonModifiers
{
    private static string GetText(CSharpSyntaxKind kind)
        => SyntaxFacts.GetText(kind);

    public static ImmutableArray<string> Internal { get; } = [
        GetText(CSharpSyntaxKind.InternalKeyword)];

    public static ImmutableArray<string> InternalSealed { get; } = [
        GetText(CSharpSyntaxKind.InternalKeyword),
        GetText(CSharpSyntaxKind.SealedKeyword)];

    public static ImmutableArray<string> InternalStatic { get; } = [
        GetText(CSharpSyntaxKind.InternalKeyword),
        GetText(CSharpSyntaxKind.StaticKeyword)];

    public static ImmutableArray<string> Private { get; } = [
        GetText(CSharpSyntaxKind.PrivateKeyword)];

    public static ImmutableArray<string> PrivateReadOnly { get; } = [
        GetText(CSharpSyntaxKind.PrivateKeyword),
        GetText(CSharpSyntaxKind.ReadOnlyKeyword)];

    public static ImmutableArray<string> PrivateSealed { get; } = [
        GetText(CSharpSyntaxKind.PrivateKeyword),
        GetText(CSharpSyntaxKind.SealedKeyword)];

    public static ImmutableArray<string> FileSealed { get; } = [
        GetText(CSharpSyntaxKind.FileKeyword),
        GetText(CSharpSyntaxKind.SealedKeyword)];

    public static ImmutableArray<string> Protected { get; } = [
        GetText(CSharpSyntaxKind.ProtectedKeyword)];

    public static ImmutableArray<string> ProtectedOverride { get; } = [
        GetText(CSharpSyntaxKind.ProtectedKeyword),
        GetText(CSharpSyntaxKind.OverrideKeyword)];

    public static ImmutableArray<string> Public { get; } = [
        GetText(CSharpSyntaxKind.PublicKeyword)];

    public static ImmutableArray<string> PublicAsyncOverride { get; } = [
        GetText(CSharpSyntaxKind.PublicKeyword),
        GetText(CSharpSyntaxKind.AsyncKeyword),
        GetText(CSharpSyntaxKind.OverrideKeyword)];

    public static ImmutableArray<string> PublicOverrideAsync { get; } = [
        GetText(CSharpSyntaxKind.PublicKeyword),
        GetText(CSharpSyntaxKind.OverrideKeyword),
        GetText(CSharpSyntaxKind.AsyncKeyword)];

    public static ImmutableArray<string> PublicPartial { get; } = [
        GetText(CSharpSyntaxKind.PublicKeyword),
        GetText(CSharpSyntaxKind.PartialKeyword)];
}
