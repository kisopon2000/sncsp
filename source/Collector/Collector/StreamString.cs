using System;
using System.IO;
using System.Text;

namespace Collector
{
    class StreamString
    {
        private Stream _m_cIoStream;
        private UnicodeEncoding _m_cStreamEncoding;

        public StreamString(Stream ioStream)
        {
            this._m_cIoStream = ioStream;
            this._m_cStreamEncoding = new UnicodeEncoding();
        }

        public String ReadString()
        {
            Int32 nLength = 0;

            nLength = _m_cIoStream.ReadByte() * 256;
            nLength += _m_cIoStream.ReadByte();
            byte[] inBuffer = new byte[nLength];
            _m_cIoStream.Read(inBuffer, 0, nLength);

            return this._m_cStreamEncoding.GetString(inBuffer);
        }

        public Int32 WriteString(String outString)
        {
            byte[] outBuffer = this._m_cStreamEncoding.GetBytes(outString);
            Int32 nLength = outBuffer.Length;
            if (nLength > UInt16.MaxValue)
            {
                nLength = (Int32)UInt16.MaxValue;
            }
            _m_cIoStream.WriteByte((byte)(nLength / 256));
            _m_cIoStream.WriteByte((byte)(nLength & 255));
            _m_cIoStream.Write(outBuffer, 0, nLength);
            _m_cIoStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}
