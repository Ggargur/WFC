using System;
using System.Collections.Generic;
using System.Linq;

namespace WaveFunction
{
    public static class LinqExtensions
    {
        public static T GetRandomByWeight<T>(this IEnumerable<(float weight, T item)> enumerable, Random random = null)
        {
            var list = enumerable.ToList();
            var totalWeight = list.Sum(entry => entry.weight);

            if (totalWeight <= 0f)
                throw new InvalidOperationException("The sum of the weights must be more than zero.");

            random ??= new Random();
            var randomValue = random.NextDouble() * totalWeight;

            var cumulative = 0f;
            foreach (var (weight, item) in list)
            {
                cumulative += weight;
                if (randomValue < cumulative)
                    return item;
            }

            return list.Last().item;
        }

        public static T GetRandom<T>(this IEnumerable<T> list, Random random = null)
        {
            return list.Select(x => (1f, x)).GetRandomByWeight(random);
        }
    }
}