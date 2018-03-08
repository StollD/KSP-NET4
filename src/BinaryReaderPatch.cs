using System;
using System.IO;
using System.Reflection;
using System.Text;
using FinePrint;
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
            Int32 ret = ReadChar();
            BaseStream.Position = pos;
            return ret;
        }
    }
}