# Ninject

First, install the [Nancy.Bootstrappers.Ninject](http://nuget.org/packages/Nancy.Bootstrappers.Ninject) package. Then, make your custom bootstrapper inherit from `NinjectNancyBootstrapper` instead of `DefaultNancyBootstrapper`. Finally, override the `ConfigureApplicationContainer` and the `ConfigureRequestContainer` methods, and bind your dependencies. The `container` parameter in `ConfigureRequestContainer` is a child container which is disposed at the end of the request.

    public class Bootstrapper : NinjectNancyBootstrapper
    {             
        protected override void ConfigureApplicationContainer(IKernel existingContainer)
        {
            //application singleton
            existingContainer.Bind<IApplicationSingleton>()
                .To<ApplicationSingleton>().InSingletonScope();
            //transient binding
            existingContainer.Bind<ICommandHandler>().To<CommandHandler>();
        }

        protected override void ConfigureRequestContainer(IKernel container, NancyContext context)
        {
            //container here is a child container. I.e. singletons here are in request scope.
            //IDisposables will get disposed at the end of the request when the child container does.
            container.Bind<IPerRequest>().To<PerRequest>().InSingletonScope();
        }
    }

# Autofac
From [stackoverflow](https://stackoverflow.com/questions/17325840/registering-startup-class-in-nancy-using-autofac-bootstrapper/18997394#18997394)    

    public class Bootstrapper : AutofacNancyBootstrapper
    {
        protected override void ConfigureApplicationContainer(ILifetimeScope existingContainer)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<User>()
                   .As<IUser>()
                   .SingleInstance();

            builder.Update(existingContainer.ComponentRegistry);          
        }
    }
# More to come

...