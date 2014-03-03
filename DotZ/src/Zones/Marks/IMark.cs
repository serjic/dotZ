using System.Collections.Generic;

namespace DotZ.Zones.Marks
{
  public interface IMark
  {
    IMark Substract(IMark ownerMark);
    IEnumerable<int> EnumerateIndicies();
  }
}