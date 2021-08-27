# Getting started with `Corvus.Identity`

The Azure SDK has been in a state of flux since a major redesign started in 2019. (See the [Azure SDK changes](old-vs-new-azure-sdk.md) article for details.) Some Azure client libraries have not yet been updated to this new style. The right way to get started with `Corvus.Identity` will depend on which kinds of client libraries you need.

If you are writing new code that needs to work with new-style Azure SDK components, or with other client libraries based on Microsoft's [`Azure.Core`](xref:Azure.Core) component, read the [Getting started with new-style client libraries](getting-started-new-style.md) article.

If you are writing code that uses old-style Azure SDK components, or anything else based on the [`Microsoft.Rest`](xref:Microsoft.Rest) components, read the [Getting started with old-style client libraries](getting-started-old-style.md) article.

If you need to use a mixture of old-style and new-style Azure SDK components (e.g., because some of the Azure services you are using have already deprecated the old-style libraries, but you are also using some other Azure services for which new-style libraries are not yet available) read the [Getting started with a mixture of old- and new-style client libraries](getting-started-mixed-style.md) article.
