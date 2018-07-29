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
                    PrevHash = new UInt256("0x2eb5b375f7051cafe8b96d7901cd98491e1bc608a9fb9911908f425bb30f3f95".Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //16639 GAS
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>{ new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AG2wnhxQDoerwMSGHCZxtdKCnGKg2Q2HiG"), //SGAS 地址
                Value = new Fixed8((long)(1 * (long)Math.Pow(10, 8))) //Value
            },new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AJd31a8rYPEBkY1QSxpsGy8mdU4vTYTD4U"), //找零地址
                Value = new Fixed8((long)(16638 * (long)Math.Pow(10, 8))) //Value
            }}.ToArray();

            var scriptHash = new UInt160("0x8db627d6e7773afd4368cdca4d09396354b2d902".Remove(0, 2).HexToBytes().Reverse().ToArray());

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
                new CoinReference(){
                    PrevHash = new UInt256("0xa657e8e385649ea31d5b973ed3617fe295b96b3a45acca125bdac6809620983c".Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //1
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>{ new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AG2wnhxQDoerwMSGHCZxtdKCnGKg2Q2HiG"), //SGAS 地址
                Value = new Fixed8((long)(1 * (long)Math.Pow(10, 8))) //Value
            }}.ToArray();

            Transaction tx = null;
            
            var scriptHash = new UInt160("0x8db627d6e7773afd4368cdca4d09396354b2d902".Remove(0, 2).HexToBytes().Reverse().ToArray());
            var applicationScript = new byte[0];
            var from = Wallet.ToScriptHash("AJd31a8rYPEBkY1QSxpsGy8mdU4vTYTD4U");
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(scriptHash, "refund", from);
                sb.Emit(OpCode.THROWIFNOT);
                applicationScript = sb.ToArray();
            }
            
            tx = new InvocationTransaction
            {
                Version = 0,
                Script = applicationScript,
                Outputs = outputs,
                Inputs = inputs,
                Attributes = new TransactionAttribute[]
                {
                    new TransactionAttribute
                    {
                        Usage = TransactionAttributeUsage.Script,
                        Data = from.ToArray()//附加人的 Script Hash
                    }
                }
            };


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

            //Sign in wallet 生成附加人的签名
            var context = new ContractParametersContext(tx);
            var additionalSignature = new byte[0];
            foreach (UInt160 hash in context.ScriptHashes)
            {
                if (hash == from)
                {
                    WalletAccount account = wallet.GetAccount(hash);
                    if (account?.HasKey != true) continue;
                    KeyPair key = account.GetKey();
                    additionalSignature = context.Verifiable.Sign(key);
                }
            }
            var additionalVerificationScript = new byte[0];
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(additionalSignature);
                additionalVerificationScript = sb.ToArray();
            }

            var witness = new Witness
            {
                InvocationScript = applicationScript,
                VerificationScript = Blockchain.Default.GetContract(scriptHash).Script
            };
            var additionalWitness = new Witness
            {
                InvocationScript = additionalVerificationScript,
                VerificationScript = "2103ad1d70f140d84a90ad4491cdf175fa64bfa9287a006e8cbd8f8db8500b5205baac".HexToBytes()
            };
            var witnesses = new Witness[2] { witness, additionalWitness };
            tx.Scripts = witnesses.ToList().OrderBy(p => p.ScriptHash).ToArray();

            try
            {
                tx = Transaction.DeserializeFrom(tx.ToArray());
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid Transaction Format");
            }

            if (tx.Verify(new List<Transaction> { tx }))
            {
                Console.WriteLine("Verify Transaction: True");
                Console.WriteLine("Raw Transaction:");
                Console.WriteLine(tx.ToArray().ToHexString());
                //Console.WriteLine(tx.ToJson());


                //Then Call neo-cli API：sendrawtransaction in postman.
            }
            else
            {
                Console.WriteLine("Verify Transaction: False");
            }
        }

        public static void Verify()
        {
            var inputs = new List<CoinReference> {
                new CoinReference(){
                    PrevHash = new UInt256("0x2795c2660e962f012532d5b15454b8238d859d7d135e1cc353d1016930adb003".Remove(0, 2).HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //1
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>{ new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AG2wnhxQDoerwMSGHCZxtdKCnGKg2Q2HiG"), //SGAS 地址
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
            var scriptHash = new UInt160("0x8db627d6e7773afd4368cdca4d09396354b2d902".Remove(0, 2).HexToBytes().Reverse().ToArray());

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
            if (tx.Verify(new List<Transaction> { tx }))
            {
                Console.WriteLine("Verify Transaction: True");
                Console.WriteLine("Raw Transaction:");
                Console.WriteLine(tx.ToArray().ToHexString());
                //Console.WriteLine(tx.ToJson());


                //Then Call neo-cli API：sendrawtransaction in postman.
            }
            else
            {
                Console.WriteLine("Verify Transaction: False");
            }
        }
    }
}
