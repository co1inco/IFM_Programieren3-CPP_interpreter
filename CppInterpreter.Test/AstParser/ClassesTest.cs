using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Test.Helper;
using Shouldly;
using static CppInterpreter.Test.Helper.ParserHelper;

namespace CppInterpreter.Test.AstParser;

[TestClass]
public class ClassesTest
{


    [TestMethod]
    public void SimpleClassDefinition()
    {
        //Arrange
        var tree = GetTree("class TestClass {};", t => t.@class());

        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);
        
        //Assert
        ast.Ident.Value.ShouldBe("TestClass");
        ast.BaseTypes.ShouldBeEmpty();
        ast.Functions.ShouldBeEmpty();
        ast.Variables.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void WithBaseClass()
    {
        //Arrange
        var tree = GetTree("class TestClass : BaseClass1 {};", t => t.@class());

        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);
        
        //Assert
        ast.Ident.Value.ShouldBe("TestClass");
        ast.BaseTypes.ShouldHaveCount(1);
        ast.BaseTypes[0].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Value.ShouldBe("BaseClass1")
        );
        ast.Functions.ShouldBeEmpty();
        ast.Variables.ShouldBeEmpty();
    }
 
    [TestMethod]
    public void WithBaseClasses()
    {
        //Arrange
        var tree = GetTree("class TestClass : BaseClass1, public BaseClass2, private BaseClass3 {};", t => t.@class());

        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);
        
        //Assert
        ast.Ident.Value.ShouldBe("TestClass");
        ast.BaseTypes.ShouldHaveCount(3);
        ast.BaseTypes[0].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Value.ShouldBe("BaseClass1")
        );
        ast.BaseTypes[1].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Public),
            x => x.Member.Value.ShouldBe("BaseClass2")
        );
        ast.BaseTypes[2].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Value.ShouldBe("BaseClass3")
        );
        
        ast.Functions.ShouldBeEmpty();
        ast.Variables.ShouldBeEmpty();
    }

    [TestMethod]
    public void WithFuncDefinition()
    {
        //Arrange
        var tree = GetTree("class TestClass { void foo() {} int bar(int x, int y) {} };", t => t.@class());

        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);

        //Assert
        ast.Functions.ShouldHaveCount(2);
        ast.Functions[0].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Ident.Value.ShouldBe("foo"),
            x => x.Member.ReturnType.Ident.ShouldBe("void")
        );
        
        ast.Functions[1].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Ident.Value.ShouldBe("bar"),
            x => x.Member.ReturnType.Ident.ShouldBe("int")
        );
    }
    
    [TestMethod]
    public void WithVarDefinition()
    {
        //Arrange
        var tree = GetTree("class TestClass { int foo = 5; string bar; };", t => t.@class());

        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);

        //Assert
        ast.Variables.ShouldHaveCount(2);
        ast.Variables[0].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Ident.Value.ShouldBe("foo"),
            x => x.Member.Type.Ident.ShouldBe("int"),
            x => x.Member.Initializer.ShouldNotBeNull()
        );
        
        ast.Variables[1].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Ident.Value.ShouldBe("bar"),
            x => x.Member.Type.Ident.ShouldBe("string")
        );
    }

    [TestMethod]
    public void WithConstructors()
    {
        //Arrange
        var tree = GetTree("class TestClass { TestClass() {} TestClass(int x) {} };", t => t.@class());
        
        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);
        
        //Assert
        ast.Constructors.ShouldHaveCount(2);
        ast.Constructors[0].ShouldSatisfyAllConditions(
            x => x.Member.Ident.Value.ShouldBe("$ctor"),
            x => x.Member.ReturnType.Ident.ShouldBe("void"),
            x => x.Member.Arguments.ShouldBeEmpty()
        );
        ast.Constructors[1].ShouldSatisfyAllConditions(
            x => x.Member.Ident.Value.ShouldBe("$ctor"),
            x => x.Member.ReturnType.Ident.ShouldBe("void"),
            x => x.Member.Arguments.ShouldHaveCount(1)
        );
    }
    
    [TestMethod]
    public void WithDestructor()
    {
        //Arrange
        var tree = GetTree("class TestClass { ~TestClass() {} };", t => t.@class());
        
        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);
        
        //Assert
        ast.Destructor.ShouldNotBeNull();
        ast.Destructor.Ident.Value.ShouldBe("$dtor");
    }
    
    [TestMethod]
    public void WrongDestructorName()
    {
        //Arrange
        var tree = GetTree("class TestClass { ~TestClassX() {} };", t => t.@class());
        
        //Act
        
        //Assert
        Should.Throw<ParserException>(() => Ast.AstParser.ParseCompoundTypeDefinition(tree));
    }
        
    [TestMethod]
    public void MultipleDestructors()
    {
        //Arrange
        var tree = GetTree("class TestClass { ~TestClass() {} ~TestClass() {} };", t => t.@class());
        
        //Act
        
        //Assert
        Should.Throw<ParserException>(() => Ast.AstParser.ParseCompoundTypeDefinition(tree));
    }
    
}