using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Word = System.String;

namespace Anagrams
{
    using Occurrences = IEnumerable<Tuple<char, int>>;
    using Words = IEnumerable<Word>;
    using Sentence = IEnumerable<Word>;

    internal class Program
    {
        private static void Main()
        {
            var sentenceAnagrams = SentenceAnagrams(new[]{"Yes", "man"});
            foreach (var sentenceAnagram in sentenceAnagrams)
            {
                Console.WriteLine(string.Join(", ", sentenceAnagram));
            }
        }

        private static Words _dictionary;
        private static Words Dictionary {
            get { return _dictionary ?? (_dictionary = File.ReadAllLines("linuxwords.txt").ToList()); }
        }

        private static Occurrences WordOccurrences(Word w)
        {
            return w
                .ToLower()
                .GroupBy(c => c)
                .Select(g => Tuple.Create(g.Key, g.Count()))
                .OrderBy(p => p.Item1)
                .ToList();
        }

        private static Occurrences SentenceOccurrences(Words words)
        {
            return WordOccurrences(string.Concat(words));
        }

        private class OccurrencesEqualityComparer : IEqualityComparer<Occurrences>
        {
            public bool Equals(Occurrences x, Occurrences y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(Occurrences x)
            {
                var hashCode = 0;
                foreach (var tuple in x)
                {
                    hashCode += tuple.Item1.GetHashCode();
                    hashCode += tuple.Item2.GetHashCode();
                }
                return hashCode;
            }
        }

        private static IDictionary<Occurrences, Words> _dictionaryByOccurrences;
        private static IDictionary<Occurrences, Words> DictionaryByOccurrences {
            get
            {
                if (_dictionaryByOccurrences == null)
                {
                    var lookup = Dictionary
                        .GroupBy(WordOccurrences)
                        .ToLookup(
                            g => g.Key,
                            g => g.ToList() as Words,
                            new OccurrencesEqualityComparer());

                    _dictionaryByOccurrences = lookup.ToDictionary(
                        x => x.Key,
                        x => x.SelectMany(words => words).ToList() as Words,
                        new OccurrencesEqualityComparer());
                }

                return _dictionaryByOccurrences;
            }
        }

        private static IEnumerable<Occurrences> Combinations(Occurrences occurrences)
        {
            var occurrencesAsList = occurrences.ToList();

            if (!occurrencesAsList.Any())
            {
                yield return new List<Tuple<char, int>>();
                yield break;
            }

            var hd = occurrencesAsList.First();
            var tl = occurrencesAsList.Skip(1).ToList();
            var c = hd.Item1;
            var n = hd.Item2;

            foreach (var x in Enumerable.Range(0, n + 1))
            {
                var first = (x > 0) ? Tuple.Create(c, x) : null;
                foreach (var others in Combinations(tl))
                {
                    var y = new List<Tuple<char, int>>();
                    if (first != null) y.Add(first);
                    y.AddRange(others);
                    yield return y;
                }
            }
        }

        private static Occurrences Subtract(Occurrences x, Occurrences y)
        {
            var xm = x.ToDictionary(a => a.Item1, a => a.Item2);
            var ym = y.ToDictionary(a => a.Item1, a => a.Item2);
            var zm = new Dictionary<char, int>();

            foreach (var xkvp in xm)
            {
                var c = xkvp.Key;
                var nx = xkvp.Value;
                int ny;
                if (ym.TryGetValue(c, out ny))
                    zm[c] = nx - ny;
                else
                    zm[c] = nx;
            }

            return zm
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => Tuple.Create(kvp.Key, kvp.Value))
                .OrderBy(tuple => tuple.Item1)
                .ToList();
        }

        private static IEnumerable<Sentence> SentenceAnagrams(Sentence sentence)
        {
            var allOccurrences = SentenceOccurrences(sentence);
            return SentenceAnagramsIter(allOccurrences).ToList();
        }

        private static IEnumerable<Words> SentenceAnagramsIter(Occurrences occurrences)
        {
            var occurrencesAsList = occurrences.ToList();

            if (!occurrencesAsList.Any())
            {
                yield return new List<Word>();
                yield break;
            }

            foreach (var combination in Combinations(occurrencesAsList))
            {
                var combinationAsList = combination.ToList();
                var remainingOccurrences = Subtract(occurrencesAsList, combinationAsList).ToList();
                foreach (var word in WordsForCombination(combinationAsList))
                {
                    foreach (var innerSentence in SentenceAnagramsIter(remainingOccurrences))
                    {
                        var sentence = new List<Word> {word};
                        sentence.AddRange(innerSentence);
                        yield return sentence;
                    }
                }
            }
        }

        private static Words WordsForCombination(Occurrences occurrences)
        {
            Words words;
            return DictionaryByOccurrences.TryGetValue(occurrences, out words) ? words : Enumerable.Empty<Word>();
        }
    }
}
