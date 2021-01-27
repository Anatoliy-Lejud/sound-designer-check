using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.UBindr.Examples.Tutorial
{
    public class NumberList : MonoBehaviour
    {
        public NumberList()
        {
            Numbers = Enumerable.Range(0, 64).Select(x => new Number { Value = x, NumberList = this }).ToList();
        }

        public class Number
        {
            public int Value { get; set; }
            public NumberList NumberList { get; set; }

            public void DeleteMe()
            {
                NumberList.Numbers.Remove(this);
            }
        }

        public List<Number> Numbers { get; set; }

        //public IEnumerable<IEnumerable<Number>> Enbatched
        //{
        //    get
        //    {
        //        return Enbatch(Numbers, 8);
        //    }
        //}

        //public IEnumerable<IEnumerable<T>> Enbatch<T>(IEnumerable<T> items, int batchSize)
        //{
        //    var left = items.ToList();
        //    while (left.Any())
        //    {
        //        yield return left.Take(batchSize);
        //        left = left.Skip(batchSize).ToList();
        //    }
        //}
    }
}