#include "hsbi_runtime.h"

class Test
{
public:
    void foo()
    {
        print_string("foo");
    }
};

int main() {
    
    Test t;
    t.foo();

    return 0;
}
/* EXPECT:
foo
*/