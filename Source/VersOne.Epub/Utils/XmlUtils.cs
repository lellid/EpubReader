﻿using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace VersOne.Epub.Internal
{
    internal static class XmlUtils
    {
        public static async Task<XDocument> LoadDocumentAsync(Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                memoryStream.Position = 0;
                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore,
                    Async = true
                };
                using (XmlReader xmlReader = XmlReader.Create(memoryStream, xmlReaderSettings))
                {
                    return await Task.Run(() => LoadXDocument(memoryStream)).ConfigureAwait(false);
                }
            }
        }

        private static XDocument LoadXDocument(MemoryStream memoryStream)
        {
            try
            {
                return XDocument.Load(memoryStream);
            }
            catch (System.Xml.XmlException xx)
            {
                if (xx.Message.StartsWith("Version number '1.1'")) // try to solve the known problem that .NET framework does not support XML version 1.1
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var buffer = new byte[512];
                    var read = memoryStream.Read(buffer, 0, buffer.Length); // read first 512 byte

                    for (int i = 2; i < read; ++i) // search for "1.1" in the buffer
                    {
                        if (buffer[i - 2] == 0x31 && buffer[i - 1] == 0x2E && buffer[i] == 0x31) // if string is "1.1"
                        {
                            memoryStream.Seek(i, SeekOrigin.Begin); // seek to index i
                            memoryStream.WriteByte(0x30);           // replace by '0' to get version number "1.0"
                            memoryStream.Seek(0, SeekOrigin.Begin); // rewind memory stream
                            return XDocument.Load(memoryStream);
                        }
                    }
                }
                throw;
            }
        }
    }
}
