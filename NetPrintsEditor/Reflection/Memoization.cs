﻿using System;
using System.Collections.Generic;

namespace NetPrintsEditor.Reflection
{
    public static class Memoization
    {
        // https://stackoverflow.com/a/2852595/4332314

        public static Func<R> Memoize<R>(this Func<R> f)
        {
            R r = default;

            return () =>
            {
                r ??= f();

                return r;
            };
        }

        public static Func<A, R> Memoize<A, R>(this Func<A, R> f)
        {
            Dictionary<A, R> d = [];

            return a =>
            {
                if (!d.TryGetValue(a, out R r))
                {
                    r = f(a);
                    d.Add(a, r);
                }

                return r;
            };
        }

        public static Func<A, B, R> Memoize<A, B, R>(this Func<A, B, R> f)
        {
            return f.Tuplify().Memoize().Detuplify();
        }

        public static Func<Tuple<A, B>, R> Tuplify<A, B, R>(this Func<A, B, R> f)
        {
            return t => f(t.Item1, t.Item2);
        }

        public static Func<A, B, R> Detuplify<A, B, R>(this Func<Tuple<A, B>, R> f)
        {
            return (a, b) => f(Tuple.Create(a, b));
        }
    }
}
