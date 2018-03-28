// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "System.Web"

using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Web.Configuration;
using static System.Environment;

private const string KeyVaultUrl = "https://roslyninfra.vault.azure.net:443";

public static async Task<string> GetSecret(string secretName)
{
    var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));
    var secret = await kv.GetSecretAsync(KeyVaultUrl, secretName);
    return secret.Value;
}

private static async Task<string> GetAccessToken(string authority, string resource, string scope)
{
    var ctx = new AuthenticationContext(authority);
    var clientId = WebConfigurationManager.AppSettings["ClientId"];
    var clientSecret = WebConfigurationManager.AppSettings["ClientSecret"];
    var clientCred = new ClientCredential(clientId, clientSecret);

    var authResult = await ctx.AcquireTokenAsync(resource, clientCred);
    return authResult.AccessToken;
}
