' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.FindUsages
Imports System.Threading

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToImplementation
    <[UseExportProvider]>
    Public Class GoToImplementationTests

        Private Shared Async Function TestAsync(workspaceDefinition As XElement, host As TestHost, Optional shouldSucceed As Boolean = True, Optional metadataDefinitions As String() = Nothing) As Task
            Await GoToHelpers.TestAsync(
                workspaceDefinition,
                host,
                Async Function(document As Document, position As Integer, context As SimpleFindUsagesContext) As Task
                    Dim findUsagesService = document.GetLanguageService(Of IFindUsagesService)
                    Await findUsagesService.FindImplementationsAsync(context, document, position, CancellationToken.None).ConfigureAwait(False)
                End Function,
                shouldSucceed,
                metadataDefinitions)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestEmptyFile(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
$$
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host, shouldSucceed:=False)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSingleClass(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|$$C|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithAbstractClass(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
abstract class [|$$C|]
{
}

class [|D|] : C
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithAbstractClassFromInterface(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface $$I { }
abstract class [|C|] : I { }
class [|D|] : C { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSealedClass(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
sealed class [|$$C|]
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithStruct(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
struct [|$$C|]
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithEnum(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
enum [|$$C|]
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithNonAbstractClass(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|$$C|]
{
}

class [|D|] : C
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSingleClassImplementation(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|C|] : I { }
interface $$I { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithTwoClassImplementations(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|C|] : I { }
class [|D|] : I { }
interface $$I { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_01(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_02(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
interface I { void [|$$M|]() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_03(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { void I.[|M|]() { } }
interface I { void [|$$M|]() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_04(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface C : I 
{
    void I.[|M|]() { }
    void M();
}
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_05(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface C : I
{
    void I.[|M|]() { }
    void M();
}
interface I { void [|$$M|]() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneEventImplementation(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C : I { public event EventHandler [|E|]; }
interface I { event EventHandler $$E; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithTwoMethodImplementations(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
class D : I { public void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithNonInheritedImplementation(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C { public void [|M|]() { } }
class D : C, I { }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(6752, "https://github.com/dotnet/roslyn/issues/6752")>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnBaseClass(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public virtual void [|M|]() { } }
class D : C { public override void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(6752, "https://github.com/dotnet/roslyn/issues/6752")>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnDerivedClass(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C { public virtual void M() { } }
class D : C, I { public override void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(6752, "https://github.com/dotnet/roslyn/issues/6752")>
        Public Async Function TestWithVirtualMethodImplementationAndInterfaceImplementedOnDerivedType(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public virtual void [|M|]() { } }
class D : C, I { public override void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(6752, "https://github.com/dotnet/roslyn/issues/6752")>
        Public Async Function TestWithAbstractMethodImplementation(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public abstract void M() { } }
class D : C { public override void [|M|]() { } }}
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithInterfaceMemberFromMetdataAtUseSite(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C : IDisposable
{
    public void [|Dispose|]()
    {
        IDisposable d;
        d.$$Dispose();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host, metadataDefinitions:={"mscorlib:ActivationContext.Dispose", "mscorlib:AsymmetricAlgorithm.Dispose", "mscorlib:AsyncFlowControl.Dispose", "mscorlib:BinaryReader.Dispose", "mscorlib:BinaryWriter.Dispose", "mscorlib:CancellationTokenRegistration.Dispose", "mscorlib:CancellationTokenSource.Dispose", "mscorlib:CharEnumerator.Dispose", "mscorlib:CountdownEvent.Dispose", "mscorlib:CriticalHandle.Dispose", "mscorlib:CryptoAPITransform.Dispose", "mscorlib:DeriveBytes.Dispose", "mscorlib:Enumerator.Dispose", "mscorlib:Enumerator.Dispose", "mscorlib:Enumerator.Dispose", "mscorlib:Enumerator.Dispose", "mscorlib:EventListener.Dispose", "mscorlib:EventSource.Dispose", "mscorlib:ExecutionContext.Dispose", "mscorlib:FromBase64Transform.Dispose", "mscorlib:HashAlgorithm.Dispose", "mscorlib:HostExecutionContext.Dispose", "mscorlib:IsolatedStorageFile.Dispose", "mscorlib:ManualResetEventSlim.Dispose", "mscorlib:MemoryFailPoint.Dispose", "mscorlib:RandomNumberGenerator.Dispose", "mscorlib:RegistryKey.Dispose", "mscorlib:ResourceReader.Dispose", "mscorlib:ResourceSet.Dispose", "mscorlib:ResourceWriter.Dispose", "mscorlib:RijndaelManagedTransform.Dispose", "mscorlib:SafeHandle.Dispose", "mscorlib:SecureString.Dispose", "mscorlib:SecurityContext.Dispose", "mscorlib:SemaphoreSlim.Dispose", "mscorlib:Stream.Dispose", "mscorlib:SymmetricAlgorithm.Dispose", "mscorlib:Task.Dispose", "mscorlib:TextReader.Dispose", "mscorlib:TextWriter.Dispose", "mscorlib:ThreadLocal.Dispose", "mscorlib:Timer.Dispose", "mscorlib:ToBase64Transform.Dispose", "mscorlib:UnmanagedMemoryAccessor.Dispose", "mscorlib:WaitHandle.Dispose", "mscorlib:WindowsIdentity.Dispose", "mscorlib:WindowsImpersonationContext.Dispose", "mscorlib:X509Certificate.Dispose", "System.Core:CngKey.Dispose", "System.Core:CounterSet.Dispose", "System.Core:CounterSetInstance.Dispose", "System.Core:CounterSetInstanceCounterDataSet.Dispose", "System.Core:ECDiffieHellmanPublicKey.Dispose", "System.Core:Enumerator.Dispose", "System.Core:EventLogConfiguration.Dispose", "System.Core:EventLogPropertySelector.Dispose", "System.Core:EventLogReader.Dispose", "System.Core:EventLogSession.Dispose", "System.Core:EventLogWatcher.Dispose", "System.Core:EventProvider.Dispose", "System.Core:EventRecord.Dispose", "System.Core:MemoryMappedFile.Dispose", "System.Core:ProviderMetadata.Dispose", "System.Core:ReaderWriterLockSlim.Dispose", "System:AlternateViewCollection.Dispose", "System:AttachmentBase.Dispose", "System:AttachmentCollection.Dispose", "System:Barrier.Dispose", "System:BlockingCollection.Dispose", "System:ClientWebSocket.Dispose", "System:Component.Dispose", "System:Container.Dispose", "System:Enumerator.Dispose", "System:Enumerator.Dispose", "System:Enumerator.Dispose", "System:Enumerator.Dispose", "System:Enumerator.Dispose", "System:Enumerator.Dispose", "System:Enumerator.Dispose", "System:EventHandlerList.Dispose", "System:License.Dispose", "System:LinkedResourceCollection.Dispose", "System:MailMessage.Dispose", "System:MarshalByValueComponent.Dispose", "System:ServiceContainer.Dispose", "System:SmtpClient.Dispose", "System:Socket.Dispose", "System:SocketAsyncEventArgs.Dispose", "System:TcpClient.Dispose", "System:TraceListener.Dispose", "System:UdpClient.Dispose", "System:WebResponse.Dispose", "System:X509Chain.Dispose", "System:X509Store.Dispose"})
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSimpleMethod(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public void [|$$M|]() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOverridableMethodOnBase(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public virtual void [|$$M|]() { }
}

class D : C
{
    public override void [|M|]() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOverridableMethodOnImplementation(host As TestHost) As Task
            ' Our philosophy is to only show derived in this case, since we know the implementation of 
            ' D could never call C.M here
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public virtual void M() { }
}

class D : C
{
    public override void [|$$M|]() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(19700, "https://github.com/dotnet/roslyn/issues/19700")>
        Public Async Function TestWithIntermediateAbstractOverrides(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    abstract class A {
        public virtual void $$[|M|]() { }
    }
    abstract class B : A {
        public abstract override void M();
    }
    sealed class C1 : B {
        public override void [|M|]() { }
    }
    sealed class C2 : A {
        public override void [|M|]() => base.M();
    }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(43093, "https://github.com/dotnet/roslyn/issues/43093")>
        Public Async Function TestMultiTargetting1(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Name="BaseProjectCore" Language="C#" CommonReferencesNetCoreApp="true">
        <Document FilePath="C.cs">
public interface $$IInterface
{
}
        </Document>
    </Project>
    <Project Name="BaseProjectStandard" Language="C#" CommonReferencesNetStandard20="true">
        <Document IsLinkFile="true" LinkProjectName="BaseProjectCore" LinkFilePath="C.cs">
public interface IInterface
{
}
        </Document>
    </Project>
    <Project Name="ImplProject" Language="C#" CommonReferences="true">
        <ProjectReference>BaseProjectStandard</ProjectReference>
        <Document>
public class [|Impl|] : IInterface
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(46818, "https://github.com/dotnet/roslyn/issues/46818")>
        Public Async Function TestCrossTargetting1(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Name="BaseProjectCore" Language="C#" CommonReferencesNetCoreApp="true">
        <ProjectReference>BaseProjectStandard</ProjectReference>
        <Document FilePath="C.cs">
using System;
using System.Threading.Tasks;

namespace MultiTargettingCore
{
    public class Class1
    {
        static async Task Main(string[] args)
        {
            IStringCreator strCreator = new StringCreator();
            var result = await strCreator.$$CreateStringAsync();
        }
    }
}
        </Document>
    </Project>
    <Project Name="BaseProjectStandard" Language="C#" CommonReferencesNetStandard20="true">
        <Document>
using System.Threading.Tasks;

public interface IStringCreator
{
    Task&lt;string&gt; CreateStringAsync();
}

public class StringCreator : IStringCreator
{
    public async Task&lt;string&gt; [|CreateStringAsync|]()
    {
        return "Another hello world - async!";
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(46818, "https://github.com/dotnet/roslyn/issues/46818")>
        Public Async Function TestCrossTargetting2(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Name="BaseProjectCore" Language="C#" CommonReferencesNetCoreApp="true">
        <ProjectReference>BaseProjectStandard</ProjectReference>
        <Document FilePath="C.cs">
using System;
using System.Threading.Tasks;

namespace MultiTargettingCore
{
    public class Class1
    {
        static async Task Main(string[] args)
        {
            IStringCreator strCreator = new StringCreator();
            var result = await strCreator.$$CreateTupleAsync();
        }
    }
}
        </Document>
    </Project>
    <Project Name="BaseProjectStandard" Language="C#" CommonReferencesNetStandard20="true">
        <Document>
using System.Threading.Tasks;

public interface IStringCreator
{
    Task&lt;(string s, string t)&gt; CreateTupleAsync();
}

public class StringCreator : IStringCreator
{
    public async Task&lt;(string x, string y)&gt; [|CreateTupleAsync|]()
    {
        return default;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(46818, "https://github.com/dotnet/roslyn/issues/46818")>
        Public Async Function TestCrossTargetting3(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Name="BaseProjectCore" Language="C#" CommonReferencesNetCoreApp="true">
        <ProjectReference>BaseProjectStandard</ProjectReference>
        <Document FilePath="C.cs">
using System;
using System.Threading.Tasks;

namespace MultiTargettingCore
{
    public class Class1
    {
        static async Task Main(string[] args)
        {
            IStringCreator strCreator = new StringCreator();
            var result = await strCreator.$$CreateNintAsync();
        }
    }
}
        </Document>
    </Project>
    <Project Name="BaseProjectStandard" Language="C#" CommonReferencesNetStandard20="true">
        <Document>
using System.Threading.Tasks;

public interface IStringCreator
{
    Task&lt;nint&gt; CreateNintAsync();
}

public class StringCreator : IStringCreator
{
    public async Task&lt;nint&gt; [|CreateNintAsync|]()
    {
        return default;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(26167, "https://github.com/dotnet/roslyn/issues/26167")>
        Public Async Function SkipIntermediaryAbstractMethodIfOverridden(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public abstract void M(); }
class D : C { public override void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(26167, "https://github.com/dotnet/roslyn/issues/26167")>
        Public Async Function IncludeAbstractMethodIfNotOverridden(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public abstract void [|M|](); }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestUnsignedRightShiftImplementation_01(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I&lt;C&gt; { public static C operator [|>>>|](C x, int y) { return x; } }
interface I&lt;T&gt; { static abstract T operator $$>>>(T x, int y); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestUnsignedRightShiftImplementation_02(host As TestHost) As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I&lt;C&gt; { static C I&lt;C&gt;.operator [|>>>|](C x, int y) { return x; } }
interface I&lt;T&gt; { static abstract T operator $$>>>(T x, int y); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, host)
        End Function
    End Class
End Namespace
