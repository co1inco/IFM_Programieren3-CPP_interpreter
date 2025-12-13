// using Antlr4.Runtime;
// using Language;
// using Shouldly;
//
// namespace CppInterpreter.Test.Antlr;
//
//
//
// [TestClass]
// public class Comments
// {
//
//
//     [TestMethod]
//     public void Comment()
//     {
//         //Arrange
//         var tree = GetTree("// hello world");
//
//         //Act
//         
//         //Assert
//         //TODO validate program
//         tree.ShouldNotBeNull();
//     }
//
//
//     private GrammarParser.ProgramContext GetTree(string text)
//     {
//         var lexer = new GrammarLexer(CharStreams.fromString(text));
//         var parser = new GrammarParser(new CommonTokenStream(lexer));
//         return parser.program();
//     }
//     
// }