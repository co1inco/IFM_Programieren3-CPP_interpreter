// See https://aka.ms/new-console-template for more information

using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using Language;

Console.WriteLine("Hello, World!");


// var a = new CppInt32Value(1);
// var b = new CppInt32Value(2);
// var c = a.InvokeMemberFunc("operator+", b);
//
// Console.WriteLine($"{a} + {b} = {c}");

// var scope = new CppStage1Scope()
// {
//     Values = new Scope<ICppValueBase>(),
//     Types = new Scope<ICppType>()
// };
//
// scope.Values.TryBindSymbol("test", new CppInt32Value(0));

var typeScope = new Scope<ICppType>();
typeScope.TryBindSymbol("int", CppTypes.Int32);
typeScope.TryBindSymbol("long", CppTypes.Int64);

var stage1Scope = Stage1Parser.CreateBaseScope();
var stage2Scope = Stage2Parser.CreateBaseScope();
var scope = new Scope<ICppValueBase>();

while (true)
{
    Console.Write(">>> ");
    var line = Console.ReadLine();
    
    if (line == "quit")
        break;
    if (string.IsNullOrWhiteSpace(line))
        continue;

    var lexer = new GrammarLexer(CharStreams.fromString(line));
    var aParser = new GrammarParser(new CommonTokenStream(lexer));

    // var ast = AstParser.ParseExpression(aParser.expression());

    var ast = AstParser.ParseStatement(aParser.statement());
    var s1 = Stage1Parser.ParseProgram([ ast ], stage1Scope);
    var s2 = Stage2Parser.ParseProgram(s1, stage2Scope);

    try
    {
        var stmt = Stage3Parser.ParseProgram(s2);

        Console.WriteLine(stmt(scope)?.StringRep() ?? "<void>");
    }
    catch (NotImplementedException)
    {
        throw;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Eval failed: {ex.Message}");
    }
     
    // var cpp = CppInterpreter.CppParser.CppParser.ParseExpression(ast, scope);
    //
    // Console.WriteLine(cpp.Evaluate().StringRep());
}


// var intType = new CppType("int", [], []);
// CppTypes.Instance.AddType(intType);
//
//
// var stringType = new CppType("string", [], []);
// CppTypes.Instance.AddType(stringType);



//
//
// public interface ICppType
// {
//     public string Name { get; }
//     public IReadOnlyCollection<ICppFunction> Function { get; }
//     
//     // bool CanApply(string operation, params IEnumerable<ICppObject> args);
//     // bool TryApply(string name, out CppObject result, params IEnumerable<ICppObject> args);
//     //
//     // void Apply(string operation, params IEnumerable<ICppObject> args);
//     //
//     // bool TryConvertTo<T>() where T : ICppObject;
//     //
//     // bool TryConvertFrom<T>() where T : ICppObject;
// }
//
//
// public interface ICppValue
// {
//     CppType Type { get; }
//     object? Value { get; }
// }
//
// public abstract class PrimitiveValue<T> : ICppValue
// {
//     public PrimitiveValue(CppType type, T value)
//     {
//         Type = type;
//         Value = value;
//     }
//     
//     public CppType Type { get; }
//     public T Value { get; }
//     
//     object ICppValue.Value => Value;
// }
//
// public sealed class IntValue(int value) : PrimitiveValue<int>(CppTypes.Instance.GetType("int"), value);
// public sealed class StringValue(string value) : PrimitiveValue<string>(CppTypes.Instance.GetType("string"), value);
//
//
//
// public interface ICppFunction
// {
//     public string Name { get; }
//     
//     public IEnumerable<ICppType> Parameters { get; }
//     
//     public ICppType Invoke(ICppType? target, params ICppType[] args);
//
//     public bool TryInvoke(ICppType? target, out ICppType result, params ICppType[] args);
//
// }
//
//
// public class CppFunction<TResult>(string name, TResult resultType) : ICppFunction where TResult : ICppType
// {
//     public string Name => name;
//     
//     public IEnumerable<ICppType> Parameters { get; }
//     
//     // public ICppType ReturnType => 
//     
//     public ICppType Invoke(ICppType? target, params ICppType[] args)
//     {
//         throw new NotImplementedException();
//     }
//
//     public bool TryInvoke(ICppType? target, out ICppType result, params ICppType[] args)
//     {
//         throw new NotImplementedException();
//     }
// }
//
// public interface ICppConversion
// {
//     public ICppType SourceType { get; }
//     public ICppType TargetType { get; }
//
//     public ICppValue Convert(ICppValue value);
// } 
//
//
//
// public class CppType
// {
//     private readonly List<ICppFunction> _functions;
//     private readonly Dictionary<string, ICppFunction> _functionsByName;
//     private readonly ICppConversion[] _conversions;
//
//
//     public CppType(
//         string name,
//         IEnumerable<ICppFunction> functions,
//         IEnumerable<ICppConversion> conversions)
//     {
//         Name = name;
//         
//         _functions = functions.ToList();
//         _functionsByName = _functions.ToDictionary(f => f.Name);
//         
//         _conversions = conversions.ToArray();
//     }
//     
//     
//     
//     public string Name { get; }
//
//     public ICollection<ICppFunction> Functions => _functions;
//
//     public ICollection<ICppConversion> Conversions => _conversions;
//
//
//     public void AddFunction(ICppFunction function)
//     {
//         _functions.Add(function);
//     }
// }
//
//
//
// public class CppTypes
// {
//     public static CppTypes Instance { get; } = new CppTypes();
//
//     private readonly Dictionary<string, CppType> _types = new Dictionary<string, CppType>();
//     
//     public CppTypes()
//     {
//         
//     }
//
//     public CppType GetType(string name)
//     {
//         return _types[name];
//     }
//
//     public bool TryGetType(string name, [NotNullWhen(true)] out CppType? type) =>  
//         _types.TryGetValue(name, out type);
//
//     public void AddType(CppType type)
//     {
//         _types.Add(type.Name, type);
//     }
//     
// }