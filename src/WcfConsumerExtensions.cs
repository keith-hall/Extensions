using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.ServiceModel;

namespace HallLibrary.Extensions {
	public static class WcfConsumerExtensions {
		public static TReturn CallWCFMethodWithHeaders<TChannel, TReturn> (TChannel channel, IEnumerable<System.ServiceModel.Channels.MessageHeader> headers, Func<TChannel, TReturn> callWCFMethod) {
			using (var ocs = new OperationContextScope((IContextChannel)channel)) {
				foreach (var header in headers)
					OperationContext.Current.OutgoingMessageHeaders.Add(header);
				return callWCFMethod(channel);
			}
		}
		
		public static void CallWCFMethodWithHeaders<TChannel> (TChannel channel, IEnumerable<System.ServiceModel.Channels.MessageHeader> headers, Action<TChannel> callWCFMethod) {
			CallWCFMethodWithHeaders(channel, headers, ch => { callWCFMethod(ch); return 0; });
		}
		
		public static System.ServiceModel.Channels.MessageHeader CreateWCFHeader<T> (string name, T value, string nameSpace = null) {
			return new MessageHeader<T>(value).GetUntypedHeader(name, nameSpace ?? string.Empty);
		}
	}
	
	/// <summary>
	/// Contains methods for parsing and extracting information from a WCF log
	/// </summary>
	public static class TraceHelper
	{
		/// <summary>
		/// Contains information about a single event retrieved from a WCF log
		/// </summary>
		public struct TraceData
		{
			public string Computer;
			public DateTime TimeCreated;
			public string Action;
			public string MessageType;
			public Guid ActivityID;
			public string Source;
			public string Address;
			public XElement Content;
			public XElement Original;
		}
		
		public static IEnumerable<XElement> GetElementsByName<T>(this T source, string name, bool ignoreNamespace = false)
			where T : XContainer
		{
			var names = name.Split(':');
			if (names.Length > 2)
				throw new ArgumentException(nameof(name));
			if (names.Length == 1)
				names = Enumerable.Repeat((string)null, 1).Concat(names).ToArray();
			
			Func<XElement, bool> where = xe => xe.Name.LocalName == names[1] && (ignoreNamespace || Object.Equals(xe.GetPrefixOfNamespace(xe.Name.NamespaceName), names[0]));
			return source.Elements().Where(where);
		}
		
		public static XElement GetElementByName<T>(this T source, string name, bool ignoreNamespace = false)
			where T : XContainer
		{
			return source.GetElementsByName(name, ignoreNamespace).FirstOrDefault();
		}
		
		public static IEnumerable<XElement> GetElementsByNameAndNamespace<T>(this T source, string localName, string namespaceURI)
			where T : XContainer
		{
			Func<string, string> ensureEndsInSlash = s => s.EndsWith("/") ? s : (s + "/");
			Func<XElement, bool> where = xe => xe.Name.LocalName == localName && ensureEndsInSlash(xe.Name.NamespaceName).Equals(namespaceURI);
			
			namespaceURI = ensureEndsInSlash(namespaceURI);
			return source.Elements().Where(where);
		}
		
		public static XElement GetElementByNameAndNamespace<T>(this T source, string localName, string namespaceURI)
			where T : XContainer
		{
			return source.GetElementsByNameAndNamespace(localName, namespaceURI).FirstOrDefault();
		}
		
		public static XAttribute AttributeAnyNS<T>(this T source, string localName)
			where T : XElement
		{
			return source.Attributes().FirstOrDefault(a => a.Name.LocalName == localName);
		}
		
		private static string[] skipMessageTypes = new[] { "System.ServiceModel.Channels.NullMessage", "System.ServiceModel.Description.ServiceMetadataExtension+HttpGetImpl+MetadataOnHelpPageMessage" };
		private static string[] skipActions = new[] { "http://tempuri.org/IConnectionRegister/", "http://schemas.xmlsoap.org/" };
		
		private static Nullable<TraceData> ParseTraceData(XElement traceEvent)
		{
			var traceData = new TraceData();
			traceData.Original = traceEvent;
			
			var system = traceEvent.GetElementByName("System");
			traceData.TimeCreated = DateTime.Parse(system.GetElementByName("TimeCreated").AttributeAnyNS("SystemTime").Value);
			traceData.Computer = system.GetElementByName("Computer").Value;
			traceData.ActivityID = Guid.Parse(system.GetElementByName("Correlation").AttributeAnyNS("ActivityID").Value);
			
			var dataItem = traceEvent.GetElementByName("ApplicationData").GetElementByName("TraceData")?.GetElementByName("DataItem");
			if (dataItem != null)
			{
				var messageLog = dataItem.GetElementByName("MessageLogTraceRecord");
				if (messageLog == null)
				{
					var traceRecord = dataItem.GetElementByName("TraceRecord");
					if (traceRecord == null)
					{
						traceData.MessageType = "String";
						traceData.Content = dataItem;
					}
					else
					{
						traceData.Action = traceRecord.AttributeAnyNS("Severity").Value;
						traceData.MessageType = "Trace";
						
						traceData.Content = traceRecord;
					}
					return traceData;
				}
				else
				{
					traceData.Source = messageLog.AttributeAnyNS("Source")?.Value;
					traceData.MessageType = messageLog.AttributeAnyNS("Type")?.Value;
					if (traceData.MessageType == null && traceData.Source == "Malformed")
					{
						traceData.Content = messageLog;
						return traceData;
					}
					else if (!skipMessageTypes.Contains(traceData.MessageType))
					{
						var soapEnvelope = messageLog.GetElementByName("Envelope", true);
						if (soapEnvelope == null)
						{
								traceData.Content = messageLog;
						}
						else
						{
							var header = soapEnvelope.GetElementByName("Header", true);
							traceData.Action = messageLog.GetElementByName("Addressing")?.GetElementByName("Action").Value ?? header?.GetElementByName("Action", true)?.Value;
							traceData.Address = header?.GetElementByName("To", true)?.Value;
							var body = soapEnvelope.GetElementByName("Body", true);
							if (body != null && body.HasElements)
								traceData.Content = body.Elements().FirstOrDefault();
						}
						
						if (traceData.Action == null || !skipActions.Any(traceData.Action.StartsWith))
							return traceData;
					}
				}
			}
			return null;
		}
		
		public static IEnumerable<TraceData> ReadTracesFromFile(string path)
		{
			var xr = XmlTextReader.Create(path, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
			
			xr.Read();
			do
			{
				if (xr.NodeType == XmlNodeType.Element && xr.Name.Equals("E2ETraceEvent"))
				{
					var xdr = xr.ReadSubtree();
					
					xdr.Read();
					XElement xd = null;
					XmlException xe = null;
					try
					{
						xd = (XElement)XDocument.ReadFrom(xdr);
					}
					catch (XmlException e)
					{
						xe = e;
					}
					if (xe == null)
					{
						var traceData = ParseTraceData(xd);
						
						if (traceData.HasValue)
							yield return traceData.Value;
					} else {
						var exceptionInfo = new TraceData();
						exceptionInfo.Content = new XElement("Exception");
						exceptionInfo.Content.Value = xe.ToString();
						yield return exceptionInfo;
						break;
					}
					
				}
			} while (xr.ReadToNextSibling("E2ETraceEvent"));
		}
		
		public static void WriteTracesToFile(string outputPath, IEnumerable<TraceData> traces)
		{
			using (var file = XmlWriter.Create(outputPath, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				try
				{
					foreach (var trace in traces.Select(td => td.Original ?? td.Content))
					{
						trace.WriteTo(file);
					}
				}
				finally
				{
					file.Close();
				}
			}
		}
		
		public static void FilterTracesFromFile(string inputPath, Func<TraceData, bool> filter, string outputPath)
		{
			WriteTracesToFile(outputPath, ReadTracesFromFile(inputPath).Where(filter));
		}
		
		/// <summary>
		/// Contains information about a request and response pair retrieved from a WCF log
		/// </summary>
		public struct MessageData
		{
			public TraceData Request;
			public TraceData Response;
		}
		
		public static IEnumerable<MessageData> ParseMessagesFromFile(string path, DateTime? since = null)
		{
			var traces = ReadTracesFromFile(path)
				.Where(th => th.Source.StartsWith("ServiceLevel") || (th.Source == "TransportSend" && th.MessageType == "System.ServiceModel.Channels.BodyWriterMessage")); // anything at Service Level, and Fault responses, which are Transport level
			if (since.HasValue)
				traces = traces.Where(th => th.TimeCreated > since);
			var grouped = traces.GroupBy(th => th.ActivityID)
				.Select(g => new MessageData { Request = g.First(th => th.Source.EndsWith("Request")), Response = g.FirstOrDefault(th => th.Source.EndsWith("Reply") || th.Source == "TransportSend") })
				.Where(rp => rp.Request.ActivityID != null && rp.Response.ActivityID != null) // ignore where a request or response is missing - incomplete data due to WCF service being in use and still writing to log...
			.OrderBy(rp => rp.Request.TimeCreated);
			return grouped;
		}
	}
}
