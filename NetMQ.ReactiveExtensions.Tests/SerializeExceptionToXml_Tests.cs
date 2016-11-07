using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace NetMQ.ReactiveExtensions.Tests
{
    public class SerializeExceptionToXml_Tests
    {

        [TestFixture]
        public static class Serialize_Exception_To_Xml_Tests
        {
            [Test]
            public static void Convert_Exception_To_XML()
            {
                ApplicationException outerException = new ApplicationException();
                ExceptionXElement xmlRaw;
                {
                    try
                    {
                        var path = File.ReadAllLines("file does not exist");
                    }
                    catch (Exception ex)
                    {
                        outerException = new ApplicationException("Outer exception", ex);
                    }

                    xmlRaw = new ExceptionXElement(outerException);
                }

                var xml = xmlRaw.ToString();    
                          
                Assert.IsTrue(xml.Contains("ApplicationException"));
                Assert.IsTrue(xml.Contains("<"));
                Assert.IsTrue(xml.Contains(">"));
            }
        }
    }
}

