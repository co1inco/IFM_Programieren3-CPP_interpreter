#include "hsbi_runtime.h"

int main()
{
    
    for (int i=0; i<5; ++i)
    {
        print(i);
    }
    
    return 0;
}
/* EXPECT:
1
2
3
4
5
*/
