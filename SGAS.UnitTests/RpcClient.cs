using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Neo;
using Neo.Core;
using Neo.IO;
using Newtonsoft.Json.Linq;

namespace SGAS.UnitTests
{
    public class RpcClient
    {
        /// <summary>
        /// End point
        /// </summary>
        public readonly IPEndPoint EndPoint;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="endPoint">End point</param>
        public RpcClient(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        /// <summary>
        /// Make rpc call
        /// </summary>
        public JObject Call(string method, string parameters = null)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                parameters = "[]";
            }

            using (HttpClient wb = new HttpClient())
            {
                var content = new StringContent
                    (
                    "{\"jsonrpc\": \"2.0\", \"method\": \"" + method + "\", \"params\": " + parameters + ", \"id\":1}", Encoding.UTF8,
                    "application/json"
                    );

                var task = wb.PostAsync("http://" + EndPoint.Address.ToString() + ":" + EndPoint.Port.ToString(), content);
                task.Wait();
                var rest = task.Result;

                if (!rest.IsSuccessStatusCode)
                {
                    throw new Exception(rest.StatusCode + " - " + rest.ReasonPhrase);
                }

                var task2 = rest.Content.ReadAsStringAsync();
                task2.Wait();

                return JObject.Parse(task2.Result);
            }
        }

        /// <summary>
        /// Get asset id
        /// </summary>
        /// <param name="hash">Asset id</param>
        /// <returns>Asset state</returns>
        public AssetState GetAssetState(UInt256 hash)
        {
            var json = Call("getassetstate", $"[\"{hash.ToString()}\"]");

            if (json == null || !json.ContainsKey("result")) return null;

            var val = json["result"];

            if (val == null) return null;

            // TODO: Parse

            return new AssetState()
            {
                AssetType = (AssetType)Enum.Parse(typeof(AssetType), val["type"].Value<string>()),
                Precision = val["precision"].Value<byte>(),
                Expiration = val["expiration"].Value<uint>(),
            };
        }

        /// <summary>
        /// Get contract state
        /// </summary>
        /// <param name="hash">Hash</param>
        /// <returns>Contract state</returns>
        public ContractState GetContract(UInt160 hash)
        {
            var json = Call("getcontractstate", $"[\"{hash.ToString()}\"]");

            if (json == null || !json.ContainsKey("result")) return null;

            var val = json["result"];

            if (val == null) return null;

            // TODO: Parse

            return new ContractState()
            {
                Script = val["script"].Value<string>().HexToBytes(),
            };
        }

        /// <summary>
        /// Send transaction
        /// </summary>
        /// <param name="tx">Tx</param>
        public JObject SendTransaction(Transaction tx)
        {
            return Call("sendrawtransaction", $"[\"{tx.ToArray().ToHexString()}\"]");
        }

        /// <summary>
        /// Get transaction
        /// </summary>
        public Transaction GetTransaction(UInt256 hash)
        {
            var json = Call("getrawtransaction", $"[\"{hash.ToString()}\"]");

            if (json == null || !json.ContainsKey("result")) return null;

            var hex = json["result"].Value<string>();

            if (string.IsNullOrEmpty(hex)) return null;

            return Transaction.DeserializeFrom(hex.HexToBytes());
        }
    }
}