using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPClient
{
	[Serializable]
    public class User
    {
		public string Name { get; set; }
		public string Message { get; set; }

		public User(string name)
		{
			Name = name;
		}

		public User()
		{

		}
	}
}
