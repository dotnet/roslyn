' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities


Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class MissingTypeReferences
        Inherits BasicTestBase

        <Fact, WorkItem(910594, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/910594")>
        Public Sub Test1()
            Dim assembly = MetadataTestHelpers.LoadFromBytes(TestResources.General.MDTestLib2)

            TestMissingTypeReferencesHelper1(assembly)

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {TestReferences.SymbolsTests.MissingTypes.MDMissingType,
                                     TestReferences.SymbolsTests.MissingTypes.MDMissingTypeLib,
                                     TestReferences.NetFx.v4_0_21006.mscorlib})

            TestMissingTypeReferencesHelper2(assemblies)
        End Sub

        Private Sub TestMissingTypeReferencesHelper1(assembly As AssemblySymbol)

            Dim module0 = assembly.Modules(0)

            Dim TC10 = module0.GlobalNamespace.GetTypeMembers("TC10").Single()

            Dim base As MissingMetadataTypeSymbol = DirectCast(TC10.BaseType, MissingMetadataTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.Equal("Object", base.Name)
            Assert.Equal("System", base.ContainingSymbol.Name)
            Assert.Equal(0, base.Arity)
            Assert.Equal("System.Object[missing]", base.ToTestDisplayString())
            Assert.NotNull(base.ContainingNamespace)
            Assert.NotNull(base.ContainingSymbol)
            Assert.True(base.ContainingAssembly.IsMissing)
            Assert.Equal("mscorlib", base.ContainingAssembly.Identity.Name)

            Dim TC8 = module0.GlobalNamespace.GetTypeMembers("TC8").Single()
            Dim genericBase = DirectCast(TC8.BaseType, SubstitutedErrorType)
            Assert.Equal("C1(Of System.Type[missing])[missing]", genericBase.ToTestDisplayString())

            base = DirectCast(genericBase.ConstructedFrom, MissingMetadataTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.True(base.Name.Equals("C1"))
            Assert.Equal(1, base.Arity)
            Assert.Equal("C1(Of )[missing]", base.ToTestDisplayString())
            Assert.NotNull(base.ContainingAssembly)
            Assert.NotNull(base.ContainingNamespace)
            Assert.NotNull(base.ContainingSymbol)
            Assert.True(base.ContainingAssembly.IsMissing)
            Assert.Equal("MDTestLib1", base.ContainingAssembly.Identity.Name)

            Dim TC7 = module0.GlobalNamespace.GetTypeMembers("TC7").Single()
            genericBase = DirectCast(TC7.BaseType, SubstitutedErrorType)
            base = DirectCast(genericBase.OriginalDefinition, MissingMetadataTypeSymbol)

            Assert.Equal("C1(Of TC7_T1)[missing].C3[missing].C4(Of TC7_T2)[missing]", genericBase.ToTestDisplayString())
            Assert.True(genericBase.ContainingAssembly.IsMissing)
            Assert.False(genericBase.CanConstruct)
            Assert.Equal(base.GetUseSiteErrorInfo().ToString(), genericBase.GetUseSiteErrorInfo().ToString())
            Assert.Equal(base.ErrorInfo.ToString(), genericBase.ErrorInfo.ToString())

            Dim constructedFrom = DirectCast(genericBase.ConstructedFrom, SubstitutedErrorType)
            Assert.Equal("C1(Of TC7_T1)[missing].C3[missing].C4(Of )[missing]", constructedFrom.ToTestDisplayString())

            Assert.True(constructedFrom.CanConstruct)
            Assert.Same(constructedFrom, constructedFrom.Construct(constructedFrom.TypeParameters.As(Of TypeSymbol)()))
            Assert.Equal(genericBase, constructedFrom.Construct(genericBase.TypeArguments))

            genericBase = DirectCast(genericBase.ContainingSymbol, SubstitutedErrorType)
            Assert.Equal("C1(Of TC7_T1)[missing].C3[missing]", genericBase.ToTestDisplayString())
            Assert.Same(genericBase, genericBase.ConstructedFrom)

            genericBase = DirectCast(genericBase.ContainingSymbol, SubstitutedErrorType)
            Assert.Equal("C1(Of TC7_T1)[missing]", genericBase.ToTestDisplayString())
            Assert.Same(genericBase.OriginalDefinition, genericBase.ConstructedFrom)
            Assert.Equal("C1(Of )[missing]", genericBase.OriginalDefinition.ToTestDisplayString())

            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.Equal("C4", base.Name)
            Assert.Equal(1, base.Arity)
            Assert.Equal("C1(Of )[missing].C3[missing].C4(Of )[missing]", base.ToTestDisplayString())
            Assert.NotNull(base.ContainingAssembly)
            Assert.NotNull(base.ContainingNamespace)
            Assert.NotNull(base.ContainingSymbol)
            Assert.Equal("MDTestLib1", base.ContainingAssembly.Identity.Name)

            Assert.Equal(SymbolKind.ErrorType, base.ContainingSymbol.Kind)
            Assert.NotNull(base.ContainingSymbol.ContainingAssembly)
            Assert.Same(base.ContainingAssembly, base.ContainingSymbol.ContainingAssembly)

            Dim baseContainerContainer = base.ContainingSymbol.ContainingSymbol
            Assert.Equal(SymbolKind.ErrorType, baseContainerContainer.Kind)
            Assert.NotNull(baseContainerContainer.ContainingAssembly)
            Assert.Same(base.ContainingAssembly, baseContainerContainer.ContainingAssembly)

        End Sub


        Private Sub TestMissingTypeReferencesHelper2(assemblies() As AssemblySymbol, Optional reflectionOnly As Boolean = False)

            Dim module1 = assemblies(0).Modules(0)
            Dim module2 = assemblies(1).Modules(0)

            Dim assembly2 = DirectCast(assemblies(1), MetadataOrSourceAssemblySymbol)

            Dim TC As NamedTypeSymbol
            TC = module1.GlobalNamespace.GetTypeMembers("TC1").Single()
            Dim base = DirectCast(TC.BaseType, MissingMetadataTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.Equal("MissingC1", base.Name)
            Assert.Equal(0, base.Arity)
            Assert.Equal("MissingNS1.MissingC1[missing]", base.ToTestDisplayString())
            Assert.Same(base.ContainingAssembly, module2.ContainingAssembly)
            Assert.NotNull(base.ContainingNamespace)
            Assert.Equal("MissingNS1", base.ContainingNamespace.Name)
            Assert.Equal("", base.ContainingNamespace.ContainingNamespace.Name)
            Assert.NotNull(base.ContainingSymbol)
            Assert.NotNull(base.ContainingAssembly)

            TC = module1.GlobalNamespace.GetTypeMembers("TC2").Single()
            base = DirectCast(TC.BaseType, MissingMetadataTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.True(base.Name.Equals("MissingC2"))
            Assert.Equal(0, base.Arity)
            Assert.Equal("MissingNS2.MissingNS3.MissingC2[missing]", base.ToTestDisplayString())
            Assert.Same(base.ContainingAssembly, module2.ContainingAssembly)
            Assert.Equal("MissingNS3", base.ContainingNamespace.Name)
            Assert.Equal("MissingNS2", base.ContainingNamespace.ContainingNamespace.Name)
            Assert.Equal("", base.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name)
            Assert.NotNull(base.ContainingSymbol)
            Assert.NotNull(base.ContainingAssembly)

            TC = module1.GlobalNamespace.GetTypeMembers("TC3").Single()
            base = DirectCast(TC.BaseType, MissingMetadataTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.True(base.Name.Equals("MissingC3"))
            Assert.Equal(0, base.Arity)
            Assert.Equal("NS4.MissingNS5.MissingC3[missing]", base.ToTestDisplayString())
            Assert.Same(base.ContainingAssembly, module2.ContainingAssembly)
            Assert.NotNull(base.ContainingNamespace)
            Assert.NotNull(base.ContainingSymbol)
            Assert.NotNull(base.ContainingModule)

            TC = module1.GlobalNamespace.GetTypeMembers("TC4").Single()
            Dim genericBase = DirectCast(TC.BaseType, SubstitutedErrorType)
            Assert.Equal(SymbolKind.ErrorType, genericBase.Kind)
            Assert.Equal("MissingC4(Of T1, S1)[missing]", genericBase.ToTestDisplayString())

            base = DirectCast(genericBase.OriginalDefinition, MissingMetadataTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.Equal("MissingC4", base.Name)
            Assert.Equal(2, base.Arity)
            Assert.Equal("MissingC4(Of ,)[missing]", base.ToTestDisplayString())
            Assert.Same(base.ContainingAssembly, module2.ContainingAssembly)
            Assert.NotNull(base.ContainingNamespace)
            Assert.NotNull(base.ContainingSymbol)
            Assert.NotNull(base.ContainingModule)
            Dim MissingC4 = base

            TC = module1.GlobalNamespace.GetTypeMembers("TC5").Single()

            genericBase = DirectCast(TC.BaseType, SubstitutedErrorType)
            Assert.Equal("MissingC4(Of T1, S1)[missing].MissingC5(Of U1, V1, W1)[missing]", genericBase.ToTestDisplayString())

            base = DirectCast(genericBase.OriginalDefinition, MissingMetadataTypeSymbol)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.Equal("MissingC5", base.Name)
            Assert.Equal(3, base.Arity)
            Assert.Equal("MissingC4(Of ,)[missing].MissingC5(Of ,,)[missing]", base.ToTestDisplayString())
            Assert.Same(base.ContainingAssembly, module2.ContainingAssembly)
            Assert.True(base.ContainingNamespace.IsGlobalNamespace)
            Assert.Same(base.ContainingSymbol, MissingC4)

            Dim C6 = module2.GlobalNamespace.GetTypeMembers("C6").Single()

            TC = module1.GlobalNamespace.GetTypeMembers("TC6").Single()

            genericBase = DirectCast(TC.BaseType, SubstitutedErrorType)
            Assert.Equal("C6.MissingC7(Of U, V)[missing]", genericBase.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, genericBase.ContainingSymbol.Kind)

            base = DirectCast(genericBase.OriginalDefinition, MissingMetadataTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.Equal("MissingC7", base.Name)
            Assert.Equal(2, base.Arity)
            Assert.Equal("C6.MissingC7(Of ,)[missing]", base.ToTestDisplayString())
            Assert.Same(base.ContainingAssembly, module2.ContainingAssembly)
            Assert.Same(base.ContainingSymbol, C6)
            Assert.Same(base.ContainingNamespace, C6.ContainingNamespace)

            Dim MissingC7 = base

            TC = module1.GlobalNamespace.GetTypeMembers("TC7").Single()
            Dim TC7 = TC

            genericBase = DirectCast(TC.BaseType, SubstitutedErrorType)
            Assert.Equal("C6.MissingC7(Of U, V)[missing].MissingC8[missing]", genericBase.ToTestDisplayString())
            Assert.Equal(SymbolKind.ErrorType, genericBase.ContainingSymbol.Kind)

            Dim type1 = DirectCast(genericBase.ContainingSymbol, SubstitutedErrorType)

            base = DirectCast(genericBase.OriginalDefinition, MissingMetadataTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.True(base.Name.Equals("MissingC8"))
            Assert.Equal(0, base.Arity)
            Assert.Equal("C6.MissingC7(Of ,)[missing].MissingC8[missing]", base.ToTestDisplayString())
            Assert.Same(base.ContainingAssembly, module2.ContainingAssembly)
            If Not reflectionOnly Then
                Assert.Same(base.ContainingSymbol, MissingC7)
            End If
            Assert.True(base.ContainingSymbol.ToString().Equals(MissingC7.ToString))
            Assert.Same(base.ContainingNamespace, C6.ContainingNamespace)

            Dim MissingC8 = base

            TC = module1.GlobalNamespace.GetTypeMembers("TC8").Single()
            genericBase = DirectCast(TC.BaseType, SubstitutedErrorType)
            Assert.Equal("C6.MissingC7(Of U, V)[missing].MissingC8[missing].MissingC9[missing]", genericBase.ToTestDisplayString())

            Dim type2 = DirectCast(DirectCast(genericBase.ContainingSymbol.ContainingSymbol.OriginalDefinition, NamedTypeSymbol).Construct(type1.TypeArguments), SubstitutedErrorType)

            Assert.NotSame(type1, type2)
            Assert.Equal(type1, type2)
            Assert.Equal(type1.GetHashCode(), type2.GetHashCode())

            base = DirectCast(genericBase.OriginalDefinition, MissingMetadataTypeSymbol)
            Assert.Equal(SymbolKind.ErrorType, base.Kind)
            Assert.False(base.IsNamespace)
            Assert.True(base.IsType)
            Assert.True(base.Name.Equals("MissingC9"))
            Assert.Equal(0, base.Arity)
            Assert.Equal("C6.MissingC7(Of ,)[missing].MissingC8[missing].MissingC9[missing]", base.ToTestDisplayString())
            Assert.Same(base.ContainingAssembly, module2.ContainingAssembly)
            If Not reflectionOnly Then
                Assert.Same(base.ContainingSymbol, MissingC8)
            End If
            Assert.True(base.ContainingSymbol.ToString().Equals(MissingC8.ToString))
            Assert.Same(base.ContainingNamespace, C6.ContainingNamespace)

            Assert.IsAssignableFrom(Of MissingMetadataTypeSymbol)(assembly2.CachedTypeByEmittedName("MissingNS1.MissingC1"))
            Assert.IsAssignableFrom(Of MissingMetadataTypeSymbol)(assembly2.CachedTypeByEmittedName("MissingNS2.MissingNS3.MissingC2"))
            Assert.IsAssignableFrom(Of MissingMetadataTypeSymbol)(assembly2.CachedTypeByEmittedName("NS4.MissingNS5.MissingC3"))
            Assert.IsAssignableFrom(Of MissingMetadataTypeSymbol)(assembly2.CachedTypeByEmittedName("MissingC4`2"))

            Dim missing As NamedTypeSymbol = New MissingMetadataTypeSymbol.Nested(TC, "Doesn'tExist", 1, True)
            Dim missing2 As NamedTypeSymbol = New MissingMetadataTypeSymbol.Nested(TC, "Doesn'tExist", 1, True)

            Dim param1 = missing.TypeParameters(0)
            Dim param2 = missing2.TypeParameters(0)

            Assert.NotSame(param1, param2)
            Assert.Equal(param1, param2)
            Assert.Equal(param1.GetHashCode(), param2.GetHashCode())
            Assert.NotEqual(param1, Nothing)
            Assert.NotNull(param1)
            Assert.Equal(0, param2.Ordinal)
            Assert.Same(missing2, param2.ContainingSymbol)
            Assert.Same(param2, missing2.TypeArguments(0))

            Assert.True(missing.CanConstruct)
            Assert.Same(missing, missing.Construct(missing.TypeParameters.As(Of TypeSymbol)()))

            Dim wrongSubstitution = TypeSubstitution.Create(TC7, {TC7.TypeParameters(0)}.AsImmutableOrNull(),
                                                                              {DirectCast(param2, TypeSymbol)}.AsImmutableOrNull())

            Dim substitution = TypeSubstitution.Create(missing, {TC.TypeParameters(0), TC.TypeParameters(1), missing.TypeParameters(0)}.AsImmutableOrNull(),
                                                       ImmutableArray.Create(Of TypeSymbol)(TC.TypeParameters(1), TC.TypeParameters(0), TC.TypeParameters(1)))

            missing = DirectCast(missing.Construct(substitution), NamedTypeSymbol)

            Assert.Equal("TC8(Of V, U).Doesn'tExist(Of V)[missing]", missing.ToTestDisplayString())

            Assert.IsType(Of SubstitutedErrorType)(missing)
            Assert.NotNull(TryCast(missing.ContainingSymbol, SubstitutedNamedType))

            Assert.Same(substitution, DirectCast(missing, SubstitutedErrorType).TypeSubstitution)
            Assert.Same(TC.ContainingAssembly, missing.ContainingAssembly)

            Assert.Same(missing, missing.InternalSubstituteTypeParameters(wrongSubstitution).AsTypeSymbolOnly())
            Assert.Same(missing.OriginalDefinition, missing.OriginalDefinition.InternalSubstituteTypeParameters(wrongSubstitution).AsTypeSymbolOnly())

            substitution = TypeSubstitution.Create(TC, {TC.TypeParameters(0), TC.TypeParameters(1)}.AsImmutableOrNull(),
                                                       ImmutableArray.Create(Of TypeSymbol)(TC.TypeParameters(1), TC.TypeParameters(0)))
            missing = DirectCast(missing.OriginalDefinition.Construct(substitution), NamedTypeSymbol)

            Assert.Equal("TC8(Of V, U).Doesn'tExist(Of )[missing]", missing.ToTestDisplayString())
            Assert.NotEqual(missing.OriginalDefinition, missing)
            Assert.Same(missing, missing.ConstructedFrom)

            substitution = TypeSubstitution.Create(MissingC4, {MissingC4.TypeParameters(0)}.AsImmutableOrNull(),
                                                       ImmutableArray.Create(Of TypeSymbol)(TC.TypeParameters(0)))

            missing = MissingC4.Construct(substitution)
            Assert.NotEqual(MissingC4, missing)

            missing2 = DirectCast(missing.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
            Assert.Same(missing, missing2)

            substitution = TypeSubstitution.Create(TC, {TC.TypeParameters(0)}.AsImmutableOrNull(),
                                                       ImmutableArray.Create(Of TypeSymbol)(MissingC4.TypeParameters(0)))

            missing2 = DirectCast(missing.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
            Assert.Same(MissingC4, missing2)

            substitution = TypeSubstitution.Create(MissingC4, {MissingC4.TypeParameters(1)}.AsImmutableOrNull(),
                                                       ImmutableArray.Create(Of TypeSymbol)(TC.TypeParameters(1)))

            missing2 = DirectCast(missing.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
            Assert.NotEqual(missing, missing2)
            Assert.NotEqual(MissingC4, missing2)
            Assert.Same(MissingC4, missing2.OriginalDefinition)
            Assert.Same(MissingC4, missing2.ConstructedFrom)
            Assert.Equal("MissingC4(Of U, V)[missing]", missing2.ToTestDisplayString())

            substitution = TypeSubstitution.Create(MissingC7, {MissingC7.TypeParameters(0)}.AsImmutableOrNull(),
                                                       ImmutableArray.Create(Of TypeSymbol)(TC.TypeParameters(0)))

            missing = MissingC7.Construct(substitution)
            Assert.NotEqual(MissingC7, missing)
            Assert.Same(MissingC7, missing.OriginalDefinition)
            Assert.Same(MissingC7, missing.ConstructedFrom)

            missing2 = DirectCast(missing.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
            Assert.Same(missing, missing2)

            substitution = TypeSubstitution.Create(TC, {TC.TypeParameters(0)}.AsImmutableOrNull(),
                                                       ImmutableArray.Create(Of TypeSymbol)(MissingC7.TypeParameters(0)))

            missing2 = DirectCast(missing.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
            Assert.Same(MissingC7, missing2)

            substitution = TypeSubstitution.Create(MissingC7, {MissingC7.TypeParameters(1)}.AsImmutableOrNull(),
                                                       ImmutableArray.Create(Of TypeSymbol)(TC.TypeParameters(1)))

            missing2 = DirectCast(missing.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
            Assert.NotEqual(missing, missing2)
            Assert.NotEqual(MissingC7, missing2)
            Assert.Same(MissingC7, missing2.OriginalDefinition)
            Assert.Same(MissingC7, missing2.ConstructedFrom)
            Assert.Equal("C6.MissingC7(Of U, V)[missing]", missing2.ToTestDisplayString())
        End Sub


        <Fact>
        Public Sub Equality()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
            {
                TestReferences.SymbolsTests.MissingTypes.MissingTypesEquality1,
                TestReferences.SymbolsTests.MissingTypes.MissingTypesEquality2,
                TestReferences.SymbolsTests.MDTestLib1,
                TestReferences.SymbolsTests.MDTestLib2
            })

            Dim asm1 = assemblies(0)

            Dim asm1classC = asm1.GlobalNamespace.GetTypeMembers("C").Single()

            Dim asm1m1 = asm1classC.GetMembers("M1").OfType(Of MethodSymbol)().Single()
            Dim asm1m2 = asm1classC.GetMembers("M2").OfType(Of MethodSymbol)().Single()
            Dim asm1m3 = asm1classC.GetMembers("M3").OfType(Of MethodSymbol)().Single()
            Dim asm1m4 = asm1classC.GetMembers("M4").OfType(Of MethodSymbol)().Single()
            Dim asm1m5 = asm1classC.GetMembers("M5").OfType(Of MethodSymbol)().Single()
            Dim asm1m6 = asm1classC.GetMembers("M6").OfType(Of MethodSymbol)().Single()
            Dim asm1m7 = asm1classC.GetMembers("M7").OfType(Of MethodSymbol)().Single()
            Dim asm1m8 = asm1classC.GetMembers("M8").OfType(Of MethodSymbol)().Single()

            Assert.NotEqual(asm1m2.ReturnType, asm1m1.ReturnType)
            Assert.NotEqual(asm1m3.ReturnType, asm1m1.ReturnType)
            Assert.NotEqual(asm1m4.ReturnType, asm1m1.ReturnType)

            Assert.NotEqual(asm1m5.ReturnType, asm1m4.ReturnType)
            Assert.NotEqual(asm1m6.ReturnType, asm1m4.ReturnType)

            Assert.Equal(asm1m7.ReturnType, asm1m1.ReturnType)
            Assert.Equal(asm1m8.ReturnType, asm1m4.ReturnType)

            Dim asm2 = assemblies(1)

            Dim asm2classC = asm2.GlobalNamespace.GetTypeMembers("C").Single()

            Dim asm2m1 = asm2classC.GetMembers("M1").OfType(Of MethodSymbol)().Single()
            Dim asm2m4 = asm2classC.GetMembers("M4").OfType(Of MethodSymbol)().Single()

            Assert.Equal(asm2m1.ReturnType, asm1m1.ReturnType)

            Assert.NotSame(asm1m4.ReturnType, asm2m4.ReturnType)
            Assert.Equal(asm2m4.ReturnType, asm1m4.ReturnType)

            Assert.Equal(asm1.GetSpecialType(SpecialType.System_Boolean), asm1.GetSpecialType(SpecialType.System_Boolean))
            Assert.Equal(asm2.GetSpecialType(SpecialType.System_Boolean), asm1.GetSpecialType(SpecialType.System_Boolean))

            Dim missingTypes1(14) As MissingMetadataTypeSymbol
            Dim missingTypes2(14) As MissingMetadataTypeSymbol

            Dim defaultName = New AssemblyIdentity("missing")

            missingTypes1(0) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(defaultName).Modules(0), "", "test1", 0, True)
            missingTypes1(1) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(defaultName).Modules(0), "", "test1", 1, True)
            missingTypes1(2) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(defaultName).Modules(0), "", "test2", 0, True)
            missingTypes1(3) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(New AssemblyIdentity("asm1")).Modules(0), "", "test1", 0, True)
            missingTypes1(4) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(New AssemblyIdentity("asm1")).Modules(0), "", "test1", 1, True)
            missingTypes1(5) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(New AssemblyIdentity("asm1")).Modules(0), "", "test2", 0, True)
            missingTypes1(6) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(New AssemblyIdentity("asm2")).Modules(0), "", "test1", 0, True)
            missingTypes1(7) = New MissingMetadataTypeSymbol.TopLevel(asm1.Modules(0), "", "test1", 0, True)
            missingTypes1(8) = New MissingMetadataTypeSymbol.TopLevel(asm1.Modules(0), "", "test1", 1, True)
            missingTypes1(9) = New MissingMetadataTypeSymbol.TopLevel(asm1.Modules(0), "", "test2", 0, True)
            missingTypes1(10) = New MissingMetadataTypeSymbol.TopLevel(asm2.Modules(0), "", "test1", 0, True)
            missingTypes1(11) = New MissingMetadataTypeSymbol.Nested(asm1classC, "test1", 0, True)
            missingTypes1(12) = New MissingMetadataTypeSymbol.Nested(asm1classC, "test1", 1, True)
            missingTypes1(13) = New MissingMetadataTypeSymbol.Nested(asm1classC, "test2", 0, True)
            missingTypes1(14) = New MissingMetadataTypeSymbol.Nested(asm2classC, "test1", 0, True)

            missingTypes2(0) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(defaultName).Modules(0), "", "test1", 0, True)
            missingTypes2(1) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(defaultName).Modules(0), "", "test1", 1, True)
            missingTypes2(2) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(defaultName).Modules(0), "", "test2", 0, True)
            missingTypes2(3) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(New AssemblyIdentity("asm1")).Modules(0), "", "test1", 0, True)
            missingTypes2(4) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(New AssemblyIdentity("asm1")).Modules(0), "", "test1", 1, True)
            missingTypes2(5) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(New AssemblyIdentity("asm1")).Modules(0), "", "test2", 0, True)
            missingTypes2(6) = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(New AssemblyIdentity("asm2")).Modules(0), "", "test1", 0, True)
            missingTypes2(7) = New MissingMetadataTypeSymbol.TopLevel(asm1.Modules(0), "", "test1", 0, True)
            missingTypes2(8) = New MissingMetadataTypeSymbol.TopLevel(asm1.Modules(0), "", "test1", 1, True)
            missingTypes2(9) = New MissingMetadataTypeSymbol.TopLevel(asm1.Modules(0), "", "test2", 0, True)
            missingTypes2(10) = New MissingMetadataTypeSymbol.TopLevel(asm2.Modules(0), "", "test1", 0, True)
            missingTypes2(11) = New MissingMetadataTypeSymbol.Nested(asm1classC, "test1", 0, True)
            missingTypes2(12) = New MissingMetadataTypeSymbol.Nested(asm1classC, "test1", 1, True)
            missingTypes2(13) = New MissingMetadataTypeSymbol.Nested(asm1classC, "test2", 0, True)
            missingTypes2(14) = New MissingMetadataTypeSymbol.Nested(asm2classC, "test1", 0, True)

            For i As Integer = 0 To missingTypes1.Length - 1
                For j As Integer = 0 To missingTypes2.Length - 1
                    If (i = j) Then
                        Assert.Equal(missingTypes2(j), missingTypes1(i))
                        Assert.Equal(missingTypes1(i), missingTypes2(j))
                    Else
                        Assert.NotEqual(missingTypes2(j), missingTypes1(i))
                        Assert.NotEqual(missingTypes1(i), missingTypes2(j))
                    End If
                Next
            Next

            Dim missingAssembly = New MissingAssemblySymbol(New AssemblyIdentity("asm1"))
            Assert.True(missingAssembly.Equals(missingAssembly))
            Assert.NotEqual(New Object(), missingAssembly)
            Assert.False(missingAssembly.Equals(Nothing))
        End Sub


    End Class

End Namespace
