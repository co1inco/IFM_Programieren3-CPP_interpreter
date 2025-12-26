using System.Numerics;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public static class CppCommonOperators
{



    public static IEnumerable<ICppFunction> ArithmeticOperators<T, TP>()
        where T : ICppPrimitiveValue<TP, T>
        where TP : INumber<TP> =>
    [
        new MemberFunction<T, T, T>("operator+", (a, b) => T.Create(a.Value + b.Value)),
        new MemberFunction<T, T, T>("operator-", (a, b) => T.Create(a.Value - b.Value)),
        new MemberFunction<T, T, T>("operator*", (a, b) => T.Create(a.Value * b.Value)),
        new MemberFunction<T, T, T>("operator/", (a, b) => T.Create(a.Value / b.Value)),
        new MemberFunction<T, T, T>("operator%", (a, b) => T.Create(a.Value % b.Value)),
    ];

    public static IEnumerable<ICppFunction> EquatorOperators<T, TP>()
        where T : ICppPrimitiveValue<TP, T>
        where TP : IEquatable<TP> =>
    [
        new MemberFunction<T, T, CppBoolValue>("operator==", (a, b) => new CppBoolValue(a.Value.Equals(b.Value))),
        new MemberFunction<T, T, CppBoolValue>("operator!=", (a, b) => new CppBoolValue(!a.Value.Equals(b.Value)))
    ];
    
    public static IEnumerable<ICppFunction> ComparisionOperators<T, TP>()
        where T : ICppPrimitiveValue<TP, T>
        where TP : IComparable<TP> =>
    [
        new MemberFunction<T, T, CppBoolValue>("operator<", (a, b) => new CppBoolValue(a.Value.CompareTo(b.Value) < 0)),
        new MemberFunction<T, T, CppBoolValue>("operator<=", (a, b) => new CppBoolValue(a.Value.CompareTo(b.Value) <= 0)),
        new MemberFunction<T, T, CppBoolValue>("operator>", (a, b) => new CppBoolValue(a.Value.CompareTo(b.Value) > 0)),
        new MemberFunction<T, T, CppBoolValue>("operator>=", (a, b) => new CppBoolValue(a.Value.CompareTo(b.Value) >= 0))
    ];
    
    public static ICppFunction PrimitiveAssignment<T, TP>() where T : ICppPrimitiveValue<TP, T> => 
        new MemberAction<T, T>("operator=", (i, a) => i.Value = a.Value);
    
    public static IEnumerable<ICppFunction> IntegerOperators<T, TP>()
        where T : ICppPrimitiveValue<TP, T>
        where TP : INumber<TP> =>
    [
        ..ArithmeticOperators<T, TP>(),
        ..EquatorOperators<T, TP>(),
        ..ComparisionOperators<T, TP>(),
        PrimitiveAssignment<T, TP>()
    ];
}
