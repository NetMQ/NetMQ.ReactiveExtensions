using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetMQ.ReactiveExtensions
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Intent: Represent an exception as XML data. In the Visual Studio debugger view for a variable, we can "View as XML" to see a formatted version.
    /// https://seattlesoftware.wordpress.com/2008/08/22/serializing-exceptions-to-xml/
    /// </summary>
    public class ExceptionXElement : XElement
    {
        /// <summary>Create an instance of ExceptionXElement.</summary>
        /// <param name="exception">The Exception to serialize.</param>
        public ExceptionXElement(Exception exception)
            : this(exception, false)
        { }

        /// <summary>Create an instance of ExceptionXElement.</summary>
        /// <param name="exception">The Exception to serialize.</param>
        /// <param name="omitStackTrace">
        /// Whether or not to serialize the Exception.StackTrace member
        /// if it's not null.
        /// </param>
        public ExceptionXElement(Exception exception, bool omitStackTrace)
            : base(new Func<XElement>(() =>
            {
                // Validate arguments.
                if (exception == null)
                {
                    throw new ArgumentNullException(nameof(exception));
                }

                // The root element is the Exception's type.
                // TODO: When .NET Core 1.1 is available, fix these lines.
                string type = nameof(exception).ToString(); // Compatible with .NET Core 1.0.
                //string type = exception.GetType().ToString(); // Compatible with .NET Core 1.1+.
                XElement root = new XElement(type);

                root.Add(new XElement("Message", exception.Message));

                // StackTrace can be null, e.g. "new ExceptionAsXml(new Exception())".
                if (!omitStackTrace && exception.StackTrace != null)
                {
                    root.Add
                    (
                        new XElement("StackTrace",
                            from frame in exception.StackTrace.Split('\n')
                            let prettierFrame = frame.Substring(6).Trim()
                            select new XElement("Frame", prettierFrame))
                    );
                }

                // Data is never null; it's empty if there is no data.
                if (exception.Data.Count > 0)
                {
                    root.Add
                    (
                        new XElement("Data",
                            from entry in
                                exception.Data.Cast<DictionaryEntry>()
                            let key = entry.Key.ToString()
                            let value = entry.Value?.ToString() ?? "null"
                            select new XElement(key, value))
                    );
                }

                // Recursively add any InnerExceptions if they exist.
                if (exception.InnerException != null)
                {
                    root.Add
                    (
                        new ExceptionXElement
                            (exception.InnerException, omitStackTrace)
                    );
                }

                return root;
            })())
        { }
    }
}
