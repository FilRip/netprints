using System.Collections.Generic;

namespace NetPrints.Helpers
{
    public static class ExtensionsList
    {
        public static void MoveRange<T>(this List<T> list, int start, int count, int newStart) where T : class
        {
            List<T> backup = list.GetRange(start, count);
            list.RemoveRange(start, count);
            list.InsertRange(newStart, backup);
        }
    }
}
