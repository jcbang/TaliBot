using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tali
{
    public class AccountInformation
	{
		public string _id { get; set; }
		public string type { get; set; }
		public string nickname { get; set; }
		public int rewards { get; set; }
		public int balance { get; set; }
		// public string account_number { get; set; }
		public string customer_id { get; set; }
	}
}
