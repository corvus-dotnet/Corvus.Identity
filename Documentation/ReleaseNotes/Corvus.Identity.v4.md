# Release notes for Corvus.Identity v4.0

## v4.1.0

### New Features

#### Client Certificate Authentication Support
* **NEW**: Added [`Corvus.Identity.Certificates`](../Solutions/Corvus.Identity.Certificates/Corvus.Identity.Certificates.csproj:1) NuGet package for certificate-based authentication
* **NEW**: [`ClientCertificateConfiguration`](../Solutions/Corvus.Identity.Certificates/Corvus/Identity/Certificates/ClientCertificateConfiguration.cs:1) class for configuring certificate authentication
* **NEW**: [`ICertificateFromConfiguration`](../Solutions/Corvus.Identity.Certificates/Corvus/Identity/Certificates/ICertificateFromConfiguration.cs:1) interface for certificate resolution
* **NEW**: [`CertificateNotFoundException`](../Solutions/Corvus.Identity.Certificates/Corvus/Identity/Certificates/CertificateNotFoundException.cs:1) for certificate error handling
* **NEW**: [`IdentityCertificateServiceCollectionExtensions`](../Solutions/Corvus.Identity.Certificates/Microsoft/Extensions/DependencyInjection/IdentityCertificateServiceCollectionExtensions.cs:1) for DI registration

#### Enhanced Azure Identity Support
* **NEW**: [`ClientIdentityConfiguration.AzureAdAppClientCertificate`](../Solutions/Corvus.Identity.Azure/Corvus/Identity/ClientAuthentication/Azure/ClientIdentityConfiguration.cs:1) property for certificate-based Azure AD authentication
* **NEW**: [`TestableClientCertificateCredential`](../Solutions/Corvus.Identity.Azure/Corvus/Identity/ClientAuthentication/Azure/Internal/TestableClientCertificateCredential.cs:1) for improved testing support
* **ENHANCED**: Enhanced [`ClientIdentityConfigurationValidation`](../Solutions/Corvus.Identity.Azure/Corvus/Identity/ClientAuthentication/Azure/ClientIdentityConfigurationValidation.cs:1) with certificate validation support

### Framework and Infrastructure Updates

#### .NET 8.0 Migration
* **BREAKING**: All projects now target .NET 8.0 exclusively
* **BREAKING**: Removed .NET 6.0 support
* **UPDATED**: All project files updated to use .NET 8.0 target framework

#### Build and CI/CD Improvements
* **NEW**: Added [`.github/workflows/build.yml`](../.github/workflows/build.yml:1) GitHub Actions workflow
* **UPDATED**: Enhanced [`build.ps1`](../build.ps1:1) script with latest tooling (v1.5.4)
* **REMOVED**: Deprecated [`azure-pipelines.yml`](../azure-pipelines.yml:1) and [`azure-pipelines.release.yml`](../azure-pipelines.release.yml:1)
* **NEW**: Added [`.zf/config.ps1`](../.zf/config.ps1:1) configuration file
* **UPDATED**: Enhanced [`.github/workflows/auto_release.yml`](../.github/workflows/auto_release.yml:1) and [`.github/workflows/dependabot_approve_and_label.yml`](../.github/workflows/dependabot_approve_and_label.yml:1)

#### Testing Infrastructure Modernization
* **BREAKING**: Migrated from SpecFlow to Reqnroll
  * **NEW**: [`reqnroll.json`](../Solutions/Corvus.Identity.Specs/reqnroll.json:1) configuration
  * **REMOVED**: [`specflow.json`](../Solutions/Corvus.Identity.Specs/specflow.json:1) configuration
* **NEW**: [`AsyncTestTaskExtensions`](../Solutions/Corvus.Identity.Specs/Idg/AsyncTest/TaskExtensions/AsyncTestTaskExtensions.cs:1) for improved async testing support
* **NEW**: Comprehensive certificate authentication test scenarios in [`AdAppWithClientCertificate.feature`](../Solutions/Corvus.Identity.Specs/Corvus/Identity/Azure/TokenCredentialSourceFromDynamicConfiguration/AdAppWithClientCertificate.feature:1)

### Breaking Changes

#### Microsoft.Rest Support Removal
* **BREAKING**: Removed entire [`Corvus.Identity.MicrosoftRest`](../Solutions/Corvus.Identity.MicrosoftRest/Corvus.Identity.MicrosoftRest.csproj:1) package and all related components:
  * [`IMicrosoftRestTokenProviderSource`](../Solutions/Corvus.Identity.MicrosoftRest/Corvus/Identity/ClientAuthentication/MicrosoftRest/IMicrosoftRestTokenProviderSource.cs:1)
  * [`IMicrosoftRestTokenProviderSourceFromDynamicConfiguration`](../Solutions/Corvus.Identity.MicrosoftRest/Corvus/Identity/ClientAuthentication/MicrosoftRest/IMicrosoftRestTokenProviderSourceFromDynamicConfiguration.cs:1)
  * [`IServiceIdentityMicrosoftRestTokenProviderSource`](../Solutions/Corvus.Identity.MicrosoftRest/Corvus/Identity/ClientAuthentication/MicrosoftRest/IServiceIdentityMicrosoftRestTokenProviderSource.cs:1)
  * [`MicrosoftRestTokenProvider`](../Solutions/Corvus.Identity.MicrosoftRest/Corvus/Identity/ClientAuthentication/MicrosoftRest/MicrosoftRestTokenProvider.cs:1)
  * [`MicrosoftRestIdentityServiceCollectionExtensions`](../Solutions/Corvus.Identity.MicrosoftRest/Microsoft/Extensions/DependencyInjection/MicrosoftRestIdentityServiceCollectionExtensions.cs:1)
* **BREAKING**: Removed all Microsoft.Rest related examples:
  * [`Corvus.Identity.Examples.UsingMicrosoftRest`](../Solutions/Corvus.Identity.Examples.UsingMicrosoftRest/Corvus.Identity.Examples.UsingMicrosoftRest.csproj:1) project
  * [`UseMicrosoftRestFunction`](../Solutions/Corvus.Identity.Examples.AzureFunctions/UseMicrosoftRestFunction.cs:1) from Azure Functions examples

### Dependency Updates

#### Core Dependencies
* **UPDATED**: Azure.Identity to v1.10 (from v1.8)
* **UPDATED**: Azure.Core to v1.37.0 (from v1.36.0)  
* **UPDATED**: Azure.Security.KeyVault.Secrets to v4.6.0 (from v4.5.0)
* **UPDATED**: Microsoft.Extensions.* packages to v8.* versions
* **UPDATED**: Various other dependencies for .NET 8.0 compatibility

#### Package Management
* **UPDATED**: All [`packages.lock.json`](../Solutions/Corvus.Identity.Specs/packages.lock.json:1) files reflect new dependency versions
* **ENHANCED**: Improved package version management and formatting

### Code Quality and Maintenance

#### Project Structure
* **ENHANCED**: Updated all [`.csproj`](../Solutions/Corvus.Identity.Abstractions/Corvus.Identity.Abstractions.csproj:1) files with modern SDK-style project format
* **ENHANCED**: Improved code organization and namespace structure
* **UPDATED**: [`GitVersion.yml`](../GitVersion.yml:1) configuration for v4.0 release cycle

#### Configuration and Settings
* **UPDATED**: [`local.settings.template.json`](../Solutions/Corvus.Identity.Examples.AzureFunctions/local.settings.template.json:1) for Azure Functions examples
* **ENHANCED**: [`.gitignore`](../.gitignore:1) with additional exclusions
* **UPDATED**: Various configuration files for improved development experience

### Migration Guide

For users upgrading from v3.x to v4.0:

1. **Framework Migration**: Update your project to target .NET 8.0
2. **Microsoft.Rest Removal**: If using Microsoft.Rest integration, migrate to Azure.Core-based authentication
3. **Certificate Authentication**: Consider adopting the new certificate authentication features for enhanced security
4. **Package References**: Update all Corvus.Identity package references to v4.0+
5. **Testing**: If using SpecFlow-based tests, consider migrating to Reqnroll

### Known Issues

None at this time.

---

## v4.0.1

Initial patch release with dependency updates and configuration improvements for Azure Functions support.

## v4.0.0

Major version release with .NET 8.0 migration and removal of Microsoft.Rest support. See detailed changelog above.