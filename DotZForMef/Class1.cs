using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Registration;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Zones;
using NUnit.Framework;

namespace DotZForMef
{
  [TestFixture]
  public class Class1
  {
    [Test]
    public void Foo()
    {
      var reflectionContext = new RegistrationBuilder();
      
      reflectionContext.ForTypesMatching(type =>
      {
        return type.GetCustomAttributes(typeof (ZoneAttribute)).Any();
      }).Export();
      
      var assemblyCatalog = new AssemblyCatalog(typeof(Class1).Assembly, reflectionContext);
      foreach (var part in assemblyCatalog)
      {
      }
    }

    private bool Filter(ComposablePartDefinition arg)
    {
      return true;
    }
  }

  public interface IFoo
  {
  }

  [Zone]
  class Foo : IRequire<Xxx>
  {
    public Foo()
    {
    }
  }

  [ZoneDefinition]
  internal class Xxx : IZone
  {
  }
}