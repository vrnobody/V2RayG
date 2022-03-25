using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apis.Tests
{
    [TestClass]
    public class ZipExtensionsTests
    {
        public ZipExtensionsTests()
        { }

        [DataTestMethod]
        [DataRow("hello, world!")]
        [DataRow("he中llo780, wo文rld!123")]
        [DataRow("")]
        public void ZipBase64StringTest(string s)
        {
            var cs = Apis.Libs.Infr.ZipExtensions.CompressToBase64(s);
            var de = Apis.Libs.Infr.ZipExtensions.DecompressFromBase64(cs);

            Assert.AreEqual(s, de);
        }

        [DataTestMethod]
        [DataRow("hello, world!")]
        [DataRow("he中llo780, wo文rld!123")]
        [DataRow("")]
        public void ZipByteTest(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            var cb = Apis.Libs.Infr.ZipExtensions.Compress(bytes);
            var de = Apis.Libs.Infr.ZipExtensions.Decompress(cb);

            Assert.IsTrue(bytes.SequenceEqual(de));

        }

    }

}
