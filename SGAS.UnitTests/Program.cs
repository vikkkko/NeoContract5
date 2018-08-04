using System;
using System.Net;
using Neo;
using Neo.Core;
using Neo.Implementations.Wallets.NEP6;
using SGAS.UnitTests;

namespace NeoContract.UnitTests
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start private net and deploy the contract

            // -----------------------------------------------------------------
            // Configure
            // -----------------------------------------------------------------

            var rpcAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10332);

            var sGASContactHash = UInt160.Parse("458b5a1309b659f3594b40f8ddbda467c768f34a");
            var mintAmount = new Fixed8(10 * (long)Math.Pow(10, 8));
            var token = Blockchain.UtilityToken.Hash;

            // for the find unspent coins

            //var outputHashForTxMint = UInt256.Parse("df24cc0193a1d5c6d8da3c5f6bff28f2972040b2a3c99a812b71d2ee495dbb34");
            var inputHashForTxMint = UInt256.Parse("508cef0ee52df2d6467c36f002f137e291149c66ba736f503f13851569740ad8");
            var inputHashForTxRefund = UInt256.Parse("ea4c1b5664e5ca7d49978be997221a9827546273f9b59985d91a2282863fd0f8");

            // Open wallet

            var wallet = new NEP6Wallet("./1234567890.json");
            var walletPassword = "1234567890";

            // -----------------------------------------------------------------
            // Register blockchain , we need the BC only for sign (references)
            // -----------------------------------------------------------------

            var rpc = new RpcClient(rpcAddress);
            Blockchain.RegisterBlockchain(new RPCBlockChain(rpc));

            // -----------------------------------------------------------------

            Console.ForegroundColor = ConsoleColor.White;
            Transaction txMint, txVerify, txRefund;
            var test = new SGASTest(rpc, sGASContactHash, token);

            using (wallet.Unlock(walletPassword))
            {
                Console.WriteLine("Mint");

                // txMint = rpc.GetTransaction(inputHashForTxMint);
                txMint = test.MintTokens(wallet, mintAmount, inputHashForTxMint);

                Console.WriteLine("Press enter for: send Mint");

                Console.ReadLine();
                Console.WriteLine(rpc.SendTransaction(txMint).ToString());
                Console.WriteLine("Refund");

                txRefund = test.Refund(wallet, rpc.GetTransaction(inputHashForTxRefund));

                Console.WriteLine("Press enter for: send Refund (Ensure that the mint was received!)");

                Console.ReadLine();
                Console.WriteLine(rpc.SendTransaction(txRefund).ToString());
                Console.WriteLine("Verify");

                txVerify = test.Verify(wallet, txRefund);

                Console.WriteLine("Press enter for: send Verify");
                Console.ReadLine();

                Console.WriteLine(rpc.SendTransaction(txVerify).ToString());
            }
        }
    }
}