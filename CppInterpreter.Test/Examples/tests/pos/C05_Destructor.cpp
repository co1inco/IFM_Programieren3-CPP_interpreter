#include "hsbi_runtime.h"

class Test
{
public:
    ~Test()
    {
        print("destruct");
    }
    
};

int main() {
   
    {
        Test t;    
    }
    print("end");
    
    return 0;
}
/* EXPECT:
destruct
end
*/