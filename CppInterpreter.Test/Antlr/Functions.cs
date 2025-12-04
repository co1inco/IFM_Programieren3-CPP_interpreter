using CppInterpreter.Test.Helper;
using Language;
using Shouldly;

namespace CppInterpreter.Test.Antlr;

[TestClass]
public class Functions
{

    [TestMethod]
    [DataRow("int test()")]
    [DataRow("int* test()")]
    [DataRow("int *test()")]
    [DataRow("const int *test()")]
    [DataRow("int * const test()")]
    [DataRow("void test()")]
    [DataRow("void* test()")]
    [DataRow("void* const test()")]
    [DataRow("const void* test()")]
    public void FunctionParsing(string text)
    {
        //Arrange
        var decl = GetDeclTree(text);

        //Act

        //Assert
        decl.ident.ShouldNotBeNull();
    }



    private GrammarParser.FuncDeclContext GetDeclTree(string text) =>
        ParserHelper.GetTree(text, g => g.funcDecl());
}