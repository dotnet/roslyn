// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class DependentTypeFinderTests : ServicesTestBase
    {
        [WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        [Fact]
        public async Task ImmediatelyDerivedTypes_CSharp()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", MscorlibRefPortable);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class DerivedClass : BaseClass { }
}
", MscorlibRef, solution.Projects.Single(pid => pid.Name == "PortableProject").Id);

            // get symbols for types
            var portableCompilation = await solution.Projects.Single(p => p.Name == "PortableProject").GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await DependentTypeFinder.GetTypesImmediatelyDerivedFromClassesAsync(baseClassSymbol, solution, CancellationToken.None);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        [WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        [Fact]
        public async Task ImmediatelyDerivedTypes_CSharp_PortableProfile7()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", MscorlibRefPortable);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class DerivedClass : BaseClass { }
}
", SystemRuntimePP7Ref, solution.Projects.Single(pid => pid.Name == "PortableProject").Id);

            // get symbols for types
            var portableCompilation = await solution.Projects.Single(p => p.Name == "PortableProject").GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await DependentTypeFinder.GetTypesImmediatelyDerivedFromClassesAsync(baseClassSymbol, solution, CancellationToken.None);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        [WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        [Fact]
        public async Task ImmediatelyDerivedTypes_VisualBasic()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.VisualBasic, @"
Namespace N
    Public MustInherit Class BaseClass
    End Class
End Namespace
", MscorlibRefPortable);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.VisualBasic, @"
Imports N
Namespace M
    Public Class DerivedClass
        Inherits BaseClass
    End Class
End Namespace
", MscorlibRef, solution.Projects.Single(pid => pid.Name == "PortableProject").Id);

            // get symbols for types
            var portableCompilation = await solution.Projects.Single(p => p.Name == "PortableProject").GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await DependentTypeFinder.GetTypesImmediatelyDerivedFromClassesAsync(baseClassSymbol, solution, CancellationToken.None);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        [WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        [Fact]
        public async Task ImmediatelyDerivedTypes_CrossLanguage()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", MscorlibRefPortable);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.VisualBasic, @"
Imports N
Namespace M
    Public Class DerivedClass
        Inherits BaseClass
    End Class
End Namespace
", MscorlibRef, solution.Projects.Single(pid => pid.Name == "PortableProject").Id);

            // get symbols for types
            var portableCompilation = await solution.Projects.Single(p => p.Name == "PortableProject").GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await DependentTypeFinder.GetTypesImmediatelyDerivedFromClassesAsync(baseClassSymbol, solution, CancellationToken.None);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        [WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        [Fact]
        public async Task ImmediatelyDerivedInterfaces_CSharp()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            // create portable assembly with an interface
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public interface IBaseInterface { }
}
", MscorlibRefPortable);

            // create a normal assembly with a type implementing that interface
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class ImplementingClass : IBaseInterface { }
}
", MscorlibRef, solution.Projects.Single(pid => pid.Name == "PortableProject").Id);

            // get symbols for types
            var portableCompilation = await solution.Projects.Single(p => p.Name == "PortableProject").GetCompilationAsync();
            var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.IBaseInterface");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var implementingClassSymbol = normalCompilation.GetTypeByMetadataName("M.ImplementingClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseInterfaceSymbol, implementingClassSymbol.Interfaces.Single());

            // verify that the implementing types of `N.IBaseInterface` correctly resolve to `M.ImplementingClass`
            var typesThatImplementInterface = await DependentTypeFinder.GetTypesImmediatelyDerivedFromInterfacesAsync(baseInterfaceSymbol, solution, CancellationToken.None);
            Assert.Equal(implementingClassSymbol, typesThatImplementInterface.Single());
        }

        [WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        [Fact]
        public async Task ImmediatelyDerivedInterfaces_VisualBasic()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            // create portable assembly with an interface
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.VisualBasic, @"
Namespace N
    Public Interface IBaseInterface
    End Interface
End Namespace
", MscorlibRefPortable);

            // create a normal assembly with a type implementing that interface
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.VisualBasic, @"
Imports N
Namespace M
    Public Class ImplementingClass
        Implements IBaseInterface
    End Class
End Namespace
", MscorlibRef, solution.Projects.Single(pid => pid.Name == "PortableProject").Id);

            // get symbols for types
            var portableCompilation = await solution.Projects.Single(p => p.Name == "PortableProject").GetCompilationAsync();
            var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.IBaseInterface");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var implementingClassSymbol = normalCompilation.GetTypeByMetadataName("M.ImplementingClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseInterfaceSymbol, implementingClassSymbol.Interfaces.Single());

            // verify that the implementing types of `N.IBaseInterface` correctly resolve to `M.ImplementingClass`
            var typesThatImplementInterface = await DependentTypeFinder.GetTypesImmediatelyDerivedFromInterfacesAsync(baseInterfaceSymbol, solution, CancellationToken.None);
            Assert.Equal(implementingClassSymbol, typesThatImplementInterface.Single());
        }

        [WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        [Fact]
        public async Task ImmediatelyDerivedInterfaces_CrossLanguage()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            // create portable assembly with an interface
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.VisualBasic, @"
Namespace N
    Public Interface IBaseInterface
    End Interface
End Namespace
", MscorlibRefPortable);

            // create a normal assembly with a type implementing that interface
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class ImplementingClass : IBaseInterface { }
}
", MscorlibRef, solution.Projects.Single(pid => pid.Name == "PortableProject").Id);

            // get symbols for types
            var portableCompilation = await solution.Projects.Single(p => p.Name == "PortableProject").GetCompilationAsync();
            var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.IBaseInterface");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var implementingClassSymbol = normalCompilation.GetTypeByMetadataName("M.ImplementingClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseInterfaceSymbol, implementingClassSymbol.Interfaces.Single());

            // verify that the implementing types of `N.IBaseInterface` correctly resolve to `M.ImplementingClass`
            var typesThatImplementInterface = await DependentTypeFinder.GetTypesImmediatelyDerivedFromInterfacesAsync(baseInterfaceSymbol, solution, CancellationToken.None);
            Assert.Equal(implementingClassSymbol, typesThatImplementInterface.Single());
        }
    }
}
