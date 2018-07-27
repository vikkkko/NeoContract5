using Neo;
using Neo.Core;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeoContract.UnitTests
{
    public static class SGASTest
    {
        //SGAS MintTokens
        public static void MintTokens()
        {
            var inputs = new List<CoinReference> {
                //coin reference A
                new CoinReference(){
                    PrevHash = new UInt256("0x411e85c38660d0acfc3aa52832558d6ce32e2aec803cf21c93dcdf8daca30dc5".Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //1
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>{ new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AHebA26s4HQxQvZHDewQG835rKh2N5QDSW"), //SGAS 地址
                Value = new Fixed8((long)(1 * (long)Math.Pow(10, 8))) //Value
            }}.ToArray();

            var scriptHash = new UInt160("0xc7f35a63a8b81725e9d5e573fca754c7e6928f14".Remove(0, 2).HexToBytes().Reverse().ToArray());

            Transaction tx = null;

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(scriptHash, "mintTokens");
                sb.Emit(OpCode.THROWIFNOT);

                byte[] nonce = new byte[8];
                Random rand = new Random();
                rand.NextBytes(nonce);
                sb.Emit(OpCode.RET, nonce);
                tx = new InvocationTransaction
                {
                    Version = 1,
                    Script = sb.ToArray(),
                    Outputs = outputs,
                    Inputs = inputs,
                    Attributes = new TransactionAttribute[0],
                    Scripts = new Witness[0]
                };
            }

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

            try
            {
                tx = Transaction.DeserializeFrom(tx.ToArray());
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid Transaction Format");
            }

            Console.WriteLine("Verify Transaction:" + tx.Verify(new List<Transaction> { tx }));

            Console.WriteLine("Raw Transaction:");
            Console.WriteLine(tx.ToArray().ToHexString());

            //Then Call neo-cli API：sendrawtransaction in postman.
        }

        public static void Refund()
        {
            var inputs = new List<CoinReference> {
                //coin reference A
                new CoinReference(){
                    PrevHash = new UInt256("0xd054297e3f5cd6b0de998cedf795d9cfa7242e834845e72d61c7c2fa5573e086".Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //1
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>{ new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AHebA26s4HQxQvZHDewQG835rKh2N5QDSW"), //SGAS 地址
                Value = new Fixed8((long)(1 * (long)Math.Pow(10, 8))) //Value
            }}.ToArray();

            Transaction tx = null;

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                var scriptHash = new UInt160("0xc7f35a63a8b81725e9d5e573fca754c7e6928f14".Remove(0, 2).HexToBytes().Reverse().ToArray());
                var from = Wallet.ToScriptHash("AJd31a8rYPEBkY1QSxpsGy8mdU4vTYTD4U");
                sb.EmitAppCall(scriptHash, "refund", from);
                sb.Emit(OpCode.THROWIFNOT);

                byte[] nonce = new byte[8];
                Random rand = new Random();
                rand.NextBytes(nonce);
                sb.Emit(OpCode.RET, nonce);

                using (ScriptBuilder invocScript = new ScriptBuilder())
                {
                    invocScript.EmitPush(2);
                    invocScript.EmitPush(1);

                    //附加人的签名
                    //var witnesSsign = new Witness();
                    //witnessSign.InvocationScript = ;

                    var witness = new Witness
                    {
                        InvocationScript = invocScript.ToArray(),
                        VerificationScript = Blockchain.Default.GetContract(scriptHash).Script
                    };
                    //witness.VerificationScript =;
                    tx = new InvocationTransaction
                    {
                        Version = 0,
                        Script = sb.ToArray(),
                        Outputs = outputs,
                        Inputs = inputs,
                        Attributes = new TransactionAttribute[0],
                        //{
                        //    new TransactionAttribute
                        //    {
                        //        Usage = TransactionAttributeUsage.Script,
                        //        Data = from.ToArray()//附加人的签名
                        //    }
                        //},
                        Scripts = new Witness[] { witness }
                    };
                }
            }

            if (tx == null)
            {
                Console.WriteLine("Create Transaction Failed");
                Console.ReadLine();
                return;
            }

            try
            {
                tx = Transaction.DeserializeFrom(tx.ToArray());
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid Transaction Format");
            }

            Console.WriteLine("Verify Transaction:" + tx.Verify(new List<Transaction> { tx }));

            Console.WriteLine("Raw Transaction:");
            Console.WriteLine(tx.ToArray().ToHexString());

            //Then Call neo-cli API：sendrawtransaction in postman.
        }
    }
}
