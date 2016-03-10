// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.PEWriter
{
    public sealed class UsedNamespaceOrTypeTests
    {
        public class EqualsProxy
        {
            public readonly string Name;

            public EqualsProxy(string name)
            {
                Name = name;
            }

            public override int GetHashCode() => Name.GetHashCode();
            public override bool Equals(object obj) => (obj as EqualsProxy)?.Name.Equals(Name) == true;
        }

        private static Mock<T> CreateEqualsInterface<T>(string name) where T : class
        {
            var mock = new Mock<EqualsProxy>(name) { CallBase = true };
            return mock.As<T>();
        }

        private static void RunAll(EqualityUnit<UsedNamespaceOrType> unit)
        {
            EqualityUtil.RunAll(unit);
        }

        [Fact]
        public void EqualsTargetTypeSameObject()
        {
            var ref1 = CreateEqualsInterface<ITypeReference>("ref1");
            var ref2 = CreateEqualsInterface<ITypeReference>("ref2");

            var value = UsedNamespaceOrType.CreateType(ref1.Object, "alias");
            var unit = EqualityUnit
                .Create(value)
                .WithEqualValues(
                    value,
                    UsedNamespaceOrType.CreateType(ref1.Object, "alias"))
                .WithNotEqualValues(
                    UsedNamespaceOrType.CreateNamespace(new Mock<INamespace>().Object),
                    UsedNamespaceOrType.CreateType(ref2.Object, "alias"),
                    UsedNamespaceOrType.CreateType(ref1.Object, "different alias"));
            RunAll(unit);
        }

        [WorkItem(7015, "https://github.com/dotnet/roslyn/issues/7015")]
        [Fact]
        public void EqualsTargetTypeSameValue()
        {
            var type1 = CreateEqualsInterface<ITypeReference>("type name");
            var type2 = CreateEqualsInterface<ITypeReference>("type name");
            var type3 = CreateEqualsInterface<ITypeReference>("other type name");

            Assert.True(type1.Object.Equals(type2.Object));
            Assert.False(type1.Object.Equals(type3.Object));
            Assert.True(object.Equals(type1.Object, type2.Object));

            var value = UsedNamespaceOrType.CreateType(type1.Object, "alias");
            var unit = EqualityUnit
                .Create(value)
                .WithEqualValues(
                    value,
                    UsedNamespaceOrType.CreateType(type1.Object, "alias"),
                    UsedNamespaceOrType.CreateType(type2.Object, "alias"))
                .WithNotEqualValues(
                    UsedNamespaceOrType.CreateType(type1.Object, "different alias"),
                    UsedNamespaceOrType.CreateType(type2.Object, "different alias"),
                    UsedNamespaceOrType.CreateType(type3.Object, "alias"),
                    UsedNamespaceOrType.CreateNamespace(new Mock<INamespace>().Object));
            RunAll(unit);
        }

        [Fact]
        public void EqualsExternAlias()
        {
            var value = UsedNamespaceOrType.CreateExternAlias("alias1");
            var unit = EqualityUnit
                .Create(value)
                .WithEqualValues(
                    value,
                    UsedNamespaceOrType.CreateExternAlias("alias1"))
                .WithNotEqualValues(UsedNamespaceOrType.CreateExternAlias("alias2"));
            RunAll(unit);
        }

        [Fact]
        public void EqualsNamespace()
        {
            var ns1 = CreateEqualsInterface<INamespace>("namespace");
            var ns2 = CreateEqualsInterface<INamespace>("namespace");
            var ns3 = CreateEqualsInterface<INamespace>("other namespace");

            var value = UsedNamespaceOrType.CreateNamespace(ns1.Object);
            var unit = EqualityUnit
                .Create(value)
                .WithEqualValues(
                    value,
                    UsedNamespaceOrType.CreateNamespace(ns1.Object),
                    UsedNamespaceOrType.CreateNamespace(ns2.Object))
                .WithNotEqualValues(
                    UsedNamespaceOrType.CreateExternAlias("alias"),
                    UsedNamespaceOrType.CreateNamespace(ns1.Object, CreateEqualsInterface<IAssemblyReference>("a").Object),
                    UsedNamespaceOrType.CreateNamespace(ns3.Object));
            RunAll(unit);
        }

        [Fact]
        public void EqualsNamespaceAndAssembly()
        {
            var assembly1 = CreateEqualsInterface<IAssembly>("assembly");
            var assembly2 = CreateEqualsInterface<IAssembly>("assembly");
            var assembly3 = CreateEqualsInterface<IAssembly>("other assembly");
            var ns1 = CreateEqualsInterface<INamespace>("namespace");
            var ns2 = CreateEqualsInterface<INamespace>("namespace");
            var ns3 = CreateEqualsInterface<INamespace>("other namespace");

            var value = UsedNamespaceOrType.CreateNamespace(ns1.Object, assembly1.Object);
            var unit = EqualityUnit
                .Create(value)
                .WithEqualValues(
                    value,
                    UsedNamespaceOrType.CreateNamespace(ns1.Object, assembly1.Object),
                    UsedNamespaceOrType.CreateNamespace(ns1.Object, assembly2.Object),
                    UsedNamespaceOrType.CreateNamespace(ns2.Object, assembly1.Object))
                .WithNotEqualValues(
                    UsedNamespaceOrType.CreateExternAlias("alias"),
                    UsedNamespaceOrType.CreateNamespace(ns1.Object, new Mock<IAssemblyReference>().Object),
                    UsedNamespaceOrType.CreateNamespace(ns3.Object));
            RunAll(unit);
        }
    }
}
