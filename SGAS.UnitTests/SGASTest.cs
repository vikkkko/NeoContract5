using Neo;
using Neo.Core;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
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
                    PrevHash = new UInt256("0x844084349cfa8585d6404aa3d2cf1f879412d62bd5fdbbba092b1f0a4d43fdce".Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //1
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>{ new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AYuD4feLBjDqo6o1Nv6aZJMybPXVUPxVKn"), //SGAS 地址
                Value = new Fixed8((long)(1 * (long)Math.Pow(10, 8))) //Value
            }}.ToArray();

            var scriptHash = new UInt160("0xf5d124ec167db159c9cfec1916121f352042ddbb".Remove(0, 2).HexToBytes().Reverse().ToArray());

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
                    PrevHash = new UInt256("0xf5042f686c8e36bedbe268d07af1791ae0a8e4d4ecce11601fbc31bbd5da9b5f".Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //1
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>{ new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AYuD4feLBjDqo6o1Nv6aZJMybPXVUPxVKn"), //SGAS 地址
                Value = new Fixed8((long)(1 * (long)Math.Pow(10, 8))) //Value
            }}.ToArray();

            Transaction tx = null;

            var verificationScript = new byte[0];
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(2);
                sb.EmitPush("1");
                verificationScript = sb.ToArray();
            }
            var scriptHash = new UInt160("0xf5d124ec167db159c9cfec1916121f352042ddbb".Remove(0, 2).HexToBytes().Reverse().ToArray());
            //var applicationScript = new byte[0];
            //using (ScriptBuilder sb = new ScriptBuilder())
            //{
            //    
            //    var from = Wallet.ToScriptHash("AJd31a8rYPEBkY1QSxpsGy8mdU4vTYTD4U");
            //    sb.EmitAppCall(scriptHash, "refund", from);
            //    sb.Emit(OpCode.THROWIFNOT);

            //    byte[] nonce = new byte[8];
            //    Random rand = new Random();
            //    rand.NextBytes(nonce);
            //    sb.Emit(OpCode.RET, nonce);
            //    applicationScript = sb.ToArray();
            //}

            var witness = new Witness
            {
                InvocationScript = verificationScript,
                //VerificationScript = File.ReadAllBytes("C:\\Users\\chenz\\Documents\\1Code\\chenzhitong\\NeoContract5\\NeoContract\\bin\\Debug\\SGAS.avm")
                VerificationScript = Blockchain.Default.GetContract(scriptHash).Script
            };
            tx = new ContractTransaction
            {
                Version = 0,
                //Script = applicationScript,
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
                Scripts = new Witness[]{ witness }
            };


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

        public static void Verify()
        {
            var inputs = new List<CoinReference> {
                new CoinReference(){
                    PrevHash = new UInt256("0xf5042f686c8e36bedbe268d07af1791ae0a8e4d4ecce11601fbc31bbd5da9b5f".Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //1
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>{ new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AYuD4feLBjDqo6o1Nv6aZJMybPXVUPxVKn"), //SGAS 地址
                Value = new Fixed8((long)(1 * (long)Math.Pow(10, 8))) //Value
            }}.ToArray();

            Transaction tx = null;

            var verificationScript = new byte[0];
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(2);
                sb.EmitPush("1");
                verificationScript = sb.ToArray();
            }
            var scriptHash = new UInt160("0xf5d124ec167db159c9cfec1916121f352042ddbb".Remove(0, 2).HexToBytes().Reverse().ToArray());

            var witness = new Witness
            {
                InvocationScript = verificationScript,
                //未部署的合约不能执行 Storage.Get() 方法，所以要将合约部署，而不是调用本地的 AVM 文件
                //VerificationScript = File.ReadAllBytes("C:\\Users\\chenz\\Documents\\1Code\\chenzhitong\\NeoContract5\\NeoContract\\bin\\Debug\\SGAS.avm")
                VerificationScript = Blockchain.Default.GetContract(scriptHash).Script
            };
            tx = new ContractTransaction
            {
                Version = 0,
                //Script = applicationScript,
                Outputs = outputs,
                Inputs = inputs,
                Attributes = new TransactionAttribute[0],
                Scripts = new Witness[] { witness }
            };


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
