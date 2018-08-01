using System;
using System.Linq;
using System.Net;
using Neo;
using Neo.Core;
using Neo.Implementations.Wallets.NEP6;
using Neo.IO;
using SGAS.UnitTests;

namespace NeoContract.UnitTests
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start private net and deploy the contract

            // -------------------------------------
            // Configure
            // -------------------------------------

            var rpc = new RpcClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10332));
            var sgasHash = new UInt160("4913c8ee9eb88fcb8ab3b87af78d6519990cf213".HexToBytes().Reverse().ToArray());
            var amount = new Fixed8(5 * (long)Math.Pow(10, 8));
            var token = Blockchain.UtilityToken.Hash;

            // for the find unspent coins

            var txOutputHash = new UInt256("d25a9729513a75cd3b1c9045a6f866250d7e85a49bfaa5074f28418ff055898d".HexToBytes().Reverse().ToArray());

            // Open wallet

            var wallet = new NEP6Wallet("./1234567890.json");
            var walletPassword = "1234567890";

            // -------------------------------------

            // We need the BC only for sign (references

            Blockchain.RegisterBlockchain(new RPCBlockChain(rpc));

            // -----------------------------------------------------------------

            Console.ForegroundColor = ConsoleColor.White;
            Transaction txMint, txVerify, txRefund;
            var test = new SGASTest(rpc, sgasHash, token);
            using (wallet.Unlock(walletPassword))
            {
                Console.WriteLine("Mint");

                //txMint = rpc.GetTransaction(txOutputHash);
                txMint = test.MintTokens(wallet, amount, txOutputHash);

                Console.WriteLine("Press enter for: send Mint");
                Console.ReadLine();
                Console.WriteLine(rpc.SendTransaction(txMint).ToString());

                Console.WriteLine("Verify");

                txVerify = test.Verify(wallet, amount, txMint);

                Console.WriteLine("Refund");

                txRefund = test.Refund(wallet, txVerify.Hash, txMint);
            }

            // Send rpc

            Console.WriteLine("Press enter for: send Refund");
            Console.ReadLine();
            Console.WriteLine(rpc.SendTransaction(txRefund).ToString());

            Console.WriteLine("Press enter for: send Verify");
            Console.ReadLine();
            Console.WriteLine(rpc.SendTransaction(txMint).ToString());
        }
    }
}