// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal static partial class Extensions
{
    /// <summary>
    /// This is the subset of values from <c>Microsoft.VisualStudio.Imaging.KnownImageIds</c> that we
    /// care about. Copying them here avoids referencing Microsoft.VisualStudio.ImageCatalog.dll.
    /// </summary>
    private static class KnownImageIds
    {
        public static readonly Guid ImageCatalogGuid = Guid.Parse("ae27a6b0-e345-4288-96df-5eaf394ee369");

        public const int Assembly = 196;

        public const int ClassInternal = 466;
        public const int ClassPrivate = 471;
        public const int ClassProtected = 472;
        public const int ClassPublic = 473;

        public const int ConstantInternal = 617;
        public const int ConstantPrivate = 618;
        public const int ConstantProtected = 619;
        public const int ConstantPublic = 620;

        public const int CSFileNode = 738;
        public const int CSProjectNode = 758;

        public const int DelegateInternal = 910;
        public const int DelegatePrivate = 911;
        public const int DelegateProtected = 912;
        public const int DelegatePublic = 913;

        public const int EnumerationInternal = 1121;
        public const int EnumerationPrivate = 1129;
        public const int EnumerationProtected = 1130;
        public const int EnumerationPublic = 1131;

        public const int EnumerationItemPublic = 1125;

        public const int EventInternal = 1145;
        public const int EventPrivate = 1150;
        public const int EventProtected = 1151;
        public const int EventPublic = 1152;

        public const int ExtensionMethod = 1204;

        public const int FieldInternal = 1218;
        public const int FieldPrivate = 1220;
        public const int FieldProtected = 1221;
        public const int FieldPublic = 1222;

        public const int IntellisenseKeyword = 1589;
        public const int IntellisenseWarning = 1591;

        public const int InterfaceInternal = 1605;
        public const int InterfacePrivate = 1606;
        public const int InterfaceProtected = 1607;
        public const int InterfacePublic = 1608;

        public const int Label = 1661;

        public const int LocalVariable = 1747;

        public const int MatchType = 3790;

        public const int MethodInternal = 1876;
        public const int MethodPrivate = 1878;
        public const int MethodProtected = 1879;
        public const int MethodPublic = 1880;

        public const int ModuleInternal = 1916;
        public const int ModulePrivate = 1917;
        public const int ModuleProtected = 1918;
        public const int ModulePublic = 1919;

        public const int Namespace = 1951;

        public const int NuGet = 3150;

        public const int OpenFolder = 2162;

        public const int OperatorInternal = 2175;
        public const int OperatorPrivate = 2176;
        public const int OperatorProtected = 2173;
        public const int OperatorPublic = 2174;

        public const int PropertyInternal = 2431;
        public const int PropertyPrivate = 2434;
        public const int PropertyProtected = 2435;
        public const int PropertyPublic = 2436;

        public const int Reference = 2521;

        public const int Snippet = 2852;

        public const int StatusInformation = 2933;
        public const int StatusError = 2926;

        public const int Type = 3233;

        public const int ValueTypeInternal = 3332;
        public const int ValueTypePrivate = 3333;
        public const int ValueTypeProtected = 3334;
        public const int ValueTypePublic = 3335;

        public const int VBFileNode = 3361;
        public const int VBProjectNode = 3380;
    }
}
