using System;
using System.IO;
using System.Reflection;
using System.Text;
using Harmony;

namespace KSPNET4
{
    [HarmonyPatch(typeof(BinaryReader))]
    [HarmonyPatch("PeekChar")]
    public class PeekCharPatch
    {
        public static Boolean Prefix(ref BinaryReader __instance, ref Int32 __result)
        {
            if (__instance.BaseStream == null)
            {
                throw new ObjectDisposedException(null, "Cannot access a closed file.");
            }

            if (!__instance.BaseStream.CanSeek)
            {
                __result = -1;
                return false;
            }

            if (__instance.BaseStream.Position >= __instance.BaseStream.Length)
            {
                __result = -1;
                return false;
            }

            Int64 pos = __instance.BaseStream.Position;
            Int32 ret = InternalReadChar(__instance);
            __instance.BaseStream.Position = pos;
            __result = ret;
            return false;
        }

        private static Int32 InternalReadChar(BinaryReader reader)
        {
            Int32 num1 = 0;
            Int64 num2 = 0;
            if (reader.BaseStream.CanSeek)
            {
                num2 = reader.BaseStream.Position;
            }

            Byte[] charBytes = new Byte[128];
            Char[] singleChar = new Char[1];
            while (num1 == 0)
            {
                Int32 num3 = reader.BaseStream.ReadByte();
                charBytes[0] = (Byte) num3;
                if (num3 == -1)
                {
                    return -1;
                }

                try
                {
                    num1 = Encoding.ASCII.GetChars(charBytes, 0, 1, singleChar, 0);
                }
                catch
                {
                    if (reader.BaseStream.CanSeek)
                        reader.BaseStream.Seek(num2 - reader.BaseStream.Position, SeekOrigin.Current);
                    throw;
                }
            }

            if (num1 == 0)
            {
                return -1;
            }

            return singleChar[0];
        }
    }
}