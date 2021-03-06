Title: Accessing The Client Certificate When Using SSL
Order: 11
---
To authenticate the client the client can send a certificate. To do this in Nancy you need one of three hosting solutions: `Aspnet`, `WCF`, `OWIN` or `Hosting.Self`. Here is shown howto configure all three to work with SSL and client certificates.

# Configuration of `Aspnet`.

If the `web.config` file within the `system.webServer` tag we need to specify we want to be able to receive a ClientCertificate. Like this:

```xml
<security>
  <access sslFlags="SslNegotiateCert"/>
  <authentication>
    <clientCertificateMappingAuthentication enabled="true"/>
  </authentication>
</security>
```

You may get an error telling you this:

> This configuration section cannot be used at this path. This happens when the section is locked at a parent level. Locking is either by default (overrideModeDefault="Deny"), or set explicitly by a location tag with overrideMode="Deny" or the legacy allowOverride="false".

This is solved by editing your applicationhost.config and setting the `overrideModeDefault` to `Allow` for the following elements.

```xml
<section name="access" overrideModeDefault="Allow" />
<section name="clientCertificateMappingAuthentication" overrideModeDefault="Allow" />
```

See [here](http://www.microsoft.com/web/post/securing-web-communications-certificates-ssl-and-https) how to enable SSL for IISexpress.

See [here](http://www.iis.net/learn/manage/configuring-security/how-to-set-up-ssl-on-iis) how to do it on IIS.

# Configuration of `WCF`

Nothing is ever easy with `WCF` configuration, this is no exception. 

Lets start with the basic host:

```csharp
private static readonly Uri BaseUri = new Uri("https://192.168.123.126:1234/Nancy/");

var host = new WebServiceHost(
    new NancyWcfGenericService(new DefaultNancyBootstrapper()),
    BaseUri);
```

We need to tell the binding we want to use `Transport Security` and we need to tell it to expect a certificate from the client. We also need to tell `WCF` not to worry about whether the certificate is valid. Or at least determine ourselves what valid is.

## Binding

```csharp
var binding = new WebHttpBinding();
binding.Security.Mode = WebHttpSecurityMode.Transport;
binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
```

## Custom validation of the certificate.

```csharp
public class Auth : X509CertificateValidator
{
    public override void Validate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
    {
        return;
    }
}
```

Tell the host where to find the `Validator`

```csharp
host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.Custom;
host.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new Auth();
```

## Endpoint
Add the endpoint:
```csharp
host.AddServiceEndpoint(typeof(NancyWcfGenericService),binding,"");
```

Tell the host where to find the server certificate:
```csharp
host.Credentials.ServiceCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindByThumbprint, "30 3b 4a db 5a eb 17 ee ac 00 d8 57 66 93 a9 08 c0 1e 0b 71");
```

Open it:
```csharp
host.Open();
```

## Command line configuration
But this wont work you need to run a `netsh` command like this:
where certhash is the thumbprint of the server certificate without spaces.
```sh
netsh http add sslcert ipport=0.0.0.0:1234 certhash=303b4adb5aeb17eeac00d8576693a908c01e0b71 appid={00112233-4455-6677-8899-AABBCCDDEEFF} clientcertnegotiation=enable
```

# Configuration of `OWIN`
It'll just be there if the host sends it on.

If you use IIS as a host. You'll need to do the same config as with Aspnet. And you'll need an OWIN Aspnet host that supports the ClientCertificate. The [one](https://github.com/NancyFx/Nancy/blob/master/src/Nancy.Demo.Hosting.Owin/SimpleOwinAspNetHost.cs) in the OWIN demo in Nancy does. The [one](https://github.com/prabirshrestha/simple-owin) by @prabirshrestha also does.


# Configuration of `Hosting.Self`

It starts with a commandline command like the one in wcf: (remember the certhash is the thumbprint without spaces)
```sh
netsh http add sslcert ipport=0.0.0.0:1234 certhash=303b4adb5aeb17eeac00d8576693a908c01e0b71 appid={00112233-4455-6677-8899-AABBCCDDEEFF} clientcertnegotiation=enable
```

Then you can just set the url of the selfhost to `https://localhost:1234` and it'll work. The selfhost will automatically rewrite `localhost` to `+` if it has administrative rights. Allowing the selfhost to listen on all ipaddresses. 