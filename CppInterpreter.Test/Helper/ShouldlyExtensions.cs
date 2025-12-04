using Antlr4.Runtime.Tree;
using Shouldly;

namespace CppInterpreter.Test.Helper;

[ShouldlyMethods]
public static class ShouldlyExtensions
{

    public static void ShouldBeText(this ITerminalNode node, string expected) => 
        node.GetText().ShouldBe(expected);

    public static void ShouldHaveIndex<T>(this IEnumerable<T> collection, int expected) => 
        collection.Count().ShouldBeGreaterThan(expected);

    public static IList<T> ShouldHaveCount<T>(this IList<T> collection, int expected)
    {
        collection.Count.ShouldBe(expected);
        return collection;    
    }
    
    public static IList<T> ShouldHaveItemWith<T>(this IList<T> collection, int item, Action<T> expectation)
    {
        collection.ShouldHaveIndex(item);
        expectation(collection[item]);
        return collection;
    }
}