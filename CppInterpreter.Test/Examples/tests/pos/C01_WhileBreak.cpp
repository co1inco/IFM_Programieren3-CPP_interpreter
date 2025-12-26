#include "hsbi_runtime.h"

int main() {
    // break loop
    
    int count = 0;
    while (count < 10) {
        print_bool(count);
        
        if (count == 5)
        {
            break;
        }
        
        count = count + 1;
    }

    // more complex loop
    int i = 0;
    while (i<5) {
        i = i + 1;
        
        if (count == 3)
        {
            continue;
        }
        
        print_int(i);
    }

    return 0;
}
/* EXPECT:
1
2
3
4
5
1
2
4
5
*/