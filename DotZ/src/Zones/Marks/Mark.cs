using System.Collections;
using System.Collections.Generic;

namespace DotZ.Zones.Marks
{
  internal class Mark : IMark
  {
    private readonly BitArray myBitarray;

    public Mark(BitArray bits)
    {
      myBitarray = bits;
    }

    public IMark Substract(IMark ownerMark)
    {
      var otherArray = ((Mark)ownerMark).myBitarray;
      return new Mark(((BitArray)myBitarray.Clone()).And(((BitArray)otherArray.Clone()).Not()));
    }

    public IEnumerable<int> EnumerateIndicies()
    {
      for (int i = 0; i < 300; i++)
      {
        if (myBitarray[i])
          yield return i;
      }
    }
  }
}