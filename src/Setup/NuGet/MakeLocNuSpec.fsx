#r "System.Xml.Linq.dll"

open System.Xml
open System.IO

type Args = { InFile : string; OutFile : string; ToolsPackage : bool }

let args =
    let argToArgs (args : Args) (arg : string) =
        if arg.StartsWith("/out:") then
            { args with OutFile = arg.[5..] }
        elif arg = "/toolsPackage" then
            { args with ToolsPackage = true }
        else
            { args with InFile = arg }
    Seq.fold
    <| argToArgs
    <| { InFile = ""; OutFile = ""; ToolsPackage = false }
    <| fsi.CommandLineArgs.[1..]
        

let originalXml = 
    let xml = new XmlDocument()
    xml.Load(args.InFile)
    xml

let rewrite = 
    // Is this a tools package? If so, we simply want to add the localized
    // name to the id, change the language, and sub the localized description

    let langString = "$langCode$"
    let ns = "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"
    let nsmgr = new XmlNamespaceManager(originalXml.NameTable)
    nsmgr.AddNamespace("ns", ns)
    let oldId = originalXml.SelectSingleNode("ns:package/ns:metadata/ns:id", nsmgr)
    let oldIdText = oldId.InnerText
    let newId = oldIdText + "." + langString
    oldId.InnerText <- newId

    // Change the <language /> to the language code given as an argument
    let langNode = originalXml.SelectSingleNode("ns:package/ns:metadata/ns:language", nsmgr)
    langNode.InnerText <- langString

    let filesNode = originalXml.SelectSingleNode("ns:package/ns:files", nsmgr)

    if not args.ToolsPackage then
        // Remove all existing dependencies and add one dependency to the original package
        let mutable dependenciesNode = originalXml.SelectSingleNode("ns:package/ns:metadata/ns:dependencies", nsmgr)

        if dependenciesNode <> null then
            dependenciesNode.RemoveAll()
        else
            // If we have no dependencies node, we need to create it
            dependenciesNode <- originalXml.CreateElement("dependencies", ns)

        let originalPackageDependencyNode = originalXml.CreateElement("dependency", ns)
        originalPackageDependencyNode.SetAttribute("id", oldIdText)
        originalPackageDependencyNode.SetAttribute("version", "[$currentVersion$]")
        dependenciesNode.AppendChild(originalPackageDependencyNode) |> ignore

        // Remove requireLicenseAcceptance node
        let rlaNode = originalXml.SelectSingleNode("ns:package/ns:metadata/ns:requireLicenseAcceptance", nsmgr)
        rlaNode.ParentNode.RemoveChild(rlaNode) |> ignore

        // Remove all <file> nodes
        filesNode.RemoveAll()

    // Add a comment about where to add localized files
    let fileComment = originalXml.CreateComment(" Add references to localized resource dlls here." +
                        " Ensure the target includes the loc suffix directory language code")
    filesNode.AppendChild(fileComment) |> ignore

    // Pretty print the xml
    use stringWriter = new StringWriter()
    use writer = new XmlTextWriter(stringWriter)
    writer.Formatting <- Formatting.Indented
    originalXml.WriteContentTo(writer)

    stringWriter.ToString()

if args.OutFile <> "" then
    File.WriteAllText(args.OutFile, rewrite)
else
    printfn "%s" rewrite