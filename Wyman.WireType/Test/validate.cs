using System;
using System.Collections.Generic;
using System.Linq;

namespace Wyman.WireType.Test
{
    class validate : Xunit.Assert
    {
        public static void All<T>(Func<T, bool> predicate, IReadOnlyCollection<T> collection)
        {
            NotNull(collection);

            if (!collection.All(predicate))
                throw new Xunit.Sdk.FalseException($"Not all items in the collection match the validation predicate.", false);
        }

        public static void Count<T>(int expected, IReadOnlyCollection<T> collection)
        {
            if (collection.Count != expected)
                throw new Xunit.Sdk.FalseException($"Collection count != {expected}.", false);
        }
    }
}
