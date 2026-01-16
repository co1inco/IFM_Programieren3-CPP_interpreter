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
0
1
2
3
4
*/
