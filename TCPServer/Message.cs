using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServer
{
	[Serializable]
	public class Message
	{
		public MessageType Type { get; set; }
		public string Time { get; set; }
		public string UserName { get; set; }
		public string Text { get; set; }
		public string AttachedFileName { get; set; }
		public byte[] AttachedFileData { get; set; }
		public Message(
			MessageType messageType,
			string messageTime,
			string userName,
			string messageText,
			string attachedFileName,
			byte[] attachedFileData)
		{
			Type = messageType;
			Time = messageTime;
			UserName = userName;
			Text = messageText;
			AttachedFileName = attachedFileName;
			AttachedFileData = attachedFileData;
		}
	}
}
