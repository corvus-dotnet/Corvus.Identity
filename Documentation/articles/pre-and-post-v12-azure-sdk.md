# Azure SDK pre- and post-V12

There are some aspects of `Corvus.Identity` that can be slightly confusing: it has two abstractions for obtaining a service identity, one of which is now deprecated; there are quite a lot of packages with some superficially similar sounding names. Typically, you can just ignore the deprecated packages, and follow the guidance in the documentation. However, if you are using older versions of `Corvus.Identity` and are considering upgrading, or you just want to understand exactly why it's like it is, you will need to understand the history behind these libraries, and in particular, the changes in the Azure SDK that necessitated a signifant revamp of `Corvus.Identity`. This document describes this historical background, and the impact it had on `Corvus.Identity`.

Version 12 of the Azure SDK for .NET started to emerge in 2019. (Microsoft elected not to try and release updates for every single Azure feature simulatneously, so the initial release covered only a small subset of Azure functionality.) This new SDK version introduced substantial changes, and is not backwards-compatible with older versions. It includes a change to the way that Azure AD (AAD) authentication is handled.

At the time of writing this (August 2021) most of the components that make up the Azure SDK are still on pre-V12 releases. However, there are some components (notably the Azure Storage and Azure Key Vault client libraries) for which the pre-V12 versions have been deprecated. Consequently, some applications must use a mixture of pre- and post-V12 Azure client libraries, which can in turn mean working with both the old and new authentication mechanisms.

(Note that not all Azure SDK client libraries use AAD. For example, authenticating to Azure Storage originally required either possession of the primary or secondary access key for the storage account, or a SAS token generated from one of those keys. So in cases where you need a V12-or-later client for Azure Storage, but will be using access key based authentication, it's possible that you might not need to use the new AAD authentication mechanisms. But authentication is increasingly now done through AAD access tokens, and always has been for ARM-based management plane operations. Moreover, anything using Azure Key Vault requires AAD-based authentication.)


## Authentication changes in Azure client libraries

To understand how `Corvus.Identity` adapts to the changes Azure SDK V12 introduced, it's important to know how the Azure client libraries in this SDK handled authentication before and after the changes.

**Note**: it's fairly easy to tell which style of library you're using. Before V12, Azure client libraries typically have names beginning with `Microsoft.Azure`, but starting with the V12 libraries, packages and namespaces typically begin with `Azure`.)

### Before V12

In the pre-V12 world, Azure SDK libraries in which the client authenticates via Azure AD typically revolve around the [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime) NuGet package. (This is the library that Autorest-generated clients rely on, and most of the pre-V12 Azure SDK libraries depend on it.)

These `Microsoft.Rest.ClientRuntime`-based clients typically require something that derives from [`ServiceClientCredentials`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.rest.serviceclientcredentials). There are some specialized service-specific implementations, but when those are not required, we typically use [`TokenCredentials`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.rest.tokencredentials). This derives from `ServiceClientCredentials`, and defers to an implementation of the [`ITokenProvider`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.rest.itokenprovider) interface.

Key Vault was a special case before Azure SDK V12. It required you to provide a callback that it could use to retreive an access token. Conceptually this is identical to `ITokenProvider` but it passes in additional arguments. `ITokenProvider` only allows you to specify the "resource identifer" indicating which service you wish to use (e.g., to talk to ARM you need a token issued for the resource identifer `https://management.core.windows.net` ). But the key vault client specifies not just the resource identifier, but also an _authority_ and a _scope_. The authority effectively determines the AAD tenant in use, and it's a bit odd that the key vault client should be specifying this: that's really the business of the application, not the key vault client library. And the scope determines the particular permissions being requested: this reflects the fact that modern usage of AAD often goes beyond a simple "Does this user have access to this service?" type questions, and instead allows a more fine-grained approach, e.g. distinguishing between read and read/write access to specific kinds of resources within the service. With the Azure AD 2.0 endpoint, use of scopes came more to the foreground, so `ITokenProvider`'s approach of working only with the resource identifier is somewhat dated, and simply doesn't work for scenarios in which the scope must be specified.

The `Microsoft.Rest` libraries provide implementations for `ITokenProvider` that use the Azure AD Authentication Library (generally known as ADAL). `UserTokenProvider` is designed for scenarios where a user logs into an application interactively, and the service wants to obtain tokens to act as that user. `ApplicationTokenProvider` is used in scenarios where a service wants to act with its own identity; this requires an Application and corresponding Service Principle (or "Enterprise Application" as the Azure Portal slightly confusingly calls these things) to be set up in Azure AD, and for the service to be in possession of credentials associated with that Application registration, enabling it to authenticate as itself, rather than as a human user.

For services running in Azure, another option exists: instead of having to register your own Application in Azure AD and manage the credentials, Azure can create this for you. Applications running in a context where Azure offers the "Managed Identity" feature (e.g., App Service, Virtual Machines) can just ask Azure to obtain tokens using this Managed Identity, and because Azure manages the credentials, the application itself doesn't need to have direct access to passwords or certificates. (Managed Identity is implemented as an HTTP endpoint that Azure makes available to the service. You just make requests to a well-known IP address, and as long as you're running in Azure in some context that has a Managed Identity enabled, it will obtain tokens on your behalf.)

For some time, there was no built-in implementation of `ITokenProvider` for Managed Identity. Microsoft supplied a library called `Microsoft.Azure.Services.AppAuthentication` which offered an `AzureServiceTokenProvider` class that was a wrapper around the Managed Identity endpoint, but it did not implement `ITokenProvider`. The original motivation behind the creation of `Corvus.Identity` was that we had written lots of applications that wrote a bridge between `AzureServiceTokenProvider` and `ITokenProvider`, and we wanted to have just one implementation of that bridge that we could reuse.

As it happens, Microsoft did eventually provide a Managed Identity implementation of `ITokenProvider`: [`MSITokenProvider`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.resourcemanager.fluent.authentication.msitokenprovider). (MSI—Managed Service Identity—is an older name for what Azure now callse just "Managed Identity.") However, there are a couple of problems with this. First, it derives from an `IBeta` interface, which is a marker that indicates that a type is experimental and might be removed. Second, it is part of the "Fluent" Azure client libraries, which impose a particular idiom that not everybody wants to use. And third, it is less flexible than `AzureServiceTokenProvider`. A big advantage of `AzureServiceTokenProvider` is that it offers a configuration-driven model where you can select between different authentication modes based on an application setting: you can configure deployed services to use a Managed Identity, and with only a configuration change (no code change), you can set up a local development environment to obtain a token via the `az` CLI tool, or through the user account with which the user logs into Visual Studio, or you can provide a Client ID and Secret for an Azure AD Application you have registered manually. This makes `AzureServiceTokenProvider` much easier to work with in practice, and is the reason we continued to use that even after a Managed Identity implementation of `ITokenProvider` became available.

### After V12

By the time the revamp for Azure SDK V12 was underway, various things had changed:

* The ADAL library was deprecated, with the newer Microsoft Authentication Library for .NET (MSAL.NET) being preferred
* Use of scopes had become widespread in applications secured by Azure AD
* Limitations of the Autorest tooling led ot the Azure SDK team moving away from `Microsoft.Rest`

Microsoft introduced what they call *Azure Core*. This is not just one thing: there are implementations of Azure Core for all the target platforms supported by the Azure SDK. So there is a .NET-specific implementation, the [Azure.Core](https://www.nuget.org/packages/Azure.Core/) NuGet package. For .NET code, this effectively replaces `Microsoft.Rest`.

The `Azure.Core` package defines the HTTP pipeline used in all Azure SDK libraries, and this includes the basic abstraction for credential handling, an abstract base class called `TokenCredential`. This defines [`GetToken`](https://docs.microsoft.com/en-us/dotnet/api/azure.core.tokencredential.gettoken) and [`GetTokenAsync`](https://docs.microsoft.com/en-us/dotnet/api/azure.core.tokencredential.gettokenasync) methods.

The V12 SDK does not use


## Authentication style mixes supported by `Corvus.Identity`

`Corvus.Identity` supports the following scenarios:

* All pre-V12 and/or `Microsoft.Rest.ClientRuntime`-based clients
* All post-V12 and/or `Azure.Identity`-based authentication
* Use of both authentication styles, necessitated by the mix of client libraries in use


One of the main problems `Corvus.Identity` was originally designed to solve was the fact that there was no built-in implementation of `ITokenProvider` that worked with Managed Identities. This led to an unfortunate situation in which a single component (`Corvus.Identity.ManagedServiceIdentity.ClientAuthentication`) mingled together what are really separate concerns:

* An abstraction for the act of obtaining an access token for a service protected by Azure AD (`IServiceIdentityTokenSource`)
* A concrete implementation of the above using an Azure Managed Identity (`AzureManagedIdentityTokenSource`)
* A bridge between the abstraction above and the `ITokenProvider` abstraction defined by `Microsoft.Rest`

The problem with rolling all three of these into a single component is that taking a dependency on `Corvus.Identity.ManagedServiceIdentity.ClientAuthentication` brought in implicit dependencies on:

* `Microsoft.Azure.Services.AppAuthentication` (because `AzureManagedIdentityTokenSource` is implemented on top of that library's `AzureServiceTokenProvider` class)
* `Microsoft.Rest.ClientRuntime` (home of `ITokenProvider`)

In the pre-V12 days this wasn't unreasonable, because the main point of this library was to avoid the problem in which every individual application needed to write its own bridge between `AzureServiceTokenProvider` and `ITokenProvider`. If that's what you need, you'll have a dependency on these two NuGet packages in any case.

However, with a post-V12 

## `Corvus.Identity` features

With that historical context in mind, we can now look at the features `Corvus.Identity` was designed to offer, and why the changes in the Azure SDK necessitated changes to `Corvus.Identity`.

`Corvus.Identity` provides two main services, both of which enable a service to authenticate as itself (i.e., using an identity representing the service, and not some end user):

1. A source of access tokens
1. A source of credentials in the form required by specific client libraries (e.g., such as libraries from the Azure SDK)

From a high-level architectural perspective these are the same thing. But from a practical point of view, they're not, and it was those practical differences that led to the creation of `Corvus.Identity`. They resulted in very similar boilerplate in multiple projects, which `Corvus.Identity` aimed to eliminate.

At the most basic level, anything using OAuth2-style authentication will need include `Authorization` header in all outbound HTTP requests, passing an access token using the `Bearer` scheme. So the fundamental requirement (regradless of whether we're talking to an Azure service or something else, and irrespective of the version or type of any particular client libraries) is the ability to obtain such a token. An access token is, ultimately, just some text, and in some cases, a method that provides a `string` is all that's required: this was the concept behind the original `IServiceIdentityTokenSource` interface defined by the `Corvus.Identity.ManagedServiceIdentity.ClientAuthentication` package.

Once you have an `IServiceIdentityTokenSource`, you can ask if for an access token for the resource you wish to access, and use that to create an `HttpRequestMessage` with the token in the `Authorization` header. You can then use an `HttpClient` to make a request to whatever service you want to use. This is the most basic use of `Corvus.Identity`. (In theory, it entails the fewest dependencies on anything else, but in practice, the original `IServiceIdentityTokenSource` was in a component that brought in references to some NuGet packages that you might not want or need.)

The second feature was one of convenience: it built on the fundamental ability to obtain access tokens, wrapping them in the forms required by client libraries such as `Microsoft.Rest` (which underpinned most pre-V12 Azure SDK libraries) and the old Azure Key Vault client library.