namespace HBMP.Extensions
{
    public static class ArrayExtensions
    {
        // This class is needed for the "SimplifiedTransforms" class by Entanglement devs.
        // Shortcut to append a byte array to another
        public static byte[] AddBytes(this byte[] self, byte[] array, ref int index)
        {
            for (var i = 0; i < array.Length; i++)
                self[index++] = array[i];
            return self;
        }

        public static byte[] AddBytes(this byte[] self, byte[] array, int index)
        {
            for (var i = 0; i < array.Length; i++)
                self[index++] = array[i];
            return self;
        }
    }
}