using System.Numerics;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public static class CppCommonOperators
{



    public static IEnumerable<ICppFunction> ArithmeticOperators<T, TP>()
        where T : ICppPrimitiveValueT<TP, T>
        where TP : INumber<TP> =>
    [
        new MemberFunction<T, T, T>("operator+", (a, b) => T.Create(a.Value + b.Value)),
        new MemberFunction<T, T, T>("operator-", (a, b) => T.Create(a.Value - b.Value)),
        new MemberFunction<T, T, T>("operator*", (a, b) => T.Create(a.Value * b.Value)),
        new MemberFunction<T, T, T>("operator/", (a, b) => T.Create(a.Value / b.Value)),
        new MemberFunction<T, T, T>("operator%", (a, b) => T.Create(a.Value % b.Value)),
    ];

    public static IEnumerable<ICppFunction> PostCrementOperators<T, TP>()
        where T : ICppPrimitiveValueT<TP, T>
        where TP : INumber<TP> =>
    [
        new MemberFunction<T, T, T>("operator++", (a, _) => T.Create(a.Value++)),
        new MemberFunction<T, T, T>("operator--", (a, _) => T.Create(a.Value--))
    ];
    
    public static IEnumerable<ICppFunction> PreCrementOperators<T, TP>()
        where T : ICppPrimitiveValueT<TP, T>
        where TP : INumber<TP> =>
    [
        new MemberFunction<T, T>("operator++", (a) =>
        {
            ++a.Value;
            return a;
        }),
        new MemberFunction<T, T>("operator--", (a) =>
        {
            --a.Value;
            return a;
        })
    ];
    
    
    public static IEnumerable<ICppFunction> EquatorOperators<T, TP>()
        where T : ICppPrimitiveValueT<TP, T>
        where TP : IEquatable<TP> =>
    [
        new MemberFunction<T, T, CppBoolValue>("operator==", (a, b) => new CppBoolValue(a.Value.Equals(b.Value))),
        new MemberFunction<T, T, CppBoolValue>("operator!=", (a, b) => new CppBoolValue(!a.Value.Equals(b.Value)))
    ];
    
    public static IEnumerable<ICppFunction> ComparisionOperators<T, TP>()
        where T : ICppPrimitiveValueT<TP, T>
        where TP : IComparable<TP> =>
    [
        new MemberFunction<T, T, CppBoolValue>("operator<", (a, b) => new CppBoolValue(a.Value.CompareTo(b.Value) < 0)),
        new MemberFunction<T, T, CppBoolValue>("operator<=", (a, b) => new CppBoolValue(a.Value.CompareTo(b.Value) <= 0)),
        new MemberFunction<T, T, CppBoolValue>("operator>", (a, b) => new CppBoolValue(a.Value.CompareTo(b.Value) > 0)),
        new MemberFunction<T, T, CppBoolValue>("operator>=", (a, b) => new CppBoolValue(a.Value.CompareTo(b.Value) >= 0))
    ];
    
    public static ICppFunction PrimitiveAssignment<T, TP>() where T : ICppPrimitiveValueT<TP, T> => 
        new MemberAction<T, T>("operator=", (i, a) => i.Value = a.Value);
    
    
    
    public static IEnumerable<ICppFunction> IntegerOperators<T, TP>()
        where T : ICppPrimitiveValueT<TP, T>
        where TP : INumber<TP> =>
    [
        ..ArithmeticOperators<T, TP>(),
        ..EquatorOperators<T, TP>(),
        ..ComparisionOperators<T, TP>(),
        PrimitiveAssignment<T, TP>(),
        ..PreCrementOperators<T, TP>(), // Point& operator++();       // Prefix increment operator. can simply return the same instance
        ..PostCrementOperators<T, TP>() // Point operator++(int);     // Postfix increment operator.
        //TODO add increment / decrement operator (postfix would simply return the old value before increment)
        
        
    ];
}
