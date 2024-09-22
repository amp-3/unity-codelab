/// <summary>
/// 配列の拡張メソッド
/// </summary>
public static class ArrayExtention
{

    public static bool IsNullOrEmpty<T>(this T[] self)
    {
        return (self == null || self.Length < 1);
    }
}
