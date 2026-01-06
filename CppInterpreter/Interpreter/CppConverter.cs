using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter;

public interface ICppConverter
{
    ICppType SourceType { get; }
    ICppType TargetType { get; }
    
    ICppValueT Convert(ICppValueT? value);
}

public class CppConverter<TSource, TTarget>(Func<TSource, TTarget> convert) : ICppConverter
    where TSource : ICppValueT
    where TTarget : ICppValueT
{
    public ICppType SourceType => TSource.TypeOf;
    public ICppType TargetType => TTarget.TypeOf;
    
    public ICppValueT Convert(ICppValueT? value)
    {
        if (value is not TSource source)
            throw new InvalidTypeException(TSource.TypeOf, value?.GetCppType);
        
        return convert(source);
    }
}