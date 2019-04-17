using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tali
{
    public class BillInformation
	{
		/*
		 "_id": "5c43a83eb8e2a665da3ebacc",
		"status": "recurring",
		"payee": "Honda",
		"nickname": "Car Loans",
		"payment_date": "2019-02-20",
		"recurring_date": 5,
		"payment_amount": 400,
		"creation_date": "2019-01-19",
		"account_id": "5c3f8b60322fa06b67794349",
		"upcoming_payment_date": "2019-02-05"
		 */

		public string _id { get; set; }
		public string status { get; set; }
		public string payee { get; set; }
		public string nickname { get; set; }
		public string payment_Date { get; set; }
		public int recurring_date { get; set; }
		public int payment_amount { get; set; }
		public string creation_date { get; set; }
		public string account_id { get; set; }
		public string upcoming_payment_date { get; set; }
	}
}
