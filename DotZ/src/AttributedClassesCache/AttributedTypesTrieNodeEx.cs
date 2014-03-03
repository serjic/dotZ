using System.Collections.Generic;

namespace DotZ.AttributedClassesCache
{
  public static class AttributedTypesTrieNodeEx
  {
    public static IEnumerable<AttributedTypesTrieNode> Explore(this AttributedTypesTrieNode node, string[] names)
    {
      foreach (var name in names)
      {
        node = node.GetChild(name);
        if (node == null)
          yield break;
        yield return node;
      }
    }

    public static IEnumerable<AttributedTypesTrieNode> Enumerate(this AttributedTypesTrieNode node)
    {
      yield return node;
      foreach (var child in node.Children)
        foreach (var ret in Enumerate(child.Value))
          yield return ret;
    }
  }
}