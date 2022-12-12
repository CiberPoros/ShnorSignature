namespace ShnorSignature
{
    public static class Extensios
    {
        public static bool IsNull<T>(this T obj) where T : class
        {
            return null == obj;
        }

        public static bool IsNotNull<T>(this T obj) where T : class
        {
            return null != obj;
        }

        public const string ArgumentsPath = @"..\..\..\..\Generator\test.exe";
    }
}
