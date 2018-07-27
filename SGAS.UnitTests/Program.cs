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

            SGASTest.MintTokens();
            //SGASTest.Refund();
            //SGASTest.Verify();

            Console.ReadLine();
        }
    }
}
