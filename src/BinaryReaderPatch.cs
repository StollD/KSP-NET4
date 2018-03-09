using System;
using System.IO;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace KSPNET4
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class BinaryReaderPatch : MonoBehaviour
    {
        void Awake()
        {
            // Fetch the original method
            MethodInfo brpc = typeof(BinaryReader).GetMethod("PeekChar", BindingFlags.Instance | BindingFlags.Public);
            
            // Fetch our replacement
            MethodInfo ftpc = typeof(FakeReader).GetMethod("PeekChar", BindingFlags.Instance | BindingFlags.Public);
            
            // Redirect the original method to the new one
            Detourer.TryDetourFromTo(brpc, ftpc);
        }
    }

    /// <summary>
    /// A dummy type extending BinaryReader so we can create a matching method signature
    /// </summary>
    public class FakeReader : BinaryReader
    {
        public FakeReader([NotNull] Stream input) : base(input)
        {
        }

        public FakeReader([NotNull] Stream input, [NotNull] Encoding encoding) : base(input, encoding)
        {
        }

        public FakeReader(Stream input, Encoding encoding, Boolean leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public override Int32 PeekChar()
        {
            if (BaseStream == null)
                throw new ObjectDisposedException(null, "Cannot access a closed file.");
            if (!BaseStream.CanSeek)
                return -1;
            if (BaseStream.Position >= BaseStream.Length) 
                return -1;
            
            Int64 pos = BaseStream.Position;
            Int32 ret = InternalReadChar();
            BaseStream.Position = pos;
            return ret;
        }

        private Int32 InternalReadChar()
        {
            Int32 num1 = 0;
            Int64 num2 = 0;
            if (BaseStream.CanSeek)
                num2 = BaseStream.Position;
            Byte[] charBytes = new Byte[128];
            Char[] singleChar = new Char[1];
            while (num1 == 0)
            {
                Int32 num3 = BaseStream.ReadByte();
                charBytes[0] = (Byte) num3;
                if (num3 == -1)
                    return -1;
                try
                {
                    num1 = Encoding.ASCII.GetChars(charBytes, 0, 1, singleChar, 0);
                }
                catch
                {
                    if (BaseStream.CanSeek)
                        BaseStream.Seek(num2 - BaseStream.Position, SeekOrigin.Current);
                    throw;
                }
            }
            if (num1 == 0)
                return -1;
            return singleChar[0];
        }
    }
}