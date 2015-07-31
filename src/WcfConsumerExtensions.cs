using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace HallLibrary.Extensions {
	public static class WcfConsumerExtensions {
		public static TReturn CallWCFMethodWithHeaders<TChannel, TReturn> (TChannel channel, IEnumerable<System.ServiceModel.Channels.MessageHeader> headers, Func<TChannel, TReturn> callWCFMethod) {
			using (var ocs = new OperationContextScope((System.ServiceModel.IContextChannel)channel)) {
				foreach (var header in headers)
					OperationContext.Current.OutgoingMessageHeaders.Add(header);
				return callWCFMethod(channel);
			}
		}
		
		public static void CallWCFMethodWithHeaders<TChannel> (TChannel channel, IEnumerable<System.ServiceModel.Channels.MessageHeader> headers, Action<TChannel> callWCFMethod) {
			CallWCFMethodWithHeaders(channel, headers, ch => { callWCFMethod(ch); return 0; });
		}
		
		public static System.ServiceModel.Channels.MessageHeader CreateWCFHeader<T> (string name, T value, string nameSpace = null) {
			return new System.ServiceModel.MessageHeader<T>(value).GetUntypedHeader(name, nameSpace ?? string.Empty);
		}
	}
	
	public struct TraceData
	{
		public string Computer;
		public DateTime TimeCreated;
		public string Action;
		public string MessageType;
		public string Source;
		public string Address;
		public XElement Content;
	}
	
	public static class TraceHelper {
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
			Func<XElement, bool> where = xe => xe.Name.LocalName == localName && ensureEndsInSlash(xe.Name.NamespaceName).Dump().Equals(namespaceURI.Dump()).Dump();
			
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
	
		public static IEnumerable<TraceData> ReadTracesFromFile(string path)
		{
			var xr = XmlTextReader.Create(path, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
			var skipMessageTypes = new[] { "System.ServiceModel.Channels.NullMessage", "System.ServiceModel.Description.ServiceMetadataExtension+HttpGetImpl+MetadataOnHelpPageMessage" };
			var skipActions = new[] { "http://tempuri.org/IConnectionRegister/", "http://schemas.xmlsoap.org/" };
	
			xr.Read();
			do
			{
				if (xr.NodeType == XmlNodeType.Element && xr.Name.Equals("E2ETraceEvent"))
				{
					var xdr = xr.ReadSubtree();
					
					xdr.Read();
					var xd = (XElement)XDocument.ReadFrom(xdr);
					var traceData = new TraceData();
					
					var system = xd.GetElementByName("System");
					traceData.TimeCreated = DateTime.Parse(system.GetElementByName("TimeCreated").AttributeAnyNS("SystemTime").Value);
					traceData.Computer = system.GetElementByName("Computer").Value;
					traceData.ActivityID = Guid.Parse(system.GetElementByName("Correlation").AttributeAnyNS("ActivityID").Value);
	
					var dataItem = xd.GetElementByName("ApplicationData").GetElementByName("TraceData")?.GetElementByName("DataItem");
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
							yield return traceData;
						}
						else
						{
							traceData.MessageType = messageLog.AttributeAnyNS("Type").Value;
							if (!skipMessageTypes.Contains(traceData.MessageType))
							{
								var source = messageLog.AttributeAnyNS("Source").Value;
	
								traceData.Source = source;
	
								var soapEnvelope = messageLog.GetElementByName("Envelope", true);
								if (soapEnvelope == null)
								{
									messageLog.Dump();
								}
								else
								{
									var header = soapEnvelope.GetElementByName("Header", true);
									traceData.Action = messageLog.GetElementByName("Addressing")?.GetElementByName("Action").Value ?? header?.GetElementByName("Action", true)?.Value;
									traceData.Address = header?.GetElementByName("To", true)?.Value;
									var body = soapEnvelope.GetElementByName("Body", true);
									if (body != null)
										traceData.Content = body.Elements().FirstOrDefault();
									
								}
	
								if (traceData.Action == null || !skipActions.Any(traceData.Action.StartsWith))
									yield return traceData;
							}
						}
					}
				}
			} while (xr.ReadToNextSibling("E2ETraceEvent"));
		}
	}
}
