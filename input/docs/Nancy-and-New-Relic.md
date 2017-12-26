[New Relic](http://www.newrelic.com) is a nice tool used to monitor applications and collect performance metrics running on pretty much any platform, including .NET. Wiring up Nancy with [New Relic](http://www.newrelic.com) is a cinch! 

Out of the box, the NewRelic agent traces all transactions as `NancyHttpHandler` - not very useful! But with the [.NET Agent API](https://docs.newrelic.com/docs/agents/net-agent/features/net-agent-api), it's possible to set an explicit identifier for each transaction logged.

First, install the [corresponding NuGet package](http://www.nuget.org/packages/NewRelic.Agent.Api/). Then, add or edit a `Module` that will be the base for all your other modules:

```c#
using NewRelicAgent = NewRelic.Api.Agent.NewRelic; // protip: don't give class and namespace the same name. it's awkward.

public abstract class BaseModule : NancyModule
{
    public BaseModule()
    {
        Before += ctx =>
        {
            var routeDescription = ctx.ResolvedRoute.Description;
            NewRelicAgent.SetTransactionName("Custom/Endpoint", string.Format("{0} {1}", routeDescription.Method, routeDescription.Path));

            return null;
        };
    }
}
```

Then you need to install New Relic's .Net agent on your system.  Make sure that the architecture (32-bit or 64-bit) matches your system. Also, if you're running a non-IIS app, make sure you check the `Instrument All .NET Applications` box.

**Note:** I added the request method because it doesn't seem to be traced by NewRelic...

Now that transactions get more usable, it's also interesting to add Nancy's internals to NewRelic's transaction timings. To achieve that, create and deploy a [custom instrumentation file](https://docs.newrelic.com/docs/agents/net-agent/instrumentation/net-custom-instrumentation) with the following content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<extension xmlns="urn:newrelic-extension">
    <instrumentation>
        <tracerFactory name="NewRelic.Agent.Core.Tracer.Factories.BackgroundThreadTracerFactory" metricName="Custom/Endpoint">
            <match assemblyName="Nancy" className="Nancy.NancyEngine">
                <exactMethodMatcher methodName="HandleRequest" />
            </match>
        </tracerFactory>
    </instrumentation>
</extension>
```
This file should be placed in `C:\ProgramData\New Relic\.NET Agent\Extensions`.  

You will now see the time spent by Nancy (and your application code) to handle the request.

# Alternative Approach

If you don't want to change all of your base classes, another alternative is to [hook into Nancy's module building process](http://stuff-for-geeks.com/nancys-raven/).  To take this approach, first create a class that implements the `INancyModuleBuilder` interface copying the [`DefaultNancyModuleBuilder` implementation]( https://github.com/NancyFx/Nancy/blob/master/src/Nancy/Routing/DefaultNancyModuleBuilder.cs) as a starting point.  Add a `Before` handler with a method body similar to the one shown above.

```c#
using Nancy;
using Nancy.ModelBinding;
using Nancy.Routing;
using Nancy.Validation;
using Nancy.ViewEngines;
using NewRelicAgent = NewRelic.Api.Agent.NewRelic;

namespace MyServer
{
  public class MyModuleBuilder : INancyModuleBuilder
  {
    private readonly IViewFactory viewFactory;
    private readonly IResponseFormatterFactory responseFormatterFactory;
    private readonly IModelBinderLocator modelBinderLocator;
    private readonly IModelValidatorLocator validatorLocator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultNancyModuleBuilder"/> class.
    /// </summary>
    /// <param name="viewFactory">The <see cref="IViewFactory"/> instance that should be assigned to the module.</param>
    /// <param name="responseFormatterFactory">An <see cref="IResponseFormatterFactory"/> instance that should be used to create a response formatter for the module.</param>
    /// <param name="modelBinderLocator">A <see cref="IModelBinderLocator"/> instance that should be assigned to the module.</param>
    /// <param name="validatorLocator">A <see cref="IModelValidatorLocator"/> instance that should be assigned to the module.</param>
    public MyModuleBuilder(IViewFactory viewFactory, IResponseFormatterFactory responseFormatterFactory, IModelBinderLocator modelBinderLocator, IModelValidatorLocator validatorLocator)
    {
      this.viewFactory = viewFactory;
      this.responseFormatterFactory = responseFormatterFactory;
      this.modelBinderLocator = modelBinderLocator;
      this.validatorLocator = validatorLocator;
    }

    /// <summary>
    /// Builds a fully configured <see cref="INancyModule"/> instance, based upon the provided <paramref name="module"/>.
    /// </summary>
    /// <param name="module">The <see cref="INancyModule"/> that should be configured.</param>
    /// <param name="context">The current request context.</param>
    /// <returns>A fully configured <see cref="INancyModule"/> instance.</returns>
    public INancyModule BuildModule(INancyModule module, NancyContext context)
    {
      module.Context = context;
      module.Response = this.responseFormatterFactory.Create(context);
      module.ViewFactory = this.viewFactory;
      module.ModelBinderLocator = this.modelBinderLocator;
      module.ValidatorLocator = this.validatorLocator;
      module.Before.AddItemToStartOfPipeline(ctx =>
      {
        var routeDescription = ctx.ResolvedRoute.Description;
        NewRelicAgent.SetTransactionName(module.GetType().Name, string.Format("{0} {1}", routeDescription.Method, routeDescription.Path));
        return null;
      });

      return module;
    }
  }
}
```

Then, in a custom [bootstrapper](https://github.com/NancyFx/Nancy/wiki/Bootstrapper) class, tell Nancy to use your module builder instead of the default builder

```c#
public class MyBootstrapper : DefaultNancyBootstrapper
{
  protected override NancyInternalConfiguration InternalConfiguration
  {
    get { return NancyInternalConfiguration.WithOverrides(x => x.NancyModuleBuilder = typeof(MyModuleBuilder)); }
  }  
}
```