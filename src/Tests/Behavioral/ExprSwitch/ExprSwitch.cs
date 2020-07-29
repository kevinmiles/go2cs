// _Switch statements_ express conditionals across many
// branches.

// package main -- go2cs converted at 2020 July 29 04:18:06 UTC
// Original source: D:\Projects\go2cs\src\Tests\Behavioral\ExprSwitch\ExprSwitch.go
using fmt = go.fmt_package;
using time = go.time_package;
using static go.builtin;

namespace go
{
    public static partial class main_package
    {
        private static long x = 1L;

        private static long getNext()
        {
            x++;
            return x;
        }

        private static void Main()
        {
            // Here's a basic `switch`.
            long i = 2L;
            fmt.Print("Write ", i, " as ");
            switch (i)
            {
                case 1L:
                    fmt.Println("one");
                    break;
                case 2L:
                    fmt.Println("two");
                    break;
                case 3L:
                    {
                        fmt.Println("three");
                    }
                    break;
                default:
                    fmt.Println("unknown");
                    break;
            } 

            // You can use commas to separate multiple expressions
            // in the same `case` statement. We use the optional
            // `default` case in this example as well.
            if (time.Now().Weekday() == time.Saturday || time.Now().Weekday() == time.Sunday)
                fmt.Println("It's the weekend");
            else if (time.Now().Weekday() == time.Monday)
                fmt.Println("Ugh, it's Monday");
            else
                fmt.Println("It's a weekday"); 

            // `switch` without an expression is an alternate way
            // to express if/else logic. Here we also show how the
            // `case` expressions can be non-constants.
            var t = time.Now();
            if (t.Hour() < 12L)
                fmt.Println("It's before noon");
            else
                fmt.Println("It's after noon"); 

            // A type `switch` compares types instead of values.  You
            // can use this to discover the type of an interface
            // value.  In this example, the variable `t` will have the
            // type corresponding to its clause.
            /*
                whatAmI := func(i interface{}) {
                    switch t := i.(type) {
                    case bool:
                        fmt.Println("I'm a bool")
                    case int:
                        fmt.Println("I'm an int")
                    default:
                        fmt.Printf("Don't know type %T\n", t)
                    }
                }
                whatAmI(true)
                whatAmI(1)
                whatAmI("hey")
            */
            // Here is a switch with simple statement and a fallthrough
            {
                long j = 1L;

                if (getNext() == 0L)
                {
                    fmt.Println("zero");
                    goto __switch_break0;
                }
                if (getNext() == 1L || getNext() == 2L)
                {
                    fmt.Println("one or two");
                    fallthrough = true;
                }
                if (fallthrough || getNext() == 3L)
                {
                    fmt.Printf("three, but x=%d and local i = %d\n", x, j);
                }
                // default:
                fmt.Println("plus, always a default here because of fallthrough");
                __switch_break0:;
            }
        }
    }
}
