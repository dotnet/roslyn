// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class WellKnownNamingInfo
    {
        private static SymbolKindOrTypeKind Kind(SymbolKind kind)
        {
            return new SymbolKindOrTypeKind(kind);
        }

        private static SymbolKindOrTypeKind Kind(TypeKind kind)
        {
            return new SymbolKindOrTypeKind(kind);
        }

        private static AccessibilityKind Kind(Accessibility accessibility)
        {
            return new AccessibilityKind(accessibility);
        }

        private static DeclarationModifiers NoModifiers = new DeclarationModifiers();

        private static List<AccessibilityKind> AnyAccessibility = new[]
        {
            Kind(Accessibility.Friend), Kind(Accessibility.Internal), Kind(Accessibility.Private),
            Kind(Accessibility.Protected), Kind(Accessibility.ProtectedOrFriend),
            Kind(Accessibility.ProtectedOrInternal), Kind(Accessibility.Public)
        }.ToList();

        private static List<AccessibilityKind> Internal = new List<AccessibilityKind>() { Kind(Accessibility.Internal) };
        private static List<AccessibilityKind> Public = new List<AccessibilityKind>() { Kind(Accessibility.Public) };
        private static List<AccessibilityKind> Protected = new List<AccessibilityKind>() { Kind(Accessibility.Protected) };
        private static List<AccessibilityKind> Private = new List<AccessibilityKind>() { Kind(Accessibility.Private) };

        public static List<SymbolSpecification> GetWellKnownSymbolSpecifications()
        {
            var result = new List<SymbolSpecification>();

            // class
            result.Add(new SymbolSpecification(FeaturesResources.Class, Kind(TypeKind.Class), NoModifiers, AnyAccessibility, "{5C545A62-B14D-460A-88D8-E936C0A39316}"));

            // interface
            result.Add(new SymbolSpecification(FeaturesResources.Interface, Kind(TypeKind.Interface), NoModifiers, AnyAccessibility, "{23D856B4-5089-4405-83CE-749AADA99153}"));

            // struct
            result.Add(new SymbolSpecification(FeaturesResources.Struct, Kind(TypeKind.Struct), NoModifiers, AnyAccessibility, "{D1796E78-FF66-463F-8576-EB46416060C0}"));

            // enum
            result.Add(new SymbolSpecification(FeaturesResources.Enum, Kind(TypeKind.Struct), NoModifiers, AnyAccessibility, "{D8AF8DC6-1ADE-441D-9947-8946922E198A}"));

            // namespace
            result.Add(new SymbolSpecification(FeaturesResources.Namespace, Kind(SymbolKind.Namespace), NoModifiers, AnyAccessibility, "{B11CDBC4-FCA6-450F-97D2-F5F49D5DA1F7}"));

            // type parameter
            result.Add(new SymbolSpecification(FeaturesResources.Type_Parameter_, Kind(SymbolKind.TypeParameter), NoModifiers, AnyAccessibility, "{5C2A9E28-D284-4665-983E-18F7547D98E4}"));

            // delegate
            result.Add(new SymbolSpecification(FeaturesResources.Delegate, Kind(TypeKind.Delegate), NoModifiers, AnyAccessibility, "{408A3347-B908-4B54-A954-1355E64C1DE3}"));

            // event
            result.Add(new SymbolSpecification(FeaturesResources.Event, Kind(SymbolKind.Event), NoModifiers, AnyAccessibility, "{830657F6-E7E5-4830-B328-F109D3B6C165}"));

            // public method
            result.Add(new SymbolSpecification(FeaturesResources.method, Kind(SymbolKind.Method), NoModifiers, Public, "{390CAED4-F0A9-42BB-ADBB-B44C4A302A22}"));

            // private method
            result.Add(new SymbolSpecification(FeaturesResources.Private_Method, Kind(SymbolKind.Method), NoModifiers, Private, "{AF410767-F189-47C6-B140-AECCF1FF242E}"));

            // abstract method
            result.Add(new SymbolSpecification(FeaturesResources.Abstract_Method, Kind(SymbolKind.Method), new DeclarationModifiers(isAbstract: true), AnyAccessibility, "{8076757E-6A4A-47F1-9B4B-AE8A3284E987}"));

            // static method
            result.Add(new SymbolSpecification(FeaturesResources.Static_Method, Kind(SymbolKind.Method), new DeclarationModifiers(isStatic: true), AnyAccessibility, "{16133061-A8E7-4392-92C3-1D93CD54C218}"));

            // async method
            result.Add(new SymbolSpecification(FeaturesResources.Async_Method, Kind(SymbolKind.Method), new DeclarationModifiers(isAsync: true), AnyAccessibility, "{03A274DF-B686-4A76-9138-96AECB9BD33B}"));

            // property
            result.Add(new SymbolSpecification(FeaturesResources.Property, Kind(SymbolKind.Property), NoModifiers, AnyAccessibility, "{DA6A2919-5AA6-4AD1-A24D-576776ED3974}"));

            // field
            result.Add(new SymbolSpecification(FeaturesResources.Public_or_protected_field, Kind(SymbolKind.Field), NoModifiers, Public.Concat(Protected).ToList(), "{B24A91CE-3501-4799-B6DF-BAF044156C83}"));

            // static field
            result.Add(new SymbolSpecification(FeaturesResources.Static_field, Kind(SymbolKind.Field),
                new DeclarationModifiers(isStatic: true), AnyAccessibility, "{70AF42CB-1741-4027-969C-9EDC4877D965}"));

            // private or internal field
            result.Add(new SymbolSpecification(FeaturesResources.Private_or_internal_field, Kind(SymbolKind.Field), NoModifiers, Private.Concat(Internal).ToList(), "{10790AA6-0A0B-432D-A52D-D252CA92302B}"));

            // static private/internal field
            result.Add(new SymbolSpecification(FeaturesResources.Private_or_internal_static_field, Kind(SymbolKind.Field),
                new DeclarationModifiers(isStatic: true), Private.Concat(Internal).ToList(), "{AC995BE4-88DE-4771-9DCC-A456A7C02D89}"));


            return result;
        }

        public static List<NamingStyle> GetWellKnownNamingStyles()
        {
            var result = new List<NamingStyle>();

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{F408FCBC-0C17-4974-BE20-BCB62BC81A62}"),
                Name = FeaturesResources.Camel_Case,
                CapitalizationScheme = Capitalization.CamelCase
            });

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{87E7C501-9948-4B53-B1EB-A6CBE918FEEE}"),
                Name = FeaturesResources.Pascal_Case,
                CapitalizationScheme = Capitalization.PascalCase
            });

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{868DE8A1-CA8D-4190-B7E1-88CB38210C82}"),
                Name = FeaturesResources.First_word_capitalized,
                CapitalizationScheme = Capitalization.FirstUpper
            });

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{C5032A64-92E6-4A3B-9152-F01272DF550D}"),
                Name = FeaturesResources.All_uppercase,
                CapitalizationScheme = Capitalization.AllUpper
            });

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{AFF92434-B78F-413C-A6EF-ED5007F45167}"),
                Name = FeaturesResources.All_lowercase,
                CapitalizationScheme = Capitalization.AllLower
            });

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{308152F2-A334-48B3-8BEC-DDEE40785FEB}"),
                Name = string.Format(FeaturesResources.Ends_with_0, "Async"),
                Suffix = "Async"
            });

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{54B14419-268B-4CC2-975F-526CC8460298}"),
                Name = string.Format(FeaturesResources.Begins_with_0, "_"),
                Prefix = "_"
            });

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{1CADEA25-F609-449A-A7EF-B9080BCA2163}"),
                Name = string.Format(FeaturesResources.Begins_with_0, "s_"),
                Prefix = "s_"
            });

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{2ACD535E-4C74-4838-9A0A-02B337DADFAA}"),
                Name = string.Format(FeaturesResources.Begins_with_0_camel_case, "_"),
                Prefix = "_"
            });

            result.Add(new NamingStyle()
            {
                ID = Guid.Parse("{0376DC66-18E6-40FF-8266-A54F5E1D15FC}"),
                Name = string.Format(FeaturesResources.Begins_with_0_camel_case, "s_"),
                Prefix = "s_"
            });

            return result;
        }
    }
}
