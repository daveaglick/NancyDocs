# Overview

## Routing 

Routing syntax has changed to `Get("/", args => "Hello World");`, these can be made `async` by adding the `async/await` keywords. For more info see the PR where it all changed https://github.com/NancyFx/Nancy/pull/2441

## StaticConfiguration migrated to configuration API

Most of the members in `StaticConfiguration` have been moved out and migrated over to the new [Configuration API](Configuration-API-(Draft)).

The following settings have been migrated

- Json
- Xml
- View
- Tracing
- Routing

## Context.CurrentUser ClaimsPrincipal Mapping to Domain Object

In the Nancy 1.x versions, `Context.CurrentUser` was an `IUserIdentity`, you could add properties to your implementation and then access these properties that were not on the interface by casting. E.g. `var customerId = ((MyUser) Context.CurrentUser).CustomerId;`

In v2.x of Nancy, `Context.CurrentUser` is a `ClaimsPrincipal`. By default this contains an list of claims that the user has. So to get a name value for example you would have do something like `Context.CurrentUser.FindFirst(ClaimTypes.Name).Value`.

As an example you could implement properties like in v1 like so:

```csharp
public class MyPrincipal : ClaimsPrincipal
{
   public MyPrincipal(IPrincipal principal) : base(principal)
   {
   }

   public string FullName => FindFirst(ClaimTypes.Name).Value;
}

public static class PrincipalExtensions
{
   public static MyPrincipal AsMyPrincipal(this IPrincipal principal)
   {
       if (principal != null)
       {
           return principal as MyPrincipal
               ?? new MyPrincipal(principal);
       }

       return null;
   }
}

public class HomeModule : NancyModule
{
    public HomeModule()
    {
        Get["/"] = (parameters, token) =>
        {
            var user = Context.CurrentUser.AsMyPrincipal();
            return Task.FromResult<dynamic>(user.FullName);
        };
    }
}
```

## Bootstrapper DiagnosticsConfiguration

In Nancy v2.x there is a whole new configuration API. This is now handled in the bootstrapper by overriding the `Configure` method.  For diagnostics, you can use it like this:

```csharp
public override void Configure(Nancy.Configuration.INancyEnvironment environment)
{
    environment.Diagnostics(password: "pa55w0rd!");
}
```

## Bootstrapper InternalConfiguration

This has a different signature but now takes in an `ITypeCatalog` argument. If you are using the `WithOverrides` API this should still work, you just need to change the method signature:

```csharp
protected override Func<ITypeCatalog, NancyInternalConfiguration> InternalConfiguration
{
    get
    {
        return NancyInternalConfiguration
            .WithOverrides(x =>
                x.ResponseProcessors = new[] { typeof(JsonProcessor) });
    }
}
```

## Nancy.Serializers.Json.ServiceStack Renamed

This namespace has been renamed to `Nancy.Serialization.ServiceStack`.

## DefaultResponseFormatter constructor change

One of the arguments was previously an array of `ISerializer`, this is now an `ISerializerFactory`.

## Known Issues

### Nancy.ViewEngines.Razor dependencies

As of 2.x, the `Nancy.ViewEngines.Razor` engine uses Roslyn internally to compile views. Unfortunately the Nuget for `Nancy.ViewEngines.Razor` (2.0-alpha) is missing a couple of dependencies which are needed to compile and render views. For the time being you will need to explicitly install these packages yourself (in the presented order):

- `Install-Package Microsoft.Net.Compilers -Version 1.1.1`
- `Install-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform -Version 1.0.1`
- `Install-Package Microsoft.CodeAnalysis.CSharp -Version 1.1.1`

If you get any runtime exceptions, please check your config file for any invalid assembly binding redirects.