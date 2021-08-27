---
uid: azure-sdk-2019-rewrite
---

# 'Old' vs 'new' (2019) Azure SDK

The Azure SDK started introducing some substantial changes in 2019, and as we have adapted to these, a couple of aspects of `Corvus.Identity` have become slightly confusing:

* we define two abstractions for obtaining a service identity, one of which is now deprecated
* there are quite a lot of packages with some superficially similar sounding names

Typically, you can just ignore the deprecated features, and follow the guidance in the documentation. However, if you are using the old `Corvus.Identity` v1.0 and are considering upgrading, or you just want to understand exactly why it's like it is, you will need to know the history of the changes in the Azure SDK . This document describes this historical background, and the impact it had on `Corvus.Identity`.

## Azure SDK 'old' and 'new'

In [November 2019, the Azure SDK team announced the first GA release of their 'new' SDK](https://devblogs.microsoft.com/azure-sdk/azure-sdk-release-nov-2019/). As the [November 2019 release notes](https://azure.github.io/azure-sdk/releases/2019-11/dotnet.html) said, this was a "ground-up rewrite of" the various client libraries for Azure. This new SDK version introduced substantial changes, and is not backwards-compatible with older versions. It includes a change to the way that Azure AD (AAD) authentication is handled.

Because the change was so dramatic, Microsoft made it fairly easy to tell which style of library you're using. Before these changes, Azure client libraries typically have names beginning with `Microsoft.Azure`, but with the new style, packages and namespaces typically begin with `Azure`.

Microsoft elected not to try and release updates for every single Azure feature simultaneously, so the initial release covered only a small subset of Azure functionality. There have been monthly releases ever since, gradually increasing the range of Azure services for which this "new" style of library still exists. Even so, at the time of writing this (August 2021) many of the components that make up the Azure SDK still only offer the 'old' style, with the 'new' style either still being in preview, or not yet available at all. Despite this, there are some components (notably the Azure Storage and Azure Key Vault client libraries) for which the old versions have been deprecated. Consequently, some applications must use a mixture of old and new Azure client libraries, which can in turn mean working with both the old and new authentication mechanisms within a single process.

(Note that not all Azure SDK client libraries use AAD. For example, authenticating to Azure Storage originally required either possession of the primary or secondary access key for the storage account, or a SAS token generated from one of those keys. So in cases where you need a new-style client for Azure Storage, but will be using access key based authentication, it's possible that you might not need to use the new AAD authentication mechanisms. However, authentication is increasingly now done through AAD access tokens—it is preferred over access keys for many Azure Storage scenarios—and ARM-based management plane operations have always required AAD, as has Azure Key Vault.)

## Authentication changes in Azure client libraries

To understand how `Corvus.Identity` adapts to the Azure SDK changes, it's important to know how the Azure client libraries in this SDK handled authentication before and after the changes.

### Old-style (`Microsoft.*` packages)

In the old-style world, Azure SDK libraries in which the client authenticates via Azure AD typically revolve around the [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime/) NuGet package. (This is the library that Autorest-generated clients rely on, and most of the old-style Azure SDK libraries depend on it.)

These [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime/)-based clients typically require something that derives from [`ServiceClientCredentials`](xref:Microsoft.Rest.ServiceClientCredentials). There are some specialized service-specific implementations, but when those are not required, we typically use [`TokenCredentials`](xref:Microsoft.Rest.TokenCredentials). This derives from [`ServiceClientCredentials`](xref:Microsoft.Rest.ServiceClientCredentials), and defers to an implementation of the [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) interface.

Key Vault used to be a special case. At one point, it required a special form of callback. It continues to support that, but it is also now possible to use it with [`TokenCredentials`](xref:Microsoft.Rest.TokenCredentials), but because of the history, `Corvus.Identity` still has some special handling for key vault reflecting the old differences.

The [`Microsoft.Rest`](xref:Microsoft.Rest) libraries provide implementations for [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) that use the Azure AD Authentication Library (generally known as ADAL). `UserTokenProvider` is designed for scenarios where a user logs into an application interactively, and the service wants to obtain tokens to act as that user. `ApplicationTokenProvider` is used in scenarios where a service wants to act with its own identity; this requires an Application and corresponding Service Principle (or "Enterprise Application" as the Azure Portal slightly confusingly calls these things) to be set up in Azure AD, and for the service to be in possession of credentials associated with that Application registration, enabling it to authenticate as itself, rather than as a human user.

For services running in Azure, another option exists: instead of having to register your own Application in Azure AD and manage the credentials, Azure can create this for you. Applications running in a context where Azure offers the "Managed Identity" feature (e.g., App Service, Virtual Machines) can just ask Azure to obtain tokens using this Managed Identity, and because Azure manages the credentials, the application itself doesn't need to have direct access to passwords or certificates. (Managed Identity is implemented as an HTTP endpoint that Azure makes available to the service. You just make requests to a well-known IP address, and as long as you're running in Azure in some context that has a Managed Identity enabled, it will obtain tokens on your behalf.)

For some time, there was no built-in implementation of [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) for Managed Identity. Microsoft supplied a library called [`Microsoft.Azure.Services.AppAuthentication`](https://www.nuget.org/packages/Microsoft.Azure.Services.AppAuthentication/) which offered an `AzureServiceTokenProvider` class that was a wrapper around the Managed Identity endpoint, but that did not implement [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider). The original motivation behind the creation of `Corvus.Identity` was that we had written lots of applications that wrote their own bridge between `AzureServiceTokenProvider` and [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider), and we wanted to have just one implementation of that bridge that we could reuse.

As it happens, Microsoft did eventually provide a Managed Identity implementation of [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider): [`MSITokenProvider`](xref:Microsoft.Azure.Management.ResourceManager.Fluent.Authentication.MSITokenProvider). (MSI—Managed Service Identity—is an older name for what Azure now callse just "Managed Identity.") However, there are a couple of problems with this. First, it derives from an [`IBeta`](xref:Microsoft.Azure.Management.ResourceManager.Fluent.Core.IBeta) interface, which is a marker that indicates that a type is experimental and might be removed. Second, it is part of the "Fluent" Azure client libraries, which impose a particular idiom that not everybody wants to use. And third, it is less flexible than `AzureServiceTokenProvider`. A big advantage of `AzureServiceTokenProvider` is that it offers a configuration-driven model where you can select between different authentication modes based on an application setting: you can configure deployed services to use a Managed Identity, and with only a configuration change (no code change), you can set up a local development environment to obtain a token via the `az` CLI tool, or through the user account with which the user logs into Visual Studio, or you can provide a Client ID and Secret for an Azure AD Application you have registered manually. This makes `AzureServiceTokenProvider` much easier to work with in practice, and is the reason we continued to use that even after a Managed Identity implementation of [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) became available.

### New-style (`Azure.*` packages)

By the time the Azure SDK revamp started in 2019, various things had changed:

* The ADAL library was deprecated, with the newer Microsoft Authentication Library for .NET (MSAL.NET) being preferred
* Use of scopes had become widespread in applications secured by Azure AD
* Limitations of the Autorest tooling led ot the Azure SDK team moving away from [`Microsoft.Rest`](xref:Microsoft.Rest)

Microsoft's Azure SDK team introduced what they call *Azure Core*. This is not just one thing: there are implementations of Azure Core for all the target platforms supported by the Azure SDK. So there is a .NET-specific implementation, the [Azure.Core](https://www.nuget.org/packages/Azure.Core/) NuGet package. For .NET code, this effectively replaces [`Microsoft.Rest`](xref:Microsoft.Rest).

The [`Azure.Core`](xref:Azure.Core) package defines the HTTP pipeline used in all Azure SDK libraries, and this includes the basic abstraction for credential handling, an abstract base class called [`TokenCredential`](xref:Azure.Core.TokenCredential). This defines [`GetToken`](xref:Azure.Core.TokenCredential.GetToken(Azure.Core.TokenRequestContext,System.Threading.CancellationToken)) and [`GetTokenAsync`](xref:Azure.Core.TokenCredential.GetTokenAsync(Azure.Core.TokenRequestContext,System.Threading.CancellationToken)) methods.

[`Azure.Core`](xref:Azure.Core) does not provide any concrete types derived from [`TokenCredential`](xref:Azure.Core.TokenCredential). There is a separate library, [`Azure.Identity`](https://www.nuget.org/packages/Azure.Identity/), which defines a wide range of types for different authentication mechanisms. For example,[`AzureCliCredential`](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.azureclicredential) enables developer to use the credentials acquired with the Azure CLI tool.

Although the [`Azure.Identity`](https://www.nuget.org/packages/Azure.Identity/) library provides types that individually cover all of the same scenarios as the `AzureServiceTokenProvider`, each scenarios gets its own class. There isn't a single class that can be directed to use different behaviours by changing a configuration setting.


## Authentication style mixes supported by `Corvus.Identity`

`Corvus.Identity` supports the following scenarios:

* All old-style and/or [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime/)-based clients
* All new-style and/or [`Azure.Identity`](xref:Azure.Identity)-based authentication
* Use of both authentication styles, necessitated by the mix of client libraries in use

One of the main problems `Corvus.Identity` was originally designed to solve was the fact that there was no built-in implementation of [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) that worked with Managed Identities. This led to an unfortunate situation in which a single component ([`Corvus.Identity.ManagedServiceIdentity.ClientAuthentication`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/)) mingled together what are really separate concerns:

* An abstraction for obtaining access tokens for a service protected by Azure AD, using the service's own identity ([`IServiceIdentityTokenSource`](xref:Corvus.Identity.ManagedServiceIdentity.ClientAuthentication.IServiceIdentityTokenSource))
* A concrete implementation of the above using an Azure Managed Identity (`AzureManagedIdentityTokenSource`)
* A bridge between the abstraction above and the [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) abstraction defined by [`Microsoft.Rest`](xref:Microsoft.Rest)

The problem with rolling all three of these into a single component is that taking a dependency on [`Corvus.Identity.ManagedServiceIdentity.ClientAuthentication`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/) brought in implicit dependencies on:

* [`Microsoft.Azure.Services.AppAuthentication`](https://www.nuget.org/packages/Microsoft.Azure.Services.AppAuthentication/) (because `AzureManagedIdentityTokenSource` is implemented on top of that library's `AzureServiceTokenProvider` class)
* [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime/) (home of [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider))

In the old-style SDK days this wasn't unreasonable, because the main point of this library was to avoid the problem in which every individual application needed to write its own bridge between `AzureServiceTokenProvider` and [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider). If that's what you need, you'll have a dependency on these two NuGet packages in any case.

However, if an application uses only new-style components, it won't want dependencies on either of these packages. (Instead, it's like to depend on [`Azure.Identity`](https://www.nuget.org/packages/Azure.Identity/) which in turn brings a dependency on [`Azure.Core`](https://www.nuget.org/packages/Azure.Core/).) However, an application may still want the first of the concerns listed above: the abilty to obtain access tokens to act with its own identity when using a service protected by AAD.

The `IServiceIdentityTokenSource` interface was designed to solve that problem, but it only does this well for the "all old-style" scenario. If you're writing a new-style-only application, you won't want the references to [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime/) or [`Microsoft.Azure.Services.AppAuthentication`](https://www.nuget.org/packages/Microsoft.Azure.Services.AppAuthentication/) that [`IServiceIdentityTokenSource`](xref:Corvus.Identity.ManagedServiceIdentity.ClientAuthentication.IServiceIdentityTokenSource) implies. Moreover, it doesn't support modern authentication scenarios very well anyway, because it has no awareness of scopes, and it also has some anachronistic features that originate from the time when the Key Vault library's authentication mechanism was a special case.

## `Corvus.Identity` features

With that historical context in mind, we can now look at the features `Corvus.Identity` was designed to offer, and why the changes in the Azure SDK necessitated changes to `Corvus.Identity`.

`Corvus.Identity` provides two main services, both of which enable a service to authenticate as itself (i.e., using an identity representing the service, and not some end user):

1. A source of access tokens
1. A source of credentials in the form required by specific client libraries (e.g., such as libraries from the Azure SDK)

From a high-level architectural perspective these are the same thing. But from a practical point of view, they're not, and it was those practical differences that led to the creation of `Corvus.Identity`. The additional steps required to present access tokens in the form required by client libraries resulted in very similar boilerplate appearing in multiple projects, which `Corvus.Identity` aimed to eliminate.

At the most basic level, anything using OAuth2-style authentication will need to include an `Authorization` header in all outbound HTTP requests, passing an access token using the `Bearer` scheme. So the fundamental requirement is the ability to obtain such a token (regardless of whether we're talking to an Azure service or something else, and irrespective of the version or type of any particular client libraries). Then in practice, we need to wrap those tokens in whatever form is required by the client libraries we are using.

### Old-style implementation of `Corvus.Identity` features

An access token is, ultimately, just some text, and in some cases, a method that provides a `string` is all that's required. This was the concept behind the original [`IServiceIdentityTokenSource`](xref:Corvus.Identity.ManagedServiceIdentity.ClientAuthentication.IServiceIdentityTokenSource) interface defined by the [`Corvus.Identity.ManagedServiceIdentity.ClientAuthentication`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/) package.

Once you have an [`IServiceIdentityTokenSource`](xref:Corvus.Identity.ManagedServiceIdentity.ClientAuthentication.IServiceIdentityTokenSource), you can ask it for an access token for the resource you wish to access, and use that to create an [`HttpRequestMessage`](xref:System.Net.Http.HttpRequestMessage) with the token in the `Authorization` header. You can then use an [`HttpClient`](xref:System.Net.Http.HttpClient) to make a request to whatever service you want to use. This is the most basic use of `Corvus.Identity`. (In theory, it entails the fewest dependencies on anything else, but in practice, the original [`IServiceIdentityTokenSource`](xref:Corvus.Identity.ManagedServiceIdentity.ClientAuthentication.IServiceIdentityTokenSource) was in a component that brought in references to some NuGet packages that you might not want or need.)

The second feature was one of convenience: it built on the fundamental ability to obtain access tokens, wrapping them in the forms required by client libraries such as [`Microsoft.Rest`](xref:Microsoft.Rest), or and the old Azure Key Vault client library.


### Modern implementation of `Corvus.Identity` features

The basic ability to obtain an access token representing the service's own identity is now represented by [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource). This is defined in the [`Corvus.Identity.Abstractions`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/) library, which does not depend on any particular client library. (In fact, for `.netcoreapp3.1` and later, it has no dependencies at all; for `.netstandard2.0` it depends only on [`System.Threading.Tasks.Extensions`](https:///www.nuget.org/packages/System.Threading.Tasks.Extensions/), to be able to use [`ValueTask<T>`](xref:System.Threading.Tasks.ValueTask%601).)

Of course, the upshot is that [`Corvus.Identity.Abstractions`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/) does not supply any implementations of [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource). An application would not depend on this library alone. However, other libraries can depend on just [`Corvus.Identity.Abstractions`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/), enabling them to indicate that they require this facility—the ability to obtain tokens in the service's own identity—without forcing the use of any particular technology to achieve this.

Applications that need to authenticate using [`Azure.Identity`](xref:Azure.Identity) (e.g., when using new-style Azure SDK client libraries) can use the [`Corvus.Identity.Azure`](https:///www.nuget.org/packages/Corvus.Identity.Azure/) library. This depends in turn on [`Corvus.Identity.Abstractions`](https:///www.nuget.org/packages/Corvus.Identity.Abstractions/), and provides both of the main `Corvus.Identity` features, in the following form:

* An implementation of the [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource) (a source of raw access tokens) that enables a service to authenticate with its own identity by deferring to any implementation of [`TokenCredential`](xref:Azure.Core.TokenCredential) in [`Azure.Identity`](xref:Azure.Identity)
* An [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource) implementation that can supply an actual [`TokenCredential`](xref:Azure.Core.TokenCredential) representing the service's own identity

Applications that need to use old-style SDK client libraries (or that need an [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) because they are using some other [`Microsoft.Rest`](xref:Microsoft.Rest)-based client library) are advised to move away from [`Corvus.Identity.ManagedServiceIdentity.ClientAuthentication`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/) and onto [`Corvus.Identity.MicrosoftRest`](https:///www.nuget.org/packages/Corvus.Identity.MicrosoftRest/).