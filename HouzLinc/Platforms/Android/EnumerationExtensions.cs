using Java.Util;

namespace HouzLinc.Droid;

/// <summary>
/// Extensions to convert a Java.Util.Enumeration to a .NET IEnumerable
/// </summary>
public static class EnumerationExtensions
{
    public static IEnumerable<T> ToIEnumerable<T>(this IEnumeration enumeration) where T: Java.Lang.Object
    {
        while (enumeration.HasMoreElements)
        {
            yield return (T)enumeration.NextElement();
        }
    }
}

