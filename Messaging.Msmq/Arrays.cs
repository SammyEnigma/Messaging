using System;
using System.Diagnostics.Contracts;

namespace Messaging.Msmq
{
    internal static class Arrays
    {
        public static int IndexOf<T>(this T[] input, Predicate<T> predicate) => Array.FindIndex(input, predicate);

        public static T[] Add<T>(this T[] input, T value) => InsertAt(input, input.Length, value);

        public static T[] InsertAt<T>(this T[] input, int index, T value)
        {
            Contract.Requires(input != null);
            Contract.Requires(index >= 0);
            Contract.Requires(index <= input.Length);
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == input.Length + 1);

            var copy = new T[input.Length + 1];
            if (index > 0) // copy items before index
                Array.Copy(input, 0, copy, 0, index);
            copy[index] = value;
            if (index < input.Length) // copy items after index
                Array.Copy(input, index, copy, index + 1, input.Length - index);
            return copy;
        }

        public static T[] Remove<T>(this T[] input, T value)
        {
            int idx = Array.IndexOf(input, value);
            return idx < 0 ? input : RemoveAt(input, idx);
        }

        public static T[] RemoveAt<T>(this T[] input, int index)
        {
            Contract.Requires(input != null);
            Contract.Requires(input.Length > 0);
            Contract.Requires(index >= 0);
            Contract.Requires(index < input.Length - 1);
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == input.Length - 1);

            var copyLen = input.Length - 1;
            var copy = new T[copyLen];
            if (index > 0) // copy items before index
                Array.Copy(input, 0, copy, 0, index);
            if (index != copyLen) // copy items after index
                Array.Copy(input, index + 1, copy, index, copyLen - index);
            return copy;
        }

        public static T[] SliceAt<T>(this T[] input, int index)
        {
            Contract.Requires(input != null);
            Contract.Requires(index >= 0);
            Contract.Requires(index < input.Length);
            Contract.Ensures(Contract.Result<T[]>() != null);

            return SliceAt(input, index, input.Length - index - 1);
        }

        public static T[] SliceAt<T>(this T[] input, int index, int length)
        {
            Contract.Requires(input != null);
            Contract.Requires(index >= 0);
            Contract.Requires(index < input.Length);
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == length);

            var copy = new T[length];
            if (length > 0)
                Array.Copy(input, index, copy, 0, length);
            return copy;
        }

        public static T[] Copy<T>(this T[] input)
        {
            Contract.Requires(input != null);
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == input.Length);

            return SliceAt(input, 0, input.Length);
        }
    }
}
