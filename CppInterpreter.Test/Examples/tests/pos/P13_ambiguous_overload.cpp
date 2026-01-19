#include "hsbi_runtime.h"

void f(int  r) { print_int(r); print_string("f(int)");  }
void f(int& r) { print_int(r); print_string("f(int&)"); }

int main() {
    f(1);  // uses f(int)
    
    // Error because of an ambiguous function call
    // int x = 5;
    // f(x);
    
    return 0;
}
/* EXPECT (Zeile für Zeile):
1
f(int)
*/
