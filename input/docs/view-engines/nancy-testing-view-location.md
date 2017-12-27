By default Nancy unit tests will struggle to find view files when you run your tests.  You can create the following file in your test project to help Nancy out.

```c#
public class TestingRootPathProvider : IRootPathProvider
{
  private static readonly string RootPath;

  static TestingRootPathProvider()
  {
    var directoryName = Path.GetDirectoryName(typeof (Bootstrapper).Assembly.CodeBase);

    if (directoryName != null)
    {
      var assemblyPath = directoryName.Replace(@"file:\", string.Empty);
      RootPath = Path.Combine(assemblyPath, "..", "..", "..", "Escape.Web");
    }
  }

  public string GetRootPath()
  {
    return RootPath;
  }
}
```

You will have to alter RootPath to match your solutions layout and target project's folder name. The idea is you want your view engine to not look in "bin/debug" for views but instead look in your target project for the views.  This is accomplished by using ".." to go up a folder level until you can then go into your target project.

As documented on the [root path wiki page](https://github.com/NancyFx/Nancy/wiki/The-root-path), the bootstrapper needs to be told about this class.  You can do this in a custom bootstrapper with the code

```c#
public class CustomBootstrapper : DefaultNancyBootstrapper
{
  protected override IRootPathProvider RootPathProvider
  {
    get { return new CustomRootPathProvider(); }
  }
}
```

Alternatively, in a unit test, it might look something like

```c#
[TestMethod()]
public void HomeModuleTest()
{
  // When
  var result = new Browser(with => {
    with.RootPathProvider<TestingRootPathProvider>();
    with.ViewFactory<TestingViewFactory>();
    with.AllDiscoveredModules();
  }).Get("/", with => {
    with.HttpRequest();
  });

  // Then
  Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
  Assert.AreEqual("MyViewName.cshtml", result.GetViewName());
}
```