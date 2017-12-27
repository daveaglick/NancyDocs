Order: 1
---
[Download PDF Document](https://github.com/erdomke/Innovator.Client/files/1080932/NancyPipeline.pdf)

Here you can download a PDF document that charts the lifecycle of every Nancy application, from receiving the HTTP request to sending the HTTP response back to the client.  Relative to the [ASP.NET MVC Lifecycle](https://docs.microsoft.com/en-us/aspnet/mvc/overview/getting-started/lifecycle-of-an-aspnet-mvc-5-application), the Nancy appliction lifecycle is both fairly simple and highly extensible.  These extension points are documented in other wiki articles such as

1. [Managing static content with StaticContentsConventions](Managing-static-content)
2. [The application Before, After and OnError pipelines](The-Application-Before%2C-After-and-OnError-pipelines)
3. [Defining routes](Defining-routes)  (A sample `INancyModuleBuilder` is demonstrated in [Nancy and New Relic](Nancy-and-New-Relic#alternative-approach))
4. [The before and after module hooks](The-before-and-after-module-hooks)
5. [Content negotiation with covers IResponseProcessors](Content-Negotiation)
6. [Generating a custom error page with IStatusCodeHandler(s)](Generating-a-custom-error-page)



![nancypipeline](https://user-images.githubusercontent.com/4406364/27229089-2c7eda1c-5278-11e7-89b3-b443bc7d9a8e.png)
