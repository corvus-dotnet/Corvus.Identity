# Release notes for Corvus.Identity v3.

## v3.4

New feature:

* Support for client certificate auth:
    * Added the Corvus.Identity.Certificates NuGet package.
    * Added ClientIdentityConfiguration.AzureAdAppClientCertificate to Corvus.Identity.Azure.

Dependency minor version upgrades:

* Azure.Identity 1.8 -> 1.10
* Azure.Core.1.36.0 -> Azure.Core.1.37.0
* Azure.Security.KeyVault.Secrets.4.5.0 -> Azure.Security.KeyVault.Secrets.4.6.0

## v3.3

New feature:

* Support for user-defined managed identities

Dependency minor version upgrades:

* Azure.Security.KeyVault.Secrets 4.4 -> 4.5, with the following transient upgrades:
    * Azure.Core 1.25 -> 1.30
    * Microsoft.Identity.Client 4.46 -> 4.49
    * Microsoft.Identity.Client.Extensions.Msal 2.23 -> 2.25
    * Microsoft.Identity.Model.Abstractions 6.18 -> 6.22


## v3.2

Dependency minor version upgrades:

* Azure.Identity 1.6 -> 1.8
* Azure.Security.KeyVault.Secrets 4.3 -> 4.4


## v3.1

Dependency minor version upgrades:

* Azure.Identity 1.5 -> 1.6
* Azure.Security.KeyVault.Secrets 4.2 -> 4.3

## v3.0

New features:

Targets .NET 6.0 only.

Client identity configuration, with support for `Azure.Core`-style authentication. See [Configuration and Azure AD Client Identities](../articles/configuration.md) for details



### Potentially breaking changes

There have been additions to two interfaces. These are breaking changes for any client of this library that chooses to implement these interfaces. However, these are not really designed to be implemented by other libraries; these interfaces exist to enable the implementations to be consumed through DI.

The changes will be binary-compatible for code that only consumes these interfaces, because such code won't even know about the new members. The changes are also source-compatible except for projects that treat warnings as errors, in which case the deprecation of an existing member will cause a compiler error.

* `IAccessTokenSource` has a new `GetReplacementForFailedAccessTokenAsync` method to enable client code to report that some credentials are no longer any good
* `IAzureTokenCredentialSource` has a new `GetReplacementForFailedTokenCredentialAsync` method to enable client code to report that some credentials are no longer any good
* `IAzureTokenCredentialSource` deprecates the `GetAccessTokenAsync` method, and adds a `GetTokenCredentialAsync` method, which is what that method should have been called all along; these methods are logically identical, although `GetTokenCredentialAsync` adds support for a `CancellationToken`
