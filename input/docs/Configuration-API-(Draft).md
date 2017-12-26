# Configuring your application

- [Overview](#overview)
  - [Introducing the configuration environment](#introducing-the-configuration-environment)
  - [Defining the configuration of your application](#defining-the-configuration-of-your-application)
  - [Getting a hold of an INancyEnvironment instance](#getting-a-hold-of-an inancyenvironment-instance)
  - [Accessing the environment from outside of Nancy](#accessing-the-environment-from-outside-of-nancy)

- [Adding your own configurations](#adding-your-own-configurations)
  - [Creating a configuration object](#creating-a-configuration-object)
  - [Creating environment extensions](#creating-environment-extensions)
  - [Providing default configurations](#providing-default-configurations)

- [Customizing the configuration system](#customizing-the-configuration-system)
  - [Controlling the configuration of an environment](#controlling-the-configuration-of-an-environment)
  - [Controlling the creation of an environment](#controlling-the-creation-of-an-environment)

## Overview

As of Nancy `2.x` there is a new configuration system in place. The purpose of this system is to provide a unified configuration story for both Nancy and user-written functionality. It has been designed to be both lightweight and extensible, while still encouraging a set of best practices. The core of
this system is located in the new `Nancy.Configuration` namespace.

Configuration objects do not share any common base class or interface, but instead they are plain objects that should be tailored to meet the configuration requirements of each component that they are used for.

The system is designed to be setup once, during application startup, and will be called by the `INancyBootstrapper.Initialise` method.

### Introducing the configuration environment

At the heart of the configuration system we find the `INancyEnvironment` interface. It defines the symmetric pair of base operations, for storing and retrieving configuration objects inside a Nancy application.

```c#
/// <summary>
/// Defines the functionality of a Nancy environment.
/// </summary>
public interface INancyEnvironment : IReadOnlyDictionary<string, object>, IHideObjectMembers
{
    /// <summary>
    /// Adds a <paramref name="value"/>, using a provided <paramref name="key"/>, to the environment.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of the value to add.</typeparam>
    /// <param name="key">The key to store the value as.</param>
    /// <param name="value">The value to store in the environment.</param>
    void AddValue<T>(string key, T value);

    /// <summary>
    /// Gets the value that is associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of the value to retrieve.</typeparam>
    /// <param name="key">The key to get the value for.</param>
    /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
    /// <returns><see langword="true" /> if the value could be retrieved, otherwise <see langword="false" />.</returns>
    bool TryGetValue<T>(string key, out T value);
}
```

It is as simple as that! You store and retrieve configuration objects using a basic `key` in the form of a string. Nancy does not enforce any naming conventions, or semantics, on the `key`. Instead it will be up to the individual implementations, of the `INancyEnvironment`, to impose such things.

Out of the box, Nancy provides a default `INancyEnvironment` implementation called `DefaultNancyEnvironment`. This implementation _requires each key to be unique_ or an exception will be thrown. Even though it enforces the use of a unique key, it does not enforce any naming conventions so it will be up
to the user to supply their own conventions for their configuration objects.

We _highly recommend_ that the used keys are well documented as they are required, by user code, if you want to retrieve a value.

By default, Nancy will always store its own configuration objects, using the _full type name_ of the object. We do this by making use of the `AddValue<T>` and `GetValue<T>()` extension methods as documented below. This enable use to add and retrieve any values without having to bother with _magic string_ values, and means we are less likely to break any user code if we refactor any code.

### Getting a hold of an INancyEnvironment instance

Once the environment has been configured, during application startup, it will be registered in the available container. To get an instance of the current `INancyEnvironment` instance all you have to do it take a constructor dependency on it.

### INancyEnvironment extension methods

To make life a bit simpler, Nancy provides a series of extension methods on `INancyEnvironment`. These extensions exist to help make it easier to work with configuration objects.

|Name|Description|
|----|-----------|
|`void AddValue<T>(T value)`|Adds a value to the environment, using the full name of the type defined by `T` as the key|
|`T GetValue<T>()`|Gets a value from the environment, using the full name of the type defined by `T` as the key|
|`T GetValue<T>(string key)`|Gets a value from the environment, using the provided `key`|
|`T GetValueWithDefault<T>(T defaultValue)`|Gets a value from the environment, using the full name of the type defined by `T` as the key. If the value could not be found, then `defaultValue` is returned|
|`T GetValueWithDefault<T>(string key, T defaultValue)`|Gets a value from the environment, using the provided `key`. If the value could not be found, then `defaultValue` is returned|

## Defining the configuration of your application

Each [NancyBootstrapperBase]() implementations contains a method with the signature `Configure(INancyEnvironment environment)`. By overriding this method, in your own bootstrapper, you can gain access to the `INancyEnvironment` and define the configuration of your application.

Here is a sample of what it can look like when using `Configure` to configure your application.

```c#
public override void Configure(INancyEnvironment environment)
{
    environment.Diagnostics(
        enabled: true,
        password: "password",
        path: "/_Nancy",
        cookieName: "__custom_cookie",
        slidingTimeout: 30,
        cryptographyConfiguration: CryptographyConfiguration.NoEncryption);

    environment.Tracing(
        enabled: true,
        displayErrorTraces: true);

    environment.MyConfig("Hello World");
}
```

The sample configures `Diagnostics` and `Tracing` as well as a custom `MyConfig`. The configuration methods are defined as extension methods on the `INancyEnvironment` interface and provides whatever overloads that are necessary for each set of configuration.

## Accessing the environment from outside of Nancy

Where ever you have access to an `INancyBootstrapper` instance (most likely from outside of Nancy, such in hosting environments and the likes) you can get access to the `INancyEnvironment` through the [INancyBootstrapper.GetEnvironment()](https://github.com/NancyFx/Nancy/blob/45238076ad0b7f6ecabd6bae8469e30458d02efe/src/Nancy/Bootstrapper/INancyBootstrapper.cs#L29) method.

## Adding your own configuration

As earlier stated, the configuration system has been designed so that it can be leveraged by user code and third party extensions to Nancy. How you use it is really up to you, but we have been following a certain pattern for configurations provided by Nancy and we encourage you to consider them for your own configurations.

- A configuration object should be _immutable_
- The configuration object should limit the amount of build in logic
- There should always be a default value present even if the user has not explicitly provided any configuration
- Use `INancyEnvironment` extension methods to provide an API for setting up the configuration object

### Creating a configuration object

Creating your own configuration object to be used by the configuration system, could not be easier. You simply create a class that will hold the values you are interested in and what other API you deem fit for it to expose. That is it!

Below is a sample configuration object that simple stores a string value. The class is designed to be immutable because configuration values should not be changing over the lifetime of your application.

```c#
public class MyConfig
{
    public MyConfig(string value)
    {
        this.Value = value;
    }

    public string Value { get; private set; }
}
```

### Creating environment extensions

Once you have defined your configuration object, it is time to define the API methods that users will be using to create, and add, an instance of it to the `INancyEnvironment`.

Below is a sample `INancyEnvironment` extension method, used to configure an `MyConfig` instance and add it to the environment.

```c#
public static class MyConfigExtensions
{
    /// <summary>
    /// Configures an instance of <see cref="MyConfig" />.
    /// </summary>
    /// <param name="value">The value to store in the config.</param>
    public static void MyConfig(this INancyEnvironment environment, string value)
    {
        environment.AddValue(
            typeof(MyConfig).FullName, // Using the full type name of the type to avoid collisions
            new MyConfig(value));
    }
}
```

For the configuration extensions, provided by Nancy, we try to follow a series of rules

- Limit the number of overloads to as few as possible
- Mandatory parameters should be defined at the beginning of the parameters list
- Optional parameters should be defined at the end of the parameters list
- Optional valuetype parameters should be nullable

While these rules are not mandatory to comply with, since Nancy will not enforce them, it is encouraged that you do follow them to make your own APIs consisted with the ones provided by Nancy. By following the rules you will also help increase the discoverability of your own APIs.

Below is a sample extension method, that adds a second optional `int? amount` parameter, while leaving the old `value` mandatory.

```c#
public static class MyConfigExtensions
{
    /// <summary>
    /// Configures an instance of <see cref="MyConfig" />.
    /// </summary>
    /// <param name="value">The value to store in the config.</param>
    public static void MyConfig(this INancyEnvironment environment, string value, int? amount = null)
    {
        environment.AddValue(
            typeof(MyConfig).FullName, // Using the full type name of the type to avoid collisions
            new MyConfig(
              value,
              amount ?? int.MaxValue));
    }
}
```

By making `amount` nullable, we can detect that the value was omitted and provide our own default value instead. All usages of default values should be well documented so that your users will know what will be used if they omit to provide their own value for optional parameters.

### Providing default configurations

What if the user does not explicitly provide any configuration value for your configuration object? There are several ways in which this could be handled

- Call `TryGetValue<T>` on `INancyEnvironment` and check the returned bool value to determine if a there is a configuration object available
- Make use of the `GetValueWithDefault<T>(T defaultValue)` or `GetValueWithDefault<T>(string key, T defaultValue)` methods to ensure you always get a value back

While these both work, both can be a bit tedious to use if you are reading the value from multiple places.

Fortunately, with the help of the `INancyDefaultEnvironmentProvider` interface, there is another way to solve it.

```c#
/// <summary>
/// Defines the functionality for providing default configuration values to the <see cref="INancyEnvironment"/>.
/// </summary>
public interface INancyDefaultConfigurationProvider : IHideObjectMembers
{
    /// <summary>
    /// Gets the default configuration instance to register in the <see cref="INancyEnvironment"/>.
    /// </summary>
    /// <returns>The configuration instance</returns>
    object GetDefaultConfiguration();

    /// <summary>
    /// Gets the key that will be used to store the configuration object in the <see cref="INancyEnvironment"/>.
    /// </summary>
    /// <returns>A <see cref="string"/> containing the key.</returns>
    string Key { get; }
}
```

When implemented by a class, Nancy will pick up on the implementation and query it for a configuration object and the `key` that it should be stored under, in the `INancyEnvironment`.

If Nancy detects that the user has already provided a value for the configuration (indicated by the `key` already being present in the `INancyEnvironment`), then it will simply ignore to put the default value, the value returned by the `GetDefaultConfiguration`-method, into the environment.

If you have gotten into the good habit of using the full type name, of the configuration object, when storing values in the environment, then you can make this even simpler by inheriting from `NancyDefaultConfigurationProvider<T>`.

This is a simple base-class, implementation of the `INancyDefaultConfigurationProvider`, which provides a single method `T GetDefaultConfiguration();` and will automatically use the full type name, of `T`, as the key.

Below is a sample that shows how Nancy ensures that there is always a `ViewConfiguration` object present in the environment.

```c#
using Configuration;

/// <summary>
/// Provides the default configuration for <see cref="ViewConfiguration"/>.
/// </summary>
public class DefaultViewConfigurationProvider : NancyDefaultConfigurationProvider<ViewConfiguration>
{
    /// <summary>
    /// Gets the default configuration instance to register in the <see cref="INancyEnvironment"/>.
    /// </summary>
    /// <returns>The configuration instance</returns>
    /// <remarks>Will return <see cref="ViewConfiguration.Default"/>.</remarks>
    public override ViewConfiguration GetDefaultConfiguration()
    {
        return ViewConfiguration.Default;
    }
}
```

However you could make it more complex and apply more complex logic for setting up your default configuration object. Below is a sample which reads an environment variable to get a connection string from different config files depending on where the code is running.

```c#
public class DatabaseConfigurationProvider : NancyDefaultConfigurationProvider<DatabaseConfiguration>
{
    public override DatabaseConfiguration GetDefaultConfiguration()
    {
        var env =
            Environment.GetEnvironmentVariable("runtime-env") ?? "dev";

        var configFile =
            string.Concat("connectionstrings.", env, ".config");

        var config =
            SomeConfigFileLoaderHelper.Load(configFile);

        return new DatabaseConfiguration(config.ConnectionString);
    }
}
```

As with everything else in Nancy, both `INancyDefaultEnvironmentProvider` and `NancyDefaultConfigurationProvider<T>` are registered in the application container and thus can make use of constructor dependencies of their own.

## Customizing the configuration system

At the core level, Nancy is really only aware of two configuration interfaces; `INancyEnvironmentConfigurator` the `INancyEnvironment`. Everything else is extension points that have been added by the default implementations of each of these interfaces.

### Controlling the configuration of an environment

The `INancyEnvironmentConfigurator` interface defines the functionality of a class that is responsible for handing off a configured `INancyEnvironment` instance to Nancy during `NancyBootstrapperBase.Initialise`.

```c#
/// <summary>
/// Defines the functionality for applying configuration to an <see cref="INancyEnvironment"/> instance.
/// </summary>
public interface INancyEnvironmentConfigurator : IHideObjectMembers
{
    /// <summary>
    /// Configures an <see cref="INancyEnvironment"/> instance.
    /// </summary>
    /// <param name="configuration">The configuration to apply to the environment.</param>
    /// <returns>An <see cref="INancyEnvironment"/> instance.</returns>
    INancyEnvironment ConfigureEnvironment(Action<INancyEnvironment> configuration);
}
```

`NancyBootstrapperBase.Initialise` will pass in the `NancyBootstrapperBase.Configure` function to the `ConfigureEnvironment` method. This is how it can gain access to the user provided configuration settings and it may be with it as it pleases.

The `DefaultNancyEnvironmentConfigurator` implementation introduces two new concepts, to the configuration system; the `INancyDefaultConfigurationProvider` (which was described in the section [Providing default configurations](#providing-default-configurations)) and the `INancyEnvironmentFactory` (described in the [Controlling the creation of an environment](#controlling-the-creation-of-an-environment) section further down).

So Nancy does not really know about the concept of classes that can provide default configurations if none were provided by the user, that behavior is all introduced by `DefaultNancyEnvironmentConfigurator`.

If you need to customize the functionality around how an environment is configured, you should create your own `INancyEnvironmentConfigurator` implementation and register it with the bootstrapper, in your application.

```c#
protected override Func<ITypeCatalog, NancyInternalConfiguration> InternalConfiguration
{
    get
    {
        return NancyInternalConfiguration.WithOverrides(x => x.EnvironmentConfigurator = typeof(CustomEnvironmentConfigurator));
    }
}
```

### Controlling the creation of an environment

The `DefaultNancyEnvironmentConfigurator` implementation introduces the `INancyEnvironmentFactory` interface, which is responsible for providing an `INancyEnvironment` implementation which is going to be configured and returned to Nancy.

```c#
/// <summary>
/// Defines the functionality for creating a <see cref="INancyEnvironment"/> instance.
/// </summary>
public interface INancyEnvironmentFactory : IHideObjectMembers
{
    /// <summary>
    /// Creates a new <see cref="INancyEnvironment"/> instance.
    /// </summary>
    /// <returns>A <see cref="INancyEnvironment"/> instance.</returns>
    INancyEnvironment CreateEnvironment();
}
```

The `DefaultNancyEnvironmentFactory` will simply return `new DefaultNancyEnvironment();`, nothing more, nothing less.

If you would like to make sure Nancy uses a specific `INancyEnvironment` instance, then you should provide your own `INancyEnvironmentFactory` implementation and register it with the bootstrapper, in your application.

```c#
protected override Func<ITypeCatalog, NancyInternalConfiguration> InternalConfiguration
{
    get
    {
        return NancyInternalConfiguration.WithOverrides(x => x.EnvironmentFactory = typeof(CustomEnvironmentFactory));
    }
}
```
