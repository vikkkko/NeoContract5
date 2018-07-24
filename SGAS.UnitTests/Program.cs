using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Neo;
using Neo.Core;
using Neo.Implementations.Blockchains.LevelDB;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using VMArray = Neo.VM.Types.Array;

namespace NeoContract.UnitTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //Need libleveldb.dll, and requires a platform(x86 or x64) that is consistent with the program.
            //Path of blockchain folder
            Blockchain.RegisterBlockchain(new LevelDBBlockchain("C:\\Users\\chenz\\Desktop\\PrivateNet\\neo-gui 2.7.6\\Chain_0001142D"));

            var tx = SGASTest.MintTokens();

            if (tx == null)
            {
                Console.WriteLine("Create Transaction Failed");
                Console.ReadLine();
                return;
            }

            //Open wallet
            var wallet = new Neo.Implementations.Wallets.NEP6.NEP6Wallet("0.json");
            try
            {
                wallet.Unlock("1");
            }
            catch (Exception)
            {
                Console.WriteLine("password error");
            }

            //Sign in wallet
            var context = new ContractParametersContext(tx);
            wallet.Sign(context);
            if (context.Completed)
            {
                Console.WriteLine("Sign Successful");
                tx.Scripts = context.GetScripts();
            }
            else
            {
                Console.WriteLine("Sign Faild");
            }
            
            Console.WriteLine("Verify Transaction:" + tx.Verify(new List<Transaction> { tx }));

            Console.WriteLine("Raw Transaction:");
            Console.WriteLine(tx.ToArray().ToHexString());

            //Then Call neo-cli API：sendrawtransaction in postman.

            Console.ReadLine();
        }
    }
}
