# Roslyn Feature Feeds

In order to allow streamlined dogfooding of upcoming Roslyn features, 
we provide two mechanisms that are used in conjuntion and are automatically 
available for all feature branches:

1. A CI nuget feed at https://roslyn.blob.core.windows.net/nuget/index.json
2. A [VSIX gallery feed](https://docs.microsoft.com/en-us/visualstudio/extensibility/private-galleries) 
   at https://roslyn.blob.core.windows.net/vsix/[feature]/atom.xml

To get both a feature branch's IDE as well as compiler support, you need 
to install a matching IDE extension as well as configure and install the 
specific version of the compilers compiled for that same feature, as explained 
[in the docs on contributing](https://github.com/dotnet/roslyn/blob/master/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md#deploying-with-vsix-and-nuget-package). 

The feeds allow you to consume the CI produced artifacts from a feature 
branch without having to clone and build locally.

In addition, a permalink is also provided to the latest VSIX from the 
[currently branch](https://roslyn.azurewebsites.net/latest) or alternatively 
from any branch by using https://roslyn.azurewebsites.net/latest?FEATURE (such 
as https://roslyn.azurewebsites.net/latest?records).


## NuGet Feed

To consume the compiler (and any other packages you might depend on), add 
the following package source to your NuGet.Config:

```xml
<add key="roslyn" value="https://roslyn.blob.core.windows.net/nuget/index.json" />
```

To always reference the latest compiler toolset for a feature branch named 
`features/records`, for example, you can use a wildcard like:

```xml
<PackageReference Include="Microsoft.Net.Compilers.Toolset" Version="3.7.0-ci.records.*" />
```

The label suffix after `-ci.` will match the branch name after `features/` in 
the repository.

The wildcard can be replaced with a particular build version if you want an 
exact match (i.e. to be tied to the installed VSIX too).

## VSIX Feed

Visual Studio makes it easy to [add custom gallery feeds](https://docs.microsoft.com/en-us/visualstudio/extensibility/private-galleries) 
for installing and updating extensions outside of the marketplace.  

Unlike the nuget packages where you can easily pick which feature you want 
to install by appending the right label suffix after `-ci.`, the Extension 
Manager does not allow selection of different versions of the same extension 
from a feed. So you need different feeds for each.

You pick the feature feed you want by replacing `FEATURE` with the name of 
the feature branch (after `features/` in the branch name) in the URL  
`https://roslyn.blob.core.windows.net/vsix/FEATURE/atom.xml`


## Setting Up your own CI/Azure

This section documents how to set up the DevOps and Azure resources in order 
to self-host the feeds and nuget packages.

### Prepare Azure Resources

1. Optionally (but recommended) create a separate Resource Group to contain 
   all resources that will handle the feeds (i.e. Azure Function and Blob 
   Storage)
2. Create an empty Azure Functions App, choose .NET Core runtime
3. Create a new storage account (either `StorageV2` or `BlobStorage`) and 
   create three new containers with public read access for blobs access policy:
   `vsix`, `nuget` and `latest`.

#### Azure KeyVault

Both the function app and the build pipelines need access to the storage 
account, via a `FeedStorageConnectionString` setting. You can copy/paste 
the connection string directly as a setting for both, or you can use 
Azure KeyVault. This is the recommended approach for maximum security and 
policy compliance, and it also facilitates changing that value later on 
in a single place.

If you decide to use KeyVault:
1. Create a new Azure KeyVault in the same resource group as the rest
2. Create a new Secret and set it to the connection string for the 
   storage account, name it `FeedStorageConnectionString`. Copy the 
   `Secret Identifier` URI.
3. In the function app, select the `Identity` pane and set Status = On for 
   the `System assigned` identity. This will allow adding read permissions 
   to the function app on the key vault.
4. In the `Access policies` pane of the key vault, click `Add Access Policy` 
   and create a new policy with `Get` (key *and* secret) permissions for the 
   function app principal (search for its app name in the Principal selector)
5. In the function app, add a new app setting with the following format: 
   `@Microsoft.KeyVault(SecretUri=SECRET_URI)`. Replace `SECRET_URI` with the 
   value copied in step 2. 
 
If the value was properly read by the function app, a green checkmark will 
appear next to `Key vault Reference` in the Source column for the setting.

#### Azure DevOps

1. Create a new `Service connection` (in your DevOps project settings), 
   selecting `Azure Resource Manager` and `Service principal (automatic)` 
   and select the subscription and resource group. Name it `roslyn-Azure`.
   
   > The pipelines use `roslyn-Azure` as the service connection name. You 
   > can use a different name, by providing the value via the `AzureSubscription` 
   > variable.

2. After saving the connection, click on it to see the Overview/Details pane.
   Click on `Manage Service Principal`, which takes you to Azure AD app 
   registration. Copy the `Display name`

3. In the Azure portal, navigate to the storage account, and under the 
   `Access control IAM` section, add a new role assignement. Select 
   `Storage Blob Data Contributor` role, and paste the app registration display 
   name copied above to search for the identity and save.


### Azure DevOps Pipelines

There are two separate pipelines to set up:

1. [Uploading to Blob Storage](azure-pipelines-feeds.yml) on CI builds
2. [Deploying Function App](azure-pipelines-function.yml)


#### Storage Upload Pipeline

If you're using Azure KeyVault to keep the storage connection string:
* Open the `Library` section and create a new variable group
* Select `Link secrets from an Azure key vault as variables` and link the 
  `FeedStorageConnectionString`

Steps for setting up the CI pipeline that publishes blobs to storage (both 
VSIX payloads as well as a [serverless nuget feed](https://www.cazzulino.com/serverless-nuget-feed.html)):

1. Create a new YAML-based pipeline, point it to the roslyn repo/fork and 
   let it pick the default yaml from the repo and save it (don't queue a build)
2. Click Edit, then the vertical dots button next to `Save` and select `Triggers`.
   This allows non-yaml aspects of the pipeline.
3. In the `YAML` tab, enter `src/Tools/FeatureFeed/azure-pipelines-function.yml` 
   as the YAML file path and `Azure Pipelines` as the default agent pool. 
   Optionally give the pipeline a friendly name.
4. If you're not using Azure keyvault, create a `FeedStorageConnectionString` 
   variable and set its value to the storage account connection string. Also set 
   the variable as a secret. 
   If you're using Azure key vault, just link the previously created variable 
   group instead.
5. Optionally set the value of the `AzureSubscription` variable if your service 
   connection name is not `roslyn-Azure`.
   
#### Function Deploy Pipeline

Steps for setting up the [CI pipeline](azure-pipelines-function.yml) that 
publishes and updates the Azure function app that updates the VSIX feed:

1. Create a new YAML-based pipeline, point it to the roslyn repo/fork and 
   let it pick the default yaml from the repo and save it (don't queue a build)
2. Click Edit, then the vertical dots button next to `Save` and select `Triggers`.
   This allows non-yaml aspects of the pipeline.
3. In the `YAML` tab, enter `src/Tools/FeatureFeed/azure-pipelines-function.yml` 
   as the YAML file path and `Azure Pipelines` as the default agent pool. 
   Optionally give the pipeline a friendly name.
4. In the `Variables` tab, add a new `FunctionAppName` and set the value to the 
   name of the function app you created earlier.
5. Optionally set the value of the `AzureSubscription` variable if your service 
   connection name is not `roslyn-Azure`.
