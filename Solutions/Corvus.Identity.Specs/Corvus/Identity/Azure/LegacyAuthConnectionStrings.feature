Feature: LegacyAuthConnectionStrings
	As a developer
	In order to migrate progressively from Azure SDK v11 to v12
	I want to be able to use the connection string format from the old Microsoft.Azure.Services.AppAuthentication library when using the newer Azure.Identity library

# With the old AzureServiceTokenProvider, if you provided an empty connection string, it would try
# a sequence of strategies: first it would try to use the Managed Identity mechanism, and if that
# didn't work, it would try local credentials including the token cache Visual Studio uses, and
# also the "az" CLI tool.
# Azure.Identity doesn't have a precisely equivalent mechanism. However, DefaultAzureCredential
# is pretty close. The main differences are:
#	* it tries the EnvironmentCredential first, enabling environment variables to control everything
#	* it tries InteractiveBrowserCredential as a last resort
# Since these are in addition to the things AzureServiceTokenProvider does in this scenario,
# DefaultAzureCredential is a reasonable substitute.
Scenario: Empty authentication connection string tries MSI then local credential stores
	When I create a TokenCredential with the connection string ''
	Then the TokenCredential should be of type 'DefaultAzureCredential'

Scenario: Authentication connection string explicitly requesting Managed Identity
	When I create a TokenCredential with the connection string 'RunAs=App'
	Then the TokenCredential should be of type 'ManagedIdentityCredential'

Scenario: Authentication connection string explicitly requesting Azure CLI
	When I create a TokenCredential with the connection string 'RunAs=Developer;DeveloperTool=AzureCli'
	Then the TokenCredential should be of type 'AzureCliCredential'

Scenario: Authentication connection string explicitly requesting Visual Studio
	When I create a TokenCredential with the connection string 'RunAs=Developer;DeveloperTool=VisualStudio'
	Then the TokenCredential should be of type 'VisualStudioCredential'

Scenario Outline: Authentication connection string with service princple client credentials
	When I create a TokenCredential with the connection string 'RunAs=App;AppId=<appId>;TenantId=<tenantId>;AppKey=<clientSecret>'
	Then the TokenCredential should be of type 'ClientSecretCredential'
	And the ClientSecretCredential tenantId should be '<tenantId>'
	And the ClientSecretCredential appId should be '<appId>'
	And the ClientSecretCredential clientSecret should be '<clientSecret>'
	Examples:
		| tenantId                             | appId                                | clientSecret |
		| 5eb7855b-0d07-4ce1-aa02-5531fa53379f | dbee7796-25e1-4059-8c6b-e31c2eb15147 | P@ssw0rd     |
		| b3d8cc87-11d5-4ca0-a3e9-e2a0bc363bb6 | 47618c9e-fc74-4355-af7c-1bf42ccd70f4 | 53cr3T       |
