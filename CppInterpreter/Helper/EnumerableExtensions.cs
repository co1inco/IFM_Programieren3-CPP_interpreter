namespace CppInterpreter.Helper;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> collection)
    {
        /// <summary>
        /// Variation of the <see cref="Enumerable.Zip"/> function
        /// </summary>
        /// <param name="other"></param>
        /// <typeparam name="TR"></typeparam>
        /// <returns></returns>
        public IEnumerable<(T? Left, TR? Right)> ZipFill<TR>(IEnumerable<TR> other)
        {
            using var l = collection.GetEnumerator();
            using var r = other.GetEnumerator();

            var lNext = l.MoveNext();
            var rNext = r.MoveNext();
            
            while (lNext && rNext)
            {
                yield return (l.Current, r.Current);
                
                lNext = l.MoveNext();
                rNext = r.MoveNext();
            }

            while (lNext)
            {
                yield return (l.Current, default(TR));
                lNext = l.MoveNext();
            }
            
            while (rNext)
            {
                yield return (default(T), r.Current);
                rNext = r.MoveNext();
            }
        }
        
        
        public void ForEach(Action<T> function)
        {
            foreach (var item in collection)
            {
                function(item);
            }
        }

    }
}