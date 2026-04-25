// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Serialization;

public class PlatformAgnosticClientCapabilitiesJsonConverterTest
{
    [Fact]
    public void ReadJson_ReadsValues()
    {
        // Arrange
        // Note this is a small subset of the actual ClientCapabilities provided
        // for use in basic validations.
        var rawJson = @"{
  ""workspace"": {
    ""applyEdit"": true,
    ""workspaceEdit"": {
      ""documentChanges"": true
    }
  },
  ""textDocument"": {
    ""_vs_onAutoInsert"": {
      ""dynamicRegistration"": false
    },
    ""synchronization"": {
      ""willSave"": false,
      ""willSaveWaitUntil"": false,
      ""didSave"": true,
      ""dynamicRegistration"": false
    },
    ""completion"": {
      ""completionItem"": {
        ""snippetSupport"": false,
        ""commitCharactersSupport"": true
      },
      ""completionItemKind"": {
        ""valueSet"": [
          3
        ]
      },
      ""contextSupport"": false,
      ""dynamicRegistration"": false
    },
    ""hover"": {
      ""contentFormat"": [
        ""plaintext""
      ],
      ""dynamicRegistration"": false
    },
    ""signatureHelp"": {
      ""signatureInformation"": {
        ""documentationFormat"": [
          ""plaintext""
        ]
      },
      ""contextSupport"": true,
      ""dynamicRegistration"": false
    },
    ""codeAction"": {
      ""codeActionLiteralSupport"": {
        ""codeActionKind"": {
          ""valueSet"": [
            ""refactor.extract""
          ]
        }
      },
      ""dynamicRegistration"": false
    }
  }
}";

        // Act
        var capabilities = JsonSerializer.Deserialize<VSInternalClientCapabilities>(rawJson);

        // Assert
        Assert.True(capabilities.Workspace.ApplyEdit);
        Assert.Equal(MarkupKind.PlainText, capabilities.TextDocument.Hover.ContentFormat.First());
        Assert.Equal(CompletionItemKind.Function, capabilities.TextDocument.Completion.CompletionItemKind.ValueSet.First());
        Assert.Equal(MarkupKind.PlainText, capabilities.TextDocument.SignatureHelp.SignatureInformation.DocumentationFormat.First());
        Assert.Equal(CodeActionKind.RefactorExtract, capabilities.TextDocument.CodeAction.CodeActionLiteralSupport.CodeActionKind.ValueSet.First());
    }
}
