// Bot Requirements
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

// LUIS Intents
using System;
using System.Web;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tali
{
    public class QueryCommands
	{
		public static async Task<string> GetBalanceAsync(String accountID)
		{
			var client = new HttpClient();

			if (accountID == null || accountID == "")
			{
				return "Invalid Account ID in QueryCommands.GetBalanceAsync";
			}

			string queryString = "http://api.reimaginebanking.com/accounts/" + accountID + "?key";

			string responseString = await client.GetStringAsync(queryString);

			// List<AccountInformation> result = JsonConvert.DeserializeObject<List<AccountInformation>>(responseString);

			AccountInformation result = JsonConvert.DeserializeObject<AccountInformation>(responseString);

			return result.balance.ToString();
		}

		public static async Task<int> GetNumBillsAsync(String accountID)
		{
			var client = new HttpClient();

			if (accountID == null || accountID == "")
			{
				return 0;
			}

			string queryString = "http://api.reimaginebanking.com/accounts/" + accountID + "/bills";

			string responseString = await client.GetStringAsync(queryString);

			if (String.Equals(responseString, "[]"))
			{
				return 0;
			}
			else
			{
				List<BillInformation> result = JsonConvert.DeserializeObject<List<BillInformation>>(responseString);
				return result.Count;
			}

		}

		public static async Task<List<BillInformation>> GetBillsListAsync(String accountID)
		{
			var client = new HttpClient();

			if (accountID == null || accountID == "")
			{
				return null;
			}

			string queryString = "http://api.reimaginebanking.com/accounts/" + accountID + "/bills";

			string responseString = await client.GetStringAsync(queryString);

			List<BillInformation> result = JsonConvert.DeserializeObject<List<BillInformation>>(responseString);

			return result;
		}
	}
}
