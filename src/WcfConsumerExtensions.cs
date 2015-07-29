using System;
using System.Collections.Generic;

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
		public static IEnumerable<XElement> ElementsAnyNS<T>(this T source, string localName)
			where T : XContainer
		{
			return source.Elements().Where(e => e.Name.LocalName == localName);
		}
		
		public static XElement ElementAnyNS<T>(this T source, string localName)
			where T : XContainer
		{
			return source.ElementsAnyNS(localName).FirstOrDefault();
		}
		
		public static XAttribute AttributeAnyNS<T>(this T source, string localName)
			where T : XElement
		{
			return source.Attributes().FirstOrDefault(a => a.Name.LocalName == localName);
		}
		
		public static IEnumerable<TraceData> ReadTracesFromFile (string path)
		{
			// .svclog
			var xr = XmlTextReader.Create(path, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
			//var lastWord = new Regex(@"[A-Z][a-z]+$");
			var skipMessageTypes = new[] { "System.ServiceModel.Channels.NullMessage", "System.ServiceModel.Description.ServiceMetadataExtension+HttpGetImpl+MetadataOnHelpPageMessage" };
			var skipActions = new[] { "http://tempuri.org/IConnectionRegister/",  "http://schemas.xmlsoap.org/" };
			
			xr.Read();
			do {
				if (xr.Name.Equals("E2ETraceEvent")) {
					var xd = (XElement)XDocument.ReadFrom(xr);
					
					var traceData = new TraceData();
					
					var system = xd.ElementAnyNS("System");
					traceData.TimeCreated = DateTime.Parse(system.ElementAnyNS("TimeCreated").AttributeAnyNS("SystemTime").Value);
					traceData.Computer = system.ElementAnyNS("Computer").Value;
					
					var messageLog = xd.ElementAnyNS("ApplicationData").ElementAnyNS("TraceData").ElementAnyNS("DataItem").ElementAnyNS("MessageLogTraceRecord");
					traceData.MessageType = messageLog.AttributeAnyNS("Type").Value;
					if (skipMessageTypes.Contains(traceData.MessageType))
						continue;
					
					var source = messageLog.AttributeAnyNS("Source").Value;
					
					traceData.Source = source;
					
					var soapEnvelope = messageLog.ElementAnyNS("Envelope");
					if (soapEnvelope == null)
					{
						messageLog.Dump();
					}
					else
					{
						var header = soapEnvelope.ElementAnyNS("Header");
						if (header == null)
						{
							traceData.Action = messageLog.ElementAnyNS("Addressing")?.ElementAnyNS("Action").Value;
						}
						else
						{
							traceData.Action = header.ElementAnyNS("Action")?.Value;
	
							traceData.Address = header.ElementAnyNS("To")?.Value;
						}
	
						traceData.Content = soapEnvelope.ElementAnyNS("Body").Elements().FirstOrDefault();
					}
					
					if (skipActions.Any(traceData.Action.StartsWith))
						continue;
					yield return traceData;
				}
			} while (xr.ReadToNextSibling("E2ETraceEvent"));
		}
	}
}
