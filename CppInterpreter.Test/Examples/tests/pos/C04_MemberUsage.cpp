#include "hsbi_runtime.h"

class Test
{
public:
    Test(int n)
    {
        x = n;
    }
    
    void foo()
    {
        x = x + 1;
    }
    
    int x;
};

int main() {
    
    Test t = Test(5);
    print(t.x);
    
    t.foo();   
    t.foo();
    
    print(t.x);
    
    return 0;
}
/* EXPECT:
5
7
*/