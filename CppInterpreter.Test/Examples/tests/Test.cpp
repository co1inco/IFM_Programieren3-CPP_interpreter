
#define fn auto
#define let const auto
#define var auto

class B
{
    virtual abstract test();
    int virtual test() { return 4; }
    
    fn foo() -> int
    {
        var i = 0;
        i += 1;
        return i;
    }
};

class A : public B
{
    
};