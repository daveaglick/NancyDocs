Order: 2
---
This document provides an overview on how to enable stateless authentication in your Nancy application. Stateless authentication enables you to inspect each incoming request and, based on information about that request, decide if it should be treated as an authenticated request or not.

For instance, you could inspect the request to make sure that a query string parameter was passed in (perhaps an api key), that a certain header is available, or that the request originated from a certain ip-address. A good solution for transferring stateless authentication information is using [JWT](https://jwt.io/introduction/) 

The full request is at your disposal!

Stateless authentication can be setup for requests on a
- All modules (ie. application wide)
- Per module (ie. on a specific module only).

To enable stateless authentication, in your application, you need to complete the following steps

1. Install the `Nancy.Authentication.Stateless` package
1. Configure and enable Stateless Authentication
1. [Secure your modules](https://github.com/NancyFx/Nancy/wiki/Authentication-overview)

# Configure and enable stateless authentication

Stateless Authentication can be enabled for:

## All modules (ie. application wide)
```c#
StatelessAuthentication.Enable(pipelines, statelessAuthConfiguration);
```

This should be called from either `ApplicationStartup` or `RequestStartup` methods of your bootstrapper.

## Per module
```c#
StatelessAuthentication.Enable(this, statelessAuthConfiguration);
```

If you want to enable it per module, it has to be done in the module constructor.

The `statelessAuthConfiguration` variable, that is passed into `StatelessAuthentication.Enable` method, is an instance of the `StatelessAuthenticationConfiguration` type, which enables you to customize the behavior of the stateless authentication provider.

When creating an instance of the `StatelessAuthenticationConfiguration` type, it expects a single parameter of type `Func<NancyContext, IUserIdentity>`. The function is what is used to inspect the request (or anything else in the context for that matter) and return `null` if the request should not be treated as authenticated, or the appropriate [IUserIdentity](https://github.com/NancyFx/Nancy/wiki/Authentication-overview) if it should.

# Sample configuration

```c#
var configuration =
    new StatelessAuthenticationConfiguration(ctx =>
    {
        if (!ctx.Request.Query.apikey.HasValue)
        {
            return null;
        }

        // This would where you authenticated the request. IUserApiMapper is
        // not a Nancy type.
        var userValidator = 
            container.Resolve<IUserApiMapper>();

        return userValidator.GetUserFromAccessToken(ctx.Request.Query.apikey);
    });
```

Sample for securing a single module with JWT using [jose-jwt](https://github.com/dvsekhvalnov/jose-jwt) library :

```c#
var configuration =
    new StatelessAuthenticationConfiguration(ctx =>
    {
        var jwtToken = ctx.Request.Headers.Authorization;

        try
        {
            var payload = Jose.JWT.Decode<JwtToken>(jwtToken, SecretKey);

            var tokenExpires = DateTime.FromBinary(payload.exp);

            if (tokenExpires > DateTime.UtcNow)
            {
                return new ClaimsPrincipal(new HttpListenerBasicIdentity(payload.sub, null));
            }

            return null;


        }
        catch (Exception)
        {
            return null;
        }
    });

StatelessAuthentication.Enable(this, configuration);

```

Where `JwtToken` is a simple data class:

```
public class JwtToken
{
    public string  sub;
    public long exp;
}
```
