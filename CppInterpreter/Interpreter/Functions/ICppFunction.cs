using CppInterpreter.Helper;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Functions;


public record CppFunctionParameter(string Name, ICppType Type, bool IsReference);

public interface ICppFunction
{
    string Name { get; }
    ICppType ReturnType { get; }
    ICppType? InstanceType { get; } // TODO: remove instance type from ICppFunction
    CppFunctionParameter[] ParameterTypes { get; }

    ICppValue Invoke(ICppValue? instance, ICppValue[] parameters);
}

public static class CppFunctionExtensions
{
    extension(IEnumerable<ICppType> a)
    {
        public bool FunctionParametersMatch(IEnumerable<ICppType> b) => 
            a.ZipFill(b)
                .All(z => z.Left?.Equals(z.Right) ?? false);

        public bool FunctionParametersMatch(IEnumerable<ICppValue> values) => 
            a.FunctionParametersMatch(values.Select(x => x.GetCppType));
        
        public bool FunctionParametersMissMatch(IEnumerable<ICppType> b) => 
            a.ZipFill(b)
                .Any(z => !(z.Left?.Equals(z.Right) ?? false));

        public bool FunctionParametersMissMatch(IEnumerable<ICppValue> values) => 
            a.FunctionParametersMissMatch(values.Select(x => x.GetCppType));
    }

    extension(IEnumerable<CppFunctionParameter> parameters)
    {
        public bool ParametersMatch(IEnumerable<ICppType> types) =>
            parameters.Select(x => x.Type)
                .FunctionParametersMatch(types);
        
        public bool ParametersMatch(IEnumerable<ICppValue> types) =>
            parameters.Select(x => x.Type)
                .FunctionParametersMatch(types);
        
        public bool ParametersMissMatch(IEnumerable<ICppType> types) =>
            parameters.Select(x => x.Type)
                .FunctionParametersMissMatch(types);
        
        public bool ParametersMissMatch(IEnumerable<ICppValue> types) =>
            parameters.Select(x => x.Type)
                .FunctionParametersMissMatch(types);
    }
    
    extension(ICppFunction function)
    {
        public bool ParametersMatch(IEnumerable<ICppType> types) => 
            function.ParameterTypes.ParametersMatch(types);
        
        public bool ParametersMatch(IEnumerable<ICppValue> types) => 
            function.ParameterTypes.ParametersMatch(types);

        public bool ParametersMatch(ICppFunction other) =>
            function.ParameterTypes.ParametersMatch(other.ParameterTypes.Select(x => x.Type));
        
        public bool ParametersMissMatch(IEnumerable<ICppType> types) => 
            function.ParameterTypes.ParametersMissMatch(types);
        
        public bool ParametersMissMatch(IEnumerable<ICppValue> types) => 
            function.ParameterTypes.ParametersMissMatch(types);
    }
}