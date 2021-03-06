using fmt = go.fmt_package;
using static go.builtin;

namespace go
{
    public static partial class main_package
    {
        public partial interface Animal
        {
            @string Type();
            @string Swim();
        }

        public partial struct Dog
        {
            public @string Name;
            public @string Breed;
        }

        public partial struct Frog
        {
            public @string Name;
            public @string Color;
        }

        private static void Main()
        {
            var f = @new<Frog>();
            var d = @new<Dog>();
            array<Animal> zoo = new array<Animal>(new Animal[] { Animal.As(f), Animal.As(d) });

            Animal a = null;
            fmt.Printf("%T\n", a);

            {
                Animal a__prev1 = a;

                foreach (var (_, __a) in zoo)
                {
                    a = __a;
                    fmt.Println(a.Type(), "can", a.Swim());
                } // Redclared post comment

                a = a__prev1;
            }

            fmt.Printf("%T\n", a);

            foreach (var (_, __a) in zoo)
            {
                a = __a;
                fmt.Println(a.Type(), "can", a.Swim());
            } 

            // Post for comment

            fmt.Printf("%T\n", a); 

            // vowels[ch] is true if ch is a vowel
            array<bool> vowels = new array<bool>(InitKeyedValues<bool>(128, ('a', true), ('e', true), ('i', true), ('o', true), ('u', true), ('y', true)));
            fmt.Println(vowels);
        }

        private static @string Type(this ref Frog f)
        {
            return "Frog";
        }

        private static @string Swim(this ref Frog f)
        {
            return "Kick";
        }

        private static @string Swim(this ref Dog d)
        {
            return "Paddle";
        }

        private static @string Type(this ref Dog d)
        {
            return "Doggie";
        }
    }
}
