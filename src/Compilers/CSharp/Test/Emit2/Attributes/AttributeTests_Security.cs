// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Security : WellKnownAttributesTestBase
    {
        #region Functional Tests

        [WorkItem(544918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544918")]
        [Fact]
        public void HostProtectionSecurityAttribute()
        {
            string source = @"
[System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
public struct EventDescriptor
{
}";
            Func<bool, Action<ModuleSymbol>> attributeValidator = isFromSource => (ModuleSymbol module) =>
            {
                var assembly = module.ContainingAssembly;
                var type = (Cci.ITypeDefinition)module.GlobalNamespace.GetMember("EventDescriptor").GetCciAdapter();

                if (isFromSource)
                {
                    var sourceAssembly = (SourceAssemblySymbol)assembly;
                    var compilation = sourceAssembly.DeclaringCompilation;

                    Assert.True(type.HasDeclarativeSecurity);
                    IEnumerable<Cci.SecurityAttribute> typeSecurityAttributes = type.SecurityAttributes;

                    // Get System.Security.Permissions.HostProtection
                    var emittedName = MetadataTypeName.FromNamespaceAndTypeName("System.Security.Permissions", "HostProtectionAttribute");
                    NamedTypeSymbol hostProtectionAttr = sourceAssembly.CorLibrary.LookupDeclaredTopLevelMetadataType(ref emittedName);
                    Assert.NotNull(hostProtectionAttr);

                    // Verify type security attributes
                    Assert.Equal(1, typeSecurityAttributes.Count());

                    // Verify [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
                    var securityAttribute = typeSecurityAttributes.First();
                    Assert.Equal(DeclarativeSecurityAction.LinkDemand, securityAttribute.Action);
                    var typeAttribute = (CSharpAttributeData)securityAttribute.Attribute;
                    Assert.Equal(hostProtectionAttr, typeAttribute.AttributeClass);
                    Assert.Equal(0, typeAttribute.CommonConstructorArguments.Length);
                    typeAttribute.VerifyNamedArgumentValue(0, "MayLeakOnAbort", TypedConstantKind.Primitive, true);
                }
            };

            CompileAndVerifyWithMscorlib40(source, symbolValidator: attributeValidator(false), sourceSymbolValidator: attributeValidator(true));
        }

        [Fact, WorkItem(544956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544956")]
        public void SuppressUnmanagedCodeSecurityAttribute()
        {
            string source = @"
[System.Security.SuppressUnmanagedCodeSecurityAttribute]
class Goo
{
    [System.Security.SuppressUnmanagedCodeSecurityAttribute]
    public static void Main() {}
}";
            CompileAndVerify(source);
        }

        [WorkItem(544929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544929")]
        [Fact]
        public void PrincipalPermissionAttribute()
        {
            string source = @"
using System.Security.Permissions;
 
class Program
{
    [PrincipalPermission((SecurityAction)1)]        // Native compiler allows this security action value for type/method security attributes, but not for assembly.
    [PrincipalPermission(SecurityAction.Assert)]
    [PrincipalPermission(SecurityAction.Demand)]
    [PrincipalPermission(SecurityAction.Deny)]
    [PrincipalPermission(SecurityAction.InheritanceDemand)]     // CS7052
    [PrincipalPermission(SecurityAction.LinkDemand)]            // CS7052
    [PrincipalPermission(SecurityAction.PermitOnly)]
    static void Main(string[] args)
    {
    }
}";
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (9,26): warning CS0618: 'System.Security.Permissions.SecurityAction.Deny' is obsolete: 'Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                //     [PrincipalPermission(SecurityAction.Deny)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.Deny").WithArguments("System.Security.Permissions.SecurityAction.Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (10,26): error CS7052: SecurityAction value 'SecurityAction.InheritanceDemand' is invalid for PrincipalPermission attribute
                //     [PrincipalPermission(SecurityAction.InheritanceDemand)]     // CS7052
                Diagnostic(ErrorCode.ERR_PrincipalPermissionInvalidAction, "SecurityAction.InheritanceDemand").WithArguments("SecurityAction.InheritanceDemand"),
                // (11,26): error CS7052: SecurityAction value 'SecurityAction.LinkDemand' is invalid for PrincipalPermission attribute
                //     [PrincipalPermission(SecurityAction.LinkDemand)]            // CS7052
                Diagnostic(ErrorCode.ERR_PrincipalPermissionInvalidAction, "SecurityAction.LinkDemand").WithArguments("SecurityAction.LinkDemand"));
        }

        [WorkItem(544918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544918")]
        [Fact]
        public void CS7048ERR_SecurityAttributeMissingAction()
        {
            string source = @"
using System.Security;
using System.Security.Permissions;

class MySecurityAttribute : CodeAccessSecurityAttribute
{
    public bool Field;
    public bool Prop { get; set; }
    public override IPermission CreatePermission() { return null; }

    public MySecurityAttribute() : base(SecurityAction.Assert) { }
    public MySecurityAttribute(int x, SecurityAction a1) : base(a1) { }
}

[MySecurityAttribute()]
[MySecurityAttribute(Field = true)]
[MySecurityAttribute(Field = true, Prop = true)]
[MySecurityAttribute(Prop = true)]
[MySecurityAttribute(Prop = true, Field = true)]
[MySecurityAttribute(x: 0, a1: SecurityAction.Assert)]
[MySecurityAttribute(a1: SecurityAction.Assert, x: 0)]
public class C {}
";
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (15,2): error CS7048: First argument to a security attribute must be a valid SecurityAction
                // [MySecurityAttribute()]
                Diagnostic(ErrorCode.ERR_SecurityAttributeMissingAction, "MySecurityAttribute"),
                // (16,2): error CS7048: First argument to a security attribute must be a valid SecurityAction
                // [MySecurityAttribute(Field = true)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeMissingAction, "MySecurityAttribute"),
                // (17,2): error CS7048: First argument to a security attribute must be a valid SecurityAction
                // [MySecurityAttribute(Field = true, Prop = true)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeMissingAction, "MySecurityAttribute"),
                // (18,2): error CS7048: First argument to a security attribute must be a valid SecurityAction
                // [MySecurityAttribute(Prop = true)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeMissingAction, "MySecurityAttribute"),
                // (19,2): error CS7048: First argument to a security attribute must be a valid SecurityAction
                // [MySecurityAttribute(Prop = true, Field = true)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeMissingAction, "MySecurityAttribute"),
                // (20,2): error CS7048: First argument to a security attribute must be a valid SecurityAction
                // [MySecurityAttribute(x: 0, a1: SecurityAction.Assert)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeMissingAction, "MySecurityAttribute"),
                // (21,2): error CS7048: First argument to a security attribute must be a valid SecurityAction
                // [MySecurityAttribute(a1: SecurityAction.Assert, x: 0)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeMissingAction, "MySecurityAttribute"));
        }

        [WorkItem(544918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544918")]
        [Fact]
        public void CS7049ERR_SecurityAttributeInvalidAction()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    [PrincipalPermission((SecurityAction)0)]        // Invalid attribute argument
    [PrincipalPermission((SecurityAction)11)]       // Invalid attribute argument
    [PrincipalPermission((SecurityAction)(-1))]     // Invalid attribute argument
    [PrincipalPermission()]                         // Invalid attribute constructor
    public class C
	{
        [PrincipalPermission(SecurityAction.Demand)]   // Invalid attribute target
        public int x;
    }
}";
            var compilation = CreateCompilationWithMscorlib40(source);
            compilation.VerifyDiagnostics(
                // (9,6): error CS7036: There is no argument given that corresponds to the required parameter 'action' of 'System.Security.Permissions.PrincipalPermissionAttribute.PrincipalPermissionAttribute(System.Security.Permissions.SecurityAction)'
                //     [PrincipalPermission()]                         // Invalid attribute constructor
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "PrincipalPermission()").WithArguments("action", "System.Security.Permissions.PrincipalPermissionAttribute.PrincipalPermissionAttribute(System.Security.Permissions.SecurityAction)").WithLocation(9, 6),
                // (6,26): error CS7049: Security attribute 'PrincipalPermission' has an invalid SecurityAction value '(SecurityAction)0'
                //     [PrincipalPermission((SecurityAction)0)]        // Invalid attribute argument
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "(SecurityAction)0").WithArguments("PrincipalPermission", "(SecurityAction)0").WithLocation(6, 26),
                // (7,26): error CS7049: Security attribute 'PrincipalPermission' has an invalid SecurityAction value '(SecurityAction)11'
                //     [PrincipalPermission((SecurityAction)11)]       // Invalid attribute argument
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "(SecurityAction)11").WithArguments("PrincipalPermission", "(SecurityAction)11").WithLocation(7, 26),
                // (8,26): error CS7049: Security attribute 'PrincipalPermission' has an invalid SecurityAction value '(SecurityAction)(-1)'
                //     [PrincipalPermission((SecurityAction)(-1))]     // Invalid attribute argument
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "(SecurityAction)(-1)").WithArguments("PrincipalPermission", "(SecurityAction)(-1)").WithLocation(8, 26),
                // (12,10): error CS0592: Attribute 'PrincipalPermission' is not valid on this declaration type. It is only valid on 'class, method' declarations.
                //         [PrincipalPermission(SecurityAction.Demand)]   // Invalid attribute target
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "PrincipalPermission").WithArguments("PrincipalPermission", "class, method").WithLocation(12, 10));
        }

        [WorkItem(544918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544918")]
        [Fact]
        public void CS7049ERR_SecurityAttributeInvalidAction_02()
        {
            string source = @"
using System.Security.Permissions;

public class MySecurityAttribute : SecurityAttribute
{
    public MySecurityAttribute(SecurityAction action): base(action) {}
    public override System.Security.IPermission CreatePermission() { return null; }
}

namespace N
{
    [MySecurityAttribute((SecurityAction)0)]        // Invalid attribute argument
    [MySecurityAttribute((SecurityAction)11)]       // Invalid attribute argument
    [MySecurityAttribute((SecurityAction)(-1))]     // Invalid attribute argument
    [MySecurityAttribute()]                         // Invalid attribute constructor
    public class C
	{
        [MySecurityAttribute(SecurityAction.Demand)]   // Invalid attribute target
        public int x;
    }
}";
            var compilation = CreateCompilationWithMscorlib40(source);
            compilation.VerifyDiagnostics(
                // (12,26): error CS7049: Security attribute 'MySecurityAttribute' has an invalid SecurityAction value '(SecurityAction)0'
                //     [MySecurityAttribute((SecurityAction)0)]        // Invalid attribute argument
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "(SecurityAction)0").WithArguments("MySecurityAttribute", "(SecurityAction)0").WithLocation(12, 26),
                // (13,26): error CS7049: Security attribute 'MySecurityAttribute' has an invalid SecurityAction value '(SecurityAction)11'
                //     [MySecurityAttribute((SecurityAction)11)]       // Invalid attribute argument
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "(SecurityAction)11").WithArguments("MySecurityAttribute", "(SecurityAction)11").WithLocation(13, 26),
                // (14,26): error CS7049: Security attribute 'MySecurityAttribute' has an invalid SecurityAction value '(SecurityAction)(-1)'
                //     [MySecurityAttribute((SecurityAction)(-1))]     // Invalid attribute argument
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "(SecurityAction)(-1)").WithArguments("MySecurityAttribute", "(SecurityAction)(-1)").WithLocation(14, 26),
                // (15,6): error CS7036: There is no argument given that corresponds to the required parameter 'action' of 'MySecurityAttribute.MySecurityAttribute(System.Security.Permissions.SecurityAction)'
                //     [MySecurityAttribute()]                         // Invalid attribute constructor
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "MySecurityAttribute()").WithArguments("action", "MySecurityAttribute.MySecurityAttribute(System.Security.Permissions.SecurityAction)").WithLocation(15, 6),
                // (18,10): error CS0592: Attribute 'MySecurityAttribute' is not valid on this declaration type. It is only valid on 'assembly, class, struct, constructor, method' declarations.
                //         [MySecurityAttribute(SecurityAction.Demand)]   // Invalid attribute target
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "MySecurityAttribute").WithArguments("MySecurityAttribute", "assembly, class, struct, constructor, method").WithLocation(18, 10));
        }

        [WorkItem(544918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544918")]
        [Fact]
        public void ValidSecurityAttributeActionsForAssembly()
        {
            string source = @"
using System;
using System.Security;
using System.Security.Permissions;

[assembly: MySecurityAttribute(SecurityAction.RequestMinimum)]
[assembly: MySecurityAttribute(SecurityAction.RequestOptional)]
[assembly: MySecurityAttribute(SecurityAction.RequestRefuse)]

[assembly: MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)]
[assembly: MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)]
[assembly: MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)]


class MySecurityAttribute : SecurityAttribute
{
    public MySecurityAttribute(SecurityAction a) : base(a) { }
    public override IPermission CreatePermission() { return null; }
}

class MyCodeAccessSecurityAttribute : CodeAccessSecurityAttribute
{
    public MyCodeAccessSecurityAttribute(SecurityAction a) : base(a) { }
    public override IPermission CreatePermission() { return null; }
    public static void Main() {}
}
";
            CompileAndVerify(source);
        }

        [WorkItem(544918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544918")]
        [Fact]
        public void CS7050ERR_SecurityAttributeInvalidActionAssembly()
        {
            string source = @"
using System.Security;
using System.Security.Permissions;

[assembly: MySecurityAttribute((SecurityAction)1)]        // Native compiler allows this security action value for type/method security attributes, but not for assembly.
[assembly: MySecurityAttribute(SecurityAction.Assert)]
[assembly: MySecurityAttribute(SecurityAction.Demand)]
[assembly: MySecurityAttribute(SecurityAction.Deny)]
[assembly: MySecurityAttribute(SecurityAction.InheritanceDemand)]
[assembly: MySecurityAttribute(SecurityAction.LinkDemand)]
[assembly: MySecurityAttribute(SecurityAction.PermitOnly)]

[assembly: MyCodeAccessSecurityAttribute((SecurityAction)1)]        // Native compiler allows this security action value for type/method security attributes, but not for assembly.
[assembly: MyCodeAccessSecurityAttribute(SecurityAction.Assert)]
[assembly: MyCodeAccessSecurityAttribute(SecurityAction.Demand)]
[assembly: MyCodeAccessSecurityAttribute(SecurityAction.Deny)]
[assembly: MyCodeAccessSecurityAttribute(SecurityAction.InheritanceDemand)]
[assembly: MyCodeAccessSecurityAttribute(SecurityAction.LinkDemand)]
[assembly: MyCodeAccessSecurityAttribute(SecurityAction.PermitOnly)]


class MySecurityAttribute : SecurityAttribute
{
    public MySecurityAttribute(SecurityAction a) : base(a) { }
    public override IPermission CreatePermission() { return null; }
}

class MyCodeAccessSecurityAttribute : CodeAccessSecurityAttribute
{
    public MyCodeAccessSecurityAttribute(SecurityAction a) : base(a) { }
    public override IPermission CreatePermission() { return null; }
    public static void Main() {}
}";
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (8,32): warning CS0618: 'System.Security.Permissions.SecurityAction.Deny' is obsolete: 'Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: MySecurityAttribute(SecurityAction.Deny)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.Deny").WithArguments("System.Security.Permissions.SecurityAction.Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (16,42): warning CS0618: 'System.Security.Permissions.SecurityAction.Deny' is obsolete: 'Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: MyCodeAccessSecurityAttribute(SecurityAction.Deny)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.Deny").WithArguments("System.Security.Permissions.SecurityAction.Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (5,32): error CS7050: SecurityAction value '(SecurityAction)1' is invalid for security attributes applied to an assembly
                // [assembly: MySecurityAttribute((SecurityAction)1)]        // Native compiler allows this security action value for type/method security attributes, but not for assembly.
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "(SecurityAction)1").WithArguments("(SecurityAction)1"),
                // (6,32): error CS7050: SecurityAction value 'SecurityAction.Assert' is invalid for security attributes applied to an assembly
                // [assembly: MySecurityAttribute(SecurityAction.Assert)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Assert").WithArguments("SecurityAction.Assert"),
                // (7,32): error CS7050: SecurityAction value 'SecurityAction.Demand' is invalid for security attributes applied to an assembly
                // [assembly: MySecurityAttribute(SecurityAction.Demand)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Demand").WithArguments("SecurityAction.Demand"),
                // (8,32): error CS7050: SecurityAction value 'SecurityAction.Deny' is invalid for security attributes applied to an assembly
                // [assembly: MySecurityAttribute(SecurityAction.Deny)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Deny").WithArguments("SecurityAction.Deny"),
                // (9,32): error CS7050: SecurityAction value 'SecurityAction.InheritanceDemand' is invalid for security attributes applied to an assembly
                // [assembly: MySecurityAttribute(SecurityAction.InheritanceDemand)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.InheritanceDemand").WithArguments("SecurityAction.InheritanceDemand"),
                // (10,32): error CS7050: SecurityAction value 'SecurityAction.LinkDemand' is invalid for security attributes applied to an assembly
                // [assembly: MySecurityAttribute(SecurityAction.LinkDemand)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.LinkDemand").WithArguments("SecurityAction.LinkDemand"),
                // (11,32): error CS7050: SecurityAction value 'SecurityAction.PermitOnly' is invalid for security attributes applied to an assembly
                // [assembly: MySecurityAttribute(SecurityAction.PermitOnly)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.PermitOnly").WithArguments("SecurityAction.PermitOnly"),
                // (13,42): error CS7050: SecurityAction value '(SecurityAction)1' is invalid for security attributes applied to an assembly
                // [assembly: MyCodeAccessSecurityAttribute((SecurityAction)1)]        // Native compiler allows this security action value for type/method security attributes, but not for assembly.
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "(SecurityAction)1").WithArguments("(SecurityAction)1"),
                // (14,42): error CS7050: SecurityAction value 'SecurityAction.Assert' is invalid for security attributes applied to an assembly
                // [assembly: MyCodeAccessSecurityAttribute(SecurityAction.Assert)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Assert").WithArguments("SecurityAction.Assert"),
                // (15,42): error CS7050: SecurityAction value 'SecurityAction.Demand' is invalid for security attributes applied to an assembly
                // [assembly: MyCodeAccessSecurityAttribute(SecurityAction.Demand)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Demand").WithArguments("SecurityAction.Demand"),
                // (16,42): error CS7050: SecurityAction value 'SecurityAction.Deny' is invalid for security attributes applied to an assembly
                // [assembly: MyCodeAccessSecurityAttribute(SecurityAction.Deny)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Deny").WithArguments("SecurityAction.Deny"),
                // (17,42): error CS7050: SecurityAction value 'SecurityAction.InheritanceDemand' is invalid for security attributes applied to an assembly
                // [assembly: MyCodeAccessSecurityAttribute(SecurityAction.InheritanceDemand)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.InheritanceDemand").WithArguments("SecurityAction.InheritanceDemand"),
                // (18,42): error CS7050: SecurityAction value 'SecurityAction.LinkDemand' is invalid for security attributes applied to an assembly
                // [assembly: MyCodeAccessSecurityAttribute(SecurityAction.LinkDemand)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.LinkDemand").WithArguments("SecurityAction.LinkDemand"),
                // (19,42): error CS7050: SecurityAction value 'SecurityAction.PermitOnly' is invalid for security attributes applied to an assembly
                // [assembly: MyCodeAccessSecurityAttribute(SecurityAction.PermitOnly)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.PermitOnly").WithArguments("SecurityAction.PermitOnly"));
        }

        [WorkItem(544918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544918")]
        [Fact]
        public void ValidSecurityAttributeActionsForTypeOrMethod()
        {
            string source = @"
using System;
using System.Security;
using System.Security.Permissions;

class MySecurityAttribute : SecurityAttribute
{
    public MySecurityAttribute(SecurityAction a) : base(a) { }
    public override IPermission CreatePermission() { return null; }
}

class MyCodeAccessSecurityAttribute : CodeAccessSecurityAttribute
{
    public MyCodeAccessSecurityAttribute(SecurityAction a) : base(a) { }
    public override IPermission CreatePermission() { return null; }
}

[MySecurityAttribute((SecurityAction)1)]        // Native compiler allows this security action value for type/method security attributes, but not for assembly.
[MySecurityAttribute(SecurityAction.Assert)]
[MySecurityAttribute(SecurityAction.Demand)]
[MySecurityAttribute(SecurityAction.Deny)]
[MySecurityAttribute(SecurityAction.InheritanceDemand)]
[MySecurityAttribute(SecurityAction.LinkDemand)]
[MySecurityAttribute(SecurityAction.PermitOnly)]
[MyCodeAccessSecurityAttribute((SecurityAction)1)]        // Native compiler allows this security action value for type/method security attributes, but not for assembly.
[MyCodeAccessSecurityAttribute(SecurityAction.Assert)]
[MyCodeAccessSecurityAttribute(SecurityAction.Demand)]
[MyCodeAccessSecurityAttribute(SecurityAction.Deny)]
[MyCodeAccessSecurityAttribute(SecurityAction.InheritanceDemand)]
[MyCodeAccessSecurityAttribute(SecurityAction.LinkDemand)]
[MyCodeAccessSecurityAttribute(SecurityAction.PermitOnly)]
class Test
{
    [MySecurityAttribute((SecurityAction)1)]        // Native compiler allows this security action value for type/method security attributes, but not for assembly.
    [MySecurityAttribute(SecurityAction.Assert)]
    [MySecurityAttribute(SecurityAction.Demand)]
    [MySecurityAttribute(SecurityAction.Deny)]
    [MySecurityAttribute(SecurityAction.InheritanceDemand)]
    [MySecurityAttribute(SecurityAction.LinkDemand)]
    [MySecurityAttribute(SecurityAction.PermitOnly)]
    [MyCodeAccessSecurityAttribute((SecurityAction)1)]        // Native compiler allows this security action value for type/method security attributes, but not for assembly.
    [MyCodeAccessSecurityAttribute(SecurityAction.Assert)]
    [MyCodeAccessSecurityAttribute(SecurityAction.Demand)]
    [MyCodeAccessSecurityAttribute(SecurityAction.Deny)]
    [MyCodeAccessSecurityAttribute(SecurityAction.InheritanceDemand)]
    [MyCodeAccessSecurityAttribute(SecurityAction.LinkDemand)]
    [MyCodeAccessSecurityAttribute(SecurityAction.PermitOnly)]
    public static void Main() {}
}
";
            CompileAndVerify(source);
        }

        [WorkItem(544918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544918")]
        [Fact]
        public void CS7051ERR_SecurityAttributeInvalidActionTypeOrMethod()
        {
            string source = @"
using System.Security;
using System.Security.Permissions;

class MySecurityAttribute : SecurityAttribute
{
    public MySecurityAttribute(SecurityAction a) : base(a) { }
    public override IPermission CreatePermission() { return null; }
}

class MyCodeAccessSecurityAttribute : CodeAccessSecurityAttribute
{
    public MyCodeAccessSecurityAttribute(SecurityAction a) : base(a) { }
    public override IPermission CreatePermission() { return null; }
}

[MySecurityAttribute(SecurityAction.RequestMinimum)]
[MySecurityAttribute(SecurityAction.RequestOptional)]
[MySecurityAttribute(SecurityAction.RequestRefuse)]
[MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)]
[MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)]
[MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)]
class Test
{
    [MySecurityAttribute(SecurityAction.RequestMinimum)]
    [MySecurityAttribute(SecurityAction.RequestOptional)]
    [MySecurityAttribute(SecurityAction.RequestRefuse)]
    [MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)]
    [MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)]
    [MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)]
    public static void Main() {}
}";
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (17,22): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [MySecurityAttribute(SecurityAction.RequestMinimum)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (18,22): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestOptional' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [MySecurityAttribute(SecurityAction.RequestOptional)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestOptional").WithArguments("System.Security.Permissions.SecurityAction.RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (19,22): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestRefuse' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [MySecurityAttribute(SecurityAction.RequestRefuse)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestRefuse").WithArguments("System.Security.Permissions.SecurityAction.RequestRefuse", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (20,32): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (21,32): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestOptional' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestOptional").WithArguments("System.Security.Permissions.SecurityAction.RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (22,32): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestRefuse' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestRefuse").WithArguments("System.Security.Permissions.SecurityAction.RequestRefuse", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (17,22): error CS7051: SecurityAction value 'SecurityAction.RequestMinimum' is invalid for security attributes applied to a type or a method
                // [MySecurityAttribute(SecurityAction.RequestMinimum)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestMinimum").WithArguments("SecurityAction.RequestMinimum"),
                // (18,22): error CS7051: SecurityAction value 'SecurityAction.RequestOptional' is invalid for security attributes applied to a type or a method
                // [MySecurityAttribute(SecurityAction.RequestOptional)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestOptional").WithArguments("SecurityAction.RequestOptional"),
                // (19,22): error CS7051: SecurityAction value 'SecurityAction.RequestRefuse' is invalid for security attributes applied to a type or a method
                // [MySecurityAttribute(SecurityAction.RequestRefuse)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestRefuse").WithArguments("SecurityAction.RequestRefuse"),
                // (20,32): error CS7051: SecurityAction value 'SecurityAction.RequestMinimum' is invalid for security attributes applied to a type or a method
                // [MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestMinimum").WithArguments("SecurityAction.RequestMinimum"),
                // (21,32): error CS7051: SecurityAction value 'SecurityAction.RequestOptional' is invalid for security attributes applied to a type or a method
                // [MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestOptional").WithArguments("SecurityAction.RequestOptional"),
                // (22,32): error CS7051: SecurityAction value 'SecurityAction.RequestRefuse' is invalid for security attributes applied to a type or a method
                // [MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestRefuse").WithArguments("SecurityAction.RequestRefuse"),
                // (25,26): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                //     [MySecurityAttribute(SecurityAction.RequestMinimum)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (26,26): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestOptional' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                //     [MySecurityAttribute(SecurityAction.RequestOptional)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestOptional").WithArguments("System.Security.Permissions.SecurityAction.RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (27,26): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestRefuse' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                //     [MySecurityAttribute(SecurityAction.RequestRefuse)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestRefuse").WithArguments("System.Security.Permissions.SecurityAction.RequestRefuse", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (28,36): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                //     [MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (29,36): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestOptional' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                //     [MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestOptional").WithArguments("System.Security.Permissions.SecurityAction.RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (30,36): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestRefuse' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                //     [MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestRefuse").WithArguments("System.Security.Permissions.SecurityAction.RequestRefuse", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (25,26): error CS7051: SecurityAction value 'SecurityAction.RequestMinimum' is invalid for security attributes applied to a type or a method
                //     [MySecurityAttribute(SecurityAction.RequestMinimum)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestMinimum").WithArguments("SecurityAction.RequestMinimum"),
                // (26,26): error CS7051: SecurityAction value 'SecurityAction.RequestOptional' is invalid for security attributes applied to a type or a method
                //     [MySecurityAttribute(SecurityAction.RequestOptional)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestOptional").WithArguments("SecurityAction.RequestOptional"),
                // (27,26): error CS7051: SecurityAction value 'SecurityAction.RequestRefuse' is invalid for security attributes applied to a type or a method
                //     [MySecurityAttribute(SecurityAction.RequestRefuse)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestRefuse").WithArguments("SecurityAction.RequestRefuse"),
                // (28,36): error CS7051: SecurityAction value 'SecurityAction.RequestMinimum' is invalid for security attributes applied to a type or a method
                //     [MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestMinimum").WithArguments("SecurityAction.RequestMinimum"),
                // (29,36): error CS7051: SecurityAction value 'SecurityAction.RequestOptional' is invalid for security attributes applied to a type or a method
                //     [MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestOptional").WithArguments("SecurityAction.RequestOptional"),
                // (30,36): error CS7051: SecurityAction value 'SecurityAction.RequestRefuse' is invalid for security attributes applied to a type or a method
                //     [MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestRefuse").WithArguments("SecurityAction.RequestRefuse"));
        }

        [WorkItem(546623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546623")]
        [Fact]
        public void CS7070ERR_SecurityAttributeInvalidTarget()
        {
            string source = @"
using System;
using System.Security;
using System.Security.Permissions;
 
class Program
{
    [MyPermission(SecurityAction.Demand)]
    public int x = 0;
}
 
[AttributeUsage(AttributeTargets.All)]
class MyPermissionAttribute : CodeAccessSecurityAttribute
{
    public MyPermissionAttribute(SecurityAction action) : base(action)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}";
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (8,6): error CS7070: Security attribute 'MyPermission' is not valid on this declaration type. Security attributes are only valid on assembly, type and method declarations.
                //     [MyPermission(SecurityAction.Demand)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidTarget, "MyPermission").WithArguments("MyPermission"));
        }

        [WorkItem(546056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546056")]
        [Fact]
        public void TestMissingCodeAccessSecurityAttributeGeneratesNoErrors()
        {
            string source = @"
namespace System
{
    public enum AttributeTargets { All = 32767, }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets targets) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class MyAttr : Attribute { }
    [MyAttr()] // error here
    public class C { }
}

// following are the minimum code to make Dev11 pass when using /nostdlib option
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public class Enum { }
    public class String { }
    public struct Byte { }
    public struct Int16 { }
    public struct Int32 { }
    public struct Int64 { }
    public struct Single { }
    public struct Double { }
    public struct Char { }
    public struct Boolean { }
    public struct SByte { }
    public struct UInt16 { }
    public struct UInt32 { }
    public struct UInt64 { }
    public struct IntPtr { }
    public struct UIntPtr { }
    public class Delegate { }
    public class MulticastDelegate : Delegate { }
    public class Array { }
    public class Exception { }
    public class Type { }
    public struct Void { }
    public interface IDisposable { void Dispose(); }
    public class Attribute { }
    public class ParamArrayAttribute : Attribute { }
    public struct RuntimeTypeHandle { }
    public struct RuntimeFieldHandle { }
    namespace Runtime.InteropServices
    {
        public class OutAttribute : Attribute { }
    }

    namespace Reflection
    {
        public class DefaultMemberAttribute { }
    }
    namespace Collections
    {
        public interface IEnumerable { }
        public interface IEnumerator { }
    }
}
";
            var comp = CreateEmptyCompilation(source);
            comp.EmitToArray(options: new EmitOptions(runtimeMetadataVersion: "v4.0.31019"), expectedWarnings: new DiagnosticDescription[0]);
        }

        #endregion

        #region Metadata validation tests

        [Fact]
        public void CheckSecurityAttributeOnType()
        {
            string source = @"
using System.Security;
using System.Security.Permissions;

class MySecurityAttribute : SecurityAttribute
{
    public MySecurityAttribute(SecurityAction a) : base(a) { }
    public override IPermission CreatePermission() { return null; }
}

namespace N
{
    [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
    [MySecurityAttribute(SecurityAction.Assert)]
	public class C
	{
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll, assemblyName: "Test");
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Assert,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0050" + // length of UTF-8 string
                        "MySecurityAttribute, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" + // attr type name
                        "\u0001" + // number of bytes in the encoding of the named arguments
                        "\u0000" // number of named arguments
                });
            });
        }

        [Fact]
        public void CheckSecurityAttributeOnMethod()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
	public class C
	{
        [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
		public static void Goo() {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"Goo",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                });
            });
        }

        [Fact]
        public void CheckSecurityAttributeOnLocalFunction()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    public class C
    {
        void M()
        {
            [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
            static void local1() {}
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular9);
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"<M>g__local1|0_0",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                });
            });
        }

        [Fact]
        public void CheckSecurityAttributeOnLambda()
        {
            string source =
@"using System;
using System.Security.Permissions;
class Program
{
    static void Main()
    {
        Action a = [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")] () => { };
    }
}";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"<Main>b__0_0",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                });
            });
        }

        [Fact]
        public void CheckMultipleSecurityAttributes_SameType_SameAction()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	[PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	public class C
	{
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0002" + // number of attributes (small enough to fit in 1 byte)

                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1" + // argument value (@"User1")

                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                });
            });
        }

        [Fact]
        public void CheckMultipleSecurityAttributes_SameMethod_SameAction()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    public class C
	{
        [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	    [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
        public static void Goo() {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"Goo",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0002" + // number of attributes (small enough to fit in 1 byte)

                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1" + // argument value (@"User1")

                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                });
            });
        }

        [Fact]
        public void CheckMultipleSecurityAttributes_SameType_DifferentAction()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	[PrincipalPermission(SecurityAction.Assert, Role=@""User2"")]
	public class C
	{
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Assert,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User2", // argument value (@"User2")
                });
            });
        }

        [Fact]
        public void CheckMultipleSecurityAttributes_SameMethod_DifferentAction()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    public class C
	{
        [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	    [PrincipalPermission(SecurityAction.Assert, Role=@""User2"")]
        public static void Goo() {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"Goo",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Assert,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"Goo",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User2", // argument value (@"User2")
                });
            });
        }

        [Fact]
        public void CheckMultipleSecurityAttributes_DifferentType_SameAction()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	public class C
	{
    }
}

namespace N2
{
    [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	public class C2
	{
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C2",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                });
            });
        }

        [Fact]
        public void CheckMultipleSecurityAttributes_DifferentMethod_SameAction()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    public class C
	{
        [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	    public static void Goo1() {}

        [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	    public static void Goo2() {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"Goo1",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"Goo2",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                });
            });
        }

        [Fact]
        public void CheckMultipleSecurityAttributes_Assembly_Type_Method()
        {
            string source = @"
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration = true)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode = true)]

namespace N
{
    [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
    public class C
	{
        [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	    public static void Goo() {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseDll);

            compilation.VerifyDiagnostics(
                // (4,31): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestOptional' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration = true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestOptional").WithArguments("System.Security.Permissions.SecurityAction.RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (5,31): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode = true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."));

            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestOptional,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u001a" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0015" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "RemotingConfiguration" + // property name
                        "\u0001", // argument value (true)
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0012" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u000d" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "UnmanagedCode" + // property name
                        "\u0001", // argument value (true)
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"Goo",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                });
            });
        }

        [Fact]
        public void CheckMultipleSecurityAttributes_Type_Method_UnsafeDll()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
    public class C
	{
        [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
	    public static void Goo() {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.UnsafeReleaseDll);
            CompileAndVerify(compilation, verify: Verification.Passes, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0015" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0010" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "SkipVerification" + // property name
                        "\u0001", // argument value (true)
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"Goo",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                });
            });
        }

        [Fact]
        public void GetSecurityAttributes_Type_Method_Assembly()
        {
            string source = @"
using System.Security.Permissions;

namespace N
{
    [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
    [PrincipalPermission(SecurityAction.Assert, Role=@""User2"")]
    public class C
	{
        [PrincipalPermission(SecurityAction.Demand, Role=@""User1"")]
        [PrincipalPermission(SecurityAction.Demand, Role=@""User2"")]
	    public static void Goo() {}
    }
}
";

            var compilation = CreateCompilationWithMscorlib40(source, options: TestOptions.UnsafeReleaseDll);
            CompileAndVerify(compilation, verify: Verification.Passes, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0015" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0010" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "SkipVerification" + // property name
                        "\u0001", // argument value (true)
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1", // argument value (@"User1")
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Assert,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"C",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User2", // argument value (@"User2")
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Demand,
                    ParentKind = SymbolKind.Method,
                    ParentNameOpt = @"Goo",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0002" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User1" + // argument value (@"User1")
                        "\u0080\u0085" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u000e" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0004" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "Role" + // property name
                        "\u0005" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "User2", // argument value (@"User2")
                });
            });
        }

        [WorkItem(545084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545084"), WorkItem(529492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529492")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void PermissionSetAttribute_Fixup()
        {
            var tempDir = Temp.CreateDirectory();
            var tempFile = tempDir.CreateFile("pset.xml");

            string text = @"
<PermissionSet class=""System.Security.PermissionSet"" version=""1"">
  <Permission class=""System.Security.Permissions.FileIOPermission, mscorlib"" version=""1""><AllWindows/></Permission>
  <Permission class=""System.Security.Permissions.RegistryPermission, mscorlib"" version=""1""><Unrestricted/></Permission>
</PermissionSet>";

            tempFile.WriteAllText(text);

            string hexFileContent = PermissionSetAttributeWithFileReference.ConvertToHex(new MemoryStream(Encoding.UTF8.GetBytes(text)));

            string source = @"
using System.Security.Permissions;

[PermissionSetAttribute(SecurityAction.Deny, File = @""pset.xml"")]
public class MyClass
{
    public static void Main() 
    {
        typeof(MyClass).GetCustomAttributes(false);
    }
}";
            var syntaxTree = Parse(source);
            var resolver = new XmlFileResolver(tempDir.Path);
            var compilation = CSharpCompilation.Create(
                GetUniqueName(),
                new[] { syntaxTree },
                new[] { MscorlibRef },
                TestOptions.ReleaseDll.WithXmlReferenceResolver(resolver));

            compilation.VerifyDiagnostics(
                // (4,25): warning CS0618: 'System.Security.Permissions.SecurityAction.Deny' is obsolete: 'Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [PermissionSetAttribute(SecurityAction.Deny, File = @"pset.xml")]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.Deny").WithArguments("System.Security.Permissions.SecurityAction.Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."));

            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.Deny,
                    ParentKind = SymbolKind.NamedType,
                    ParentNameOpt = @"MyClass",
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u007f" + // length of string
                        "System.Security.Permissions.PermissionSetAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0082" + "\u008f" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u000e" + // type string
                        "\u0003" + // length of string (small enough to fit in 1 byte)
                        "Hex" + // property name
                        "\u0082" + "\u0086" + // length of string
                        hexFileContent // argument value
                });
            });
        }

        [WorkItem(545084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545084"), WorkItem(529492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529492")]
        [Fact]
        public void CS7056ERR_PermissionSetAttributeInvalidFile()
        {
            string source = @"
using System.Security.Permissions;

[PermissionSetAttribute(SecurityAction.Deny, File = @""NonExistentFile.xml"")]
[PermissionSetAttribute(SecurityAction.Deny, File = null)]
public class MyClass 
{
}";
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (4,25): warning CS0618: 'System.Security.Permissions.SecurityAction.Deny' is obsolete: 'Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [PermissionSetAttribute(SecurityAction.Deny, File = @"NonExistentFile.xml")]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.Deny").WithArguments("System.Security.Permissions.SecurityAction.Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (5,25): warning CS0618: 'System.Security.Permissions.SecurityAction.Deny' is obsolete: 'Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [PermissionSetAttribute(SecurityAction.Deny, File = null)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.Deny").WithArguments("System.Security.Permissions.SecurityAction.Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (4,46): error CS7056: Unable to resolve file path 'NonExistentFile.xml' specified for the named argument 'File' for PermissionSet attribute
                // [PermissionSetAttribute(SecurityAction.Deny, File = @"NonExistentFile.xml")]
                Diagnostic(ErrorCode.ERR_PermissionSetAttributeInvalidFile, @"File = @""NonExistentFile.xml""").WithArguments("NonExistentFile.xml", "File").WithLocation(4, 46),
                // (5,46): error CS7056: Unable to resolve file path '<null>' specified for the named argument 'File' for PermissionSet attribute
                // [PermissionSetAttribute(SecurityAction.Deny, File = null)]
                Diagnostic(ErrorCode.ERR_PermissionSetAttributeInvalidFile, "File = null").WithArguments("<null>", "File").WithLocation(5, 46));
        }

        [Fact]
        public void CS7056ERR_PermissionSetAttributeInvalidFile_WithXmlReferenceResolver()
        {
            var tempDir = Temp.CreateDirectory();
            var tempFile = tempDir.CreateFile("pset.xml");

            string text = @"
<PermissionSet class=""System.Security.PermissionSet"" version=""1"">
</PermissionSet>";

            tempFile.WriteAllText(text);

            string source = @"
using System.Security.Permissions;

[PermissionSetAttribute(SecurityAction.Deny, File = @""NonExistentFile.xml"")]
[PermissionSetAttribute(SecurityAction.Deny, File = null)]
public class MyClass
{
}";
            var resolver = new XmlFileResolver(tempDir.Path);
            CreateCompilationWithMscorlib40(source, options: TestOptions.DebugDll.WithXmlReferenceResolver(resolver)).VerifyDiagnostics(
                // (4,25): warning CS0618: 'System.Security.Permissions.SecurityAction.Deny' is obsolete: 'Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [PermissionSetAttribute(SecurityAction.Deny, File = @"NonExistentFile.xml")]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.Deny").WithArguments("System.Security.Permissions.SecurityAction.Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (5,25): warning CS0618: 'System.Security.Permissions.SecurityAction.Deny' is obsolete: 'Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [PermissionSetAttribute(SecurityAction.Deny, File = null)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.Deny").WithArguments("System.Security.Permissions.SecurityAction.Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (4,46): error CS7056: Unable to resolve file path 'NonExistentFile.xml' specified for the named argument 'File' for PermissionSet attribute
                // [PermissionSetAttribute(SecurityAction.Deny, File = @"NonExistentFile.xml")]
                Diagnostic(ErrorCode.ERR_PermissionSetAttributeInvalidFile, @"File = @""NonExistentFile.xml""").WithArguments("NonExistentFile.xml", "File").WithLocation(4, 46),
                // (5,46): error CS7056: Unable to resolve file path '<null>' specified for the named argument 'File' for PermissionSet attribute
                // [PermissionSetAttribute(SecurityAction.Deny, File = null)]
                Diagnostic(ErrorCode.ERR_PermissionSetAttributeInvalidFile, "File = null").WithArguments("<null>", "File").WithLocation(5, 46));
        }

        [WorkItem(545084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545084"), WorkItem(529492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529492")]
        [Fact]
        public void CS7057ERR_PermissionSetAttributeFileReadError()
        {
            var tempDir = Temp.CreateDirectory();
            string filePath = Path.Combine(tempDir.Path, "pset_01.xml");

            string source = @"
using System.Security.Permissions;

[PermissionSetAttribute(SecurityAction.Deny, File = @""pset_01.xml"")]
public class MyClass
{
    public static void Main()
    {
        typeof(MyClass).GetCustomAttributes(false);
    }
}";
            var syntaxTree = Parse(source);
            CSharpCompilation comp;

            // create file with no file sharing allowed and verify ERR_PermissionSetAttributeFileReadError during emit
            using (var fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None))
            {
                comp = CSharpCompilation.Create(
                    GetUniqueName(),
                    new[] { syntaxTree },
                    new[] { MscorlibRef },
                    TestOptions.ReleaseDll.WithXmlReferenceResolver(new XmlFileResolver(tempDir.Path)));

                comp.VerifyDiagnostics(
                    // (4,25): warning CS0618: 'System.Security.Permissions.SecurityAction.Deny' is obsolete: 'Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                    // [PermissionSetAttribute(SecurityAction.Deny, File = @"pset_01.xml")]
                    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.Deny").WithArguments("System.Security.Permissions.SecurityAction.Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."));

                using (var output = new MemoryStream())
                {
                    var emitResult = comp.Emit(output);

                    Assert.False(emitResult.Success);
                    emitResult.Diagnostics.VerifyErrorCodes(
                        Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr),
                        Diagnostic(ErrorCode.ERR_PermissionSetAttributeFileReadError));
                }
            }

            // emit succeeds now since we closed the file:

            using (var output = new MemoryStream())
            {
                var emitResult = comp.Emit(output);
                Assert.True(emitResult.Success);
            }
        }

        #endregion

        [Fact]
        [WorkItem(1034429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1034429")]
        public void CrashOnParamsInSecurityAttribute()
        {
            const string source = @"
using System.Security.Permissions;

class Program
{
    [A(SecurityAction.Assert)]
    static void Main()
    {
    }
}

public class A : CodeAccessSecurityAttribute
{
    public A(params SecurityAction a)
    {
    }
}";
            CreateCompilationWithMscorlib46(source).GetDiagnostics();
        }

        [Fact]
        [WorkItem(1036339, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036339")]
        public void CrashOnOptionalParameterInSecurityAttribute()
        {
            const string source = @"
using System.Security.Permissions;

[A]
[A()]
class A : CodeAccessSecurityAttribute
{
    public A(SecurityAction a = 0) : base(a)
    {
    }
}";
            CreateCompilationWithMscorlib46(source).VerifyDiagnostics(
                // (4,2): error CS7049: Security attribute 'A' has an invalid SecurityAction value '0'
                // [A]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "A").WithArguments("A", "0").WithLocation(4, 2),
                // (5,2): error CS7049: Security attribute 'A' has an invalid SecurityAction value '0'
                // [A()]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "A()").WithArguments("A", "0").WithLocation(5, 2),
                // (6,7): error CS0534: 'A' does not implement inherited abstract member 'SecurityAttribute.CreatePermission()'
                // class A : CodeAccessSecurityAttribute
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "A").WithArguments("A", "System.Security.Permissions.SecurityAttribute.CreatePermission()").WithLocation(6, 7));
        }
    }
}
